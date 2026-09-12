namespace FarmManagement.Application.DTOs.Labor;

public sealed record SettlementActor(Guid UserId, Guid OrganizationId);

public sealed record WorkerSettlementQuery(
    Guid WorkerId,
    DateOnly? PeriodFrom = null,
    DateOnly? PeriodTo = null,
    DateOnly? AsOfDate = null);

public sealed record BatchWorkerSettlementQuery(
    IReadOnlyList<Guid>? WorkerIds = null,
    DateOnly? PeriodFrom = null,
    DateOnly? PeriodTo = null,
    DateOnly? AsOfDate = null);

public sealed record WorkerSettlementCalculationResponse(
    Guid WorkerId,
    string WorkerDisplayName,
    Guid OrganizationId,
    DateOnly? PeriodFrom,
    DateOnly? PeriodTo,
    DateOnly? AsOfDate,
    decimal TotalEarnings,
    decimal TotalApprovedPositiveAdjustments,
    decimal TotalApprovedNegativeAdjustments,
    decimal TotalApprovedAdjustments,
    decimal GrossPayable,
    decimal TotalAdvances,
    decimal TotalAdvanceApplied,
    decimal CurrentAdvanceBalance,
    decimal TotalPayouts,
    decimal TotalPayoutsApplied,
    decimal TotalPaid,
    decimal OutstandingAmount,
    decimal AmountAvailableForPayout,
    decimal AdvanceCarriedForward,
    string SettlementStatus,
    Guid? CurrencyId,
    string? CurrencyCode,
    string? CurrencySymbol,
    int UnsettledEarningCount,
    int SettledEarningCount,
    int PendingApprovalEarningCount);
