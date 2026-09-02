using FarmManagement.Domain.Enums;

namespace FarmManagement.Domain.Entities;

public sealed class CropCycle
{
    private CropCycle()
    {
        CycleCode = string.Empty;
        CycleName = string.Empty;
    }

    public CropCycle(
        Guid organizationId,
        Guid plantationId,
        string cycleCode,
        string cycleName,
        int seasonYear,
        string? seasonName,
        DateOnly plannedStartDate,
        DateOnly? expectedEndDate,
        Guid createdBy)
    {
        if (organizationId == Guid.Empty) throw new ArgumentException("An organization is required.", nameof(organizationId));
        if (plantationId == Guid.Empty) throw new ArgumentException("A plantation is required.", nameof(plantationId));
        if (string.IsNullOrWhiteSpace(cycleCode)) throw new ArgumentException("A cycle code is required.", nameof(cycleCode));
        if (string.IsNullOrWhiteSpace(cycleName)) throw new ArgumentException("A cycle name is required.", nameof(cycleName));
        if (seasonYear <= 0) throw new ArgumentOutOfRangeException(nameof(seasonYear), "Season year must be greater than zero.");
        if (expectedEndDate is not null && expectedEndDate < plannedStartDate)
        {
            throw new ArgumentException("The expected end date cannot be before the planned start date.", nameof(expectedEndDate));
        }
        if (createdBy == Guid.Empty) throw new ArgumentException("A user is required.", nameof(createdBy));

        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        PlantationId = plantationId;
        CycleCode = cycleCode.Trim().ToUpperInvariant();
        CycleName = cycleName.Trim();
        SeasonYear = seasonYear;
        SeasonName = string.IsNullOrWhiteSpace(seasonName) ? null : seasonName.Trim();
        PlannedStartDate = plannedStartDate;
        ExpectedEndDate = expectedEndDate;
        Status = CropCycleStatus.Planned;
        CreatedAt = DateTimeOffset.UtcNow;
        CreatedBy = createdBy;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid PlantationId { get; private set; }
    public string CycleCode { get; private set; }
    public string CycleName { get; private set; }
    public int SeasonYear { get; private set; }
    public string? SeasonName { get; private set; }
    public DateOnly PlannedStartDate { get; private set; }
    public DateOnly? ActualStartDate { get; private set; }
    public DateOnly? ExpectedEndDate { get; private set; }
    public DateOnly? ActualEndDate { get; private set; }
    public CropCycleStatus Status { get; private set; }
    public Guid? CancellationReasonId { get; private set; }
    public string? CancellationNotes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public Organization? Organization { get; private set; }
    public CropPlantation? Plantation { get; private set; }
    public PlantationEndReason? CancellationReason { get; private set; }

    public bool Cancel(
        DateOnly cancellationDate,
        Guid cancellationReasonId,
        string? cancellationNotes,
        DateTimeOffset now,
        Guid updatedBy)
    {
        if (Status != CropCycleStatus.Active) return false;
        if (cancellationReasonId == Guid.Empty) throw new ArgumentException("A cancellation reason is required.", nameof(cancellationReasonId));
        if (cancellationDate < (ActualStartDate ?? PlannedStartDate))
        {
            throw new ArgumentException("The cancellation date cannot be before the cycle start date.", nameof(cancellationDate));
        }

        Status = CropCycleStatus.Cancelled;
        ActualEndDate = cancellationDate;
        CancellationReasonId = cancellationReasonId;
        CancellationNotes = string.IsNullOrWhiteSpace(cancellationNotes) ? null : cancellationNotes.Trim();
        UpdatedAt = now;
        UpdatedBy = updatedBy;
        return true;
    }
}
