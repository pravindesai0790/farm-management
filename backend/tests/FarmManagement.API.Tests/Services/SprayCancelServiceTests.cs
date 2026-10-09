using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.Sprays;
using FarmManagement.Application.Interfaces.Sprays;
using FarmManagement.Application.Services;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Services;

public class SprayCancelServiceTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly FakeCancelSprayStore _store = new();
    private readonly SprayService _sut;

    public SprayCancelServiceTests()
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

    private Unit CreateUnit(string code = "L", string name = "Liter", UnitCategory category = UnitCategory.Volume)
    {
        var unit = new Unit(null, code, name, code, category, isSystem: true);
        _store.Units.Add(unit);
        return unit;
    }

    private InventoryItem CreateInventoryItem(string name = "Copper Oxychloride")
    {
        var unit = CreateUnit();
        var item = new InventoryItem(_orgId, name, unit.Id, _userId);
        _store.InventoryItems.Add(item);
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
        var messages = ex.Errors?.Values.SelectMany(v => v) ?? [];
        Assert.Contains(messages, m => m.Contains(text, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CancelAsync_WhenDraftSpray_TransitionsToCancelled_SetsReason_AndLogsAudit()
    {
        // Arrange
        var farm = CreateActiveFarm();
        var location = CreateStorageLocation(farm.Id);
        var item = CreateInventoryItem();
        var balance = CreateStockBalance(farm.Id, location.Id, item.Id, 100m);

        var spray = new Spray(
            _orgId,
            farm.Id,
            _userId,
            status: SprayStatus.Draft,
            plannedDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)));
        var product = new SprayProduct(spray.Id, item.Id, _userId, location.Id, plannedQuantity: 10m);
        spray.AddProduct(product);
        _store.Sprays.Add(spray);

        var request = new CancelSprayRequest("High wind speeds forecast for the entire week");

        // Act
        var result = await _sut.CancelAsync(CreateActor(), spray.Id, request, "127.0.0.1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(SprayStatus.Cancelled, result.Status);
        Assert.Equal("CANCELLED", result.StatusName);
        Assert.Equal("High wind speeds forecast for the entire week", result.CancellationReason);

        // Inventory is untouched
        Assert.Equal(100m, balance.QuantityOnHand);
        Assert.Empty(_store.StockMovements);

        // Audit log recorded
        var audit = Assert.Single(_store.AuditLogs);
        Assert.Equal("Spray.Cancelled", audit.Action);
        Assert.Equal(spray.Id, audit.EntityId);
    }

    [Fact]
    public async Task CancelAsync_WhenScheduledSpray_TransitionsToCancelled_SetsReason_AndLogsAudit()
    {
        // Arrange
        var farm = CreateActiveFarm();
        var spray = new Spray(
            _orgId,
            farm.Id,
            _userId,
            status: SprayStatus.Scheduled,
            plannedDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            scheduledDateTime: DateTimeOffset.UtcNow.AddDays(1).AddHours(8));
        _store.Sprays.Add(spray);

        var request = new CancelSprayRequest("Target pest already subsided due to natural biological control");

        // Act
        var result = await _sut.CancelAsync(CreateActor(), spray.Id, request, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(SprayStatus.Cancelled, result.Status);
        Assert.Equal("CANCELLED", result.StatusName);
        Assert.Equal("Target pest already subsided due to natural biological control", result.CancellationReason);

        var audit = Assert.Single(_store.AuditLogs);
        Assert.Equal("Spray.Cancelled", audit.Action);
    }

    [Fact]
    public async Task CancelAsync_WhenSprayNotFound_ThrowsResourceNotFoundException()
    {
        // Arrange
        var request = new CancelSprayRequest("Weather issues");

        // Act & Assert
        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _sut.CancelAsync(CreateActor(), Guid.NewGuid(), request, null));
    }

    [Fact]
    public async Task CancelAsync_WhenSprayBelongsToDifferentOrganization_ThrowsResourceNotFoundException()
    {
        // Arrange
        var farm = CreateActiveFarm();
        var otherOrgId = Guid.NewGuid();
        var spray = new Spray(otherOrgId, farm.Id, _userId, status: SprayStatus.Draft);
        _store.Sprays.Add(spray);

        var request = new CancelSprayRequest("Weather issues");

        // Act & Assert
        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _sut.CancelAsync(CreateActor(), spray.Id, request, null));
    }

    [Fact]
    public async Task CancelAsync_WhenSprayInProgress_ThrowsConflictException()
    {
        // Arrange
        var farm = CreateActiveFarm();
        var spray = new Spray(
            _orgId,
            farm.Id,
            _userId,
            status: SprayStatus.InProgress,
            actualApplicationDateTime: DateTimeOffset.UtcNow.AddMinutes(-10));
        _store.Sprays.Add(spray);

        var request = new CancelSprayRequest("Mechanical failure");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.CancelAsync(CreateActor(), spray.Id, request, null));
        Assert.Contains("Only draft or scheduled sprays can be cancelled", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CancelAsync_WhenSprayCompleted_ThrowsConflictException()
    {
        // Arrange
        var farm = CreateActiveFarm();
        var spray = new Spray(
            _orgId,
            farm.Id,
            _userId,
            status: SprayStatus.Completed,
            actualApplicationDateTime: DateTimeOffset.UtcNow.AddHours(-2));
        _store.Sprays.Add(spray);

        var request = new CancelSprayRequest("Application mistake");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.CancelAsync(CreateActor(), spray.Id, request, null));
        Assert.Contains("Only draft or scheduled sprays can be cancelled", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CancelAsync_WhenSprayAlreadyCancelled_ThrowsConflictException()
    {
        // Arrange
        var farm = CreateActiveFarm();
        var spray = new Spray(
            _orgId,
            farm.Id,
            _userId,
            status: SprayStatus.Cancelled,
            cancellationReason: "Original cancellation reason");
        _store.Sprays.Add(spray);

        var request = new CancelSprayRequest("Attempting second cancellation");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.CancelAsync(CreateActor(), spray.Id, request, null));
        Assert.Contains("Only draft or scheduled sprays can be cancelled", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CancelAsync_WhenReasonNullOrWhitespace_ThrowsValidationException(string? invalidReason)
    {
        // Arrange
        var farm = CreateActiveFarm();
        var spray = new Spray(_orgId, farm.Id, _userId, status: SprayStatus.Draft);
        _store.Sprays.Add(spray);

        var request = new CancelSprayRequest(invalidReason!);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CancelAsync(CreateActor(), spray.Id, request, null));
        AssertValidationError(ex, "cancellation reason is required");
    }

    private sealed class FakeCancelSprayStore : ISprayStore
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
        public List<StockMovement> StockMovements { get; } = [];
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
            Task.FromResult(true);

        public Task<StorageLocation?> FindStorageLocationAsync(Guid storageLocationId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(StorageLocations.FirstOrDefault(l => l.Id == storageLocationId && l.OrganizationId == organizationId));

        public Task<StockBalance?> FindStockBalanceAsync(Guid storageLocationId, Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(StockBalances.FirstOrDefault(sb => sb.StorageLocationId == storageLocationId && sb.InventoryItemId == inventoryItemId && sb.OrganizationId == organizationId));

        public void RemoveSprayProduct(SprayProduct product) { }

        public void AddMovement(StockMovement movement) => StockMovements.Add(movement);

        public Task<StockBalance?> LockBalanceAsync(Guid storageLocationId, Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default) =>
            FindStockBalanceAsync(storageLocationId, inventoryItemId, organizationId, cancellationToken);

        public Task AcquireAdvisoryLockAsync(Guid storageLocationId, Guid inventoryItemId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) =>
            operation(cancellationToken);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
