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

    Task<(IReadOnlyList<CropCycle> Items, int TotalCount)> ListPagedAsync(
        Guid organizationId,
        Guid? farmId,
        Guid? farmAreaId,
        Guid? plantationId,
        CropCycleStatus? status,
        int? seasonYear,
        int skip,
        int take,
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

    Task<CropLifecycleTemplate?> FindLifecycleTemplateAsync(
        Guid templateId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveCycleAsync(
        Guid plantationId,
        Guid? excludingCycleId = null,
        CancellationToken cancellationToken = default);

    Task<bool> HasCycleForSeasonAsync(
        Guid plantationId,
        int seasonYear,
        Guid? excludingCycleId = null,
        CancellationToken cancellationToken = default);

    void Add(CropCycle cycle);
    void AddStage(CropCycleStage stage);
    Task<bool> HasStagesAsync(Guid cycleId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CropCycleStage>> GetStagesAsync(Guid cycleId, CancellationToken cancellationToken = default);
    void AddAuditLog(AuditLog auditLog);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);
}
