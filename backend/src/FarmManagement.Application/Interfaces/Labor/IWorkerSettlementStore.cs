using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Interfaces.Labor;

public interface IWorkerSettlementStore
{
    Task<Worker?> FindWorkerAsync(
        Guid workerId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Worker>> ListWorkersAsync(
        Guid organizationId,
        IReadOnlyList<Guid>? workerIds = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkerEarningsLedger>> GetEarningsForSettlementAsync(
        Guid organizationId,
        Guid workerId,
        DateOnly? periodFrom = null,
        DateOnly? periodTo = null,
        DateOnly? asOfDate = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkerPayment>> GetCompletedPaymentsAsync(
        Guid organizationId,
        Guid workerId,
        DateOnly? asOfDate = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkerPaymentAllocation>> GetAllocationsForWorkerAsync(
        Guid organizationId,
        Guid workerId,
        DateOnly? asOfDate = null,
        CancellationToken cancellationToken = default);
}
