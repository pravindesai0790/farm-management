using FarmManagement.Application.DTOs.Labor;

namespace FarmManagement.Application.Interfaces.Labor;

public interface IWorkerPaymentAllocationService
{
    Task<WorkerPaymentAllocationResponse> CreateAllocationAsync(
        PaymentActor actor,
        CreateWorkerPaymentAllocationRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkerPaymentAllocationResponse>> AutoAllocatePaymentAsync(
        PaymentActor actor,
        AutoAllocatePaymentRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task<WorkerPaymentAllocationResponse> GetByIdAsync(
        PaymentActor actor,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkerPaymentAllocationResponse>> ListByPaymentAsync(
        PaymentActor actor,
        Guid paymentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkerPaymentAllocationResponse>> ListByEarningAsync(
        PaymentActor actor,
        Guid earningId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkerPaymentAllocationResponse>> ListByWorkerAsync(
        PaymentActor actor,
        Guid workerId,
        CancellationToken cancellationToken = default);

    Task DeleteAllocationAsync(
        PaymentActor actor,
        Guid id,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EarningUnpaidBalanceResponse>> GetUnsettledEarningsForWorkerAsync(
        PaymentActor actor,
        Guid workerId,
        DateOnly? upToDate = null,
        CancellationToken cancellationToken = default);

    Task<PaymentUnallocatedBalanceResponse> GetPaymentUnallocatedBalanceAsync(
        PaymentActor actor,
        Guid paymentId,
        CancellationToken cancellationToken = default);
}
