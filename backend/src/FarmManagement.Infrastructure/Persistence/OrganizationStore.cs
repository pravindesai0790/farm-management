using FarmManagement.Application.Interfaces.Organizations;
using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FarmManagement.Infrastructure.Persistence;

public sealed class OrganizationStore(ApplicationDbContext dbContext) : IOrganizationStore
{
    public async Task<IReadOnlyList<Organization>> ListAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.Organizations
            .AsNoTracking()
            .OrderBy(organization => organization.Name)
            .ThenBy(organization => organization.Id)
            .ToListAsync(cancellationToken);

    public Task<Organization?> FindAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.Organizations
            .SingleOrDefaultAsync(
                organization => organization.Id == organizationId,
                cancellationToken);

    public Task<bool> CodeExistsAsync(
        string code,
        Guid? excludingOrganizationId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToLowerInvariant();
        var query = dbContext.Organizations
            .Where(organization => organization.Code.ToLower() == normalizedCode);
        if (excludingOrganizationId is not null)
        {
            query = query.Where(organization => organization.Id != excludingOrganizationId.Value);
        }

        return query.AnyAsync(
            cancellationToken);
    }

    public void Add(Organization organization) => dbContext.Organizations.Add(organization);

    public void AddAuditLog(AuditLog auditLog) => dbContext.AuditLogs.Add(auditLog);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
