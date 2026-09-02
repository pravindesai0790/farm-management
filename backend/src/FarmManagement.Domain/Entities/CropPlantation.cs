using FarmManagement.Domain.Enums;

namespace FarmManagement.Domain.Entities;

public sealed class CropPlantation
{
    private CropPlantation()
    {
        PlantationCode = string.Empty;
        PlantationName = string.Empty;
    }

    public CropPlantation(
        Guid organizationId,
        Guid farmId,
        Guid farmAreaId,
        Guid cropId,
        Guid? varietyId,
        Guid? lifecycleTemplateId,
        string plantationCode,
        string plantationName,
        decimal allocatedArea,
        Guid areaUnitId,
        DateOnly plantingDate,
        DateOnly? expectedEndDate,
        Guid createdBy)
    {
        ValidateIds(organizationId, farmId, farmAreaId, cropId, areaUnitId, createdBy);
        ValidateOptionalId(varietyId, nameof(varietyId));
        ValidateOptionalId(lifecycleTemplateId, nameof(lifecycleTemplateId));
        ValidateText(plantationCode, "A plantation code is required.", nameof(plantationCode));
        ValidateText(plantationName, "A plantation name is required.", nameof(plantationName));
        ValidateArea(allocatedArea);
        ValidateDates(plantingDate, expectedEndDate);

        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        FarmId = farmId;
        FarmAreaId = farmAreaId;
        CropId = cropId;
        VarietyId = varietyId;
        LifecycleTemplateId = lifecycleTemplateId;
        PlantationCode = plantationCode.Trim().ToUpperInvariant();
        PlantationName = plantationName.Trim();
        AllocatedArea = allocatedArea;
        AreaUnitId = areaUnitId;
        PlantingDate = plantingDate;
        ExpectedEndDate = expectedEndDate;
        Status = PlantationStatus.Planned;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
        CreatedBy = createdBy;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid FarmAreaId { get; private set; }
    public Guid CropId { get; private set; }
    public Guid? VarietyId { get; private set; }
    public Guid? LifecycleTemplateId { get; private set; }
    public string PlantationCode { get; private set; }
    public string PlantationName { get; private set; }
    public decimal AllocatedArea { get; private set; }
    public Guid AreaUnitId { get; private set; }
    public DateOnly PlantingDate { get; private set; }
    public DateOnly? ExpectedEndDate { get; private set; }
    public DateOnly? ActualEndDate { get; private set; }
    public PlantationStatus Status { get; private set; }
    public Guid? EndReasonId { get; private set; }
    public string? EndNotes { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public Organization? Organization { get; private set; }
    public Farm? Farm { get; private set; }
    public FarmArea? FarmArea { get; private set; }
    public Crop? Crop { get; private set; }
    public CropVariety? Variety { get; private set; }
    public CropLifecycleTemplate? LifecycleTemplate { get; private set; }
    public Unit? AreaUnit { get; private set; }
    public PlantationEndReason? EndReason { get; private set; }

    public void Update(
        Guid farmAreaId,
        Guid cropId,
        Guid? varietyId,
        Guid? lifecycleTemplateId,
        string plantationCode,
        string plantationName,
        decimal allocatedArea,
        Guid areaUnitId,
        DateOnly plantingDate,
        DateOnly? expectedEndDate,
        DateTimeOffset now,
        Guid updatedBy)
    {
        ValidateIds(OrganizationId, FarmId, farmAreaId, cropId, areaUnitId, updatedBy);
        ValidateOptionalId(varietyId, nameof(varietyId));
        ValidateOptionalId(lifecycleTemplateId, nameof(lifecycleTemplateId));
        ValidateText(plantationCode, "A plantation code is required.", nameof(plantationCode));
        ValidateText(plantationName, "A plantation name is required.", nameof(plantationName));
        ValidateArea(allocatedArea);
        ValidateDates(plantingDate, expectedEndDate);

        FarmAreaId = farmAreaId;
        CropId = cropId;
        VarietyId = varietyId;
        LifecycleTemplateId = lifecycleTemplateId;
        PlantationCode = plantationCode.Trim().ToUpperInvariant();
        PlantationName = plantationName.Trim();
        AllocatedArea = allocatedArea;
        AreaUnitId = areaUnitId;
        PlantingDate = plantingDate;
        ExpectedEndDate = expectedEndDate;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    public bool Activate(DateTimeOffset now, Guid updatedBy)
    {
        if (Status != PlantationStatus.Planned) return false;
        Status = PlantationStatus.Active;
        IsActive = true;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
        return true;
    }

    public bool Terminate(DateOnly actualEndDate, Guid endReasonId, string? endNotes, DateTimeOffset now, Guid updatedBy)
    {
        if (Status != PlantationStatus.Active) return false;
        if (endReasonId == Guid.Empty) throw new ArgumentException("An end reason is required.", nameof(endReasonId));
        if (actualEndDate < PlantingDate) throw new ArgumentException("The termination date cannot be before the planting date.", nameof(actualEndDate));

        Status = PlantationStatus.Terminated;
        ActualEndDate = actualEndDate;
        EndReasonId = endReasonId;
        EndNotes = string.IsNullOrWhiteSpace(endNotes) ? null : endNotes.Trim();
        IsActive = false;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
        return true;
    }

    public bool Archive(DateTimeOffset now, Guid updatedBy)
    {
        if (Status != PlantationStatus.Terminated) return false;
        Status = PlantationStatus.Archived;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
        return true;
    }

    private static void ValidateIds(Guid organizationId, Guid farmId, Guid farmAreaId, Guid cropId, Guid areaUnitId, Guid userId)
    {
        if (organizationId == Guid.Empty) throw new ArgumentException("An organization is required.", nameof(organizationId));
        if (farmId == Guid.Empty) throw new ArgumentException("A farm is required.", nameof(farmId));
        if (farmAreaId == Guid.Empty) throw new ArgumentException("A farm area is required.", nameof(farmAreaId));
        if (cropId == Guid.Empty) throw new ArgumentException("A crop is required.", nameof(cropId));
        if (areaUnitId == Guid.Empty) throw new ArgumentException("An area unit is required.", nameof(areaUnitId));
        if (userId == Guid.Empty) throw new ArgumentException("A user is required.", nameof(userId));
    }

    private static void ValidateOptionalId(Guid? id, string name)
    {
        if (id == Guid.Empty) throw new ArgumentException("The identifier must be valid.", name);
    }

    private static void ValidateText(string value, string message, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(message, name);
    }

    private static void ValidateArea(decimal value)
    {
        if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), "Allocated area must be greater than zero.");
    }

    private static void ValidateDates(DateOnly plantingDate, DateOnly? expectedEndDate)
    {
        if (expectedEndDate is not null && expectedEndDate < plantingDate)
        {
            throw new ArgumentException("The expected end date cannot be before the planting date.", nameof(expectedEndDate));
        }
    }
}
