using FarmManagement.Application.DTOs.Expenses;
using FarmManagement.Application.Interfaces.Expenses;
using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FarmManagement.Infrastructure.Persistence;

public sealed class ExpenseStore(ApplicationDbContext dbContext) : IExpenseStore
{
    public Task<int> CountAsync(
        Guid organizationId,
        ExpenseFilter filter,
        CancellationToken cancellationToken = default) =>
        BuildQuery(organizationId, filter).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<Expense>> ListAsync(
        Guid organizationId,
        ExpenseFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        return await BuildQuery(organizationId, filter)
            .AsNoTracking()
            .Include(e => e.Farm)
            .Include(e => e.ExpenseCategory)
            .Include(e => e.Currency)
            .Include(e => e.Supplier)
            .Include(e => e.FarmArea)
            .Include(e => e.Plantation)
            .Include(e => e.CropCycle)
            .Include(e => e.CropCycleStage)
            .OrderByDescending(e => e.ExpenseDate)
            .ThenByDescending(e => e.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<Expense?> FindAsync(
        Guid expenseId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Expenses
            .Include(e => e.Farm)
            .Include(e => e.ExpenseCategory)
            .Include(e => e.Currency)
            .Include(e => e.Supplier)
            .Include(e => e.FarmArea)
            .Include(e => e.Plantation)
            .Include(e => e.CropCycle)
            .Include(e => e.CropCycleStage)
            .SingleOrDefaultAsync(e => e.Id == expenseId && e.OrganizationId == organizationId, cancellationToken);
    }

    public Task<bool> FarmBelongsToOrganizationAsync(
        Guid farmId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.Farms.AnyAsync(f => f.Id == farmId && f.OrganizationId == organizationId, cancellationToken);

    public Task<bool> CategoryExistsAndActiveAsync(
        Guid categoryId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.ExpenseCategories.AnyAsync(
            c => c.Id == categoryId &&
                 (c.IsSystemDefault || c.OrganizationId == organizationId) &&
                 c.IsActive,
            cancellationToken);

    public Task<bool> CurrencyExistsAndActiveAsync(
        Guid currencyId,
        CancellationToken cancellationToken = default) =>
        dbContext.Currencies.AnyAsync(c => c.Id == currencyId && c.IsActive, cancellationToken);

    public Task<bool> SupplierBelongsToOrganizationAndActiveAsync(
        Guid supplierId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.Suppliers.AnyAsync(
            s => s.Id == supplierId && s.OrganizationId == organizationId && s.IsActive,
            cancellationToken);

    public Task<bool> AreaBelongsToFarmAsync(
        Guid farmAreaId,
        Guid farmId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.FarmAreas.AnyAsync(
            a => a.Id == farmAreaId && a.FarmId == farmId && a.OrganizationId == organizationId,
            cancellationToken);

    public Task<bool> PlantationBelongsToFarmAsync(
        Guid plantationId,
        Guid farmId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.CropPlantations.AnyAsync(
            p => p.Id == plantationId && p.FarmId == farmId && p.OrganizationId == organizationId,
            cancellationToken);

    public Task<bool> CropCycleBelongsToFarmAsync(
        Guid cropCycleId,
        Guid farmId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.CropCycles.AnyAsync(
            c => c.Id == cropCycleId &&
                 c.OrganizationId == organizationId &&
                 dbContext.CropPlantations.Any(p => p.Id == c.PlantationId && p.FarmId == farmId && p.OrganizationId == organizationId),
            cancellationToken);

    public Task<bool> StageBelongsToCropCycleAsync(
        Guid cropCycleStageId,
        Guid cropCycleId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.CropCycleStages.AnyAsync(
            s => s.Id == cropCycleStageId &&
                 s.CropCycleId == cropCycleId &&
                 dbContext.CropCycles.Any(c => c.Id == cropCycleId && c.OrganizationId == organizationId),
            cancellationToken);

    public void Add(Expense expense) => dbContext.Expenses.Add(expense);

    public void AddAuditLog(AuditLog auditLog) => dbContext.AuditLogs.Add(auditLog);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);

    private IQueryable<Expense> BuildQuery(Guid organizationId, ExpenseFilter filter)
    {
        var query = dbContext.Expenses.Where(e => e.OrganizationId == organizationId);

        if (filter.From.HasValue)
        {
            query = query.Where(e => e.ExpenseDate >= filter.From.Value);
        }

        if (filter.To.HasValue)
        {
            query = query.Where(e => e.ExpenseDate <= filter.To.Value);
        }

        if (filter.FarmId.HasValue)
        {
            query = query.Where(e => e.FarmId == filter.FarmId.Value);
        }

        if (filter.CategoryId.HasValue)
        {
            query = query.Where(e => e.ExpenseCategoryId == filter.CategoryId.Value);
        }

        if (filter.SupplierId.HasValue)
        {
            query = query.Where(e => e.SupplierId == filter.SupplierId.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(e => e.Status == filter.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var normalizedSearch = filter.Search.Trim().ToLower();
            query = query.Where(e =>
                e.Description.ToLower().Contains(normalizedSearch) ||
                (e.ReferenceNumber != null && e.ReferenceNumber.ToLower().Contains(normalizedSearch)));
        }

        return query;
    }
}
