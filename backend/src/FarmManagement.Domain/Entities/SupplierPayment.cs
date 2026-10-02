using FarmManagement.Domain.Enums;

namespace FarmManagement.Domain.Entities;

public sealed class SupplierPayment
{
    private SupplierPayment()
    {
        Allocations = [];
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid SupplierId { get; private set; }
    public DateOnly PaymentDate { get; private set; }
    public decimal Amount { get; private set; }
    public Guid CurrencyId { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public string? ReferenceNumber { get; private set; }
    public string? Notes { get; private set; }
    public SupplierPaymentStatus Status { get; private set; }
    public string? IdempotencyKey { get; private set; }
    public DateTimeOffset? ReversedAt { get; private set; }
    public Guid? ReversedBy { get; private set; }
    public string? ReversalReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public Organization? Organization { get; private set; }
    public Supplier? Supplier { get; private set; }
    public Currency? Currency { get; private set; }
    public ICollection<SupplierPaymentAllocation> Allocations { get; private set; }

    public static SupplierPayment Create(
        Guid organizationId,
        Guid supplierId,
        DateOnly paymentDate,
        decimal amount,
        Guid currencyId,
        PaymentMethod paymentMethod,
        Guid createdBy,
        string? referenceNumber = null,
        string? notes = null,
        string? idempotencyKey = null)
    {
        if (organizationId == Guid.Empty) throw new ArgumentException("An organization is required.", nameof(organizationId));
        if (supplierId == Guid.Empty) throw new ArgumentException("A supplier is required.", nameof(supplierId));
        if (amount <= 0m) throw new ArgumentOutOfRangeException(nameof(amount), "Payment amount must be greater than zero.");
        if (currencyId == Guid.Empty) throw new ArgumentException("A currency is required.", nameof(currencyId));
        if (!Enum.IsDefined(paymentMethod)) throw new ArgumentOutOfRangeException(nameof(paymentMethod), "Invalid payment method.");
        if (createdBy == Guid.Empty) throw new ArgumentException("A creating user is required.", nameof(createdBy));

        var now = DateTimeOffset.UtcNow;
        return new SupplierPayment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            SupplierId = supplierId,
            PaymentDate = paymentDate,
            Amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero),
            CurrencyId = currencyId,
            PaymentMethod = paymentMethod,
            ReferenceNumber = NormalizeOptional(referenceNumber, 100),
            Notes = NormalizeOptional(notes, 1000),
            Status = SupplierPaymentStatus.Completed,
            IdempotencyKey = NormalizeOptional(idempotencyKey, 100),
            CreatedAt = now,
            CreatedBy = createdBy,
            Allocations = []
        };
    }

    public void AddAllocation(SupplierPaymentAllocation allocation)
    {
        ArgumentNullException.ThrowIfNull(allocation);

        if (Status != SupplierPaymentStatus.Completed)
        {
            throw new InvalidOperationException($"Cannot allocate payment in status {Status}.");
        }

        Allocations.Add(allocation);
    }

    public void Reverse(string reason, Guid reversedBy, DateTimeOffset? now = null)
    {
        if (Status != SupplierPaymentStatus.Completed)
        {
            throw new InvalidOperationException($"Only completed payments can be reversed. Current status: {Status}.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A reversal reason is required.", nameof(reason));
        }

        if (reversedBy == Guid.Empty)
        {
            throw new ArgumentException("A reversing user is required.", nameof(reversedBy));
        }

        var timestamp = now ?? DateTimeOffset.UtcNow;
        Status = SupplierPaymentStatus.Reversed;
        ReversedAt = timestamp;
        ReversedBy = reversedBy;
        ReversalReason = reason.Trim();
        UpdatedAt = timestamp;
        UpdatedBy = reversedBy;
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }
}
