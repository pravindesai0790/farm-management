using System.Data;
using FarmManagement.Application.Interfaces.CropCycles;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FarmManagement.Infrastructure.Persistence;

public sealed class CropCycleStore(ApplicationDbContext dbContext) : ICropCycleStore
{
    public async Task<IReadOnlyList<CropCycle>> ListAsync(
        Guid organizationId,
        Guid? farmId,
        Guid? farmAreaId,
        Guid? plantationId,
        CropCycleStatus? status,
        int? seasonYear,
        CancellationToken cancellationToken = default) =>
        await BuildQuery(organizationId, farmId, farmAreaId, plantationId, status, seasonYear)
            .AsNoTracking()
            .AsSplitQuery()
            .OrderByDescending(cycle => cycle.SeasonYear)
            .ThenByDescending(cycle => cycle.PlannedStartDate)
            .ThenBy(cycle => cycle.CycleName)
            .ThenBy(cycle => cycle.Id)
            .ToListAsync(cancellationToken);

    public Task<CropCycle?> FindAsync(
        Guid cycleId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        BuildQuery(organizationId, null, null, null, null, null)
            .AsSplitQuery()
            .SingleOrDefaultAsync(cycle => cycle.Id == cycleId, cancellationToken);

    public Task<CropCycle?> LockAsync(
        Guid cycleId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.CropCycles
            .FromSqlInterpolated($"SELECT * FROM crop_cycles WHERE id = {cycleId} AND organization_id = {organizationId} FOR UPDATE")
            .Include(cycle => cycle.Plantation)
                .ThenInclude(plantation => plantation!.Crop)
            .Include(cycle => cycle.Plantation)
                .ThenInclude(plantation => plantation!.Farm)
            .Include(cycle => cycle.Plantation)
                .ThenInclude(plantation => plantation!.FarmArea)
            .Include(cycle => cycle.CancellationReason)
            .SingleOrDefaultAsync(cancellationToken);

    public Task<CropPlantation?> LockPlantationAsync(
        Guid plantationId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.CropPlantations
            .FromSqlInterpolated($"SELECT * FROM crop_plantations WHERE id = {plantationId} AND organization_id = {organizationId} FOR UPDATE")
            .Include(plantation => plantation.Crop)
            .Include(plantation => plantation.Farm)
            .Include(plantation => plantation.FarmArea)
            .SingleOrDefaultAsync(cancellationToken);

    public Task<PlantationEndReason?> FindCancellationReasonAsync(
        Guid reasonId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.PlantationEndReasons.SingleOrDefaultAsync(
            reason => reason.Id == reasonId && reason.IsActive &&
                      ((reason.IsSystem && reason.OrganizationId == null) || reason.OrganizationId == organizationId),
            cancellationToken);

    public Task<bool> CodeExistsAsync(
        Guid organizationId,
        string cycleCode,
        Guid? excludingCycleId = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.CropCycles.Where(cycle =>
            cycle.OrganizationId == organizationId && cycle.CycleCode == cycleCode);
        if (excludingCycleId is not null)
        {
            query = query.Where(cycle => cycle.Id != excludingCycleId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> HasActiveCycleAsync(
        Guid plantationId,
        Guid? excludingCycleId = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.CropCycles.Where(cycle =>
            cycle.PlantationId == plantationId && cycle.Status == CropCycleStatus.Active);
        if (excludingCycleId is not null)
        {
            query = query.Where(cycle => cycle.Id != excludingCycleId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public void Add(CropCycle cycle) => dbContext.CropCycles.Add(cycle);

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

    private IQueryable<CropCycle> BuildQuery(
        Guid organizationId,
        Guid? farmId,
        Guid? farmAreaId,
        Guid? plantationId,
        CropCycleStatus? status,
        int? seasonYear)
    {
        var query = dbContext.CropCycles
            .Include(cycle => cycle.Plantation)
                .ThenInclude(plantation => plantation!.Crop)
            .Include(cycle => cycle.Plantation)
                .ThenInclude(plantation => plantation!.Farm)
            .Include(cycle => cycle.Plantation)
                .ThenInclude(plantation => plantation!.FarmArea)
            .Include(cycle => cycle.CancellationReason)
            .Where(cycle => cycle.OrganizationId == organizationId);

        if (farmId is not null) query = query.Where(cycle => cycle.Plantation!.FarmId == farmId.Value);
        if (farmAreaId is not null) query = query.Where(cycle => cycle.Plantation!.FarmAreaId == farmAreaId.Value);
        if (plantationId is not null) query = query.Where(cycle => cycle.PlantationId == plantationId.Value);
        if (status is not null) query = query.Where(cycle => cycle.Status == status.Value);
        if (seasonYear is not null) query = query.Where(cycle => cycle.SeasonYear == seasonYear.Value);
        return query;
    }
}
