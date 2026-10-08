namespace FarmManagement.Domain.Entities;

public sealed class ProductType
{
    private ProductType()
    {
        Name = string.Empty;
        Code = string.Empty;
    }

    public ProductType(
        Guid? organizationId,
        string code,
        string name,
        bool isSystem = false,
        string? description = null,
        int displayOrder = 0,
        Guid? createdBy = null)
    {
        if (isSystem && organizationId is not null)
        {
            throw new ArgumentException("A system product type cannot belong to an organization.", nameof(organizationId));
        }

        if (!isSystem && organizationId is null)
        {
            throw new ArgumentException("An organization is required for an organization-specific product type.", nameof(organizationId));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("A product type code is required.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A product type name is required.", nameof(name));
        }

        if (displayOrder < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(displayOrder), "Display order cannot be negative.");
        }

        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        DisplayOrder = displayOrder;
        IsSystem = isSystem;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
        CreatedBy = createdBy;
    }

    public Guid Id { get; private set; }
    public Guid? OrganizationId { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsSystem { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public Organization? Organization { get; private set; }

    public void Update(string name, string? description, int displayOrder, DateTimeOffset now, Guid updatedBy)
    {
        if (IsSystem)
        {
            throw new InvalidOperationException("System product types cannot be modified.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A product type name is required.", nameof(name));
        }

        if (displayOrder < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(displayOrder), "Display order cannot be negative.");
        }

        if (updatedBy == Guid.Empty)
        {
            throw new ArgumentException("A user is required.", nameof(updatedBy));
        }

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        DisplayOrder = displayOrder;
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
}
