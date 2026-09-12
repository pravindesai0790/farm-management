namespace FarmManagement.Application.DTOs.Labor;

public sealed record CreateWorkerPaymentAllocationRequest(
    Guid WorkerPaymentId,
    Guid? WorkerEarningsLedgerId,
    decimal AllocatedAmount,
    string AllocationType,
    DateOnly AllocationDate,
    string? Notes = null);

public sealed record AutoAllocatePaymentRequest(
    Guid WorkerPaymentId,
    DateOnly? AllocationDate = null,
    string? AllocationType = null,
    string? Notes = null);

public sealed record WorkerPaymentAllocationResponse(
    Guid Id,
    Guid OrganizationId,
    Guid WorkerPaymentId,
    Guid? WorkerEarningsLedgerId,
    decimal AllocatedAmount,
    string AllocationType,
    DateOnly AllocationDate,
    string? Notes,
    DateTimeOffset CreatedAt,
    Guid CreatedBy,
    string? PaymentType = null,
    decimal? PaymentAmount = null,
    DateOnly? EarningDate = null,
    decimal? EarningGrossAmount = null);

public sealed record EarningUnpaidBalanceResponse(
    Guid WorkerEarningsLedgerId,
    Guid WorkerId,
    DateOnly EarningsDate,
    string EntryType,
    decimal GrossAmount,
    decimal AllocatedAmount,
    decimal RemainingUnpaidAmount);

public sealed record PaymentUnallocatedBalanceResponse(
    Guid WorkerPaymentId,
    Guid WorkerId,
    string PaymentType,
    DateOnly PaymentDate,
    decimal Amount,
    decimal AllocatedAmount,
    decimal RemainingUnallocatedAmount);
