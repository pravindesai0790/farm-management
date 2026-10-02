using FarmManagement.Domain.Enums;

namespace FarmManagement.Domain.Entities;

public sealed class PurchaseInvoice
{
    private PurchaseInvoice()
    {
        SupplierInvoiceNumber = string.Empty;
        Lines = [];
        PaymentAllocations = [];
        ReceiptLines = [];
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid SupplierId { get; private set; }
    public Guid FarmId { get; private set; }
    public string SupplierInvoiceNumber { get; private set; }
    public DateOnly InvoiceDate { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public Guid CurrencyId { get; private set; }
    public string? PaymentTerms { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal OtherCharges { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public PurchaseInvoiceStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public string? AttachmentReference { get; private set; }
    public DateTimeOffset? PostedAt { get; private set; }
    public Guid? PostedBy { get; private set; }
    public DateTimeOffset? ReversedAt { get; private set; }
    public Guid? ReversedBy { get; private set; }
    public string? ReversalReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public Organization? Organization { get; private set; }
    public Supplier? Supplier { get; private set; }
    public Farm? Farm { get; private set; }
    public Currency? Currency { get; private set; }
    public ICollection<PurchaseInvoiceLine> Lines { get; private set; }
    public ICollection<SupplierPaymentAllocation> PaymentAllocations { get; private set; }
    public ICollection<PurchaseInvoiceReceiptLine> ReceiptLines { get; private set; }

    public static PurchaseInvoice CreateDraft(
        Guid organizationId,
        Guid supplierId,
        Guid farmId,
        string supplierInvoiceNumber,
        DateOnly invoiceDate,
        Guid currencyId,
        Guid createdBy,
        DateOnly? dueDate = null,
        string? paymentTerms = null,
        decimal taxAmount = 0m,
        decimal otherCharges = 0m,
        decimal discountAmount = 0m,
        string? notes = null,
        string? attachmentReference = null)
    {
        ValidateCommon(organizationId, supplierId, farmId, supplierInvoiceNumber, currencyId, createdBy);
        ValidateFinancials(taxAmount, otherCharges, discountAmount);
        ValidateDates(invoiceDate, dueDate);

        var now = DateTimeOffset.UtcNow;
        return new PurchaseInvoice
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            SupplierId = supplierId,
            FarmId = farmId,
            SupplierInvoiceNumber = supplierInvoiceNumber.Trim(),
            InvoiceDate = invoiceDate,
            DueDate = dueDate,
            CurrencyId = currencyId,
            PaymentTerms = NormalizeOptional(paymentTerms, 200),
            TaxAmount = Math.Round(taxAmount, 2, MidpointRounding.AwayFromZero),
            OtherCharges = Math.Round(otherCharges, 2, MidpointRounding.AwayFromZero),
            DiscountAmount = Math.Round(discountAmount, 2, MidpointRounding.AwayFromZero),
            Subtotal = 0m,
            TotalAmount = 0m,
            Status = PurchaseInvoiceStatus.Draft,
            Notes = NormalizeOptional(notes, 1000),
            AttachmentReference = NormalizeOptional(attachmentReference, 500),
            CreatedAt = now,
            CreatedBy = createdBy,
            Lines = [],
            PaymentAllocations = [],
            ReceiptLines = []
        };
    }

    public void UpdateDraft(
        Guid supplierId,
        Guid farmId,
        string supplierInvoiceNumber,
        DateOnly invoiceDate,
        Guid currencyId,
        Guid updatedBy,
        DateOnly? dueDate = null,
        string? paymentTerms = null,
        decimal taxAmount = 0m,
        decimal otherCharges = 0m,
        decimal discountAmount = 0m,
        string? notes = null,
        string? attachmentReference = null,
        DateTimeOffset? now = null)
    {
        if (Status != PurchaseInvoiceStatus.Draft)
        {
            throw new InvalidOperationException($"Only draft invoices can be updated. Current status: {Status}.");
        }

        ValidateCommon(OrganizationId, supplierId, farmId, supplierInvoiceNumber, currencyId, updatedBy);
        ValidateFinancials(taxAmount, otherCharges, discountAmount);
        ValidateDates(invoiceDate, dueDate);

        SupplierId = supplierId;
        FarmId = farmId;
        SupplierInvoiceNumber = supplierInvoiceNumber.Trim();
        InvoiceDate = invoiceDate;
        DueDate = dueDate;
        CurrencyId = currencyId;
        PaymentTerms = NormalizeOptional(paymentTerms, 200);
        TaxAmount = Math.Round(taxAmount, 2, MidpointRounding.AwayFromZero);
        OtherCharges = Math.Round(otherCharges, 2, MidpointRounding.AwayFromZero);
        DiscountAmount = Math.Round(discountAmount, 2, MidpointRounding.AwayFromZero);
        Notes = NormalizeOptional(notes, 1000);
        AttachmentReference = NormalizeOptional(attachmentReference, 500);
        UpdatedAt = now ?? DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;

        RecalculateTotals();
    }

    public void RecalculateTotals()
    {
        Subtotal = Math.Round(Lines.Sum(l => l.LineAmount), 2, MidpointRounding.AwayFromZero);
        TotalAmount = Math.Max(0m, Math.Round(Subtotal + TaxAmount + OtherCharges - DiscountAmount, 2, MidpointRounding.AwayFromZero));
    }

    public void Post(Guid postedBy, DateTimeOffset? now = null)
    {
        if (Status != PurchaseInvoiceStatus.Draft)
        {
            throw new InvalidOperationException($"Only draft invoices can be posted. Current status: {Status}.");
        }

        if (postedBy == Guid.Empty)
        {
            throw new ArgumentException("A posting user is required.", nameof(postedBy));
        }

        if (Lines.Count == 0)
        {
            throw new InvalidOperationException("An invoice must contain at least one line item before posting.");
        }

        RecalculateTotals();

        var timestamp = now ?? DateTimeOffset.UtcNow;
        Status = PurchaseInvoiceStatus.Posted;
        PostedAt = timestamp;
        PostedBy = postedBy;
        UpdatedAt = timestamp;
        UpdatedBy = postedBy;
    }

    public void Reverse(string reason, Guid reversedBy, DateTimeOffset? now = null)
    {
        if (Status != PurchaseInvoiceStatus.Posted)
        {
            throw new InvalidOperationException($"Only posted invoices can be reversed. Current status: {Status}.");
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
        Status = PurchaseInvoiceStatus.Reversed;
        ReversedAt = timestamp;
        ReversedBy = reversedBy;
        ReversalReason = reason.Trim();
        UpdatedAt = timestamp;
        UpdatedBy = reversedBy;
    }

    private static void ValidateCommon(
        Guid organizationId,
        Guid supplierId,
        Guid farmId,
        string supplierInvoiceNumber,
        Guid currencyId,
        Guid userId)
    {
        if (organizationId == Guid.Empty) throw new ArgumentException("An organization is required.", nameof(organizationId));
        if (supplierId == Guid.Empty) throw new ArgumentException("A supplier is required.", nameof(supplierId));
        if (farmId == Guid.Empty) throw new ArgumentException("A farm is required.", nameof(farmId));
        if (string.IsNullOrWhiteSpace(supplierInvoiceNumber)) throw new ArgumentException("A supplier invoice number is required.", nameof(supplierInvoiceNumber));
        if (supplierInvoiceNumber.Trim().Length > 100) throw new ArgumentException("Supplier invoice number cannot exceed 100 characters.", nameof(supplierInvoiceNumber));
        if (currencyId == Guid.Empty) throw new ArgumentException("A currency is required.", nameof(currencyId));
        if (userId == Guid.Empty) throw new ArgumentException("A valid user identifier is required.", nameof(userId));
    }

    private static void ValidateFinancials(decimal taxAmount, decimal otherCharges, decimal discountAmount)
    {
        if (taxAmount < 0m) throw new ArgumentOutOfRangeException(nameof(taxAmount), "Tax amount cannot be negative.");
        if (otherCharges < 0m) throw new ArgumentOutOfRangeException(nameof(otherCharges), "Other charges cannot be negative.");
        if (discountAmount < 0m) throw new ArgumentOutOfRangeException(nameof(discountAmount), "Discount amount cannot be negative.");
    }

    private static void ValidateDates(DateOnly invoiceDate, DateOnly? dueDate)
    {
        if (dueDate.HasValue && dueDate.Value < invoiceDate)
        {
            throw new ArgumentException("Due date cannot be earlier than invoice date.", nameof(dueDate));
        }
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }
}
