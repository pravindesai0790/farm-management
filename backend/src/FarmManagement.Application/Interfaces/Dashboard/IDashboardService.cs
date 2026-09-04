using FarmManagement.Application.DTOs.Dashboard;

namespace FarmManagement.Application.Interfaces.Dashboard;

public interface IDashboardService
{
    Task<DashboardSummaryResponse> GetSummaryAsync(
        DashboardActor actor,
        Guid? farmId,
        CancellationToken cancellationToken = default);
}
