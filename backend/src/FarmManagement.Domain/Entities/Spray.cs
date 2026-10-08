using FarmManagement.Domain.Enums;

namespace FarmManagement.Domain.Entities;

public sealed class Spray
{
    private readonly List<SprayProduct> _products = [];

    private Spray()
    {
    }

    public Spray(
        Guid organizationId,
        Guid farmId,
        Guid createdBy,
        Guid? farmAreaId = null,
        Guid? plantationId = null,
        Guid? cropCycleId = null,
        Guid? cropCycleStageId = null,
        SprayStatus status = SprayStatus.Draft,
        DateOnly? plannedDate = null,
        DateTimeOffset? scheduledDateTime = null,
        DateTimeOffset? actualApplicationDateTime = null,
        decimal? plannedArea = null,
        Guid? plannedAreaUnitId = null,
        decimal? actualTreatedArea = null,
        Guid? actualTreatedAreaUnitId = null,
        decimal? waterQuantity = null,
        Guid? waterUnitId = null,
        Guid? targetId = null,
        Guid? applicationMethodId = null,
        string? purposeReason = null,
        string? cancellationReason = null)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("An organization is required.", nameof(organizationId));
        }

        if (farmId == Guid.Empty)
        {
            throw new ArgumentException("A farm is required.", nameof(farmId));
        }

        if (createdBy == Guid.Empty)
        {
            throw new ArgumentException("A creating user is required.", nameof(createdBy));
        }

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), "Invalid spray status.");
        }

        ValidateArea(plannedArea, plannedAreaUnitId, nameof(plannedArea), nameof(plannedAreaUnitId));
        ValidateArea(actualTreatedArea, actualTreatedAreaUnitId, nameof(actualTreatedArea), nameof(actualTreatedAreaUnitId));
        ValidateWater(waterQuantity, waterUnitId);

        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        FarmId = farmId;
        FarmAreaId = farmAreaId;
        PlantationId = plantationId;
        CropCycleId = cropCycleId;
        CropCycleStageId = cropCycleStageId;
        Status = status;
        PlannedDate = plannedDate;
        ScheduledDateTime = scheduledDateTime;
        ActualApplicationDateTime = actualApplicationDateTime;
        PlannedArea = plannedArea;
        PlannedAreaUnitId = plannedAreaUnitId;
        ActualTreatedArea = actualTreatedArea;
        ActualTreatedAreaUnitId = actualTreatedAreaUnitId;
        WaterQuantity = waterQuantity;
        WaterUnitId = waterUnitId;
        TargetId = targetId;
        ApplicationMethodId = applicationMethodId;
        PurposeReason = NormalizeOptional(purposeReason);
        CancellationReason = NormalizeOptional(cancellationReason);
        CreatedAt = DateTimeOffset.UtcNow;
        CreatedBy = createdBy;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid? FarmAreaId { get; private set; }
    public Guid? PlantationId { get; private set; }
    public Guid? CropCycleId { get; private set; }
    public Guid? CropCycleStageId { get; private set; }
    public SprayStatus Status { get; private set; }
    public DateOnly? PlannedDate { get; private set; }
    public DateTimeOffset? ScheduledDateTime { get; private set; }
    public DateTimeOffset? ActualApplicationDateTime { get; private set; }
    public decimal? PlannedArea { get; private set; }
    public Guid? PlannedAreaUnitId { get; private set; }
    public decimal? ActualTreatedArea { get; private set; }
    public Guid? ActualTreatedAreaUnitId { get; private set; }
    public decimal? WaterQuantity { get; private set; }
    public Guid? WaterUnitId { get; private set; }
    public Guid? TargetId { get; private set; }
    public Guid? ApplicationMethodId { get; private set; }
    public string? PurposeReason { get; private set; }
    public string? CancellationReason { get; private set; }
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
    public Unit? PlannedAreaUnit { get; private set; }
    public Unit? ActualTreatedAreaUnit { get; private set; }
    public Unit? WaterUnit { get; private set; }
    public Target? Target { get; private set; }
    public ApplicationMethod? ApplicationMethod { get; private set; }

    public IReadOnlyCollection<SprayProduct> Products => _products.AsReadOnly();

    public void UpdateDraft(
        Guid farmId,
        Guid? farmAreaId,
        Guid? plantationId,
        Guid? cropCycleId,
        Guid? cropCycleStageId,
        DateOnly plannedDate,
        decimal? plannedArea,
        Guid? plannedAreaUnitId,
        decimal? waterQuantity,
        Guid? waterUnitId,
        Guid? targetId,
        Guid? applicationMethodId,
        string? purposeReason,
        DateTimeOffset now,
        Guid updatedBy)
    {
        if (Status != SprayStatus.Draft)
        {
            throw new InvalidOperationException("Only draft sprays can be modified via draft update.");
        }

        if (farmId == Guid.Empty)
        {
            throw new ArgumentException("A farm is required.", nameof(farmId));
        }

        if (updatedBy == Guid.Empty)
        {
            throw new ArgumentException("An updating user is required.", nameof(updatedBy));
        }

        ValidateArea(plannedArea, plannedAreaUnitId, nameof(plannedArea), nameof(plannedAreaUnitId));
        ValidateWater(waterQuantity, waterUnitId);

        FarmId = farmId;
        FarmAreaId = farmAreaId;
        PlantationId = plantationId;
        CropCycleId = cropCycleId;
        CropCycleStageId = cropCycleStageId;
        PlannedDate = plannedDate;
        PlannedArea = plannedArea;
        PlannedAreaUnitId = plannedAreaUnitId;
        WaterQuantity = waterQuantity;
        WaterUnitId = waterUnitId;
        TargetId = targetId;
        ApplicationMethodId = applicationMethodId;
        PurposeReason = NormalizeOptional(purposeReason);
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    public void Schedule(
        DateTimeOffset scheduledDateTime,
        DateOnly plannedDate,
        DateTimeOffset now,
        Guid updatedBy)
    {
        if (Status != SprayStatus.Draft)
        {
            throw new InvalidOperationException("Only draft sprays can be scheduled.");
        }

        if (updatedBy == Guid.Empty)
        {
            throw new ArgumentException("An updating user is required.", nameof(updatedBy));
        }

        ScheduledDateTime = scheduledDateTime;
        PlannedDate = plannedDate;
        Status = SprayStatus.Scheduled;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    public void Reschedule(
        DateTimeOffset scheduledDateTime,
        DateTimeOffset now,
        Guid updatedBy)
    {
        if (Status != SprayStatus.Scheduled)
        {
            throw new InvalidOperationException("Only scheduled sprays can be rescheduled.");
        }

        if (updatedBy == Guid.Empty)
        {
            throw new ArgumentException("An updating user is required.", nameof(updatedBy));
        }

        ScheduledDateTime = scheduledDateTime;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    public void Start(
        DateTimeOffset actualDateTime,
        DateTimeOffset now,
        Guid updatedBy)
    {
        if (Status != SprayStatus.Scheduled)
        {
            throw new InvalidOperationException("Only scheduled sprays can be started.");
        }

        if (actualDateTime > now)
        {
            throw new InvalidOperationException("Actual application date/time cannot be in the future.");
        }

        if (updatedBy == Guid.Empty)
        {
            throw new ArgumentException("An updating user is required.", nameof(updatedBy));
        }

        ActualApplicationDateTime = actualDateTime;
        Status = SprayStatus.InProgress;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    public void SaveExecution(
        DateTimeOffset actualDateTime,
        decimal? actualTreatedArea,
        Guid? actualTreatedAreaUnitId,
        decimal? waterQuantity,
        Guid? waterUnitId,
        Guid? targetId,
        Guid? applicationMethodId,
        string? purposeReason,
        DateTimeOffset now,
        Guid updatedBy)
    {
        if (Status != SprayStatus.InProgress)
        {
            throw new InvalidOperationException("Execution details can only be saved while spray is in progress.");
        }

        if (actualDateTime > now)
        {
            throw new InvalidOperationException("Actual application date/time cannot be in the future.");
        }

        if (updatedBy == Guid.Empty)
        {
            throw new ArgumentException("An updating user is required.", nameof(updatedBy));
        }

        ValidateArea(actualTreatedArea, actualTreatedAreaUnitId, nameof(actualTreatedArea), nameof(actualTreatedAreaUnitId));
        ValidateWater(waterQuantity, waterUnitId);

        ActualApplicationDateTime = actualDateTime;
        ActualTreatedArea = actualTreatedArea;
        ActualTreatedAreaUnitId = actualTreatedAreaUnitId;
        WaterQuantity = waterQuantity;
        WaterUnitId = waterUnitId;
        TargetId = targetId;
        ApplicationMethodId = applicationMethodId;
        PurposeReason = NormalizeOptional(purposeReason);
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    public void Complete(
        DateTimeOffset actualDateTime,
        DateTimeOffset now,
        Guid updatedBy)
    {
        if (Status != SprayStatus.InProgress)
        {
            throw new InvalidOperationException("Only in-progress sprays can be completed.");
        }

        if (actualDateTime > now)
        {
            throw new InvalidOperationException("Actual application date/time cannot be in the future.");
        }

        if (updatedBy == Guid.Empty)
        {
            throw new ArgumentException("An updating user is required.", nameof(updatedBy));
        }

        ActualApplicationDateTime = actualDateTime;
        Status = SprayStatus.Completed;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    public void Cancel(
        string reason,
        DateTimeOffset now,
        Guid updatedBy)
    {
        if (Status != SprayStatus.Draft && Status != SprayStatus.Scheduled)
        {
            throw new InvalidOperationException("Only draft or scheduled sprays can be cancelled.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A cancellation reason is required.", nameof(reason));
        }

        if (updatedBy == Guid.Empty)
        {
            throw new ArgumentException("An updating user is required.", nameof(updatedBy));
        }

        CancellationReason = reason.Trim();
        Status = SprayStatus.Cancelled;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    public void AddProduct(SprayProduct product)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (_products.Any(p => p.InventoryItemId == product.InventoryItemId))
        {
            throw new InvalidOperationException("An inventory item cannot be added more than once to the same spray.");
        }

        _products.Add(product);
    }

    public void RemoveProduct(Guid inventoryItemId)
    {
        var existing = _products.FirstOrDefault(p => p.InventoryItemId == inventoryItemId);
        if (existing is not null)
        {
            _products.Remove(existing);
        }
    }

    public void ClearProducts() => _products.Clear();

    private static void ValidateArea(decimal? area, Guid? unitId, string areaParam, string unitParam)
    {
        if (area.HasValue && area.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(areaParam, "Area must be greater than zero.");
        }

        if (area.HasValue && !unitId.HasValue)
        {
            throw new ArgumentException("An area unit is required when area is specified.", unitParam);
        }

        if (!area.HasValue && unitId.HasValue)
        {
            throw new ArgumentException("An area is required when area unit is specified.", areaParam);
        }
    }

    private static void ValidateWater(decimal? quantity, Guid? unitId)
    {
        if (quantity.HasValue && quantity.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Water quantity must be greater than zero.");
        }

        if (quantity.HasValue && !unitId.HasValue)
        {
            throw new ArgumentException("A water unit is required when water quantity is specified.", nameof(unitId));
        }

        if (!quantity.HasValue && unitId.HasValue)
        {
            throw new ArgumentException("A water quantity is required when water unit is specified.", nameof(quantity));
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
