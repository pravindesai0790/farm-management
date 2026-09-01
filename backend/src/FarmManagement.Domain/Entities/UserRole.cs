namespace FarmManagement.Domain.Entities;

public sealed class UserRole
{
    private UserRole()
    {
    }

    public UserRole(
        Guid userId,
        Guid roleId,
        DateTimeOffset assignedAt,
        Guid? assignedBy = null,
        Role? role = null)
    {
        UserId = userId;
        RoleId = roleId;
        AssignedAt = assignedAt;
        AssignedBy = assignedBy;
        Role = role!;
    }

    public Guid UserId { get; private set; }

    public Guid RoleId { get; private set; }

    public DateTimeOffset AssignedAt { get; private set; }

    public Guid? AssignedBy { get; private set; }

    public User User { get; private set; } = null!;

    public Role Role { get; private set; } = null!;
}
