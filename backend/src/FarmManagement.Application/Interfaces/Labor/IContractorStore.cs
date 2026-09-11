using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Interfaces.Labor;

public interface IContractorStore
{
    Task<int> CountAsync(
        Guid organizationId,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<(Contractor Contractor, int WorkerCount)>> ListAsync(
        Guid organizationId,
        int skip,
        int take,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<Contractor?> FindAsync(
        Guid contractorId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<int> GetWorkerCountAsync(
        Guid contractorId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(
        Guid organizationId,
        string name,
        Guid? excludingContractorId = null,
        CancellationToken cancellationToken = default);

    void Add(Contractor contractor);

    void AddAuditLog(AuditLog auditLog);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
