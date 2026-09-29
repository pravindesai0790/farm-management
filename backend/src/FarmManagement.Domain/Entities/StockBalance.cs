namespace FarmManagement.Domain.Entities;

public sealed class StockBalance
{
    private StockBalance()
    {
    }

    public StockBalance(
        Guid organizationId,
        Guid farmId,
        Guid storageLocationId,
        Guid inventoryItemId,
        decimal initialQuantity = 0m)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("An organization is required.", nameof(organizationId));
        }

        if (farmId == Guid.Empty)
        {
            throw new ArgumentException("A farm is required.", nameof(farmId));
        }

        if (storageLocationId == Guid.Empty)
        {
            throw new ArgumentException("A storage location is required.", nameof(storageLocationId));
        }

        if (inventoryItemId == Guid.Empty)
        {
            throw new ArgumentException("An inventory item is required.", nameof(inventoryItemId));
        }

        if (initialQuantity < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(initialQuantity), "Stock quantity cannot be negative.");
        }

        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        FarmId = farmId;
        StorageLocationId = storageLocationId;
        InventoryItemId = inventoryItemId;
        QuantityOnHand = initialQuantity;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid StorageLocationId { get; private set; }
    public Guid InventoryItemId { get; private set; }
    public decimal QuantityOnHand { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public Organization? Organization { get; private set; }
    public Farm? Farm { get; private set; }
    public StorageLocation? StorageLocation { get; private set; }
    public InventoryItem? InventoryItem { get; private set; }

    public void AddStock(decimal quantity, DateTimeOffset now, Guid updatedBy)
    {
        if (quantity <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity to add must be positive.");
        }

        if (updatedBy == Guid.Empty)
        {
            throw new ArgumentException("A user is required.", nameof(updatedBy));
        }

        QuantityOnHand += quantity;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    public void DeductStock(decimal quantity, DateTimeOffset now, Guid updatedBy)
    {
        if (quantity <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity to deduct must be positive.");
        }

        if (updatedBy == Guid.Empty)
        {
            throw new ArgumentException("A user is required.", nameof(updatedBy));
        }

        if (QuantityOnHand < quantity)
        {
            throw new InvalidOperationException(
                $"Insufficient stock on hand ({QuantityOnHand}) to deduct {quantity}.");
        }

        QuantityOnHand -= quantity;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }
}
