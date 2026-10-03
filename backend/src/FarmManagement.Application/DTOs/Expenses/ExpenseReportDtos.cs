namespace FarmManagement.Application.DTOs.Expenses;

/// <summary>
/// Filter criteria for generating expense summary reports.
/// </summary>
public sealed record ExpenseReportFilter(
    DateOnly? From = null,
    DateOnly? To = null,
    Guid? FarmId = null,
    Guid? CategoryId = null,
    Guid? CropCycleId = null,
    Guid? CurrencyId = null);

/// <summary>
/// Consolidated expense report summary grouped by currency.
/// </summary>
public sealed record ExpenseReportSummaryResponse(
    ExpenseReportFilter Filter,
    IReadOnlyList<ExpenseReportCurrencySummary> CurrencySummaries);

/// <summary>
/// Expense summary aggregated for a specific currency.
/// </summary>
public sealed record ExpenseReportCurrencySummary(
    Guid CurrencyId,
    string CurrencyCode,
    string CurrencySymbol,
    decimal TotalDirectExpensesAmount,
    int DirectExpensesCount,
    decimal TotalInvoicedAmount,
    int InvoicesCount,
    decimal TotalSettledAmount,
    decimal TotalOutstandingPayables,
    decimal GrandTotalExpenses,
    IReadOnlyList<ExpenseCategorySummaryItem> CategoryBreakdown,
    IReadOnlyList<ExpenseFarmSummaryItem> FarmBreakdown,
    IReadOnlyList<ExpenseMonthlyTrendItem> MonthlyTrend,
    IReadOnlyList<OverdueInvoiceItem> TopOverdueInvoices);

/// <summary>
/// Cost breakdown by expense category within a specific currency.
/// </summary>
public sealed record ExpenseCategorySummaryItem(
    Guid CategoryId,
    string CategoryName,
    decimal DirectExpensesAmount,
    decimal InvoicedAmount,
    decimal TotalAmount,
    decimal Percentage);

/// <summary>
/// Cost breakdown by farm location within a specific currency.
/// </summary>
public sealed record ExpenseFarmSummaryItem(
    Guid FarmId,
    string FarmName,
    decimal DirectExpensesAmount,
    decimal InvoicedAmount,
    decimal TotalAmount,
    decimal Percentage);

/// <summary>
/// Monthly cost progression trend within a specific currency.
/// </summary>
public sealed record ExpenseMonthlyTrendItem(
    int Year,
    int Month,
    string MonthLabel,
    decimal DirectExpensesAmount,
    decimal InvoicedAmount,
    decimal TotalAmount);

/// <summary>
/// Concise details of an overdue purchase invoice for report alerting.
/// </summary>
public sealed record OverdueInvoiceItem(
    Guid InvoiceId,
    string SupplierInvoiceNumber,
    Guid SupplierId,
    string SupplierName,
    Guid? FarmId,
    string? FarmName,
    DateOnly InvoiceDate,
    DateOnly DueDate,
    int DaysOverdue,
    decimal TotalAmount,
    decimal AmountPaid,
    decimal OutstandingAmount,
    Guid CurrencyId,
    string CurrencyCode,
    string CurrencySymbol);
