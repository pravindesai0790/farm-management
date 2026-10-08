namespace FarmManagement.Domain.Entities;

public sealed class SprayProduct
{
    private SprayProduct()
    {
    }

    public SprayProduct(
        Guid sprayId,
        Guid inventoryItemId,
        Guid createdBy,
        Guid? storageLocationId = null,
        decimal? plannedQuantity = null,
        decimal? actualQuantity = null,
        string? dosage = null)
    {
        if (sprayId == Guid.Empty)
        {
            throw new ArgumentException("A spray is required.", nameof(sprayId));
        }

        if (inventoryItemId == Guid.Empty)
        {
            throw new ArgumentException("An inventory item is required.", nameof(inventoryItemId));
        }

        if (createdBy == Guid.Empty)
        {
            throw new ArgumentException("A creating user is required.", nameof(createdBy));
        }

        if (plannedQuantity.HasValue && plannedQuantity.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(plannedQuantity), "Planned quantity must be greater than zero.");
        }

        if (actualQuantity.HasValue && actualQuantity.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(actualQuantity), "Actual quantity must be greater than zero.");
        }

        Id = Guid.NewGuid();
        SprayId = sprayId;
        InventoryItemId = inventoryItemId;
        StorageLocationId = storageLocationId;
        PlannedQuantity = plannedQuantity;
        ActualQuantity = actualQuantity;
        Dosage = NormalizeOptional(dosage);
        CreatedAt = DateTimeOffset.UtcNow;
        CreatedBy = createdBy;
    }

    public Guid Id { get; private set; }
    public Guid SprayId { get; private set; }
    public Guid InventoryItemId { get; private set; }
    public Guid? StorageLocationId { get; private set; }
    public decimal? PlannedQuantity { get; private set; }
    public decimal? ActualQuantity { get; private set; }
    public string? Dosage { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public Spray? Spray { get; private set; }
    public InventoryItem? InventoryItem { get; private set; }
    public StorageLocation? StorageLocation { get; private set; }

    public void UpdateDraft(decimal? plannedQuantity, string? dosage, DateTimeOffset now, Guid updatedBy)
    {
        if (plannedQuantity.HasValue && plannedQuantity.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(plannedQuantity), "Planned quantity must be greater than zero.");
        }

        if (updatedBy == Guid.Empty)
        {
            throw new ArgumentException("An updating user is required.", nameof(updatedBy));
        }

        PlannedQuantity = plannedQuantity;
        Dosage = NormalizeOptional(dosage);
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    public void SetExecution(Guid storageLocationId, decimal actualQuantity, string? dosage, DateTimeOffset now, Guid updatedBy)
    {
        if (storageLocationId == Guid.Empty)
        {
            throw new ArgumentException("A storage location is required.", nameof(storageLocationId));
        }

        if (actualQuantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(actualQuantity), "Actual quantity must be greater than zero.");
        }

        if (updatedBy == Guid.Empty)
        {
            throw new ArgumentException("An updating user is required.", nameof(updatedBy));
        }

        StorageLocationId = storageLocationId;
        ActualQuantity = actualQuantity;
        Dosage = NormalizeOptional(dosage);
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    public void UpdateActualQuantityAndDosage(decimal actualQuantity, string? dosage, DateTimeOffset now, Guid updatedBy)
    {
        if (actualQuantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(actualQuantity), "Actual quantity must be greater than zero.");
        }

        if (updatedBy == Guid.Empty)
        {
            throw new ArgumentException("An updating user is required.", nameof(updatedBy));
        }

        ActualQuantity = actualQuantity;
        Dosage = NormalizeOptional(dosage);
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
