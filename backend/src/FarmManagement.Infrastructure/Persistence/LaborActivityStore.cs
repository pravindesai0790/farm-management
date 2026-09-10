using System.Data;
using FarmManagement.Application.Interfaces.LaborActivities;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FarmManagement.Infrastructure.Persistence;

public sealed class LaborActivityStore(ApplicationDbContext dbContext) : ILaborActivityStore
{
    public async Task<(IReadOnlyList<LaborActivity> Items, int TotalCount)> ListPagedAsync(
        Guid organizationId,
        Guid? farmId,
        Guid? farmAreaId,
        Guid? plantationId,
        Guid? cropCycleId,
        Guid? activityTypeId,
        DateOnly? fromDate,
        DateOnly? toDate,
        LaborActivityStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(
            organizationId,
            farmId,
            farmAreaId,
            plantationId,
            cropCycleId,
            activityTypeId,
            fromDate,
            toDate,
            status);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .AsNoTracking()
            .AsSplitQuery()
            .OrderByDescending(activity => activity.ActivityDate)
            .ThenByDescending(activity => activity.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<LaborActivity?> FindAsync(
        Guid activityId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        BuildQuery(organizationId, null, null, null, null, null, null, null, null)
            .AsSplitQuery()
            .SingleOrDefaultAsync(activity => activity.Id == activityId, cancellationToken);

    public Task<LaborActivity?> LockAsync(
        Guid activityId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.LaborActivities
            .FromSqlInterpolated($"SELECT * FROM labor_activities WHERE id = {activityId} AND organization_id = {organizationId} FOR UPDATE")
            .Include(activity => activity.Farm)
            .Include(activity => activity.FarmArea)
            .Include(activity => activity.Plantation)
            .Include(activity => activity.CropCycle)
            .Include(activity => activity.LaborActivityType)
            .SingleOrDefaultAsync(cancellationToken);

    public Task<Farm?> FindFarmAsync(
        Guid farmId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.Farms.SingleOrDefaultAsync(
            farm => farm.Id == farmId && farm.OrganizationId == organizationId,
            cancellationToken);

    public Task<FarmArea?> FindFarmAreaAsync(
        Guid farmAreaId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.FarmAreas.SingleOrDefaultAsync(
            area => area.Id == farmAreaId && area.OrganizationId == organizationId,
            cancellationToken);

    public Task<CropPlantation?> FindPlantationAsync(
        Guid plantationId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.CropPlantations.SingleOrDefaultAsync(
            plantation => plantation.Id == plantationId && plantation.OrganizationId == organizationId,
            cancellationToken);

    public Task<CropCycle?> FindCropCycleAsync(
        Guid cropCycleId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.CropCycles.SingleOrDefaultAsync(
            cycle => cycle.Id == cropCycleId && cycle.OrganizationId == organizationId,
            cancellationToken);

    public Task<LaborActivityType?> FindLaborActivityTypeAsync(
        Guid activityTypeId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.LaborActivityTypes.SingleOrDefaultAsync(
            type => type.Id == activityTypeId &&
                    ((type.IsSystem && type.OrganizationId == null) || type.OrganizationId == organizationId),
            cancellationToken);

    public async Task<IReadOnlyList<LaborActivityType>> ListLaborActivityTypesAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        await dbContext.LaborActivityTypes
            .AsNoTracking()
            .Where(type => type.IsActive && ((type.IsSystem && type.OrganizationId == null) || type.OrganizationId == organizationId))
            .OrderBy(type => type.DisplayOrder)
            .ThenBy(type => type.Name)
            .ToListAsync(cancellationToken);

    public async Task<string> ResolveOrganizationCurrencyAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var setting = await dbContext.SystemSettings
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.Key == "OrganizationCurrency" || s.Key == "DefaultCurrency", cancellationToken);

        return string.IsNullOrWhiteSpace(setting?.Value) ? "INR" : setting.Value.Trim();
    }

    public void Add(LaborActivity activity) => dbContext.LaborActivities.Add(activity);

    public void AddAuditLog(AuditLog auditLog) => dbContext.AuditLogs.Add(auditLog);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        try
        {
            var result = await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private IQueryable<LaborActivity> BuildQuery(
        Guid organizationId,
        Guid? farmId,
        Guid? farmAreaId,
        Guid? plantationId,
        Guid? cropCycleId,
        Guid? activityTypeId,
        DateOnly? fromDate,
        DateOnly? toDate,
        LaborActivityStatus? status)
    {
        var query = dbContext.LaborActivities
            .Include(activity => activity.Farm)
            .Include(activity => activity.FarmArea)
            .Include(activity => activity.Plantation)
            .Include(activity => activity.CropCycle)
            .Include(activity => activity.LaborActivityType)
            .Where(activity => activity.OrganizationId == organizationId);

        if (farmId.HasValue) query = query.Where(a => a.FarmId == farmId.Value);
        if (farmAreaId.HasValue) query = query.Where(a => a.FarmAreaId == farmAreaId.Value);
        if (plantationId.HasValue) query = query.Where(a => a.PlantationId == plantationId.Value);
        if (cropCycleId.HasValue) query = query.Where(a => a.CropCycleId == cropCycleId.Value);
        if (activityTypeId.HasValue) query = query.Where(a => a.LaborActivityTypeId == activityTypeId.Value);
        if (fromDate.HasValue) query = query.Where(a => a.ActivityDate >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(a => a.ActivityDate <= toDate.Value);
        if (status.HasValue) query = query.Where(a => a.Status == status.Value);

        return query;
    }
}
