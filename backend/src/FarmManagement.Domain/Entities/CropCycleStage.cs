using FarmManagement.Domain.Enums;

namespace FarmManagement.Domain.Entities;

public sealed class CropCycleStage
{
    private CropCycleStage()
    {
        StageName = string.Empty;
    }

    public CropCycleStage(
        Guid cropCycleId,
        Guid lifecycleTemplateStageId,
        string stageName,
        int sequenceNumber,
        int? expectedDurationDays,
        DateOnly? plannedStartDate,
        DateOnly? plannedEndDate,
        Guid createdBy,
        CropCycleStageStatus status = CropCycleStageStatus.NotStarted,
        DateOnly? actualStartDate = null,
        DateOnly? actualEndDate = null,
        string? notes = null)
    {
        if (cropCycleId == Guid.Empty) throw new ArgumentException("A crop cycle is required.", nameof(cropCycleId));
        if (lifecycleTemplateStageId == Guid.Empty) throw new ArgumentException("A lifecycle template stage is required.", nameof(lifecycleTemplateStageId));
        ValidateStageName(stageName);
        ValidateSequenceNumber(sequenceNumber);
        ValidateExpectedDurationDays(expectedDurationDays);
        ValidatePlannedDates(plannedStartDate, plannedEndDate);
        ValidateActualDates(actualStartDate, actualEndDate);
        if (createdBy == Guid.Empty) throw new ArgumentException("A user is required.", nameof(createdBy));

        Id = Guid.NewGuid();
        CropCycleId = cropCycleId;
        LifecycleTemplateStageId = lifecycleTemplateStageId;
        StageName = stageName.Trim();
        SequenceNumber = sequenceNumber;
        ExpectedDurationDays = expectedDurationDays;
        PlannedStartDate = plannedStartDate;
        PlannedEndDate = plannedEndDate;
        ActualStartDate = actualStartDate;
        ActualEndDate = actualEndDate;
        Status = status;
        Notes = NormalizeOptional(notes);
        CreatedAt = DateTimeOffset.UtcNow;
        CreatedBy = createdBy;
    }

    public Guid Id { get; private set; }
    public Guid CropCycleId { get; private set; }
    public Guid LifecycleTemplateStageId { get; private set; }
    public string StageName { get; private set; }
    public int SequenceNumber { get; private set; }
    public int? ExpectedDurationDays { get; private set; }
    public DateOnly? PlannedStartDate { get; private set; }
    public DateOnly? PlannedEndDate { get; private set; }
    public DateOnly? ActualStartDate { get; private set; }
    public DateOnly? ActualEndDate { get; private set; }
    public CropCycleStageStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public CropCycle CropCycle { get; private set; } = null!;
    public CropLifecycleStage LifecycleTemplateStage { get; private set; } = null!;

    public bool Start(DateOnly actualStartDate, DateTimeOffset now, Guid updatedBy)
    {
        if (Status != CropCycleStageStatus.NotStarted) return false;
        EnsureUpdatedBy(updatedBy);

        ActualStartDate = actualStartDate;
        ActualEndDate = null;
        Status = CropCycleStageStatus.InProgress;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
        return true;
    }

    public bool Complete(DateOnly actualEndDate, DateTimeOffset now, Guid updatedBy, string? notes = null)
    {
        if (Status != CropCycleStageStatus.InProgress) return false;
        if (ActualStartDate is not null && actualEndDate < ActualStartDate.Value)
        {
            throw new ArgumentException("The actual completion date cannot be before the stage start date.", nameof(actualEndDate));
        }
        EnsureUpdatedBy(updatedBy);

        ActualEndDate = actualEndDate;
        Status = CropCycleStageStatus.Completed;
        if (!string.IsNullOrWhiteSpace(notes))
        {
            Notes = string.IsNullOrWhiteSpace(Notes) ? notes.Trim() : $"{Notes}\n{notes.Trim()}";
        }
        UpdatedAt = now;
        UpdatedBy = updatedBy;
        return true;
    }

    public bool Skip(string reason, DateTimeOffset now, Guid updatedBy)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A skip reason is required.", nameof(reason));
        }
        if (Status is CropCycleStageStatus.Completed or CropCycleStageStatus.Cancelled)
        {
            return false;
        }
        EnsureUpdatedBy(updatedBy);

        Status = CropCycleStageStatus.Skipped;
        Notes = string.IsNullOrWhiteSpace(Notes) ? $"[Skipped] {reason.Trim()}" : $"{Notes}\n[Skipped] {reason.Trim()}";
        UpdatedAt = now;
        UpdatedBy = updatedBy;
        return true;
    }

    public bool Reopen(string reason, DateTimeOffset now, Guid updatedBy)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A reopen reason is required.", nameof(reason));
        }
        if (Status is not (CropCycleStageStatus.Completed or CropCycleStageStatus.Skipped))
        {
            return false;
        }
        EnsureUpdatedBy(updatedBy);

        Status = CropCycleStageStatus.InProgress;
        ActualEndDate = null;
        Notes = string.IsNullOrWhiteSpace(Notes) ? $"[Reopened] {reason.Trim()}" : $"{Notes}\n[Reopened] {reason.Trim()}";
        UpdatedAt = now;
        UpdatedBy = updatedBy;
        return true;
    }

    public bool Override(
        CropCycleStageStatus newStatus,
        DateOnly? actualStartDate,
        DateOnly? actualEndDate,
        string reason,
        DateTimeOffset now,
        Guid updatedBy)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("An override reason is required.", nameof(reason));
        }
        ValidateActualDates(actualStartDate, actualEndDate);
        EnsureUpdatedBy(updatedBy);

        Status = newStatus;
        ActualStartDate = actualStartDate;
        ActualEndDate = actualEndDate;
        Notes = string.IsNullOrWhiteSpace(Notes) ? $"[Override to {newStatus}] {reason.Trim()}" : $"{Notes}\n[Override to {newStatus}] {reason.Trim()}";
        UpdatedAt = now;
        UpdatedBy = updatedBy;
        return true;
    }

    public void UpdatePlannedDates(DateOnly? plannedStartDate, DateOnly? plannedEndDate, DateTimeOffset now, Guid updatedBy)
    {
        ValidatePlannedDates(plannedStartDate, plannedEndDate);
        EnsureUpdatedBy(updatedBy);

        PlannedStartDate = plannedStartDate;
        PlannedEndDate = plannedEndDate;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    public void UpdateNotes(string? notes, DateTimeOffset now, Guid updatedBy)
    {
        EnsureUpdatedBy(updatedBy);
        Notes = NormalizeOptional(notes);
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    private static void ValidateStageName(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A stage name is required.", nameof(value));
    }

    private static void ValidateSequenceNumber(int value)
    {
        if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), "A stage sequence number must be greater than zero.");
    }

    private static void ValidateExpectedDurationDays(int? value)
    {
        if (value is not null && value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Expected duration days must be greater than zero.");
        }
    }

    private static void ValidatePlannedDates(DateOnly? startDate, DateOnly? endDate)
    {
        if (startDate is not null && endDate is not null && endDate < startDate)
        {
            throw new ArgumentException("Planned end date cannot be before planned start date.", nameof(endDate));
        }
    }

    private static void ValidateActualDates(DateOnly? startDate, DateOnly? endDate)
    {
        if (startDate is not null && endDate is not null && endDate < startDate)
        {
            throw new ArgumentException("Actual end date cannot be before actual start date.", nameof(endDate));
        }
    }

    private static void EnsureUpdatedBy(Guid updatedBy)
    {
        if (updatedBy == Guid.Empty) throw new ArgumentException("A user is required.", nameof(updatedBy));
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
