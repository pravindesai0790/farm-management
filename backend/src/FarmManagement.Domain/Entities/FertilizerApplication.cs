using FarmManagement.Domain.Enums;

namespace FarmManagement.Domain.Entities;

public sealed class FertilizerApplication
{
    private readonly List<FertilizerApplicationProduct> _products = [];

    private FertilizerApplication()
    {
        ReferenceNumber = string.Empty;
    }

    public FertilizerApplication(
        Guid organizationId,
        string referenceNumber,
        Guid createdBy,
        Guid? farmId = null,
        Guid? farmAreaId = null,
        Guid? plantationId = null,
        Guid? cropCycleId = null,
        Guid? cropCycleStageId = null,
        Guid? applicationMethodId = null,
        string? otherMethodDescription = null,
        Guid? irrigationEventId = null,
        DateOnly? plannedDate = null,
        DateOnly? scheduledDate = null,
        decimal? plannedArea = null,
        Guid? plannedAreaUnitId = null,
        string? purposeReason = null,
        string? idempotencyKey = null)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("An organization is required.", nameof(organizationId));
        }

        if (string.IsNullOrWhiteSpace(referenceNumber))
        {
            throw new ArgumentException("A reference number is required.", nameof(referenceNumber));
        }

        if (createdBy == Guid.Empty)
        {
            throw new ArgumentException("A creating user is required.", nameof(createdBy));
        }

        ValidateArea(plannedArea, plannedAreaUnitId, nameof(plannedArea), nameof(plannedAreaUnitId));

        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        ReferenceNumber = referenceNumber.Trim().ToUpperInvariant();
        Status = FertilizerApplicationStatus.Draft;
        FarmId = farmId;
        FarmAreaId = farmAreaId;
        PlantationId = plantationId;
        CropCycleId = cropCycleId;
        CropCycleStageId = cropCycleStageId;
        ApplicationMethodId = applicationMethodId;
        OtherMethodDescription = NormalizeOptional(otherMethodDescription);
        IrrigationEventId = irrigationEventId;
        PlannedDate = plannedDate;
        ScheduledDate = scheduledDate;
        PlannedArea = plannedArea;
        PlannedAreaUnitId = plannedAreaUnitId;
        PurposeReason = NormalizeOptional(purposeReason);
        IdempotencyKey = NormalizeOptional(idempotencyKey);
        Version = 1;
        CreatedAt = DateTimeOffset.UtcNow;
        CreatedBy = createdBy;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string ReferenceNumber { get; private set; }
    public FertilizerApplicationStatus Status { get; private set; }
    public Guid? FarmId { get; private set; }
    public Guid? FarmAreaId { get; private set; }
    public Guid? PlantationId { get; private set; }
    public Guid? CropCycleId { get; private set; }
    public Guid? CropCycleStageId { get; private set; }
    public Guid? ApplicationMethodId { get; private set; }
    public string? OtherMethodDescription { get; private set; }
    public Guid? IrrigationEventId { get; private set; }
    public string? PurposeReason { get; private set; }
    public DateOnly? PlannedDate { get; private set; }
    public DateOnly? ScheduledDate { get; private set; }
    public DateOnly? ActualApplicationDate { get; private set; }
    public decimal? PlannedArea { get; private set; }
    public Guid? PlannedAreaUnitId { get; private set; }
    public decimal? ActualArea { get; private set; }
    public Guid? ActualAreaUnitId { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public Guid? CancelledBy { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public Guid? StartedBy { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public Guid? CompletedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public uint Version { get; private set; }
    public string? IdempotencyKey { get; private set; }

    public IReadOnlyCollection<FertilizerApplicationProduct> Products => _products.AsReadOnly();

    public Organization? Organization { get; private set; }
    public Farm? Farm { get; private set; }
    public FarmArea? FarmArea { get; private set; }
    public CropPlantation? Plantation { get; private set; }
    public CropCycle? CropCycle { get; private set; }
    public CropCycleStage? CropCycleStage { get; private set; }
    public ApplicationMethod? ApplicationMethod { get; private set; }
    public IrrigationEvent? IrrigationEvent { get; private set; }
    public Unit? PlannedAreaUnit { get; private set; }
    public Unit? ActualAreaUnit { get; private set; }

    public void AddProduct(FertilizerApplicationProduct product)
    {
        ArgumentNullException.ThrowIfNull(product);

        EnsureNotFrozen("Cannot add products after start or completion.");

        if (_products.Any(p => p.InventoryItemId == product.InventoryItemId))
        {
            throw new InvalidOperationException($"Product with InventoryItemId '{product.InventoryItemId}' already exists in this application.");
        }

        _products.Add(product);
    }

    public FertilizerApplicationProduct AddProduct(
        Guid inventoryItemId,
        decimal plannedQuantity,
        Guid createdBy,
        Guid? storageLocationId = null)
    {
        var product = new FertilizerApplicationProduct(
            Id,
            inventoryItemId,
            plannedQuantity,
            createdBy,
            storageLocationId);
        AddProduct(product);
        return product;
    }

    public static FertilizerApplication CreateEmptyDraft(
        Guid organizationId,
        string referenceNumber,
        Guid createdBy) =>
        new(organizationId, referenceNumber, createdBy);

    public void RemoveProduct(Guid inventoryItemId)
    {
        EnsureNotFrozen("Cannot remove products after start or completion.");

        var existing = _products.FirstOrDefault(p => p.InventoryItemId == inventoryItemId);
        if (existing is not null)
        {
            _products.Remove(existing);
        }
    }

    public void ClearProducts()
    {
        EnsureNotFrozen("Cannot modify products after start or completion.");
        _products.Clear();
    }

    public void UpdateDraft(
        Guid? farmId,
        Guid? farmAreaId,
        Guid? plantationId,
        Guid? cropCycleId,
        Guid? cropCycleStageId,
        Guid? applicationMethodId,
        string? otherMethodDescription,
        Guid? irrigationEventId,
        DateOnly? plannedDate,
        DateOnly? scheduledDate,
        decimal? plannedArea,
        Guid? plannedAreaUnitId,
        string? purposeReason,
        DateTimeOffset now,
        Guid updatedBy)
    {
        if (Status != FertilizerApplicationStatus.Draft)
        {
            throw new InvalidOperationException("Only draft fertilizer applications can be updated.");
        }

        if (updatedBy == Guid.Empty)
        {
            throw new ArgumentException("An updating user is required.", nameof(updatedBy));
        }

        ValidateArea(plannedArea, plannedAreaUnitId, nameof(plannedArea), nameof(plannedAreaUnitId));

        FarmId = farmId;
        FarmAreaId = farmAreaId;
        PlantationId = plantationId;
        CropCycleId = cropCycleId;
        CropCycleStageId = cropCycleStageId;
        ApplicationMethodId = applicationMethodId;
        OtherMethodDescription = NormalizeOptional(otherMethodDescription);
        IrrigationEventId = irrigationEventId;
        PlannedDate = plannedDate;
        ScheduledDate = scheduledDate;
        PlannedArea = plannedArea;
        PlannedAreaUnitId = plannedAreaUnitId;
        PurposeReason = NormalizeOptional(purposeReason);
        UpdatedAt = now;
        UpdatedBy = updatedBy;
        Version++;
    }

    public void Plan(DateTimeOffset now, Guid updatedBy)
    {
        if (Status != FertilizerApplicationStatus.Draft)
        {
            throw new InvalidOperationException("Only draft fertilizer applications can be transitioned to planned.");
        }

        if (!FarmId.HasValue || FarmId.Value == Guid.Empty)
        {
            throw new InvalidOperationException("A farm is required to plan a fertilizer application.");
        }

        if (!FarmAreaId.HasValue || FarmAreaId.Value == Guid.Empty)
        {
            throw new InvalidOperationException("A farm area is required to plan a fertilizer application.");
        }

        if (!ApplicationMethodId.HasValue || ApplicationMethodId.Value == Guid.Empty)
        {
            throw new InvalidOperationException("An application method is required to plan a fertilizer application.");
        }

        if (!PlannedDate.HasValue)
        {
            throw new InvalidOperationException("A planned date is required to plan a fertilizer application.");
        }

        if (_products.Count == 0)
        {
            throw new InvalidOperationException("At least one product line is required to plan a fertilizer application.");
        }

        foreach (var p in _products)
        {
            if (p.PlannedQuantity <= 0m)
            {
                throw new InvalidOperationException("Planned quantity must be positive for all products to plan.");
            }
        }

        Status = FertilizerApplicationStatus.Planned;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
        Version++;
    }

    public void Schedule(DateOnly scheduledDate, DateTimeOffset now, Guid updatedBy)
    {
        if (Status != FertilizerApplicationStatus.Planned && Status != FertilizerApplicationStatus.Draft)
        {
            throw new InvalidOperationException("Only draft or planned fertilizer applications can be scheduled.");
        }

        if (updatedBy == Guid.Empty)
        {
            throw new ArgumentException("An updating user is required.", nameof(updatedBy));
        }

        ScheduledDate = scheduledDate;
        Status = FertilizerApplicationStatus.Scheduled;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
        Version++;
    }

    public void Reschedule(DateOnly scheduledDate, DateTimeOffset now, Guid updatedBy)
    {
        if (Status != FertilizerApplicationStatus.Planned && Status != FertilizerApplicationStatus.Scheduled)
        {
            throw new InvalidOperationException("Only planned or scheduled applications can be rescheduled.");
        }

        if (updatedBy == Guid.Empty)
        {
            throw new ArgumentException("An updating user is required.", nameof(updatedBy));
        }

        ScheduledDate = scheduledDate;
        Status = FertilizerApplicationStatus.Scheduled;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
        Version++;
    }

    public void Start(DateTimeOffset now, Guid userId)
    {
        if (Status != FertilizerApplicationStatus.Planned && Status != FertilizerApplicationStatus.Scheduled)
        {
            throw new InvalidOperationException("Only planned or scheduled applications can be started.");
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("A user identifier is required.", nameof(userId));
        }

        if (_products.Count == 0)
        {
            throw new InvalidOperationException("At least one product is required to start.");
        }

        foreach (var p in _products)
        {
            if (!p.StorageLocationId.HasValue || p.StorageLocationId.Value == Guid.Empty)
            {
                throw new InvalidOperationException("Storage location is required for all products to start.");
            }

            if (p.PlannedQuantity <= 0m)
            {
                throw new InvalidOperationException("Planned quantity must be positive for all products to start.");
            }
        }

        Status = FertilizerApplicationStatus.InProgress;
        StartedAt = now;
        StartedBy = userId;
        UpdatedAt = now;
        UpdatedBy = userId;
        Version++;
    }

    public void Complete(
        DateOnly actualApplicationDate,
        decimal? actualArea,
        Guid? actualAreaUnitId,
        DateTimeOffset now,
        Guid userId)
    {
        if (Status != FertilizerApplicationStatus.InProgress)
        {
            throw new InvalidOperationException("Only in-progress applications can be completed.");
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("A user identifier is required.", nameof(userId));
        }

        ValidateArea(actualArea, actualAreaUnitId, nameof(actualArea), nameof(actualAreaUnitId));

        if (_products.Count == 0)
        {
            throw new InvalidOperationException("At least one product line is required to complete.");
        }

        foreach (var p in _products)
        {
            if (!p.ActualQuantity.HasValue || p.ActualQuantity.Value <= 0m)
            {
                throw new InvalidOperationException("Actual quantity must be greater than zero for all products to complete.");
            }

            if (!p.StorageLocationId.HasValue || p.StorageLocationId.Value == Guid.Empty)
            {
                throw new InvalidOperationException("A storage location is required for all products to complete.");
            }
        }

        ActualApplicationDate = actualApplicationDate;
        ActualArea = actualArea;
        ActualAreaUnitId = actualAreaUnitId;
        Status = FertilizerApplicationStatus.Completed;
        CompletedAt = now;
        CompletedBy = userId;
        UpdatedAt = now;
        UpdatedBy = userId;
        Version++;
    }

    public void Cancel(
        string reason,
        DateTimeOffset now,
        Guid userId,
        DateOnly? actualApplicationDate = null)
    {
        if (Status == FertilizerApplicationStatus.Completed || Status == FertilizerApplicationStatus.Cancelled)
        {
            throw new InvalidOperationException("Completed or already cancelled applications cannot be cancelled.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A cancellation reason is required.", nameof(reason));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("A user identifier is required.", nameof(userId));
        }

        CancellationReason = reason.Trim();
        ActualApplicationDate = actualApplicationDate;
        Status = FertilizerApplicationStatus.Cancelled;
        CancelledAt = now;
        CancelledBy = userId;
        UpdatedAt = now;
        UpdatedBy = userId;
        Version++;
    }

    public static FertilizerApplication RecordCompletedDirect(
        Guid organizationId,
        string referenceNumber,
        Guid farmId,
        Guid farmAreaId,
        Guid applicationMethodId,
        DateOnly actualApplicationDate,
        Guid createdBy,
        Guid? plantationId = null,
        Guid? cropCycleId = null,
        Guid? cropCycleStageId = null,
        string? otherMethodDescription = null,
        Guid? irrigationEventId = null,
        decimal? actualArea = null,
        Guid? actualAreaUnitId = null,
        string? purposeReason = null,
        string? idempotencyKey = null)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("An organization is required.", nameof(organizationId));
        }

        if (string.IsNullOrWhiteSpace(referenceNumber))
        {
            throw new ArgumentException("A reference number is required.", nameof(referenceNumber));
        }

        if (farmId == Guid.Empty)
        {
            throw new ArgumentException("A farm is required.", nameof(farmId));
        }

        if (farmAreaId == Guid.Empty)
        {
            throw new ArgumentException("A farm area is required.", nameof(farmAreaId));
        }

        if (applicationMethodId == Guid.Empty)
        {
            throw new ArgumentException("An application method is required.", nameof(applicationMethodId));
        }

        if (createdBy == Guid.Empty)
        {
            throw new ArgumentException("A creating user is required.", nameof(createdBy));
        }

        ValidateArea(actualArea, actualAreaUnitId, nameof(actualArea), nameof(actualAreaUnitId));

        var now = DateTimeOffset.UtcNow;
        var app = new FertilizerApplication
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ReferenceNumber = referenceNumber.Trim().ToUpperInvariant(),
            Status = FertilizerApplicationStatus.Completed,
            FarmId = farmId,
            FarmAreaId = farmAreaId,
            PlantationId = plantationId,
            CropCycleId = cropCycleId,
            CropCycleStageId = cropCycleStageId,
            ApplicationMethodId = applicationMethodId,
            OtherMethodDescription = NormalizeOptional(otherMethodDescription),
            IrrigationEventId = irrigationEventId,
            PlannedDate = null,
            ScheduledDate = null,
            ActualApplicationDate = actualApplicationDate,
            PlannedArea = null,
            PlannedAreaUnitId = null,
            ActualArea = actualArea,
            ActualAreaUnitId = actualAreaUnitId,
            PurposeReason = NormalizeOptional(purposeReason),
            IdempotencyKey = NormalizeOptional(idempotencyKey),
            CompletedAt = now,
            CompletedBy = createdBy,
            CreatedAt = now,
            CreatedBy = createdBy,
            Version = 1
        };

        return app;
    }

    private void EnsureNotFrozen(string message)
    {
        if (Status == FertilizerApplicationStatus.InProgress ||
            Status == FertilizerApplicationStatus.Completed ||
            Status == FertilizerApplicationStatus.Cancelled)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void ValidateArea(decimal? area, Guid? unitId, string areaParamName, string unitParamName)
    {
        if (area.HasValue && area.Value <= 0m)
        {
            throw new ArgumentOutOfRangeException(areaParamName, "Area must be positive.");
        }

        if (area.HasValue && !unitId.HasValue)
        {
            throw new ArgumentException("An area unit is required when area is specified.", unitParamName);
        }

        if (!area.HasValue && unitId.HasValue)
        {
            throw new ArgumentException("An area value is required when area unit is specified.", areaParamName);
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
