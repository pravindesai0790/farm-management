namespace FarmManagement.Domain.Entities;

public sealed class Permission
{
    private Permission()
    {
        Name = string.Empty;
        Module = string.Empty;
        RolePermissions = [];
    }

    public Permission(string name, string module, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A permission name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(module))
        {
            throw new ArgumentException("A permission module is required.", nameof(module));
        }

        Id = Guid.NewGuid();
        Name = name.Trim();
        Description = description?.Trim();
        Module = module.Trim();
        CreatedAt = DateTimeOffset.UtcNow;
        RolePermissions = [];
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public string Module { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public ICollection<RolePermission> RolePermissions { get; private set; }
}
