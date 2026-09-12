using FarmManagement.Application.Common.Models;
using FarmManagement.Application.Interfaces.Labor;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FarmManagement.Infrastructure.Persistence;

public sealed class LaborWageRateStore(ApplicationDbContext dbContext) : ILaborWageRateStore
{
    public async Task<PagedResponse<LaborWageRate>> ListAsync(
        Guid organizationId,
        int page,
        int pageSize,
        Gender? gender,
        WageType? wageType,
        bool? isActive,
        DateOnly? businessDate,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.LaborWageRates
            .Include(r => r.Currency)
            .Where(r => r.OrganizationId == organizationId);

        if (gender.HasValue)
        {
            query = query.Where(r => r.Gender == gender.Value);
        }

        if (wageType.HasValue)
        {
            query = query.Where(r => r.WageType == wageType.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(r => r.IsActive == isActive.Value);
        }

        if (businessDate.HasValue)
        {
            query = query.Where(r =>
                r.EffectiveFrom <= businessDate.Value &&
                (!r.EffectiveTo.HasValue || r.EffectiveTo.Value >= businessDate.Value));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .AsNoTracking()
            .OrderByDescending(r => r.IsActive)
            .ThenBy(r => r.Gender)
            .ThenBy(r => r.WageType)
            .ThenByDescending(r => r.EffectiveFrom)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<LaborWageRate>(items, totalCount, page, pageSize);
    }

    public async Task<IReadOnlyList<LaborWageRate>> ListAllAsync(
        Guid organizationId,
        Gender? gender,
        WageType? wageType,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.LaborWageRates
            .Include(r => r.Currency)
            .Where(r => r.OrganizationId == organizationId);

        if (gender.HasValue)
        {
            query = query.Where(r => r.Gender == gender.Value);
        }

        if (wageType.HasValue)
        {
            query = query.Where(r => r.WageType == wageType.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(r => r.IsActive == isActive.Value);
        }

        return await query
            .AsNoTracking()
            .OrderByDescending(r => r.IsActive)
            .ThenBy(r => r.Gender)
            .ThenBy(r => r.WageType)
            .ThenByDescending(r => r.EffectiveFrom)
            .ToListAsync(cancellationToken);
    }

    public Task<LaborWageRate?> FindAsync(
        Guid id,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.LaborWageRates
            .Include(r => r.Currency)
            .SingleOrDefaultAsync(r => r.Id == id && r.OrganizationId == organizationId, cancellationToken);

    public async Task<IReadOnlyList<LaborWageRate>> GetActiveRatesForOrganizationAsync(
        Guid organizationId,
        Gender gender,
        WageType wageType,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.LaborWageRates
            .Where(r => r.OrganizationId == organizationId &&
                        r.Gender == gender &&
                        r.WageType == wageType &&
                        r.IsActive);

        if (excludeId.HasValue)
        {
            query = query.Where(r => r.Id != excludeId.Value);
        }

        return await query
            .OrderBy(r => r.EffectiveFrom)
            .ToListAsync(cancellationToken);
    }

    public Task<LaborWageRate?> FindApplicableRateAsync(
        Guid organizationId,
        Gender gender,
        WageType wageType,
        DateOnly businessDate,
        CancellationToken cancellationToken = default) =>
        dbContext.LaborWageRates
            .Include(r => r.Currency)
            .Where(r => r.OrganizationId == organizationId &&
                        r.Gender == gender &&
                        r.WageType == wageType &&
                        r.IsActive &&
                        r.EffectiveFrom <= businessDate &&
                        (!r.EffectiveTo.HasValue || r.EffectiveTo.Value >= businessDate))
            .OrderByDescending(r => r.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<Currency?> FindCurrencyAsync(
        Guid currencyId,
        CancellationToken cancellationToken = default) =>
        dbContext.Currencies
            .SingleOrDefaultAsync(c => c.Id == currencyId, cancellationToken);

    public void Add(LaborWageRate wageRate) =>
        dbContext.LaborWageRates.Add(wageRate);

    public void AddAuditLog(AuditLog auditLog) =>
        dbContext.AuditLogs.Add(auditLog);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
