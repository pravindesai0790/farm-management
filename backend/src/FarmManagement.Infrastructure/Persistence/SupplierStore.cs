using FarmManagement.Application.Interfaces.Expenses;
using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FarmManagement.Infrastructure.Persistence;

public sealed class SupplierStore(ApplicationDbContext dbContext) : ISupplierStore
{
    public Task<int> CountAsync(
        Guid organizationId,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken = default) =>
        BuildQuery(organizationId, search, isActive).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<Supplier>> ListAsync(
        Guid organizationId,
        int skip,
        int take,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        return await BuildQuery(organizationId, search, isActive)
            .AsNoTracking()
            .OrderBy(supplier => supplier.Name)
            .ThenBy(supplier => supplier.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<Supplier?> FindAsync(
        Guid supplierId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.Suppliers.SingleOrDefaultAsync(
            supplier => supplier.Id == supplierId && supplier.OrganizationId == organizationId,
            cancellationToken);

    public Task<bool> NameExistsAsync(
        Guid organizationId,
        string name,
        Guid? excludingSupplierId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim().ToLower();
        var query = dbContext.Suppliers.Where(s =>
            s.OrganizationId == organizationId &&
            s.Name.ToLower() == normalizedName);

        if (excludingSupplierId.HasValue)
        {
            query = query.Where(s => s.Id != excludingSupplierId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public async Task<bool> HasHistoricalReferencesAsync(
        Guid supplierId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var hasExpenses = await dbContext.Expenses.AnyAsync(
            e => e.SupplierId == supplierId && e.OrganizationId == organizationId,
            cancellationToken);
        if (hasExpenses) return true;

        var hasInvoices = await dbContext.PurchaseInvoices.AnyAsync(
            i => i.SupplierId == supplierId && i.OrganizationId == organizationId,
            cancellationToken);
        if (hasInvoices) return true;

        var hasPayments = await dbContext.SupplierPayments.AnyAsync(
            p => p.SupplierId == supplierId && p.OrganizationId == organizationId,
            cancellationToken);
        return hasPayments;
    }

    public void Add(Supplier supplier) => dbContext.Suppliers.Add(supplier);

    public void AddAuditLog(AuditLog auditLog) => dbContext.AuditLogs.Add(auditLog);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);

    private IQueryable<Supplier> BuildQuery(Guid organizationId, string? search, bool? isActive)
    {
        var query = dbContext.Suppliers.Where(s => s.OrganizationId == organizationId);

        if (isActive is not null)
        {
            query = query.Where(s => s.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLower();
            query = query.Where(s =>
                s.Name.ToLower().Contains(normalizedSearch) ||
                (s.ContactPerson != null && s.ContactPerson.ToLower().Contains(normalizedSearch)) ||
                (s.Phone != null && s.Phone.ToLower().Contains(normalizedSearch)) ||
                (s.Email != null && s.Email.ToLower().Contains(normalizedSearch)) ||
                (s.RegistrationIdentifier != null && s.RegistrationIdentifier.ToLower().Contains(normalizedSearch)));
        }

        return query;
    }
}
