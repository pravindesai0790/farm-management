using FarmManagement.Application.DTOs.Sprays;
using FarmManagement.Application.Interfaces.Sprays;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FarmManagement.Infrastructure.Persistence;

public sealed class SprayStore(ApplicationDbContext dbContext) : ISprayStore
{
    public Task<Spray?> FindAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.Sprays
            .Include(s => s.Farm)
            .Include(s => s.FarmArea)
            .Include(s => s.Plantation)
            .Include(s => s.CropCycle)
            .Include(s => s.CropCycleStage)
            .Include(s => s.PlannedAreaUnit)
            .Include(s => s.ActualTreatedAreaUnit)
            .Include(s => s.WaterUnit)
            .Include(s => s.Target)
            .Include(s => s.ApplicationMethod)
            .Include(s => s.Products)
                .ThenInclude(p => p.InventoryItem)
                    .ThenInclude(i => i!.StockUnit)
            .Include(s => s.Products)
                .ThenInclude(p => p.StorageLocation)
            .SingleOrDefaultAsync(s => s.Id == id && s.OrganizationId == organizationId, cancellationToken);

    public async Task<int> CountAsync(
        Guid organizationId,
        SprayListQuery query,
        DateTimeOffset now,
        CancellationToken cancellationToken = default) =>
        await BuildQuery(organizationId, query, now).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<Spray>> ListAsync(
        Guid organizationId,
        SprayListQuery query,
        int skip,
        int take,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var baseQuery = BuildQuery(organizationId, query, now)
            .AsNoTracking()
            .Include(s => s.Farm)
            .Include(s => s.FarmArea)
            .Include(s => s.Plantation)
            .Include(s => s.CropCycle)
            .Include(s => s.CropCycleStage)
            .Include(s => s.Target)
            .Include(s => s.ApplicationMethod)
            .Include(s => s.Products);

        var isAscending = string.Equals(query.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);

        var sortedQuery = (query.SortBy?.Trim().ToLowerInvariant()) switch
        {
            "planneddate" => isAscending
                ? baseQuery.OrderBy(s => s.PlannedDate).ThenByDescending(s => s.CreatedAt)
                : baseQuery.OrderByDescending(s => s.PlannedDate).ThenByDescending(s => s.CreatedAt),

            "scheduleddatetime" => isAscending
                ? baseQuery.OrderBy(s => s.ScheduledDateTime).ThenByDescending(s => s.CreatedAt)
                : baseQuery.OrderByDescending(s => s.ScheduledDateTime).ThenByDescending(s => s.CreatedAt),

            "actualdatetime" or "actualapplicationdatetime" => isAscending
                ? baseQuery.OrderBy(s => s.ActualApplicationDateTime).ThenByDescending(s => s.CreatedAt)
                : baseQuery.OrderByDescending(s => s.ActualApplicationDateTime).ThenByDescending(s => s.CreatedAt),

            "status" => isAscending
                ? baseQuery.OrderBy(s => s.Status).ThenByDescending(s => s.CreatedAt)
                : baseQuery.OrderByDescending(s => s.Status).ThenByDescending(s => s.CreatedAt),

            "farmname" => isAscending
                ? baseQuery.OrderBy(s => s.Farm != null ? s.Farm.Name : string.Empty).ThenByDescending(s => s.CreatedAt)
                : baseQuery.OrderByDescending(s => s.Farm != null ? s.Farm.Name : string.Empty).ThenByDescending(s => s.CreatedAt),

            _ => isAscending
                ? baseQuery.OrderBy(s => s.CreatedAt)
                : baseQuery.OrderByDescending(s => s.CreatedAt)
        };

        return await sortedQuery
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public void Add(Spray spray) => dbContext.Sprays.Add(spray);

    public void AddAuditLog(AuditLog auditLog) => dbContext.AuditLogs.Add(auditLog);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);

    private IQueryable<Spray> BuildQuery(Guid organizationId, SprayListQuery query, DateTimeOffset now)
    {
        var q = dbContext.Sprays.Where(s => s.OrganizationId == organizationId);

        if (query.FarmId.HasValue && query.FarmId.Value != Guid.Empty)
        {
            q = q.Where(s => s.FarmId == query.FarmId.Value);
        }

        if (query.FarmAreaId.HasValue && query.FarmAreaId.Value != Guid.Empty)
        {
            q = q.Where(s => s.FarmAreaId == query.FarmAreaId.Value);
        }

        if (query.PlantationId.HasValue && query.PlantationId.Value != Guid.Empty)
        {
            q = q.Where(s => s.PlantationId == query.PlantationId.Value);
        }

        if (query.CropCycleId.HasValue && query.CropCycleId.Value != Guid.Empty)
        {
            q = q.Where(s => s.CropCycleId == query.CropCycleId.Value);
        }

        if (query.CropCycleStageId.HasValue && query.CropCycleStageId.Value != Guid.Empty)
        {
            q = q.Where(s => s.CropCycleStageId == query.CropCycleStageId.Value);
        }

        if (query.TargetId.HasValue && query.TargetId.Value != Guid.Empty)
        {
            q = q.Where(s => s.TargetId == query.TargetId.Value);
        }

        // Status & Overdue filtering
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var statusStr = query.Status.Trim();
            if (string.Equals(statusStr, "OVERDUE", StringComparison.OrdinalIgnoreCase))
            {
                q = q.Where(s => s.Status == SprayStatus.Scheduled && s.ScheduledDateTime.HasValue && s.ScheduledDateTime.Value < now);
            }
            else if (Enum.TryParse<SprayStatus>(statusStr, true, out var parsedStatus))
            {
                q = q.Where(s => s.Status == parsedStatus);
            }
        }
        else if (query.IncludeOverdue)
        {
            q = q.Where(s => s.Status == SprayStatus.Scheduled && s.ScheduledDateTime.HasValue && s.ScheduledDateTime.Value < now);
        }

        // Date range filtering
        if (query.FromDate.HasValue)
        {
            var from = query.FromDate.Value;
            var fromDt = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            q = q.Where(s =>
                (s.ActualApplicationDateTime.HasValue && s.ActualApplicationDateTime.Value >= fromDt) ||
                (s.ScheduledDateTime.HasValue && s.ScheduledDateTime.Value >= fromDt) ||
                (s.PlannedDate.HasValue && s.PlannedDate.Value >= from));
        }

        if (query.ToDate.HasValue)
        {
            var to = query.ToDate.Value;
            var toDt = to.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            q = q.Where(s =>
                (s.ActualApplicationDateTime.HasValue && s.ActualApplicationDateTime.Value <= toDt) ||
                (s.ScheduledDateTime.HasValue && s.ScheduledDateTime.Value <= toDt) ||
                (s.PlannedDate.HasValue && s.PlannedDate.Value <= to));
        }

        // Search filtering
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim().ToLowerInvariant()}%";
            q = q.Where(s =>
                (s.PurposeReason != null && EF.Functions.ILike(s.PurposeReason, pattern)) ||
                (s.Farm != null && EF.Functions.ILike(s.Farm.Name, pattern)) ||
                (s.FarmArea != null && EF.Functions.ILike(s.FarmArea.Name, pattern)) ||
                (s.Target != null && EF.Functions.ILike(s.Target.Name, pattern)) ||
                s.Products.Any(p => p.InventoryItem != null && EF.Functions.ILike(p.InventoryItem.Name, pattern)));
        }

        return q;
    }
}
