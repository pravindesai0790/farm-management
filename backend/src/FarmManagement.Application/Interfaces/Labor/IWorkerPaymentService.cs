using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Labor;

namespace FarmManagement.Application.Interfaces.Labor;

public interface IWorkerPaymentService
{
    Task<WorkerPaymentResponse> RecordPaymentAsync(
        PaymentActor actor,
        RecordWorkerPaymentRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task<WorkerPaymentResponse> CancelPaymentAsync(
        PaymentActor actor,
        Guid paymentId,
        CancelWorkerPaymentRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task<WorkerPaymentResponse> GetByIdAsync(
        PaymentActor actor,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PagedResponse<WorkerPaymentResponse>> ListByWorkerAsync(
        PaymentActor actor,
        Guid workerId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        string? paymentType = null,
        string? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<WorkerFinancialSummaryResponse> GetWorkerFinancialSummaryAsync(
        PaymentActor actor,
        Guid workerId,
        DateOnly? asOfDate = null,
        CancellationToken cancellationToken = default);
}
