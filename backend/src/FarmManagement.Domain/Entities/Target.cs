using FarmManagement.Domain.Enums;

namespace FarmManagement.Domain.Entities;

public sealed class Target
{
    private Target()
    {
        Name = string.Empty;
        Code = string.Empty;
    }

    public Target(
        Guid? organizationId,
        string code,
        string name,
        TargetType targetType,
        bool isSystem = false,
        string? description = null,
        int displayOrder = 0,
        Guid? createdBy = null)
    {
        if (isSystem && organizationId is not null)
        {
            throw new ArgumentException("A system target cannot belong to an organization.", nameof(organizationId));
        }

        if (!isSystem && organizationId is null)
        {
            throw new ArgumentException("An organization is required for an organization-specific target.", nameof(organizationId));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("A target code is required.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A target name is required.", nameof(name));
        }

        if (!Enum.IsDefined(targetType))
        {
            throw new ArgumentOutOfRangeException(nameof(targetType), "Invalid target type.");
        }

        if (displayOrder < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(displayOrder), "Display order cannot be negative.");
        }

        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        TargetType = targetType;
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
    public TargetType TargetType { get; private set; }
    public string? Description { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsSystem { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public Organization? Organization { get; private set; }

    public void Update(string name, TargetType targetType, string? description, int displayOrder, DateTimeOffset now, Guid updatedBy)
    {
        if (IsSystem)
        {
            throw new InvalidOperationException("System targets cannot be modified.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A target name is required.", nameof(name));
        }

        if (!Enum.IsDefined(targetType))
        {
            throw new ArgumentOutOfRangeException(nameof(targetType), "Invalid target type.");
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
        TargetType = targetType;
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
