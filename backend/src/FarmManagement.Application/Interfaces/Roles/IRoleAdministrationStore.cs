using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Interfaces.Roles;

public interface IRoleAdministrationStore
{
    Task<IReadOnlyList<Role>> ListRolesAsync(
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<Role?> FindRoleAsync(
        Guid roleId,
        CancellationToken cancellationToken = default);

    Task<bool> RoleNameExistsAsync(
        string normalizedName,
        Guid? excludingRoleId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Permission>> ListPermissionsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Permission>> FindPermissionsAsync(
        IReadOnlyCollection<Guid> permissionIds,
        CancellationToken cancellationToken = default);

    void AddRole(Role role);

    void AddAuditLog(AuditLog auditLog);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
