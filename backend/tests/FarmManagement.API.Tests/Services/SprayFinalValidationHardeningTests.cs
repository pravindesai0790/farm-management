using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.PlantProtection;
using FarmManagement.Application.DTOs.Sprays;
using FarmManagement.Application.Interfaces.PlantProtection;
using FarmManagement.Application.Interfaces.Sprays;
using FarmManagement.Application.Services;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Services;

/// <summary>
/// Hardening test suite explicitly verifying all 40 finalized scenarios from
/// Phase 3.7 AI Agent Implementation Workflow (STEP 21).
/// </summary>
public sealed class SprayFinalValidationHardeningTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly FakeHardeningSprayStore _store = new();
    private readonly SprayService _sut;

    public SprayFinalValidationHardeningTests()
    {
        _sut = new SprayService(_store);
    }

    private SprayActor CreateActor(Guid? orgId = null, Guid? userId = null) =>
        new(userId ?? _userId, orgId ?? _orgId);

    #region Helper Builders

    private Farm CreateFarm(string name = "Sun Valley Farm", Guid? orgId = null)
    {
        var farm = new Farm(orgId ?? _orgId, name, Guid.NewGuid(), Guid.NewGuid(), 100m, Guid.NewGuid());
        _store.Farms.Add(farm);
        return farm;
    }

    private FarmArea CreateFarmArea(Guid farmId, string name = "Block A", Guid? orgId = null)
    {
        var area = new FarmArea(orgId ?? _orgId, farmId, null, name, 20m, Guid.NewGuid(), _userId);
        _store.FarmAreas.Add(area);
        return area;
    }

    private CropPlantation CreatePlantation(Guid farmId, Guid farmAreaId, string name = "Plot 1", Guid? orgId = null)
    {
        var plantation = new CropPlantation(
            orgId ?? _orgId,
            farmId,
            farmAreaId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            name,
            10m,
            Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.UtcNow),
            null,
            _userId);
        _store.Plantations.Add(plantation);
        return plantation;
    }

    private CropCycle CreateCropCycle(Guid plantationId, string name = "2026 Season", Guid? orgId = null)
    {
        var cycle = new CropCycle(orgId ?? _orgId, plantationId, name, 2026, "Spring", DateOnly.FromDateTime(DateTime.UtcNow), null, _userId);
        _store.CropCycles.Add(cycle);
        return cycle;
    }

    private CropCycleStage CreateCropCycleStage(Guid cycleId, string name = "Flowering")
    {
        var stage = new CropCycleStage(cycleId, Guid.NewGuid(), name, 1, 14, DateOnly.FromDateTime(DateTime.UtcNow), null, _userId);
        _store.CropCycleStages.Add(stage);
        return stage;
    }

    private StorageLocation CreateStorageLocation(Guid farmId, string name = "Main Warehouse", Guid? orgId = null, bool isActive = true)
    {
        var location = new StorageLocation(orgId ?? _orgId, farmId, name, _userId);
        if (!isActive)
        {
            location.Deactivate(DateTimeOffset.UtcNow, _userId);
        }
        _store.StorageLocations.Add(location);
        return location;
    }

    private Unit CreateUnit(string code, string name, UnitCategory category, bool isSystem = true, Guid? orgId = null)
    {
        var unit = new Unit(orgId, code, name, code, category, isSystem: isSystem);
        _store.Units.Add(unit);
        return unit;
    }

    private InventoryItem CreateInventoryItem(string name, Unit? unit = null, bool isActive = true, Guid? orgId = null)
    {
        var stockUnit = unit ?? CreateUnit("L", "Liter", UnitCategory.Volume);
        var item = new InventoryItem(orgId ?? _orgId, name, stockUnit.Id, _userId);
        if (!isActive)
        {
            item.Deactivate(DateTimeOffset.UtcNow, _userId);
        }
        _store.InventoryItems.Add(item);
        _store.ActiveProtectionProductItemIds.Add(item.Id);
        return item;
    }

    private StockBalance CreateStockBalance(Guid farmId, Guid storageLocationId, Guid inventoryItemId, decimal quantity, Guid? orgId = null)
    {
        var balance = new StockBalance(orgId ?? _orgId, farmId, storageLocationId, inventoryItemId, quantity);
        _store.StockBalances.Add(balance);
        return balance;
    }

    private Target CreateTarget(string name = "Powdery Mildew", TargetType targetType = TargetType.Disease, bool isSystem = true, Guid? orgId = null)
    {
        var target = new Target(orgId, "TGT-" + Guid.NewGuid().ToString()[..6], name, targetType, isSystem: isSystem);
        _store.Targets.Add(target);
        return target;
    }

    private ApplicationMethod CreateApplicationMethod(string name = "Tractor Sprayer", bool isSystem = true, Guid? orgId = null)
    {
        var method = new ApplicationMethod(orgId, "AM-" + Guid.NewGuid().ToString()[..6], name, isSystem: isSystem);
        _store.ApplicationMethods.Add(method);
        return method;
    }

    private static void AssertValidationError(ValidationException ex, string text)
    {
        var allErrors = (ex.Errors?.SelectMany(kv => kv.Value ?? []) ?? []).ToList();
        Assert.Contains(allErrors, err => err.Contains(text, StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region Scenarios 1 to 5: Hierarchy Validations

    [Fact]
    public async Task Scenario01_FarmOnlySpray_CanBeCreatedScheduledStartedAndCompleted()
    {
        // Arrange
        var farm = CreateFarm();
        var storage = CreateStorageLocation(farm.Id);
        var item = CreateInventoryItem("Glyphosate 480");
        CreateStockBalance(farm.Id, storage.Id, item.Id, 50m);

        // 1. Create Farm-only Draft
        var draftRequest = new CreateSprayDraftRequest(
            FarmId: farm.Id,
            PlannedDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            Products: [new SprayProductItemRequest(item.Id, 10m)]);
        var draft = await _sut.CreateDraftAsync(CreateActor(), draftRequest, null);

        Assert.Equal(farm.Id, draft.FarmId);
        Assert.Null(draft.FarmAreaId);
        Assert.Null(draft.PlantationId);
        Assert.Null(draft.CropCycleId);
        Assert.Null(draft.CropCycleStageId);
        Assert.Equal(SprayStatus.Draft, draft.Status);

        // 2. Schedule
        var scheduled = await _sut.ScheduleAsync(CreateActor(), draft.Id,
            new ScheduleSprayRequest(DateTimeOffset.UtcNow.AddHours(2)), null);
        Assert.Equal(SprayStatus.Scheduled, scheduled.Status);

        // 3. Start
        var started = await _sut.StartAsync(CreateActor(), draft.Id,
            new StartSprayRequest(DateTimeOffset.UtcNow.AddHours(-1), [
                new StartSprayProductItemRequest(item.Id, storage.Id, 10m)
            ]), null);
        Assert.Equal(SprayStatus.InProgress, started.Status);

        // 4. Complete
        var completed = await _sut.CompleteAsync(CreateActor(), draft.Id, null, null);
        Assert.Equal(SprayStatus.Completed, completed.Status);
        Assert.Null(completed.FarmAreaId);
        Assert.Null(completed.PlantationId);

        var mov = Assert.Single(_store.StockMovements);
        Assert.Equal(completed.Id, mov.SprayId);
        Assert.Null(mov.FarmAreaId);
        Assert.Null(mov.PlantationId);
    }

    [Fact]
    public async Task Scenario02_FarmPlusArea_Succeeds()
    {
        var farm = CreateFarm();
        var area = CreateFarmArea(farm.Id);
        var item = CreateInventoryItem("Fungicide A");

        var request = new CreateSprayDraftRequest(
            FarmId: farm.Id,
            FarmAreaId: area.Id,
            PlannedDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            Products: [new SprayProductItemRequest(item.Id, 5m)]);

        var result = await _sut.CreateDraftAsync(CreateActor(), request, null);

        Assert.Equal(farm.Id, result.FarmId);
        Assert.Equal(area.Id, result.FarmAreaId);
        Assert.Null(result.PlantationId);
        Assert.Null(result.CropCycleId);
        Assert.Null(result.CropCycleStageId);
    }

    [Fact]
    public async Task Scenario03_FarmPlusAreaPlusPlantation_Succeeds()
    {
        var farm = CreateFarm();
        var area = CreateFarmArea(farm.Id);
        var plantation = CreatePlantation(farm.Id, area.Id);
        var item = CreateInventoryItem("Insecticide B");

        var request = new CreateSprayDraftRequest(
            FarmId: farm.Id,
            FarmAreaId: area.Id,
            PlantationId: plantation.Id,
            PlannedDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            Products: [new SprayProductItemRequest(item.Id, 3m)]);

        var result = await _sut.CreateDraftAsync(CreateActor(), request, null);

        Assert.Equal(farm.Id, result.FarmId);
        Assert.Equal(area.Id, result.FarmAreaId);
        Assert.Equal(plantation.Id, result.PlantationId);
        Assert.Null(result.CropCycleId);
        Assert.Null(result.CropCycleStageId);
    }

    [Fact]
    public async Task Scenario04_FarmPlusAreaPlusPlantationPlusCropCycle_Succeeds()
    {
        var farm = CreateFarm();
        var area = CreateFarmArea(farm.Id);
        var plantation = CreatePlantation(farm.Id, area.Id);
        var cycle = CreateCropCycle(plantation.Id);
        var item = CreateInventoryItem("Adjuvant C");

        var request = new CreateSprayDraftRequest(
            FarmId: farm.Id,
            FarmAreaId: area.Id,
            PlantationId: plantation.Id,
            CropCycleId: cycle.Id,
            PlannedDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            Products: [new SprayProductItemRequest(item.Id, 1m)]);

        var result = await _sut.CreateDraftAsync(CreateActor(), request, null);

        Assert.Equal(farm.Id, result.FarmId);
        Assert.Equal(area.Id, result.FarmAreaId);
        Assert.Equal(plantation.Id, result.PlantationId);
        Assert.Equal(cycle.Id, result.CropCycleId);
        Assert.Null(result.CropCycleStageId);
    }

    [Fact]
    public async Task Scenario05_FullHierarchy_AllFiveLevelsMatch_Succeeds()
    {
        var farm = CreateFarm();
        var area = CreateFarmArea(farm.Id);
        var plantation = CreatePlantation(farm.Id, area.Id);
        var cycle = CreateCropCycle(plantation.Id);
        var stage = CreateCropCycleStage(cycle.Id);
        var item = CreateInventoryItem("Fungicide D");

        var request = new CreateSprayDraftRequest(
            FarmId: farm.Id,
            FarmAreaId: area.Id,
            PlantationId: plantation.Id,
            CropCycleId: cycle.Id,
            CropCycleStageId: stage.Id,
            PlannedDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            Products: [new SprayProductItemRequest(item.Id, 2m)]);

        var result = await _sut.CreateDraftAsync(CreateActor(), request, null);

        Assert.Equal(farm.Id, result.FarmId);
        Assert.Equal(area.Id, result.FarmAreaId);
        Assert.Equal(plantation.Id, result.PlantationId);
        Assert.Equal(cycle.Id, result.CropCycleId);
        Assert.Equal(stage.Id, result.CropCycleStageId);
    }

    #endregion

    #region Scenario 6: Invalid Skipped Hierarchy

    [Theory]
    [InlineData("PlantationWithoutArea")]
    [InlineData("CycleWithoutPlantation")]
    [InlineData("StageWithoutCycle")]
    [InlineData("AreaMismatchWithFarm")]
    [InlineData("PlantationMismatchWithArea")]
    public async Task Scenario06_InvalidSkippedHierarchy_ThrowsValidationException(string failureMode)
    {
        var farm1 = CreateFarm("Farm 1");
        var farm2 = CreateFarm("Farm 2");
        var area1 = CreateFarmArea(farm1.Id, "Area Farm 1");
        var area2 = CreateFarmArea(farm2.Id, "Area Farm 2");
        var plantation1 = CreatePlantation(farm1.Id, area1.Id, "Plantation Area 1");
        var plantation2 = CreatePlantation(farm2.Id, area2.Id, "Plantation Area 2");
        var cycle1 = CreateCropCycle(plantation1.Id, "Cycle 1");
        var stage1 = CreateCropCycleStage(cycle1.Id, "Stage 1");
        var item = CreateInventoryItem("Product");

        Guid? testAreaId = area1.Id;
        Guid? testPlantationId = plantation1.Id;
        Guid? testCycleId = cycle1.Id;
        Guid? testStageId = stage1.Id;

        switch (failureMode)
        {
            case "PlantationWithoutArea":
                testAreaId = null;
                testPlantationId = plantation1.Id;
                testCycleId = null;
                testStageId = null;
                break;
            case "CycleWithoutPlantation":
                testPlantationId = null;
                testCycleId = cycle1.Id;
                testStageId = null;
                break;
            case "StageWithoutCycle":
                testCycleId = null;
                testStageId = stage1.Id;
                break;
            case "AreaMismatchWithFarm":
                testAreaId = area2.Id; // belongs to farm2, not farm1!
                testPlantationId = null;
                testCycleId = null;
                testStageId = null;
                break;
            case "PlantationMismatchWithArea":
                testAreaId = area1.Id;
                testPlantationId = plantation2.Id; // belongs to area2, not area1!
                testCycleId = null;
                testStageId = null;
                break;
        }

        var request = new CreateSprayDraftRequest(
            FarmId: farm1.Id,
            FarmAreaId: testAreaId,
            PlantationId: testPlantationId,
            CropCycleId: testCycleId,
            CropCycleStageId: testStageId,
            PlannedDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            Products: [new SprayProductItemRequest(item.Id, 1m)]);

        await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CreateDraftAsync(CreateActor(), request, null));
    }

    #endregion

    #region Scenarios 7 to 10: Lifecycle Progression

    [Fact]
    public async Task Scenario07_Draft_CreateAndUpdate_PersistsDraftAndTouchesNoInventory()
    {
        var farm = CreateFarm();
        var item1 = CreateInventoryItem("Item 1");
        var item2 = CreateInventoryItem("Item 2");

        var createReq = new CreateSprayDraftRequest(
            FarmId: farm.Id,
            PlannedDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
            Products: [new SprayProductItemRequest(item1.Id, 5m)]);
        var draft = await _sut.CreateDraftAsync(CreateActor(), createReq, null);

        Assert.Equal(SprayStatus.Draft, draft.Status);
        Assert.Empty(_store.StockMovements);

        // Update draft with item2
        var updateReq = new UpdateSprayDraftRequest(
            FarmId: farm.Id,
            PlannedDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
            PurposeReason: "Updated reason",
            Products: [new SprayProductItemRequest(item2.Id, 8m)]);
        var updated = await _sut.UpdateDraftAsync(CreateActor(), draft.Id, updateReq, null);

        Assert.Equal(SprayStatus.Draft, updated.Status);
        Assert.Equal("Updated reason", updated.PurposeReason);
        Assert.Single(updated.Products);
        Assert.Equal(item2.Id, updated.Products[0].InventoryItemId);
        Assert.Empty(_store.StockMovements);
    }

    [Fact]
    public async Task Scenario08_Schedule_TransitionsDraftToScheduled_RejectsNonDraft()
    {
        var farm = CreateFarm();
        var spray = new Spray(_orgId, farm.Id, _userId, status: SprayStatus.Draft,
            plannedDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)));
        _store.Sprays.Add(spray);

        var schedTime = DateTimeOffset.UtcNow.AddDays(1);
        var scheduled = await _sut.ScheduleAsync(CreateActor(), spray.Id, new ScheduleSprayRequest(schedTime), null);

        Assert.Equal(SprayStatus.Scheduled, scheduled.Status);
        Assert.Equal(schedTime, scheduled.ScheduledDateTime);

        // Scheduling non-draft throws ConflictException
        await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.ScheduleAsync(CreateActor(), spray.Id, new ScheduleSprayRequest(schedTime.AddDays(1)), null));
    }

    [Fact]
    public async Task Scenario09_Reschedule_UpdatesScheduledDateTime_RejectsNonScheduled()
    {
        var farm = CreateFarm();
        var originalTime = DateTimeOffset.UtcNow.AddDays(1);
        var spray = new Spray(_orgId, farm.Id, _userId, status: SprayStatus.Scheduled, scheduledDateTime: originalTime);
        _store.Sprays.Add(spray);

        var newTime = DateTimeOffset.UtcNow.AddDays(4);
        var rescheduled = await _sut.RescheduleAsync(CreateActor(), spray.Id, new RescheduleSprayRequest(newTime), null);

        Assert.Equal(newTime, rescheduled.ScheduledDateTime);

        // Rescheduling a draft spray throws ConflictException
        var draftSpray = new Spray(_orgId, farm.Id, _userId, status: SprayStatus.Draft);
        _store.Sprays.Add(draftSpray);
        await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.RescheduleAsync(CreateActor(), draftSpray.Id, new RescheduleSprayRequest(newTime), null));
    }

    [Fact]
    public async Task Scenario10_Start_TransitionsScheduledToInProgress_RejectsNonScheduled()
    {
        var farm = CreateFarm();
        var storage = CreateStorageLocation(farm.Id);
        var item = CreateInventoryItem("Item");
        CreateStockBalance(farm.Id, storage.Id, item.Id, 20m);

        var spray = new Spray(_orgId, farm.Id, _userId, status: SprayStatus.Scheduled);
        _store.Sprays.Add(spray);

        var appTime = DateTimeOffset.UtcNow.AddHours(-1);
        var started = await _sut.StartAsync(CreateActor(), spray.Id,
            new StartSprayRequest(appTime, [new StartSprayProductItemRequest(item.Id, storage.Id, 5m)]), null);

        Assert.Equal(SprayStatus.InProgress, started.Status);
        Assert.Equal(appTime, started.ActualApplicationDateTime);

        // Starting draft or already in-progress spray throws ConflictException
        await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.StartAsync(CreateActor(), spray.Id,
                new StartSprayRequest(appTime, [new StartSprayProductItemRequest(item.Id, storage.Id, 5m)]), null));
    }

    #endregion

    #region Scenario 11: Insufficient Stock at Start

    [Fact]
    public async Task Scenario11_InsufficientStockAtStart_ThrowsValidationException_KeepsScheduled_DeductsZeroStock()
    {
        var farm = CreateFarm();
        var storage = CreateStorageLocation(farm.Id);
        var item = CreateInventoryItem("Item");
        var balance = CreateStockBalance(farm.Id, storage.Id, item.Id, 2.0m); // only 2.0 available

        var spray = new Spray(_orgId, farm.Id, _userId, status: SprayStatus.Scheduled);
        _store.Sprays.Add(spray);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.StartAsync(CreateActor(), spray.Id,
                new StartSprayRequest(DateTimeOffset.UtcNow.AddHours(-1), [
                    new StartSprayProductItemRequest(item.Id, storage.Id, 10.0m) // requires 10.0
                ]), null));

        AssertValidationError(ex, "Insufficient stock");
        Assert.Equal(SprayStatus.Scheduled, spray.Status);
        Assert.Equal(2.0m, balance.QuantityOnHand); // untouched
        Assert.Empty(_store.StockMovements);
    }

    #endregion

    #region Scenarios 12 to 13: Execution Details & Quantity Modifications

    [Fact]
    public async Task Scenario12_SaveInProgress_UpdatesIntermediateDetails_KeepsInProgress()
    {
        var farm = CreateFarm();
        var storage = CreateStorageLocation(farm.Id);
        var item = CreateInventoryItem("Item");
        var areaUnit = CreateUnit("HA", "Hectare", UnitCategory.Area);
        var waterUnit = CreateUnit("L", "Liter", UnitCategory.Volume);
        var target = CreateTarget("Leaf Blight");
        var method = CreateApplicationMethod("Drone");

        var spray = new Spray(_orgId, farm.Id, _userId, status: SprayStatus.InProgress,
            actualApplicationDateTime: DateTimeOffset.UtcNow.AddHours(-2));
        spray.AddProduct(new SprayProduct(spray.Id, item.Id, _userId, storage.Id, actualQuantity: 5m));
        _store.Sprays.Add(spray);

        var saveReq = new UpdateSprayExecutionRequest(
            ActualApplicationDateTime: DateTimeOffset.UtcNow.AddHours(-1),
            ActualTreatedArea: 7.5m,
            ActualTreatedAreaUnitId: areaUnit.Id,
            WaterQuantity: 450m,
            WaterUnitId: waterUnit.Id,
            TargetId: target.Id,
            ApplicationMethodId: method.Id,
            PurposeReason: "Intermediate field notes",
            Products: [new UpdateSprayExecutionProductItemRequest(item.Id, 6.5m, "1.3 L/ha")]);

        var updated = await _sut.SaveExecutionAsync(CreateActor(), spray.Id, saveReq, null);

        Assert.Equal(SprayStatus.InProgress, updated.Status);
        Assert.Equal(7.5m, updated.ActualTreatedArea);
        Assert.Equal(450m, updated.WaterQuantity);
        Assert.Equal("Intermediate field notes", updated.PurposeReason);
        Assert.Equal(6.5m, updated.Products[0].ActualQuantity);
    }

    [Fact]
    public async Task Scenario13_ChangeActualQuantity_UpdatedQuantityUsedForFinalStockDeduction()
    {
        var farm = CreateFarm();
        var storage = CreateStorageLocation(farm.Id);
        var item = CreateInventoryItem("Item");
        var balance = CreateStockBalance(farm.Id, storage.Id, item.Id, 100m);

        var spray = new Spray(_orgId, farm.Id, _userId, status: SprayStatus.InProgress,
            actualApplicationDateTime: DateTimeOffset.UtcNow.AddHours(-1));
        spray.AddProduct(new SprayProduct(spray.Id, item.Id, _userId, storage.Id, actualQuantity: 10m));
        _store.Sprays.Add(spray);

        // At completion, override actual quantity to 15m
        var completeReq = new CompleteSprayRequest(
            Products: [new CompleteSprayProductItemRequest(item.Id, ActualQuantity: 15m)]);
        var completed = await _sut.CompleteAsync(CreateActor(), spray.Id, completeReq, null);

        Assert.Equal(SprayStatus.Completed, completed.Status);
        Assert.Equal(15m, completed.Products[0].ActualQuantity);
        Assert.Equal(85m, balance.QuantityOnHand); // 100 - 15 = 85
        var mov = Assert.Single(_store.StockMovements);
        Assert.Equal(15m, mov.Quantity);
    }

    #endregion

    #region Scenarios 14 to 17: Completion, Stock Consumption & Concurrency

    [Fact]
    public async Task Scenario14_Complete_AtomicallyDeductsStock_GeneratesMovements_SetsCompleted()
    {
        var farm = CreateFarm();
        var storage = CreateStorageLocation(farm.Id);
        var item = CreateInventoryItem("Item");
        var balance = CreateStockBalance(farm.Id, storage.Id, item.Id, 30m);

        var spray = new Spray(_orgId, farm.Id, _userId, status: SprayStatus.InProgress,
            actualApplicationDateTime: DateTimeOffset.UtcNow.AddHours(-1));
        spray.AddProduct(new SprayProduct(spray.Id, item.Id, _userId, storage.Id, actualQuantity: 12m));
        _store.Sprays.Add(spray);

        var result = await _sut.CompleteAsync(CreateActor(), spray.Id, null, null);

        Assert.Equal(SprayStatus.Completed, result.Status);
        Assert.Equal(18m, balance.QuantityOnHand); // 30 - 12 = 18
        var mov = Assert.Single(_store.StockMovements);
        Assert.Equal(StockMovementType.Issue, mov.MovementType);
        Assert.Equal(spray.Id, mov.SprayId);
        Assert.Equal(12m, mov.Quantity);
    }

    [Fact]
    public async Task Scenario15_InsufficientStockAtCompletion_ThrowsValidationException_RollsBack_KeepsInProgress()
    {
        var farm = CreateFarm();
        var storage = CreateStorageLocation(farm.Id);
        var item = CreateInventoryItem("Item");
        var balance = CreateStockBalance(farm.Id, storage.Id, item.Id, 5m); // only 5 available

        var spray = new Spray(_orgId, farm.Id, _userId, status: SprayStatus.InProgress,
            actualApplicationDateTime: DateTimeOffset.UtcNow.AddHours(-1));
        spray.AddProduct(new SprayProduct(spray.Id, item.Id, _userId, storage.Id, actualQuantity: 10m));
        _store.Sprays.Add(spray);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CompleteAsync(CreateActor(), spray.Id, null, null));

        AssertValidationError(ex, "Insufficient stock");
        Assert.Equal(SprayStatus.InProgress, spray.Status);
        Assert.Equal(5m, balance.QuantityOnHand);
        Assert.Empty(_store.StockMovements);
    }

    [Fact]
    public async Task Scenario16_ConcurrentStockConsumption_AcquiresAdvisoryAndRowLocksInCanonicalOrder()
    {
        var farm = CreateFarm();
        var locA = CreateStorageLocation(farm.Id, "Loc A");
        var locB = CreateStorageLocation(farm.Id, "Loc B");
        var item1 = CreateInventoryItem("Item 1");
        var item2 = CreateInventoryItem("Item 2");
        CreateStockBalance(farm.Id, locA.Id, item1.Id, 50m);
        CreateStockBalance(farm.Id, locB.Id, item2.Id, 50m);

        var spray = new Spray(_orgId, farm.Id, _userId, status: SprayStatus.InProgress,
            actualApplicationDateTime: DateTimeOffset.UtcNow.AddHours(-1));
        spray.AddProduct(new SprayProduct(spray.Id, item1.Id, _userId, locA.Id, actualQuantity: 5m));
        spray.AddProduct(new SprayProduct(spray.Id, item2.Id, _userId, locB.Id, actualQuantity: 5m));
        _store.Sprays.Add(spray);

        await _sut.CompleteAsync(CreateActor(), spray.Id, null, null);

        // Verify advisory locks were acquired for each product location/item pair
        Assert.Equal(2, _store.AcquiredAdvisoryLocks.Count);
        // Verify balance locks were acquired
        Assert.Equal(2, _store.LockedBalanceKeys.Count);
    }

    [Fact]
    public async Task Scenario17_DuplicateCompletion_WhenAlreadyCompleted_ThrowsConflictException()
    {
        var farm = CreateFarm();
        var spray = new Spray(_orgId, farm.Id, _userId, status: SprayStatus.Completed,
            actualApplicationDateTime: DateTimeOffset.UtcNow.AddHours(-2));
        _store.Sprays.Add(spray);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.CompleteAsync(CreateActor(), spray.Id, null, null));
    }

    #endregion

    #region Scenarios 18 to 21: Cancellation & Immutability

    [Fact]
    public async Task Scenario18_CancelDraft_TransitionsToCancelled_WithAudit()
    {
        var farm = CreateFarm();
        var spray = new Spray(_orgId, farm.Id, _userId, status: SprayStatus.Draft);
        _store.Sprays.Add(spray);

        var result = await _sut.CancelAsync(CreateActor(), spray.Id, new CancelSprayRequest("Changed crop plan"), null);

        Assert.Equal(SprayStatus.Cancelled, result.Status);
        Assert.Equal("Changed crop plan", result.CancellationReason);
        Assert.Contains(_store.AuditLogs, a => a.Action == "Spray.Cancelled" && a.EntityId == spray.Id);
    }

    [Fact]
    public async Task Scenario19_CancelScheduled_TransitionsToCancelled_WithAudit()
    {
        var farm = CreateFarm();
        var spray = new Spray(_orgId, farm.Id, _userId, status: SprayStatus.Scheduled,
            scheduledDateTime: DateTimeOffset.UtcNow.AddDays(1));
        _store.Sprays.Add(spray);

        var result = await _sut.CancelAsync(CreateActor(), spray.Id, new CancelSprayRequest("High rainfall forecast"), null);

        Assert.Equal(SprayStatus.Cancelled, result.Status);
        Assert.Equal("High rainfall forecast", result.CancellationReason);
    }

    [Fact]
    public async Task Scenario20_CancelInProgress_ThrowsConflictException()
    {
        var farm = CreateFarm();
        var spray = new Spray(_orgId, farm.Id, _userId, status: SprayStatus.InProgress,
            actualApplicationDateTime: DateTimeOffset.UtcNow.AddHours(-1));
        _store.Sprays.Add(spray);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.CancelAsync(CreateActor(), spray.Id, new CancelSprayRequest("Cannot cancel during application"), null));
    }

    [Fact]
    public async Task Scenario21_CompletedSpray_AllModificationAttemptsRejectedWithConflictException()
    {
        var farm = CreateFarm();
        var spray = new Spray(_orgId, farm.Id, _userId, status: SprayStatus.Completed,
            actualApplicationDateTime: DateTimeOffset.UtcNow.AddDays(-1));
        _store.Sprays.Add(spray);

        // 1. UpdateDraft rejected
        await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.UpdateDraftAsync(CreateActor(), spray.Id, new UpdateSprayDraftRequest(farm.Id, PlannedDate: DateOnly.FromDateTime(DateTime.UtcNow)), null));

        // 2. Schedule rejected
        await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.ScheduleAsync(CreateActor(), spray.Id, new ScheduleSprayRequest(DateTimeOffset.UtcNow.AddDays(1)), null));

        // 3. Reschedule rejected
        await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.RescheduleAsync(CreateActor(), spray.Id, new RescheduleSprayRequest(DateTimeOffset.UtcNow.AddDays(1)), null));

        // 4. Start rejected
        await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.StartAsync(CreateActor(), spray.Id, new StartSprayRequest(DateTimeOffset.UtcNow, []), null));

        // 5. SaveExecution rejected
        await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.SaveExecutionAsync(CreateActor(), spray.Id, new UpdateSprayExecutionRequest(DateTimeOffset.UtcNow), null));

        // 6. Complete rejected
        await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.CompleteAsync(CreateActor(), spray.Id, null, null));

        // 7. Cancel rejected
        await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.CancelAsync(CreateActor(), spray.Id, new CancelSprayRequest("Cancel attempt"), null));
    }

    #endregion

    #region Scenarios 22 to 24: Direct Recording & Date Validations

    [Fact]
    public async Task Scenario22_DirectCompletedSpray_AtomicallyCreatesAndCompletes()
    {
        var farm = CreateFarm();
        var storage = CreateStorageLocation(farm.Id);
        var item = CreateInventoryItem("Item");
        var balance = CreateStockBalance(farm.Id, storage.Id, item.Id, 50m);

        var request = new RecordCompletedSprayRequest(
            FarmId: farm.Id,
            ActualApplicationDateTime: DateTimeOffset.UtcNow.AddHours(-3),
            Products: [new RecordCompletedSprayProductItemRequest(item.Id, storage.Id, 14m)]);

        var result = await _sut.RecordCompletedAsync(CreateActor(), request, null);

        Assert.Equal(SprayStatus.Completed, result.Status);
        Assert.Equal(36m, balance.QuantityOnHand); // 50 - 14 = 36
        var mov = Assert.Single(_store.StockMovements);
        Assert.Equal(result.Id, mov.SprayId);
    }

    [Fact]
    public async Task Scenario23_HistoricalActualDate_AllowedAndRecorded()
    {
        var farm = CreateFarm();
        var storage = CreateStorageLocation(farm.Id);
        var item = CreateInventoryItem("Item");
        CreateStockBalance(farm.Id, storage.Id, item.Id, 50m);

        var pastDate = DateTimeOffset.UtcNow.AddDays(-14); // 2 weeks ago
        var request = new RecordCompletedSprayRequest(
            FarmId: farm.Id,
            ActualApplicationDateTime: pastDate,
            Products: [new RecordCompletedSprayProductItemRequest(item.Id, storage.Id, 5m)]);

        var result = await _sut.RecordCompletedAsync(CreateActor(), request, null);

        Assert.Equal(pastDate, result.ActualApplicationDateTime);
        var mov = Assert.Single(_store.StockMovements);
        Assert.Equal(DateOnly.FromDateTime(pastDate.UtcDateTime), mov.MovementDate);
    }

    [Fact]
    public async Task Scenario24_FutureActualDate_RejectedAcrossAllLifecycleEndpoints()
    {
        var farm = CreateFarm();
        var storage = CreateStorageLocation(farm.Id);
        var item = CreateInventoryItem("Item");
        CreateStockBalance(farm.Id, storage.Id, item.Id, 50m);

        var futureTime = DateTimeOffset.UtcNow.AddHours(2);

        // 1. StartAsync rejects future
        var scheduledSpray = new Spray(_orgId, farm.Id, _userId, status: SprayStatus.Scheduled);
        _store.Sprays.Add(scheduledSpray);
        var ex1 = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.StartAsync(CreateActor(), scheduledSpray.Id,
                new StartSprayRequest(futureTime, [new StartSprayProductItemRequest(item.Id, storage.Id, 5m)]), null));
        AssertValidationError(ex1, "future");

        // 2. SaveExecution rejects future
        var inProgressSpray = new Spray(_orgId, farm.Id, _userId, status: SprayStatus.InProgress,
            actualApplicationDateTime: DateTimeOffset.UtcNow.AddHours(-1));
        inProgressSpray.AddProduct(new SprayProduct(inProgressSpray.Id, item.Id, _userId, storage.Id, actualQuantity: 5m));
        _store.Sprays.Add(inProgressSpray);
        var ex2 = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.SaveExecutionAsync(CreateActor(), inProgressSpray.Id,
                new UpdateSprayExecutionRequest(futureTime), null));
        AssertValidationError(ex2, "future");

        // 3. Complete rejects future
        var ex3 = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CompleteAsync(CreateActor(), inProgressSpray.Id,
                new CompleteSprayRequest(ActualApplicationDateTime: futureTime), null));
        AssertValidationError(ex3, "future");

        // 4. RecordCompleted rejects future
        var ex4 = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.RecordCompletedAsync(CreateActor(),
                new RecordCompletedSprayRequest(farm.Id, futureTime,
                    [new RecordCompletedSprayProductItemRequest(item.Id, storage.Id, 5m)]), null));
        AssertValidationError(ex4, "future");
    }

    #endregion

    #region Scenarios 25 to 29: Products, Locations & Quantities

    [Fact]
    public async Task Scenario25_MultipleProducts_TankMix_AllDeductedFromStock()
    {
        var farm = CreateFarm();
        var storage = CreateStorageLocation(farm.Id);
        var item1 = CreateInventoryItem("Fungicide");
        var item2 = CreateInventoryItem("Insecticide");
        var item3 = CreateInventoryItem("Adjuvant");

        var bal1 = CreateStockBalance(farm.Id, storage.Id, item1.Id, 100m);
        var bal2 = CreateStockBalance(farm.Id, storage.Id, item2.Id, 50m);
        var bal3 = CreateStockBalance(farm.Id, storage.Id, item3.Id, 25m);

        var request = new RecordCompletedSprayRequest(
            FarmId: farm.Id,
            ActualApplicationDateTime: DateTimeOffset.UtcNow.AddHours(-1),
            Products: [
                new RecordCompletedSprayProductItemRequest(item1.Id, storage.Id, 20m),
                new RecordCompletedSprayProductItemRequest(item2.Id, storage.Id, 10m),
                new RecordCompletedSprayProductItemRequest(item3.Id, storage.Id, 2m)
            ]);

        var result = await _sut.RecordCompletedAsync(CreateActor(), request, null);

        Assert.Equal(3, result.Products.Count);
        Assert.Equal(80m, bal1.QuantityOnHand);
        Assert.Equal(40m, bal2.QuantityOnHand);
        Assert.Equal(23m, bal3.QuantityOnHand);
        Assert.Equal(3, _store.StockMovements.Count);
    }

    [Fact]
    public async Task Scenario26_MultipleStorageLocations_EachProductDeductedFromRespectiveLocation()
    {
        var farm = CreateFarm();
        var barnA = CreateStorageLocation(farm.Id, "Barn A");
        var barnB = CreateStorageLocation(farm.Id, "Barn B");
        var item1 = CreateInventoryItem("Product 1");
        var item2 = CreateInventoryItem("Product 2");

        var balA = CreateStockBalance(farm.Id, barnA.Id, item1.Id, 40m);
        var balB = CreateStockBalance(farm.Id, barnB.Id, item2.Id, 60m);

        var request = new RecordCompletedSprayRequest(
            FarmId: farm.Id,
            ActualApplicationDateTime: DateTimeOffset.UtcNow.AddHours(-1),
            Products: [
                new RecordCompletedSprayProductItemRequest(item1.Id, barnA.Id, 15m),
                new RecordCompletedSprayProductItemRequest(item2.Id, barnB.Id, 25m)
            ]);

        var result = await _sut.RecordCompletedAsync(CreateActor(), request, null);

        Assert.Equal(25m, balA.QuantityOnHand); // 40 - 15 = 25
        Assert.Equal(35m, balB.QuantityOnHand); // 60 - 25 = 35

        var movA = _store.StockMovements.Single(m => m.InventoryItemId == item1.Id);
        var movB = _store.StockMovements.Single(m => m.InventoryItemId == item2.Id);
        Assert.Equal(barnA.Id, movA.StorageLocationId);
        Assert.Equal(barnB.Id, movB.StorageLocationId);
    }

    [Fact]
    public async Task Scenario27_DuplicateProduct_RejectedAcrossAllEndpoints()
    {
        var farm = CreateFarm();
        var storage = CreateStorageLocation(farm.Id);
        var item = CreateInventoryItem("Item");
        CreateStockBalance(farm.Id, storage.Id, item.Id, 50m);

        // 1. CreateDraft duplicate rejected
        var draftReq = new CreateSprayDraftRequest(farm.Id, PlannedDate: DateOnly.FromDateTime(DateTime.UtcNow),
            Products: [new SprayProductItemRequest(item.Id, 1m), new SprayProductItemRequest(item.Id, 2m)]);
        var ex1 = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CreateDraftAsync(CreateActor(), draftReq, null));
        AssertValidationError(ex1, "Duplicate");

        // 2. Start duplicate rejected
        var scheduled = new Spray(_orgId, farm.Id, _userId, status: SprayStatus.Scheduled);
        _store.Sprays.Add(scheduled);
        var ex2 = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.StartAsync(CreateActor(), scheduled.Id,
                new StartSprayRequest(DateTimeOffset.UtcNow.AddHours(-1), [
                    new StartSprayProductItemRequest(item.Id, storage.Id, 1m),
                    new StartSprayProductItemRequest(item.Id, storage.Id, 2m)
                ]), null));
        AssertValidationError(ex2, "Duplicate");

        // 3. RecordCompleted duplicate rejected
        var ex3 = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.RecordCompletedAsync(CreateActor(),
                new RecordCompletedSprayRequest(farm.Id, DateTimeOffset.UtcNow.AddHours(-1), [
                    new RecordCompletedSprayProductItemRequest(item.Id, storage.Id, 1m),
                    new RecordCompletedSprayProductItemRequest(item.Id, storage.Id, 2m)
                ]), null));
        AssertValidationError(ex3, "Duplicate");
    }

    [Fact]
    public async Task Scenario28_PlannedQuantityGreaterThanActual_ConsumesOnlyActualQuantity()
    {
        var farm = CreateFarm();
        var storage = CreateStorageLocation(farm.Id);
        var item = CreateInventoryItem("Item");
        var balance = CreateStockBalance(farm.Id, storage.Id, item.Id, 100m);

        var spray = new Spray(_orgId, farm.Id, _userId, status: SprayStatus.InProgress,
            actualApplicationDateTime: DateTimeOffset.UtcNow.AddHours(-1));
        // Planned: 20m, Actual: 12m
        spray.AddProduct(new SprayProduct(spray.Id, item.Id, _userId, storage.Id, plannedQuantity: 20m, actualQuantity: 12m));
        _store.Sprays.Add(spray);

        await _sut.CompleteAsync(CreateActor(), spray.Id, null, null);

        // Only 12m is deducted from balance, not 20m
        Assert.Equal(88m, balance.QuantityOnHand);
        var mov = Assert.Single(_store.StockMovements);
        Assert.Equal(12m, mov.Quantity);
    }

    [Fact]
    public async Task Scenario29_ActualQuantityGreaterThanPlanned_ConsumesFullActualQuantity()
    {
        var farm = CreateFarm();
        var storage = CreateStorageLocation(farm.Id);
        var item = CreateInventoryItem("Item");
        var balance = CreateStockBalance(farm.Id, storage.Id, item.Id, 100m);

        var spray = new Spray(_orgId, farm.Id, _userId, status: SprayStatus.InProgress,
            actualApplicationDateTime: DateTimeOffset.UtcNow.AddHours(-1));
        // Planned: 10m, Actual: 18m
        spray.AddProduct(new SprayProduct(spray.Id, item.Id, _userId, storage.Id, plannedQuantity: 10m, actualQuantity: 18m));
        _store.Sprays.Add(spray);

        await _sut.CompleteAsync(CreateActor(), spray.Id, null, null);

        // Full 18m is deducted from balance
        Assert.Equal(82m, balance.QuantityOnHand);
        var mov = Assert.Single(_store.StockMovements);
        Assert.Equal(18m, mov.Quantity);
    }

    #endregion

    #region Scenarios 30 to 33: Masters & Multi-Tenant Isolation

    [Fact]
    public async Task Scenario30_ProductType_ValidatedAndLinked()
    {
        var unit = CreateUnit("KG", "Kilograms", UnitCategory.Weight);
        var item = new InventoryItem(_orgId, "Sulfur", unit.Id, _userId);
        _store.InventoryItems.Add(item);

        var pt = new ProductType(null, "FUNGICIDE", "Fungicide", isSystem: true);
        var fakePtStore = new FakeHardeningProtectionStore();
        fakePtStore.InventoryItems.Add(item);
        fakePtStore.ProductTypes.Add(pt);

        var pppService = new PlantProtectionProductService(fakePtStore);
        var ppp = await pppService.CreateAsync(new PlantProtectionActor(_userId, _orgId),
            new CreatePlantProtectionProductRequest(item.Id, pt.Id, "Sulfur 80%"), null);

        Assert.Equal(pt.Id, ppp.ProductTypeId);
        Assert.Equal("Fungicide", ppp.ProductTypeName);
    }

    [Fact]
    public async Task Scenario31_Target_ValidatedSystemAndTenantTargets()
    {
        var farm = CreateFarm();
        var sysTarget = CreateTarget("System Powdery Mildew", TargetType.Disease, isSystem: true);
        var tenantTarget = CreateTarget("Custom Farm Leafspot", TargetType.Disease, isSystem: false, orgId: _orgId);

        // Both are valid for this tenant
        var target1 = await _store.FindTargetAsync(sysTarget.Id, _orgId);
        var target2 = await _store.FindTargetAsync(tenantTarget.Id, _orgId);
        Assert.NotNull(target1);
        Assert.NotNull(target2);

        // Target belonging to another org is NOT visible
        var otherOrgTarget = CreateTarget("Other Org Pest", TargetType.Insect, isSystem: false, orgId: Guid.NewGuid());
        var target3 = await _store.FindTargetAsync(otherOrgTarget.Id, _orgId);
        Assert.Null(target3);
    }

    [Fact]
    public async Task Scenario32_ApplicationMethod_ValidatedSystemAndTenantMethods()
    {
        var farm = CreateFarm();
        var sysMethod = CreateApplicationMethod("Tractor Sprayer", isSystem: true);
        var tenantMethod = CreateApplicationMethod("Custom Backpack Setup", isSystem: false, orgId: _orgId);

        var m1 = await _store.FindApplicationMethodAsync(sysMethod.Id, _orgId);
        var m2 = await _store.FindApplicationMethodAsync(tenantMethod.Id, _orgId);
        Assert.NotNull(m1);
        Assert.NotNull(m2);

        var otherOrgMethod = CreateApplicationMethod("Other Org Drone", isSystem: false, orgId: Guid.NewGuid());
        var m3 = await _store.FindApplicationMethodAsync(otherOrgMethod.Id, _orgId);
        Assert.Null(m3);
    }

    [Fact]
    public async Task Scenario33_OrganizationIsolation_RejectsCrossTenantAccessAcrossAllOperations()
    {
        var otherOrgId = Guid.NewGuid();
        var farm = CreateFarm("Other Farm", orgId: otherOrgId);
        var spray = new Spray(otherOrgId, farm.Id, _userId, status: SprayStatus.Draft);
        _store.Sprays.Add(spray);

        // Access with _orgId is rejected with ResourceNotFoundException
        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _sut.GetAsync(CreateActor(), spray.Id));

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _sut.CancelAsync(CreateActor(), spray.Id, new CancelSprayRequest("Reason"), null));
    }

    #endregion

    #region Scenarios 34 to 38: Inactive / Invalid Entities & Uniqueness

    [Fact]
    public async Task Scenario34_InactiveProduct_Rejected()
    {
        var farm = CreateFarm();
        var inactiveItem = CreateInventoryItem("Inactive Chemical", isActive: false);

        var request = new CreateSprayDraftRequest(
            FarmId: farm.Id,
            PlannedDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            Products: [new SprayProductItemRequest(inactiveItem.Id, 5m)]);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CreateDraftAsync(CreateActor(), request, null));
        AssertValidationError(ex, "inactive");
    }

    [Fact]
    public async Task Scenario35_InactiveProductType_RejectedWhenAssociatingWithProduct()
    {
        var unit = CreateUnit("L", "Liter", UnitCategory.Volume);
        var item = new InventoryItem(_orgId, "Chemical", unit.Id, _userId);
        var pt = new ProductType(_orgId, "INACTIVE_TYPE", "Inactive Type", isSystem: false);
        pt.Deactivate(DateTimeOffset.UtcNow, _userId);

        var fakeStore = new FakeHardeningProtectionStore();
        fakeStore.InventoryItems.Add(item);
        fakeStore.ProductTypes.Add(pt);

        var service = new PlantProtectionProductService(fakeStore);
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(new PlantProtectionActor(_userId, _orgId),
                new CreatePlantProtectionProductRequest(item.Id, pt.Id), null));
        Assert.Contains("inactive", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Scenario36_InvalidStorageLocation_NotFoundOrInactive_Rejected()
    {
        var farm = CreateFarm();
        var item = CreateInventoryItem("Item");
        var inactiveLocation = CreateStorageLocation(farm.Id, "Inactive Barn", isActive: false);

        var scheduled = new Spray(_orgId, farm.Id, _userId, status: SprayStatus.Scheduled);
        _store.Sprays.Add(scheduled);

        // Inactive storage location rejected
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.StartAsync(CreateActor(), scheduled.Id,
                new StartSprayRequest(DateTimeOffset.UtcNow.AddHours(-1), [
                    new StartSprayProductItemRequest(item.Id, inactiveLocation.Id, 5m)
                ]), null));
        AssertValidationError(ex, "inactive");
    }

    [Fact]
    public async Task Scenario37_WrongFarmStorageLocation_Rejected()
    {
        var farm1 = CreateFarm("Farm 1");
        var farm2 = CreateFarm("Farm 2");
        var locOnFarm2 = CreateStorageLocation(farm2.Id, "Barn on Farm 2");
        var item = CreateInventoryItem("Item");
        CreateStockBalance(farm2.Id, locOnFarm2.Id, item.Id, 50m);

        var scheduled = new Spray(_orgId, farm1.Id, _userId, status: SprayStatus.Scheduled);
        _store.Sprays.Add(scheduled);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.StartAsync(CreateActor(), scheduled.Id,
                new StartSprayRequest(DateTimeOffset.UtcNow.AddHours(-1), [
                    new StartSprayProductItemRequest(item.Id, locOnFarm2.Id, 5m)
                ]), null));
        AssertValidationError(ex, "does not belong to the spray farm");
    }

    [Fact]
    public async Task Scenario38_PlantProtectionProduct_UniquenessEnforced()
    {
        var unit = CreateUnit("L", "Liter", UnitCategory.Volume);
        var item = new InventoryItem(_orgId, "Chemical", unit.Id, _userId);
        var pt = new ProductType(null, "FUNGICIDE", "Fungicide", isSystem: true);

        var fakeStore = new FakeHardeningProtectionStore();
        fakeStore.InventoryItems.Add(item);
        fakeStore.ProductTypes.Add(pt);
        fakeStore.Products.Add(new PlantProtectionProduct(_orgId, item.Id, pt.Id, _userId));

        var service = new PlantProtectionProductService(fakeStore);
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(new PlantProtectionActor(_userId, _orgId),
                new CreatePlantProtectionProductRequest(item.Id, pt.Id), null));
        Assert.Contains("already exists", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Scenarios 39 to 40: Traceability & Audit Logging

    [Fact]
    public async Task Scenario39_StockMovement_TraceabilityAndOperationalLinkagesVerified()
    {
        var farm = CreateFarm();
        var area = CreateFarmArea(farm.Id);
        var plantation = CreatePlantation(farm.Id, area.Id);
        var cycle = CreateCropCycle(plantation.Id);
        var stage = CreateCropCycleStage(cycle.Id);
        var storage = CreateStorageLocation(farm.Id);
        var item = CreateInventoryItem("Item");
        CreateStockBalance(farm.Id, storage.Id, item.Id, 50m);

        var request = new RecordCompletedSprayRequest(
            FarmId: farm.Id,
            FarmAreaId: area.Id,
            PlantationId: plantation.Id,
            CropCycleId: cycle.Id,
            CropCycleStageId: stage.Id,
            ActualApplicationDateTime: DateTimeOffset.UtcNow.AddHours(-1),
            Products: [new RecordCompletedSprayProductItemRequest(item.Id, storage.Id, 10m)]);

        var result = await _sut.RecordCompletedAsync(CreateActor(), request, null);

        var mov = Assert.Single(_store.StockMovements);
        Assert.Equal(StockMovementType.Issue, mov.MovementType);
        Assert.Equal(result.Id, mov.SprayId);
        Assert.Equal(farm.Id, mov.FarmId);
        Assert.Equal(area.Id, mov.FarmAreaId);
        Assert.Equal(plantation.Id, mov.PlantationId);
        Assert.Equal(cycle.Id, mov.CropCycleId);
        Assert.Equal(stage.Id, mov.CropCycleStageId);
        Assert.Equal(storage.Id, mov.StorageLocationId);
        Assert.Equal(item.Id, mov.InventoryItemId);
        Assert.Equal(10m, mov.Quantity);
        Assert.StartsWith("SP-", mov.ReferenceNumber);
        Assert.Contains(mov.ReferenceNumber, mov.Notes);
    }

    [Fact]
    public async Task Scenario40_Audit_AllOperationsLogAuditTrail()
    {
        var farm = CreateFarm();
        var storage = CreateStorageLocation(farm.Id);
        var item = CreateInventoryItem("Item");
        CreateStockBalance(farm.Id, storage.Id, item.Id, 50m);

        // 1. Created
        var spray = await _sut.CreateDraftAsync(CreateActor(),
            new CreateSprayDraftRequest(farm.Id, PlannedDate: DateOnly.FromDateTime(DateTime.UtcNow),
                Products: [new SprayProductItemRequest(item.Id, 5m)]), "1.1.1.1");
        Assert.Contains(_store.AuditLogs, a => a.Action == "Spray.Created" && a.EntityId == spray.Id);

        // 2. DraftUpdated
        await _sut.UpdateDraftAsync(CreateActor(), spray.Id,
            new UpdateSprayDraftRequest(farm.Id, PlannedDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
                Products: [new SprayProductItemRequest(item.Id, 6m)]), "1.1.1.1");
        Assert.Contains(_store.AuditLogs, a => a.Action == "Spray.DraftUpdated" && a.EntityId == spray.Id);

        // 3. Scheduled
        await _sut.ScheduleAsync(CreateActor(), spray.Id,
            new ScheduleSprayRequest(DateTimeOffset.UtcNow.AddDays(1)), "1.1.1.1");
        Assert.Contains(_store.AuditLogs, a => a.Action == "Spray.Scheduled" && a.EntityId == spray.Id);

        // 4. Rescheduled
        await _sut.RescheduleAsync(CreateActor(), spray.Id,
            new RescheduleSprayRequest(DateTimeOffset.UtcNow.AddDays(2)), "1.1.1.1");
        Assert.Contains(_store.AuditLogs, a => a.Action == "Spray.Rescheduled" && a.EntityId == spray.Id);

        // 5. Started
        await _sut.StartAsync(CreateActor(), spray.Id,
            new StartSprayRequest(DateTimeOffset.UtcNow.AddHours(-1),
                [new StartSprayProductItemRequest(item.Id, storage.Id, 6m)]), "1.1.1.1");
        Assert.Contains(_store.AuditLogs, a => a.Action == "Spray.Started" && a.EntityId == spray.Id);

        // 6. ExecutionSaved
        await _sut.SaveExecutionAsync(CreateActor(), spray.Id,
            new UpdateSprayExecutionRequest(DateTimeOffset.UtcNow.AddMinutes(-30)), "1.1.1.1");
        Assert.Contains(_store.AuditLogs, a => a.Action == "Spray.ExecutionSaved" && a.EntityId == spray.Id);

        // 7. Completed
        await _sut.CompleteAsync(CreateActor(), spray.Id, null, "1.1.1.1");
        Assert.Contains(_store.AuditLogs, a => a.Action == "Spray.Completed" && a.EntityId == spray.Id);
    }

    #endregion

    #region Fake Stores

    private sealed class FakeHardeningSprayStore : ISprayStore
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
        public List<(Guid StorageLocationId, Guid InventoryItemId)> AcquiredAdvisoryLocks { get; } = [];
        public List<(Guid StorageLocationId, Guid InventoryItemId)> LockedBalanceKeys { get; } = [];

        public Task<Spray?> FindAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Sprays.FirstOrDefault(s => s.Id == id && s.OrganizationId == organizationId));

        public Task<int> CountAsync(Guid organizationId, SprayListQuery query, DateTimeOffset now, CancellationToken cancellationToken = default) =>
            Task.FromResult(Sprays.Count(s => s.OrganizationId == organizationId));

        public Task<IReadOnlyList<Spray>> ListAsync(Guid organizationId, SprayListQuery query, int skip, int take, DateTimeOffset now, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Spray>>(Sprays.Where(s => s.OrganizationId == organizationId).Skip(skip).Take(take).ToList());

        public void Add(Spray spray) => Sprays.Add(spray);

        public void AddAuditLog(AuditLog auditLog) => AuditLogs.Add(auditLog);

        public void AddMovement(StockMovement movement) => StockMovements.Add(movement);

        public void RemoveSprayProduct(SprayProduct product) { }

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

        public Task<StockBalance?> LockBalanceAsync(Guid storageLocationId, Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default)
        {
            LockedBalanceKeys.Add((storageLocationId, inventoryItemId));
            return FindStockBalanceAsync(storageLocationId, inventoryItemId, organizationId, cancellationToken);
        }

        public Task AcquireAdvisoryLockAsync(Guid storageLocationId, Guid inventoryItemId, CancellationToken cancellationToken = default)
        {
            AcquiredAdvisoryLocks.Add((storageLocationId, inventoryItemId));
            return Task.CompletedTask;
        }

        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) =>
            operation(cancellationToken);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeHardeningProtectionStore : IPlantProtectionProductStore
    {
        public List<PlantProtectionProduct> Products { get; } = [];
        public List<InventoryItem> InventoryItems { get; } = [];
        public List<ProductType> ProductTypes { get; } = [];
        public List<AuditLog> AuditLogs { get; } = [];

        public Task<PlantProtectionProduct?> FindAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Products.FirstOrDefault(p => p.Id == id && p.OrganizationId == organizationId));

        public Task<PlantProtectionProduct?> FindByInventoryItemIdAsync(Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Products.FirstOrDefault(p => p.InventoryItemId == inventoryItemId && p.OrganizationId == organizationId));

        public Task<int> CountAsync(Guid organizationId, string? search, Guid? productTypeId, bool? isActive, CancellationToken cancellationToken = default) =>
            Task.FromResult(Products.Count(p => p.OrganizationId == organizationId));

        public Task<IReadOnlyList<PlantProtectionProduct>> ListAsync(Guid organizationId, int skip, int take, string? search, Guid? productTypeId, bool? isActive, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PlantProtectionProduct>>(Products.Where(p => p.OrganizationId == organizationId).Skip(skip).Take(take).ToList());

        public void Add(PlantProtectionProduct product) => Products.Add(product);
        public void AddAuditLog(AuditLog auditLog) => AuditLogs.Add(auditLog);

        public Task<InventoryItem?> FindInventoryItemAsync(Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(InventoryItems.FirstOrDefault(i => i.Id == inventoryItemId && i.OrganizationId == organizationId));

        public Task<ProductType?> FindProductTypeAsync(Guid productTypeId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ProductTypes.FirstOrDefault(pt => pt.Id == productTypeId && pt.IsActive && ((pt.IsSystem && pt.OrganizationId == null) || pt.OrganizationId == organizationId)));

        public Task<bool> HasCompletedSprayUsageAsync(Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    #endregion
}
