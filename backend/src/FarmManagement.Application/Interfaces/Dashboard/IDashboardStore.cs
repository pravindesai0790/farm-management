using FarmManagement.Application.DTOs.Dashboard;
using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Interfaces.Dashboard;

public interface IDashboardStore
{
    Task<IReadOnlyList<Farm>> GetFarmsWithAreasAsync(
        Guid organizationId,
        Guid? farmId,
        CancellationToken cancellationToken = default);

    Task<int> GetActiveFarmAreasCountAsync(
        Guid organizationId,
        Guid? farmId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CropPlantation>> GetPlantationsAsync(
        Guid organizationId,
        Guid? farmId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CropCycle>> GetCyclesAsync(
        Guid organizationId,
        Guid? farmId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Unit>> GetAreaUnitsAsync(
        CancellationToken cancellationToken = default);

    Task<LaborDashboardRawData> GetLaborDashboardDataAsync(
        Guid organizationId,
        Guid? farmId,
        DateOnly today,
        CancellationToken cancellationToken = default);

    Task<DashboardPrioritiesRawData> GetPrioritiesRawDataAsync(
        Guid organizationId,
        Guid? farmId,
        DateOnly today,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RawUpcomingActivity>> GetUpcomingActivitiesAsync(
        Guid organizationId,
        Guid? farmId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);

    Task<DashboardInventoryRawData> GetInventoryRawDataAsync(
        Guid organizationId,
        Guid? farmId,
        DateOnly consumptionStartDate,
        CancellationToken cancellationToken = default);

    Task<DashboardFinancialRawData> GetFinancialRawDataAsync(
        Guid organizationId,
        Guid? farmId,
        DateOnly monthStartDate,
        DateOnly today,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RawRecentActivity>> GetRecentActivitiesAsync(
        Guid organizationId,
        Guid? farmId,
        int limit,
        CancellationToken cancellationToken = default);
}
