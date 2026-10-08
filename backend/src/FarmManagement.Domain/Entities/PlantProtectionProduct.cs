namespace FarmManagement.Domain.Entities;

public sealed class PlantProtectionProduct
{
    private PlantProtectionProduct()
    {
    }

    public PlantProtectionProduct(
        Guid organizationId,
        Guid inventoryItemId,
        Guid productTypeId,
        Guid createdBy,
        string? activeIngredient = null,
        string? manufacturer = null,
        string? description = null)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("An organization is required.", nameof(organizationId));
        }

        if (inventoryItemId == Guid.Empty)
        {
            throw new ArgumentException("An inventory item is required.", nameof(inventoryItemId));
        }

        if (productTypeId == Guid.Empty)
        {
            throw new ArgumentException("A product type is required.", nameof(productTypeId));
        }

        if (createdBy == Guid.Empty)
        {
            throw new ArgumentException("A creating user is required.", nameof(createdBy));
        }

        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        InventoryItemId = inventoryItemId;
        ProductTypeId = productTypeId;
        ActiveIngredient = NormalizeOptional(activeIngredient);
        Manufacturer = NormalizeOptional(manufacturer);
        Description = NormalizeOptional(description);
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
        CreatedBy = createdBy;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid InventoryItemId { get; private set; }
    public Guid ProductTypeId { get; private set; }
    public string? ActiveIngredient { get; private set; }
    public string? Manufacturer { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public Organization? Organization { get; private set; }
    public InventoryItem? InventoryItem { get; private set; }
    public ProductType? ProductType { get; private set; }

    public void Update(
        Guid productTypeId,
        string? activeIngredient,
        string? manufacturer,
        string? description,
        DateTimeOffset now,
        Guid updatedBy)
    {
        if (productTypeId == Guid.Empty)
        {
            throw new ArgumentException("A product type is required.", nameof(productTypeId));
        }

        if (updatedBy == Guid.Empty)
        {
            throw new ArgumentException("An updating user is required.", nameof(updatedBy));
        }

        ProductTypeId = productTypeId;
        ActiveIngredient = NormalizeOptional(activeIngredient);
        Manufacturer = NormalizeOptional(manufacturer);
        Description = NormalizeOptional(description);
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    public void UpdateProfileOnly(
        string? activeIngredient,
        string? manufacturer,
        string? description,
        DateTimeOffset now,
        Guid updatedBy)
    {
        if (updatedBy == Guid.Empty)
        {
            throw new ArgumentException("An updating user is required.", nameof(updatedBy));
        }

        ActiveIngredient = NormalizeOptional(activeIngredient);
        Manufacturer = NormalizeOptional(manufacturer);
        Description = NormalizeOptional(description);
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    public bool Activate(DateTimeOffset now, Guid updatedBy)
    {
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
