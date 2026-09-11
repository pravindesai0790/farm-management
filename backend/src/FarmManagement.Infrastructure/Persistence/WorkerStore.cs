using FarmManagement.Application.Interfaces.Labor;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FarmManagement.Infrastructure.Persistence;

public sealed class WorkerStore(ApplicationDbContext dbContext) : IWorkerStore
{
    public Task<int> CountAsync(
        Guid organizationId,
        string? search,
        bool? isActive,
        Guid? contractorId,
        Guid? laborCategoryId,
        Gender? gender,
        EmploymentType? employmentType,
        CancellationToken cancellationToken = default) =>
        BuildQuery(organizationId, search, isActive, contractorId, laborCategoryId, gender, employmentType)
            .CountAsync(cancellationToken);

    public async Task<IReadOnlyList<Worker>> ListAsync(
        Guid organizationId,
        int skip,
        int take,
        string? search,
        bool? isActive,
        Guid? contractorId,
        Guid? laborCategoryId,
        Gender? gender,
        EmploymentType? employmentType,
        CancellationToken cancellationToken = default) =>
        await BuildQuery(organizationId, search, isActive, contractorId, laborCategoryId, gender, employmentType)
            .AsNoTracking()
            .OrderBy(worker => worker.DisplayName)
            .ThenBy(worker => worker.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task<Worker?> FindAsync(
        Guid workerId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.Workers
            .Include(worker => worker.Contractor)
            .Include(worker => worker.LaborCategory)
            .SingleOrDefaultAsync(
                worker => worker.Id == workerId && worker.OrganizationId == organizationId,
                cancellationToken);

    public Task<Contractor?> FindContractorAsync(
        Guid contractorId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.Contractors.SingleOrDefaultAsync(
            contractor => contractor.Id == contractorId && contractor.OrganizationId == organizationId,
            cancellationToken);

    public Task<LaborCategory?> FindLaborCategoryAsync(
        Guid laborCategoryId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.LaborCategories.SingleOrDefaultAsync(
            category => category.Id == laborCategoryId &&
                        (category.IsSystem || category.OrganizationId == organizationId),
            cancellationToken);

    public void Add(Worker worker) => dbContext.Workers.Add(worker);

    public void AddAuditLog(AuditLog auditLog) => dbContext.AuditLogs.Add(auditLog);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);

    private IQueryable<Worker> BuildQuery(
        Guid organizationId,
        string? search,
        bool? isActive,
        Guid? contractorId,
        Guid? laborCategoryId,
        Gender? gender,
        EmploymentType? employmentType)
    {
        var query = dbContext.Workers
            .Include(worker => worker.Contractor)
            .Include(worker => worker.LaborCategory)
            .Where(worker => worker.OrganizationId == organizationId);

        if (isActive is not null)
        {
            query = query.Where(worker => worker.IsActive == isActive.Value);
        }

        if (contractorId is not null)
        {
            query = query.Where(worker => worker.ContractorId == contractorId.Value);
        }

        if (laborCategoryId is not null)
        {
            query = query.Where(worker => worker.LaborCategoryId == laborCategoryId.Value);
        }

        if (gender is not null)
        {
            query = query.Where(worker => worker.Gender == gender.Value);
        }

        if (employmentType is not null)
        {
            query = query.Where(worker => worker.EmploymentType == employmentType.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLower();
            query = query.Where(worker =>
                worker.DisplayName.ToLower().Contains(normalizedSearch) ||
                worker.FirstName.ToLower().Contains(normalizedSearch) ||
                (worker.LastName != null && worker.LastName.ToLower().Contains(normalizedSearch)) ||
                (worker.MobileNumber != null && worker.MobileNumber.ToLower().Contains(normalizedSearch)));
        }

        return query;
    }
}
