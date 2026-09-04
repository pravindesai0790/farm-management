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
}
