using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Domain;

public sealed class SprayModelTests
{
    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly Guid _farmId = Guid.NewGuid();
    private readonly Guid _farmAreaId = Guid.NewGuid();
    private readonly Guid _plantationId = Guid.NewGuid();
    private readonly Guid _cropCycleId = Guid.NewGuid();
    private readonly Guid _cropCycleStageId = Guid.NewGuid();
    private readonly Guid _unitId = Guid.NewGuid();
    private readonly Guid _waterUnitId = Guid.NewGuid();
    private readonly Guid _targetId = Guid.NewGuid();
    private readonly Guid _methodId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public void Spray_Create_WithMinimalValidFields_SetsDefaults()
    {
        var spray = new Spray(
            _organizationId,
            _farmId,
            _userId);

        Assert.NotEqual(Guid.Empty, spray.Id);
        Assert.Equal(_organizationId, spray.OrganizationId);
        Assert.Equal(_farmId, spray.FarmId);
        Assert.Equal(_userId, spray.CreatedBy);
        Assert.Equal(SprayStatus.Draft, spray.Status);
        Assert.Null(spray.FarmAreaId);
        Assert.Null(spray.PlantationId);
        Assert.Null(spray.CropCycleId);
        Assert.Null(spray.CropCycleStageId);
        Assert.Null(spray.PlannedDate);
        Assert.Null(spray.ScheduledDateTime);
        Assert.Null(spray.ActualApplicationDateTime);
        Assert.Empty(spray.Products);
    }

    [Fact]
    public void Spray_Create_EmptyOrganizationId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new Spray(
            Guid.Empty,
            _farmId,
            _userId));
    }

    [Fact]
    public void Spray_Create_EmptyFarmId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new Spray(
            _organizationId,
            Guid.Empty,
            _userId));
    }

    [Fact]
    public void Spray_Create_AreaWithoutUnit_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new Spray(
            _organizationId,
            _farmId,
            _userId,
            plannedArea: 5.5m,
            plannedAreaUnitId: null));
    }

    [Fact]
    public void Spray_Create_WaterWithoutUnit_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new Spray(
            _organizationId,
            _farmId,
            _userId,
            waterQuantity: 100m,
            waterUnitId: null));
    }

    [Fact]
    public void Spray_Schedule_WhenDraft_TransitionsToScheduled()
    {
        var spray = new Spray(_organizationId, _farmId, _userId);
        var now = DateTimeOffset.UtcNow;
        var scheduled = now.AddDays(2);
        var plannedDate = DateOnly.FromDateTime(scheduled.DateTime);

        spray.Schedule(scheduled, plannedDate, now, _userId);

        Assert.Equal(SprayStatus.Scheduled, spray.Status);
        Assert.Equal(scheduled, spray.ScheduledDateTime);
        Assert.Equal(plannedDate, spray.PlannedDate);
        Assert.Equal(now, spray.UpdatedAt);
        Assert.Equal(_userId, spray.UpdatedBy);
    }

    [Fact]
    public void Spray_Schedule_WhenNotDraft_ThrowsInvalidOperationException()
    {
        var spray = new Spray(_organizationId, _farmId, _userId, status: SprayStatus.InProgress);
        var now = DateTimeOffset.UtcNow;

        Assert.Throws<InvalidOperationException>(() =>
            spray.Schedule(now.AddDays(1), DateOnly.FromDateTime(now.DateTime), now, _userId));
    }

    [Fact]
    public void Spray_Reschedule_WhenScheduled_UpdatesScheduledDateTime()
    {
        var now = DateTimeOffset.UtcNow;
        var spray = new Spray(_organizationId, _farmId, _userId, status: SprayStatus.Scheduled, scheduledDateTime: now.AddDays(1));
        var newScheduled = now.AddDays(3);

        spray.Reschedule(newScheduled, now, _userId);

        Assert.Equal(SprayStatus.Scheduled, spray.Status);
        Assert.Equal(newScheduled, spray.ScheduledDateTime);
    }

    [Fact]
    public void Spray_Start_WhenScheduled_TransitionsToInProgress()
    {
        var now = DateTimeOffset.UtcNow;
        var spray = new Spray(_organizationId, _farmId, _userId, status: SprayStatus.Scheduled, scheduledDateTime: now.AddDays(1));

        spray.Start(now, now, _userId);

        Assert.Equal(SprayStatus.InProgress, spray.Status);
        Assert.Equal(now, spray.ActualApplicationDateTime);
    }

    [Fact]
    public void Spray_Start_FutureDateTime_ThrowsInvalidOperationException()
    {
        var now = DateTimeOffset.UtcNow;
        var spray = new Spray(_organizationId, _farmId, _userId, status: SprayStatus.Scheduled);

        Assert.Throws<InvalidOperationException>(() =>
            spray.Start(now.AddHours(2), now, _userId));
    }

    [Fact]
    public void Spray_SaveExecution_WhenInProgress_UpdatesProperties()
    {
        var now = DateTimeOffset.UtcNow;
        var spray = new Spray(_organizationId, _farmId, _userId, status: SprayStatus.InProgress);

        spray.SaveExecution(
            now,
            actualTreatedArea: 2.5m,
            actualTreatedAreaUnitId: _unitId,
            waterQuantity: 200m,
            waterUnitId: _waterUnitId,
            targetId: _targetId,
            applicationMethodId: _methodId,
            purposeReason: "Powdery mildew prevention",
            now: now,
            updatedBy: _userId);

        Assert.Equal(2.5m, spray.ActualTreatedArea);
        Assert.Equal(_unitId, spray.ActualTreatedAreaUnitId);
        Assert.Equal(200m, spray.WaterQuantity);
        Assert.Equal(_waterUnitId, spray.WaterUnitId);
        Assert.Equal("Powdery mildew prevention", spray.PurposeReason);
    }

    [Fact]
    public void Spray_Complete_WhenInProgress_TransitionsToCompleted()
    {
        var now = DateTimeOffset.UtcNow;
        var spray = new Spray(_organizationId, _farmId, _userId, status: SprayStatus.InProgress);

        spray.Complete(now, now, _userId);

        Assert.Equal(SprayStatus.Completed, spray.Status);
        Assert.Equal(now, spray.ActualApplicationDateTime);
    }

    [Fact]
    public void Spray_Complete_WhenNotInProgress_ThrowsInvalidOperationException()
    {
        var now = DateTimeOffset.UtcNow;
        var spray = new Spray(_organizationId, _farmId, _userId, status: SprayStatus.Draft);

        Assert.Throws<InvalidOperationException>(() =>
            spray.Complete(now, now, _userId));
    }

    [Fact]
    public void Spray_Cancel_FromDraftOrScheduled_TransitionsToCancelled()
    {
        var now = DateTimeOffset.UtcNow;
        var spray = new Spray(_organizationId, _farmId, _userId, status: SprayStatus.Scheduled);

        spray.Cancel("Heavy rain forecast", now, _userId);

        Assert.Equal(SprayStatus.Cancelled, spray.Status);
        Assert.Equal("Heavy rain forecast", spray.CancellationReason);
    }

    [Fact]
    public void Spray_Cancel_WhenInProgress_ThrowsInvalidOperationException()
    {
        var now = DateTimeOffset.UtcNow;
        var spray = new Spray(_organizationId, _farmId, _userId, status: SprayStatus.InProgress);

        Assert.Throws<InvalidOperationException>(() =>
            spray.Cancel("Weather change", now, _userId));
    }

    [Fact]
    public void Spray_Cancel_EmptyReason_ThrowsArgumentException()
    {
        var now = DateTimeOffset.UtcNow;
        var spray = new Spray(_organizationId, _farmId, _userId, status: SprayStatus.Draft);

        Assert.Throws<ArgumentException>(() =>
            spray.Cancel("   ", now, _userId));
    }

    [Fact]
    public void Spray_AddProduct_DuplicateItem_ThrowsInvalidOperationException()
    {
        var spray = new Spray(_organizationId, _farmId, _userId);
        var itemId = Guid.NewGuid();

        var p1 = new SprayProduct(spray.Id, itemId, _userId, plannedQuantity: 10m);
        var p2 = new SprayProduct(spray.Id, itemId, _userId, plannedQuantity: 20m);

        spray.AddProduct(p1);
        Assert.Single(spray.Products);

        Assert.Throws<InvalidOperationException>(() => spray.AddProduct(p2));
    }

    [Fact]
    public void SprayProduct_SetExecution_UpdatesLocationAndActualQuantity()
    {
        var sprayId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var locId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var product = new SprayProduct(sprayId, itemId, _userId, plannedQuantity: 5m);
        product.SetExecution(locId, 5.2m, "2 ml/L", now, _userId);

        Assert.Equal(locId, product.StorageLocationId);
        Assert.Equal(5.2m, product.ActualQuantity);
        Assert.Equal("2 ml/L", product.Dosage);
        Assert.Equal(now, product.UpdatedAt);
    }
}
