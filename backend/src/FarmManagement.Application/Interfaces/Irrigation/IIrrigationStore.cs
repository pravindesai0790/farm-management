using FarmManagement.Application.DTOs.Irrigation;
using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Interfaces.Irrigation;

public interface IIrrigationStore
{
    Task<IrrigationEvent?> FindAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        Guid organizationId,
        IrrigationListQuery query,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IrrigationEvent>> ListAsync(
        Guid organizationId,
        IrrigationListQuery query,
        int skip,
        int take,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    void Add(IrrigationEvent irrigation);

    void AddAuditLog(AuditLog auditLog);

    Task<Farm?> FindFarmAsync(Guid farmId, Guid organizationId, CancellationToken cancellationToken = default);

    Task<FarmArea?> FindFarmAreaAsync(Guid farmAreaId, Guid organizationId, CancellationToken cancellationToken = default);

    Task<CropPlantation?> FindPlantationAsync(Guid plantationId, Guid organizationId, CancellationToken cancellationToken = default);

    Task<CropCycle?> FindCropCycleAsync(Guid cropCycleId, Guid organizationId, CancellationToken cancellationToken = default);

    Task<CropCycleStage?> FindCropCycleStageAsync(Guid cropCycleStageId, Guid organizationId, CancellationToken cancellationToken = default);

    Task<Unit?> FindUnitAsync(Guid unitId, Guid organizationId, CancellationToken cancellationToken = default);

    Task<IrrigationMethod?> FindIrrigationMethodAsync(Guid methodId, Guid organizationId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IrrigationMethod>> ListMethodsAsync(Guid organizationId, bool activeOnly, CancellationToken cancellationToken = default);

    Task<Dictionary<Guid, string>> GetUserNamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken = default);

    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
