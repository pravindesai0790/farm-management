using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.Labor;
using FarmManagement.Application.Interfaces.Labor;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.Services;

public sealed class WorkerSettlementService(IWorkerSettlementStore store) : IWorkerSettlementService
{
    public async Task<WorkerSettlementCalculationResponse> CalculateSettlementAsync(
        SettlementActor actor,
        WorkerSettlementQuery query,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ArgumentNullException.ThrowIfNull(query);

        if (query.WorkerId == Guid.Empty)
        {
            throw new ValidationException("A worker is required.");
        }

        var worker = await store.FindWorkerAsync(query.WorkerId, actor.OrganizationId, cancellationToken);
        if (worker is null)
        {
            throw new ResourceNotFoundException("The worker was not found.");
        }

        return await CalculateWorkerInternalAsync(actor, worker, query.PeriodFrom, query.PeriodTo, query.AsOfDate, cancellationToken);
    }

    public async Task<IReadOnlyList<WorkerSettlementCalculationResponse>> CalculateBatchSettlementAsync(
        SettlementActor actor,
        BatchWorkerSettlementQuery query,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ArgumentNullException.ThrowIfNull(query);

        var workers = await store.ListWorkersAsync(actor.OrganizationId, query.WorkerIds, cancellationToken);
        var results = new List<WorkerSettlementCalculationResponse>(workers.Count);

        foreach (var worker in workers)
        {
            var result = await CalculateWorkerInternalAsync(
                actor,
                worker,
                query.PeriodFrom,
                query.PeriodTo,
                query.AsOfDate,
                cancellationToken);

            results.Add(result);
        }

        return results;
    }

    private async Task<WorkerSettlementCalculationResponse> CalculateWorkerInternalAsync(
        SettlementActor actor,
        Worker worker,
        DateOnly? periodFrom,
        DateOnly? periodTo,
        DateOnly? asOfDate,
        CancellationToken cancellationToken)
    {
        // 1. Fetch all earnings for the worker matching the scope (approved, calculated, adjustments)
        var allEarningsInScope = await store.GetEarningsForSettlementAsync(
            actor.OrganizationId,
            worker.Id,
            periodFrom,
            periodTo,
            asOfDate,
            cancellationToken);

        var approvedEarnings = allEarningsInScope
            .Where(e => e.Status == EarningsLedgerStatus.Approved && e.EntryType != EarningsEntryType.Reversal)
            .ToList();

        var pendingApprovalCount = allEarningsInScope
            .Count(e => e.Status == EarningsLedgerStatus.Calculated && e.EntryType != EarningsEntryType.Reversal);

        // 2. Compute gross earnings and adjustments
        var totalEarnings = approvedEarnings
            .Where(e => e.EntryType == EarningsEntryType.Earning)
            .Sum(e => e.GrossAmount);

        var positiveAdjustments = approvedEarnings
            .Where(e => e.EntryType == EarningsEntryType.Adjustment && e.GrossAmount > 0)
            .Sum(e => e.GrossAmount);

        var negativeAdjustments = approvedEarnings
            .Where(e => e.EntryType == EarningsEntryType.Adjustment && e.GrossAmount < 0)
            .Sum(e => Math.Abs(e.GrossAmount));

        var totalApprovedAdjustments = positiveAdjustments - negativeAdjustments;
        var grossPayable = totalEarnings + totalApprovedAdjustments;

        // 3. Fetch completed payments up to asOfDate
        var payments = await store.GetCompletedPaymentsAsync(
            actor.OrganizationId,
            worker.Id,
            asOfDate,
            cancellationToken);

        var totalAdvances = payments
            .Where(p => p.PaymentType == PaymentType.Advance)
            .Sum(p => p.Amount);

        var totalPayouts = payments
            .Where(p => p.PaymentType == PaymentType.Payout)
            .Sum(p => p.Amount);

        var totalPaid = totalAdvances + totalPayouts;

        // 4. Fetch active allocations for the worker up to asOfDate
        var allAllocations = await store.GetAllocationsForWorkerAsync(
            actor.OrganizationId,
            worker.Id,
            asOfDate,
            cancellationToken);

        // All-time advance allocations determine the remaining unallocated advance balance
        var totalAllTimeAdvanceApplied = allAllocations
            .Where(a => a.AllocationType == PaymentAllocationType.AdvanceOffset)
            .Sum(a => a.AllocatedAmount);

        var currentAdvanceBalance = Math.Max(0m, totalAdvances - totalAllTimeAdvanceApplied);

        // Allocations specific to the earnings within this scope
        var periodEarningIds = approvedEarnings.Select(e => e.Id).ToHashSet();
        var periodAllocations = allAllocations
            .Where(a => a.WorkerEarningsLedgerId.HasValue && periodEarningIds.Contains(a.WorkerEarningsLedgerId.Value))
            .ToList();

        var totalAdvanceApplied = periodAllocations
            .Where(a => a.AllocationType == PaymentAllocationType.AdvanceOffset)
            .Sum(a => a.AllocatedAmount);

        var totalPayoutsApplied = periodAllocations
            .Where(a => a.AllocationType == PaymentAllocationType.EarningSettlement)
            .Sum(a => a.AllocatedAmount);

        // 5. Conceptual Formula:
        // Outstanding = Gross Payable - Advance Applied - Payouts Applied
        var outstandingAmount = Math.Max(0m, grossPayable - totalAdvanceApplied - totalPayoutsApplied);

        // 6. Available for payout & Advance carried forward:
        // Current unallocated advance balance offsets what can be paid in cash right now
        var amountAvailableForPayout = Math.Max(0m, outstandingAmount - currentAdvanceBalance);
        var advanceCarriedForward = Math.Max(0m, currentAdvanceBalance - outstandingAmount);

        // 7. Settled / Unsettled earnings count in scope
        var settledEarningCount = 0;
        var unsettledEarningCount = 0;

        foreach (var earning in approvedEarnings)
        {
            var settledAmount = periodAllocations
                .Where(a => a.WorkerEarningsLedgerId == earning.Id)
                .Sum(a => a.AllocatedAmount);

            if (settledAmount >= earning.GrossAmount)
            {
                settledEarningCount++;
            }
            else
            {
                unsettledEarningCount++;
            }
        }

        // 8. Settlement Status derivation
        var settlementStatus = DeriveSettlementStatus(
            grossPayable,
            outstandingAmount,
            totalAdvanceApplied + totalPayoutsApplied,
            currentAdvanceBalance,
            approvedEarnings.Count,
            pendingApprovalCount);

        var currency = approvedEarnings.FirstOrDefault()?.Currency ?? payments.FirstOrDefault()?.Currency;

        return new WorkerSettlementCalculationResponse(
            WorkerId: worker.Id,
            WorkerDisplayName: worker.DisplayName,
            OrganizationId: actor.OrganizationId,
            PeriodFrom: periodFrom,
            PeriodTo: periodTo,
            AsOfDate: asOfDate,
            TotalEarnings: totalEarnings,
            TotalApprovedPositiveAdjustments: positiveAdjustments,
            TotalApprovedNegativeAdjustments: negativeAdjustments,
            TotalApprovedAdjustments: totalApprovedAdjustments,
            GrossPayable: grossPayable,
            TotalAdvances: totalAdvances,
            TotalAdvanceApplied: totalAdvanceApplied,
            CurrentAdvanceBalance: currentAdvanceBalance,
            TotalPayouts: totalPayouts,
            TotalPayoutsApplied: totalPayoutsApplied,
            TotalPaid: totalPaid,
            OutstandingAmount: outstandingAmount,
            AmountAvailableForPayout: amountAvailableForPayout,
            AdvanceCarriedForward: advanceCarriedForward,
            SettlementStatus: FormatSettlementStatus(settlementStatus),
            CurrencyId: currency?.Id,
            CurrencyCode: currency?.Code ?? "INR",
            CurrencySymbol: currency?.Symbol ?? "₹",
            UnsettledEarningCount: unsettledEarningCount,
            SettledEarningCount: settledEarningCount,
            PendingApprovalEarningCount: pendingApprovalCount);
    }

    private static SettlementStatus DeriveSettlementStatus(
        decimal grossPayable,
        decimal outstandingAmount,
        decimal totalApplied,
        decimal currentAdvanceBalance,
        int approvedCount,
        int pendingApprovalCount)
    {
        if (grossPayable == 0)
        {
            if (currentAdvanceBalance > 0)
            {
                return SettlementStatus.CarryForwardOnly;
            }

            if (pendingApprovalCount > 0)
            {
                return SettlementStatus.NotReady;
            }

            return SettlementStatus.Paid;
        }

        if (approvedCount == 0 && pendingApprovalCount > 0)
        {
            return SettlementStatus.NotReady;
        }

        if (outstandingAmount == 0)
        {
            return SettlementStatus.Paid;
        }

        if (totalApplied > 0)
        {
            return SettlementStatus.PartiallyPaid;
        }

        return SettlementStatus.ReadyForPayout;
    }

    private static string FormatSettlementStatus(SettlementStatus status) =>
        status switch
        {
            SettlementStatus.NotReady => "NOT_READY",
            SettlementStatus.ReadyForPayout => "READY_FOR_PAYOUT",
            SettlementStatus.PartiallyPaid => "PARTIALLY_PAID",
            SettlementStatus.Paid => "PAID",
            SettlementStatus.CarryForwardOnly => "CARRY_FORWARD_ONLY",
            _ => status.ToString().ToUpperInvariant()
        };

    private static void ValidateActor(SettlementActor actor)
    {
        ArgumentNullException.ThrowIfNull(actor);
        if (actor.UserId == Guid.Empty)
        {
            throw new ValidationException("An active user is required.");
        }

        if (actor.OrganizationId == Guid.Empty)
        {
            throw new ValidationException("An organization context is required.");
        }
    }
}
