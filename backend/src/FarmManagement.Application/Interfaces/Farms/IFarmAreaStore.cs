using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Interfaces.Farms;

public interface IFarmAreaStore
{
    Task<IReadOnlyList<FarmArea>> ListAsync(
        Guid farmId,
        Guid organizationId,
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<FarmArea?> FindAsync(
        Guid farmAreaId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<Farm?> FindFarmAsync(
        Guid farmId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<FarmArea?> FindParentAsync(
        Guid parentFarmAreaId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<Unit?> FindAreaUnitAsync(
        Guid unitId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(
        Guid farmId,
        string code,
        Guid? excludingFarmAreaId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FarmArea>> ListActiveChildrenAsync(
        Guid parentFarmAreaId,
        Guid? excludingFarmAreaId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CropPlantation>> ListActivePlantationsAsync(
        Guid farmAreaId,
        CancellationToken cancellationToken = default);

    Task<bool> HasChildrenAsync(
        Guid parentFarmAreaId,
        CancellationToken cancellationToken = default);

    void Add(FarmArea farmArea);

    void AddAuditLog(AuditLog auditLog);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
