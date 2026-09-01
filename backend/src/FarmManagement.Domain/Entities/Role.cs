namespace FarmManagement.Domain.Entities;

public sealed class Role
{
    private Role()
    {
        Name = string.Empty;
        UserRoles = [];
        RolePermissions = [];
    }

    public Role(string name, string? description = null, bool isSystemRole = false)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A role name is required.", nameof(name));
        }

        Id = Guid.NewGuid();
        Name = name.Trim();
        Description = description?.Trim();
        IsSystemRole = isSystemRole;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
        UserRoles = [];
        RolePermissions = [];
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public bool IsSystemRole { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public ICollection<UserRole> UserRoles { get; private set; }

    public ICollection<RolePermission> RolePermissions { get; private set; }

    public void MarkAsSystemRole()
    {
        IsSystemRole = true;
    }

    public bool UpdateDetails(
        string name,
        string? description,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A role name is required.", nameof(name));
        }

        var normalizedName = name.Trim();
        var normalizedDescription = string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();
        var changed = !string.Equals(Name, normalizedName, StringComparison.Ordinal) ||
            !string.Equals(Description, normalizedDescription, StringComparison.Ordinal);

        if (changed)
        {
            Name = normalizedName;
            Description = normalizedDescription;
            UpdatedAt = now;
        }

        return changed;
    }

    public bool Activate(DateTimeOffset now)
    {
        if (IsActive)
        {
            return false;
        }

        IsActive = true;
        UpdatedAt = now;
        return true;
    }

    public bool Deactivate(DateTimeOffset now)
    {
        if (!IsActive)
        {
            return false;
        }

        IsActive = false;
        UpdatedAt = now;
        return true;
    }
}
