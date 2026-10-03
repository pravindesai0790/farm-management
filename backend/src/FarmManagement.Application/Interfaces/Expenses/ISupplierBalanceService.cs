using FarmManagement.Application.DTOs.Expenses;

namespace FarmManagement.Application.Interfaces.Expenses;

/// <summary>
/// Application service orchestration for supplier balances and accounts payable aging.
/// </summary>
public interface ISupplierBalanceService
{
    /// <summary>
    /// Generates supplier balances and aging analysis grouped by currency.
    /// </summary>
    Task<SupplierBalanceSummaryResponse> GetSupplierBalancesAsync(
        Guid organizationId,
        SupplierBalanceFilter filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a list of overdue invoices across suppliers.
    /// </summary>
    Task<IReadOnlyList<OverdueInvoiceItem>> GetOverdueInvoicesAsync(
        Guid organizationId,
        SupplierBalanceFilter filter,
        CancellationToken cancellationToken = default);
}
