using FarmManagement.Application.Interfaces.Labor;
using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FarmManagement.Infrastructure.Persistence;

public sealed class LaborCategoryStore(ApplicationDbContext dbContext) : ILaborCategoryStore
{
    public Task<int> CountAsync(
        Guid organizationId,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken = default) =>
        BuildQuery(organizationId, search, isActive).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<(LaborCategory Category, int WorkerCount)>> ListAsync(
        Guid organizationId,
        int skip,
        int take,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        var rawList = await BuildQuery(organizationId, search, isActive)
            .AsNoTracking()
            .OrderByDescending(category => category.IsSystem)
            .ThenBy(category => category.Name)
            .ThenBy(category => category.Id)
            .Skip(skip)
            .Take(take)
            .Select(c => new
            {
                Category = c,
                WorkerCount = dbContext.Workers.Count(w => w.LaborCategoryId == c.Id && w.OrganizationId == organizationId)
            })
            .ToListAsync(cancellationToken);

        return rawList.Select(x => (x.Category, x.WorkerCount)).ToArray();
    }

    public Task<LaborCategory?> FindAsync(
        Guid categoryId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.LaborCategories.SingleOrDefaultAsync(
            category => category.Id == categoryId &&
                        (category.IsSystem || category.OrganizationId == organizationId),
            cancellationToken);

    public Task<int> GetWorkerCountAsync(
        Guid categoryId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.Workers.CountAsync(
            worker => worker.LaborCategoryId == categoryId && worker.OrganizationId == organizationId,
            cancellationToken);

    public Task<bool> NameExistsAsync(
        Guid organizationId,
        string name,
        Guid? excludingCategoryId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim().ToLower();
        var query = dbContext.LaborCategories.Where(category =>
            (category.IsSystem || category.OrganizationId == organizationId) &&
            category.Name.ToLower() == normalizedName);

        if (excludingCategoryId.HasValue)
        {
            query = query.Where(category => category.Id != excludingCategoryId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public void Add(LaborCategory category) => dbContext.LaborCategories.Add(category);

    public void AddAuditLog(AuditLog auditLog) => dbContext.AuditLogs.Add(auditLog);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);

    private IQueryable<LaborCategory> BuildQuery(Guid organizationId, string? search, bool? isActive)
    {
        var query = dbContext.LaborCategories.Where(category =>
            category.IsSystem || category.OrganizationId == organizationId);

        if (isActive is not null)
        {
            query = query.Where(category => category.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLower();
            query = query.Where(category =>
                category.Name.ToLower().Contains(normalizedSearch) ||
                (category.Description != null && category.Description.ToLower().Contains(normalizedSearch)));
        }

        return query;
    }
}
