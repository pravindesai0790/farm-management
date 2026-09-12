using FarmManagement.Application.Common.Models;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.Interfaces.Labor;

public interface IWorkerEarningsLedgerStore
{
    Task<WorkerEarningsLedger?> FindAsync(
        Guid id,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<WorkerEarningsLedger?> FindByAttendanceIdAsync(
        Guid organizationId,
        Guid attendanceId,
        CancellationToken cancellationToken = default);

    Task<PagedResponse<WorkerEarningsLedger>> ListByWorkerAsync(
        Guid organizationId,
        Guid workerId,
        DateOnly? fromDate,
        DateOnly? toDate,
        EarningsLedgerStatus? status,
        EarningsEntryType? entryType,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkerEarningsLedger>> ListForBalanceCalculationAsync(
        Guid organizationId,
        Guid workerId,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveReversalAsync(
        Guid originalLedgerId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<Worker?> FindWorkerAsync(
        Guid workerId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<LaborWageRate?> FindApplicableWageRateAsync(
        Guid organizationId,
        Gender gender,
        WageType wageType,
        DateOnly businessDate,
        CancellationToken cancellationToken = default);

    Task<Currency?> FindCurrencyAsync(
        Guid currencyId,
        CancellationToken cancellationToken = default);

    void Add(WorkerEarningsLedger entry);

    void AddAuditLog(AuditLog auditLog);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
