using FarmManagement.Application.Common.Models;
using FarmManagement.Application.Interfaces.Labor;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FarmManagement.Infrastructure.Persistence;

public sealed class WorkerPaymentAllocationStore(ApplicationDbContext context) : IWorkerPaymentAllocationStore
{
    public async Task<WorkerPaymentAllocation?> FindAsync(
        Guid id,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        await context.WorkerPaymentAllocations
            .Include(a => a.WorkerPayment)
            .Include(a => a.WorkerEarningsLedger)
            .FirstOrDefaultAsync(a => a.Id == id && a.OrganizationId == organizationId, cancellationToken);

    public async Task<WorkerPayment?> FindPaymentAsync(
        Guid paymentId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        await context.WorkerPayments
            .Include(p => p.Worker)
            .Include(p => p.Currency)
            .FirstOrDefaultAsync(p => p.Id == paymentId && p.OrganizationId == organizationId, cancellationToken);

    public async Task<WorkerEarningsLedger?> FindEarningAsync(
        Guid earningId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        await context.WorkerEarningsLedgers
            .Include(e => e.Worker)
            .Include(e => e.Currency)
            .FirstOrDefaultAsync(e => e.Id == earningId && e.OrganizationId == organizationId, cancellationToken);

    public async Task<IReadOnlyList<WorkerPaymentAllocation>> ListByPaymentAsync(
        Guid paymentId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        await context.WorkerPaymentAllocations
            .Include(a => a.WorkerPayment)
            .Include(a => a.WorkerEarningsLedger)
            .Where(a => a.WorkerPaymentId == paymentId && a.OrganizationId == organizationId)
            .OrderByDescending(a => a.AllocationDate)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<WorkerPaymentAllocation>> ListByEarningAsync(
        Guid earningId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        await context.WorkerPaymentAllocations
            .Include(a => a.WorkerPayment)
            .Include(a => a.WorkerEarningsLedger)
            .Where(a => a.WorkerEarningsLedgerId == earningId && a.OrganizationId == organizationId)
            .OrderByDescending(a => a.AllocationDate)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<WorkerPaymentAllocation>> ListByWorkerAsync(
        Guid workerId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        await context.WorkerPaymentAllocations
            .Include(a => a.WorkerPayment)
            .Include(a => a.WorkerEarningsLedger)
            .Where(a => a.OrganizationId == organizationId && a.WorkerPayment != null && a.WorkerPayment.WorkerId == workerId)
            .OrderByDescending(a => a.AllocationDate)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<decimal> GetTotalAllocatedForPaymentAsync(
        Guid paymentId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var total = await context.WorkerPaymentAllocations
            .Where(a => a.WorkerPaymentId == paymentId && a.OrganizationId == organizationId)
            .SumAsync(a => (decimal?)a.AllocatedAmount, cancellationToken);

        return total ?? 0m;
    }

    public async Task<decimal> GetTotalSettledForEarningAsync(
        Guid earningId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var total = await context.WorkerPaymentAllocations
            .Where(a => a.WorkerEarningsLedgerId == earningId &&
                        a.OrganizationId == organizationId &&
                        a.WorkerPayment != null &&
                        a.WorkerPayment.Status != PaymentStatus.Cancelled)
            .SumAsync(a => (decimal?)a.AllocatedAmount, cancellationToken);

        return total ?? 0m;
    }

    public async Task<IReadOnlyDictionary<Guid, decimal>> GetSettledTotalsForEarningsBatchAsync(
        IEnumerable<Guid> earningIds,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var ids = earningIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, decimal>();
        }

        var results = await context.WorkerPaymentAllocations
            .Where(a => a.OrganizationId == organizationId &&
                        a.WorkerEarningsLedgerId.HasValue &&
                        ids.Contains(a.WorkerEarningsLedgerId.Value) &&
                        a.WorkerPayment != null &&
                        a.WorkerPayment.Status != PaymentStatus.Cancelled)
            .GroupBy(a => a.WorkerEarningsLedgerId!.Value)
            .Select(g => new { EarningId = g.Key, Settled = g.Sum(x => x.AllocatedAmount) })
            .ToDictionaryAsync(x => x.EarningId, x => x.Settled, cancellationToken);

        return results;
    }

    public async Task<IReadOnlyList<WorkerEarningsLedger>> ListUnsettledEarningsForWorkerAsync(
        Guid organizationId,
        Guid workerId,
        DateOnly? upToDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.WorkerEarningsLedgers
            .Where(e => e.OrganizationId == organizationId &&
                        e.WorkerId == workerId &&
                        e.Status == EarningsLedgerStatus.Approved &&
                        e.EntryType != EarningsEntryType.Reversal);

        if (upToDate.HasValue)
        {
            query = query.Where(e => e.EarningsDate <= upToDate.Value);
        }

        return await query
            .OrderBy(e => e.EarningsDate)
            .ThenBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public void Add(WorkerPaymentAllocation allocation) =>
        context.WorkerPaymentAllocations.Add(allocation);

    public void AddRange(IEnumerable<WorkerPaymentAllocation> allocations) =>
        context.WorkerPaymentAllocations.AddRange(allocations);

    public void Remove(WorkerPaymentAllocation allocation) =>
        context.WorkerPaymentAllocations.Remove(allocation);

    public void RemoveRange(IEnumerable<WorkerPaymentAllocation> allocations) =>
        context.WorkerPaymentAllocations.RemoveRange(allocations);

    public void AddAuditLog(AuditLog auditLog) =>
        context.AuditLogs.Add(auditLog);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
