using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Domain;

public sealed class FertilizerApplicationModelTests
{
    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly Guid _farmId = Guid.NewGuid();
    private readonly Guid _farmAreaId = Guid.NewGuid();
    private readonly Guid _plantationId = Guid.NewGuid();
    private readonly Guid _cropCycleId = Guid.NewGuid();
    private readonly Guid _cropCycleStageId = Guid.NewGuid();
    private readonly Guid _methodId = Guid.NewGuid();
    private readonly Guid _areaUnitId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public void CreateEmptyDraft_WithMinimalValidFields_SetsDraftDefaults()
    {
        var app = FertilizerApplication.CreateEmptyDraft(
            _organizationId,
            "FA-2026-0001",
            _userId);

        Assert.NotEqual(Guid.Empty, app.Id);
        Assert.Equal(_organizationId, app.OrganizationId);
        Assert.Equal("FA-2026-0001", app.ReferenceNumber);
        Assert.Equal(FertilizerApplicationStatus.Draft, app.Status);
        Assert.Equal(_userId, app.CreatedBy);
        Assert.Null(app.FarmId);
        Assert.Null(app.FarmAreaId);
        Assert.Null(app.PlantationId);
        Assert.Null(app.CropCycleId);
        Assert.Null(app.CropCycleStageId);
        Assert.Null(app.PlannedDate);
        Assert.Null(app.ScheduledDate);
        Assert.Null(app.ActualApplicationDate);
        Assert.Empty(app.Products);
        Assert.Equal(1u, app.Version);
    }

    [Fact]
    public void CreateEmptyDraft_WithEmptyOrganizationId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            FertilizerApplication.CreateEmptyDraft(Guid.Empty, "FA-2026-0001", _userId));
    }

    [Fact]
    public void CreateEmptyDraft_WithEmptyCreatedBy_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            FertilizerApplication.CreateEmptyDraft(_organizationId, "FA-2026-0001", Guid.Empty));
    }

    [Fact]
    public void CreateEmptyDraft_WithEmptyReferenceNumber_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            FertilizerApplication.CreateEmptyDraft(_organizationId, "   ", _userId));
    }

    [Fact]
    public void UpdateDraft_WhenInDraft_UpdatesContextAndFields()
    {
        var app = FertilizerApplication.CreateEmptyDraft(_organizationId, "FA-2026-0001", _userId);
        var now = DateTimeOffset.UtcNow;
        var plannedDate = new DateOnly(2026, 11, 15);

        app.UpdateDraft(
            _farmId,
            _farmAreaId,
            _plantationId,
            _cropCycleId,
            _cropCycleStageId,
            _methodId,
            null,
            null,
            plannedDate,
            null,
            5.25m,
            _areaUnitId,
            "Soil enrichment",
            now,
            _userId);

        Assert.Equal(_farmId, app.FarmId);
        Assert.Equal(_farmAreaId, app.FarmAreaId);
        Assert.Equal(_plantationId, app.PlantationId);
        Assert.Equal(_cropCycleId, app.CropCycleId);
        Assert.Equal(_cropCycleStageId, app.CropCycleStageId);
        Assert.Equal(_methodId, app.ApplicationMethodId);
        Assert.Equal("Soil enrichment", app.PurposeReason);
        Assert.Equal(plannedDate, app.PlannedDate);
        Assert.Equal(5.25m, app.PlannedArea);
        Assert.Equal(_areaUnitId, app.PlannedAreaUnitId);
        Assert.Equal(now, app.UpdatedAt);
        Assert.Equal(_userId, app.UpdatedBy);
    }

    [Fact]
    public void Plan_WhenValid_TransitionsToPlanned()
    {
        var app = FertilizerApplication.CreateEmptyDraft(_organizationId, "FA-2026-0001", _userId);
        var plannedDate = new DateOnly(2026, 11, 15);
        var now = DateTimeOffset.UtcNow;

        app.UpdateDraft(
            _farmId,
            _farmAreaId,
            null,
            null,
            null,
            _methodId,
            null,
            null,
            plannedDate,
            null,
            10m,
            _areaUnitId,
            "Base dressing",
            now,
            _userId);

        app.AddProduct(Guid.NewGuid(), 50m, _userId);

        app.Plan(now, _userId);

        Assert.Equal(FertilizerApplicationStatus.Planned, app.Status);
    }

    [Fact]
    public void Plan_WithoutProducts_ThrowsInvalidOperationException()
    {
        var app = FertilizerApplication.CreateEmptyDraft(_organizationId, "FA-2026-0001", _userId);
        var now = DateTimeOffset.UtcNow;

        app.UpdateDraft(
            _farmId,
            _farmAreaId,
            null,
            null,
            null,
            _methodId,
            null,
            null,
            new DateOnly(2026, 11, 15),
            null,
            null,
            null,
            null,
            now,
            _userId);

        Assert.Throws<InvalidOperationException>(() => app.Plan(now, _userId));
    }

    [Fact]
    public void Plan_WithoutFarmId_ThrowsInvalidOperationException()
    {
        var app = FertilizerApplication.CreateEmptyDraft(_organizationId, "FA-2026-0001", _userId);
        app.AddProduct(Guid.NewGuid(), 25m, _userId);
        var now = DateTimeOffset.UtcNow;

        Assert.Throws<InvalidOperationException>(() => app.Plan(now, _userId));
    }

    [Fact]
    public void Schedule_WhenPlanned_TransitionsToScheduled()
    {
        var app = FertilizerApplication.CreateEmptyDraft(_organizationId, "FA-2026-0001", _userId);
        var plannedDate = new DateOnly(2026, 11, 15);
        var scheduledDate = new DateOnly(2026, 11, 16);
        var now = DateTimeOffset.UtcNow;

        app.UpdateDraft(_farmId, _farmAreaId, null, null, null, _methodId, null, null, plannedDate, null, null, null, null, now, _userId);
        app.AddProduct(Guid.NewGuid(), 20m, _userId);
        app.Plan(now, _userId);

        app.Schedule(scheduledDate, now, _userId);

        Assert.Equal(FertilizerApplicationStatus.Scheduled, app.Status);
        Assert.Equal(scheduledDate, app.ScheduledDate);
    }

    [Fact]
    public void Reschedule_WhenScheduled_UpdatesScheduledDate()
    {
        var app = FertilizerApplication.CreateEmptyDraft(_organizationId, "FA-2026-0001", _userId);
        var now = DateTimeOffset.UtcNow;
        app.UpdateDraft(_farmId, _farmAreaId, null, null, null, _methodId, null, null, new DateOnly(2026, 11, 15), null, null, null, null, now, _userId);
        app.AddProduct(Guid.NewGuid(), 20m, _userId);
        app.Plan(now, _userId);
        app.Schedule(new DateOnly(2026, 11, 16), now, _userId);

        var newDate = new DateOnly(2026, 11, 18);
        app.Reschedule(newDate, now.AddHours(1), _userId);

        Assert.Equal(newDate, app.ScheduledDate);
    }

    [Fact]
    public void Start_WhenScheduled_TransitionsToInProgress()
    {
        var app = FertilizerApplication.CreateEmptyDraft(_organizationId, "FA-2026-0001", _userId);
        var now = DateTimeOffset.UtcNow;
        app.UpdateDraft(_farmId, _farmAreaId, null, null, null, _methodId, null, null, new DateOnly(2026, 11, 15), null, null, null, null, now, _userId);
        var product = app.AddProduct(Guid.NewGuid(), 20m, _userId, Guid.NewGuid());
        app.Plan(now, _userId);
        app.Schedule(new DateOnly(2026, 11, 16), now, _userId);

        var startNow = now.AddHours(2);
        app.Start(startNow, _userId);

        Assert.Equal(FertilizerApplicationStatus.InProgress, app.Status);
        Assert.Equal(startNow, app.StartedAt);
        Assert.Equal(_userId, app.StartedBy);
    }

    [Fact]
    public void Start_WhenProductMissingStorageLocation_ThrowsInvalidOperationException()
    {
        var app = FertilizerApplication.CreateEmptyDraft(_organizationId, "FA-2026-0001", _userId);
        var now = DateTimeOffset.UtcNow;
        app.UpdateDraft(_farmId, _farmAreaId, null, null, null, _methodId, null, null, new DateOnly(2026, 11, 15), null, null, null, null, now, _userId);
        app.AddProduct(Guid.NewGuid(), 20m, _userId); // no storage location
        app.Plan(now, _userId);
        app.Schedule(new DateOnly(2026, 11, 16), now, _userId);

        Assert.Throws<InvalidOperationException>(() => app.Start(now.AddHours(2), _userId));
    }

    [Fact]
    public void Start_WhenNotInScheduledStatus_ThrowsInvalidOperationException()
    {
        var app = FertilizerApplication.CreateEmptyDraft(_organizationId, "FA-2026-0001", _userId);
        var now = DateTimeOffset.UtcNow;

        Assert.Throws<InvalidOperationException>(() => app.Start(now, _userId));
    }

    [Fact]
    public void Complete_WhenInProgressAndProductsAssigned_TransitionsToCompleted()
    {
        var app = FertilizerApplication.CreateEmptyDraft(_organizationId, "FA-2026-0001", _userId);
        var now = DateTimeOffset.UtcNow;
        var locationId = Guid.NewGuid();
        app.UpdateDraft(_farmId, _farmAreaId, null, null, null, _methodId, null, null, new DateOnly(2026, 11, 15), null, null, null, null, now, _userId);
        var product = app.AddProduct(Guid.NewGuid(), 20m, _userId, locationId);
        app.Plan(now, _userId);
        app.Schedule(new DateOnly(2026, 11, 16), now, _userId);
        app.Start(now.AddHours(1), _userId);

        product.RecordActual(18.5m, Guid.NewGuid(), now, _userId);

        var completionDate = new DateOnly(2026, 11, 16);
        var completionTime = now.AddHours(3);
        app.Complete(completionDate, 5.0m, _areaUnitId, completionTime, _userId);

        Assert.Equal(FertilizerApplicationStatus.Completed, app.Status);
        Assert.Equal(completionDate, app.ActualApplicationDate);
        Assert.Equal(5.0m, app.ActualArea);
        Assert.Equal(_areaUnitId, app.ActualAreaUnitId);
        Assert.Equal(completionTime, app.CompletedAt);
        Assert.Equal(_userId, app.CompletedBy);
    }

    [Fact]
    public void Complete_WhenProductMissingActualQuantity_ThrowsInvalidOperationException()
    {
        var app = FertilizerApplication.CreateEmptyDraft(_organizationId, "FA-2026-0001", _userId);
        var now = DateTimeOffset.UtcNow;
        var locationId = Guid.NewGuid();
        app.UpdateDraft(_farmId, _farmAreaId, null, null, null, _methodId, null, null, new DateOnly(2026, 11, 15), null, null, null, null, now, _userId);
        var product = app.AddProduct(Guid.NewGuid(), 20m, _userId, locationId);
        app.Plan(now, _userId);
        app.Schedule(new DateOnly(2026, 11, 16), now, _userId);
        app.Start(now.AddHours(1), _userId);

        // Actual NOT recorded

        Assert.Throws<InvalidOperationException>(() =>
            app.Complete(new DateOnly(2026, 11, 16), null, null, now.AddHours(3), _userId));
    }

    [Fact]
    public void Cancel_WhenValidStatus_TransitionsToCancelledWithReason()
    {
        var app = FertilizerApplication.CreateEmptyDraft(_organizationId, "FA-2026-0001", _userId);
        var now = DateTimeOffset.UtcNow;

        app.Cancel("Heavy rain forecasted", now, _userId);

        Assert.Equal(FertilizerApplicationStatus.Cancelled, app.Status);
        Assert.Equal("Heavy rain forecasted", app.CancellationReason);
        Assert.Equal(now, app.CancelledAt);
        Assert.Equal(_userId, app.CancelledBy);
    }

    [Fact]
    public void Cancel_WithoutReason_ThrowsArgumentException()
    {
        var app = FertilizerApplication.CreateEmptyDraft(_organizationId, "FA-2026-0001", _userId);
        var now = DateTimeOffset.UtcNow;

        Assert.Throws<ArgumentException>(() => app.Cancel("   ", now, _userId));
    }

    [Fact]
    public void RecordCompletedDirect_WithValidInputs_CreatesDirectCompletedApplication()
    {
        var appDate = new DateOnly(2026, 11, 14);

        var app = FertilizerApplication.RecordCompletedDirect(
            _organizationId,
            "FA-2026-0002",
            _farmId,
            _farmAreaId,
            _methodId,
            appDate,
            _userId,
            actualArea: 2.5m,
            actualAreaUnitId: _areaUnitId,
            purposeReason: "Emergency micronutrient spray",
            idempotencyKey: "IDEMP-12345");

        Assert.Equal(FertilizerApplicationStatus.Completed, app.Status);
        Assert.Equal("FA-2026-0002", app.ReferenceNumber);
        Assert.Equal(appDate, app.ActualApplicationDate);
        Assert.Null(app.PlannedDate);
        Assert.Equal(2.5m, app.ActualArea);
        Assert.Equal(_areaUnitId, app.ActualAreaUnitId);
        Assert.NotNull(app.CompletedAt);
        Assert.Equal(_userId, app.CompletedBy);
        Assert.Equal("IDEMP-12345", app.IdempotencyKey);
    }

    [Fact]
    public void AreaValidation_AreaWithoutUnit_ThrowsArgumentException()
    {
        var app = FertilizerApplication.CreateEmptyDraft(_organizationId, "FA-2026-0001", _userId);
        var now = DateTimeOffset.UtcNow;

        Assert.Throws<ArgumentException>(() =>
            app.UpdateDraft(_farmId, null, null, null, null, null, null, null, null, null, 10m, null, null, now, _userId));
    }
}
