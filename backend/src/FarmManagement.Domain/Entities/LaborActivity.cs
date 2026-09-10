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
        int workerCount,
        Guid createdBy,
        Guid? farmAreaId = null,
        Guid? plantationId = null,
        Guid? cropCycleId = null,
        decimal? totalWorkingHours = null,
        decimal? costAmount = null,
        Guid? currencyId = null,
        string? description = null,
        LaborActivityStatus status = LaborActivityStatus.Completed)
    {
        ValidateRequiredIdentifiers(organizationId, farmId, laborActivityTypeId, createdBy);
        ValidateOptionalIdentifiers(farmAreaId, plantationId, cropCycleId, currencyId);
        ValidateMetrics(workerCount, totalWorkingHours, costAmount);

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
        LaborActivityTypeId = laborActivityTypeId;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        WorkerCount = workerCount;
        TotalWorkingHours = totalWorkingHours;
        CostAmount = costAmount;
        CurrencyId = currencyId;
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
    public Guid LaborActivityTypeId { get; private set; }
    public string? Description { get; private set; }
    public int WorkerCount { get; private set; }
    public decimal? TotalWorkingHours { get; private set; }
    public decimal? CostAmount { get; private set; }
    public Guid? CurrencyId { get; private set; }
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
    public LaborActivityType? LaborActivityType { get; private set; }

    public void Update(
        DateOnly activityDate,
        Guid farmId,
        Guid? farmAreaId,
        Guid? plantationId,
        Guid? cropCycleId,
        Guid laborActivityTypeId,
        int workerCount,
        decimal? totalWorkingHours,
        decimal? costAmount,
        Guid? currencyId,
        string? description,
        DateTimeOffset now,
        Guid updatedBy)
    {
        if (Status == LaborActivityStatus.Cancelled)
        {
            throw new InvalidOperationException("Cancelled labor activities cannot be modified.");
        }

        ValidateRequiredIdentifiers(OrganizationId, farmId, laborActivityTypeId, updatedBy);
        ValidateOptionalIdentifiers(farmAreaId, plantationId, cropCycleId, currencyId);
        ValidateMetrics(workerCount, totalWorkingHours, costAmount);

        ActivityDate = activityDate;
        FarmId = farmId;
        FarmAreaId = farmAreaId;
        PlantationId = plantationId;
        CropCycleId = cropCycleId;
        LaborActivityTypeId = laborActivityTypeId;
        WorkerCount = workerCount;
        TotalWorkingHours = totalWorkingHours;
        CostAmount = costAmount;
        CurrencyId = currencyId;
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

    private static void ValidateOptionalIdentifiers(Guid? farmAreaId, Guid? plantationId, Guid? cropCycleId, Guid? currencyId)
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

        if (currencyId == Guid.Empty)
        {
            throw new ArgumentException("Currency identifier must be valid.", nameof(currencyId));
        }
    }

    private static void ValidateMetrics(int workerCount, decimal? totalWorkingHours, decimal? costAmount)
    {
        if (workerCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(workerCount), "Worker count must be greater than zero.");
        }

        if (totalWorkingHours is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalWorkingHours), "Total working hours must be greater than zero.");
        }

        if (costAmount is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(costAmount), "Cost amount cannot be negative.");
        }
    }
}
