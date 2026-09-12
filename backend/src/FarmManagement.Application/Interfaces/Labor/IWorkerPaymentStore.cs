using FarmManagement.Application.Common.Models;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.Interfaces.Labor;

public interface IWorkerPaymentStore
{
    Task<WorkerPayment?> FindAsync(
        Guid id,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<PagedResponse<WorkerPayment>> ListByWorkerAsync(
        Guid organizationId,
        Guid workerId,
        DateOnly? fromDate,
        DateOnly? toDate,
        PaymentType? paymentType,
        PaymentStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkerPayment>> ListPaymentsForFinancialSummaryAsync(
        Guid organizationId,
        Guid workerId,
        DateOnly? asOfDate,
        CancellationToken cancellationToken = default);

    Task<Worker?> FindWorkerAsync(
        Guid workerId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<Currency?> FindCurrencyAsync(
        Guid currencyId,
        CancellationToken cancellationToken = default);

    void Add(WorkerPayment payment);

    void AddAuditLog(AuditLog auditLog);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
