using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.Inventory;
using FarmManagement.Application.Interfaces.Inventory;
using FarmManagement.Application.Services;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Services;

public sealed class InventoryStockServiceTests
{
    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    private InventoryActor CreateActor() => new(_userId, _organizationId);

    [Fact]
    public async Task RecordOpeningStockAsync_FutureDate_ThrowsValidationException()
    {
        var store = new FakeInventoryStockStore();
        var (item, loc) = SetupItemAndLocation(store, _organizationId);
        var service = new InventoryStockService(store);

        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));
        var request = new RecordOpeningStockRequest(loc.FarmId, loc.Id, item.Id, 100m, futureDate, "Opening");

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.RecordOpeningStockAsync(CreateActor(), request, "127.0.0.1"));

        Assert.Contains("future", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RecordOpeningStockAsync_DuplicateOpeningStock_ThrowsConflictException()
    {
        var store = new FakeInventoryStockStore();
        var (item, loc) = SetupItemAndLocation(store, _organizationId);
        var service = new InventoryStockService(store);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var request = new RecordOpeningStockRequest(loc.FarmId, loc.Id, item.Id, 50m, today, "Initial opening stock");
        await service.RecordOpeningStockAsync(CreateActor(), request, "127.0.0.1");

        // Attempting second opening stock for the same item and location must fail with ConflictException
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            service.RecordOpeningStockAsync(CreateActor(), request, "127.0.0.1"));

        Assert.Contains("Opening stock has already been recorded", ex.Message);
    }

    [Fact]
    public async Task RecordStockReceiptAsync_WhenBalanceDoesNotExist_CreatesNewBalanceAndMovement()
    {
        var store = new FakeInventoryStockStore();
        var (item, loc) = SetupItemAndLocation(store, _organizationId);
        var service = new InventoryStockService(store);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var request = new RecordStockReceiptRequest(loc.FarmId, loc.Id, item.Id, 250m, today, "PO-1001", "Received 250 units");
        var response = await service.RecordStockReceiptAsync(CreateActor(), request, "127.0.0.1");

        Assert.NotNull(response);
        Assert.Equal(StockMovementType.Receipt, response.MovementType);
        Assert.Equal(250m, response.Quantity);
        Assert.Equal("PO-1001", response.ReferenceNumber);

        var balance = await service.GetBalanceAsync(CreateActor(), loc.Id, item.Id);
        Assert.NotNull(balance);
        Assert.Equal(250m, balance.QuantityOnHand);
    }

    [Fact]
    public async Task RecordStockReceiptAsync_WhenBalanceExists_AddsToOnHandQuantity()
    {
        var store = new FakeInventoryStockStore();
        var (item, loc) = SetupItemAndLocation(store, _organizationId);
        var service = new InventoryStockService(store);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await service.RecordStockReceiptAsync(CreateActor(), new RecordStockReceiptRequest(loc.FarmId, loc.Id, item.Id, 100m, today), "127.0.0.1");
        await service.RecordStockReceiptAsync(CreateActor(), new RecordStockReceiptRequest(loc.FarmId, loc.Id, item.Id, 50m, today), "127.0.0.1");

        var balance = await service.GetBalanceAsync(CreateActor(), loc.Id, item.Id);
        Assert.NotNull(balance);
        Assert.Equal(150m, balance.QuantityOnHand);
    }

    [Fact]
    public async Task RecordStockIssueAsync_InsufficientStock_ThrowsValidationException()
    {
        var store = new FakeInventoryStockStore();
        var (item, loc) = SetupItemAndLocation(store, _organizationId);
        var service = new InventoryStockService(store);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await service.RecordStockReceiptAsync(CreateActor(), new RecordStockReceiptRequest(loc.FarmId, loc.Id, item.Id, 20m, today), "127.0.0.1");

        var request = new RecordStockIssueRequest(loc.FarmId, loc.Id, item.Id, 50m, today, "ISSUE-01", "Field application");
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.RecordStockIssueAsync(CreateActor(), request, "127.0.0.1"));

        Assert.Contains("Insufficient stock", ex.Message);
    }

    [Fact]
    public async Task RecordStockIssueAsync_SufficientStock_DeductsFromBalance()
    {
        var store = new FakeInventoryStockStore();
        var (item, loc) = SetupItemAndLocation(store, _organizationId);
        var service = new InventoryStockService(store);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await service.RecordStockReceiptAsync(CreateActor(), new RecordStockReceiptRequest(loc.FarmId, loc.Id, item.Id, 100m, today), "127.0.0.1");
        var response = await service.RecordStockIssueAsync(CreateActor(), new RecordStockIssueRequest(loc.FarmId, loc.Id, item.Id, 30m, today), "127.0.0.1");

        Assert.Equal(30m, response.Quantity);
        var balance = await service.GetBalanceAsync(CreateActor(), loc.Id, item.Id);
        Assert.NotNull(balance);
        Assert.Equal(70m, balance.QuantityOnHand);
    }

    [Fact]
    public async Task RecordStockAdjustmentAsync_AdjustmentInAndOut_UpdatesBalanceCorrectly()
    {
        var store = new FakeInventoryStockStore();
        var (item, loc) = SetupItemAndLocation(store, _organizationId);
        var service = new InventoryStockService(store);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Adjustment In
        await service.RecordStockAdjustmentAsync(CreateActor(), new RecordStockAdjustmentRequest(loc.FarmId, loc.Id, item.Id, StockMovementType.AdjustmentIn, 80m, today, "Stock count adjustment"), "127.0.0.1");
        var balance1 = await service.GetBalanceAsync(CreateActor(), loc.Id, item.Id);
        Assert.Equal(80m, balance1!.QuantityOnHand);

        // Adjustment Out
        await service.RecordStockAdjustmentAsync(CreateActor(), new RecordStockAdjustmentRequest(loc.FarmId, loc.Id, item.Id, StockMovementType.AdjustmentOut, 30m, today, "Damaged bags write-off"), "127.0.0.1");
        var balance2 = await service.GetBalanceAsync(CreateActor(), loc.Id, item.Id);
        Assert.Equal(50m, balance2!.QuantityOnHand);
    }

    [Fact]
    public async Task RecordStockTransferAsync_SameStorageLocation_ThrowsValidationException()
    {
        var store = new FakeInventoryStockStore();
        var (item, loc) = SetupItemAndLocation(store, _organizationId);
        var service = new InventoryStockService(store);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var request = new RecordStockTransferRequest(loc.FarmId, loc.Id, loc.FarmId, loc.Id, item.Id, 10m, today);
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.RecordStockTransferAsync(CreateActor(), request, "127.0.0.1"));

        Assert.Contains("must be different", ex.Message);
    }

    [Fact]
    public async Task RecordStockTransferAsync_ValidTransfer_LocksInDeterministicOrderAndCreatesPairedMovements()
    {
        var store = new FakeInventoryStockStore();
        var farm = new Farm(_organizationId, "Main Farm", Guid.NewGuid(), _userId);
        store.Farms.Add(farm);

        var unit = new Unit(null, "KG", "Kilogram", "kg", UnitCategory.Weight, null, 1.0m, true, 1);
        store.Units.Add(unit);

        var item = new InventoryItem(_organizationId, "N-P-K Fertilizer", unit.Id, _userId);
        store.Items.Add(item);

        var locA = new StorageLocation(_organizationId, farm.Id, "Shed Alpha", _userId);
        var locB = new StorageLocation(_organizationId, farm.Id, "Shed Beta", _userId);
        store.Locations.Add(locA);
        store.Locations.Add(locB);

        var service = new InventoryStockService(store);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Receive 200 kg into Shed Alpha
        await service.RecordStockReceiptAsync(CreateActor(), new RecordStockReceiptRequest(farm.Id, locA.Id, item.Id, 200m, today), "127.0.0.1");

        store.AdvisoryLockHistory.Clear();

        // Transfer 75 kg from Shed Alpha -> Shed Beta
        var transferRequest = new RecordStockTransferRequest(farm.Id, locA.Id, farm.Id, locB.Id, item.Id, 75m, today, "TR-500", "Inter-shed transfer");
        var movements = await service.RecordStockTransferAsync(CreateActor(), transferRequest, "127.0.0.1");

        Assert.Equal(2, movements.Count);
        Assert.Equal(StockMovementType.TransferOut, movements[0].MovementType);
        Assert.Equal(StockMovementType.TransferIn, movements[1].MovementType);
        Assert.Equal(movements[0].ParentTransactionId, movements[1].ParentTransactionId);

        // Check balances
        var balA = await service.GetBalanceAsync(CreateActor(), locA.Id, item.Id);
        var balB = await service.GetBalanceAsync(CreateActor(), locB.Id, item.Id);

        Assert.Equal(125m, balA!.QuantityOnHand);
        Assert.Equal(75m, balB!.QuantityOnHand);

        // Verify deterministic lock ordering call trace
        var expectedFirstLock = locA.Id.CompareTo(locB.Id) < 0 ? locA.Id : locB.Id;
        var expectedSecondLock = expectedFirstLock == locA.Id ? locB.Id : locA.Id;
        Assert.Equal(2, store.AdvisoryLockHistory.Count);
        Assert.Equal(expectedFirstLock, store.AdvisoryLockHistory[0]);
        Assert.Equal(expectedSecondLock, store.AdvisoryLockHistory[1]);
    }

    [Fact]
    public async Task RecordOpeningStockAsync_InactiveItem_ThrowsValidationException()
    {
        var store = new FakeInventoryStockStore();
        var (item, loc) = SetupItemAndLocation(store, _organizationId);
        item.Deactivate(DateTimeOffset.UtcNow, _userId);
        var service = new InventoryStockService(store);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var request = new RecordOpeningStockRequest(loc.FarmId, loc.Id, item.Id, 50m, today, "Inactive test");
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.RecordOpeningStockAsync(CreateActor(), request, "127.0.0.1"));

        Assert.Contains("inactive", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RecordStockReceiptAsync_InactiveLocation_ThrowsValidationException()
    {
        var store = new FakeInventoryStockStore();
        var (item, loc) = SetupItemAndLocation(store, _organizationId);
        loc.Deactivate(DateTimeOffset.UtcNow, _userId);
        var service = new InventoryStockService(store);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var request = new RecordStockReceiptRequest(loc.FarmId, loc.Id, item.Id, 100m, today);
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.RecordStockReceiptAsync(CreateActor(), request, "127.0.0.1"));

        Assert.Contains("inactive", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RecordStockAdjustmentAsync_AdjustmentOut_InsufficientStock_ThrowsValidationException()
    {
        var store = new FakeInventoryStockStore();
        var (item, loc) = SetupItemAndLocation(store, _organizationId);
        var service = new InventoryStockService(store);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Receive 20 units
        await service.RecordStockReceiptAsync(CreateActor(), new RecordStockReceiptRequest(loc.FarmId, loc.Id, item.Id, 20m, today), "127.0.0.1");

        // Attempting adjustment out of 50 units must fail
        var request = new RecordStockAdjustmentRequest(loc.FarmId, loc.Id, item.Id, StockMovementType.AdjustmentOut, 50m, today, "Over-deduction attempt");
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.RecordStockAdjustmentAsync(CreateActor(), request, "127.0.0.1"));

        Assert.Contains("Insufficient stock", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RecordStockTransferAsync_InsufficientSourceStock_ThrowsValidationException()
    {
        var store = new FakeInventoryStockStore();
        var farm = new Farm(_organizationId, "Main Farm", Guid.NewGuid(), _userId);
        store.Farms.Add(farm);

        var unit = new Unit(null, "KG", "Kilogram", "kg", UnitCategory.Weight, null, 1.0m, true, 1);
        store.Units.Add(unit);

        var item = new InventoryItem(_organizationId, "Fertilizer", unit.Id, _userId);
        store.Items.Add(item);

        var locA = new StorageLocation(_organizationId, farm.Id, "Shed Alpha", _userId);
        var locB = new StorageLocation(_organizationId, farm.Id, "Shed Beta", _userId);
        store.Locations.Add(locA);
        store.Locations.Add(locB);

        var service = new InventoryStockService(store);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Receive 30 units in Shed Alpha
        await service.RecordStockReceiptAsync(CreateActor(), new RecordStockReceiptRequest(farm.Id, locA.Id, item.Id, 30m, today), "127.0.0.1");

        // Attempting to transfer 100 units must fail
        var transferRequest = new RecordStockTransferRequest(farm.Id, locA.Id, farm.Id, locB.Id, item.Id, 100m, today);
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.RecordStockTransferAsync(CreateActor(), transferRequest, "127.0.0.1"));

        Assert.Contains("Insufficient stock", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ListBalancesAsync_ReturnsPagedStockBalances()
    {
        var store = new FakeInventoryStockStore();
        var (item, loc) = SetupItemAndLocation(store, _organizationId);
        var service = new InventoryStockService(store);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await service.RecordStockReceiptAsync(CreateActor(), new RecordStockReceiptRequest(loc.FarmId, loc.Id, item.Id, 150m, today), "127.0.0.1");

        var balances = await store.ListBalancesAsync(_organizationId, loc.FarmId, loc.Id, item.Id, 0, 10);
        var totalCount = await store.CountBalancesAsync(_organizationId, loc.FarmId, loc.Id, item.Id);

        Assert.Equal(1, totalCount);
        Assert.Single(balances);
        Assert.Equal(150m, balances[0].QuantityOnHand);
    }

    [Fact]
    public async Task ListMovementsAsync_ReturnsPagedStockMovements()
    {
        var store = new FakeInventoryStockStore();
        var (item, loc) = SetupItemAndLocation(store, _organizationId);
        var service = new InventoryStockService(store);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await service.RecordStockReceiptAsync(CreateActor(), new RecordStockReceiptRequest(loc.FarmId, loc.Id, item.Id, 100m, today), "127.0.0.1");
        await service.RecordStockIssueAsync(CreateActor(), new RecordStockIssueRequest(loc.FarmId, loc.Id, item.Id, 40m, today), "127.0.0.1");

        var movements = await store.ListMovementsAsync(_organizationId, loc.FarmId, loc.Id, item.Id, null, null, null, 0, 10);
        var totalCount = await store.CountMovementsAsync(_organizationId, loc.FarmId, loc.Id, item.Id, null, null, null);

        Assert.Equal(2, totalCount);
        Assert.Equal(2, movements.Count);
    }

    private static (InventoryItem Item, StorageLocation Location) SetupItemAndLocation(FakeInventoryStockStore store, Guid orgId)
    {
        var farm = new Farm(orgId, "Green Valley Farm", Guid.NewGuid(), Guid.NewGuid());
        store.Farms.Add(farm);

        var unit = new Unit(null, "KG", "Kilogram", "kg", UnitCategory.Weight, null, 1.0m, true, 1);
        store.Units.Add(unit);

        var item = new InventoryItem(orgId, "Urea Fertilizer", unit.Id, Guid.NewGuid());
        store.Items.Add(item);

        var loc = new StorageLocation(orgId, farm.Id, "Main Chemical Shed", Guid.NewGuid());
        store.Locations.Add(loc);

        return (item, loc);
    }
}

/// <summary>
/// In-memory fake implementation of IInventoryStockStore for fast, isolated unit testing.
/// Tracks transaction execution and advisory lock history to verify deterministic locking patterns.
/// </summary>
public sealed class FakeInventoryStockStore : IInventoryStockStore
{
    public List<Farm> Farms { get; } = [];
    public List<Unit> Units { get; } = [];
    public List<InventoryItem> Items { get; } = [];
    public List<StorageLocation> Locations { get; } = [];
    public List<StockBalance> Balances { get; } = [];
    public List<StockMovement> Movements { get; } = [];
    public List<AuditLog> AuditLogs { get; } = [];
    public List<Guid> AdvisoryLockHistory { get; } = [];

    public Task<InventoryItem?> FindItemAsync(Guid itemId, Guid organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(i => i.Id == itemId && i.OrganizationId == organizationId));

    public Task<StorageLocation?> FindLocationAsync(Guid locationId, Guid organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Locations.FirstOrDefault(l => l.Id == locationId && l.OrganizationId == organizationId));

    public Task<Farm?> FindFarmAsync(Guid farmId, Guid organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Farms.FirstOrDefault(f => f.Id == farmId && f.OrganizationId == organizationId));

    public Task<StockBalance?> FindBalanceAsync(Guid locationId, Guid itemId, Guid organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Balances.FirstOrDefault(b => b.StorageLocationId == locationId && b.InventoryItemId == itemId && b.OrganizationId == organizationId));

    public Task<StockBalance?> LockBalanceAsync(Guid locationId, Guid itemId, Guid organizationId, CancellationToken cancellationToken = default) =>
        FindBalanceAsync(locationId, itemId, organizationId, cancellationToken);

    public Task AcquireAdvisoryLockAsync(Guid locationId, Guid itemId, CancellationToken cancellationToken = default)
    {
        AdvisoryLockHistory.Add(locationId);
        return Task.CompletedTask;
    }

    public Task<bool> HasOpeningStockAsync(Guid locationId, Guid itemId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Movements.Any(m => m.StorageLocationId == locationId && m.InventoryItemId == itemId && m.MovementType == StockMovementType.OpeningStock));

    public Task<int> CountBalancesAsync(Guid organizationId, Guid? farmId, Guid? locationId, Guid? itemId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Balances.Count(b => b.OrganizationId == organizationId &&
            (!farmId.HasValue || b.FarmId == farmId.Value) &&
            (!locationId.HasValue || b.StorageLocationId == locationId.Value) &&
            (!itemId.HasValue || b.InventoryItemId == itemId.Value)));

    public Task<IReadOnlyList<StockBalance>> ListBalancesAsync(Guid organizationId, Guid? farmId, Guid? locationId, Guid? itemId, int skip, int take, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<StockBalance>>(Balances.Where(b => b.OrganizationId == organizationId &&
            (!farmId.HasValue || b.FarmId == farmId.Value) &&
            (!locationId.HasValue || b.StorageLocationId == locationId.Value) &&
            (!itemId.HasValue || b.InventoryItemId == itemId.Value))
            .Skip(skip).Take(take).ToList());

    public Task<int> CountMovementsAsync(Guid organizationId, Guid? farmId, Guid? locationId, Guid? itemId, StockMovementType? movementType, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken = default) =>
        Task.FromResult(Movements.Count(m => m.OrganizationId == organizationId &&
            (!farmId.HasValue || m.FarmId == farmId.Value) &&
            (!locationId.HasValue || m.StorageLocationId == locationId.Value) &&
            (!itemId.HasValue || m.InventoryItemId == itemId.Value) &&
            (!movementType.HasValue || m.MovementType == movementType.Value) &&
            (!fromDate.HasValue || m.MovementDate >= fromDate.Value) &&
            (!toDate.HasValue || m.MovementDate <= toDate.Value)));

    public Task<IReadOnlyList<StockMovement>> ListMovementsAsync(Guid organizationId, Guid? farmId, Guid? locationId, Guid? itemId, StockMovementType? movementType, DateOnly? fromDate, DateOnly? toDate, int skip, int take, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<StockMovement>>(Movements.Where(m => m.OrganizationId == organizationId &&
            (!farmId.HasValue || m.FarmId == farmId.Value) &&
            (!locationId.HasValue || m.StorageLocationId == locationId.Value) &&
            (!itemId.HasValue || m.InventoryItemId == itemId.Value) &&
            (!movementType.HasValue || m.MovementType == movementType.Value) &&
            (!fromDate.HasValue || m.MovementDate >= fromDate.Value) &&
            (!toDate.HasValue || m.MovementDate <= toDate.Value))
            .Skip(skip).Take(take).ToList());

    public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) =>
        await operation(cancellationToken);

    public void AddBalance(StockBalance balance) => Balances.Add(balance);
    public void AddMovement(StockMovement movement) => Movements.Add(movement);
    public void AddAuditLog(AuditLog auditLog) => AuditLogs.Add(auditLog);
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
