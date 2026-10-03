using FarmManagement.Application.DTOs.Expenses;
using FarmManagement.Application.Interfaces.Expenses;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FarmManagement.Infrastructure.Persistence;

public sealed class ExpenseReportStore(ApplicationDbContext dbContext) : IExpenseReportStore
{
    public async Task<IReadOnlyList<Expense>> GetPostedExpensesAsync(
        Guid organizationId,
        ExpenseReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Expenses
            .AsNoTracking()
            .Include(e => e.Farm)
            .Include(e => e.ExpenseCategory)
            .Include(e => e.Currency)
            .Include(e => e.Supplier)
            .Where(e => e.OrganizationId == organizationId && e.Status == ExpenseStatus.Posted);

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

        if (filter.CropCycleId.HasValue)
        {
            query = query.Where(e => e.CropCycleId == filter.CropCycleId.Value);
        }

        if (filter.CurrencyId.HasValue)
        {
            query = query.Where(e => e.CurrencyId == filter.CurrencyId.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PurchaseInvoice>> GetPostedInvoicesAsync(
        Guid organizationId,
        ExpenseReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.PurchaseInvoices
            .AsNoTracking()
            .Include(pi => pi.Supplier)
            .Include(pi => pi.Farm)
            .Include(pi => pi.Currency)
            .Include(pi => pi.Lines)
                .ThenInclude(l => l.ExpenseCategory)
            .Include(pi => pi.PaymentAllocations)
                .ThenInclude(pa => pa.SupplierPayment)
            .Where(pi => pi.OrganizationId == organizationId && pi.Status == PurchaseInvoiceStatus.Posted);

        if (filter.From.HasValue)
        {
            query = query.Where(pi => pi.InvoiceDate >= filter.From.Value);
        }

        if (filter.To.HasValue)
        {
            query = query.Where(pi => pi.InvoiceDate <= filter.To.Value);
        }

        if (filter.FarmId.HasValue)
        {
            query = query.Where(pi => pi.FarmId == filter.FarmId.Value);
        }

        if (filter.CategoryId.HasValue)
        {
            query = query.Where(pi => pi.Lines.Any(l => l.ExpenseCategoryId == filter.CategoryId.Value));
        }

        if (filter.CropCycleId.HasValue)
        {
            query = query.Where(pi => pi.Lines.Any(l => l.CropCycleId == filter.CropCycleId.Value));
        }

        if (filter.CurrencyId.HasValue)
        {
            query = query.Where(pi => pi.CurrencyId == filter.CurrencyId.Value);
        }

        return await query.ToListAsync(cancellationToken);
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
