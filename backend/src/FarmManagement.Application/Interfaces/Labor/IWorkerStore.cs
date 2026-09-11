using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.Interfaces.Labor;

public interface IWorkerStore
{
    Task<int> CountAsync(
        Guid organizationId,
        string? search,
        bool? isActive,
        Guid? contractorId,
        Guid? laborCategoryId,
        Gender? gender,
        EmploymentType? employmentType,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Worker>> ListAsync(
        Guid organizationId,
        int skip,
        int take,
        string? search,
        bool? isActive,
        Guid? contractorId,
        Guid? laborCategoryId,
        Gender? gender,
        EmploymentType? employmentType,
        CancellationToken cancellationToken = default);

    Task<Worker?> FindAsync(
        Guid workerId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<Contractor?> FindContractorAsync(
        Guid contractorId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<LaborCategory?> FindLaborCategoryAsync(
        Guid laborCategoryId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    void Add(Worker worker);

    void AddAuditLog(AuditLog auditLog);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
