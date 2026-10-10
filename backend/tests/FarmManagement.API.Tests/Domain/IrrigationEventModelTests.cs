using System;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Domain;

public class IrrigationEventModelTests
{
    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid FarmId = Guid.NewGuid();
    private static readonly Guid FarmAreaId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid MethodId = Guid.NewGuid();
    private static readonly Guid UnitId = Guid.NewGuid();

    [Fact]
    public void Constructor_ValidParameters_InstantiatesInDraftStatus()
    {
        var plannedAt = DateTimeOffset.UtcNow.AddDays(1);
        var irrigation = new IrrigationEvent(
            organizationId: OrgId,
            farmId: FarmId,
            farmAreaId: FarmAreaId,
            createdBy: UserId,
            plantationId: null,
            cropCycleId: null,
            cropCycleStageId: null,
            irrigationMethodId: MethodId,
            status: IrrigationStatus.Draft,
            plannedAt: plannedAt,
            scheduledAt: null,
            actualStartedAt: null,
            actualEndedAt: null,
            actualDurationMinutes: null,
            plannedWaterQuantity: 1500.50m,
            plannedWaterUnitId: UnitId,
            actualWaterQuantity: null,
            actualWaterUnitId: null,
            notes: "Initial draft note",
            cancellationReason: null,
            completedAt: null);

        Assert.Equal(OrgId, irrigation.OrganizationId);
        Assert.Equal(FarmId, irrigation.FarmId);
        Assert.Equal(FarmAreaId, irrigation.FarmAreaId);
        Assert.Equal(IrrigationStatus.Draft, irrigation.Status);
        Assert.Equal(plannedAt, irrigation.PlannedAt);
        Assert.Null(irrigation.ScheduledAt);
        Assert.Equal(1500.50m, irrigation.PlannedWaterQuantity);
        Assert.Equal(UnitId, irrigation.PlannedWaterUnitId);
        Assert.Equal("Initial draft note", irrigation.Notes);
        Assert.Equal(UserId, irrigation.CreatedBy);
    }

    [Fact]
    public void Constructor_NegativeQuantity_ThrowsArgumentOutOfRangeException()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new IrrigationEvent(
                organizationId: OrgId,
                farmId: FarmId,
                farmAreaId: FarmAreaId,
                createdBy: UserId,
                plannedWaterQuantity: -5m,
                plannedWaterUnitId: UnitId));

        Assert.Contains("Water quantity must be greater than zero", ex.Message);
    }

    [Fact]
    public void Constructor_QuantityWithoutUnit_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new IrrigationEvent(
                organizationId: OrgId,
                farmId: FarmId,
                farmAreaId: FarmAreaId,
                createdBy: UserId,
                plannedWaterQuantity: 100m,
                plannedWaterUnitId: null));

        Assert.Contains("A water unit is required when water quantity is specified", ex.Message);
    }

    [Fact]
    public void Schedule_FromDraft_SetsStatusAndScheduledAt()
    {
        var irrigation = new IrrigationEvent(
            organizationId: OrgId,
            farmId: FarmId,
            farmAreaId: FarmAreaId,
            createdBy: UserId);

        var now = DateTimeOffset.UtcNow;
        var scheduledTime = now.AddDays(2);
        irrigation.Schedule(scheduledTime, now, UserId);

        Assert.Equal(IrrigationStatus.Scheduled, irrigation.Status);
        Assert.Equal(scheduledTime, irrigation.ScheduledAt);
        Assert.Equal(UserId, irrigation.UpdatedBy);
    }

    [Fact]
    public void Reschedule_WhenScheduled_UpdatesScheduledAt()
    {
        var now = DateTimeOffset.UtcNow;
        var irrigation = new IrrigationEvent(
            organizationId: OrgId,
            farmId: FarmId,
            farmAreaId: FarmAreaId,
            createdBy: UserId);

        irrigation.Schedule(now.AddDays(1), now, UserId);
        var rescheduledTime = now.AddDays(3);
        irrigation.Reschedule(rescheduledTime, now.AddMinutes(5), UserId);

        Assert.Equal(rescheduledTime, irrigation.ScheduledAt);
        Assert.Equal(IrrigationStatus.Scheduled, irrigation.Status);
    }

    [Fact]
    public void Reschedule_WhenNotScheduled_ThrowsInvalidOperationException()
    {
        var now = DateTimeOffset.UtcNow;
        var irrigation = new IrrigationEvent(
            organizationId: OrgId,
            farmId: FarmId,
            farmAreaId: FarmAreaId,
            createdBy: UserId);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            irrigation.Reschedule(now.AddDays(2), now, UserId));

        Assert.Contains("Only scheduled irrigation events can be rescheduled", ex.Message);
    }

    [Fact]
    public void Start_FromScheduled_SetsStatusAndStartedAt()
    {
        var now = DateTimeOffset.UtcNow;
        var irrigation = new IrrigationEvent(
            organizationId: OrgId,
            farmId: FarmId,
            farmAreaId: FarmAreaId,
            createdBy: UserId);

        irrigation.Schedule(now.AddHours(1), now, UserId);

        var startTime = now.AddMinutes(30);
        irrigation.Start(startTime, startTime, UserId);

        Assert.Equal(IrrigationStatus.InProgress, irrigation.Status);
        Assert.Equal(startTime, irrigation.ActualStartedAt);
    }

    [Fact]
    public void Complete_FromInProgress_SetsStatusCompletedAndActuals()
    {
        var now = DateTimeOffset.UtcNow;
        var irrigation = new IrrigationEvent(
            organizationId: OrgId,
            farmId: FarmId,
            farmAreaId: FarmAreaId,
            createdBy: UserId);

        irrigation.Schedule(now.AddHours(-2), now.AddHours(-3), UserId);
        var startTime = now.AddHours(-1);
        irrigation.Start(startTime, startTime, UserId);

        var endTime = now;
        irrigation.Complete(
            irrigationMethodId: MethodId,
            actualStartedAt: startTime,
            actualEndedAt: endTime,
            actualDurationMinutes: 60,
            actualWaterQuantity: 2500m,
            actualWaterUnitId: UnitId,
            notes: "Execution completed smoothly",
            now: now,
            updatedBy: UserId);

        Assert.Equal(IrrigationStatus.Completed, irrigation.Status);
        Assert.Equal(startTime, irrigation.ActualStartedAt);
        Assert.Equal(endTime, irrigation.ActualEndedAt);
        Assert.Equal(60, irrigation.ActualDurationMinutes);
        Assert.Equal(2500m, irrigation.ActualWaterQuantity);
        Assert.Equal(UnitId, irrigation.ActualWaterUnitId);
        Assert.Equal(now, irrigation.CompletedAt);
    }

    [Fact]
    public void Complete_WhenEndedBeforeStarted_ThrowsArgumentException()
    {
        var now = DateTimeOffset.UtcNow;
        var irrigation = new IrrigationEvent(
            organizationId: OrgId,
            farmId: FarmId,
            farmAreaId: FarmAreaId,
            createdBy: UserId);

        var startTime = now;
        irrigation.Start(startTime, now, UserId);

        var ex = Assert.Throws<ArgumentException>(() =>
            irrigation.Complete(
                irrigationMethodId: MethodId,
                actualStartedAt: startTime,
                actualEndedAt: startTime.AddMinutes(-30),
                actualDurationMinutes: 30,
                actualWaterQuantity: 100m,
                actualWaterUnitId: UnitId,
                notes: null,
                now: now,
                updatedBy: UserId));

        Assert.Contains("Actual ended date/time cannot be earlier than actual started date/time", ex.Message);
    }

    [Fact]
    public void Complete_WithoutIrrigationMethod_ThrowsArgumentException()
    {
        var now = DateTimeOffset.UtcNow;
        var irrigation = new IrrigationEvent(
            organizationId: OrgId,
            farmId: FarmId,
            farmAreaId: FarmAreaId,
            createdBy: UserId);

        irrigation.Start(now, now, UserId);

        var ex = Assert.Throws<ArgumentException>(() =>
            irrigation.Complete(
                irrigationMethodId: Guid.Empty,
                actualStartedAt: now.AddHours(-1),
                actualEndedAt: now,
                actualDurationMinutes: null,
                actualWaterQuantity: null,
                actualWaterUnitId: null,
                notes: null,
                now: now,
                updatedBy: UserId));

        Assert.Contains("An irrigation method is required to complete an irrigation event", ex.Message);
    }

    [Fact]
    public void Cancel_FromScheduled_SetsStatusAndCancellationReason()
    {
        var now = DateTimeOffset.UtcNow;
        var irrigation = new IrrigationEvent(
            organizationId: OrgId,
            farmId: FarmId,
            farmAreaId: FarmAreaId,
            createdBy: UserId);

        irrigation.Schedule(now.AddDays(1), now, UserId);
        irrigation.Cancel("Rain expected", now, UserId);

        Assert.Equal(IrrigationStatus.Cancelled, irrigation.Status);
        Assert.Equal("Rain expected", irrigation.CancellationReason);
    }

    [Fact]
    public void Cancel_WhenAlreadyCompleted_ThrowsInvalidOperationException()
    {
        var now = DateTimeOffset.UtcNow;
        var irrigation = new IrrigationEvent(
            organizationId: OrgId,
            farmId: FarmId,
            farmAreaId: FarmAreaId,
            createdBy: UserId);

        irrigation.Complete(
            irrigationMethodId: MethodId,
            actualStartedAt: now.AddHours(-1),
            actualEndedAt: now,
            actualDurationMinutes: 60,
            actualWaterQuantity: null,
            actualWaterUnitId: null,
            notes: null,
            now: now,
            updatedBy: UserId);

        var ex = Assert.Throws<InvalidOperationException>(() => irrigation.Cancel("Try cancel", now, UserId));
        Assert.Contains("Cannot modify an irrigation event with terminal status 'Completed'", ex.Message);
    }

    [Fact]
    public void UpdatePlanning_WhenDraft_UpdatesSuccessfully()
    {
        var now = DateTimeOffset.UtcNow;
        var irrigation = new IrrigationEvent(
            organizationId: OrgId,
            farmId: FarmId,
            farmAreaId: FarmAreaId,
            createdBy: UserId);

        var newAreaId = Guid.NewGuid();
        var newPlannedTime = now.AddDays(3);
        irrigation.UpdatePlanning(
            farmId: FarmId,
            farmAreaId: newAreaId,
            plantationId: null,
            cropCycleId: null,
            cropCycleStageId: null,
            plannedAt: newPlannedTime,
            plannedWaterQuantity: 800m,
            plannedWaterUnitId: UnitId,
            notes: "Updated planning",
            now: now,
            updatedBy: UserId);

        Assert.Equal(newAreaId, irrigation.FarmAreaId);
        Assert.Equal(newPlannedTime, irrigation.PlannedAt);
        Assert.Equal(800m, irrigation.PlannedWaterQuantity);
        Assert.Equal(UnitId, irrigation.PlannedWaterUnitId);
        Assert.Equal("Updated planning", irrigation.Notes);
    }
}
