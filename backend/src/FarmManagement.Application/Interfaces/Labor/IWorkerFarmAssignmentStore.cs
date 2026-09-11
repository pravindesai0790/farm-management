using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Interfaces.Labor;

public interface IWorkerFarmAssignmentStore
{
    Task<IReadOnlyList<WorkerFarmAssignment>> ListByWorkerAsync(
        Guid organizationId,
        Guid workerId,
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkerFarmAssignment>> ListByFarmAsync(
        Guid organizationId,
        Guid farmId,
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<WorkerFarmAssignment?> FindAsync(
        Guid assignmentId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<Worker?> FindWorkerAsync(
        Guid workerId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<Farm?> FindFarmAsync(
        Guid farmId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    void Add(WorkerFarmAssignment assignment);

    void AddAuditLog(AuditLog auditLog);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
