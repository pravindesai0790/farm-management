using FarmManagement.Application.Interfaces.Labor;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FarmManagement.Infrastructure.Persistence;

public sealed class WorkerSettlementStore(ApplicationDbContext context) : IWorkerSettlementStore
{
    public async Task<Worker?> FindWorkerAsync(
        Guid workerId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        await context.Workers
            .FirstOrDefaultAsync(w => w.Id == workerId && w.OrganizationId == organizationId, cancellationToken);

    public async Task<IReadOnlyList<Worker>> ListWorkersAsync(
        Guid organizationId,
        IReadOnlyList<Guid>? workerIds = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.Workers.Where(w => w.OrganizationId == organizationId);

        if (workerIds is not null && workerIds.Count > 0)
        {
            query = query.Where(w => workerIds.Contains(w.Id));
        }
        else
        {
            query = query.Where(w => w.IsActive);
        }

        return await query
            .OrderBy(w => w.FirstName)
            .ThenBy(w => w.LastName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkerEarningsLedger>> GetEarningsForSettlementAsync(
        Guid organizationId,
        Guid workerId,
        DateOnly? periodFrom = null,
        DateOnly? periodTo = null,
        DateOnly? asOfDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.WorkerEarningsLedgers
            .Include(e => e.Currency)
            .Where(e => e.OrganizationId == organizationId && e.WorkerId == workerId);

        if (periodFrom.HasValue)
        {
            query = query.Where(e => e.EarningsDate >= periodFrom.Value);
        }

        DateOnly? effectiveCutoff = null;
        if (periodTo.HasValue && asOfDate.HasValue)
        {
            effectiveCutoff = periodTo.Value < asOfDate.Value ? periodTo.Value : asOfDate.Value;
        }
        else if (periodTo.HasValue)
        {
            effectiveCutoff = periodTo.Value;
        }
        else if (asOfDate.HasValue)
        {
            effectiveCutoff = asOfDate.Value;
        }

        if (effectiveCutoff.HasValue)
        {
            query = query.Where(e => e.EarningsDate <= effectiveCutoff.Value);
        }

        return await query
            .OrderBy(e => e.EarningsDate)
            .ThenBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkerPayment>> GetCompletedPaymentsAsync(
        Guid organizationId,
        Guid workerId,
        DateOnly? asOfDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.WorkerPayments
            .Include(p => p.Currency)
            .Where(p => p.OrganizationId == organizationId &&
                        p.WorkerId == workerId &&
                        p.Status == PaymentStatus.Completed);

        if (asOfDate.HasValue)
        {
            query = query.Where(p => p.PaymentDate <= asOfDate.Value);
        }

        return await query
            .OrderBy(p => p.PaymentDate)
            .ThenBy(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkerPaymentAllocation>> GetAllocationsForWorkerAsync(
        Guid organizationId,
        Guid workerId,
        DateOnly? asOfDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.WorkerPaymentAllocations
            .Include(a => a.WorkerPayment)
            .Where(a => a.OrganizationId == organizationId &&
                        a.WorkerPayment != null &&
                        a.WorkerPayment.WorkerId == workerId &&
                        a.WorkerPayment.Status != PaymentStatus.Cancelled);

        if (asOfDate.HasValue)
        {
            query = query.Where(a => a.AllocationDate <= asOfDate.Value);
        }

        return await query
            .OrderBy(a => a.AllocationDate)
            .ThenBy(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
