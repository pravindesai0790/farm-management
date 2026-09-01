using FarmManagement.Application.Interfaces.Users;
using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FarmManagement.Infrastructure.Persistence;

public sealed class UserAdministrationStore(ApplicationDbContext dbContext) : IUserAdministrationStore
{
    public Task<int> CountUsersAsync(
        Guid? organizationId,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken = default) =>
        BuildUserQuery(organizationId, search, isActive).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<User>> ListUsersAsync(
        Guid? organizationId,
        int skip,
        int take,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken = default) =>
        await BuildUserQuery(organizationId, search, isActive)
            .AsNoTracking()
            .OrderBy(user => user.LastName)
            .ThenBy(user => user.FirstName)
            .ThenBy(user => user.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task<User?> FindUserAsync(
        Guid userId,
        Guid? organizationId,
        CancellationToken cancellationToken = default)
    {
        var query = BuildUserQuery(organizationId, search: null, isActive: null);
        return query.SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);
    }

    public Task<bool> EmailExistsAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default) =>
        dbContext.Users.AnyAsync(
            user => user.Email.ToLower() == normalizedEmail,
            cancellationToken);

    public Task<Organization?> FindOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.Organizations.SingleOrDefaultAsync(
            organization => organization.Id == organizationId,
            cancellationToken);

    public async Task<IReadOnlyList<Role>> FindActiveRolesAsync(
        IReadOnlyCollection<Guid> roleIds,
        CancellationToken cancellationToken = default) =>
        await dbContext.Roles
            .Where(role => role.IsActive && roleIds.Contains(role.Id))
            .ToListAsync(cancellationToken);

    public void AddUser(User user) => dbContext.Users.Add(user);

    public void AddAuditLog(AuditLog auditLog) => dbContext.AuditLogs.Add(auditLog);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);

    private IQueryable<User> BuildUserQuery(
        Guid? organizationId,
        string? search,
        bool? isActive)
    {
        IQueryable<User> query = dbContext.Users
            .AsSplitQuery()
            .Include(user => user.Organization)
            .Include(user => user.UserRoles)
                .ThenInclude(userRole => userRole.Role);

        if (organizationId is not null)
        {
            query = query.Where(user => user.OrganizationId == organizationId.Value);
        }

        if (isActive is not null)
        {
            query = query.Where(user => user.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLowerInvariant();
            query = query.Where(user =>
                user.FirstName.ToLower().Contains(normalizedSearch) ||
                user.LastName.ToLower().Contains(normalizedSearch) ||
                user.Email.ToLower().Contains(normalizedSearch));
        }

        return query;
    }
}
