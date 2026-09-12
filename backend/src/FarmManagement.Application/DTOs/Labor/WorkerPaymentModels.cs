namespace FarmManagement.Application.DTOs.Labor;

public sealed record PaymentActor(Guid UserId, Guid OrganizationId);

public sealed record RecordWorkerPaymentRequest(
    Guid WorkerId,
    DateOnly PaymentDate,
    string PaymentType,
    decimal Amount,
    Guid CurrencyId,
    string PaymentMethod,
    string? ReferenceNumber = null,
    DateOnly? PaymentPeriodFrom = null,
    DateOnly? PaymentPeriodTo = null,
    string Status = "COMPLETED",
    string? Notes = null,
    bool AutoAllocate = true);

public sealed record CancelWorkerPaymentRequest(
    string? Reason = null);

public sealed record WorkerPaymentResponse(
    Guid Id,
    Guid OrganizationId,
    Guid WorkerId,
    string WorkerDisplayName,
    DateOnly PaymentDate,
    string PaymentType,
    decimal Amount,
    Guid CurrencyId,
    string CurrencyCode,
    string CurrencySymbol,
    string PaymentMethod,
    string? ReferenceNumber,
    DateOnly? PaymentPeriodFrom,
    DateOnly? PaymentPeriodTo,
    string Status,
    string? Notes,
    DateTimeOffset? CancelledAt,
    Guid? CancelledBy,
    string? CancellationReason,
    DateTimeOffset CreatedAt,
    Guid CreatedBy,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedBy);

public sealed record WorkerFinancialSummaryResponse(
    Guid WorkerId,
    Guid OrganizationId,
    string WorkerDisplayName,
    DateOnly? AsOfDate,
    decimal GrossEarnings,
    decimal TotalAdvances,
    decimal TotalPayouts,
    decimal TotalAdjustments,
    decimal NetOutstanding,
    decimal CarryForwardAdvanceBalance,
    int CompletedPaymentCount,
    int CancelledPaymentCount,
    Guid? CurrencyId,
    string? CurrencyCode,
    string? CurrencySymbol);
