using FarmManagement.Domain.Enums;

namespace FarmManagement.Domain.Entities;

public sealed class PurchaseInvoiceLine
{
    private PurchaseInvoiceLine()
    {
        Description = string.Empty;
        ReceiptLines = [];
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid PurchaseInvoiceId { get; private set; }
    public InvoiceLineType LineType { get; private set; }
    public Guid? InventoryItemId { get; private set; }
    public Guid? ExpenseCategoryId { get; private set; }
    public string Description { get; private set; }
    public decimal? Quantity { get; private set; }
    public Guid? StockUnitId { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal LineAmount { get; private set; }
    public Guid? FarmAreaId { get; private set; }
    public Guid? PlantationId { get; private set; }
    public Guid? CropCycleId { get; private set; }
    public int SortOrder { get; private set; }

    public PurchaseInvoice? PurchaseInvoice { get; private set; }
    public Organization? Organization { get; private set; }
    public InventoryItem? InventoryItem { get; private set; }
    public ExpenseCategory? ExpenseCategory { get; private set; }
    public Unit? StockUnit { get; private set; }
    public FarmArea? FarmArea { get; private set; }
    public CropPlantation? Plantation { get; private set; }
    public CropCycle? CropCycle { get; private set; }
    public ICollection<PurchaseInvoiceReceiptLine> ReceiptLines { get; private set; }

    public static PurchaseInvoiceLine CreateInventoryLine(
        Guid organizationId,
        Guid purchaseInvoiceId,
        Guid inventoryItemId,
        Guid stockUnitId,
        decimal quantity,
        decimal unitPrice,
        string? description = null,
        Guid? farmAreaId = null,
        Guid? plantationId = null,
        Guid? cropCycleId = null,
        int sortOrder = 0)
    {
        if (organizationId == Guid.Empty) throw new ArgumentException("An organization is required.", nameof(organizationId));
        if (purchaseInvoiceId == Guid.Empty) throw new ArgumentException("A purchase invoice is required.", nameof(purchaseInvoiceId));
        if (inventoryItemId == Guid.Empty) throw new ArgumentException("An inventory item is required.", nameof(inventoryItemId));
        if (stockUnitId == Guid.Empty) throw new ArgumentException("A stock unit is required.", nameof(stockUnitId));
        if (quantity <= 0m) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        if (unitPrice < 0m) throw new ArgumentOutOfRangeException(nameof(unitPrice), "Unit price cannot be negative.");

        var lineAmount = Math.Round(quantity * unitPrice, 2, MidpointRounding.AwayFromZero);

        return new PurchaseInvoiceLine
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            PurchaseInvoiceId = purchaseInvoiceId,
            LineType = InvoiceLineType.InventoryItem,
            InventoryItemId = inventoryItemId,
            StockUnitId = stockUnitId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            LineAmount = lineAmount,
            Description = NormalizeOptional(description, 500) ?? "Inventory item line",
            FarmAreaId = farmAreaId,
            PlantationId = plantationId,
            CropCycleId = cropCycleId,
            SortOrder = sortOrder,
            ReceiptLines = []
        };
    }

    public static PurchaseInvoiceLine CreateNonInventoryLine(
        Guid organizationId,
        Guid purchaseInvoiceId,
        Guid expenseCategoryId,
        string description,
        decimal amount,
        Guid? farmAreaId = null,
        Guid? plantationId = null,
        Guid? cropCycleId = null,
        int sortOrder = 0)
    {
        if (organizationId == Guid.Empty) throw new ArgumentException("An organization is required.", nameof(organizationId));
        if (purchaseInvoiceId == Guid.Empty) throw new ArgumentException("A purchase invoice is required.", nameof(purchaseInvoiceId));
        if (expenseCategoryId == Guid.Empty) throw new ArgumentException("An expense category is required.", nameof(expenseCategoryId));
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("A description is required for non-inventory lines.", nameof(description));
        if (description.Trim().Length > 500) throw new ArgumentException("Description cannot exceed 500 characters.", nameof(description));
        if (amount <= 0m) throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");

        var roundedAmount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);

        return new PurchaseInvoiceLine
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            PurchaseInvoiceId = purchaseInvoiceId,
            LineType = InvoiceLineType.NonInventoryExpense,
            ExpenseCategoryId = expenseCategoryId,
            Description = description.Trim(),
            UnitPrice = roundedAmount,
            LineAmount = roundedAmount,
            Quantity = null,
            StockUnitId = null,
            InventoryItemId = null,
            FarmAreaId = farmAreaId,
            PlantationId = plantationId,
            CropCycleId = cropCycleId,
            SortOrder = sortOrder,
            ReceiptLines = []
        };
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }
}
