using FarmManagement.Application.DTOs.Expenses;
using FarmManagement.Application.Interfaces.Expenses;
using FarmManagement.Application.Services;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Services;

public class ExpenseReportServiceTests
{
    private readonly TestExpenseReportStore _store = new();
    private readonly ExpenseReportService _service;

    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid FarmId = Guid.NewGuid();
    private static readonly Guid CurrencyInr = Guid.NewGuid();
    private static readonly Guid CurrencyUsd = Guid.NewGuid();

    public ExpenseReportServiceTests()
    {
        _service = new ExpenseReportService(_store);

        _store.Currencies.Add(new Currency("INR", "Indian Rupee", "₹", true, 1, CurrencyInr));
        _store.Currencies.Add(new Currency("USD", "US Dollar", "$", true, 2, CurrencyUsd));
    }

    [Fact]
    public async Task GetSummaryReportAsync_GroupsByCurrencyCorrectly()
    {
        // Arrange
        var farm = new Farm(OrgId, "Main Farm", Guid.NewGuid(), UserId);
        var cat = new ExpenseCategory(OrgId, "Fertilizer", createdBy: UserId);
        var currInr = _store.Currencies.First(c => c.Id == CurrencyInr);
        var currUsd = _store.Currencies.First(c => c.Id == CurrencyUsd);

        var exp1 = Expense.CreateDraft(OrgId, farm.Id, cat.Id, new DateOnly(2026, 3, 1), "Fertilizer buy", 500m, CurrencyInr, UserId);
        exp1.Post(UserId);
        SetNavProperties(exp1, farm, cat, currInr);
        _store.PostedExpenses.Add(exp1);

        var exp2 = Expense.CreateDraft(OrgId, farm.Id, cat.Id, new DateOnly(2026, 3, 2), "Imported tools", 100m, CurrencyUsd, UserId);
        exp2.Post(UserId);
        SetNavProperties(exp2, farm, cat, currUsd);
        _store.PostedExpenses.Add(exp2);

        // Act
        var result = await _service.GetSummaryReportAsync(OrgId, new ExpenseReportFilter());

        // Assert
        Assert.Equal(2, result.CurrencySummaries.Count);
        var inrSummary = result.CurrencySummaries.First(c => c.CurrencyCode == "INR");
        Assert.Equal(500m, inrSummary.TotalDirectExpensesAmount);
        Assert.Equal(500m, inrSummary.GrandTotalExpenses);

        var usdSummary = result.CurrencySummaries.First(c => c.CurrencyCode == "USD");
        Assert.Equal(100m, usdSummary.TotalDirectExpensesAmount);
        Assert.Equal(100m, usdSummary.GrandTotalExpenses);
    }

    [Fact]
    public async Task GetSummaryReportAsync_CombinesDirectExpensesAndInvoices()
    {
        // Arrange
        var farm = new Farm(OrgId, "Main Farm", Guid.NewGuid(), UserId);
        var cat = new ExpenseCategory(OrgId, "Seeds", createdBy: UserId);
        var supplier = Supplier.Create(OrgId, "Agri Supplier", UserId);
        var currInr = _store.Currencies.First(c => c.Id == CurrencyInr);

        var exp = Expense.CreateDraft(OrgId, farm.Id, cat.Id, new DateOnly(2026, 3, 1), "Seeds cash", 300m, CurrencyInr, UserId);
        exp.Post(UserId);
        SetNavProperties(exp, farm, cat, currInr);
        _store.PostedExpenses.Add(exp);

        var invoice = PurchaseInvoice.CreateDraft(
            OrgId,
            supplier.Id,
            farm.Id,
            "INV-001",
            new DateOnly(2026, 3, 5),
            CurrencyInr,
            UserId,
            new DateOnly(2026, 3, 20));

        var line = PurchaseInvoiceLine.CreateNonInventoryLine(
            OrgId,
            invoice.Id,
            cat.Id,
            700m,
            "Seed purchase");

        invoice.Lines.Add(line);
        invoice.RecalculateTotals();
        invoice.Post(UserId);

        typeof(PurchaseInvoice).GetProperty(nameof(PurchaseInvoice.Supplier))?.SetValue(invoice, supplier);
        typeof(PurchaseInvoice).GetProperty(nameof(PurchaseInvoice.Farm))?.SetValue(invoice, farm);
        typeof(PurchaseInvoice).GetProperty(nameof(PurchaseInvoice.Currency))?.SetValue(invoice, currInr);
        typeof(PurchaseInvoiceLine).GetProperty(nameof(PurchaseInvoiceLine.ExpenseCategory))?.SetValue(line, cat);

        _store.PostedInvoices.Add(invoice);

        // Act
        var result = await _service.GetSummaryReportAsync(OrgId, new ExpenseReportFilter());

        // Assert
        Assert.Single(result.CurrencySummaries);
        var summary = result.CurrencySummaries[0];
        Assert.Equal(300m, summary.TotalDirectExpensesAmount);
        Assert.Equal(700m, summary.TotalInvoicedAmount);
        Assert.Equal(1000m, summary.GrandTotalExpenses);
        Assert.Equal(0m, summary.TotalSettledAmount);
        Assert.Equal(700m, summary.TotalOutstandingPayables);

        Assert.Single(summary.CategoryBreakdown);
        Assert.Equal(1000m, summary.CategoryBreakdown[0].TotalAmount);
        Assert.Equal(100m, summary.CategoryBreakdown[0].Percentage);
    }

    private static void SetNavProperties(Expense expense, Farm farm, ExpenseCategory category, Currency currency)
    {
        typeof(Expense).GetProperty(nameof(Expense.Farm))?.SetValue(expense, farm);
        typeof(Expense).GetProperty(nameof(Expense.ExpenseCategory))?.SetValue(expense, category);
        typeof(Expense).GetProperty(nameof(Expense.Currency))?.SetValue(expense, currency);
    }

    private sealed class TestExpenseReportStore : IExpenseReportStore
    {
        public List<Expense> PostedExpenses { get; } = new();
        public List<PurchaseInvoice> PostedInvoices { get; } = new();
        public List<Currency> Currencies { get; } = new();

        public Task<IReadOnlyList<Expense>> GetPostedExpensesAsync(
            Guid organizationId,
            ExpenseReportFilter filter,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Expense>>(PostedExpenses);

        public Task<IReadOnlyList<PurchaseInvoice>> GetPostedInvoicesAsync(
            Guid organizationId,
            ExpenseReportFilter filter,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PurchaseInvoice>>(PostedInvoices);

        public Task<IReadOnlyList<Currency>> GetActiveCurrenciesAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Currency>>(Currencies);
    }
}
