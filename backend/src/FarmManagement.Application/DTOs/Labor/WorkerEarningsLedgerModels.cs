using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.DTOs.Labor;

public sealed record EarningsActor(Guid UserId, Guid OrganizationId);

public sealed record FinalizedAttendanceRecord(
    Guid AttendanceId,
    Guid OrganizationId,
    Guid WorkerId,
    DateOnly AttendanceDate,
    WageType WageType,
    decimal Quantity,
    string? Description = null,
    bool AutoApprove = false);

public sealed record CalculateAttendanceEarningsRequest(
    Guid WorkerId,
    DateOnly AttendanceDate,
    string AttendanceType,
    decimal Quantity = 1m);

public sealed record AttendanceEarningsCalculationResult(
    Guid WorkerId,
    string WorkerDisplayName,
    Gender Gender,
    string AttendanceType,
    string? WageType,
    decimal Quantity,
    decimal WageRate,
    decimal GrossAmount,
    Guid? CurrencyId,
    string CurrencyCode,
    string CurrencySymbol,
    bool IsEarningEligible,
    bool IsWorkerEligible,
    string? IneligibilityReason = null);

public sealed record ProcessAttendanceEarningsRequest(
    Guid AttendanceId,
    Guid WorkerId,
    DateOnly AttendanceDate,
    string AttendanceType,
    decimal Quantity = 1m,
    string? Description = null,
    bool AutoApprove = false);

public sealed record ReverseEarningsRequest(
    string? Reason = null);

public sealed record CreateEarningsAdjustmentRequest(
    Guid WorkerId,
    DateOnly EarningsDate,
    decimal GrossAmount,
    Guid CurrencyId,
    string WageType = "HOURLY",
    decimal Quantity = 1m,
    decimal? WageRate = null,
    bool ApproveImmediately = false,
    Guid? ReferenceLedgerId = null,
    string? Description = null);

public sealed record WorkerEarningsLedgerResponse(
    Guid Id,
    Guid OrganizationId,
    Guid WorkerId,
    string WorkerDisplayName,
    Guid? AttendanceId,
    DateOnly EarningsDate,
    string WageType,
    decimal Quantity,
    decimal WageRate,
    Guid CurrencyId,
    string CurrencyCode,
    string CurrencySymbol,
    decimal GrossAmount,
    string EntryType,
    string Status,
    Guid? ReferenceLedgerId,
    string? Description,
    DateTimeOffset? FinalizedAt,
    Guid? FinalizedBy,
    DateTimeOffset CreatedAt,
    Guid CreatedBy,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedBy);

public sealed record WorkerGrossEarnedBalanceResponse(
    Guid WorkerId,
    Guid OrganizationId,
    DateOnly? FromDate,
    DateOnly? ToDate,
    decimal GrossEarned,
    decimal TotalApprovedEarnings,
    decimal TotalReversals,
    decimal TotalAdjustments,
    decimal TotalPendingCalculated,
    int ApprovedEntryCount,
    int ReversedEntryCount,
    int CalculatedEntryCount,
    Guid? CurrencyId,
    string? CurrencyCode,
    string? CurrencySymbol);
