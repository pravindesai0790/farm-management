using FarmManagement.Application.Interfaces.Roles;
using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FarmManagement.Infrastructure.Persistence;

public sealed class RoleAdministrationStore(ApplicationDbContext dbContext) : IRoleAdministrationStore
{
    public async Task<IReadOnlyList<Role>> ListRolesAsync(
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        var query = BuildRoleQuery();
        if (isActive is not null)
        {
            query = query.Where(role => role.IsActive == isActive.Value);
        }

        return await query
            .AsNoTracking()
            .OrderBy(role => role.Name)
            .ThenBy(role => role.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<Role?> FindRoleAsync(
        Guid roleId,
        CancellationToken cancellationToken = default) =>
        BuildRoleQuery().SingleOrDefaultAsync(role => role.Id == roleId, cancellationToken);

    public Task<bool> RoleNameExistsAsync(
        string normalizedName,
        Guid? excludingRoleId = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Roles.Where(role => role.Name.ToLower() == normalizedName.ToLower());
        if (excludingRoleId is not null)
        {
            query = query.Where(role => role.Id != excludingRoleId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Permission>> ListPermissionsAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.Permissions
            .AsNoTracking()
            .OrderBy(permission => permission.Module)
            .ThenBy(permission => permission.Name)
            .ThenBy(permission => permission.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Permission>> FindPermissionsAsync(
        IReadOnlyCollection<Guid> permissionIds,
        CancellationToken cancellationToken = default) =>
        await dbContext.Permissions
            .Where(permission => permissionIds.Contains(permission.Id))
            .ToListAsync(cancellationToken);

    public void AddRole(Role role) => dbContext.Roles.Add(role);

    public void AddAuditLog(AuditLog auditLog) => dbContext.AuditLogs.Add(auditLog);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);

    private IQueryable<Role> BuildRoleQuery() =>
        dbContext.Roles
            .AsSplitQuery()
            .Include(role => role.RolePermissions)
                .ThenInclude(rolePermission => rolePermission.Permission);
}
