using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.Interfaces.CropCycles;

public interface ICropCycleStore
{
    Task<IReadOnlyList<CropCycle>> ListAsync(
        Guid organizationId,
        Guid? farmId,
        Guid? farmAreaId,
        Guid? plantationId,
        CropCycleStatus? status,
        int? seasonYear,
        CancellationToken cancellationToken = default);

    Task<CropCycle?> FindAsync(
        Guid cycleId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<CropCycle?> LockAsync(
        Guid cycleId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<CropPlantation?> LockPlantationAsync(
        Guid plantationId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<PlantationEndReason?> FindCancellationReasonAsync(
        Guid reasonId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(
        Guid organizationId,
        string cycleCode,
        Guid? excludingCycleId = null,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveCycleAsync(
        Guid plantationId,
        Guid? excludingCycleId = null,
        CancellationToken cancellationToken = default);

    void Add(CropCycle cycle);
    void AddAuditLog(AuditLog auditLog);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);
}
