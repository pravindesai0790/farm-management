using FarmManagement.Domain.Enums;

namespace FarmManagement.Domain.Entities;

public sealed class WorkerPaymentAllocation
{
    private WorkerPaymentAllocation()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid WorkerPaymentId { get; private set; }
    public Guid? WorkerEarningsLedgerId { get; private set; }
    public decimal AllocatedAmount { get; private set; }
    public PaymentAllocationType AllocationType { get; private set; }
    public DateOnly AllocationDate { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public Organization? Organization { get; private set; }
    public WorkerPayment? WorkerPayment { get; private set; }
    public WorkerEarningsLedger? WorkerEarningsLedger { get; private set; }

    public static WorkerPaymentAllocation Create(
        Guid organizationId,
        Guid workerPaymentId,
        Guid? workerEarningsLedgerId,
        decimal allocatedAmount,
        PaymentAllocationType allocationType,
        DateOnly allocationDate,
        Guid createdBy,
        string? notes = null)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("An organization is required.", nameof(organizationId));
        }

        if (workerPaymentId == Guid.Empty)
        {
            throw new ArgumentException("A payment is required.", nameof(workerPaymentId));
        }

        if (createdBy == Guid.Empty)
        {
            throw new ArgumentException("A creating user is required.", nameof(createdBy));
        }

        if (allocatedAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(allocatedAmount), "The allocated amount must be greater than zero.");
        }

        if (!Enum.IsDefined(allocationType))
        {
            throw new ArgumentOutOfRangeException(nameof(allocationType), "The allocation type is invalid.");
        }

        if (workerEarningsLedgerId == Guid.Empty)
        {
            throw new ArgumentException("The earnings ledger ID cannot be an empty GUID.", nameof(workerEarningsLedgerId));
        }

        var now = DateTimeOffset.UtcNow;
        return new WorkerPaymentAllocation
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            WorkerPaymentId = workerPaymentId,
            WorkerEarningsLedgerId = workerEarningsLedgerId,
            AllocatedAmount = allocatedAmount,
            AllocationType = allocationType,
            AllocationDate = allocationDate,
            Notes = NormalizeOptional(notes),
            CreatedAt = now,
            CreatedBy = createdBy
        };
    }

    public static WorkerPaymentAllocation CreateEarningSettlement(
        Guid organizationId,
        Guid workerPaymentId,
        Guid workerEarningsLedgerId,
        decimal allocatedAmount,
        DateOnly allocationDate,
        Guid createdBy,
        string? notes = null) =>
        Create(
            organizationId,
            workerPaymentId,
            workerEarningsLedgerId,
            allocatedAmount,
            PaymentAllocationType.EarningSettlement,
            allocationDate,
            createdBy,
            notes);

    public static WorkerPaymentAllocation CreateAdvanceOffset(
        Guid organizationId,
        Guid workerPaymentId,
        Guid workerEarningsLedgerId,
        decimal allocatedAmount,
        DateOnly allocationDate,
        Guid createdBy,
        string? notes = null) =>
        Create(
            organizationId,
            workerPaymentId,
            workerEarningsLedgerId,
            allocatedAmount,
            PaymentAllocationType.AdvanceOffset,
            allocationDate,
            createdBy,
            notes);

    public void ValidateOrganizationBoundary(WorkerPayment? payment, WorkerEarningsLedger? earning)
    {
        if (payment is not null && payment.OrganizationId != OrganizationId)
        {
            throw new InvalidOperationException("The payment belongs to a different organization.");
        }

        if (earning is not null)
        {
            if (earning.OrganizationId != OrganizationId)
            {
                throw new InvalidOperationException("The earnings ledger entry belongs to a different organization.");
            }

            if (payment is not null && earning.WorkerId != payment.WorkerId)
            {
                throw new InvalidOperationException("The payment and earnings ledger entry belong to different workers.");
            }
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
