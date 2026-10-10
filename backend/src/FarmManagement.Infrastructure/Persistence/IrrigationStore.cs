using System.Data;
using FarmManagement.Application.DTOs.Irrigation;
using FarmManagement.Application.Interfaces.Irrigation;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FarmManagement.Infrastructure.Persistence;

public sealed class IrrigationStore(ApplicationDbContext dbContext) : IIrrigationStore
{
    public async Task<IrrigationEvent?> FindAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await dbContext.IrrigationEvents
            .Include(e => e.Farm)
            .Include(e => e.FarmArea)
            .Include(e => e.Plantation)
            .Include(e => e.CropCycle)
            .Include(e => e.CropCycleStage)
            .Include(e => e.IrrigationMethod)
            .Include(e => e.PlannedWaterUnit)
            .Include(e => e.ActualWaterUnit)
            .FirstOrDefaultAsync(e => e.Id == id && e.OrganizationId == organizationId, cancellationToken);
    }

    public async Task<int> CountAsync(
        Guid organizationId,
        IrrigationListQuery query,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        return await BuildQuery(organizationId, query, now).CountAsync(cancellationToken);
    }

    public async Task<IrrigationSummaryCountsResponse> GetSummaryCountsAsync(
        Guid organizationId,
        Guid? farmId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var q = dbContext.IrrigationEvents.Where(e => e.OrganizationId == organizationId);
        if (farmId.HasValue && farmId.Value != Guid.Empty)
        {
            q = q.Where(e => e.FarmId == farmId.Value);
        }

        var counts = await q
            .GroupBy(e => 1)
            .Select(g => new
            {
                TotalCount = g.Count(),
                DraftCount = g.Count(e => e.Status == IrrigationStatus.Draft),
                ScheduledCount = g.Count(e => e.Status == IrrigationStatus.Scheduled),
                InProgressCount = g.Count(e => e.Status == IrrigationStatus.InProgress),
                CompletedCount = g.Count(e => e.Status == IrrigationStatus.Completed),
                CancelledCount = g.Count(e => e.Status == IrrigationStatus.Cancelled),
                OverdueCount = g.Count(e => e.Status == IrrigationStatus.Scheduled && e.ScheduledAt.HasValue && e.ScheduledAt.Value < now)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return counts == null
            ? new IrrigationSummaryCountsResponse(0, 0, 0, 0, 0, 0, 0)
            : new IrrigationSummaryCountsResponse(
                counts.TotalCount,
                counts.DraftCount,
                counts.ScheduledCount,
                counts.InProgressCount,
                counts.CompletedCount,
                counts.CancelledCount,
                counts.OverdueCount);
    }

    public async Task<IReadOnlyList<IrrigationEvent>> ListAsync(
        Guid organizationId,
        IrrigationListQuery query,
        int skip,
        int take,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var q = BuildQuery(organizationId, query, now);

        q = (query.SortBy?.ToLowerInvariant(), query.SortDirection?.ToLowerInvariant()) switch
        {
            ("createdat", "asc") => q.OrderBy(e => e.CreatedAt),
            ("scheduledat", "asc") => q.OrderBy(e => e.ScheduledAt),
            ("scheduledat", "desc") => q.OrderByDescending(e => e.ScheduledAt),
            ("status", "asc") => q.OrderBy(e => e.Status),
            ("status", "desc") => q.OrderByDescending(e => e.Status),
            _ => q.OrderByDescending(e => e.CreatedAt)
        };

        return await q
            .Include(e => e.Farm)
            .Include(e => e.FarmArea)
            .Include(e => e.Plantation)
            .Include(e => e.CropCycle)
            .Include(e => e.CropCycleStage)
            .Include(e => e.IrrigationMethod)
            .Include(e => e.PlannedWaterUnit)
            .Include(e => e.ActualWaterUnit)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public void Add(IrrigationEvent irrigation)
    {
        dbContext.IrrigationEvents.Add(irrigation);
    }

    public void AddAuditLog(AuditLog auditLog)
    {
        dbContext.AuditLogs.Add(auditLog);
    }

    public async Task<Farm?> FindFarmAsync(Guid farmId, Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Farms
            .FirstOrDefaultAsync(f => f.Id == farmId && f.OrganizationId == organizationId, cancellationToken);
    }

    public async Task<FarmArea?> FindFarmAreaAsync(Guid farmAreaId, Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await dbContext.FarmAreas
            .FirstOrDefaultAsync(a => a.Id == farmAreaId && a.OrganizationId == organizationId, cancellationToken);
    }

    public async Task<CropPlantation?> FindPlantationAsync(Guid plantationId, Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await dbContext.CropPlantations
            .FirstOrDefaultAsync(p => p.Id == plantationId && p.OrganizationId == organizationId, cancellationToken);
    }

    public async Task<CropCycle?> FindCropCycleAsync(Guid cropCycleId, Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await dbContext.CropCycles
            .FirstOrDefaultAsync(c => c.Id == cropCycleId && c.OrganizationId == organizationId, cancellationToken);
    }

    public async Task<CropCycleStage?> FindCropCycleStageAsync(Guid cropCycleStageId, Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await (from stage in dbContext.CropCycleStages
                      join cycle in dbContext.CropCycles on stage.CropCycleId equals cycle.Id
                      where stage.Id == cropCycleStageId && cycle.OrganizationId == organizationId
                      select stage)
                      .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Unit?> FindUnitAsync(Guid unitId, Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Units
            .FirstOrDefaultAsync(u => u.Id == unitId && (u.OrganizationId == organizationId || (u.IsSystem && u.OrganizationId == null)), cancellationToken);
    }

    public async Task<IrrigationMethod?> FindIrrigationMethodAsync(Guid methodId, Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await dbContext.IrrigationMethods
            .FirstOrDefaultAsync(m => m.Id == methodId && (m.OrganizationId == organizationId || (m.IsSystem && m.OrganizationId == null)), cancellationToken);
    }

    public async Task<IReadOnlyList<IrrigationMethod>> ListMethodsAsync(Guid organizationId, bool activeOnly, CancellationToken cancellationToken = default)
    {
        var q = dbContext.IrrigationMethods
            .Where(m => m.OrganizationId == organizationId || (m.IsSystem && m.OrganizationId == null));

        if (activeOnly)
        {
            q = q.Where(m => m.IsActive);
        }

        return await q
            .OrderBy(m => m.DisplayOrder)
            .ThenBy(m => m.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<Guid, string>> GetUserNamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var users = await dbContext.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, FullName = u.FirstName + " " + u.LastName })
            .ToListAsync(cancellationToken);

        return users.ToDictionary(u => u.Id, u => u.FullName.Trim());
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
    {
        if (dbContext.Database.CurrentTransaction is not null)
        {
            return await operation(cancellationToken);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        try
        {
            var result = await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<IrrigationEvent> BuildQuery(Guid organizationId, IrrigationListQuery query, DateTimeOffset now)
    {
        var q = dbContext.IrrigationEvents.Where(e => e.OrganizationId == organizationId);

        if (query.FarmId.HasValue && query.FarmId.Value != Guid.Empty)
        {
            q = q.Where(e => e.FarmId == query.FarmId.Value);
        }

        if (query.FarmAreaId.HasValue && query.FarmAreaId.Value != Guid.Empty)
        {
            q = q.Where(e => e.FarmAreaId == query.FarmAreaId.Value);
        }

        if (query.PlantationId.HasValue && query.PlantationId.Value != Guid.Empty)
        {
            q = q.Where(e => e.PlantationId == query.PlantationId.Value);
        }

        if (query.CropCycleId.HasValue && query.CropCycleId.Value != Guid.Empty)
        {
            q = q.Where(e => e.CropCycleId == query.CropCycleId.Value);
        }

        if (query.CropCycleStageId.HasValue && query.CropCycleStageId.Value != Guid.Empty)
        {
            q = q.Where(e => e.CropCycleStageId == query.CropCycleStageId.Value);
        }

        if (query.IrrigationMethodId.HasValue && query.IrrigationMethodId.Value != Guid.Empty)
        {
            q = q.Where(e => e.IrrigationMethodId == query.IrrigationMethodId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (Enum.TryParse<IrrigationStatus>(query.Status, true, out var parsedStatus))
            {
                q = q.Where(e => e.Status == parsedStatus);
            }
        }

        if (query.IncludeOverdue)
        {
            q = q.Where(e => e.Status == IrrigationStatus.Scheduled && e.ScheduledAt.HasValue && e.ScheduledAt.Value < now);
        }

        if (query.FromDate.HasValue)
        {
            var fromDto = new DateTimeOffset(query.FromDate.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            q = q.Where(e => (e.ScheduledAt.HasValue && e.ScheduledAt >= fromDto) ||
                             (e.ActualStartedAt.HasValue && e.ActualStartedAt >= fromDto) ||
                             (e.PlannedAt.HasValue && e.PlannedAt >= fromDto));
        }

        if (query.ToDate.HasValue)
        {
            var toDto = new DateTimeOffset(query.ToDate.Value.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);
            q = q.Where(e => (e.ScheduledAt.HasValue && e.ScheduledAt <= toDto) ||
                             (e.ActualStartedAt.HasValue && e.ActualStartedAt <= toDto) ||
                             (e.PlannedAt.HasValue && e.PlannedAt <= toDto));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            q = q.Where(e => (e.Notes != null && e.Notes.ToLower().Contains(search)) ||
                             (e.CancellationReason != null && e.CancellationReason.ToLower().Contains(search)));
        }

        return q;
    }
}
