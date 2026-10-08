using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.Sprays;
using FarmManagement.Application.Interfaces.Sprays;
using FarmManagement.Application.Services;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Services;

public class SprayCompleteServiceTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly FakeCompleteSprayStore _store = new();
    private readonly SprayService _sut;

    public SprayCompleteServiceTests()
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
    public async Task CompleteAsync_WhenValidInProgressSpray_TransitionsToCompleted_DeductsStock_CreatesStockMovements_AndLogsAudit()
    {
        // Arrange
        var farm = CreateActiveFarm();
        var storageLocation = CreateStorageLocation(farm.Id);
        var item1 = CreateInventoryItem("Fungicide A");
        var item2 = CreateInventoryItem("Insecticide B");
        var balance1 = CreateStockBalance(farm.Id, storageLocation.Id, item1.Id, 50m);
        var balance2 = CreateStockBalance(farm.Id, storageLocation.Id, item2.Id, 30m);

        var areaUnit = CreateUnit("HA", "Hectare", UnitCategory.Area);
        var waterUnit = CreateUnit("L", "Liter", UnitCategory.Volume);

        var spray = new Spray(
            _orgId,
            farm.Id,
            _userId,
            status: SprayStatus.InProgress,
            actualApplicationDateTime: DateTimeOffset.UtcNow.AddHours(-1),
            actualTreatedArea: 2.5m,
            actualTreatedAreaUnitId: areaUnit.Id,
            waterQuantity: 200m,
            waterUnitId: waterUnit.Id);

        var product1 = new SprayProduct(spray.Id, item1.Id, _userId, storageLocation.Id, plannedQuantity: 10m, actualQuantity: 8m, dosage: "1.5 kg/ha");
        var product2 = new SprayProduct(spray.Id, item2.Id, _userId, storageLocation.Id, plannedQuantity: 5m, actualQuantity: 4m, dosage: "1 L/ha");
        spray.AddProduct(product1);
        spray.AddProduct(product2);
        _store.Sprays.Add(spray);

        // Act
        var result = await _sut.CompleteAsync(CreateActor(), spray.Id, null, "127.0.0.1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(SprayStatus.Completed, result.Status);
        Assert.Equal("COMPLETED", result.StatusName);

        // Verify stock balances were deducted
        Assert.Equal(42m, balance1.QuantityOnHand); // 50 - 8
        Assert.Equal(26m, balance2.QuantityOnHand); // 30 - 4

        // Verify stock movements created
        Assert.Equal(2, _store.StockMovements.Count);

        var mov1 = _store.StockMovements.Single(m => m.InventoryItemId == item1.Id);
        Assert.Equal(StockMovementType.Issue, mov1.MovementType);
        Assert.Equal(spray.Id, mov1.SprayId);
        Assert.Equal(farm.Id, mov1.FarmId);
        Assert.Equal(storageLocation.Id, mov1.StorageLocationId);
        Assert.Equal(8m, mov1.Quantity);
        Assert.Equal(item1.StockUnitId, mov1.StockUnitId);

        var mov2 = _store.StockMovements.Single(m => m.InventoryItemId == item2.Id);
        Assert.Equal(StockMovementType.Issue, mov2.MovementType);
        Assert.Equal(spray.Id, mov2.SprayId);
        Assert.Equal(4m, mov2.Quantity);

        // Verify audit log
        var audit = Assert.Single(_store.AuditLogs);
        Assert.Equal("Spray.Completed", audit.Action);
        Assert.Equal(spray.Id, audit.EntityId);
    }

    [Fact]
    public async Task CompleteAsync_WhenSprayNotFound_ThrowsResourceNotFoundException()
    {
        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _sut.CompleteAsync(CreateActor(), Guid.NewGuid(), null, null));
    }

    [Theory]
    [InlineData(SprayStatus.Draft)]
    [InlineData(SprayStatus.Scheduled)]
    [InlineData(SprayStatus.Completed)]
    [InlineData(SprayStatus.Cancelled)]
    public async Task CompleteAsync_WhenSprayNotInProgress_ThrowsConflictException(SprayStatus status)
    {
        // Arrange
        var farm = CreateActiveFarm();
        var spray = new Spray(_orgId, farm.Id, _userId, status: status);
        _store.Sprays.Add(spray);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.CompleteAsync(CreateActor(), spray.Id, null, null));
    }

    [Fact]
    public async Task CompleteAsync_WhenInsufficientStock_ThrowsValidationException_KeepsSprayInProgress_AndRollsBack()
    {
        // Arrange
        var farm = CreateActiveFarm();
        var storageLocation = CreateStorageLocation(farm.Id);
        var item = CreateInventoryItem();
        var balance = CreateStockBalance(farm.Id, storageLocation.Id, item.Id, 5.0m); // only 5 available

        var spray = new Spray(
            _orgId,
            farm.Id,
            _userId,
            status: SprayStatus.InProgress,
            actualApplicationDateTime: DateTimeOffset.UtcNow.AddMinutes(-30));
        var product = new SprayProduct(spray.Id, item.Id, _userId, storageLocation.Id, actualQuantity: 10.0m); // requiring 10
        spray.AddProduct(product);
        _store.Sprays.Add(spray);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CompleteAsync(CreateActor(), spray.Id, null, null));
        AssertValidationError(ex, "Insufficient stock");

        // Invariant checks:
        Assert.Equal(SprayStatus.InProgress, spray.Status);
        Assert.Equal(5.0m, balance.QuantityOnHand); // balance unchanged
        Assert.Empty(_store.StockMovements); // no movements created
        Assert.Empty(_store.AuditLogs);
    }

    [Fact]
    public async Task CompleteAsync_WhenNoProducts_ThrowsValidationException()
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

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CompleteAsync(CreateActor(), spray.Id, null, null));
        AssertValidationError(ex, "At least one product is required");
    }

    [Fact]
    public async Task CompleteAsync_WhenProductStorageLocationNull_ThrowsValidationException()
    {
        // Arrange
        var farm = CreateActiveFarm();
        var item = CreateInventoryItem();

        var spray = new Spray(
            _orgId,
            farm.Id,
            _userId,
            status: SprayStatus.InProgress,
            actualApplicationDateTime: DateTimeOffset.UtcNow.AddMinutes(-10));
        var product = new SprayProduct(spray.Id, item.Id, _userId, storageLocationId: null, actualQuantity: 5m);
        spray.AddProduct(product);
        _store.Sprays.Add(spray);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CompleteAsync(CreateActor(), spray.Id, null, null));
        AssertValidationError(ex, "storage location is required");
    }

    [Fact]
    public async Task CompleteAsync_WhenProductStorageLocationOnDifferentFarm_ThrowsValidationException()
    {
        // Arrange
        var farm1 = CreateActiveFarm();
        var farm2 = CreateActiveFarm();
        var locationOtherFarm = CreateStorageLocation(farm2.Id);
        var item = CreateInventoryItem();
        CreateStockBalance(farm2.Id, locationOtherFarm.Id, item.Id, 50m);

        var spray = new Spray(
            _orgId,
            farm1.Id,
            _userId,
            status: SprayStatus.InProgress,
            actualApplicationDateTime: DateTimeOffset.UtcNow.AddMinutes(-10));
        var product = new SprayProduct(spray.Id, item.Id, _userId, locationOtherFarm.Id, actualQuantity: 5m);
        spray.AddProduct(product);
        _store.Sprays.Add(spray);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CompleteAsync(CreateActor(), spray.Id, null, null));
        AssertValidationError(ex, "does not belong to the spray farm");
    }

    [Fact]
    public async Task CompleteAsync_WhenInventoryItemMissingPlantProtectionProfile_ThrowsValidationException()
    {
        // Arrange
        var farm = CreateActiveFarm();
        var storageLocation = CreateStorageLocation(farm.Id);
        var item = CreateInventoryItem();
        _store.ActiveProtectionProductItemIds.Remove(item.Id);
        CreateStockBalance(farm.Id, storageLocation.Id, item.Id, 50m);

        var spray = new Spray(
            _orgId,
            farm.Id,
            _userId,
            status: SprayStatus.InProgress,
            actualApplicationDateTime: DateTimeOffset.UtcNow.AddMinutes(-10));
        var product = new SprayProduct(spray.Id, item.Id, _userId, storageLocation.Id, actualQuantity: 5m);
        spray.AddProduct(product);
        _store.Sprays.Add(spray);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CompleteAsync(CreateActor(), spray.Id, null, null));
        AssertValidationError(ex, "active plant protection product");
    }

    [Fact]
    public async Task CompleteAsync_WithExecutionOverrides_AppliesOverridesBeforeCompleting()
    {
        // Arrange
        var farm = CreateActiveFarm();
        var storageLocation = CreateStorageLocation(farm.Id);
        var item = CreateInventoryItem();
        var balance = CreateStockBalance(farm.Id, storageLocation.Id, item.Id, 50m);
        var areaUnit = CreateUnit("HA", "Hectare", UnitCategory.Area);
        var waterUnit = CreateUnit("L", "Liter", UnitCategory.Volume);

        var spray = new Spray(
            _orgId,
            farm.Id,
            _userId,
            status: SprayStatus.InProgress,
            actualApplicationDateTime: DateTimeOffset.UtcNow.AddHours(-2));
        var product = new SprayProduct(spray.Id, item.Id, _userId, storageLocation.Id, actualQuantity: 5m, dosage: "1 L/ha");
        spray.AddProduct(product);
        _store.Sprays.Add(spray);

        var request = new CompleteSprayRequest(
            ActualApplicationDateTime: DateTimeOffset.UtcNow.AddMinutes(-15),
            ActualTreatedArea: 3.0m,
            ActualTreatedAreaUnitId: areaUnit.Id,
            WaterQuantity: 150m,
            WaterUnitId: waterUnit.Id,
            Products: [
                new CompleteSprayProductItemRequest(item.Id, ActualQuantity: 12.0m, Dosage: "2.4 L/ha")
            ]);

        // Act
        var result = await _sut.CompleteAsync(CreateActor(), spray.Id, request, null);

        // Assert
        Assert.Equal(SprayStatus.Completed, result.Status);
        Assert.Equal(3.0m, result.ActualTreatedArea);
        Assert.Equal(150m, result.WaterQuantity);

        var completedProduct = Assert.Single(result.Products);
        Assert.Equal(12.0m, completedProduct.ActualQuantity);
        Assert.Equal("2.4 L/ha", completedProduct.Dosage);

        // Stock deduction was based on the overridden 12.0m quantity: 50 - 12 = 38
        Assert.Equal(38.0m, balance.QuantityOnHand);

        var movement = Assert.Single(_store.StockMovements);
        Assert.Equal(12.0m, movement.Quantity);
    }

    private sealed class FakeCompleteSprayStore : ISprayStore
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

        public void AddMovement(StockMovement movement) => StockMovements.Add(movement);

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

        public Task<StockBalance?> LockBalanceAsync(Guid storageLocationId, Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default) =>
            FindStockBalanceAsync(storageLocationId, inventoryItemId, organizationId, cancellationToken);

        public Task AcquireAdvisoryLockAsync(Guid storageLocationId, Guid inventoryItemId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) =>
            operation(cancellationToken);

        public void RemoveSprayProduct(SprayProduct product) { }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
