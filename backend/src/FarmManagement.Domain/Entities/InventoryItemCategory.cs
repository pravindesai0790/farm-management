namespace FarmManagement.Domain.Entities;

public sealed class InventoryItemCategory
{
    private InventoryItemCategory()
    {
        Name = string.Empty;
        Code = string.Empty;
        Icon = "category";
    }

    public InventoryItemCategory(
        Guid? organizationId,
        string name,
        string code,
        string? description = null,
        string? examples = null,
        string icon = "category",
        int displayOrder = 0,
        bool isSystem = false,
        Guid? createdBy = null,
        Guid? id = null)
    {
        if (isSystem != (organizationId is null))
        {
            throw new ArgumentException(
                "System inventory categories cannot belong to an organization, and organization inventory categories require an organization.",
                nameof(organizationId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("An inventory category name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("An inventory category code is required.", nameof(code));
        }

        if (!isSystem && (createdBy is null || createdBy == Guid.Empty))
        {
            throw new ArgumentException("A creating user is required for custom inventory categories.", nameof(createdBy));
        }

        Id = id ?? Guid.NewGuid();
        OrganizationId = organizationId;
        Name = name.Trim();
        Code = code.Trim().ToUpperInvariant();
        Description = NormalizeOptional(description);
        Examples = NormalizeOptional(examples);
        Icon = string.IsNullOrWhiteSpace(icon) ? "category" : icon.Trim();
        DisplayOrder = displayOrder;
        IsSystem = isSystem;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
        CreatedBy = createdBy;
    }

    public Guid Id { get; private set; }
    public Guid? OrganizationId { get; private set; }
    public string Name { get; private set; }
    public string Code { get; private set; }
    public string? Description { get; private set; }
    public string? Examples { get; private set; }
    public string Icon { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsSystem { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public Organization? Organization { get; private set; }

    public void Update(
        string name,
        string? description,
        string? examples,
        string icon,
        int displayOrder,
        Guid updatedBy,
        DateTimeOffset? now = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("An inventory category name is required.", nameof(name));
        }

        if (updatedBy == Guid.Empty)
        {
            throw new ArgumentException("An updating user is required.", nameof(updatedBy));
        }

        Name = name.Trim();
        Description = NormalizeOptional(description);
        Examples = NormalizeOptional(examples);
        Icon = string.IsNullOrWhiteSpace(icon) ? "category" : icon.Trim();
        DisplayOrder = displayOrder;
        UpdatedAt = now ?? DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }

    public bool Deactivate(Guid updatedBy, DateTimeOffset? now = null)
    {
        if (!IsActive) return false;
        if (updatedBy == Guid.Empty) throw new ArgumentException("An updating user is required.", nameof(updatedBy));

        IsActive = false;
        UpdatedAt = now ?? DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
        return true;
    }

    public bool Activate(Guid updatedBy, DateTimeOffset? now = null)
    {
        if (IsActive) return false;
        if (updatedBy == Guid.Empty) throw new ArgumentException("An updating user is required.", nameof(updatedBy));

        IsActive = true;
        UpdatedAt = now ?? DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
        return true;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
