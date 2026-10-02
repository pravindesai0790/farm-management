namespace FarmManagement.Domain.Entities;

public sealed class SupplierPaymentAllocation
{
    private SupplierPaymentAllocation()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid SupplierPaymentId { get; private set; }
    public Guid PurchaseInvoiceId { get; private set; }
    public decimal AllocatedAmount { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }

    public SupplierPayment? SupplierPayment { get; private set; }
    public PurchaseInvoice? PurchaseInvoice { get; private set; }
    public Organization? Organization { get; private set; }

    public static SupplierPaymentAllocation Create(
        Guid organizationId,
        Guid supplierPaymentId,
        Guid purchaseInvoiceId,
        decimal allocatedAmount,
        Guid createdBy)
    {
        if (organizationId == Guid.Empty) throw new ArgumentException("An organization is required.", nameof(organizationId));
        if (supplierPaymentId == Guid.Empty) throw new ArgumentException("A supplier payment is required.", nameof(supplierPaymentId));
        if (purchaseInvoiceId == Guid.Empty) throw new ArgumentException("A purchase invoice is required.", nameof(purchaseInvoiceId));
        if (allocatedAmount <= 0m) throw new ArgumentOutOfRangeException(nameof(allocatedAmount), "Allocated amount must be greater than zero.");
        if (createdBy == Guid.Empty) throw new ArgumentException("A creating user is required.", nameof(createdBy));

        return new SupplierPaymentAllocation
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            SupplierPaymentId = supplierPaymentId,
            PurchaseInvoiceId = purchaseInvoiceId,
            AllocatedAmount = Math.Round(allocatedAmount, 2, MidpointRounding.AwayFromZero),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy
        };
    }
}
