using FarmManagement.Domain.Enums;

namespace FarmManagement.Domain.Entities;

public sealed class IrrigationEvent
{
    private IrrigationEvent()
    {
    }

    public IrrigationEvent(
        Guid organizationId,
        Guid farmId,
        Guid farmAreaId,
        Guid createdBy,
        Guid? plantationId = null,
        Guid? cropCycleId = null,
        Guid? cropCycleStageId = null,
        Guid? irrigationMethodId = null,
        IrrigationStatus status = IrrigationStatus.Draft,
        DateTimeOffset? plannedAt = null,
        DateTimeOffset? scheduledAt = null,
        DateTimeOffset? actualStartedAt = null,
        DateTimeOffset? actualEndedAt = null,
        int? actualDurationMinutes = null,
        decimal? plannedWaterQuantity = null,
        Guid? plannedWaterUnitId = null,
        decimal? actualWaterQuantity = null,
        Guid? actualWaterUnitId = null,
        string? notes = null,
        string? cancellationReason = null,
        DateTimeOffset? completedAt = null)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("An organization is required.", nameof(organizationId));
        }

        if (farmId == Guid.Empty)
        {
            throw new ArgumentException("A farm is required.", nameof(farmId));
        }

        if (farmAreaId == Guid.Empty)
        {
            throw new ArgumentException("A farm area is required.", nameof(farmAreaId));
        }

        if (createdBy == Guid.Empty)
        {
            throw new ArgumentException("A creating user is required.", nameof(createdBy));
        }

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), "Invalid irrigation status.");
        }

        ValidateWater(plannedWaterQuantity, plannedWaterUnitId, nameof(plannedWaterQuantity), nameof(plannedWaterUnitId));
        ValidateWater(actualWaterQuantity, actualWaterUnitId, nameof(actualWaterQuantity), nameof(actualWaterUnitId));
        ValidateDuration(actualDurationMinutes);
        ValidateActualTimestamps(actualStartedAt, actualEndedAt);

        if (status == IrrigationStatus.Cancelled && string.IsNullOrWhiteSpace(cancellationReason))
        {
            throw new ArgumentException("A cancellation reason is required when status is Cancelled.", nameof(cancellationReason));
        }

        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        FarmId = farmId;
        FarmAreaId = farmAreaId;
        PlantationId = plantationId;
        CropCycleId = cropCycleId;
        CropCycleStageId = cropCycleStageId;
        IrrigationMethodId = irrigationMethodId;
        Status = status;
        PlannedAt = plannedAt;
        ScheduledAt = scheduledAt;
        ActualStartedAt = actualStartedAt;
        ActualEndedAt = actualEndedAt;
        ActualDurationMinutes = actualDurationMinutes;
        PlannedWaterQuantity = plannedWaterQuantity;
        PlannedWaterUnitId = plannedWaterUnitId;
        ActualWaterQuantity = actualWaterQuantity;
        ActualWaterUnitId = actualWaterUnitId;
        Notes = NormalizeOptional(notes);
        CancellationReason = NormalizeOptional(cancellationReason);
        CompletedAt = completedAt;
        CreatedAt = DateTimeOffset.UtcNow;
        CreatedBy = createdBy;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid FarmAreaId { get; private set; }
    public Guid? PlantationId { get; private set; }
    public Guid? CropCycleId { get; private set; }
    public Guid? CropCycleStageId { get; private set; }
    public Guid? IrrigationMethodId { get; private set; }
    public IrrigationStatus Status { get; private set; }
    public DateTimeOffset? PlannedAt { get; private set; }
    public DateTimeOffset? ScheduledAt { get; private set; }
    public DateTimeOffset? ActualStartedAt { get; private set; }
    public DateTimeOffset? ActualEndedAt { get; private set; }
    public int? ActualDurationMinutes { get; private set; }
    public decimal? PlannedWaterQuantity { get; private set; }
    public Guid? PlannedWaterUnitId { get; private set; }
    public decimal? ActualWaterQuantity { get; private set; }
    public Guid? ActualWaterUnitId { get; private set; }
    public string? Notes { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public Organization? Organization { get; private set; }
    public Farm? Farm { get; private set; }
    public FarmArea? FarmArea { get; private set; }
    public CropPlantation? Plantation { get; private set; }
    public CropCycle? CropCycle { get; private set; }
    public CropCycleStage? CropCycleStage { get; private set; }
    public IrrigationMethod? IrrigationMethod { get; private set; }
    public Unit? PlannedWaterUnit { get; private set; }
    public Unit? ActualWaterUnit { get; private set; }

    public void Schedule(DateTimeOffset scheduledAt, DateTimeOffset now, Guid updatedBy)
    {
        EnsureNotTerminal();
        if (Status != IrrigationStatus.Draft)
        {
            throw new InvalidOperationException($"Only draft irrigation events can be scheduled; current status is '{Status}'.");
        }
        if (updatedBy == Guid.Empty) throw new ArgumentException("A user is required.", nameof(updatedBy));

        ScheduledAt = scheduledAt;
        Status = IrrigationStatus.Scheduled;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    public void Reschedule(DateTimeOffset newScheduledAt, DateTimeOffset now, Guid updatedBy)
    {
        EnsureNotTerminal();
        if (Status != IrrigationStatus.Scheduled)
        {
            throw new InvalidOperationException("Only scheduled irrigation events can be rescheduled.");
        }
        if (updatedBy == Guid.Empty) throw new ArgumentException("A user is required.", nameof(updatedBy));

        ScheduledAt = newScheduledAt;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    public void Start(DateTimeOffset? actualStartedAt, DateTimeOffset now, Guid updatedBy)
    {
        EnsureNotTerminal();
        if (Status != IrrigationStatus.Draft && Status != IrrigationStatus.Scheduled)
        {
            throw new InvalidOperationException($"Cannot start an irrigation event in '{Status}' status; allowed transitions are from Draft or Scheduled.");
        }
        if (updatedBy == Guid.Empty) throw new ArgumentException("A user is required.", nameof(updatedBy));

        ActualStartedAt = actualStartedAt ?? now;
        Status = IrrigationStatus.InProgress;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    public void Complete(
        Guid irrigationMethodId,
        DateTimeOffset? actualStartedAt,
        DateTimeOffset? actualEndedAt,
        int? actualDurationMinutes,
        decimal? actualWaterQuantity,
        Guid? actualWaterUnitId,
        string? notes,
        DateTimeOffset now,
        Guid updatedBy)
    {
        EnsureNotTerminal();
        if (irrigationMethodId == Guid.Empty)
        {
            throw new ArgumentException("An irrigation method is required to complete an irrigation event.", nameof(irrigationMethodId));
        }
        if (updatedBy == Guid.Empty) throw new ArgumentException("A user is required.", nameof(updatedBy));

        ValidateWater(actualWaterQuantity, actualWaterUnitId, nameof(actualWaterQuantity), nameof(actualWaterUnitId));
        ValidateDuration(actualDurationMinutes);
        ValidateActualTimestamps(actualStartedAt ?? ActualStartedAt, actualEndedAt);

        IrrigationMethodId = irrigationMethodId;
        ActualStartedAt = actualStartedAt ?? ActualStartedAt ?? now;
        ActualEndedAt = actualEndedAt;
        ActualDurationMinutes = actualDurationMinutes;
        ActualWaterQuantity = actualWaterQuantity;
        ActualWaterUnitId = actualWaterUnitId;
        if (!string.IsNullOrWhiteSpace(notes))
        {
            Notes = notes.Trim();
        }
        Status = IrrigationStatus.Completed;
        CompletedAt = now;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    public void Cancel(string reason, DateTimeOffset now, Guid updatedBy)
    {
        EnsureNotTerminal();
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A cancellation reason is required.", nameof(reason));
        }
        if (updatedBy == Guid.Empty) throw new ArgumentException("A user is required.", nameof(updatedBy));

        CancellationReason = reason.Trim();
        Status = IrrigationStatus.Cancelled;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    public void UpdatePlanning(
        Guid farmId,
        Guid farmAreaId,
        Guid? plantationId,
        Guid? cropCycleId,
        Guid? cropCycleStageId,
        DateTimeOffset? plannedAt,
        decimal? plannedWaterQuantity,
        Guid? plannedWaterUnitId,
        string? notes,
        DateTimeOffset now,
        Guid updatedBy)
    {
        EnsureNotTerminal();
        if (Status != IrrigationStatus.Draft)
        {
            throw new InvalidOperationException("Only draft irrigation events can have core planning fields updated directly.");
        }
        if (farmId == Guid.Empty) throw new ArgumentException("A farm is required.", nameof(farmId));
        if (farmAreaId == Guid.Empty) throw new ArgumentException("A farm area is required.", nameof(farmAreaId));
        if (updatedBy == Guid.Empty) throw new ArgumentException("A user is required.", nameof(updatedBy));

        ValidateWater(plannedWaterQuantity, plannedWaterUnitId, nameof(plannedWaterQuantity), nameof(plannedWaterUnitId));

        FarmId = farmId;
        FarmAreaId = farmAreaId;
        PlantationId = plantationId;
        CropCycleId = cropCycleId;
        CropCycleStageId = cropCycleStageId;
        PlannedAt = plannedAt;
        PlannedWaterQuantity = plannedWaterQuantity;
        PlannedWaterUnitId = plannedWaterUnitId;
        Notes = NormalizeOptional(notes);
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    private void EnsureNotTerminal()
    {
        if (Status == IrrigationStatus.Completed || Status == IrrigationStatus.Cancelled)
        {
            throw new InvalidOperationException($"Cannot modify an irrigation event with terminal status '{Status}'.");
        }
    }

    private static void ValidateWater(decimal? quantity, Guid? unitId, string quantityParam, string unitParam)
    {
        if (quantity.HasValue && quantity.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(quantityParam, "Water quantity must be greater than zero.");
        }

        if (quantity.HasValue && !unitId.HasValue)
        {
            throw new ArgumentException("A water unit is required when water quantity is specified.", unitParam);
        }

        if (!quantity.HasValue && unitId.HasValue)
        {
            throw new ArgumentException("A water quantity is required when water unit is specified.", quantityParam);
        }
    }

    private static void ValidateDuration(int? durationMinutes)
    {
        if (durationMinutes.HasValue && durationMinutes.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationMinutes), "Duration must be greater than zero minutes.");
        }
    }

    private static void ValidateActualTimestamps(DateTimeOffset? startedAt, DateTimeOffset? endedAt)
    {
        if (startedAt.HasValue && endedAt.HasValue && endedAt.Value < startedAt.Value)
        {
            throw new ArgumentException("Actual ended date/time cannot be earlier than actual started date/time.", nameof(endedAt));
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
