using FarmManagement.Application.DTOs.Expenses;
using FarmManagement.Application.Interfaces.Expenses;
using FarmManagement.Application.Services;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Services;

public class SupplierBalanceServiceTests
{
    private readonly TestSupplierBalanceStore _store = new();
    private readonly SupplierBalanceService _service;

    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid FarmId = Guid.NewGuid();
    private static readonly Guid CurrencyInr = Guid.NewGuid();
    private static readonly Guid CatId = Guid.NewGuid();

    public SupplierBalanceServiceTests()
    {
        _service = new SupplierBalanceService(_store);

        var currency = new Currency("INR", "Indian Rupee", "₹", true, 1, CurrencyInr);
        var supplier = Supplier.Create(OrgId, "Seed Supplier Co", UserId);

        _store.Currencies.Add(currency);
        _store.Suppliers.Add(supplier);
    }

    [Fact]
    public async Task GetSupplierBalancesAsync_CalculatesAgingBucketsCorrectly()
    {
        // Arrange
        var today = new DateOnly(2026, 3, 15);
        var supplier = _store.Suppliers[0];
        var currency = _store.Currencies.First(c => c.Id == CurrencyInr);
        var farm = new Farm(OrgId, "North Farm", Guid.NewGuid(), UserId);

        // 1. Current / Not Due (Due in future: March 20)
        var inv1 = CreateInvoice(OrgId, supplier.Id, farm.Id, "INV-NOT-DUE", new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 20), CurrencyInr, 1000m, supplier, farm, currency);
        _store.Invoices.Add(inv1);

        // 2. 1-30 days overdue (Due Feb 20 -> 23 days overdue on Mar 15)
        var inv2 = CreateInvoice(OrgId, supplier.Id, farm.Id, "INV-1-30", new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 20), CurrencyInr, 2000m, supplier, farm, currency);
        _store.Invoices.Add(inv2);

        // 3. >90 days overdue (Due Nov 15, 2025 -> 120 days overdue)
        var inv3 = CreateInvoice(OrgId, supplier.Id, farm.Id, "INV-OVER-90", new DateOnly(2025, 11, 1), new DateOnly(2025, 11, 15), CurrencyInr, 5000m, supplier, farm, currency);
        _store.Invoices.Add(inv3);

        var filter = new SupplierBalanceFilter(AsOfDate: today);

        // Act
        var result = await _service.GetSupplierBalancesAsync(OrgId, filter);

        // Assert
        Assert.Single(result.CurrencySummaries);
        var currSummary = result.CurrencySummaries[0];
        Assert.Equal(8000m, currSummary.TotalOutstandingBalance);
        Assert.Equal(7000m, currSummary.TotalOverdueBalance);
        Assert.Equal(1000m, currSummary.TotalCurrentNotDueBalance);

        Assert.Equal(1000m, currSummary.AgingTotals.CurrentNotDue);
        Assert.Equal(2000m, currSummary.AgingTotals.Days1To30);
        Assert.Equal(0m, currSummary.AgingTotals.Days31To60);
        Assert.Equal(0m, currSummary.AgingTotals.Days61To90);
        Assert.Equal(5000m, currSummary.AgingTotals.DaysOver90);

        Assert.Single(currSummary.SupplierBalances);
        var supItem = currSummary.SupplierBalances[0];
        Assert.Equal(3, supItem.OpenInvoices.Count);
    }

    [Fact]
    public async Task GetOverdueInvoicesAsync_ReturnsOverdueOnlySortedByDays()
    {
        // Arrange
        var today = new DateOnly(2026, 3, 15);
        var supplier = _store.Suppliers[0];
        var currency = _store.Currencies.First(c => c.Id == CurrencyInr);
        var farm = new Farm(OrgId, "North Farm", Guid.NewGuid(), UserId);

        var invRecent = CreateInvoice(OrgId, supplier.Id, farm.Id, "INV-RECENT", new DateOnly(2026, 2, 20), new DateOnly(2026, 3, 5), CurrencyInr, 500m, supplier, farm, currency);
        _store.Invoices.Add(invRecent);

        var invOld = CreateInvoice(OrgId, supplier.Id, farm.Id, "INV-OLD", new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 15), CurrencyInr, 1500m, supplier, farm, currency);
        _store.Invoices.Add(invOld);

        var filter = new SupplierBalanceFilter(AsOfDate: today);

        // Act
        var overdue = await _service.GetOverdueInvoicesAsync(OrgId, filter);

        // Assert
        Assert.Equal(2, overdue.Count);
        Assert.Equal("INV-OLD", overdue[0].SupplierInvoiceNumber);
        Assert.Equal("INV-RECENT", overdue[1].SupplierInvoiceNumber);
    }

    private static PurchaseInvoice CreateInvoice(
        Guid orgId,
        Guid supplierId,
        Guid farmId,
        string invNum,
        DateOnly invDate,
        DateOnly dueDate,
        Guid currencyId,
        decimal amount,
        Supplier supplier,
        Farm farm,
        Currency currency)
    {
        var invoice = PurchaseInvoice.CreateDraft(
            orgId,
            supplierId,
            farmId,
            invNum,
            invDate,
            currencyId,
            UserId,
            dueDate);

        var line = PurchaseInvoiceLine.CreateNonInventoryLine(
            orgId,
            invoice.Id,
            CatId,
            amount,
            "Item line");

        invoice.Lines.Add(line);
        invoice.RecalculateTotals();
        invoice.Post(UserId);

        typeof(PurchaseInvoice).GetProperty(nameof(PurchaseInvoice.Supplier))?.SetValue(invoice, supplier);
        typeof(PurchaseInvoice).GetProperty(nameof(PurchaseInvoice.Farm))?.SetValue(invoice, farm);
        typeof(PurchaseInvoice).GetProperty(nameof(PurchaseInvoice.Currency))?.SetValue(invoice, currency);

        return invoice;
    }

    private sealed class TestSupplierBalanceStore : ISupplierBalanceStore
    {
        public List<PurchaseInvoice> Invoices { get; } = new();
        public List<Supplier> Suppliers { get; } = new();
        public List<Currency> Currencies { get; } = new();

        public Task<IReadOnlyList<PurchaseInvoice>> GetOpenOrUnpaidInvoicesAsync(
            Guid organizationId,
            SupplierBalanceFilter filter,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PurchaseInvoice>>(Invoices);

        public Task<IReadOnlyList<Supplier>> GetActiveSuppliersAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Supplier>>(Suppliers);

        public Task<IReadOnlyList<Currency>> GetActiveCurrenciesAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Currency>>(Currencies);
    }
}
