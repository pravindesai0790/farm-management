using FarmManagement.Application.DTOs.Expenses;
using FarmManagement.Application.Interfaces.Expenses;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FarmManagement.Infrastructure.Persistence;

public sealed class SupplierBalanceStore(ApplicationDbContext dbContext) : ISupplierBalanceStore
{
    public async Task<IReadOnlyList<PurchaseInvoice>> GetOpenOrUnpaidInvoicesAsync(
        Guid organizationId,
        SupplierBalanceFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.PurchaseInvoices
            .AsNoTracking()
            .Include(pi => pi.Supplier)
            .Include(pi => pi.Farm)
            .Include(pi => pi.Currency)
            .Include(pi => pi.PaymentAllocations)
                .ThenInclude(pa => pa.SupplierPayment)
            .Where(pi => pi.OrganizationId == organizationId && pi.Status == PurchaseInvoiceStatus.Posted);

        if (filter.AsOfDate.HasValue)
        {
            query = query.Where(pi => pi.InvoiceDate <= filter.AsOfDate.Value);
        }

        if (filter.SupplierId.HasValue)
        {
            query = query.Where(pi => pi.SupplierId == filter.SupplierId.Value);
        }

        if (filter.FarmId.HasValue)
        {
            query = query.Where(pi => pi.FarmId == filter.FarmId.Value);
        }

        if (filter.CurrencyId.HasValue)
        {
            query = query.Where(pi => pi.CurrencyId == filter.CurrencyId.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Supplier>> GetActiveSuppliersAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Suppliers
            .AsNoTracking()
            .Where(s => s.OrganizationId == organizationId && s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Currency>> GetActiveCurrenciesAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Currencies
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Code)
            .ToListAsync(cancellationToken);
    }
}
