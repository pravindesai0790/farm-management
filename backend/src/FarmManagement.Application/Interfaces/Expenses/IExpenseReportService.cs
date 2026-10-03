using FarmManagement.Application.DTOs.Expenses;

namespace FarmManagement.Application.Interfaces.Expenses;

/// <summary>
/// Application service orchestration for generating farm expense reports and analytics.
/// </summary>
public interface IExpenseReportService
{
    /// <summary>
    /// Generates a multi-currency expense report summary based on filter criteria.
    /// </summary>
    Task<ExpenseReportSummaryResponse> GetSummaryReportAsync(
        Guid organizationId,
        ExpenseReportFilter filter,
        CancellationToken cancellationToken = default);
}
