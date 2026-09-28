using FarmManagement.Domain.Enums;

namespace FarmManagement.Domain.Entities;

public sealed class LaborActivity
{
    private LaborActivity()
    {
    }

    public LaborActivity(
        Guid organizationId,
        DateOnly activityDate,
        Guid farmId,
        Guid laborActivityTypeId,
        Guid createdBy,
        Guid? farmAreaId = null,
        Guid? plantationId = null,
        Guid? cropCycleId = null,
        Guid? cropCycleStageId = null,
        string? description = null,
        LaborActivityStatus status = LaborActivityStatus.Completed)
    {
        ValidateRequiredIdentifiers(organizationId, farmId, laborActivityTypeId, createdBy);
        ValidateOptionalIdentifiers(farmAreaId, plantationId, cropCycleId, cropCycleStageId);

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), "The labor activity status is invalid.");
        }

        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        ActivityDate = activityDate;
        FarmId = farmId;
        FarmAreaId = farmAreaId;
        PlantationId = plantationId;
        CropCycleId = cropCycleId;
        CropCycleStageId = cropCycleStageId;
        LaborActivityTypeId = laborActivityTypeId;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Status = status;
        CreatedAt = DateTimeOffset.UtcNow;
        CreatedBy = createdBy;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public DateOnly ActivityDate { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid? FarmAreaId { get; private set; }
    public Guid? PlantationId { get; private set; }
    public Guid? CropCycleId { get; private set; }
    public Guid? CropCycleStageId { get; private set; }
    public Guid LaborActivityTypeId { get; private set; }
    public string? Description { get; private set; }
    public LaborActivityStatus Status { get; private set; }
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
    public LaborActivityType? LaborActivityType { get; private set; }

    public void Update(
        DateOnly activityDate,
        Guid farmId,
        Guid? farmAreaId,
        Guid? plantationId,
        Guid? cropCycleId,
        Guid? cropCycleStageId,
        Guid laborActivityTypeId,
        string? description,
        DateTimeOffset now,
        Guid updatedBy)
    {
        if (Status == LaborActivityStatus.Cancelled)
        {
            throw new InvalidOperationException("Cancelled labor activities cannot be modified.");
        }

        ValidateRequiredIdentifiers(OrganizationId, farmId, laborActivityTypeId, updatedBy);
        ValidateOptionalIdentifiers(farmAreaId, plantationId, cropCycleId, cropCycleStageId);

        ActivityDate = activityDate;
        FarmId = farmId;
        FarmAreaId = farmAreaId;
        PlantationId = plantationId;
        CropCycleId = cropCycleId;
        CropCycleStageId = cropCycleStageId;
        LaborActivityTypeId = laborActivityTypeId;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    public bool Cancel(DateTimeOffset now, Guid updatedBy)
    {
        if (Status == LaborActivityStatus.Cancelled)
        {
            return false;
        }

        if (updatedBy == Guid.Empty)
        {
            throw new ArgumentException("A user is required.", nameof(updatedBy));
        }

        Status = LaborActivityStatus.Cancelled;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
        return true;
    }

    public bool Complete(DateTimeOffset now, Guid updatedBy)
    {
        if (Status == LaborActivityStatus.Completed)
        {
            return false;
        }

        if (Status == LaborActivityStatus.Cancelled)
        {
            throw new InvalidOperationException("Cancelled labor activities cannot be completed.");
        }

        if (updatedBy == Guid.Empty)
        {
            throw new ArgumentException("A user is required.", nameof(updatedBy));
        }

        Status = LaborActivityStatus.Completed;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
        return true;
    }

    private static void ValidateRequiredIdentifiers(Guid organizationId, Guid farmId, Guid laborActivityTypeId, Guid userId)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("An organization is required.", nameof(organizationId));
        }

        if (farmId == Guid.Empty)
        {
            throw new ArgumentException("A farm is required.", nameof(farmId));
        }

        if (laborActivityTypeId == Guid.Empty)
        {
            throw new ArgumentException("A labor activity type is required.", nameof(laborActivityTypeId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("A user is required.", nameof(userId));
        }
    }

    private static void ValidateOptionalIdentifiers(Guid? farmAreaId, Guid? plantationId, Guid? cropCycleId, Guid? cropCycleStageId)
    {
        if (farmAreaId == Guid.Empty)
        {
            throw new ArgumentException("Farm area identifier must be valid.", nameof(farmAreaId));
        }

        if (plantationId == Guid.Empty)
        {
            throw new ArgumentException("Plantation identifier must be valid.", nameof(plantationId));
        }

        if (cropCycleId == Guid.Empty)
        {
            throw new ArgumentException("Crop cycle identifier must be valid.", nameof(cropCycleId));
        }

        if (cropCycleStageId == Guid.Empty)
        {
            throw new ArgumentException("Crop cycle stage identifier must be valid.", nameof(cropCycleStageId));
        }

        if (cropCycleId.HasValue && !plantationId.HasValue)
        {
            throw new ArgumentException("A plantation is required when specifying a crop cycle.", nameof(cropCycleId));
        }

        if (cropCycleStageId.HasValue && !cropCycleId.HasValue)
        {
            throw new ArgumentException("A crop cycle is required when specifying a crop cycle stage.", nameof(cropCycleStageId));
        }
    }
}
