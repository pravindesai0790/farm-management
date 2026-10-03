using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Domain;

public class PurchaseInvoiceModelTests
{
    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid SupplierId = Guid.NewGuid();
    private static readonly Guid FarmId = Guid.NewGuid();
    private static readonly Guid CurrencyId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ItemId = Guid.NewGuid();
    private static readonly Guid UnitId = Guid.NewGuid();
    private static readonly Guid CategoryId = Guid.NewGuid();

    [Fact]
    public void CreateDraft_WithValidData_InitializesTotalsToZeroAndStatusDraft()
    {
        var invoice = PurchaseInvoice.CreateDraft(
            OrgId,
            SupplierId,
            FarmId,
            "INV-2026-001",
            new DateOnly(2026, 10, 1),
            CurrencyId,
            UserId,
            dueDate: new DateOnly(2026, 10, 31),
            paymentTerms: "Net 30",
            taxAmount: 15.50m,
            otherCharges: 25.00m,
            discountAmount: 10.00m,
            notes: "Initial test purchase invoice");

        Assert.NotEqual(Guid.Empty, invoice.Id);
        Assert.Equal(OrgId, invoice.OrganizationId);
        Assert.Equal(SupplierId, invoice.SupplierId);
        Assert.Equal(FarmId, invoice.FarmId);
        Assert.Equal("INV-2026-001", invoice.SupplierInvoiceNumber);
        Assert.Equal(new DateOnly(2026, 10, 1), invoice.InvoiceDate);
        Assert.Equal(new DateOnly(2026, 10, 31), invoice.DueDate);
        Assert.Equal("Net 30", invoice.PaymentTerms);
        Assert.Equal(0m, invoice.Subtotal);
        Assert.Equal(0m, invoice.TotalAmount);
        Assert.Equal(15.50m, invoice.TaxAmount);
        Assert.Equal(25.00m, invoice.OtherCharges);
        Assert.Equal(10.00m, invoice.DiscountAmount);
        Assert.Equal(PurchaseInvoiceStatus.Draft, invoice.Status);
    }

    [Fact]
    public void CreateDraft_WithDueDateEarlierThanInvoiceDate_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            PurchaseInvoice.CreateDraft(
                OrgId,
                SupplierId,
                FarmId,
                "INV-001",
                new DateOnly(2026, 10, 15),
                CurrencyId,
                UserId,
                dueDate: new DateOnly(2026, 10, 10)));
    }

    [Fact]
    public void AddLine_InventoryLine_CalculatesLineAmountAccurately()
    {
        var invoice = PurchaseInvoice.CreateDraft(
            OrgId, SupplierId, FarmId, "INV-001", new DateOnly(2026, 10, 1), CurrencyId, UserId);

        var line = PurchaseInvoiceLine.CreateInventoryLine(
            OrgId,
            invoice.Id,
            ItemId,
            UnitId,
            quantity: 25.5m,
            unitPrice: 12.40m,
            description: "NPK Fertilizer 50kg");

        Assert.Equal(InvoiceLineType.InventoryItem, line.LineType);
        Assert.Equal(ItemId, line.InventoryItemId);
        Assert.Equal(UnitId, line.StockUnitId);
        Assert.Equal(25.5m, line.Quantity);
        Assert.Equal(12.40m, line.UnitPrice);
        Assert.Equal(316.20m, line.LineAmount); // 25.5 * 12.40 = 316.20
    }

    [Fact]
    public void AddLine_NonInventoryLine_SetsAmountAndNoQuantity()
    {
        var invoice = PurchaseInvoice.CreateDraft(
            OrgId, SupplierId, FarmId, "INV-001", new DateOnly(2026, 10, 1), CurrencyId, UserId);

        var line = PurchaseInvoiceLine.CreateNonInventoryLine(
            OrgId,
            invoice.Id,
            CategoryId,
            75.50m,
            "Delivery and freight charge");

        Assert.Equal(InvoiceLineType.NonInventoryExpense, line.LineType);
        Assert.Equal(CategoryId, line.ExpenseCategoryId);
        Assert.Null(line.InventoryItemId);
        Assert.Null(line.Quantity);
        Assert.Null(line.StockUnitId);
        Assert.Equal(75.50m, line.UnitPrice);
        Assert.Equal(75.50m, line.LineAmount);
    }

    [Fact]
    public void RecalculateTotals_WithMixedLines_ComputesSubtotalAndTotalAmount()
    {
        var invoice = PurchaseInvoice.CreateDraft(
            OrgId,
            SupplierId,
            FarmId,
            "INV-001",
            new DateOnly(2026, 10, 1),
            CurrencyId,
            UserId,
            taxAmount: 20m,
            otherCharges: 10m,
            discountAmount: 15m);

        var line1 = PurchaseInvoiceLine.CreateInventoryLine(
            OrgId, invoice.Id, ItemId, UnitId, 10m, 15m); // 150.00
        var line2 = PurchaseInvoiceLine.CreateNonInventoryLine(
            OrgId, invoice.Id, CategoryId, 50m, "Inspection Fee"); // 50.00

        invoice.Lines.Add(line1);
        invoice.Lines.Add(line2);
        invoice.RecalculateTotals();

        Assert.Equal(200.00m, invoice.Subtotal); // 150 + 50
        // Total = Subtotal (200) + Tax (20) + Other (10) - Discount (15) = 215.00
        Assert.Equal(215.00m, invoice.TotalAmount);
    }

    [Fact]
    public void Post_WithoutLines_ThrowsInvalidOperationException()
    {
        var invoice = PurchaseInvoice.CreateDraft(
            OrgId, SupplierId, FarmId, "INV-001", new DateOnly(2026, 10, 1), CurrencyId, UserId);

        Assert.Throws<InvalidOperationException>(() => invoice.Post(UserId));
    }

    [Fact]
    public void Post_WithValidLines_TransitionsToPosted()
    {
        var invoice = PurchaseInvoice.CreateDraft(
            OrgId, SupplierId, FarmId, "INV-001", new DateOnly(2026, 10, 1), CurrencyId, UserId);
        var line = PurchaseInvoiceLine.CreateNonInventoryLine(
            OrgId, invoice.Id, CategoryId, 500m, "Consulting fee");
        invoice.Lines.Add(line);

        var postTime = DateTimeOffset.UtcNow;
        invoice.Post(UserId, postTime);

        Assert.Equal(PurchaseInvoiceStatus.Posted, invoice.Status);
        Assert.Equal(UserId, invoice.PostedBy);
        Assert.Equal(postTime, invoice.PostedAt);
        Assert.Equal(500m, invoice.TotalAmount);
    }

    [Fact]
    public void Post_WhenAlreadyPosted_ThrowsInvalidOperationException()
    {
        var invoice = PurchaseInvoice.CreateDraft(
            OrgId, SupplierId, FarmId, "INV-001", new DateOnly(2026, 10, 1), CurrencyId, UserId);
        invoice.Lines.Add(PurchaseInvoiceLine.CreateNonInventoryLine(OrgId, invoice.Id, CategoryId, 100m, "Fee"));
        invoice.Post(UserId);

        Assert.Throws<InvalidOperationException>(() => invoice.Post(UserId));
    }

    [Fact]
    public void UpdateDraft_WhenPosted_ThrowsInvalidOperationException()
    {
        var invoice = PurchaseInvoice.CreateDraft(
            OrgId, SupplierId, FarmId, "INV-001", new DateOnly(2026, 10, 1), CurrencyId, UserId);
        invoice.Lines.Add(PurchaseInvoiceLine.CreateNonInventoryLine(OrgId, invoice.Id, CategoryId, 100m, "Fee"));
        invoice.Post(UserId);

        Assert.Throws<InvalidOperationException>(() =>
            invoice.UpdateDraft(SupplierId, FarmId, "INV-MODIFIED", new DateOnly(2026, 10, 2), CurrencyId, UserId));
    }

    [Fact]
    public void Reverse_WhenPosted_SetsStatusReversedWithReason()
    {
        var invoice = PurchaseInvoice.CreateDraft(
            OrgId, SupplierId, FarmId, "INV-001", new DateOnly(2026, 10, 1), CurrencyId, UserId);
        invoice.Lines.Add(PurchaseInvoiceLine.CreateNonInventoryLine(OrgId, invoice.Id, CategoryId, 100m, "Fee"));
        invoice.Post(UserId);

        var reverserId = Guid.NewGuid();
        var reverseTime = DateTimeOffset.UtcNow;

        invoice.Reverse("Vendor issued incorrect invoice number", reverserId, reverseTime);

        Assert.Equal(PurchaseInvoiceStatus.Reversed, invoice.Status);
        Assert.Equal("Vendor issued incorrect invoice number", invoice.ReversalReason);
        Assert.Equal(reverserId, invoice.ReversedBy);
        Assert.Equal(reverseTime, invoice.ReversedAt);
    }

    [Fact]
    public void Reverse_WhenDraft_ThrowsInvalidOperationException()
    {
        var invoice = PurchaseInvoice.CreateDraft(
            OrgId, SupplierId, FarmId, "INV-001", new DateOnly(2026, 10, 1), CurrencyId, UserId);

        Assert.Throws<InvalidOperationException>(() =>
            invoice.Reverse("Reason", UserId));
    }
}
