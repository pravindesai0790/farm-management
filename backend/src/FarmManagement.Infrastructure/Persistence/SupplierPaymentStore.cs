using FarmManagement.Application.DTOs.Expenses;
using FarmManagement.Application.Interfaces.Expenses;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FarmManagement.Infrastructure.Persistence;

public sealed class SupplierPaymentStore(ApplicationDbContext dbContext) : ISupplierPaymentStore
{
    public async Task<IReadOnlyList<SupplierPayment>> ListAsync(
        Guid organizationId,
        SupplierPaymentFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        return await BuildQuery(organizationId, filter)
            .AsNoTracking()
            .Include(sp => sp.Supplier)
            .Include(sp => sp.Currency)
            .Include(sp => sp.Allocations)
                .ThenInclude(alloc => alloc.PurchaseInvoice)
            .OrderByDescending(sp => sp.PaymentDate)
            .ThenByDescending(sp => sp.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(
        Guid organizationId,
        SupplierPaymentFilter filter,
        CancellationToken cancellationToken = default)
    {
        return await BuildQuery(organizationId, filter)
            .CountAsync(cancellationToken);
    }

    public async Task<SupplierPayment?> FindByIdWithDetailsAsync(
        Guid id,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.SupplierPayments
            .Include(sp => sp.Supplier)
            .Include(sp => sp.Currency)
            .Include(sp => sp.Allocations)
                .ThenInclude(alloc => alloc.PurchaseInvoice)
            .SingleOrDefaultAsync(sp => sp.Id == id && sp.OrganizationId == organizationId, cancellationToken);
    }

    public async Task<SupplierPayment?> FindByIdempotencyKeyAsync(
        Guid organizationId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var normalizedKey = idempotencyKey.Trim();
        return await dbContext.SupplierPayments
            .Include(sp => sp.Supplier)
            .Include(sp => sp.Currency)
            .Include(sp => sp.Allocations)
                .ThenInclude(alloc => alloc.PurchaseInvoice)
            .SingleOrDefaultAsync(
                sp => sp.OrganizationId == organizationId && sp.IdempotencyKey == normalizedKey,
                cancellationToken);
    }

    public async Task AddPaymentWithAllocationsAsync(
        SupplierPayment payment,
        IEnumerable<SupplierPaymentAllocation> allocations,
        CancellationToken cancellationToken = default)
    {
        await dbContext.SupplierPayments.AddAsync(payment, cancellationToken);
        await dbContext.SupplierPaymentAllocations.AddRangeAsync(allocations, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdatePaymentAsync(
        SupplierPayment payment,
        CancellationToken cancellationToken = default)
    {
        dbContext.SupplierPayments.Update(payment);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddAuditLogAsync(
        AuditLog auditLog,
        CancellationToken cancellationToken = default)
    {
        await dbContext.AuditLogs.AddAsync(auditLog, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            await action(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }

    private IQueryable<SupplierPayment> BuildQuery(Guid organizationId, SupplierPaymentFilter filter)
    {
        var query = dbContext.SupplierPayments.Where(sp => sp.OrganizationId == organizationId);

        if (filter.SupplierId.HasValue)
        {
            query = query.Where(sp => sp.SupplierId == filter.SupplierId.Value);
        }

        if (filter.InvoiceId.HasValue)
        {
            query = query.Where(sp => sp.Allocations.Any(alloc => alloc.PurchaseInvoiceId == filter.InvoiceId.Value));
        }

        if (!string.IsNullOrWhiteSpace(filter.Status) && Enum.TryParse<SupplierPaymentStatus>(filter.Status, true, out var status))
        {
            query = query.Where(sp => sp.Status == status);
        }

        if (filter.From.HasValue)
        {
            query = query.Where(sp => sp.PaymentDate >= filter.From.Value);
        }

        if (filter.To.HasValue)
        {
            query = query.Where(sp => sp.PaymentDate <= filter.To.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(sp =>
                (sp.ReferenceNumber != null && sp.ReferenceNumber.ToLower().Contains(search)) ||
                (sp.Notes != null && sp.Notes.ToLower().Contains(search)) ||
                (sp.Supplier != null && sp.Supplier.Name.ToLower().Contains(search)) ||
                sp.Allocations.Any(a => a.PurchaseInvoice != null && a.PurchaseInvoice.SupplierInvoiceNumber.ToLower().Contains(search)));
        }

        return query;
    }
}
