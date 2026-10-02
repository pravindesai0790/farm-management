using FarmManagement.Application.Interfaces.Expenses;
using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FarmManagement.Infrastructure.Persistence;

public sealed class ExpenseCategoryStore(ApplicationDbContext dbContext) : IExpenseCategoryStore
{
    public Task<int> CountAsync(
        Guid organizationId,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken = default) =>
        BuildQuery(organizationId, search, isActive).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<ExpenseCategory>> ListAsync(
        Guid organizationId,
        int skip,
        int take,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        return await BuildQuery(organizationId, search, isActive)
            .AsNoTracking()
            .OrderByDescending(c => c.IsSystemDefault)
            .ThenBy(c => c.Name)
            .ThenBy(c => c.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<ExpenseCategory?> FindAsync(
        Guid categoryId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.ExpenseCategories.SingleOrDefaultAsync(
            c => c.Id == categoryId && (c.IsSystemDefault || c.OrganizationId == organizationId),
            cancellationToken);

    public Task<bool> NameExistsAsync(
        Guid organizationId,
        string name,
        Guid? excludingCategoryId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim().ToLower();
        var query = dbContext.ExpenseCategories.Where(c =>
            (c.IsSystemDefault || c.OrganizationId == organizationId) &&
            c.Name.ToLower() == normalizedName);

        if (excludingCategoryId.HasValue)
        {
            query = query.Where(c => c.Id != excludingCategoryId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public async Task<bool> HasHistoricalReferencesAsync(
        Guid categoryId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var hasExpenses = await dbContext.Expenses.AnyAsync(
            e => e.ExpenseCategoryId == categoryId && e.OrganizationId == organizationId,
            cancellationToken);
        if (hasExpenses) return true;

        var hasInvoiceLines = await dbContext.PurchaseInvoiceLines.AnyAsync(
            l => l.ExpenseCategoryId == categoryId && l.OrganizationId == organizationId,
            cancellationToken);
        return hasInvoiceLines;
    }

    public void Add(ExpenseCategory category) => dbContext.ExpenseCategories.Add(category);

    public void AddAuditLog(AuditLog auditLog) => dbContext.AuditLogs.Add(auditLog);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);

    private IQueryable<ExpenseCategory> BuildQuery(Guid organizationId, string? search, bool? isActive)
    {
        var query = dbContext.ExpenseCategories.Where(c =>
            c.IsSystemDefault || c.OrganizationId == organizationId);

        if (isActive is not null)
        {
            query = query.Where(c => c.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(normalizedSearch) ||
                (c.Description != null && c.Description.ToLower().Contains(normalizedSearch)));
        }

        return query;
    }
}
