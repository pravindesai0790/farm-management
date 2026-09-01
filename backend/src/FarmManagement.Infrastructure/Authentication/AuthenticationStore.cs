using FarmManagement.Application.Interfaces.Authentication;
using FarmManagement.Domain.Entities;
using FarmManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FarmManagement.Infrastructure.Authentication;

public sealed class AuthenticationStore(ApplicationDbContext dbContext) : IAuthenticationStore
{
    public Task<User?> FindUserByEmailAsync(
        string email,
        CancellationToken cancellationToken = default) =>
        dbContext.Users
            .AsSplitQuery()
            .Include(user => user.Organization)
            .Include(user => user.UserRoles)
                .ThenInclude(userRole => userRole.Role)
                    .ThenInclude(role => role.RolePermissions)
                        .ThenInclude(rolePermission => rolePermission.Permission)
            .SingleOrDefaultAsync(user => user.Email == email, cancellationToken);

    public Task<User?> FindUserByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        dbContext.Users
            .AsSplitQuery()
            .Include(user => user.Organization)
            .Include(user => user.UserRoles)
                .ThenInclude(userRole => userRole.Role)
                    .ThenInclude(role => role.RolePermissions)
                        .ThenInclude(rolePermission => rolePermission.Permission)
            .SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);

    public void AddAuditLog(AuditLog auditLog)
    {
        dbContext.AuditLogs.Add(auditLog);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
