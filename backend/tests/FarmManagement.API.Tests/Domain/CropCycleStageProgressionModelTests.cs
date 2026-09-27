using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Domain;

public sealed class CropCycleStageProgressionModelTests
{
    private readonly Guid _cropCycleId = Guid.NewGuid();
    private readonly Guid _lifecycleTemplateStageId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public void Complete_WhenInProgress_SetsCompletedStatusAndEndDate()
    {
        var stage = CreateStage(CropCycleStageStatus.InProgress, actualStartDate: new DateOnly(2026, 5, 1));
        var now = DateTimeOffset.UtcNow;
        var endDate = new DateOnly(2026, 5, 15);

        var result = stage.Complete(endDate, now, _userId, "Stage finished on time.");

        Assert.True(result);
        Assert.Equal(CropCycleStageStatus.Completed, stage.Status);
        Assert.Equal(endDate, stage.ActualEndDate);
        Assert.Contains("Stage finished on time.", stage.Notes ?? "");
        Assert.Equal(now, stage.UpdatedAt);
        Assert.Equal(_userId, stage.UpdatedBy);
    }

    [Fact]
    public void Complete_WhenEndDateBeforeStartDate_ThrowsArgumentException()
    {
        var stage = CreateStage(CropCycleStageStatus.InProgress, actualStartDate: new DateOnly(2026, 5, 10));
        var now = DateTimeOffset.UtcNow;
        var invalidEndDate = new DateOnly(2026, 5, 5);

        var ex = Assert.Throws<ArgumentException>(() => stage.Complete(invalidEndDate, now, _userId));
        Assert.Contains("actual completion date cannot be before the stage start date", ex.Message);
    }

    [Fact]
    public void Complete_WhenNotStarted_ReturnsFalse()
    {
        var stage = CreateStage(CropCycleStageStatus.NotStarted);
        var result = stage.Complete(new DateOnly(2026, 5, 15), DateTimeOffset.UtcNow, _userId);
        Assert.False(result);
    }

    [Fact]
    public void Skip_WithValidReason_SetsSkippedStatusAndNotes()
    {
        var stage = CreateStage(CropCycleStageStatus.InProgress, actualStartDate: new DateOnly(2026, 5, 1));
        var now = DateTimeOffset.UtcNow;
        var skipDate = new DateOnly(2026, 5, 5);

        var result = stage.Skip("Frost damage eliminated flowering stage.", now, _userId, skipDate);

        Assert.True(result);
        Assert.Equal(CropCycleStageStatus.Skipped, stage.Status);
        Assert.Equal(skipDate, stage.ActualEndDate);
        Assert.Contains("[Skipped] Frost damage eliminated flowering stage.", stage.Notes);
    }

    [Fact]
    public void Skip_WithoutReason_ThrowsArgumentException()
    {
        var stage = CreateStage(CropCycleStageStatus.InProgress);
        var ex = Assert.Throws<ArgumentException>(() => stage.Skip("   ", DateTimeOffset.UtcNow, _userId));
        Assert.Contains("skip reason is required", ex.Message);
    }

    [Fact]
    public void Reopen_WhenCompleted_SetsInProgressStatusAndClearsEndDate()
    {
        var stage = CreateStage(CropCycleStageStatus.Completed, actualStartDate: new DateOnly(2026, 5, 1), actualEndDate: new DateOnly(2026, 5, 15));
        var now = DateTimeOffset.UtcNow;

        var result = stage.Reopen("Additional pruning needed.", now, _userId);

        Assert.True(result);
        Assert.Equal(CropCycleStageStatus.InProgress, stage.Status);
        Assert.Null(stage.ActualEndDate);
        Assert.Contains("[Reopened] Additional pruning needed.", stage.Notes);
    }

    [Fact]
    public void ResetToNotStarted_ClearsActualDatesAndSetsNotStartedStatus()
    {
        var stage = CreateStage(CropCycleStageStatus.Completed, actualStartDate: new DateOnly(2026, 5, 1), actualEndDate: new DateOnly(2026, 5, 15));
        var now = DateTimeOffset.UtcNow;

        stage.ResetToNotStarted("[Reconciled due to reopening of Stage 1]", now, _userId);

        Assert.Equal(CropCycleStageStatus.NotStarted, stage.Status);
        Assert.Null(stage.ActualStartDate);
        Assert.Null(stage.ActualEndDate);
        Assert.Contains("[Reconciled due to reopening of Stage 1]", stage.Notes);
    }

    [Fact]
    public void Override_SetsTargetStatusDatesAndReasonNotes()
    {
        var stage = CreateStage(CropCycleStageStatus.NotStarted);
        var now = DateTimeOffset.UtcNow;
        var start = new DateOnly(2026, 5, 1);
        var end = new DateOnly(2026, 5, 20);

        var result = stage.Override(CropCycleStageStatus.Completed, start, end, "Emergency override after manual harvest.", now, _userId);

        Assert.True(result);
        Assert.Equal(CropCycleStageStatus.Completed, stage.Status);
        Assert.Equal(start, stage.ActualStartDate);
        Assert.Equal(end, stage.ActualEndDate);
        Assert.Contains("[Override to Completed] Emergency override after manual harvest.", stage.Notes);
    }

    private CropCycleStage CreateStage(
        CropCycleStageStatus status,
        DateOnly? actualStartDate = null,
        DateOnly? actualEndDate = null)
    {
        return new CropCycleStage(
            cropCycleId: _cropCycleId,
            lifecycleTemplateStageId: _lifecycleTemplateStageId,
            stageName: "Pruning",
            sequenceNumber: 2,
            expectedDurationDays: 15,
            plannedStartDate: new DateOnly(2026, 5, 1),
            plannedEndDate: new DateOnly(2026, 5, 16),
            createdBy: _userId,
            status: status,
            actualStartDate: actualStartDate,
            actualEndDate: actualEndDate);
    }
}
