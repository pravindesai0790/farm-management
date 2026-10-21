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

public class FertilizerLifecycleTransitionTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly FakeFertilizerStore _store = new();
    private readonly FertilizerApplicationService _sut;
    private readonly TestReferenceGenerator _refGen = new();

    public FertilizerLifecycleTransitionTests()
    {
        var validator = new FertilizerValidator(_store);
        var stockManager = new FertilizerStockManager(_store);
        _sut = new FertilizerApplicationService(_store, validator, _refGen, stockManager);
    }

    private FertilizerActor CreateActor(Guid? orgId = null) =>
        new(_userId, orgId ?? _orgId);

    // =========================================================================
    // PLAN TESTS
    // =========================================================================

    [Fact]
    public async Task PlanAsync_FromDraft_WithAllRequiredFields_TransitionsToPlanned()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, plantation, cycle, stage) = CreateValidHierarchyChain();
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var location = CreateStorageLocation(farm.Id);

        var draft = new FertilizerApplication(
            _orgId, "FERT-2026-000001", _userId,
            farmId: farm.Id, farmAreaId: area.Id, plantationId: plantation.Id,
            cropCycleId: cycle.Id, cropCycleStageId: stage.Id,
            applicationMethodId: method.Id,
            plannedDate: DateTimeOffset.UtcNow.AddDays(1));
        draft.AddProduct(item.Id, 100m, _userId, location.Id);
        _store.Applications.Add(draft);

        // Act
        var result = await _sut.PlanAsync(actor, draft.Id, new PlanFertilizerApplicationRequest(), "127.0.0.1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(FertilizerApplicationStatus.Planned, result.Status);
        Assert.Equal("PLANNED", result.StatusName);
        Assert.Equal(3u, result.Version);

        // Audit log verified
        var audit = Assert.Single(_store.AuditLogs, a => a.Action == "Fertilizer.Planned");
        Assert.Equal(draft.Id, audit.EntityId);
        Assert.Equal(_userId, audit.UserId);

        // No stock movements/deductions created
        Assert.Empty(_store.StockBalances);
    }

    [Fact]
    public async Task PlanAsync_FromDraft_SupplyingFieldsInRequest_UpdatesAndTransitionsToPlanned()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, plantation, cycle, stage) = CreateValidHierarchyChain();
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var location = CreateStorageLocation(farm.Id);

        // Initially empty draft
        var draft = new FertilizerApplication(_orgId, "FERT-2026-000001", _userId);
        _store.Applications.Add(draft);

        var request = new PlanFertilizerApplicationRequest(
            PlannedDate: DateTimeOffset.UtcNow.AddDays(2),
            FarmId: farm.Id,
            FarmAreaId: area.Id,
            PlantationId: plantation.Id,
            CropCycleId: cycle.Id,
            CropCycleStageId: stage.Id,
            ApplicationMethodId: method.Id,
            Products:
            [
                new FertilizerDraftProductLineRequest(item.Id, 200m, location.Id)
            ]);

        // Act
        var result = await _sut.PlanAsync(actor, draft.Id, request, "127.0.0.1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(FertilizerApplicationStatus.Planned, result.Status);
        Assert.Equal(farm.Id, result.FarmId);
        Assert.Equal(area.Id, result.FarmAreaId);
        Assert.Single(result.Products);
        Assert.Equal(200m, result.Products[0].PlannedQuantity);
    }

    [Fact]
    public async Task PlanAsync_MissingFarm_ThrowsValidationException()
    {
        // Arrange
        var actor = CreateActor();
        var draft = new FertilizerApplication(_orgId, "FERT-2026-000001", _userId);
        _store.Applications.Add(draft);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.PlanAsync(actor, draft.Id, new PlanFertilizerApplicationRequest(), null));

        Assert.True(ex.Errors.ContainsKey("farmId"));
    }

    [Fact]
    public async Task PlanAsync_MissingFarmArea_ThrowsValidationException()
    {
        // Arrange
        var actor = CreateActor();
        var farm = CreateFarm();
        var draft = new FertilizerApplication(_orgId, "FERT-2026-000001", _userId, farmId: farm.Id);
        _store.Applications.Add(draft);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.PlanAsync(actor, draft.Id, new PlanFertilizerApplicationRequest(), null));

        Assert.True(ex.Errors.ContainsKey("farmAreaId"));
    }

    [Fact]
    public async Task PlanAsync_MissingApplicationMethod_ThrowsValidationException()
    {
        // Arrange
        var actor = CreateActor();
        var farm = CreateFarm();
        var area = CreateFarmArea(farm.Id);
        var draft = new FertilizerApplication(_orgId, "FERT-2026-000001", _userId, farmId: farm.Id, farmAreaId: area.Id);
        _store.Applications.Add(draft);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.PlanAsync(actor, draft.Id, new PlanFertilizerApplicationRequest(), null));

        Assert.True(ex.Errors.ContainsKey("applicationMethodId"));
    }

    [Fact]
    public async Task PlanAsync_MethodOtherWithoutDescription_ThrowsValidationException()
    {
        // Arrange
        var actor = CreateActor();
        var farm = CreateFarm();
        var area = CreateFarmArea(farm.Id);
        var otherMethod = CreateMethod("OTHER", "Other Method");
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var location = CreateStorageLocation(farm.Id);

        var draft = new FertilizerApplication(
            _orgId, "FERT-2026-000001", _userId,
            farmId: farm.Id, farmAreaId: area.Id, applicationMethodId: otherMethod.Id,
            plannedDate: DateTimeOffset.UtcNow.AddDays(1));
        draft.AddProduct(item.Id, 50m, _userId, location.Id);
        _store.Applications.Add(draft);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.PlanAsync(actor, draft.Id, new PlanFertilizerApplicationRequest(), null));

        Assert.True(ex.Errors.ContainsKey("otherMethodDescription"));
    }

    [Fact]
    public async Task PlanAsync_PlannedDateInPast_ThrowsValidationException()
    {
        // Arrange
        var actor = CreateActor();
        var farm = CreateFarm();
        var area = CreateFarmArea(farm.Id);
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var location = CreateStorageLocation(farm.Id);

        var pastDate = DateTimeOffset.UtcNow.AddDays(-2);
        var draft = new FertilizerApplication(
            _orgId, "FERT-2026-000001", _userId,
            farmId: farm.Id, farmAreaId: area.Id, applicationMethodId: method.Id,
            plannedDate: pastDate);
        draft.AddProduct(item.Id, 50m, _userId, location.Id);
        _store.Applications.Add(draft);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.PlanAsync(actor, draft.Id, new PlanFertilizerApplicationRequest(), null));

        Assert.True(ex.Errors.ContainsKey("plannedDate"));
    }

    [Fact]
    public async Task PlanAsync_NoProducts_ThrowsValidationException()
    {
        // Arrange
        var actor = CreateActor();
        var farm = CreateFarm();
        var area = CreateFarmArea(farm.Id);
        var method = CreateMethod("BROADCASTING", "Broadcasting");

        var draft = new FertilizerApplication(
            _orgId, "FERT-2026-000001", _userId,
            farmId: farm.Id, farmAreaId: area.Id, applicationMethodId: method.Id,
            plannedDate: DateTimeOffset.UtcNow.AddDays(1));
        _store.Applications.Add(draft);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.PlanAsync(actor, draft.Id, new PlanFertilizerApplicationRequest(), null));

        Assert.True(ex.Errors.ContainsKey("products"));
    }

    [Fact]
    public async Task PlanAsync_ProductWithZeroQuantity_ThrowsValidationException()
    {
        // Arrange
        var actor = CreateActor();
        var farm = CreateFarm();
        var area = CreateFarmArea(farm.Id);
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var location = CreateStorageLocation(farm.Id);

        var draft = new FertilizerApplication(
            _orgId, "FERT-2026-000001", _userId,
            farmId: farm.Id, farmAreaId: area.Id, applicationMethodId: method.Id,
            plannedDate: DateTimeOffset.UtcNow.AddDays(1));
        draft.AddProduct(item.Id, 0m, _userId, location.Id);
        _store.Applications.Add(draft);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.PlanAsync(actor, draft.Id, new PlanFertilizerApplicationRequest(), null));

        Assert.True(ex.Errors.ContainsKey("products"));
    }

    [Fact]
    public async Task PlanAsync_ProductMissingStorageLocation_ThrowsValidationException()
    {
        // Arrange
        var actor = CreateActor();
        var farm = CreateFarm();
        var area = CreateFarmArea(farm.Id);
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);

        var draft = new FertilizerApplication(
            _orgId, "FERT-2026-000001", _userId,
            farmId: farm.Id, farmAreaId: area.Id, applicationMethodId: method.Id,
            plannedDate: DateTimeOffset.UtcNow.AddDays(1));
        draft.AddProduct(item.Id, 50m, _userId, storageLocationId: null);
        _store.Applications.Add(draft);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.PlanAsync(actor, draft.Id, new PlanFertilizerApplicationRequest(), null));

        Assert.True(ex.Errors.ContainsKey("storageLocationId"));
    }

    [Fact]
    public async Task PlanAsync_ProductWithForeignFarmLocation_ThrowsValidationException()
    {
        // Arrange
        var actor = CreateActor();
        var farmA = CreateFarm();
        var farmB = CreateFarm();
        var areaA = CreateFarmArea(farmA.Id);
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var locB = CreateStorageLocation(farmB.Id); // Belongs to farm B!

        var draft = new FertilizerApplication(
            _orgId, "FERT-2026-000001", _userId,
            farmId: farmA.Id, farmAreaId: areaA.Id, applicationMethodId: method.Id,
            plannedDate: DateTimeOffset.UtcNow.AddDays(1));
        draft.AddProduct(item.Id, 50m, _userId, locB.Id);
        _store.Applications.Add(draft);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.PlanAsync(actor, draft.Id, new PlanFertilizerApplicationRequest(), null));

        Assert.True(ex.Errors.ContainsKey("storageLocationId"));
    }

    [Fact]
    public async Task PlanAsync_WhenStatusNotDraft_ThrowsConflictException()
    {
        // Arrange
        var actor = CreateActor();
        var farm = CreateFarm();
        var area = CreateFarmArea(farm.Id);
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var location = CreateStorageLocation(farm.Id);

        var app = new FertilizerApplication(
            _orgId, "FERT-2026-000001", _userId,
            farmId: farm.Id, farmAreaId: area.Id, applicationMethodId: method.Id,
            plannedDate: DateTimeOffset.UtcNow.AddDays(1));
        app.AddProduct(item.Id, 50m, _userId, location.Id);
        SetPrivateProperty(app, nameof(FertilizerApplication.Status), FertilizerApplicationStatus.Planned);
        _store.Applications.Add(app);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.PlanAsync(actor, app.Id, new PlanFertilizerApplicationRequest(), null));

        Assert.Contains("Only draft fertilizer applications can be planned", ex.Message);
    }

    [Fact]
    public async Task PlanAsync_VersionMismatch_ThrowsConflictException()
    {
        // Arrange
        var actor = CreateActor();
        var farm = CreateFarm();
        var area = CreateFarmArea(farm.Id);
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var location = CreateStorageLocation(farm.Id);

        var draft = new FertilizerApplication(
            _orgId, "FERT-2026-000001", _userId,
            farmId: farm.Id, farmAreaId: area.Id, applicationMethodId: method.Id,
            plannedDate: DateTimeOffset.UtcNow.AddDays(1));
        draft.AddProduct(item.Id, 50m, _userId, location.Id);
        _store.Applications.Add(draft);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.PlanAsync(actor, draft.Id, new PlanFertilizerApplicationRequest(Version: 99), null));

        Assert.Contains("modified by another user", ex.Message);
    }

    // =========================================================================
    // SCHEDULE TESTS
    // =========================================================================

    [Fact]
    public async Task ScheduleAsync_FromPlanned_TransitionsToScheduled()
    {
        // Arrange
        var actor = CreateActor();
        var farm = CreateFarm();
        var area = CreateFarmArea(farm.Id);
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var location = CreateStorageLocation(farm.Id);

        var app = new FertilizerApplication(
            _orgId, "FERT-2026-000001", _userId,
            farmId: farm.Id, farmAreaId: area.Id, applicationMethodId: method.Id,
            plannedDate: DateTimeOffset.UtcNow.AddDays(1));
        app.AddProduct(item.Id, 50m, _userId, location.Id);
        SetPrivateProperty(app, nameof(FertilizerApplication.Status), FertilizerApplicationStatus.Planned);
        _store.Applications.Add(app);

        var scheduledDate = DateTimeOffset.UtcNow.AddDays(3);
        var request = new ScheduleFertilizerApplicationRequest(scheduledDate);

        // Act
        var result = await _sut.ScheduleAsync(actor, app.Id, request, "127.0.0.1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(FertilizerApplicationStatus.Scheduled, result.Status);
        Assert.Equal("SCHEDULED", result.StatusName);
        Assert.Equal(scheduledDate, result.ScheduledDate);

        var audit = Assert.Single(_store.AuditLogs, a => a.Action == "Fertilizer.Scheduled");
        Assert.Equal(app.Id, audit.EntityId);
    }

    [Fact]
    public async Task ScheduleAsync_FromDraft_WhenFullyValid_PlansAndSchedulesInOneGo()
    {
        // Arrange
        var actor = CreateActor();
        var farm = CreateFarm();
        var area = CreateFarmArea(farm.Id);
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var location = CreateStorageLocation(farm.Id);

        var draft = new FertilizerApplication(
            _orgId, "FERT-2026-000001", _userId,
            farmId: farm.Id, farmAreaId: area.Id, applicationMethodId: method.Id,
            plannedDate: DateTimeOffset.UtcNow.AddDays(2));
        draft.AddProduct(item.Id, 50m, _userId, location.Id);
        _store.Applications.Add(draft);

        var scheduledDate = DateTimeOffset.UtcNow.AddDays(2);
        var request = new ScheduleFertilizerApplicationRequest(scheduledDate);

        // Act
        var result = await _sut.ScheduleAsync(actor, draft.Id, request, "127.0.0.1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(FertilizerApplicationStatus.Scheduled, result.Status);
        Assert.Equal(scheduledDate, result.ScheduledDate);

        // Both Planned and Scheduled audits generated
        Assert.Contains(_store.AuditLogs, a => a.Action == "Fertilizer.Planned");
        Assert.Contains(_store.AuditLogs, a => a.Action == "Fertilizer.Scheduled");
    }

    [Fact]
    public async Task ScheduleAsync_PastDate_ThrowsValidationException()
    {
        // Arrange
        var actor = CreateActor();
        var farm = CreateFarm();
        var area = CreateFarmArea(farm.Id);
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var location = CreateStorageLocation(farm.Id);

        var app = new FertilizerApplication(
            _orgId, "FERT-2026-000001", _userId,
            farmId: farm.Id, farmAreaId: area.Id, applicationMethodId: method.Id,
            plannedDate: DateTimeOffset.UtcNow.AddDays(1));
        app.AddProduct(item.Id, 50m, _userId, location.Id);
        SetPrivateProperty(app, nameof(FertilizerApplication.Status), FertilizerApplicationStatus.Planned);
        _store.Applications.Add(app);

        var pastDate = DateTimeOffset.UtcNow.AddDays(-1);
        var request = new ScheduleFertilizerApplicationRequest(pastDate);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.ScheduleAsync(actor, app.Id, request, null));

        Assert.True(ex.Errors.ContainsKey("scheduledDate"));
    }

    [Fact]
    public async Task ScheduleAsync_FromInvalidStatus_ThrowsConflictException()
    {
        // Arrange
        var actor = CreateActor();
        var app = new FertilizerApplication(_orgId, "FERT-2026-000001", _userId);
        SetPrivateProperty(app, nameof(FertilizerApplication.Status), FertilizerApplicationStatus.InProgress);
        _store.Applications.Add(app);

        var request = new ScheduleFertilizerApplicationRequest(DateTimeOffset.UtcNow.AddDays(1));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.ScheduleAsync(actor, app.Id, request, null));

        Assert.Contains("Only draft or planned fertilizer applications can be scheduled", ex.Message);
    }

    // =========================================================================
    // RESCHEDULE TESTS
    // =========================================================================

    [Fact]
    public async Task RescheduleAsync_WhenPlannedOrScheduled_UpdatesScheduledDateAndAudits()
    {
        // Arrange
        var actor = CreateActor();
        var farm = CreateFarm();
        var area = CreateFarmArea(farm.Id);
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var location = CreateStorageLocation(farm.Id);

        var oldScheduledDate = DateTimeOffset.UtcNow.AddDays(2);
        var app = new FertilizerApplication(
            _orgId, "FERT-2026-000001", _userId,
            farmId: farm.Id, farmAreaId: area.Id, applicationMethodId: method.Id,
            plannedDate: DateTimeOffset.UtcNow.AddDays(1),
            scheduledDate: oldScheduledDate);
        app.AddProduct(item.Id, 50m, _userId, location.Id);
        SetPrivateProperty(app, nameof(FertilizerApplication.Status), FertilizerApplicationStatus.Scheduled);
        _store.Applications.Add(app);

        var newScheduledDate = DateTimeOffset.UtcNow.AddDays(5);
        var request = new RescheduleFertilizerApplicationRequest(newScheduledDate);

        // Act
        var result = await _sut.RescheduleAsync(actor, app.Id, request, "127.0.0.1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(newScheduledDate, result.ScheduledDate);

        var audit = Assert.Single(_store.AuditLogs, a => a.Action == "Fertilizer.Rescheduled");
        Assert.Equal(app.Id, audit.EntityId);
        var json = audit.Details?.RootElement.GetRawText() ?? string.Empty;
        Assert.Contains(oldScheduledDate.ToString("yyyy-MM-dd"), json);
        Assert.Contains(newScheduledDate.ToString("yyyy-MM-dd"), json);
    }

    [Fact]
    public async Task RescheduleAsync_PastScheduledDate_ThrowsValidationException()
    {
        // Arrange
        var actor = CreateActor();
        var farm = CreateFarm();
        var area = CreateFarmArea(farm.Id);
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var location = CreateStorageLocation(farm.Id);

        var app = new FertilizerApplication(
            _orgId, "FERT-2026-000001", _userId,
            farmId: farm.Id, farmAreaId: area.Id, applicationMethodId: method.Id,
            plannedDate: DateTimeOffset.UtcNow.AddDays(1),
            scheduledDate: DateTimeOffset.UtcNow.AddDays(2));
        app.AddProduct(item.Id, 50m, _userId, location.Id);
        SetPrivateProperty(app, nameof(FertilizerApplication.Status), FertilizerApplicationStatus.Scheduled);
        _store.Applications.Add(app);

        var pastDate = DateTimeOffset.UtcNow.AddDays(-1);
        var request = new RescheduleFertilizerApplicationRequest(pastDate);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.RescheduleAsync(actor, app.Id, request, null));

        Assert.True(ex.Errors.ContainsKey("scheduledDate"));
    }

    [Fact]
    public async Task RescheduleAsync_AfterStarted_ThrowsConflictException()
    {
        // Arrange
        var actor = CreateActor();
        var app = new FertilizerApplication(_orgId, "FERT-2026-000001", _userId);
        SetPrivateProperty(app, nameof(FertilizerApplication.Status), FertilizerApplicationStatus.InProgress);
        _store.Applications.Add(app);

        var request = new RescheduleFertilizerApplicationRequest(DateTimeOffset.UtcNow.AddDays(2));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.RescheduleAsync(actor, app.Id, request, null));

        Assert.Contains("Only planned or scheduled fertilizer applications can be rescheduled", ex.Message);
    }

    // =========================================================================
    // START TESTS & STOCK GATE CHECK
    // =========================================================================

    [Fact]
    public async Task StartAsync_AllStockSufficient_TransitionsToInProgressWithZeroDeduction()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, plantation, cycle, stage) = CreateValidHierarchyChain();
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var location = CreateStorageLocation(farm.Id);

        // Sufficient stock available in storage location
        var stock = new StockBalance(_orgId, farm.Id, location.Id, item.Id, initialQuantity: 500m);
        _store.StockBalances.Add(stock);

        var app = new FertilizerApplication(
            _orgId, "FERT-2026-000001", _userId,
            farmId: farm.Id, farmAreaId: area.Id, plantationId: plantation.Id,
            cropCycleId: cycle.Id, cropCycleStageId: stage.Id,
            applicationMethodId: method.Id,
            plannedDate: DateTimeOffset.UtcNow.AddDays(1));
        app.AddProduct(item.Id, 100m, _userId, location.Id);
        SetPrivateProperty(app, nameof(FertilizerApplication.Status), FertilizerApplicationStatus.Planned);
        _store.Applications.Add(app);

        // Act
        var result = await _sut.StartAsync(actor, app.Id, new StartFertilizerApplicationRequest(), "127.0.0.1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(FertilizerApplicationStatus.InProgress, result.Status);
        Assert.Equal("INPROGRESS", result.StatusName);
        Assert.NotNull(result.StartedAt);
        Assert.Equal(_userId, result.StartedBy);

        // Stock quantity on hand is unchanged (ZERO DEDUCTION on Start)
        Assert.Equal(500m, stock.QuantityOnHand);

        // Started audit log verified
        var audit = Assert.Single(_store.AuditLogs, a => a.Action == "Fertilizer.Started");
        Assert.Equal(app.Id, audit.EntityId);
    }

    [Fact]
    public async Task StartAsync_SingleProductShortage_ThrowsValidationExceptionWithShortageDetails_LeavesStateUnchanged()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, plantation, cycle, stage) = CreateValidHierarchyChain();
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var location = CreateStorageLocation(farm.Id);

        // Only 40kg available, but 100kg required
        var stock = new StockBalance(_orgId, farm.Id, location.Id, item.Id, initialQuantity: 40m);
        _store.StockBalances.Add(stock);

        var app = new FertilizerApplication(
            _orgId, "FERT-2026-000001", _userId,
            farmId: farm.Id, farmAreaId: area.Id, plantationId: plantation.Id,
            cropCycleId: cycle.Id, cropCycleStageId: stage.Id,
            applicationMethodId: method.Id,
            plannedDate: DateTimeOffset.UtcNow.AddDays(1));
        app.AddProduct(item.Id, 100m, _userId, location.Id);
        SetPrivateProperty(app, nameof(FertilizerApplication.Status), FertilizerApplicationStatus.Planned);
        _store.Applications.Add(app);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.StartAsync(actor, app.Id, new StartFertilizerApplicationRequest(), null));

        // State remains Planned
        Assert.Equal(FertilizerApplicationStatus.Planned, app.Status);

        // Stock unchanged
        Assert.Equal(40m, stock.QuantityOnHand);

        // Validation errors detail shortage
        Assert.True(ex.Errors.ContainsKey("products[0]"));
        var errorMsg = ex.Errors["products[0]"][0];
        Assert.Contains("Insufficient stock", errorMsg);
        Assert.Contains("Required: 100", errorMsg);
        Assert.Contains("Available: 40", errorMsg);
        Assert.Contains("Shortage: 60", errorMsg);
    }

    [Fact]
    public async Task StartAsync_MultiProductShortage_GathersAllShortagesInSingleResponse()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, plantation, cycle, stage) = CreateValidHierarchyChain();
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var item1 = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId, name: "Urea 46%");
        var item2 = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId, name: "Potassium Sulfate");
        var location = CreateStorageLocation(farm.Id);

        // item1: needs 100, has 30 (shortage 70)
        // item2: needs 50, has 10 (shortage 40)
        _store.StockBalances.Add(new StockBalance(_orgId, farm.Id, location.Id, item1.Id, initialQuantity: 30m));
        _store.StockBalances.Add(new StockBalance(_orgId, farm.Id, location.Id, item2.Id, initialQuantity: 10m));

        var app = new FertilizerApplication(
            _orgId, "FERT-2026-000001", _userId,
            farmId: farm.Id, farmAreaId: area.Id, plantationId: plantation.Id,
            cropCycleId: cycle.Id, cropCycleStageId: stage.Id,
            applicationMethodId: method.Id,
            plannedDate: DateTimeOffset.UtcNow.AddDays(1));
        app.AddProduct(item1.Id, 100m, _userId, location.Id);
        app.AddProduct(item2.Id, 50m, _userId, location.Id);
        SetPrivateProperty(app, nameof(FertilizerApplication.Status), FertilizerApplicationStatus.Planned);
        _store.Applications.Add(app);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.StartAsync(actor, app.Id, new StartFertilizerApplicationRequest(), null));

        // Both products are returned in errors dictionary
        Assert.True(ex.Errors.ContainsKey("products[0]"));
        Assert.True(ex.Errors.ContainsKey("products[1]"));
        Assert.Contains("Shortage: 70", ex.Errors["products[0]"][0]);
        Assert.Contains("Shortage: 40", ex.Errors["products[1]"][0]);

        // State remains Planned
        Assert.Equal(FertilizerApplicationStatus.Planned, app.Status);
    }

    [Fact]
    public async Task StartAsync_WhenDeactivatedHierarchy_ThrowsValidationException()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, plantation, cycle, stage) = CreateValidHierarchyChain();
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var location = CreateStorageLocation(farm.Id);

        _store.StockBalances.Add(new StockBalance(_orgId, farm.Id, location.Id, item.Id, initialQuantity: 100m));

        var app = new FertilizerApplication(
            _orgId, "FERT-2026-000001", _userId,
            farmId: farm.Id, farmAreaId: area.Id, plantationId: plantation.Id,
            cropCycleId: cycle.Id, cropCycleStageId: stage.Id,
            applicationMethodId: method.Id,
            plannedDate: DateTimeOffset.UtcNow.AddDays(1));
        app.AddProduct(item.Id, 50m, _userId, location.Id);
        SetPrivateProperty(app, nameof(FertilizerApplication.Status), FertilizerApplicationStatus.Planned);
        _store.Applications.Add(app);

        // Deactivate farm prior to start!
        farm.Deactivate(DateTimeOffset.UtcNow, _userId);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.StartAsync(actor, app.Id, new StartFertilizerApplicationRequest(), null));

        Assert.True(ex.Errors.ContainsKey("farmId"));
    }

    [Fact]
    public async Task StartAsync_WhenDeactivatedProduct_ThrowsValidationException()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, plantation, cycle, stage) = CreateValidHierarchyChain();
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var location = CreateStorageLocation(farm.Id);

        _store.StockBalances.Add(new StockBalance(_orgId, farm.Id, location.Id, item.Id, initialQuantity: 100m));

        var app = new FertilizerApplication(
            _orgId, "FERT-2026-000001", _userId,
            farmId: farm.Id, farmAreaId: area.Id, plantationId: plantation.Id,
            cropCycleId: cycle.Id, cropCycleStageId: stage.Id,
            applicationMethodId: method.Id,
            plannedDate: DateTimeOffset.UtcNow.AddDays(1));
        app.AddProduct(item.Id, 50m, _userId, location.Id);
        SetPrivateProperty(app, nameof(FertilizerApplication.Status), FertilizerApplicationStatus.Planned);
        _store.Applications.Add(app);

        // Deactivate item prior to start!
        item.Deactivate(DateTimeOffset.UtcNow, _userId);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.StartAsync(actor, app.Id, new StartFertilizerApplicationRequest(), null));

        Assert.True(ex.Errors.ContainsKey("products"));
    }

    [Fact]
    public async Task StartAsync_WhenDeactivatedStorageLocation_ThrowsValidationException()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, plantation, cycle, stage) = CreateValidHierarchyChain();
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var location = CreateStorageLocation(farm.Id);

        _store.StockBalances.Add(new StockBalance(_orgId, farm.Id, location.Id, item.Id, initialQuantity: 100m));

        var app = new FertilizerApplication(
            _orgId, "FERT-2026-000001", _userId,
            farmId: farm.Id, farmAreaId: area.Id, plantationId: plantation.Id,
            cropCycleId: cycle.Id, cropCycleStageId: stage.Id,
            applicationMethodId: method.Id,
            plannedDate: DateTimeOffset.UtcNow.AddDays(1));
        app.AddProduct(item.Id, 50m, _userId, location.Id);
        SetPrivateProperty(app, nameof(FertilizerApplication.Status), FertilizerApplicationStatus.Planned);
        _store.Applications.Add(app);

        // Deactivate location prior to start!
        location.Deactivate(DateTimeOffset.UtcNow, _userId);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.StartAsync(actor, app.Id, new StartFertilizerApplicationRequest(), null));

        Assert.True(ex.Errors.ContainsKey("storageLocationId"));
    }

    [Fact]
    public async Task StartAsync_WhenDraftOrCompleted_ThrowsConflictException()
    {
        // Arrange
        var actor = CreateActor();
        var draft = new FertilizerApplication(_orgId, "FERT-2026-000001", _userId);
        _store.Applications.Add(draft);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.StartAsync(actor, draft.Id, new StartFertilizerApplicationRequest(), null));

        Assert.Contains("Only planned or scheduled fertilizer applications can be started", ex.Message);
    }

    [Fact]
    public async Task StartAsync_VersionMismatch_ThrowsConflictException()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, plantation, cycle, stage) = CreateValidHierarchyChain();
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var location = CreateStorageLocation(farm.Id);

        var app = new FertilizerApplication(
            _orgId, "FERT-2026-000001", _userId,
            farmId: farm.Id, farmAreaId: area.Id, plantationId: plantation.Id,
            cropCycleId: cycle.Id, cropCycleStageId: stage.Id,
            applicationMethodId: method.Id,
            plannedDate: DateTimeOffset.UtcNow.AddDays(1));
        app.AddProduct(item.Id, 50m, _userId, location.Id);
        SetPrivateProperty(app, nameof(FertilizerApplication.Status), FertilizerApplicationStatus.Planned);
        _store.Applications.Add(app);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.StartAsync(actor, app.Id, new StartFertilizerApplicationRequest(Version: 99), null));

        Assert.Contains("modified by another user", ex.Message);
    }

    // =========================================================================
    // POST-START FREEZE RULES ENFORCEMENT
    // =========================================================================

    [Fact]
    public async Task UpdateDraftAsync_WhenStartedOrCompleted_ThrowsConflictException()
    {
        // Arrange
        var actor = CreateActor();
        var app = new FertilizerApplication(_orgId, "FERT-2026-000001", _userId);
        SetPrivateProperty(app, nameof(FertilizerApplication.Status), FertilizerApplicationStatus.InProgress);
        _store.Applications.Add(app);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.UpdateDraftAsync(actor, app.Id, new UpdateFertilizerDraftRequest(), null));

        Assert.Contains("Only draft fertilizer applications can be", ex.Message);
    }

    [Fact]
    public void Domain_EnsureNotFrozen_ThrowsInvalidOperationException_WhenInProgressOrCompleted()
    {
        // Arrange
        var app = new FertilizerApplication(_orgId, "FERT-2026-000001", _userId);
        SetPrivateProperty(app, nameof(FertilizerApplication.Status), FertilizerApplicationStatus.InProgress);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            app.AddProduct(Guid.NewGuid(), 10m, _userId));

        Assert.Contains("start or completion", ex.Message);
    }

    // =========================================================================
    // HELPER METHODS & TEST DATA
    // =========================================================================

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

    private InventoryItem CreateInventoryItem(Guid categoryId, string name = "Urea 46%", bool isActive = true)
    {
        var item = new InventoryItem(_orgId, name, Guid.NewGuid(), _userId, "UREA-46", null, categoryId);
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

        public Task<IReadOnlyList<FertilizerApplication>> ListForExportAsync(Guid organizationId, FertilizerApplicationListQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FertilizerApplication>>(Applications.Where(a => a.OrganizationId == organizationId).ToList());

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
