using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.Interfaces.Plantations;

public interface IPlantationStore
{
    Task<IReadOnlyList<CropPlantation>> ListAsync(
        Guid organizationId,
        Guid? farmId,
        Guid? farmAreaId,
        PlantationStatus? status,
        Guid? cropId = null,
        CancellationToken cancellationToken = default);

    Task<CropPlantation?> FindAsync(Guid plantationId, Guid organizationId, CancellationToken cancellationToken = default);
    Task<FarmArea?> FindFarmAreaAsync(Guid farmAreaId, Guid organizationId, CancellationToken cancellationToken = default);
    Task<FarmArea?> LockFarmAreaAsync(Guid farmAreaId, Guid organizationId, CancellationToken cancellationToken = default);
    Task<Crop?> FindCropAsync(Guid cropId, Guid organizationId, CancellationToken cancellationToken = default);
    Task<CropVariety?> FindVarietyAsync(Guid varietyId, Guid organizationId, CancellationToken cancellationToken = default);
    Task<CropLifecycleTemplate?> FindLifecycleTemplateAsync(Guid templateId, Guid organizationId, CancellationToken cancellationToken = default);
    Task<PlantationEndReason?> FindEndReasonAsync(Guid endReasonId, Guid organizationId, CancellationToken cancellationToken = default);
    Task<Unit?> FindAreaUnitAsync(Guid unitId, Guid organizationId, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(Guid organizationId, string code, Guid? excludingPlantationId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CropPlantation>> ListActiveAllocationsAsync(Guid farmAreaId, Guid? excludingPlantationId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FarmArea>> ListActiveChildrenAsync(Guid parentFarmAreaId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CropCycle>> CancelActiveCyclesAsync(
        Guid plantationId,
        Guid organizationId,
        DateOnly cancellationDate,
        Guid cancellationReasonId,
        string? cancellationNotes,
        DateTimeOffset now,
        Guid updatedBy,
        CancellationToken cancellationToken = default);
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);
    void Add(CropPlantation plantation);
    void AddAuditLog(AuditLog auditLog);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
