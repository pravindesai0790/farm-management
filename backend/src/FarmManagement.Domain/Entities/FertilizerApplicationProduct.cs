namespace FarmManagement.Domain.Entities;

public sealed class FertilizerApplicationProduct
{
    private FertilizerApplicationProduct()
    {
    }

    public FertilizerApplicationProduct(
        Guid fertilizerApplicationId,
        Guid inventoryItemId,
        decimal plannedQuantity,
        Guid createdBy,
        Guid? storageLocationId = null,
        decimal? actualQuantity = null,
        Guid? stockMovementId = null)
    {
        if (fertilizerApplicationId == Guid.Empty)
        {
            throw new ArgumentException("A fertilizer application identifier is required.", nameof(fertilizerApplicationId));
        }

        if (inventoryItemId == Guid.Empty)
        {
            throw new ArgumentException("An inventory item is required.", nameof(inventoryItemId));
        }

        if (plannedQuantity < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(plannedQuantity), "Planned quantity cannot be negative.");
        }

        if (actualQuantity.HasValue && actualQuantity.Value < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(actualQuantity), "Actual quantity cannot be negative.");
        }

        if (createdBy == Guid.Empty)
        {
            throw new ArgumentException("A creating user is required.", nameof(createdBy));
        }

        Id = Guid.NewGuid();
        FertilizerApplicationId = fertilizerApplicationId;
        InventoryItemId = inventoryItemId;
        StorageLocationId = storageLocationId;
        PlannedQuantity = plannedQuantity;
        ActualQuantity = actualQuantity;
        StockMovementId = stockMovementId;
        CreatedAt = DateTimeOffset.UtcNow;
        CreatedBy = createdBy;
    }

    public Guid Id { get; private set; }
    public Guid FertilizerApplicationId { get; private set; }
    public Guid InventoryItemId { get; private set; }
    public Guid? StorageLocationId { get; private set; }
    public decimal PlannedQuantity { get; private set; }
    public decimal? ActualQuantity { get; private set; }
    public Guid? StockMovementId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public FertilizerApplication? FertilizerApplication { get; private set; }
    public InventoryItem? InventoryItem { get; private set; }
    public StorageLocation? StorageLocation { get; private set; }
    public StockMovement? StockMovement { get; private set; }

    public void UpdatePlanned(decimal plannedQuantity, Guid? storageLocationId, DateTimeOffset now, Guid updatedBy)
    {
        if (plannedQuantity < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(plannedQuantity), "Planned quantity cannot be negative.");
        }

        if (updatedBy == Guid.Empty)
        {
            throw new ArgumentException("An updating user is required.", nameof(updatedBy));
        }

        PlannedQuantity = plannedQuantity;
        StorageLocationId = storageLocationId;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    public void SetStorageLocation(Guid storageLocationId, DateTimeOffset now, Guid updatedBy)
    {
        if (storageLocationId == Guid.Empty)
        {
            throw new ArgumentException("A valid storage location is required.", nameof(storageLocationId));
        }

        if (updatedBy == Guid.Empty)
        {
            throw new ArgumentException("An updating user is required.", nameof(updatedBy));
        }

        StorageLocationId = storageLocationId;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    public void RecordActual(decimal actualQuantity, Guid? stockMovementId, DateTimeOffset now, Guid updatedBy)
    {
        if (actualQuantity < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(actualQuantity), "Actual quantity cannot be negative.");
        }

        if (updatedBy == Guid.Empty)
        {
            throw new ArgumentException("An updating user is required.", nameof(updatedBy));
        }

        ActualQuantity = actualQuantity;
        if (stockMovementId.HasValue && stockMovementId.Value != Guid.Empty)
        {
            StockMovementId = stockMovementId.Value;
        }

        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }
}
