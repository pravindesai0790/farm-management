namespace FarmManagement.Domain.Entities;

/// <summary>
/// Links a line item from a supplier purchase invoice to a physical stock movement receipt in the inventory ledger.
/// Represents a partial or full delivery receipt against an invoiced inventory line.
/// </summary>
public sealed class PurchaseInvoiceReceiptLine
{
    private PurchaseInvoiceReceiptLine()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid PurchaseInvoiceId { get; private set; }
    public Guid PurchaseInvoiceLineId { get; private set; }
    public Guid StockMovementId { get; private set; }

    /// <summary>
    /// Identifier grouping multiple receipt lines created together in a single receipt delivery batch.
    /// </summary>
    public Guid ReceiptGroupId { get; private set; }

    /// <summary>
    /// Optional client-supplied key to guarantee idempotent processing of receipt submissions.
    /// </summary>
    public string? IdempotencyKey { get; private set; }

    public decimal ReceivedQuantity { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }

    public PurchaseInvoice? PurchaseInvoice { get; private set; }
    public PurchaseInvoiceLine? PurchaseInvoiceLine { get; private set; }
    public StockMovement? StockMovement { get; private set; }
    public Organization? Organization { get; private set; }

    /// <summary>
    /// Factory method for creating a purchase invoice receipt line linked to a stock movement.
    /// </summary>
    public static PurchaseInvoiceReceiptLine Create(
        Guid organizationId,
        Guid purchaseInvoiceId,
        Guid purchaseInvoiceLineId,
        Guid stockMovementId,
        decimal receivedQuantity,
        Guid createdBy,
        Guid? receiptGroupId = null,
        string? idempotencyKey = null)
    {
        if (organizationId == Guid.Empty) throw new ArgumentException("An organization is required.", nameof(organizationId));
        if (purchaseInvoiceId == Guid.Empty) throw new ArgumentException("A purchase invoice is required.", nameof(purchaseInvoiceId));
        if (purchaseInvoiceLineId == Guid.Empty) throw new ArgumentException("A purchase invoice line is required.", nameof(purchaseInvoiceLineId));
        if (stockMovementId == Guid.Empty) throw new ArgumentException("A stock movement is required.", nameof(stockMovementId));
        if (receivedQuantity <= 0m) throw new ArgumentOutOfRangeException(nameof(receivedQuantity), "Received quantity must be greater than zero.");
        if (createdBy == Guid.Empty) throw new ArgumentException("A creating user is required.", nameof(createdBy));

        var cleanKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim();
        if (cleanKey != null && cleanKey.Length > 100)
        {
            cleanKey = cleanKey[..100];
        }

        return new PurchaseInvoiceReceiptLine
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            PurchaseInvoiceId = purchaseInvoiceId,
            PurchaseInvoiceLineId = purchaseInvoiceLineId,
            StockMovementId = stockMovementId,
            ReceiptGroupId = receiptGroupId ?? Guid.NewGuid(),
            IdempotencyKey = cleanKey,
            ReceivedQuantity = receivedQuantity,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy
        };
    }
}
