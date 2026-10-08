using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.Sprays;
using FarmManagement.Application.Interfaces.Sprays;
using FarmManagement.Application.Services;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Services;

public class SprayStartServiceTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly FakeStartSprayStore _store = new();
    private readonly SprayService _sut;

    public SprayStartServiceTests()
    {
        _sut = new SprayService(_store);
    }

    private SprayActor CreateActor() => new(_userId, _orgId);

    private Farm CreateActiveFarm()
    {
        var farm = new Farm(_orgId, "Highland Orchards", Guid.NewGuid(), Guid.NewGuid(), 100m, Guid.NewGuid());
        _store.Farms.Add(farm);
        return farm;
    }

    private StorageLocation CreateStorageLocation(Guid farmId, string name = "Main Barn")
    {
        var location = new StorageLocation(_orgId, farmId, name, _userId);
        _store.StorageLocations.Add(location);
        return location;
    }

    private Unit CreateUnit(string code = "L", string name = "Liter")
    {
        var unit = new Unit(null, code, name, code, UnitCategory.Volume, isSystem: true);
        _store.Units.Add(unit);
        return unit;
    }

    private InventoryItem CreateInventoryItem(string name = "Copper Oxychloride")
    {
        var unit = CreateUnit();
        var item = new InventoryItem(_orgId, name, unit.Id, _userId);
        _store.InventoryItems.Add(item);
        _store.ActiveProtectionProductItemIds.Add(item.Id);
        return item;
    }

    private StockBalance CreateStockBalance(Guid farmId, Guid storageLocationId, Guid inventoryItemId, decimal quantity)
    {
        var balance = new StockBalance(_orgId, farmId, storageLocationId, inventoryItemId, quantity);
        _store.StockBalances.Add(balance);
        return balance;
    }

    private static void AssertValidationError(ValidationException ex, string text)
    {
        var allErrors = (ex.Errors?.SelectMany(kv => kv.Value ?? []) ?? []).ToList();
        Assert.Contains(allErrors, err => err.Contains(text, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task StartAsync_WhenValidRequest_TransitionsToInProgress_UpdatesExecution_AndLogsAudit()
    {
        // Arrange
        var farm = CreateActiveFarm();
        var storageLocation = CreateStorageLocation(farm.Id);
        var item = CreateInventoryItem();
        CreateStockBalance(farm.Id, storageLocation.Id, item.Id, 50m);

        var spray = new Spray(
            _orgId,
            farm.Id,
            _userId,
            status: SprayStatus.Scheduled,
            scheduledDateTime: DateTimeOffset.UtcNow.AddHours(-1));
        var product = new SprayProduct(spray.Id, item.Id, _userId, plannedQuantity: 10m, dosage: "1.5 kg/ha");
        spray.AddProduct(product);
        _store.Sprays.Add(spray);

        var actualTime = DateTimeOffset.UtcNow.AddMinutes(-30);

        var request = new StartSprayRequest(
            ActualApplicationDateTime: actualTime,
            Products: [
                new StartSprayProductItemRequest(
                    InventoryItemId: item.Id,
                    StorageLocationId: storageLocation.Id,
                    ActualQuantity: 8.5m,
                    Dosage: "1.5 kg/ha")
            ]);

        // Act
        var result = await _sut.StartAsync(CreateActor(), spray.Id, request, "192.168.1.1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(SprayStatus.InProgress, result.Status);
        Assert.Equal("INPROGRESS", result.StatusName);
        Assert.Equal(actualTime, result.ActualApplicationDateTime);

        var resultProduct = Assert.Single(result.Products);
        Assert.Equal(item.Id, resultProduct.InventoryItemId);
        Assert.Equal(storageLocation.Id, resultProduct.StorageLocationId);
        Assert.Equal(8.5m, resultProduct.ActualQuantity);
        Assert.Equal("1.5 kg/ha", resultProduct.Dosage);

        // Verify audit log
        var audit = Assert.Single(_store.AuditLogs);
        Assert.Equal("Spray.Started", audit.Action);
        Assert.Equal(spray.Id, audit.EntityId);

        // Invariant: Verify stock balance was NOT decremented and stock was NOT reserved
        var balance = _store.StockBalances.Single(b => b.StorageLocationId == storageLocation.Id && b.InventoryItemId == item.Id);
        Assert.Equal(50m, balance.QuantityOnHand);
    }

    [Fact]
    public async Task StartAsync_WhenProductNotExistingInDraft_IsAddedSuccessfully()
    {
        // Arrange
        var farm = CreateActiveFarm();
        var storageLocation = CreateStorageLocation(farm.Id);
        var item = CreateInventoryItem();
        CreateStockBalance(farm.Id, storageLocation.Id, item.Id, 25m);

        // Spray originally had NO products
        var spray = new Spray(_orgId, farm.Id, _userId, status: SprayStatus.Scheduled);
        _store.Sprays.Add(spray);

        var request = new StartSprayRequest(
            ActualApplicationDateTime: DateTimeOffset.UtcNow.AddMinutes(-5),
            Products: [
                new StartSprayProductItemRequest(item.Id, storageLocation.Id, 12m, "2 L/ha")
            ]);

        // Act
        var result = await _sut.StartAsync(CreateActor(), spray.Id, request, null);

        // Assert
        Assert.Equal(SprayStatus.InProgress, result.Status);
        var addedProduct = Assert.Single(result.Products);
        Assert.Equal(item.Id, addedProduct.InventoryItemId);
        Assert.Equal(12m, addedProduct.ActualQuantity);
    }

    [Fact]
    public async Task StartAsync_WhenDuplicateProductsInRequest_ThrowsValidationException()
    {
        // Arrange
        var farm = CreateActiveFarm();
        var storageLocation = CreateStorageLocation(farm.Id);
        var item = CreateInventoryItem();
        CreateStockBalance(farm.Id, storageLocation.Id, item.Id, 50m);

        var spray = new Spray(_orgId, farm.Id, _userId, status: SprayStatus.Scheduled);
        _store.Sprays.Add(spray);

        var request = new StartSprayRequest(
            ActualApplicationDateTime: DateTimeOffset.UtcNow.AddMinutes(-5),
            Products: [
                new StartSprayProductItemRequest(item.Id, storageLocation.Id, 5m, null),
                new StartSprayProductItemRequest(item.Id, storageLocation.Id, 3m, null)
            ]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.StartAsync(CreateActor(), spray.Id, request, null));
        AssertValidationError(ex, "Duplicate inventory items are not allowed");
    }

    [Fact]
    public async Task StartAsync_WhenSprayNotFound_ThrowsResourceNotFoundException()
    {
        var request = new StartSprayRequest(
            ActualApplicationDateTime: DateTimeOffset.UtcNow,
            Products: [
                new StartSprayProductItemRequest(Guid.NewGuid(), Guid.NewGuid(), 10m, null)
            ]);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _sut.StartAsync(CreateActor(), Guid.NewGuid(), request, null));
    }

    [Theory]
    [InlineData(SprayStatus.Draft)]
    [InlineData(SprayStatus.InProgress)]
    [InlineData(SprayStatus.Completed)]
    [InlineData(SprayStatus.Cancelled)]
    public async Task StartAsync_WhenSprayNotScheduled_ThrowsConflictException(SprayStatus status)
    {
        // Arrange
        var farm = CreateActiveFarm();
        var spray = new Spray(_orgId, farm.Id, _userId, status: status);
        _store.Sprays.Add(spray);

        var request = new StartSprayRequest(
            ActualApplicationDateTime: DateTimeOffset.UtcNow,
            Products: [
                new StartSprayProductItemRequest(Guid.NewGuid(), Guid.NewGuid(), 5m, null)
            ]);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.StartAsync(CreateActor(), spray.Id, request, null));
    }

    [Fact]
    public async Task StartAsync_WhenActualApplicationDateTimeInFuture_ThrowsValidationException()
    {
        // Arrange
        var farm = CreateActiveFarm();
        var storageLocation = CreateStorageLocation(farm.Id);
        var item = CreateInventoryItem();
        CreateStockBalance(farm.Id, storageLocation.Id, item.Id, 50m);

        var spray = new Spray(_orgId, farm.Id, _userId, status: SprayStatus.Scheduled);
        spray.AddProduct(new SprayProduct(spray.Id, item.Id, _userId));
        _store.Sprays.Add(spray);

        var futureTime = DateTimeOffset.UtcNow.AddMinutes(15);
        var request = new StartSprayRequest(
            ActualApplicationDateTime: futureTime,
            Products: [
                new StartSprayProductItemRequest(item.Id, storageLocation.Id, 5m, null)
            ]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.StartAsync(CreateActor(), spray.Id, request, null));
        AssertValidationError(ex, "future");
    }

    [Fact]
    public async Task StartAsync_WhenNoProductsSupplied_ThrowsValidationException()
    {
        // Arrange
        var farm = CreateActiveFarm();
        var spray = new Spray(_orgId, farm.Id, _userId, status: SprayStatus.Scheduled);
        _store.Sprays.Add(spray);

        var request = new StartSprayRequest(
            ActualApplicationDateTime: DateTimeOffset.UtcNow,
            Products: []);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.StartAsync(CreateActor(), spray.Id, request, null));
        AssertValidationError(ex, "product");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task StartAsync_WhenActualQuantityZeroOrNegative_ThrowsValidationException(decimal quantity)
    {
        // Arrange
        var farm = CreateActiveFarm();
        var storageLocation = CreateStorageLocation(farm.Id);
        var item = CreateInventoryItem();
        CreateStockBalance(farm.Id, storageLocation.Id, item.Id, 50m);

        var spray = new Spray(_orgId, farm.Id, _userId, status: SprayStatus.Scheduled);
        spray.AddProduct(new SprayProduct(spray.Id, item.Id, _userId));
        _store.Sprays.Add(spray);

        var request = new StartSprayRequest(
            ActualApplicationDateTime: DateTimeOffset.UtcNow,
            Products: [
                new StartSprayProductItemRequest(item.Id, storageLocation.Id, quantity, null)
            ]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.StartAsync(CreateActor(), spray.Id, request, null));
        AssertValidationError(ex, "greater than zero");
    }

    [Fact]
    public async Task StartAsync_WhenStorageLocationBelongsToDifferentFarm_ThrowsValidationException()
    {
        // Arrange
        var farm1 = CreateActiveFarm();
        var farm2 = CreateActiveFarm();
        var storageLocationOtherFarm = CreateStorageLocation(farm2.Id);
        var item = CreateInventoryItem();
        CreateStockBalance(farm2.Id, storageLocationOtherFarm.Id, item.Id, 50m);

        var spray = new Spray(_orgId, farm1.Id, _userId, status: SprayStatus.Scheduled);
        spray.AddProduct(new SprayProduct(spray.Id, item.Id, _userId));
        _store.Sprays.Add(spray);

        var request = new StartSprayRequest(
            ActualApplicationDateTime: DateTimeOffset.UtcNow,
            Products: [
                new StartSprayProductItemRequest(item.Id, storageLocationOtherFarm.Id, 5m, null)
            ]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.StartAsync(CreateActor(), spray.Id, request, null));
        AssertValidationError(ex, "does not belong to the spray farm");
    }

    [Fact]
    public async Task StartAsync_WhenInventoryItemHasNoActivePlantProtectionProfile_ThrowsValidationException()
    {
        // Arrange
        var farm = CreateActiveFarm();
        var storageLocation = CreateStorageLocation(farm.Id);
        var item = CreateInventoryItem();
        _store.ActiveProtectionProductItemIds.Remove(item.Id); // remove active profile
        CreateStockBalance(farm.Id, storageLocation.Id, item.Id, 50m);

        var spray = new Spray(_orgId, farm.Id, _userId, status: SprayStatus.Scheduled);
        spray.AddProduct(new SprayProduct(spray.Id, item.Id, _userId));
        _store.Sprays.Add(spray);

        var request = new StartSprayRequest(
            ActualApplicationDateTime: DateTimeOffset.UtcNow,
            Products: [
                new StartSprayProductItemRequest(item.Id, storageLocation.Id, 5m, null)
            ]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.StartAsync(CreateActor(), spray.Id, request, null));
        AssertValidationError(ex, "active plant protection product");
    }

    [Fact]
    public async Task StartAsync_WhenInsufficientStock_ThrowsValidationException_KeepsSprayScheduled()
    {
        // Arrange
        var farm = CreateActiveFarm();
        var storageLocation = CreateStorageLocation(farm.Id);
        var item = CreateInventoryItem();
        CreateStockBalance(farm.Id, storageLocation.Id, item.Id, 4.0m); // only 4 available

        var spray = new Spray(_orgId, farm.Id, _userId, status: SprayStatus.Scheduled);
        spray.AddProduct(new SprayProduct(spray.Id, item.Id, _userId));
        _store.Sprays.Add(spray);

        var request = new StartSprayRequest(
            ActualApplicationDateTime: DateTimeOffset.UtcNow,
            Products: [
                new StartSprayProductItemRequest(item.Id, storageLocation.Id, 10.0m, null) // requesting 10
            ]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.StartAsync(CreateActor(), spray.Id, request, null));
        AssertValidationError(ex, "Insufficient stock");

        // Invariant: Status remains Scheduled, no audit log added, no stock changed
        Assert.Equal(SprayStatus.Scheduled, spray.Status);
        Assert.Empty(_store.AuditLogs);
        var balance = _store.StockBalances.Single(b => b.StorageLocationId == storageLocation.Id && b.InventoryItemId == item.Id);
        Assert.Equal(4.0m, balance.QuantityOnHand);
    }

    [Fact]
    public async Task StartAsync_WhenStockBalanceNotFound_ThrowsValidationException()
    {
        // Arrange
        var farm = CreateActiveFarm();
        var storageLocation = CreateStorageLocation(farm.Id);
        var item = CreateInventoryItem();
        // Do not add any stock balance record

        var spray = new Spray(_orgId, farm.Id, _userId, status: SprayStatus.Scheduled);
        spray.AddProduct(new SprayProduct(spray.Id, item.Id, _userId));
        _store.Sprays.Add(spray);

        var request = new StartSprayRequest(
            ActualApplicationDateTime: DateTimeOffset.UtcNow,
            Products: [
                new StartSprayProductItemRequest(item.Id, storageLocation.Id, 5.0m, null)
            ]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.StartAsync(CreateActor(), spray.Id, request, null));
        AssertValidationError(ex, "Insufficient stock");
        Assert.Equal(SprayStatus.Scheduled, spray.Status);
    }

    private sealed class FakeStartSprayStore : ISprayStore
    {
        public List<Spray> Sprays { get; } = [];
        public List<Farm> Farms { get; } = [];
        public List<FarmArea> FarmAreas { get; } = [];
        public List<CropPlantation> Plantations { get; } = [];
        public List<CropCycle> CropCycles { get; } = [];
        public List<CropCycleStage> CropCycleStages { get; } = [];
        public List<Target> Targets { get; } = [];
        public List<ApplicationMethod> ApplicationMethods { get; } = [];
        public List<Unit> Units { get; } = [];
        public List<InventoryItem> InventoryItems { get; } = [];
        public List<StorageLocation> StorageLocations { get; } = [];
        public List<StockBalance> StockBalances { get; } = [];
        public HashSet<Guid> ActiveProtectionProductItemIds { get; } = [];
        public List<AuditLog> AuditLogs { get; } = [];

        public Task<Spray?> FindAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default)
        {
            var spray = Sprays.FirstOrDefault(s => s.Id == id && s.OrganizationId == organizationId);
            return Task.FromResult(spray);
        }

        public Task<int> CountAsync(Guid organizationId, SprayListQuery query, DateTimeOffset now, CancellationToken cancellationToken = default) =>
            Task.FromResult(Sprays.Count(s => s.OrganizationId == organizationId));

        public Task<IReadOnlyList<Spray>> ListAsync(Guid organizationId, SprayListQuery query, int skip, int take, DateTimeOffset now, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Spray>>(Sprays.Where(s => s.OrganizationId == organizationId).Skip(skip).Take(take).ToList());

        public void Add(Spray spray) => Sprays.Add(spray);

        public void AddAuditLog(AuditLog auditLog) => AuditLogs.Add(auditLog);

        public Task<Farm?> FindFarmAsync(Guid farmId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Farms.FirstOrDefault(f => f.Id == farmId && f.OrganizationId == organizationId));

        public Task<FarmArea?> FindFarmAreaAsync(Guid farmAreaId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(FarmAreas.FirstOrDefault(fa => fa.Id == farmAreaId && fa.OrganizationId == organizationId));

        public Task<CropPlantation?> FindPlantationAsync(Guid plantationId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Plantations.FirstOrDefault(p => p.Id == plantationId && p.OrganizationId == organizationId));

        public Task<CropCycle?> FindCropCycleAsync(Guid cropCycleId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(CropCycles.FirstOrDefault(c => c.Id == cropCycleId && c.OrganizationId == organizationId));

        public Task<CropCycleStage?> FindCropCycleStageAsync(Guid cropCycleStageId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(CropCycleStages.FirstOrDefault(s => s.Id == cropCycleStageId));

        public Task<Target?> FindTargetAsync(Guid targetId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Targets.FirstOrDefault(t => t.Id == targetId && ((t.IsSystem && t.OrganizationId == null) || t.OrganizationId == organizationId)));

        public Task<ApplicationMethod?> FindApplicationMethodAsync(Guid applicationMethodId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ApplicationMethods.FirstOrDefault(am => am.Id == applicationMethodId && ((am.IsSystem && am.OrganizationId == null) || am.OrganizationId == organizationId)));

        public Task<Unit?> FindUnitAsync(Guid unitId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Units.FirstOrDefault(u => u.Id == unitId && ((u.IsSystem && u.OrganizationId == null) || u.OrganizationId == organizationId)));

        public Task<InventoryItem?> FindInventoryItemAsync(Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(InventoryItems.FirstOrDefault(i => i.Id == inventoryItemId && i.OrganizationId == organizationId));

        public Task<bool> HasActivePlantProtectionProfileAsync(Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ActiveProtectionProductItemIds.Contains(inventoryItemId));

        public Task<StorageLocation?> FindStorageLocationAsync(Guid storageLocationId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(StorageLocations.FirstOrDefault(l => l.Id == storageLocationId && l.OrganizationId == organizationId));

        public Task<StockBalance?> FindStockBalanceAsync(Guid storageLocationId, Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(StockBalances.FirstOrDefault(sb => sb.StorageLocationId == storageLocationId && sb.InventoryItemId == inventoryItemId && sb.OrganizationId == organizationId));

        public void RemoveSprayProduct(SprayProduct product) { }

        public void AddMovement(StockMovement movement) { }

        public Task<StockBalance?> LockBalanceAsync(Guid storageLocationId, Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default) =>
            FindStockBalanceAsync(storageLocationId, inventoryItemId, organizationId, cancellationToken);

        public Task AcquireAdvisoryLockAsync(Guid storageLocationId, Guid inventoryItemId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) =>
            operation(cancellationToken);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
