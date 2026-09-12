using FarmManagement.Application.Common.Models;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.Interfaces.Labor;

public interface IWorkerPaymentAllocationStore
{
    Task<WorkerPaymentAllocation?> FindAsync(
        Guid id,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<WorkerPayment?> FindPaymentAsync(
        Guid paymentId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<WorkerEarningsLedger?> FindEarningAsync(
        Guid earningId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkerPaymentAllocation>> ListByPaymentAsync(
        Guid paymentId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkerPaymentAllocation>> ListByEarningAsync(
        Guid earningId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkerPaymentAllocation>> ListByWorkerAsync(
        Guid workerId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<decimal> GetTotalAllocatedForPaymentAsync(
        Guid paymentId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<decimal> GetTotalSettledForEarningAsync(
        Guid earningId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, decimal>> GetSettledTotalsForEarningsBatchAsync(
        IEnumerable<Guid> earningIds,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkerEarningsLedger>> ListUnsettledEarningsForWorkerAsync(
        Guid organizationId,
        Guid workerId,
        DateOnly? upToDate = null,
        CancellationToken cancellationToken = default);

    void Add(WorkerPaymentAllocation allocation);

    void AddRange(IEnumerable<WorkerPaymentAllocation> allocations);

    void Remove(WorkerPaymentAllocation allocation);

    void RemoveRange(IEnumerable<WorkerPaymentAllocation> allocations);

    void AddAuditLog(AuditLog auditLog);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
