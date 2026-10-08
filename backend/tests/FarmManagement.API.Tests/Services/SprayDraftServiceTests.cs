using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.Sprays;
using FarmManagement.Application.Interfaces.Sprays;
using FarmManagement.Application.Services;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Services;

public class SprayDraftServiceTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly FakeDraftSprayStore _store = new();
    private readonly SprayService _sut;

    public SprayDraftServiceTests()
    {
        _sut = new SprayService(_store);
    }

    private SprayActor CreateActor() => new(_userId, _orgId);

    private Farm CreateActiveFarm(Guid? farmId = null)
    {
        var farm = new Farm(_orgId, "Green Valley Farm", Guid.NewGuid(), Guid.NewGuid(), 100m, Guid.NewGuid());
        _store.Farms.Add(farm);
        return farm;
    }

    private FarmArea CreateActiveFarmArea(Guid farmId)
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

    private CropCycleStage CreateActiveCropCycleStage(Guid cycleId)
    {
        var stage = new CropCycleStage(cycleId, Guid.NewGuid(), "Vegetative", 1, 14, DateOnly.FromDateTime(DateTime.UtcNow), null, _userId);
        _store.CropCycleStages.Add(stage);
        return stage;
    }

    private InventoryItem CreateActiveProtectionItem(string name = "Azoxystrobin SC")
    {
        var unit = new Unit(null, "L", "Liter", "L", UnitCategory.Volume, isSystem: true);
        _store.Units.Add(unit);

        var item = new InventoryItem(_orgId, name, unit.Id, _userId, sku: "CHEM-AZOX-01");
        _store.InventoryItems.Add(item);
        _store.ActiveProtectionProductItemIds.Add(item.Id);
        return item;
    }

    [Fact]
    public async Task CreateDraftAsync_ValidDraftWithProducts_CreatesDraftAndLogsAudit()
    {
        // Arrange
        var farm = CreateActiveFarm();
        var area = CreateActiveFarmArea(farm.Id);
        var plantation = CreateActivePlantation(farm.Id, area.Id);
        var cycle = CreateActiveCropCycle(plantation.Id);
        var stage = CreateActiveCropCycleStage(cycle.Id);
        var item1 = CreateActiveProtectionItem("Fungicide A");
        var item2 = CreateActiveProtectionItem("Insecticide B");

        var target = new Target(_orgId, "T-01", "Powdery Mildew", TargetType.Disease);
        _store.Targets.Add(target);

        var method = new ApplicationMethod(_orgId, "AM-01", "Knapsack Sprayer");
        _store.ApplicationMethods.Add(method);

        var areaUnit = new Unit(null, "ACRE", "Acre", "ac", UnitCategory.Area, isSystem: true);
        _store.Units.Add(areaUnit);

        var waterUnit = new Unit(null, "LITER", "Liter", "L", UnitCategory.Volume, isSystem: true);
        _store.Units.Add(waterUnit);

        var request = new CreateSprayDraftRequest(
            FarmId: farm.Id,
            FarmAreaId: area.Id,
            PlantationId: plantation.Id,
            CropCycleId: cycle.Id,
            CropCycleStageId: stage.Id,
            PlannedDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            PlannedArea: 2.5m,
            PlannedAreaUnitId: areaUnit.Id,
            WaterQuantity: 200m,
            WaterUnitId: waterUnit.Id,
            TargetId: target.Id,
            ApplicationMethodId: method.Id,
            PurposeReason: "Routine preventative spray",
            Products:
            [
                new SprayProductItemRequest(item1.Id, 2.0m, "1 ml / L"),
                new SprayProductItemRequest(item2.Id, 1.0m, "0.5 ml / L")
            ]);

        // Act
        var result = await _sut.CreateDraftAsync(CreateActor(), request, "127.0.0.1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(farm.Id, result.FarmId);
        Assert.Equal(SprayStatus.Draft, result.Status);
        Assert.Equal("Routine preventative spray", result.PurposeReason);
        Assert.Equal(2, result.Products.Count);
        Assert.Equal(2, _store.Sprays.Single().Products.Count);

        var audit = Assert.Single(_store.AuditLogs);
        Assert.Equal("Spray.Created", audit.Action);
    }

    [Fact]
    public async Task CreateDraftAsync_WhenFarmNotFound_ThrowsResourceNotFoundException()
    {
        var request = new CreateSprayDraftRequest(
            FarmId: Guid.NewGuid(),
            PlannedDate: DateOnly.FromDateTime(DateTime.UtcNow));

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _sut.CreateDraftAsync(CreateActor(), request, null));
    }

    [Fact]
    public async Task CreateDraftAsync_WhenPlannedDateMissing_ThrowsValidationException()
    {
        var farm = CreateActiveFarm();
        var request = new CreateSprayDraftRequest(
            FarmId: farm.Id,
            PlannedDate: null);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CreateDraftAsync(CreateActor(), request, null));
        Assert.True(ex.Errors?.ContainsKey("plannedDate"));
    }

    [Fact]
    public async Task CreateDraftAsync_WhenFarmAreaBelongsToAnotherFarm_ThrowsValidationException()
    {
        var farm1 = CreateActiveFarm();
        var farm2 = CreateActiveFarm();
        var areaOfFarm2 = CreateActiveFarmArea(farm2.Id);

        var request = new CreateSprayDraftRequest(
            FarmId: farm1.Id,
            FarmAreaId: areaOfFarm2.Id,
            PlannedDate: DateOnly.FromDateTime(DateTime.UtcNow));

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CreateDraftAsync(CreateActor(), request, null));
        Assert.True(ex.Errors?.ContainsKey("farmAreaId"));
    }

    [Fact]
    public async Task CreateDraftAsync_WhenPlantationWithoutArea_ThrowsValidationException()
    {
        var farm = CreateActiveFarm();
        var area = CreateActiveFarmArea(farm.Id);
        var plantation = CreateActivePlantation(farm.Id, area.Id);

        var request = new CreateSprayDraftRequest(
            FarmId: farm.Id,
            FarmAreaId: null, // missing area!
            PlantationId: plantation.Id,
            PlannedDate: DateOnly.FromDateTime(DateTime.UtcNow));

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CreateDraftAsync(CreateActor(), request, null));
        Assert.True(ex.Errors?.ContainsKey("plantationId"));
    }

    [Fact]
    public async Task CreateDraftAsync_WhenPlantationBelongsToAnotherArea_ThrowsValidationException()
    {
        var farm = CreateActiveFarm();
        var area1 = CreateActiveFarmArea(farm.Id);
        var area2 = CreateActiveFarmArea(farm.Id);
        var plantationInArea2 = CreateActivePlantation(farm.Id, area2.Id);

        var request = new CreateSprayDraftRequest(
            FarmId: farm.Id,
            FarmAreaId: area1.Id,
            PlantationId: plantationInArea2.Id,
            PlannedDate: DateOnly.FromDateTime(DateTime.UtcNow));

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CreateDraftAsync(CreateActor(), request, null));
        Assert.True(ex.Errors?.ContainsKey("plantationId"));
    }

    [Fact]
    public async Task CreateDraftAsync_WhenCropCycleWithoutPlantation_ThrowsValidationException()
    {
        var farm = CreateActiveFarm();
        var area = CreateActiveFarmArea(farm.Id);
        var plantation = CreateActivePlantation(farm.Id, area.Id);
        var cycle = CreateActiveCropCycle(plantation.Id);

        var request = new CreateSprayDraftRequest(
            FarmId: farm.Id,
            FarmAreaId: area.Id,
            PlantationId: null, // missing plantation!
            CropCycleId: cycle.Id,
            PlannedDate: DateOnly.FromDateTime(DateTime.UtcNow));

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CreateDraftAsync(CreateActor(), request, null));
        Assert.True(ex.Errors?.ContainsKey("cropCycleId"));
    }

    [Fact]
    public async Task CreateDraftAsync_WhenCropCycleStageWithoutCycle_ThrowsValidationException()
    {
        var farm = CreateActiveFarm();
        var area = CreateActiveFarmArea(farm.Id);
        var plantation = CreateActivePlantation(farm.Id, area.Id);
        var cycle = CreateActiveCropCycle(plantation.Id);
        var stage = CreateActiveCropCycleStage(cycle.Id);

        var request = new CreateSprayDraftRequest(
            FarmId: farm.Id,
            FarmAreaId: area.Id,
            PlantationId: plantation.Id,
            CropCycleId: null, // missing cycle!
            CropCycleStageId: stage.Id,
            PlannedDate: DateOnly.FromDateTime(DateTime.UtcNow));

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CreateDraftAsync(CreateActor(), request, null));
        Assert.True(ex.Errors?.ContainsKey("cropCycleStageId"));
    }

    [Fact]
    public async Task CreateDraftAsync_WhenDuplicateProducts_ThrowsValidationException()
    {
        var farm = CreateActiveFarm();
        var item = CreateActiveProtectionItem();

        var request = new CreateSprayDraftRequest(
            FarmId: farm.Id,
            PlannedDate: DateOnly.FromDateTime(DateTime.UtcNow),
            Products:
            [
                new SprayProductItemRequest(item.Id, 1.0m),
                new SprayProductItemRequest(item.Id, 2.0m) // duplicate!
            ]);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CreateDraftAsync(CreateActor(), request, null));
        Assert.True(ex.Errors?.ContainsKey("products"));
    }

    [Fact]
    public async Task CreateDraftAsync_WhenProductHasNoActiveProtectionProfile_ThrowsValidationException()
    {
        var farm = CreateActiveFarm();
        var unit = new Unit(null, "L", "Liter", "L", UnitCategory.Volume, isSystem: true);
        _store.Units.Add(unit);
        var item = new InventoryItem(_orgId, "Plain Fertilizer", unit.Id, _userId, sku: "FERT-01");
        _store.InventoryItems.Add(item);
        // Do not add to _store.ActiveProtectionProductItemIds!

        var request = new CreateSprayDraftRequest(
            FarmId: farm.Id,
            PlannedDate: DateOnly.FromDateTime(DateTime.UtcNow),
            Products:
            [
                new SprayProductItemRequest(item.Id, 1.0m)
            ]);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CreateDraftAsync(CreateActor(), request, null));
        Assert.True(ex.Errors?.ContainsKey("products"));
    }

    [Fact]
    public async Task UpdateDraftAsync_WhenSprayNotFound_ThrowsResourceNotFoundException()
    {
        var request = new UpdateSprayDraftRequest(
            FarmId: Guid.NewGuid(),
            PlannedDate: DateOnly.FromDateTime(DateTime.UtcNow));

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _sut.UpdateDraftAsync(CreateActor(), Guid.NewGuid(), request, null));
    }

    [Fact]
    public async Task UpdateDraftAsync_WhenSprayNotDraft_ThrowsConflictException()
    {
        // Arrange
        var farm = CreateActiveFarm();
        var spray = new Spray(
            _orgId,
            farm.Id,
            _userId,
            status: SprayStatus.Scheduled,
            scheduledDateTime: DateTimeOffset.UtcNow.AddDays(1));
        _store.Sprays.Add(spray);

        var request = new UpdateSprayDraftRequest(
            FarmId: farm.Id,
            PlannedDate: DateOnly.FromDateTime(DateTime.UtcNow));

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.UpdateDraftAsync(CreateActor(), spray.Id, request, null));
    }

    [Fact]
    public async Task UpdateDraftAsync_ValidUpdate_SynchronizesProductsAndAudits()
    {
        // Arrange
        var farm = CreateActiveFarm();
        var itemKeep = CreateActiveProtectionItem("Product Keep");
        var itemRemove = CreateActiveProtectionItem("Product Remove");
        var itemAdd = CreateActiveProtectionItem("Product Add");

        var spray = new Spray(
            _orgId,
            farm.Id,
            _userId,
            status: SprayStatus.Draft,
            plannedDate: DateOnly.FromDateTime(DateTime.UtcNow));
        var productKeep = new SprayProduct(spray.Id, itemKeep.Id, _userId, plannedQuantity: 1.0m, dosage: "Initial");
        var productRemove = new SprayProduct(spray.Id, itemRemove.Id, _userId, plannedQuantity: 2.0m);
        spray.AddProduct(productKeep);
        spray.AddProduct(productRemove);
        _store.Sprays.Add(spray);

        var request = new UpdateSprayDraftRequest(
            FarmId: farm.Id,
            PlannedDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
            PurposeReason: "Updated reason",
            Products:
            [
                new SprayProductItemRequest(itemKeep.Id, 1.5m, "Updated dosage"), // modified
                new SprayProductItemRequest(itemAdd.Id, 3.0m, "New product")      // newly added
                // itemRemove omitted -> should be deleted
            ]);

        // Act
        var result = await _sut.UpdateDraftAsync(CreateActor(), spray.Id, request, "127.0.0.1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated reason", result.PurposeReason);
        Assert.Equal(2, result.Products.Count);

        var keptDto = Assert.Single(result.Products, p => p.InventoryItemId == itemKeep.Id);
        Assert.Equal(1.5m, keptDto.PlannedQuantity);
        Assert.Equal("Updated dosage", keptDto.Dosage);

        var addedDto = Assert.Single(result.Products, p => p.InventoryItemId == itemAdd.Id);
        Assert.Equal(3.0m, addedDto.PlannedQuantity);

        Assert.DoesNotContain(result.Products, p => p.InventoryItemId == itemRemove.Id);

        var audit = Assert.Single(_store.AuditLogs);
        Assert.Equal("Spray.DraftUpdated", audit.Action);
    }

    private sealed class FakeDraftSprayStore : ISprayStore
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
        public HashSet<Guid> ActiveProtectionProductItemIds { get; } = [];
        public List<AuditLog> AuditLogs { get; } = [];
        public List<SprayProduct> RemovedProducts { get; } = [];

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
            Task.FromResult<StorageLocation?>(null);

        public Task<StockBalance?> FindStockBalanceAsync(Guid storageLocationId, Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<StockBalance?>(null);

        public void RemoveSprayProduct(SprayProduct product) => RemovedProducts.Add(product);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
