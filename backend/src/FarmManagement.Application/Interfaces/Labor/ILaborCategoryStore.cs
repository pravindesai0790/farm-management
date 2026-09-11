using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Interfaces.Labor;

public interface ILaborCategoryStore
{
    Task<int> CountAsync(
        Guid organizationId,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<(LaborCategory Category, int WorkerCount)>> ListAsync(
        Guid organizationId,
        int skip,
        int take,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<LaborCategory?> FindAsync(
        Guid categoryId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<int> GetWorkerCountAsync(
        Guid categoryId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(
        Guid organizationId,
        string name,
        Guid? excludingCategoryId = null,
        CancellationToken cancellationToken = default);

    void Add(LaborCategory category);

    void AddAuditLog(AuditLog auditLog);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
