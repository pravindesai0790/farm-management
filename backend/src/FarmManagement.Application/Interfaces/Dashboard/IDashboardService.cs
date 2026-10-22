using FarmManagement.Application.DTOs.Dashboard;

namespace FarmManagement.Application.Interfaces.Dashboard;

public interface IDashboardService
{
    Task<DashboardSummaryResponse> GetSummaryAsync(
        DashboardActor actor,
        Guid? farmId,
        CancellationToken cancellationToken = default);

    Task<DashboardPrioritiesResponse> GetPrioritiesAsync(
        DashboardActor actor,
        Guid? farmId,
        CancellationToken cancellationToken = default);

    Task<DashboardUpcomingWorkResponse> GetUpcomingWorkAsync(
        DashboardActor actor,
        Guid? farmId,
        int days = 7,
        CancellationToken cancellationToken = default);

    Task<DashboardInventorySummaryResponse> GetInventorySummaryAsync(
        DashboardActor actor,
        Guid? farmId,
        CancellationToken cancellationToken = default);

    Task<DashboardFinancialSummaryResponse> GetFinancialSummaryAsync(
        DashboardActor actor,
        Guid? farmId,
        CancellationToken cancellationToken = default);

    Task<DashboardRecentActivityResponse> GetRecentActivityAsync(
        DashboardActor actor,
        Guid? farmId,
        int limit = 15,
        CancellationToken cancellationToken = default);
}
