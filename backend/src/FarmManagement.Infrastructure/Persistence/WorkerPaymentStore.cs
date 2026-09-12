using FarmManagement.Application.Common.Models;
using FarmManagement.Application.Interfaces.Labor;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FarmManagement.Infrastructure.Persistence;

public sealed class WorkerPaymentStore(ApplicationDbContext dbContext) : IWorkerPaymentStore
{
    public Task<WorkerPayment?> FindAsync(
        Guid id,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.WorkerPayments
            .Include(payment => payment.Worker)
            .Include(payment => payment.Currency)
            .SingleOrDefaultAsync(
                payment => payment.Id == id && payment.OrganizationId == organizationId,
                cancellationToken);

    public async Task<PagedResponse<WorkerPayment>> ListByWorkerAsync(
        Guid organizationId,
        Guid workerId,
        DateOnly? fromDate,
        DateOnly? toDate,
        PaymentType? paymentType,
        PaymentStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.WorkerPayments
            .Include(payment => payment.Worker)
            .Include(payment => payment.Currency)
            .Where(payment => payment.OrganizationId == organizationId && payment.WorkerId == workerId);

        if (fromDate.HasValue)
        {
            query = query.Where(payment => payment.PaymentDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(payment => payment.PaymentDate <= toDate.Value);
        }

        if (paymentType.HasValue)
        {
            query = query.Where(payment => payment.PaymentType == paymentType.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(payment => payment.Status == status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .AsNoTracking()
            .OrderByDescending(payment => payment.PaymentDate)
            .ThenByDescending(payment => payment.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<WorkerPayment>(items, totalCount, page, pageSize);
    }

    public async Task<IReadOnlyList<WorkerPayment>> ListPaymentsForFinancialSummaryAsync(
        Guid organizationId,
        Guid workerId,
        DateOnly? asOfDate,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.WorkerPayments
            .Include(payment => payment.Currency)
            .Where(payment => payment.OrganizationId == organizationId && payment.WorkerId == workerId);

        if (asOfDate.HasValue)
        {
            query = query.Where(payment => payment.PaymentDate <= asOfDate.Value);
        }

        return await query
            .AsNoTracking()
            .OrderBy(payment => payment.PaymentDate)
            .ThenBy(payment => payment.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<Worker?> FindWorkerAsync(
        Guid workerId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.Workers
            .SingleOrDefaultAsync(
                worker => worker.Id == workerId && worker.OrganizationId == organizationId,
                cancellationToken);

    public Task<Currency?> FindCurrencyAsync(
        Guid currencyId,
        CancellationToken cancellationToken = default) =>
        dbContext.Currencies
            .SingleOrDefaultAsync(c => c.Id == currencyId, cancellationToken);

    public void Add(WorkerPayment payment) =>
        dbContext.WorkerPayments.Add(payment);

    public void AddAuditLog(AuditLog auditLog) =>
        dbContext.AuditLogs.Add(auditLog);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
