using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Labor;
using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.Interfaces.Labor;

public interface IWorkerEarningsLedgerService
{
    Task<WorkerEarningsLedgerResponse> CreateEarningFromFinalizedAttendanceAsync(
        EarningsActor actor,
        FinalizedAttendanceRecord attendance,
        CancellationToken cancellationToken = default);

    Task<WorkerEarningsLedgerResponse> ReverseEarningAsync(
        EarningsActor actor,
        Guid ledgerEntryId,
        ReverseEarningsRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task<WorkerEarningsLedgerResponse> CreateAdjustmentAsync(
        EarningsActor actor,
        CreateEarningsAdjustmentRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task<WorkerEarningsLedgerResponse> ApproveEarningAsync(
        EarningsActor actor,
        Guid ledgerEntryId,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task<WorkerGrossEarnedBalanceResponse> GetWorkerGrossEarnedBalanceAsync(
        EarningsActor actor,
        Guid workerId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default);

    Task<PagedResponse<WorkerEarningsLedgerResponse>> ListWorkerEarningsAsync(
        EarningsActor actor,
        Guid workerId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        string? status = null,
        string? entryType = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<WorkerEarningsLedgerResponse> GetByIdAsync(
        EarningsActor actor,
        Guid id,
        CancellationToken cancellationToken = default);
}
