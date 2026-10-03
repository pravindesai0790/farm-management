using System.Globalization;
using FarmManagement.Application.DTOs.Expenses;
using FarmManagement.Application.Interfaces.Expenses;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.Services;

public sealed class ExpenseReportService(IExpenseReportStore reportStore) : IExpenseReportService
{
    public async Task<ExpenseReportSummaryResponse> GetSummaryReportAsync(
        Guid organizationId,
        ExpenseReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        var postedExpenses = await reportStore.GetPostedExpensesAsync(organizationId, filter, cancellationToken);
        var postedInvoices = await reportStore.GetPostedInvoicesAsync(organizationId, filter, cancellationToken);
        var activeCurrencies = await reportStore.GetActiveCurrenciesAsync(cancellationToken);

        var currencyMap = activeCurrencies.ToDictionary(c => c.Id);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Group currency IDs present in either expenses or invoices
        var currencyIds = postedExpenses.Select(e => e.CurrencyId)
            .Concat(postedInvoices.Select(pi => pi.CurrencyId))
            .Distinct();

        if (filter.CurrencyId.HasValue)
        {
            currencyIds = currencyIds.Where(id => id == filter.CurrencyId.Value);
        }

        var currencySummaries = new List<ExpenseReportCurrencySummary>();

        foreach (var currencyId in currencyIds)
        {
            if (!currencyMap.TryGetValue(currencyId, out var currency))
            {
                continue;
            }

            var directExpenses = postedExpenses.Where(e => e.CurrencyId == currencyId).ToList();
            var invoices = postedInvoices.Where(pi => pi.CurrencyId == currencyId).ToList();

            var totalDirectAmount = directExpenses.Sum(e => e.Amount);
            var totalInvoicedAmount = invoices.Sum(pi => pi.TotalAmount);
            var totalSettledAmount = invoices.Sum(GetAmountPaid);
            var totalOutstandingPayables = invoices.Sum(GetOutstandingBalance);
            var grandTotal = totalDirectAmount + totalInvoicedAmount;

            // Category breakdown
            var categoryItems = BuildCategoryBreakdown(directExpenses, invoices, grandTotal);

            // Farm breakdown
            var farmItems = BuildFarmBreakdown(directExpenses, invoices, grandTotal);

            // Monthly trend
            var monthlyTrend = BuildMonthlyTrend(directExpenses, invoices);

            // Top overdue invoices
            var topOverdue = invoices
                .Where(pi => GetOutstandingBalance(pi) > 0 && pi.DueDate.HasValue && pi.DueDate.Value < today)
                .OrderByDescending(pi => today.DayNumber - pi.DueDate!.Value.DayNumber)
                .ThenByDescending(GetOutstandingBalance)
                .Take(5)
                .Select(pi => new OverdueInvoiceItem(
                    pi.Id,
                    pi.SupplierInvoiceNumber,
                    pi.SupplierId,
                    pi.Supplier?.Name ?? "Unknown Supplier",
                    pi.FarmId,
                    pi.Farm?.Name ?? "Unknown Farm",
                    pi.InvoiceDate,
                    pi.DueDate!.Value,
                    today.DayNumber - pi.DueDate.Value.DayNumber,
                    pi.TotalAmount,
                    GetAmountPaid(pi),
                    GetOutstandingBalance(pi),
                    currency.Id,
                    currency.Code,
                    currency.Symbol))
                .ToList();

            currencySummaries.Add(new ExpenseReportCurrencySummary(
                currency.Id,
                currency.Code,
                currency.Symbol,
                totalDirectAmount,
                directExpenses.Count,
                totalInvoicedAmount,
                invoices.Count,
                totalSettledAmount,
                totalOutstandingPayables,
                grandTotal,
                categoryItems,
                farmItems,
                monthlyTrend,
                topOverdue));
        }

        return new ExpenseReportSummaryResponse(filter, currencySummaries);
    }

    private static decimal GetAmountPaid(PurchaseInvoice invoice)
    {
        var activeAllocations = invoice.PaymentAllocations
            .Where(a => a.SupplierPayment != null && a.SupplierPayment.Status == SupplierPaymentStatus.Completed);
        return Math.Round(activeAllocations.Sum(a => a.AllocatedAmount), 2, MidpointRounding.AwayFromZero);
    }

    private static decimal GetOutstandingBalance(PurchaseInvoice invoice)
    {
        if (invoice.Status == PurchaseInvoiceStatus.Reversed)
        {
            return 0m;
        }

        var paid = GetAmountPaid(invoice);
        return Math.Max(0m, invoice.TotalAmount - paid);
    }

    private static IReadOnlyList<ExpenseCategorySummaryItem> BuildCategoryBreakdown(
        List<Expense> directExpenses,
        List<PurchaseInvoice> invoices,
        decimal grandTotal)
    {
        var categoryDict = new Dictionary<Guid, (string Name, decimal DirectAmount, decimal InvoicedAmount)>();

        foreach (var expense in directExpenses)
        {
            var catName = expense.ExpenseCategory?.Name ?? "Uncategorized";
            if (!categoryDict.TryGetValue(expense.ExpenseCategoryId, out var current))
            {
                current = (catName, 0m, 0m);
            }

            categoryDict[expense.ExpenseCategoryId] = (current.Name, current.DirectAmount + expense.Amount, current.InvoicedAmount);
        }

        foreach (var invoice in invoices)
        {
            foreach (var line in invoice.Lines)
            {
                if (!line.ExpenseCategoryId.HasValue)
                {
                    continue;
                }

                var catId = line.ExpenseCategoryId.Value;
                var catName = line.ExpenseCategory?.Name ?? "Uncategorized";

                if (!categoryDict.TryGetValue(catId, out var current))
                {
                    current = (catName, 0m, 0m);
                }

                categoryDict[catId] = (current.Name, current.DirectAmount, current.InvoicedAmount + line.LineAmount);
            }
        }

        return categoryDict
            .Select(kvp =>
            {
                var total = kvp.Value.DirectAmount + kvp.Value.InvoicedAmount;
                var pct = grandTotal > 0 ? Math.Round((total / grandTotal) * 100m, 2) : 0m;
                return new ExpenseCategorySummaryItem(
                    kvp.Key,
                    kvp.Value.Name,
                    kvp.Value.DirectAmount,
                    kvp.Value.InvoicedAmount,
                    total,
                    pct);
            })
            .OrderByDescending(item => item.TotalAmount)
            .ToList();
    }

    private static IReadOnlyList<ExpenseFarmSummaryItem> BuildFarmBreakdown(
        List<Expense> directExpenses,
        List<PurchaseInvoice> invoices,
        decimal grandTotal)
    {
        var farmDict = new Dictionary<Guid, (string Name, decimal DirectAmount, decimal InvoicedAmount)>();

        foreach (var expense in directExpenses)
        {
            var farmName = expense.Farm?.Name ?? "Unknown Farm";
            if (!farmDict.TryGetValue(expense.FarmId, out var current))
            {
                current = (farmName, 0m, 0m);
            }

            farmDict[expense.FarmId] = (current.Name, current.DirectAmount + expense.Amount, current.InvoicedAmount);
        }

        foreach (var invoice in invoices)
        {
            var farmName = invoice.Farm?.Name ?? "Unknown Farm";
            if (!farmDict.TryGetValue(invoice.FarmId, out var current))
            {
                current = (farmName, 0m, 0m);
            }

            farmDict[invoice.FarmId] = (current.Name, current.DirectAmount, current.InvoicedAmount + invoice.TotalAmount);
        }

        return farmDict
            .Select(kvp =>
            {
                var total = kvp.Value.DirectAmount + kvp.Value.InvoicedAmount;
                var pct = grandTotal > 0 ? Math.Round((total / grandTotal) * 100m, 2) : 0m;
                return new ExpenseFarmSummaryItem(
                    kvp.Key,
                    kvp.Value.Name,
                    kvp.Value.DirectAmount,
                    kvp.Value.InvoicedAmount,
                    total,
                    pct);
            })
            .OrderByDescending(item => item.TotalAmount)
            .ToList();
    }

    private static IReadOnlyList<ExpenseMonthlyTrendItem> BuildMonthlyTrend(
        List<Expense> directExpenses,
        List<PurchaseInvoice> invoices)
    {
        var monthlyDict = new Dictionary<(int Year, int Month), (decimal DirectAmount, decimal InvoicedAmount)>();

        foreach (var expense in directExpenses)
        {
            var key = (expense.ExpenseDate.Year, expense.ExpenseDate.Month);
            if (!monthlyDict.TryGetValue(key, out var current))
            {
                current = (0m, 0m);
            }

            monthlyDict[key] = (current.DirectAmount + expense.Amount, current.InvoicedAmount);
        }

        foreach (var invoice in invoices)
        {
            var key = (invoice.InvoiceDate.Year, invoice.InvoiceDate.Month);
            if (!monthlyDict.TryGetValue(key, out var current))
            {
                current = (0m, 0m);
            }

            monthlyDict[key] = (current.DirectAmount, current.InvoicedAmount + invoice.TotalAmount);
        }

        return monthlyDict
            .OrderBy(kvp => kvp.Key.Year)
            .ThenBy(kvp => kvp.Key.Month)
            .Select(kvp =>
            {
                var date = new DateTime(kvp.Key.Year, kvp.Key.Month, 1);
                var label = date.ToString("MMM yyyy", CultureInfo.InvariantCulture);
                var total = kvp.Value.DirectAmount + kvp.Value.InvoicedAmount;
                return new ExpenseMonthlyTrendItem(
                    kvp.Key.Year,
                    kvp.Key.Month,
                    label,
                    kvp.Value.DirectAmount,
                    kvp.Value.InvoicedAmount,
                    total);
            })
            .ToList();
    }
}
