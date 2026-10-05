namespace FarmManagement.Domain.Entities;

public sealed class InventoryItem
{
    private InventoryItem()
    {
        Name = string.Empty;
    }

    public InventoryItem(
        Guid organizationId,
        string name,
        Guid stockUnitId,
        Guid createdBy,
        string? sku = null,
        string? description = null,
        Guid? categoryId = null)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("An organization is required.", nameof(organizationId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("An inventory item name is required.", nameof(name));
        }

        if (stockUnitId == Guid.Empty)
        {
            throw new ArgumentException("A stock unit is required.", nameof(stockUnitId));
        }

        if (createdBy == Guid.Empty)
        {
            throw new ArgumentException("A creating user is required.", nameof(createdBy));
        }

        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        Name = name.Trim();
        StockUnitId = stockUnitId;
        Sku = NormalizeOptional(sku);
        Description = NormalizeOptional(description);
        CategoryId = categoryId;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
        CreatedBy = createdBy;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Name { get; private set; }
    public string? Sku { get; private set; }
    public string? Description { get; private set; }
    public Guid? CategoryId { get; private set; }
    public Guid StockUnitId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public Organization? Organization { get; private set; }
    public Unit? StockUnit { get; private set; }
    public InventoryItemCategory? Category { get; private set; }

    public void Update(
        string name,
        Guid stockUnitId,
        string? sku,
        string? description,
        Guid? categoryId,
        DateTimeOffset now,
        Guid updatedBy)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("An inventory item name is required.", nameof(name));
        }

        if (stockUnitId == Guid.Empty)
        {
            throw new ArgumentException("A stock unit is required.", nameof(stockUnitId));
        }

        if (updatedBy == Guid.Empty)
        {
            throw new ArgumentException("An updating user is required.", nameof(updatedBy));
        }

        Name = name.Trim();
        StockUnitId = stockUnitId;
        Sku = NormalizeOptional(sku);
        Description = NormalizeOptional(description);
        CategoryId = categoryId;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    public bool Activate(DateTimeOffset now, Guid updatedBy)
    {
        if (updatedBy == Guid.Empty)
        {
            throw new ArgumentException("A user is required.", nameof(updatedBy));
        }

        if (IsActive)
        {
            return false;
        }

        IsActive = true;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
        return true;
    }

    public bool Deactivate(DateTimeOffset now, Guid updatedBy)
    {
        if (updatedBy == Guid.Empty)
        {
            throw new ArgumentException("A user is required.", nameof(updatedBy));
        }

        if (!IsActive)
        {
            return false;
        }

        IsActive = false;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
        return true;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
