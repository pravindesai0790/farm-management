using FarmManagement.Application.Interfaces.Labor;
using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FarmManagement.Infrastructure.Persistence;

public sealed class ContractorStore(ApplicationDbContext dbContext) : IContractorStore
{
    public Task<int> CountAsync(
        Guid organizationId,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken = default) =>
        BuildQuery(organizationId, search, isActive).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<(Contractor Contractor, int WorkerCount)>> ListAsync(
        Guid organizationId,
        int skip,
        int take,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        var rawList = await BuildQuery(organizationId, search, isActive)
            .AsNoTracking()
            .OrderBy(contractor => contractor.Name)
            .ThenBy(contractor => contractor.Id)
            .Skip(skip)
            .Take(take)
            .Select(c => new
            {
                Contractor = c,
                WorkerCount = dbContext.Workers.Count(w => w.ContractorId == c.Id && w.OrganizationId == organizationId)
            })
            .ToListAsync(cancellationToken);

        return rawList.Select(x => (x.Contractor, x.WorkerCount)).ToArray();
    }

    public Task<Contractor?> FindAsync(
        Guid contractorId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.Contractors.SingleOrDefaultAsync(
            contractor => contractor.Id == contractorId && contractor.OrganizationId == organizationId,
            cancellationToken);

    public Task<int> GetWorkerCountAsync(
        Guid contractorId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.Workers.CountAsync(
            worker => worker.ContractorId == contractorId && worker.OrganizationId == organizationId,
            cancellationToken);

    public Task<bool> NameExistsAsync(
        Guid organizationId,
        string name,
        Guid? excludingContractorId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim().ToLower();
        var query = dbContext.Contractors.Where(c =>
            c.OrganizationId == organizationId &&
            c.Name.ToLower() == normalizedName);

        if (excludingContractorId.HasValue)
        {
            query = query.Where(c => c.Id != excludingContractorId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public void Add(Contractor contractor) => dbContext.Contractors.Add(contractor);

    public void AddAuditLog(AuditLog auditLog) => dbContext.AuditLogs.Add(auditLog);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);

    private IQueryable<Contractor> BuildQuery(Guid organizationId, string? search, bool? isActive)
    {
        var query = dbContext.Contractors.Where(c => c.OrganizationId == organizationId);

        if (isActive is not null)
        {
            query = query.Where(c => c.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(normalizedSearch) ||
                (c.ContactPerson != null && c.ContactPerson.ToLower().Contains(normalizedSearch)) ||
                (c.PhoneNumber != null && c.PhoneNumber.ToLower().Contains(normalizedSearch)) ||
                (c.Email != null && c.Email.ToLower().Contains(normalizedSearch)));
        }

        return query;
    }
}
