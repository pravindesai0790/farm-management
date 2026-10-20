using System.Reflection;
using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.Fertilizer;
using FarmManagement.Application.Interfaces.Fertilizer;
using FarmManagement.Application.Services;
using FarmManagement.Application.Services.Fertilizer;
using FarmManagement.Domain.Constants;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Services;

public class FertilizerDraftCrudTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly FakeFertilizerStore _store = new();
    private readonly FertilizerApplicationService _sut;
    private readonly TestReferenceGenerator _refGen = new();

    public FertilizerDraftCrudTests()
    {
        var validator = new FertilizerValidator(_store);
        var stockManager = new FertilizerStockManager(_store);
        _sut = new FertilizerApplicationService(_store, validator, _refGen, stockManager);
    }

    private FertilizerActor CreateActor(Guid? orgId = null) =>
        new(_userId, orgId ?? _orgId);

    [Fact]
    public async Task CreateDraftAsync_EmptyDraft_AllocatesReferenceNumberAndPersistsDraft()
    {
        // Arrange
        var actor = CreateActor();
        var request = new CreateFertilizerDraftRequest();

        // Act
        var result = await _sut.CreateDraftAsync(actor, request, "127.0.0.1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("FERT-2026-000001", result.ReferenceNumber);
        Assert.Equal(FertilizerApplicationStatus.Draft, result.Status);
        Assert.Equal(1u, result.Version);
        Assert.Null(result.FarmId);
        Assert.Empty(result.Products);

        Assert.Single(_store.Applications);
        var saved = _store.Applications[0];
        Assert.Equal("FERT-2026-000001", saved.ReferenceNumber);
        Assert.Equal(FertilizerApplicationStatus.Draft, saved.Status);

        Assert.Single(_store.AuditLogs);
        Assert.Equal("Fertilizer.DraftCreated", _store.AuditLogs[0].Action);
        Assert.Equal(saved.Id, _store.AuditLogs[0].EntityId);
    }

    [Fact]
    public async Task CreateDraftAsync_WithValidHierarchyAndProducts_Succeeds()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, plantation, cycle, stage) = CreateValidHierarchyChain();
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var unit = CreateUnit("Hectare", "ha");
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var location = CreateStorageLocation(farm.Id);

        var request = new CreateFertilizerDraftRequest(
            FarmId: farm.Id,
            FarmAreaId: area.Id,
            PlantationId: plantation.Id,
            CropCycleId: cycle.Id,
            CropCycleStageId: stage.Id,
            ApplicationMethodId: method.Id,
            PlannedDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
            PlannedArea: 15.5m,
            PlannedAreaUnitId: unit.Id,
            PurposeReason: "Spring dressing",
            Products:
            [
                new FertilizerDraftProductLineRequest(
                    InventoryItemId: item.Id,
                    PlannedQuantity: 250m,
                    StorageLocationId: location.Id)
            ]);

        // Act
        var result = await _sut.CreateDraftAsync(actor, request, "127.0.0.1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(farm.Id, result.FarmId);
        Assert.Equal(area.Id, result.FarmAreaId);
        Assert.Equal(plantation.Id, result.PlantationId);
        Assert.Equal(cycle.Id, result.CropCycleId);
        Assert.Equal(stage.Id, result.CropCycleStageId);
        Assert.Equal(method.Id, result.ApplicationMethodId);
        Assert.Equal(15.5m, result.PlannedArea);
        Assert.Equal("Spring dressing", result.PurposeReason);
        Assert.Single(result.Products);
        Assert.Equal(item.Id, result.Products[0].InventoryItemId);
        Assert.Equal(250m, result.Products[0].PlannedQuantity);
        Assert.Equal(location.Id, result.Products[0].StorageLocationId);
    }

    [Fact]
    public async Task CreateDraftAsync_WithInvalidHierarchy_ThrowsValidationException()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, _, _, cycle, _) = CreateValidHierarchyChain();

        // Plantation is omitted, but cycle is supplied -> orphan cycle
        var request = new CreateFertilizerDraftRequest(
            FarmId: farm.Id,
            CropCycleId: cycle.Id);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CreateDraftAsync(actor, request, null));
        Assert.True(ex.Errors?.ContainsKey("cropCycleId") ?? false);
    }

    [Fact]
    public async Task CreateDraftAsync_WithIneligibleProduct_ThrowsValidationException()
    {
        // Arrange
        var actor = CreateActor();
        var pesticideItem = CreateInventoryItem(Guid.NewGuid()); // Not FertilizersSoilId

        var request = new CreateFertilizerDraftRequest(
            Products:
            [
                new FertilizerDraftProductLineRequest(pesticideItem.Id, 100m)
            ]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CreateDraftAsync(actor, request, null));
        Assert.True(ex.Errors?.ContainsKey("products") ?? false);
    }

    [Fact]
    public async Task CreateDraftAsync_WithDuplicateInventoryItems_ThrowsValidationException()
    {
        // Arrange
        var actor = CreateActor();
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);

        var request = new CreateFertilizerDraftRequest(
            Products:
            [
                new FertilizerDraftProductLineRequest(item.Id, 50m),
                new FertilizerDraftProductLineRequest(item.Id, 75m)
            ]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CreateDraftAsync(actor, request, null));
        Assert.True(ex.Errors?.ContainsKey("products") ?? false);
    }

    [Fact]
    public async Task CreateDraftAsync_WithNegativeQuantity_ThrowsValidationException()
    {
        // Arrange
        var actor = CreateActor();
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);

        var request = new CreateFertilizerDraftRequest(
            Products:
            [
                new FertilizerDraftProductLineRequest(item.Id, -10m)
            ]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CreateDraftAsync(actor, request, null));
        Assert.True(ex.Errors?.ContainsKey("products") ?? false);
    }

    [Fact]
    public async Task CreateDraftAsync_WithStorageLocationWithoutFarm_ThrowsValidationException()
    {
        // Arrange
        var actor = CreateActor();
        var farm = CreateFarm();
        var location = CreateStorageLocation(farm.Id);
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);

        var request = new CreateFertilizerDraftRequest(
            FarmId: null, // No farm selected
            Products:
            [
                new FertilizerDraftProductLineRequest(item.Id, 50m, location.Id)
            ]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CreateDraftAsync(actor, request, null));
        Assert.True(ex.Errors?.ContainsKey("products") ?? false);
    }

    [Fact]
    public async Task CreateDraftAsync_WithStorageLocationFromDifferentFarm_ThrowsValidationException()
    {
        // Arrange
        var actor = CreateActor();
        var farmA = CreateFarm();
        var farmB = CreateFarm();
        var locationB = CreateStorageLocation(farmB.Id);
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);

        var request = new CreateFertilizerDraftRequest(
            FarmId: farmA.Id,
            Products:
            [
                new FertilizerDraftProductLineRequest(item.Id, 50m, locationB.Id)
            ]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CreateDraftAsync(actor, request, null));
        Assert.True(ex.Errors?.ContainsKey("storageLocationId") ?? false);
    }

    [Fact]
    public async Task UpdateDraftAsync_ModifiesFieldsAndProducts_IncrementsVersion()
    {
        // Arrange
        var actor = CreateActor();
        var farm = CreateFarm();
        var location1 = CreateStorageLocation(farm.Id);
        var location2 = CreateStorageLocation(farm.Id);
        var item1 = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var item2 = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);

        var createReq = new CreateFertilizerDraftRequest(
            FarmId: farm.Id,
            PurposeReason: "Initial draft",
            Products:
            [
                new FertilizerDraftProductLineRequest(item1.Id, 100m, location1.Id)
            ]);
        var created = await _sut.CreateDraftAsync(actor, createReq, null);

        var updateReq = new UpdateFertilizerDraftRequest(
            FarmId: farm.Id,
            PurposeReason: "Updated draft description",
            Version: created.Version,
            Products:
            [
                // Update item1 quantity, add item2, remove nothing
                new FertilizerDraftProductLineRequest(item1.Id, 150m, location1.Id),
                new FertilizerDraftProductLineRequest(item2.Id, 80m, location2.Id)
            ]);

        // Act
        var updated = await _sut.UpdateDraftAsync(actor, created.Id, updateReq, "127.0.0.1");

        // Assert
        Assert.NotNull(updated);
        Assert.Equal("Updated draft description", updated.PurposeReason);
        Assert.Equal(2u, updated.Version);
        Assert.Equal(2, updated.Products.Count);

        var p1 = updated.Products.First(p => p.InventoryItemId == item1.Id);
        Assert.Equal(150m, p1.PlannedQuantity);

        var p2 = updated.Products.First(p => p.InventoryItemId == item2.Id);
        Assert.Equal(80m, p2.PlannedQuantity);

        var lastAudit = _store.AuditLogs.Last();
        Assert.Equal("Fertilizer.DraftUpdated", lastAudit.Action);
    }

    [Fact]
    public async Task UpdateDraftAsync_OptimisticConcurrencyConflict_ThrowsConflictException()
    {
        // Arrange
        var actor = CreateActor();
        var createReq = new CreateFertilizerDraftRequest(PurposeReason: "Initial");
        var created = await _sut.CreateDraftAsync(actor, createReq, null);

        // Caller sends stale version (e.g. version 99 instead of 1)
        var updateReq = new UpdateFertilizerDraftRequest(
            PurposeReason: "Conflict update",
            Version: 99);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.UpdateDraftAsync(actor, created.Id, updateReq, null));
        Assert.Contains("modified by another user", ex.Message);
    }

    [Fact]
    public async Task UpdateDraftAsync_NonDraftStatus_ThrowsConflictException()
    {
        // Arrange
        var actor = CreateActor();
        var farm = CreateFarm();
        var area = CreateFarmArea(farm.Id);
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var app = new FertilizerApplication(
            _orgId, "FERT-2026-000001", _userId,
            farmId: farm.Id, farmAreaId: area.Id, applicationMethodId: method.Id,
            plannedDate: DateOnly.FromDateTime(DateTime.UtcNow));
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm.Id);
        app.AddProduct(item.Id, 100m, _userId, loc.Id);
        app.Plan(DateTimeOffset.UtcNow, _userId); // Status is now Planned
        _store.Applications.Add(app);

        var updateReq = new UpdateFertilizerDraftRequest(PurposeReason: "Trying to edit planned as draft");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.UpdateDraftAsync(actor, app.Id, updateReq, null));
        Assert.Contains("Only draft fertilizer applications can be modified", ex.Message);
    }

    [Fact]
    public async Task UpdateDraftAsync_NonExistentOrDifferentOrg_ThrowsResourceNotFoundException()
    {
        // Arrange
        var actor = CreateActor();
        var updateReq = new UpdateFertilizerDraftRequest(PurposeReason: "Does not exist");

        // Act & Assert
        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _sut.UpdateDraftAsync(actor, Guid.NewGuid(), updateReq, null));
    }

    [Fact]
    public async Task DeleteDraftAsync_DraftStatus_RemovesDraftAndLogsAudit()
    {
        // Arrange
        var actor = CreateActor();
        var createReq = new CreateFertilizerDraftRequest(PurposeReason: "Draft to be deleted");
        var created = await _sut.CreateDraftAsync(actor, createReq, null);

        // Act
        await _sut.DeleteDraftAsync(actor, created.Id, "127.0.0.1");

        // Assert
        Assert.Empty(_store.Applications);
        var deleteAudit = _store.AuditLogs.Last();
        Assert.Equal("Fertilizer.DraftDeleted", deleteAudit.Action);
        Assert.Equal(created.Id, deleteAudit.EntityId);
    }

    [Fact]
    public async Task DeleteDraftAsync_NonDraftStatus_ThrowsConflictException()
    {
        // Arrange
        var actor = CreateActor();
        var app = new FertilizerApplication(_orgId, "FERT-2026-000001", _userId);
        SetPrivateProperty(app, nameof(FertilizerApplication.Status), FertilizerApplicationStatus.Completed);
        _store.Applications.Add(app);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.DeleteDraftAsync(actor, app.Id, null));
        Assert.Contains("Only draft fertilizer applications can be deleted", ex.Message);
        Assert.Single(_store.Applications); // Not deleted
    }

    [Fact]
    public async Task DeleteDraftAsync_DifferentOrg_ThrowsResourceNotFoundException()
    {
        // Arrange
        var actor = CreateActor();
        var otherOrgApp = new FertilizerApplication(Guid.NewGuid(), "FERT-2026-000001", Guid.NewGuid());
        _store.Applications.Add(otherOrgApp);

        // Act & Assert
        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _sut.DeleteDraftAsync(actor, otherOrgApp.Id, null));
    }

    [Fact]
    public async Task CopyAsNewDraftAsync_FromDraft_CreatesNewDraftWithNewReferenceAndSameContext()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, plantation, cycle, stage) = CreateValidHierarchyChain();
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var unit = CreateUnit("Hectare", "ha");
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var location = CreateStorageLocation(farm.Id);

        var createReq = new CreateFertilizerDraftRequest(
            FarmId: farm.Id,
            FarmAreaId: area.Id,
            PlantationId: plantation.Id,
            CropCycleId: cycle.Id,
            CropCycleStageId: stage.Id,
            ApplicationMethodId: method.Id,
            PlannedDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
            PlannedArea: 20m,
            PlannedAreaUnitId: unit.Id,
            PurposeReason: "Base copy test",
            Products:
            [
                new FertilizerDraftProductLineRequest(item.Id, 300m, location.Id)
            ]);
        var sourceDraft = await _sut.CreateDraftAsync(actor, createReq, null);

        // Act
        var copied = await _sut.CopyAsNewDraftAsync(actor, sourceDraft.Id, "127.0.0.1");

        // Assert
        Assert.NotNull(copied);
        Assert.NotEqual(sourceDraft.Id, copied.Id);
        Assert.Equal("FERT-2026-000002", copied.ReferenceNumber);
        Assert.Equal(FertilizerApplicationStatus.Draft, copied.Status);
        Assert.Equal(1u, copied.Version);

        // Context copied
        Assert.Equal(farm.Id, copied.FarmId);
        Assert.Equal(area.Id, copied.FarmAreaId);
        Assert.Equal(plantation.Id, copied.PlantationId);
        Assert.Equal(cycle.Id, copied.CropCycleId);
        Assert.Equal(stage.Id, copied.CropCycleStageId);
        Assert.Equal(method.Id, copied.ApplicationMethodId);
        Assert.Equal("Base copy test", copied.PurposeReason);
        Assert.Equal(20m, copied.PlannedArea);
        Assert.Equal(unit.Id, copied.PlannedAreaUnitId);

        // Dates NOT copied
        Assert.Null(copied.PlannedDate);
        Assert.Null(copied.ScheduledDate);
        Assert.Null(copied.ActualApplicationDate);

        // Products copied
        Assert.Single(copied.Products);
        Assert.Equal(item.Id, copied.Products[0].InventoryItemId);
        Assert.Equal(300m, copied.Products[0].PlannedQuantity);
        Assert.Equal(location.Id, copied.Products[0].StorageLocationId);

        var copyAudit = _store.AuditLogs.Last();
        Assert.Equal("Fertilizer.DraftCopied", copyAudit.Action);
        Assert.Equal(copied.Id, copyAudit.EntityId);
    }

    [Fact]
    public async Task CopyAsNewDraftAsync_FromCompleted_CreatesNewDraftWithoutActualsOrMovementRefs()
    {
        // Arrange
        var actor = CreateActor();
        var farm = CreateFarm();
        var area = CreateFarmArea(farm.Id);
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm.Id);

        var completedApp = new FertilizerApplication(
            _orgId, "FERT-2026-000001", _userId,
            farmId: farm.Id, farmAreaId: area.Id, applicationMethodId: method.Id,
            plannedDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)));
        var prod = completedApp.AddProduct(item.Id, 100m, _userId, loc.Id);
        prod.RecordActual(105m, Guid.NewGuid(), DateTimeOffset.UtcNow, _userId);

        completedApp.Plan(DateTimeOffset.UtcNow, _userId);
        completedApp.Schedule(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)), DateTimeOffset.UtcNow, _userId);
        completedApp.Start(DateTimeOffset.UtcNow, _userId);
        completedApp.Complete(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)), null, null, DateTimeOffset.UtcNow, _userId);
        _store.Applications.Add(completedApp);

        // Act
        var copied = await _sut.CopyAsNewDraftAsync(actor, completedApp.Id, null);

        // Assert
        Assert.NotNull(copied);
        Assert.Equal(FertilizerApplicationStatus.Draft, copied.Status);
        Assert.Null(copied.ActualApplicationDate);
        Assert.Null(copied.CompletedAt);
        Assert.Null(copied.StartedAt);

        Assert.Single(copied.Products);
        Assert.Equal(100m, copied.Products[0].PlannedQuantity);
        Assert.Null(copied.Products[0].ActualQuantity);
        Assert.Null(copied.Products[0].StockMovementId);
    }

    [Fact]
    public async Task CopyAsNewDraftAsync_DirectCompletedSourceWithoutPlannedQty_SetsPlannedQuantityToZero()
    {
        // Arrange
        var actor = CreateActor();
        var farm = CreateFarm();
        var area = CreateFarmArea(farm.Id);
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm.Id);

        var directCompleted = new FertilizerApplication(
            _orgId, "FERT-2026-000001", _userId,
            farmId: farm.Id, farmAreaId: area.Id, applicationMethodId: method.Id);
        // Direct completed lines have plannedQuantity = 0, actualQuantity = 50
        var prod = new FertilizerApplicationProduct(
            directCompleted.Id, item.Id, plannedQuantity: 0m, createdBy: _userId,
            storageLocationId: loc.Id, actualQuantity: 50m, stockMovementId: Guid.NewGuid());
        directCompleted.AddProduct(prod);
        SetPrivateProperty(directCompleted, nameof(FertilizerApplication.Status), FertilizerApplicationStatus.Completed);
        _store.Applications.Add(directCompleted);

        // Act
        var copied = await _sut.CopyAsNewDraftAsync(actor, directCompleted.Id, null);

        // Assert
        Assert.NotNull(copied);
        Assert.Equal(FertilizerApplicationStatus.Draft, copied.Status);
        Assert.Single(copied.Products);
        Assert.Equal(0m, copied.Products[0].PlannedQuantity);
        Assert.Null(copied.Products[0].ActualQuantity);
    }

    [Fact]
    public async Task CopyAsNewDraftAsync_DeactivatedStorageLocation_ClearsLocationToNull()
    {
        // Arrange
        var actor = CreateActor();
        var farm = CreateFarm();
        var area = CreateFarmArea(farm.Id);
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm.Id, isActive: false); // Deactivated location!

        var sourceDraft = new FertilizerApplication(
            _orgId, "FERT-2026-000001", _userId,
            farmId: farm.Id, farmAreaId: area.Id, applicationMethodId: method.Id);
        sourceDraft.AddProduct(item.Id, 100m, _userId, loc.Id);
        _store.Applications.Add(sourceDraft);

        // Act
        var copied = await _sut.CopyAsNewDraftAsync(actor, sourceDraft.Id, null);

        // Assert
        Assert.NotNull(copied);
        Assert.Single(copied.Products);
        Assert.Null(copied.Products[0].StorageLocationId); // Inactive location cleared to null
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

    private sealed class TestReferenceGenerator : IFertilizerReferenceGenerator
    {
        private int _sequence;
        public Task<string> NextReferenceNumberAsync(Guid organizationId, int year, CancellationToken cancellationToken = default)
        {
            _sequence++;
            return Task.FromResult($"FERT-{year}-{_sequence:D6}");
        }
    }

    private sealed class FakeFertilizerStore : IFertilizerApplicationStore
    {
        public List<FertilizerApplication> Applications { get; } = [];
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
        public List<AuditLog> AuditLogs { get; } = [];
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

        public Task<FertilizerApplication?> FindAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Applications.FirstOrDefault(a => a.Id == id && a.OrganizationId == organizationId));

        public Task<int> CountAsync(Guid organizationId, FertilizerApplicationListQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult(Applications.Count(a => a.OrganizationId == organizationId));

        public Task<IReadOnlyList<FertilizerApplication>> ListAsync(Guid organizationId, FertilizerApplicationListQuery query, int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FertilizerApplication>>(Applications.Where(a => a.OrganizationId == organizationId).Skip(skip).Take(take).ToList());

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

        public void Add(FertilizerApplication application) => Applications.Add(application);

        public void Remove(FertilizerApplication application) => Applications.Remove(application);

        public void RemoveProduct(FertilizerApplicationProduct product)
        {
            var app = Applications.FirstOrDefault(a => a.Id == product.FertilizerApplicationId);
            if (app is not null)
            {
                app.RemoveProduct(product.InventoryItemId);
            }
        }

        public void AddAuditLog(AuditLog auditLog) => AuditLogs.Add(auditLog);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
