using FarmManagement.Application.DTOs.Expenses;
using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Interfaces.Expenses;

/// <summary>
/// Persistence operations for querying supplier open invoices and payables balance.
/// </summary>
public interface ISupplierBalanceStore
{
    /// <summary>
    /// Retrieves posted open or unpaid purchase invoices matching the balance filter.
    /// </summary>
    Task<IReadOnlyList<PurchaseInvoice>> GetOpenOrUnpaidInvoicesAsync(
        Guid organizationId,
        SupplierBalanceFilter filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves active suppliers belonging to the organization.
    /// </summary>
    Task<IReadOnlyList<Supplier>> GetActiveSuppliersAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves active system currencies.
    /// </summary>
    Task<IReadOnlyList<Currency>> GetActiveCurrenciesAsync(
        CancellationToken cancellationToken = default);
}
