using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.Interfaces.LaborActivities;

public interface ILaborActivityStore
{
    Task<(IReadOnlyList<LaborActivity> Items, int TotalCount)> ListPagedAsync(
        Guid organizationId,
        Guid? farmId,
        Guid? farmAreaId,
        Guid? plantationId,
        Guid? cropCycleId,
        Guid? activityTypeId,
        DateOnly? fromDate,
        DateOnly? toDate,
        LaborActivityStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<LaborActivity?> FindAsync(
        Guid activityId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<LaborActivity?> LockAsync(
        Guid activityId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<Farm?> FindFarmAsync(
        Guid farmId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<FarmArea?> FindFarmAreaAsync(
        Guid farmAreaId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<CropPlantation?> FindPlantationAsync(
        Guid plantationId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<CropCycle?> FindCropCycleAsync(
        Guid cropCycleId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<LaborActivityType?> FindLaborActivityTypeAsync(
        Guid activityTypeId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<string> ResolveOrganizationCurrencyAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    void Add(LaborActivity activity);

    void AddAuditLog(AuditLog auditLog);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);
}
