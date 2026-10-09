using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.Sprays;
using FarmManagement.Application.Interfaces.Sprays;
using FarmManagement.Application.Services;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Services;

public class SprayRecordCompletedServiceTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly FakeRecordCompletedSprayStore _store = new();
    private readonly SprayService _sut;

    public SprayRecordCompletedServiceTests()
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

    private FarmArea CreateActiveArea(Guid farmId)
    {
        var area = new FarmArea(_orgId, farmId, null, "North Plot", 10m, Guid.NewGuid(), _userId);
        _store.FarmAreas.Add(area);
        return area;
    }

    private CropPlantation CreateActivePlantation(Guid farmId, Guid farmAreaId)
    {
        var plantation = new CropPlantation(_orgId, farmId, farmAreaId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Block 1", 5m, Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), null, _userId);
        _store.Plantations.Add(plantation);
        return plantation;
    }

    private CropCycle CreateActiveCropCycle(Guid plantationId)
    {
        var cycle = new CropCycle(_orgId, plantationId, "Season 2026", 2026, "Spring", DateOnly.FromDateTime(DateTime.UtcNow), null, _userId);
        _store.CropCycles.Add(cycle);
        return cycle;
    }

    private CropCycleStage CreateCropCycleStage(Guid cropCycleId)
    {
        var stage = new CropCycleStage(cropCycleId, Guid.NewGuid(), "Flowering", 1, 14, DateOnly.FromDateTime(DateTime.UtcNow), null, _userId);
        _store.CropCycleStages.Add(stage);
        return stage;
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

    private Target CreateTarget(string code = "POWDERY_MILDEW", string name = "Powdery Mildew")
    {
        var target = new Target(null, code, name, TargetType.Disease, isSystem: true);
        _store.Targets.Add(target);
        return target;
    }

    private ApplicationMethod CreateApplicationMethod(string code = "TRACTOR_SPRAYER", string name = "Tractor Sprayer")
    {
        var method = new ApplicationMethod(null, code, name, isSystem: true);
        _store.ApplicationMethods.Add(method);
        return method;
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
        var messages = ex.Errors?.Values.SelectMany(v => v) ?? [];
        Assert.Contains(messages, m => m.Contains(text, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RecordCompletedAsync_ValidFullHierarchy_CreatesCompletedSpray_DeductsStock_CreatesMovements_AndLogsAudit()
    {
        // Arrange
        var farm = CreateActiveFarm();
        var area = CreateActiveArea(farm.Id);
        var plantation = CreateActivePlantation(farm.Id, area.Id);
        var cycle = CreateActiveCropCycle(plantation.Id);
        var stage = CreateCropCycleStage(cycle.Id);

        var location = CreateStorageLocation(farm.Id);
        var item = CreateInventoryItem("Kumulus DF");
        var balance = CreateStockBalance(farm.Id, location.Id, item.Id, 100m);

        var areaUnit = CreateUnit("HA", "Hectare", UnitCategory.Area);
        var waterUnit = CreateUnit("L", "Liter", UnitCategory.Volume);
        var target = CreateTarget();
        var method = CreateApplicationMethod();

        var appTime = DateTimeOffset.UtcNow.AddHours(-1);
        var request = new RecordCompletedSprayRequest(
            FarmId: farm.Id,
            FarmAreaId: area.Id,
            PlantationId: plantation.Id,
            CropCycleId: cycle.Id,
            CropCycleStageId: stage.Id,
            ActualApplicationDateTime: appTime,
            ActualTreatedArea: 4.5m,
            ActualTreatedAreaUnitId: areaUnit.Id,
            WaterQuantity: 500m,
            WaterUnitId: waterUnit.Id,
            TargetId: target.Id,
            ApplicationMethodId: method.Id,
            PurposeReason: "Routine fungicide cover",
            Products: [
                new RecordCompletedSprayProductItemRequest(
                    InventoryItemId: item.Id,
                    StorageLocationId: location.Id,
                    ActualQuantity: 25.0m,
                    Dosage: "5.5 kg/ha")
            ]);

        // Act
        var result = await _sut.RecordCompletedAsync(CreateActor(), request, "127.0.0.1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(SprayStatus.Completed, result.Status);
        Assert.Equal("COMPLETED", result.StatusName);
        Assert.Null(result.PlannedDate);
        Assert.Null(result.ScheduledDateTime);
        Assert.Null(result.PlannedArea);

        Assert.Equal(farm.Id, result.FarmId);
        Assert.Equal(area.Id, result.FarmAreaId);
        Assert.Equal(plantation.Id, result.PlantationId);
        Assert.Equal(cycle.Id, result.CropCycleId);
        Assert.Equal(stage.Id, result.CropCycleStageId);
        Assert.Equal(appTime, result.ActualApplicationDateTime);
        Assert.Equal(4.5m, result.ActualTreatedArea);
        Assert.Equal(500m, result.WaterQuantity);

        // Product assertions
        var product = Assert.Single(result.Products);
        Assert.Equal(item.Id, product.InventoryItemId);
        Assert.Equal(location.Id, product.StorageLocationId);
        Assert.Equal(25.0m, product.ActualQuantity);
        Assert.Null(product.PlannedQuantity);
        Assert.Equal("5.5 kg/ha", product.Dosage);

        // Stock deduction: 100 - 25 = 75
        Assert.Equal(75.0m, balance.QuantityOnHand);

        // Stock movement assertion
        var movement = Assert.Single(_store.StockMovements);
        Assert.Equal(StockMovementType.Issue, movement.MovementType);
        Assert.Equal(result.Id, movement.SprayId);
        Assert.Equal(farm.Id, movement.FarmId);
        Assert.Equal(location.Id, movement.StorageLocationId);
        Assert.Equal(item.Id, movement.InventoryItemId);
        Assert.Equal(25.0m, movement.Quantity);
        Assert.Equal(area.Id, movement.FarmAreaId);
        Assert.Equal(plantation.Id, movement.PlantationId);
        Assert.Equal(cycle.Id, movement.CropCycleId);
        Assert.Equal(stage.Id, movement.CropCycleStageId);
        Assert.Null(movement.LaborActivityId);
        Assert.Equal($"SP-{result.Id.ToString()[..8].ToUpperInvariant()}", movement.ReferenceNumber);

        // Audit log
        var audit = Assert.Single(_store.AuditLogs);
        Assert.Equal("Spray.RecordedCompleted", audit.Action);
        Assert.Equal(result.Id, audit.EntityId);
    }

    [Fact]
    public async Task RecordCompletedAsync_FarmLevelOnly_SucceedsWithoutSubHierarchy()
    {
        // Arrange
        var farm = CreateActiveFarm();
        var location = CreateStorageLocation(farm.Id);
        var item = CreateInventoryItem();
        var balance = CreateStockBalance(farm.Id, location.Id, item.Id, 50m);

        var request = new RecordCompletedSprayRequest(
            FarmId: farm.Id,
            ActualApplicationDateTime: DateTimeOffset.UtcNow.AddMinutes(-30),
            Products: [
                new RecordCompletedSprayProductItemRequest(
                    InventoryItemId: item.Id,
                    StorageLocationId: location.Id,
                    ActualQuantity: 10.0m)
            ]);

        // Act
        var result = await _sut.RecordCompletedAsync(CreateActor(), request, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(SprayStatus.Completed, result.Status);
        Assert.Equal("COMPLETED", result.StatusName);
        Assert.Equal(farm.Id, result.FarmId);
        Assert.Null(result.FarmAreaId);
        Assert.Null(result.PlantationId);
        Assert.Null(result.CropCycleId);
        Assert.Null(result.CropCycleStageId);

        Assert.Equal(40.0m, balance.QuantityOnHand);
        var movement = Assert.Single(_store.StockMovements);
        Assert.Equal(10.0m, movement.Quantity);
        Assert.Null(movement.FarmAreaId);
        Assert.Null(movement.PlantationId);
    }

    [Fact]
    public async Task RecordCompletedAsync_WhenInsufficientStock_ThrowsValidationException_RollsBack_CreatesNoSprayOrMovements()
    {
        // Arrange
        var farm = CreateActiveFarm();
        var location = CreateStorageLocation(farm.Id);
        var item = CreateInventoryItem();
        var balance = CreateStockBalance(farm.Id, location.Id, item.Id, 20m);

        var request = new RecordCompletedSprayRequest(
            FarmId: farm.Id,
            ActualApplicationDateTime: DateTimeOffset.UtcNow.AddMinutes(-10),
            Products: [
                new RecordCompletedSprayProductItemRequest(
                    InventoryItemId: item.Id,
                    StorageLocationId: location.Id,
                    ActualQuantity: 50.0m)
            ]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.RecordCompletedAsync(CreateActor(), request, null));
        AssertValidationError(ex, "Insufficient stock");

        // Balance untouched, 0 movements, 0 sprays
        Assert.Equal(20m, balance.QuantityOnHand);
        Assert.Empty(_store.StockMovements);
        Assert.Empty(_store.Sprays);
    }

    [Fact]
    public async Task RecordCompletedAsync_WhenFutureActualApplicationDateTime_ThrowsValidationException()
    {
        // Arrange
        var farm = CreateActiveFarm();
        var location = CreateStorageLocation(farm.Id);
        var item = CreateInventoryItem();
        CreateStockBalance(farm.Id, location.Id, item.Id, 50m);

        var request = new RecordCompletedSprayRequest(
            FarmId: farm.Id,
            ActualApplicationDateTime: DateTimeOffset.UtcNow.AddHours(2),
            Products: [
                new RecordCompletedSprayProductItemRequest(
                    InventoryItemId: item.Id,
                    StorageLocationId: location.Id,
                    ActualQuantity: 5.0m)
            ]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.RecordCompletedAsync(CreateActor(), request, null));
        AssertValidationError(ex, "future");
    }

    [Fact]
    public async Task RecordCompletedAsync_WhenNoProducts_ThrowsValidationException()
    {
        // Arrange
        var farm = CreateActiveFarm();
        var request = new RecordCompletedSprayRequest(
            FarmId: farm.Id,
            ActualApplicationDateTime: DateTimeOffset.UtcNow.AddMinutes(-10),
            Products: []);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.RecordCompletedAsync(CreateActor(), request, null));
        AssertValidationError(ex, "At least one product is required");
    }

    [Fact]
    public async Task RecordCompletedAsync_WhenProductActualQuantityZeroOrNegative_ThrowsValidationException()
    {
        // Arrange
        var farm = CreateActiveFarm();
        var location = CreateStorageLocation(farm.Id);
        var item = CreateInventoryItem();
        CreateStockBalance(farm.Id, location.Id, item.Id, 50m);

        var request = new RecordCompletedSprayRequest(
            FarmId: farm.Id,
            ActualApplicationDateTime: DateTimeOffset.UtcNow.AddMinutes(-10),
            Products: [
                new RecordCompletedSprayProductItemRequest(
                    InventoryItemId: item.Id,
                    StorageLocationId: location.Id,
                    ActualQuantity: 0m)
            ]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.RecordCompletedAsync(CreateActor(), request, null));
        AssertValidationError(ex, "greater than zero");
    }

    [Fact]
    public async Task RecordCompletedAsync_WhenDuplicateInventoryItems_ThrowsValidationException()
    {
        // Arrange
        var farm = CreateActiveFarm();
        var location = CreateStorageLocation(farm.Id);
        var item = CreateInventoryItem();
        CreateStockBalance(farm.Id, location.Id, item.Id, 50m);

        var request = new RecordCompletedSprayRequest(
            FarmId: farm.Id,
            ActualApplicationDateTime: DateTimeOffset.UtcNow.AddMinutes(-10),
            Products: [
                new RecordCompletedSprayProductItemRequest(
                    InventoryItemId: item.Id,
                    StorageLocationId: location.Id,
                    ActualQuantity: 5.0m),
                new RecordCompletedSprayProductItemRequest(
                    InventoryItemId: item.Id,
                    StorageLocationId: location.Id,
                    ActualQuantity: 3.0m)
            ]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.RecordCompletedAsync(CreateActor(), request, null));
        AssertValidationError(ex, "Duplicate inventory items");
    }

    [Fact]
    public async Task RecordCompletedAsync_WhenStorageLocationOnDifferentFarm_ThrowsValidationException()
    {
        // Arrange
        var farm1 = CreateActiveFarm();
        var farm2 = CreateActiveFarm();
        var locationOnOtherFarm = CreateStorageLocation(farm2.Id);
        var item = CreateInventoryItem();
        CreateStockBalance(farm2.Id, locationOnOtherFarm.Id, item.Id, 50m);

        var request = new RecordCompletedSprayRequest(
            FarmId: farm1.Id,
            ActualApplicationDateTime: DateTimeOffset.UtcNow.AddMinutes(-10),
            Products: [
                new RecordCompletedSprayProductItemRequest(
                    InventoryItemId: item.Id,
                    StorageLocationId: locationOnOtherFarm.Id,
                    ActualQuantity: 5.0m)
            ]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.RecordCompletedAsync(CreateActor(), request, null));
        AssertValidationError(ex, "does not belong to the spray farm");
    }

    [Fact]
    public async Task RecordCompletedAsync_WhenItemMissingPlantProtectionProfile_ThrowsValidationException()
    {
        // Arrange
        var farm = CreateActiveFarm();
        var location = CreateStorageLocation(farm.Id);
        var item = CreateInventoryItem();
        _store.ActiveProtectionProductItemIds.Remove(item.Id); // remove profile
        CreateStockBalance(farm.Id, location.Id, item.Id, 50m);

        var request = new RecordCompletedSprayRequest(
            FarmId: farm.Id,
            ActualApplicationDateTime: DateTimeOffset.UtcNow.AddMinutes(-10),
            Products: [
                new RecordCompletedSprayProductItemRequest(
                    InventoryItemId: item.Id,
                    StorageLocationId: location.Id,
                    ActualQuantity: 5.0m)
            ]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.RecordCompletedAsync(CreateActor(), request, null));
        AssertValidationError(ex, "active plant protection product");
    }

    [Fact]
    public async Task RecordCompletedAsync_WhenAreaOrWaterWithoutUnit_ThrowsValidationException()
    {
        // Arrange
        var farm = CreateActiveFarm();
        var location = CreateStorageLocation(farm.Id);
        var item = CreateInventoryItem();
        CreateStockBalance(farm.Id, location.Id, item.Id, 50m);

        var requestWithoutAreaUnit = new RecordCompletedSprayRequest(
            FarmId: farm.Id,
            ActualApplicationDateTime: DateTimeOffset.UtcNow.AddMinutes(-10),
            ActualTreatedArea: 5.0m,
            ActualTreatedAreaUnitId: null, // missing unit
            Products: [
                new RecordCompletedSprayProductItemRequest(
                    InventoryItemId: item.Id,
                    StorageLocationId: location.Id,
                    ActualQuantity: 5.0m)
            ]);

        // Act & Assert
        var ex1 = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.RecordCompletedAsync(CreateActor(), requestWithoutAreaUnit, null));
        AssertValidationError(ex1, "area unit is required");

        var requestWithoutWaterUnit = new RecordCompletedSprayRequest(
            FarmId: farm.Id,
            ActualApplicationDateTime: DateTimeOffset.UtcNow.AddMinutes(-10),
            WaterQuantity: 200m,
            WaterUnitId: null, // missing unit
            Products: [
                new RecordCompletedSprayProductItemRequest(
                    InventoryItemId: item.Id,
                    StorageLocationId: location.Id,
                    ActualQuantity: 5.0m)
            ]);

        var ex2 = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.RecordCompletedAsync(CreateActor(), requestWithoutWaterUnit, null));
        AssertValidationError(ex2, "water unit is required");
    }

    [Fact]
    public async Task RecordCompletedAsync_WhenInvalidHierarchy_ThrowsValidationException()
    {
        // Arrange
        var farm1 = CreateActiveFarm();
        var farm2 = CreateActiveFarm();
        var areaOnFarm2 = CreateActiveArea(farm2.Id);
        var location = CreateStorageLocation(farm1.Id);
        var item = CreateInventoryItem();
        CreateStockBalance(farm1.Id, location.Id, item.Id, 50m);

        var request = new RecordCompletedSprayRequest(
            FarmId: farm1.Id,
            FarmAreaId: areaOnFarm2.Id, // Area belongs to farm2
            ActualApplicationDateTime: DateTimeOffset.UtcNow.AddMinutes(-10),
            Products: [
                new RecordCompletedSprayProductItemRequest(
                    InventoryItemId: item.Id,
                    StorageLocationId: location.Id,
                    ActualQuantity: 5.0m)
            ]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.RecordCompletedAsync(CreateActor(), request, null));
        AssertValidationError(ex, "does not belong to the selected farm");
    }

    private sealed class FakeRecordCompletedSprayStore : ISprayStore
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
