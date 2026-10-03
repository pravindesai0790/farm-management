using FarmManagement.Application.DTOs.Expenses;
using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Interfaces.Expenses;

/// <summary>
/// Persistence operations for querying posted expenses and invoices for reporting.
/// </summary>
public interface IExpenseReportStore
{
    /// <summary>
    /// Retrieves posted direct expenses matching the report filter.
    /// </summary>
    Task<IReadOnlyList<Expense>> GetPostedExpensesAsync(
        Guid organizationId,
        ExpenseReportFilter filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves posted purchase invoices matching the report filter.
    /// </summary>
    Task<IReadOnlyList<PurchaseInvoice>> GetPostedInvoicesAsync(
        Guid organizationId,
        ExpenseReportFilter filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves active currencies for multi-currency reporting.
    /// </summary>
    Task<IReadOnlyList<Currency>> GetActiveCurrenciesAsync(
        CancellationToken cancellationToken = default);
}
