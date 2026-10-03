namespace FarmManagement.Application.DTOs.Expenses;

/// <summary>
/// Filter criteria for querying supplier balances and aging analysis.
/// </summary>
public sealed record SupplierBalanceFilter(
    DateOnly? AsOfDate = null,
    Guid? SupplierId = null,
    Guid? FarmId = null,
    Guid? CurrencyId = null);

/// <summary>
/// Consolidated supplier accounts payable summary grouped by currency.
/// </summary>
public sealed record SupplierBalanceSummaryResponse(
    DateOnly AsOfDate,
    IReadOnlyList<SupplierBalanceCurrencySummary> CurrencySummaries);

/// <summary>
/// Accounts payable and aging totals for a specific currency.
/// </summary>
public sealed record SupplierBalanceCurrencySummary(
    Guid CurrencyId,
    string CurrencyCode,
    string CurrencySymbol,
    decimal TotalOutstandingBalance,
    decimal TotalOverdueBalance,
    decimal TotalCurrentNotDueBalance,
    int SuppliersCount,
    int OpenInvoicesCount,
    SupplierAgingTotals AgingTotals,
    IReadOnlyList<SupplierBalanceItem> SupplierBalances);

/// <summary>
/// Standard accounts payable aging breakdown by overdue brackets.
/// </summary>
public sealed record SupplierAgingTotals(
    decimal CurrentNotDue,
    decimal Days1To30,
    decimal Days31To60,
    decimal Days61To90,
    decimal DaysOver90);

/// <summary>
/// Outstanding balance and aging details for a single supplier.
/// </summary>
public sealed record SupplierBalanceItem(
    Guid SupplierId,
    string SupplierName,
    string? ContactPerson,
    string? Phone,
    string? Email,
    decimal TotalOutstandingBalance,
    decimal OverdueBalance,
    SupplierAgingTotals Aging,
    IReadOnlyList<SupplierOpenInvoiceItem> OpenInvoices);

/// <summary>
/// Unpaid or partially paid supplier invoice record with calculated aging.
/// </summary>
public sealed record SupplierOpenInvoiceItem(
    Guid InvoiceId,
    string SupplierInvoiceNumber,
    Guid? FarmId,
    string? FarmName,
    DateOnly InvoiceDate,
    DateOnly DueDate,
    int DaysOverdue,
    decimal TotalAmount,
    decimal AmountPaid,
    decimal OutstandingAmount,
    string PaymentStatus,
    string DueStatus);
