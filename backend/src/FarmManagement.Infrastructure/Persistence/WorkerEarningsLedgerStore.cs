using FarmManagement.Application.Common.Models;
using FarmManagement.Application.Interfaces.Labor;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FarmManagement.Infrastructure.Persistence;

public sealed class WorkerEarningsLedgerStore(ApplicationDbContext dbContext) : IWorkerEarningsLedgerStore
{
    public Task<WorkerEarningsLedger?> FindAsync(
        Guid id,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.WorkerEarningsLedgers
            .Include(ledger => ledger.Worker)
            .Include(ledger => ledger.Currency)
            .Include(ledger => ledger.ReferenceLedger)
            .SingleOrDefaultAsync(
                ledger => ledger.Id == id && ledger.OrganizationId == organizationId,
                cancellationToken);

    public Task<WorkerEarningsLedger?> FindByAttendanceIdAsync(
        Guid organizationId,
        Guid attendanceId,
        CancellationToken cancellationToken = default) =>
        dbContext.WorkerEarningsLedgers
            .Include(ledger => ledger.Worker)
            .Include(ledger => ledger.Currency)
            .Include(ledger => ledger.ReferenceLedger)
            .SingleOrDefaultAsync(
                ledger => ledger.AttendanceId == attendanceId &&
                          ledger.OrganizationId == organizationId &&
                          ledger.EntryType == EarningsEntryType.Earning,
                cancellationToken);

    public async Task<PagedResponse<WorkerEarningsLedger>> ListByWorkerAsync(
        Guid organizationId,
        Guid workerId,
        DateOnly? fromDate,
        DateOnly? toDate,
        EarningsLedgerStatus? status,
        EarningsEntryType? entryType,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.WorkerEarningsLedgers
            .Include(ledger => ledger.Worker)
            .Include(ledger => ledger.Currency)
            .Include(ledger => ledger.ReferenceLedger)
            .Where(ledger => ledger.OrganizationId == organizationId && ledger.WorkerId == workerId);

        if (fromDate.HasValue)
        {
            query = query.Where(ledger => ledger.EarningsDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(ledger => ledger.EarningsDate <= toDate.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(ledger => ledger.Status == status.Value);
        }

        if (entryType.HasValue)
        {
            query = query.Where(ledger => ledger.EntryType == entryType.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .AsNoTracking()
            .OrderByDescending(ledger => ledger.EarningsDate)
            .ThenByDescending(ledger => ledger.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<WorkerEarningsLedger>(items, totalCount, page, pageSize);
    }

    public async Task<IReadOnlyList<WorkerEarningsLedger>> ListForBalanceCalculationAsync(
        Guid organizationId,
        Guid workerId,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.WorkerEarningsLedgers
            .Include(ledger => ledger.Currency)
            .Where(ledger => ledger.OrganizationId == organizationId && ledger.WorkerId == workerId);

        if (fromDate.HasValue)
        {
            query = query.Where(ledger => ledger.EarningsDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(ledger => ledger.EarningsDate <= toDate.Value);
        }

        return await query
            .AsNoTracking()
            .OrderBy(ledger => ledger.EarningsDate)
            .ThenBy(ledger => ledger.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> HasActiveReversalAsync(
        Guid originalLedgerId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.WorkerEarningsLedgers
            .AnyAsync(
                ledger => ledger.OrganizationId == organizationId &&
                          ledger.ReferenceLedgerId == originalLedgerId &&
                          ledger.EntryType == EarningsEntryType.Reversal,
                cancellationToken);

    public Task<Worker?> FindWorkerAsync(
        Guid workerId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.Workers
            .SingleOrDefaultAsync(
                worker => worker.Id == workerId && worker.OrganizationId == organizationId,
                cancellationToken);

    public Task<LaborWageRate?> FindApplicableWageRateAsync(
        Guid organizationId,
        Gender gender,
        WageType wageType,
        DateOnly businessDate,
        CancellationToken cancellationToken = default) =>
        dbContext.LaborWageRates
            .Include(rate => rate.Currency)
            .Where(rate =>
                rate.OrganizationId == organizationId &&
                rate.Gender == gender &&
                rate.WageType == wageType &&
                rate.IsActive &&
                businessDate >= rate.EffectiveFrom &&
                (!rate.EffectiveTo.HasValue || businessDate <= rate.EffectiveTo.Value))
            .OrderByDescending(rate => rate.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<Currency?> FindCurrencyAsync(
        Guid currencyId,
        CancellationToken cancellationToken = default) =>
        dbContext.Currencies
            .SingleOrDefaultAsync(c => c.Id == currencyId, cancellationToken);

    public void Add(WorkerEarningsLedger entry) =>
        dbContext.WorkerEarningsLedgers.Add(entry);

    public void AddAuditLog(AuditLog auditLog) =>
        dbContext.AuditLogs.Add(auditLog);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
