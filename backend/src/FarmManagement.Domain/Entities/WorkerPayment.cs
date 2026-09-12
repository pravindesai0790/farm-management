using FarmManagement.Domain.Enums;

namespace FarmManagement.Domain.Entities;

public sealed class WorkerPayment
{
    private WorkerPayment()
    {
        Allocations = [];
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid WorkerId { get; private set; }
    public DateOnly PaymentDate { get; private set; }
    public PaymentType PaymentType { get; private set; }
    public decimal Amount { get; private set; }
    public Guid CurrencyId { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public string? ReferenceNumber { get; private set; }
    public DateOnly? PaymentPeriodFrom { get; private set; }
    public DateOnly? PaymentPeriodTo { get; private set; }
    public PaymentStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public Guid? CancelledBy { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public Organization? Organization { get; private set; }
    public Worker? Worker { get; private set; }
    public Currency? Currency { get; private set; }
    public ICollection<WorkerPaymentAllocation> Allocations { get; private set; }

    public static WorkerPayment Create(
        Guid organizationId,
        Guid workerId,
        DateOnly paymentDate,
        PaymentType paymentType,
        decimal amount,
        Guid currencyId,
        PaymentMethod paymentMethod,
        Guid createdBy,
        string? referenceNumber = null,
        DateOnly? paymentPeriodFrom = null,
        DateOnly? paymentPeriodTo = null,
        PaymentStatus status = PaymentStatus.Completed,
        string? notes = null)
    {
        ValidateCommonIdentifiers(organizationId, workerId, currencyId, createdBy);

        if (!Enum.IsDefined(paymentType))
        {
            throw new ArgumentOutOfRangeException(nameof(paymentType), "The payment type is invalid.");
        }

        if (!Enum.IsDefined(paymentMethod))
        {
            throw new ArgumentOutOfRangeException(nameof(paymentMethod), "The payment method is invalid.");
        }

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), "The payment status is invalid.");
        }

        if (status == PaymentStatus.Cancelled)
        {
            throw new ArgumentException("A payment cannot be initially created with cancelled status.", nameof(status));
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "The payment amount must be greater than zero.");
        }

        ValidatePeriodDates(paymentPeriodFrom, paymentPeriodTo);

        var now = DateTimeOffset.UtcNow;
        return new WorkerPayment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            WorkerId = workerId,
            PaymentDate = paymentDate,
            PaymentType = paymentType,
            Amount = amount,
            CurrencyId = currencyId,
            PaymentMethod = paymentMethod,
            ReferenceNumber = NormalizeOptional(referenceNumber),
            PaymentPeriodFrom = paymentPeriodFrom,
            PaymentPeriodTo = paymentPeriodTo,
            Status = status,
            Notes = NormalizeOptional(notes),
            CreatedAt = now,
            CreatedBy = createdBy
        };
    }

    public static WorkerPayment CreateAdvance(
        Guid organizationId,
        Guid workerId,
        DateOnly paymentDate,
        decimal amount,
        Guid currencyId,
        PaymentMethod paymentMethod,
        Guid createdBy,
        string? referenceNumber = null,
        string? notes = null) =>
        Create(
            organizationId,
            workerId,
            paymentDate,
            PaymentType.Advance,
            amount,
            currencyId,
            paymentMethod,
            createdBy,
            referenceNumber,
            notes: notes);

    public static WorkerPayment CreatePayout(
        Guid organizationId,
        Guid workerId,
        DateOnly paymentDate,
        decimal amount,
        Guid currencyId,
        PaymentMethod paymentMethod,
        Guid createdBy,
        DateOnly? paymentPeriodFrom = null,
        DateOnly? paymentPeriodTo = null,
        string? referenceNumber = null,
        string? notes = null) =>
        Create(
            organizationId,
            workerId,
            paymentDate,
            PaymentType.Payout,
            amount,
            currencyId,
            paymentMethod,
            createdBy,
            referenceNumber,
            paymentPeriodFrom,
            paymentPeriodTo,
            notes: notes);

    public bool Cancel(DateTimeOffset now, Guid cancelledBy, string? reason)
    {
        if (cancelledBy == Guid.Empty)
        {
            throw new ArgumentException("A user is required.", nameof(cancelledBy));
        }

        if (Status == PaymentStatus.Cancelled)
        {
            return false;
        }

        Status = PaymentStatus.Cancelled;
        CancelledAt = now;
        CancelledBy = cancelledBy;
        CancellationReason = NormalizeOptional(reason);
        UpdatedAt = now;
        UpdatedBy = cancelledBy;
        return true;
    }

    public bool Complete(DateTimeOffset now, Guid completedBy)
    {
        if (completedBy == Guid.Empty)
        {
            throw new ArgumentException("A user is required.", nameof(completedBy));
        }

        if (Status == PaymentStatus.Cancelled)
        {
            throw new InvalidOperationException("A cancelled payment cannot be completed.");
        }

        if (Status == PaymentStatus.Completed)
        {
            return false;
        }

        Status = PaymentStatus.Completed;
        UpdatedAt = now;
        UpdatedBy = completedBy;
        return true;
    }

    public void ValidateOrganizationBoundary(Worker? worker, Currency? currency)
    {
        if (worker is not null && worker.OrganizationId != OrganizationId)
        {
            throw new InvalidOperationException("The worker belongs to a different organization.");
        }
    }

    private static void ValidateCommonIdentifiers(Guid organizationId, Guid workerId, Guid currencyId, Guid createdBy)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("An organization is required.", nameof(organizationId));
        }

        if (workerId == Guid.Empty)
        {
            throw new ArgumentException("A worker is required.", nameof(workerId));
        }

        if (currencyId == Guid.Empty)
        {
            throw new ArgumentException("A currency is required.", nameof(currencyId));
        }

        if (createdBy == Guid.Empty)
        {
            throw new ArgumentException("A creating user is required.", nameof(createdBy));
        }
    }

    private static void ValidatePeriodDates(DateOnly? paymentPeriodFrom, DateOnly? paymentPeriodTo)
    {
        if (paymentPeriodFrom.HasValue && paymentPeriodTo.HasValue && paymentPeriodTo.Value < paymentPeriodFrom.Value)
        {
            throw new ArgumentException("The payment period to date cannot be earlier than from date.", nameof(paymentPeriodTo));
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
