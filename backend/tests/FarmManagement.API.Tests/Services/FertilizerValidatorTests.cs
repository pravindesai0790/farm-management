using System.Reflection;
using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.Fertilizer;
using FarmManagement.Application.Interfaces.Fertilizer;
using FarmManagement.Application.Services.Fertilizer;
using FarmManagement.Domain.Constants;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Services;

public class FertilizerValidatorTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly FakeValidatorStore _store = new();
    private readonly FertilizerValidator _sut;

    public FertilizerValidatorTests()
    {
        _sut = new FertilizerValidator(_store);
    }

    [Fact]
    public async Task ValidateHierarchy_FullValidHierarchy_Passes()
    {
        // Arrange
        var (farm, area, plantation, cycle, stage) = CreateValidHierarchyChain();

        // Act & Assert
        await _sut.ValidateHierarchyAsync(_orgId, farm.Id, area.Id, plantation.Id, cycle.Id, stage.Id);
    }

    [Fact]
    public async Task ValidateHierarchy_FarmNotFound_ThrowsResourceNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _sut.ValidateHierarchyAsync(_orgId, Guid.NewGuid(), null, null, null, null));
    }

    [Fact]
    public async Task ValidateHierarchy_InactiveFarm_PreStart_ThrowsValidationException()
    {
        // Arrange
        var farm = CreateFarm(isActive: false);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.ValidateHierarchyAsync(_orgId, farm.Id, null, null, null, null, isPreStart: true));
        Assert.NotNull(ex.Errors);
        Assert.Contains("farmId", ex.Errors.Keys);
    }

    [Fact]
    public async Task ValidateHierarchy_InactiveFarm_PostStart_Passes()
    {
        // Arrange
        var farm = CreateFarm(isActive: false);

        // Act & Assert
        await _sut.ValidateHierarchyAsync(_orgId, farm.Id, null, null, null, null, isPreStart: false);
    }

    [Fact]
    public async Task ValidateHierarchy_AreaWithoutFarm_ThrowsValidationException()
    {
        // Arrange
        var farm = CreateFarm();
        var area = CreateFarmArea(farm.Id);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.ValidateHierarchyAsync(_orgId, null, area.Id, null, null, null));
        Assert.NotNull(ex.Errors);
        Assert.Contains("farmAreaId", ex.Errors.Keys);
    }

    [Fact]
    public async Task ValidateHierarchy_AreaNotBelongingToFarm_ThrowsValidationException()
    {
        // Arrange
        var farm1 = CreateFarm();
        var farm2 = CreateFarm();
        var area = CreateFarmArea(farm2.Id);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.ValidateHierarchyAsync(_orgId, farm1.Id, area.Id, null, null, null));
        Assert.NotNull(ex.Errors);
        Assert.Contains("farmAreaId", ex.Errors.Keys);
    }

    [Fact]
    public async Task ValidateHierarchy_PlantationWithoutArea_ThrowsValidationException()
    {
        // Arrange
        var farm = CreateFarm();
        var area = CreateFarmArea(farm.Id);
        var plantation = CreatePlantation(farm.Id, area.Id);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.ValidateHierarchyAsync(_orgId, farm.Id, null, plantation.Id, null, null));
        Assert.NotNull(ex.Errors);
        Assert.Contains("plantationId", ex.Errors.Keys);
    }

    [Fact]
    public async Task ValidateHierarchy_PlantationNotBelongingToArea_ThrowsValidationException()
    {
        // Arrange
        var farm = CreateFarm();
        var area1 = CreateFarmArea(farm.Id);
        var area2 = CreateFarmArea(farm.Id);
        var plantation = CreatePlantation(farm.Id, area1.Id);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.ValidateHierarchyAsync(_orgId, farm.Id, area2.Id, plantation.Id, null, null));
        Assert.NotNull(ex.Errors);
        Assert.Contains("plantationId", ex.Errors.Keys);
    }

    [Fact]
    public async Task ValidateHierarchy_CropCycleWithoutPlantation_ThrowsValidationException()
    {
        // Arrange
        var farm = CreateFarm();
        var area = CreateFarmArea(farm.Id);
        var plantation = CreatePlantation(farm.Id, area.Id);
        var cycle = CreateCropCycle(plantation.Id);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.ValidateHierarchyAsync(_orgId, farm.Id, area.Id, null, cycle.Id, null));
        Assert.NotNull(ex.Errors);
        Assert.Contains("cropCycleId", ex.Errors.Keys);
    }

    [Fact]
    public async Task ValidateHierarchy_CropCycleStageWithoutCycle_ThrowsValidationException()
    {
        // Arrange
        var farm = CreateFarm();
        var area = CreateFarmArea(farm.Id);
        var plantation = CreatePlantation(farm.Id, area.Id);
        var cycle = CreateCropCycle(plantation.Id);
        var stage = CreateCropCycleStage(cycle.Id);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.ValidateHierarchyAsync(_orgId, farm.Id, area.Id, plantation.Id, null, stage.Id));
        Assert.NotNull(ex.Errors);
        Assert.Contains("cropCycleStageId", ex.Errors.Keys);
    }

    [Fact]
    public async Task ValidateProductEligibility_ActiveFertilizerItem_Passes()
    {
        // Arrange
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId, isActive: true);

        // Act & Assert
        await _sut.ValidateProductEligibilityAsync(_orgId, item.Id);
    }

    [Fact]
    public async Task ValidateProductEligibility_InactiveItem_PreStart_ThrowsValidationException()
    {
        // Arrange
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId, isActive: false);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.ValidateProductEligibilityAsync(_orgId, item.Id, isPreStart: true));
        Assert.NotNull(ex.Errors);
        Assert.Contains("products", ex.Errors.Keys);
    }

    [Fact]
    public async Task ValidateProductEligibility_InactiveItem_PostStart_Passes()
    {
        // Arrange
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId, isActive: false);

        // Act & Assert
        await _sut.ValidateProductEligibilityAsync(_orgId, item.Id, isPreStart: false);
    }

    [Fact]
    public async Task ValidateProductEligibility_NonFertilizerCategory_ThrowsValidationException()
    {
        // Arrange
        var item = CreateInventoryItem(FarmInventoryCategories.AgrochemicalsPestControlId, isActive: true);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.ValidateProductEligibilityAsync(_orgId, item.Id));
        Assert.NotNull(ex.Errors);
        Assert.Contains("products", ex.Errors.Keys);
    }

    [Fact]
    public async Task ValidateProductEligibility_ActivePlantProtectionProfile_ThrowsValidationException()
    {
        // Arrange
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId, isActive: true);
        _store.ActivePppItemIds.Add(item.Id);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.ValidateProductEligibilityAsync(_orgId, item.Id));
        Assert.NotNull(ex.Errors);
        Assert.Contains("products", ex.Errors.Keys);
    }

    [Fact]
    public async Task ValidateStorageLocation_BelongingToFarm_Passes()
    {
        // Arrange
        var farm = CreateFarm();
        var location = CreateStorageLocation(farm.Id, isActive: true);

        // Act & Assert
        await _sut.ValidateStorageLocationAsync(_orgId, farm.Id, location.Id);
    }

    [Fact]
    public async Task ValidateStorageLocation_BelongingToAnotherFarm_ThrowsValidationException()
    {
        // Arrange
        var farm1 = CreateFarm();
        var farm2 = CreateFarm();
        var location = CreateStorageLocation(farm2.Id, isActive: true);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.ValidateStorageLocationAsync(_orgId, farm1.Id, location.Id));
        Assert.NotNull(ex.Errors);
        Assert.Contains("storageLocationId", ex.Errors.Keys);
    }

    [Fact]
    public async Task ValidateStorageLocation_InactiveLocation_PreStart_ThrowsValidationException()
    {
        // Arrange
        var farm = CreateFarm();
        var location = CreateStorageLocation(farm.Id, isActive: false);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.ValidateStorageLocationAsync(_orgId, farm.Id, location.Id, isPreStart: true));
        Assert.NotNull(ex.Errors);
        Assert.Contains("storageLocationId", ex.Errors.Keys);
    }

    [Fact]
    public async Task ValidateStorageLocation_InactiveLocation_PostStart_Passes()
    {
        // Arrange
        var farm = CreateFarm();
        var location = CreateStorageLocation(farm.Id, isActive: false);

        // Act & Assert
        await _sut.ValidateStorageLocationAsync(_orgId, farm.Id, location.Id, isPreStart: false);
    }

    [Fact]
    public async Task ValidateApplicationMethod_FertilizerMethod_Passes()
    {
        // Arrange
        var method = CreateMethod("BROADCASTING", "Fertilizer", module: "FERTILIZER");

        // Act & Assert
        await _sut.ValidateApplicationMethodAsync(_orgId, method.Id, null);
    }

    [Fact]
    public async Task ValidateApplicationMethod_SprayMethod_ThrowsValidationException()
    {
        // Arrange
        var method = CreateMethod("FOLIAR", "Spray Method", module: "SPRAY");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.ValidateApplicationMethodAsync(_orgId, method.Id, null));
        Assert.NotNull(ex.Errors);
        Assert.Contains("applicationMethodId", ex.Errors.Keys);
    }

    [Fact]
    public async Task ValidateApplicationMethod_OtherWithoutDescription_ThrowsValidationException()
    {
        // Arrange
        var method = CreateMethod("OTHER", "Other", module: "FERTILIZER");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.ValidateApplicationMethodAsync(_orgId, method.Id, null));
        Assert.NotNull(ex.Errors);
        Assert.Contains("otherMethodDescription", ex.Errors.Keys);
    }

    [Fact]
    public async Task ValidateApplicationMethod_OtherWithDescription_Passes()
    {
        // Arrange
        var method = CreateMethod("OTHER", "Other", module: "FERTILIZER");

        // Act & Assert
        await _sut.ValidateApplicationMethodAsync(_orgId, method.Id, "Sub-surface precision injection");
    }

    [Fact]
    public async Task ValidateIrrigationLink_WithFertigationMethod_Passes()
    {
        // Arrange
        var farm = CreateFarm();
        var area = CreateFarmArea(farm.Id);
        var method = CreateMethod("FERTIGATION", "Fertigation", module: "FERTILIZER");
        var irrigation = CreateIrrigationEvent(farm.Id, area.Id);

        // Act & Assert
        await _sut.ValidateIrrigationLinkAsync(_orgId, method.Id, irrigation.Id, farm.Id, area.Id);
    }

    [Fact]
    public async Task ValidateIrrigationLink_WithNonFertigationMethod_ThrowsValidationException()
    {
        // Arrange
        var farm = CreateFarm();
        var area = CreateFarmArea(farm.Id);
        var method = CreateMethod("BROADCASTING", "Broadcasting", module: "FERTILIZER");
        var irrigation = CreateIrrigationEvent(farm.Id, area.Id);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.ValidateIrrigationLinkAsync(_orgId, method.Id, irrigation.Id, farm.Id, area.Id));
        Assert.NotNull(ex.Errors);
        Assert.Contains("irrigationEventId", ex.Errors.Keys);
    }

    [Fact]
    public async Task ValidateIrrigationLink_WithMismatchingFarmArea_ThrowsValidationException()
    {
        // Arrange
        var farm = CreateFarm();
        var area1 = CreateFarmArea(farm.Id);
        var area2 = CreateFarmArea(farm.Id);
        var method = CreateMethod("FERTIGATION", "Fertigation", module: "FERTILIZER");
        var irrigation = CreateIrrigationEvent(farm.Id, area1.Id);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.ValidateIrrigationLinkAsync(_orgId, method.Id, irrigation.Id, farm.Id, area2.Id));
        Assert.NotNull(ex.Errors);
        Assert.Contains("irrigationEventId", ex.Errors.Keys);
    }

    [Fact]
    public async Task ValidateAreaAndUnits_PositivePlannedAreaWithActiveUnit_Passes()
    {
        // Arrange
        var unit = CreateUnit("Hectare", "ha", isActive: true);

        // Act & Assert
        await _sut.ValidateAreaAndUnitsAsync(_orgId, 5.5m, unit.Id, null, null);
    }

    [Fact]
    public async Task ValidateAreaAndUnits_ZeroOrNegativePlannedArea_ThrowsValidationException()
    {
        // Arrange
        var unit = CreateUnit("Hectare", "ha", isActive: true);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.ValidateAreaAndUnitsAsync(_orgId, 0m, unit.Id, null, null));
        Assert.NotNull(ex.Errors);
        Assert.Contains("plannedArea", ex.Errors.Keys);
    }

    [Fact]
    public async Task ValidateAreaAndUnits_PlannedAreaWithoutUnit_ThrowsValidationException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.ValidateAreaAndUnitsAsync(_orgId, 5.5m, null, null, null));
        Assert.NotNull(ex.Errors);
        Assert.Contains("plannedAreaUnitId", ex.Errors.Keys);
    }

    [Fact]
    public void ValidateFreezeRules_InProgressApplication_ChangingFrozenField_ThrowsValidationException()
    {
        // Arrange
        var farm = CreateFarm();
        var app = new FertilizerApplication(_orgId, "FERT-2026-000001", _userId, farmId: farm.Id);
        SetPrivateProperty(app, nameof(FertilizerApplication.Status), FertilizerApplicationStatus.InProgress);

        var newFarmId = Guid.NewGuid();

        // Act & Assert
        var ex = Assert.Throws<ValidationException>(() =>
            _sut.ValidateFreezeRules(app, newFarmId, null, null, null, null, null, null, null, null, null, null, null, null));
        Assert.NotNull(ex.Errors);
        Assert.Contains("status", ex.Errors.Keys);
    }

    [Fact]
    public void ValidateFreezeRules_InProgressApplication_NoChangesToFrozenFields_Passes()
    {
        // Arrange
        var farm = CreateFarm();
        var app = new FertilizerApplication(_orgId, "FERT-2026-000001", _userId, farmId: farm.Id);
        SetPrivateProperty(app, nameof(FertilizerApplication.Status), FertilizerApplicationStatus.InProgress);

        // Act & Assert
        _sut.ValidateFreezeRules(app, farm.Id, null, null, null, null, null, null, null, null, null, null, null, null);
    }

    [Fact]
    public void ValidateFreezeRules_DraftApplication_AllowsContextChanges()
    {
        // Arrange
        var farm = CreateFarm();
        var app = new FertilizerApplication(_orgId, "FERT-2026-000001", _userId, farmId: farm.Id);
        Assert.Equal(FertilizerApplicationStatus.Draft, app.Status);

        var newFarmId = Guid.NewGuid();

        // Act & Assert: does not throw
        _sut.ValidateFreezeRules(app, newFarmId, null, null, null, null, null, null, null, null, null, null, null, null);
    }

    // Helper setup methods
    private Farm CreateFarm(bool isActive = true)
    {
        var farm = new Farm(_orgId, "Valley Farm", Guid.NewGuid(), Guid.NewGuid(), 100m, Guid.NewGuid());
        if (!isActive) farm.Deactivate(DateTimeOffset.UtcNow, _userId);
        _store.Farms.Add(farm);
        return farm;
    }

    private FarmArea CreateFarmArea(Guid farmId, bool isActive = true)
    {
        var area = new FarmArea(_orgId, farmId, null, "East Plot", 25m, Guid.NewGuid(), _userId);
        if (!isActive) area.Deactivate(DateTimeOffset.UtcNow, _userId);
        _store.FarmAreas.Add(area);
        return area;
    }

    private CropPlantation CreatePlantation(Guid farmId, Guid farmAreaId, PlantationStatus status = PlantationStatus.Active)
    {
        var plantation = new CropPlantation(
            _orgId, farmId, farmAreaId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Orchard Plot A", 10m, Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), null, _userId);
        SetPrivateProperty(plantation, nameof(CropPlantation.Status), status);
        _store.Plantations.Add(plantation);
        return plantation;
    }

    private CropCycle CreateCropCycle(Guid plantationId, CropCycleStatus status = CropCycleStatus.Active)
    {
        var cycle = new CropCycle(_orgId, plantationId, "Cycle 2026-1", 2026, "Spring", DateOnly.FromDateTime(DateTime.UtcNow), null, _userId);
        SetPrivateProperty(cycle, nameof(CropCycle.Status), status);
        _store.CropCycles.Add(cycle);
        return cycle;
    }

    private CropCycleStage CreateCropCycleStage(Guid cropCycleId)
    {
        var stage = new CropCycleStage(cropCycleId, Guid.NewGuid(), "Vegetative", 1, 14, DateOnly.FromDateTime(DateTime.UtcNow), null, _userId);
        _store.CropCycleStages.Add(stage);
        return stage;
    }

    private (Farm, FarmArea, CropPlantation, CropCycle, CropCycleStage) CreateValidHierarchyChain()
    {
        var farm = CreateFarm();
        var area = CreateFarmArea(farm.Id);
        var plantation = CreatePlantation(farm.Id, area.Id);
        var cycle = CreateCropCycle(plantation.Id);
        var stage = CreateCropCycleStage(cycle.Id);
        return (farm, area, plantation, cycle, stage);
    }

    private InventoryItem CreateInventoryItem(Guid categoryId, bool isActive = true)
    {
        var item = new InventoryItem(_orgId, "Urea 46%", Guid.NewGuid(), _userId, "UREA-46", null, categoryId);
        if (!isActive) item.Deactivate(DateTimeOffset.UtcNow, _userId);
        _store.InventoryItems.Add(item);
        return item;
    }

    private StorageLocation CreateStorageLocation(Guid farmId, bool isActive = true)
    {
        var loc = new StorageLocation(_orgId, farmId, "North Silo", _userId, "Main Storage");
        if (!isActive) loc.Deactivate(DateTimeOffset.UtcNow, _userId);
        _store.StorageLocations.Add(loc);
        return loc;
    }

    private ApplicationMethod CreateMethod(string code, string name, string module = "FERTILIZER", bool isActive = true)
    {
        var method = new ApplicationMethod(_orgId, code, name, isSystem: false, description: null, displayOrder: 1, createdBy: _userId, module: module);
        if (!isActive) method.Deactivate(DateTimeOffset.UtcNow, _userId);
        _store.ApplicationMethods.Add(method);
        return method;
    }

    private IrrigationEvent CreateIrrigationEvent(Guid farmId, Guid farmAreaId)
    {
        var irrigation = new IrrigationEvent(_orgId, farmId, farmAreaId, _userId);
        _store.IrrigationEvents.Add(irrigation);
        return irrigation;
    }

    private Unit CreateUnit(string name, string symbol, bool isActive = true)
    {
        var unit = new Unit(_orgId, "HA", name, symbol, UnitCategory.Area, baseUnitCode: null, conversionFactor: null, isSystem: false, displayOrder: 1, createdBy: _userId);
        if (!isActive) unit.Deactivate(DateTimeOffset.UtcNow, _userId);
        _store.Units.Add(unit);
        return unit;
    }

    private static void SetPrivateProperty<T>(T target, string propertyName, object? value)
    {
        var prop = typeof(T).GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (prop != null && prop.CanWrite)
        {
            prop.SetValue(target, value);
        }
        else
        {
            var field = typeof(T).GetField($"<{propertyName}>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(target, value);
        }
    }

    private sealed class FakeValidatorStore : IFertilizerApplicationStore
    {
        public List<Farm> Farms { get; } = [];
        public List<FarmArea> FarmAreas { get; } = [];
        public List<CropPlantation> Plantations { get; } = [];
        public List<CropCycle> CropCycles { get; } = [];
        public List<CropCycleStage> CropCycleStages { get; } = [];
        public List<ApplicationMethod> ApplicationMethods { get; } = [];
        public List<Unit> Units { get; } = [];
        public List<InventoryItem> InventoryItems { get; } = [];
        public HashSet<Guid> ActivePppItemIds { get; } = [];
        public List<StorageLocation> StorageLocations { get; } = [];
        public List<IrrigationEvent> IrrigationEvents { get; } = [];
        public List<StockBalance> StockBalances { get; } = [];

        public Task<FertilizerApplication?> FindAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<FertilizerApplication?>(null);

        public Task<int> CountAsync(Guid organizationId, FertilizerApplicationListQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<IReadOnlyList<FertilizerApplication>> ListAsync(Guid organizationId, FertilizerApplicationListQuery query, int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FertilizerApplication>>([]);

        public Task<IReadOnlyList<FertilizerApplication>> ListForExportAsync(Guid organizationId, FertilizerApplicationListQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FertilizerApplication>>([]);

        public Task<IReadOnlyList<FertilizerProductLookupResponse>> ListProductsLookupAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FertilizerProductLookupResponse>>([]);

        public Task<IReadOnlyList<FertilizerStorageLocationLookupResponse>> ListStorageLocationsLookupAsync(Guid organizationId, Guid farmId, Guid? inventoryItemId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FertilizerStorageLocationLookupResponse>>([]);

        public Task<IReadOnlyList<FertilizerApplicationMethodLookupResponse>> ListApplicationMethodsLookupAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FertilizerApplicationMethodLookupResponse>>([]);

        public Task<IReadOnlyList<FertilizerIrrigationLookupResponse>> ListIrrigationsLookupAsync(Guid organizationId, Guid? farmId, Guid? farmAreaId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FertilizerIrrigationLookupResponse>>([]);

        public Task<IReadOnlyDictionary<Guid, string>> GetUserNamesAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());

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

        public Task<ApplicationMethod?> FindApplicationMethodAsync(Guid applicationMethodId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ApplicationMethods.FirstOrDefault(m => m.Id == applicationMethodId && ((m.IsSystem && m.OrganizationId == null) || m.OrganizationId == organizationId)));

        public Task<Unit?> FindUnitAsync(Guid unitId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Units.FirstOrDefault(u => u.Id == unitId && ((u.IsSystem && u.OrganizationId == null) || u.OrganizationId == organizationId)));

        public Task<InventoryItem?> FindInventoryItemAsync(Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(InventoryItems.FirstOrDefault(i => i.Id == inventoryItemId && i.OrganizationId == organizationId));

        public Task<bool> HasActivePlantProtectionProfileAsync(Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ActivePppItemIds.Contains(inventoryItemId));

        public Task<StorageLocation?> FindStorageLocationAsync(Guid storageLocationId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(StorageLocations.FirstOrDefault(l => l.Id == storageLocationId && l.OrganizationId == organizationId));

        public Task<IrrigationEvent?> FindIrrigationEventAsync(Guid irrigationEventId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(IrrigationEvents.FirstOrDefault(e => e.Id == irrigationEventId && e.OrganizationId == organizationId));

        public Task<StockBalance?> FindStockBalanceAsync(Guid storageLocationId, Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(StockBalances.FirstOrDefault(sb => sb.StorageLocationId == storageLocationId && sb.InventoryItemId == inventoryItemId && sb.OrganizationId == organizationId));

        public List<StockMovement> StockMovements { get; } = [];
        public List<FertilizerOperationIdempotencyRecord> IdempotencyRecords { get; } = [];

        public Task<StockBalance?> LockBalanceAsync(Guid storageLocationId, Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(StockBalances.FirstOrDefault(sb => sb.StorageLocationId == storageLocationId && sb.InventoryItemId == inventoryItemId && sb.OrganizationId == organizationId));

        public Task AcquireAdvisoryLockAsync(Guid storageLocationId, Guid inventoryItemId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) =>
            await operation(cancellationToken);

        public void AddMovement(StockMovement movement) => StockMovements.Add(movement);

        public Task<FertilizerOperationIdempotencyRecord?> FindIdempotencyRecordAsync(Guid organizationId, string operationType, string idempotencyKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(IdempotencyRecords.FirstOrDefault(r => r.OrganizationId == organizationId && r.OperationType == operationType && r.IdempotencyKey == idempotencyKey));

        public void AddIdempotencyRecord(FertilizerOperationIdempotencyRecord record) => IdempotencyRecords.Add(record);

        public void Add(FertilizerApplication application) { }
        public void Remove(FertilizerApplication application) { }
        public void AddProduct(FertilizerApplicationProduct product) { }
        public void RemoveProduct(FertilizerApplicationProduct product) { }
        public void AddAuditLog(AuditLog auditLog) { }
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
