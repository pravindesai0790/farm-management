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
}
