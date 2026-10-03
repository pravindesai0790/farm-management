using FarmManagement.Application.DTOs.Expenses;
using FarmManagement.Application.Interfaces.Expenses;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.Services;

public sealed class SupplierBalanceService(ISupplierBalanceStore balanceStore) : ISupplierBalanceService
{
    public async Task<SupplierBalanceSummaryResponse> GetSupplierBalancesAsync(
        Guid organizationId,
        SupplierBalanceFilter filter,
        CancellationToken cancellationToken = default)
    {
        var asOfDate = filter.AsOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var invoices = await balanceStore.GetOpenOrUnpaidInvoicesAsync(organizationId, filter, cancellationToken);
        var suppliers = await balanceStore.GetActiveSuppliersAsync(organizationId, cancellationToken);
        var currencies = await balanceStore.GetActiveCurrenciesAsync(cancellationToken);

        var currencyMap = currencies.ToDictionary(c => c.Id);
        var supplierMap = suppliers.ToDictionary(s => s.Id);

        // Only include posted invoices with an outstanding balance > 0
        var openInvoices = invoices.Where(pi => GetOutstandingBalance(pi) > 0).ToList();

        // Distinct currency IDs from open invoices
        var currencyIds = openInvoices.Select(pi => pi.CurrencyId).Distinct();
        if (filter.CurrencyId.HasValue)
        {
            currencyIds = currencyIds.Where(id => id == filter.CurrencyId.Value);
        }

        var currencySummaries = new List<SupplierBalanceCurrencySummary>();

        foreach (var currencyId in currencyIds)
        {
            if (!currencyMap.TryGetValue(currencyId, out var currency))
            {
                continue;
            }

            var currencyInvoices = openInvoices.Where(pi => pi.CurrencyId == currencyId).ToList();

            // Group invoices by SupplierId
            var supplierInvoicesGroup = currencyInvoices.GroupBy(pi => pi.SupplierId);

            var supplierBalanceItems = new List<SupplierBalanceItem>();
            var currencyCurrentNotDue = 0m;
            var currencyDays1To30 = 0m;
            var currencyDays31To60 = 0m;
            var currencyDays61To90 = 0m;
            var currencyDaysOver90 = 0m;

            foreach (var group in supplierInvoicesGroup)
            {
                var supplierId = group.Key;
                var supplierInvoices = group.ToList();

                if (!supplierMap.TryGetValue(supplierId, out var supplier))
                {
                    continue;
                }

                var openInvoiceDtos = new List<SupplierOpenInvoiceItem>();
                var supCurrentNotDue = 0m;
                var supDays1To30 = 0m;
                var supDays31To60 = 0m;
                var supDays61To90 = 0m;
                var supDaysOver90 = 0m;

                foreach (var invoice in supplierInvoices.OrderBy(i => i.DueDate ?? i.InvoiceDate))
                {
                    var outstanding = GetOutstandingBalance(invoice);
                    var paid = GetAmountPaid(invoice);
                    var dueDate = invoice.DueDate ?? invoice.InvoiceDate;

                    var daysOverdue = asOfDate.DayNumber - dueDate.DayNumber;
                    var daysOverdueCount = daysOverdue > 0 ? daysOverdue : 0;
                    var dueStatus = daysOverdue > 0 ? "OVERDUE" : (dueDate == asOfDate ? "DUE_TODAY" : "NOT_DUE");

                    var paymentStatusStr = invoice.Status switch
                    {
                        PurchaseInvoiceStatus.Draft => "DRAFT",
                        PurchaseInvoiceStatus.Reversed => "REVERSED",
                        _ => outstanding == 0m ? "PAID" : paid > 0m ? "PARTIALLY_PAID" : "UNPAID"
                    };

                    if (daysOverdue <= 0)
                    {
                        supCurrentNotDue += outstanding;
                    }
                    else if (daysOverdue <= 30)
                    {
                        supDays1To30 += outstanding;
                    }
                    else if (daysOverdue <= 60)
                    {
                        supDays31To60 += outstanding;
                    }
                    else if (daysOverdue <= 90)
                    {
                        supDays61To90 += outstanding;
                    }
                    else
                    {
                        supDaysOver90 += outstanding;
                    }

                    openInvoiceDtos.Add(new SupplierOpenInvoiceItem(
                        invoice.Id,
                        invoice.SupplierInvoiceNumber,
                        invoice.FarmId,
                        invoice.Farm?.Name,
                        invoice.InvoiceDate,
                        dueDate,
                        daysOverdueCount,
                        invoice.TotalAmount,
                        paid,
                        outstanding,
                        paymentStatusStr,
                        dueStatus));
                }

                var supplierTotalOutstanding = supCurrentNotDue + supDays1To30 + supDays31To60 + supDays61To90 + supDaysOver90;
                var supplierOverdueBalance = supDays1To30 + supDays31To60 + supDays61To90 + supDaysOver90;
                var supplierAging = new SupplierAgingTotals(
                    supCurrentNotDue,
                    supDays1To30,
                    supDays31To60,
                    supDays61To90,
                    supDaysOver90);

                supplierBalanceItems.Add(new SupplierBalanceItem(
                    supplier.Id,
                    supplier.Name,
                    supplier.ContactPerson,
                    supplier.Phone,
                    supplier.Email,
                    supplierTotalOutstanding,
                    supplierOverdueBalance,
                    supplierAging,
                    openInvoiceDtos));

                currencyCurrentNotDue += supCurrentNotDue;
                currencyDays1To30 += supDays1To30;
                currencyDays31To60 += supDays31To60;
                currencyDays61To90 += supDays61To90;
                currencyDaysOver90 += supDaysOver90;
            }

            var totalOutstanding = currencyCurrentNotDue + currencyDays1To30 + currencyDays31To60 + currencyDays61To90 + currencyDaysOver90;
            var totalOverdue = currencyDays1To30 + currencyDays31To60 + currencyDays61To90 + currencyDaysOver90;
            var currencyAgingTotals = new SupplierAgingTotals(
                currencyCurrentNotDue,
                currencyDays1To30,
                currencyDays31To60,
                currencyDays61To90,
                currencyDaysOver90);

            currencySummaries.Add(new SupplierBalanceCurrencySummary(
                currency.Id,
                currency.Code,
                currency.Symbol,
                totalOutstanding,
                totalOverdue,
                currencyCurrentNotDue,
                supplierBalanceItems.Count,
                currencyInvoices.Count,
                currencyAgingTotals,
                supplierBalanceItems.OrderByDescending(s => s.TotalOutstandingBalance).ToList()));
        }

        return new SupplierBalanceSummaryResponse(asOfDate, currencySummaries);
    }

    public async Task<IReadOnlyList<OverdueInvoiceItem>> GetOverdueInvoicesAsync(
        Guid organizationId,
        SupplierBalanceFilter filter,
        CancellationToken cancellationToken = default)
    {
        var balanceSummary = await GetSupplierBalancesAsync(organizationId, filter, cancellationToken);
        var overdueList = new List<OverdueInvoiceItem>();

        foreach (var currSummary in balanceSummary.CurrencySummaries)
        {
            foreach (var supItem in currSummary.SupplierBalances)
            {
                foreach (var inv in supItem.OpenInvoices.Where(i => i.DaysOverdue > 0))
                {
                    overdueList.Add(new OverdueInvoiceItem(
                        inv.InvoiceId,
                        inv.SupplierInvoiceNumber,
                        supItem.SupplierId,
                        supItem.SupplierName,
                        inv.FarmId,
                        inv.FarmName,
                        inv.InvoiceDate,
                        inv.DueDate,
                        inv.DaysOverdue,
                        inv.TotalAmount,
                        inv.AmountPaid,
                        inv.OutstandingAmount,
                        currSummary.CurrencyId,
                        currSummary.CurrencyCode,
                        currSummary.CurrencySymbol));
                }
            }
        }

        return overdueList.OrderByDescending(o => o.DaysOverdue).ToList();
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
}
