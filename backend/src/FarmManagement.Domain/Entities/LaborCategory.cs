namespace FarmManagement.Domain.Entities;

public sealed class LaborCategory
{
    private LaborCategory()
    {
        Name = string.Empty;
        Workers = [];
    }

    public LaborCategory(
        Guid? organizationId,
        string name,
        bool isSystem = false,
        string? description = null,
        Guid? createdBy = null)
    {
        if (isSystem != (organizationId is null))
        {
            throw new ArgumentException(
                "System labor categories cannot belong to an organization and organization labor categories require an organization.",
                nameof(organizationId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A labor category name is required.", nameof(name));
        }

        if (!isSystem && (createdBy is null || createdBy == Guid.Empty))
        {
            throw new ArgumentException("A creating user is required.", nameof(createdBy));
        }

        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        IsSystem = isSystem;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
        CreatedBy = createdBy;
        Workers = [];
    }

    public Guid Id { get; private set; }
    public Guid? OrganizationId { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public bool IsSystem { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public Organization? Organization { get; private set; }
    public ICollection<Worker> Workers { get; private set; }

    public void Update(
        string name,
        string? description,
        DateTimeOffset now,
        Guid? updatedBy = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A labor category name is required.", nameof(name));
        }

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    public bool Activate(DateTimeOffset now, Guid? updatedBy = null)
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

    public bool Deactivate(DateTimeOffset now, Guid? updatedBy = null)
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
