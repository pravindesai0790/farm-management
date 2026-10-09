using System.Data;
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

    public Task<Farm?> FindFarmAsync(Guid farmId, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.Farms.SingleOrDefaultAsync(f => f.Id == farmId && f.OrganizationId == organizationId, cancellationToken);

    public Task<FarmArea?> FindFarmAreaAsync(Guid farmAreaId, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.FarmAreas.SingleOrDefaultAsync(fa => fa.Id == farmAreaId && fa.OrganizationId == organizationId, cancellationToken);

    public Task<CropPlantation?> FindPlantationAsync(Guid plantationId, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.CropPlantations.SingleOrDefaultAsync(p => p.Id == plantationId && p.OrganizationId == organizationId, cancellationToken);

    public Task<CropCycle?> FindCropCycleAsync(Guid cropCycleId, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.CropCycles.SingleOrDefaultAsync(c => c.Id == cropCycleId && c.OrganizationId == organizationId, cancellationToken);

    public Task<CropCycleStage?> FindCropCycleStageAsync(Guid cropCycleStageId, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.CropCycleStages
            .Include(s => s.CropCycle)
            .SingleOrDefaultAsync(s => s.Id == cropCycleStageId && s.CropCycle != null && s.CropCycle.OrganizationId == organizationId, cancellationToken);

    public Task<Target?> FindTargetAsync(Guid targetId, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.Targets.SingleOrDefaultAsync(t => t.Id == targetId && ((t.IsSystem && t.OrganizationId == null) || t.OrganizationId == organizationId), cancellationToken);

    public Task<ApplicationMethod?> FindApplicationMethodAsync(Guid applicationMethodId, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.ApplicationMethods.SingleOrDefaultAsync(am => am.Id == applicationMethodId && ((am.IsSystem && am.OrganizationId == null) || am.OrganizationId == organizationId), cancellationToken);

    public Task<Unit?> FindUnitAsync(Guid unitId, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.Units.SingleOrDefaultAsync(u => u.Id == unitId && ((u.IsSystem && u.OrganizationId == null) || u.OrganizationId == organizationId), cancellationToken);

    public Task<InventoryItem?> FindInventoryItemAsync(Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.InventoryItems.SingleOrDefaultAsync(i => i.Id == inventoryItemId && i.OrganizationId == organizationId, cancellationToken);

    public Task<bool> HasActivePlantProtectionProfileAsync(Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.PlantProtectionProducts.AnyAsync(ppp => ppp.InventoryItemId == inventoryItemId && ppp.OrganizationId == organizationId && ppp.IsActive, cancellationToken);

    public Task<StorageLocation?> FindStorageLocationAsync(Guid storageLocationId, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.StorageLocations.SingleOrDefaultAsync(sl => sl.Id == storageLocationId && sl.OrganizationId == organizationId, cancellationToken);

    public Task<StockBalance?> FindStockBalanceAsync(Guid storageLocationId, Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.StockBalances.SingleOrDefaultAsync(sb => sb.StorageLocationId == storageLocationId && sb.InventoryItemId == inventoryItemId && sb.OrganizationId == organizationId, cancellationToken);

    public void RemoveSprayProduct(SprayProduct product) => dbContext.SprayProducts.Remove(product);

    public void AddMovement(StockMovement movement) => dbContext.StockMovements.Add(movement);

    public Task<StockBalance?> LockBalanceAsync(Guid storageLocationId, Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.StockBalances
            .FromSqlInterpolated($"SELECT * FROM stock_balances WHERE storage_location_id = {storageLocationId} AND inventory_item_id = {inventoryItemId} AND organization_id = {organizationId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

    public async Task AcquireAdvisoryLockAsync(Guid storageLocationId, Guid inventoryItemId, CancellationToken cancellationToken = default)
    {
        if (dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
        {
            var locStr = storageLocationId.ToString();
            var itemStr = inventoryItemId.ToString();
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtext({locStr}), hashtext({itemStr}))",
                cancellationToken);
        }
    }

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
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
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);

    public async Task<IReadOnlyList<SprayProductLookupResponse>> ListProductsLookupAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var products = await dbContext.PlantProtectionProducts.AsNoTracking()
            .Include(p => p.InventoryItem)
                .ThenInclude(i => i!.StockUnit)
            .Include(p => p.ProductType)
            .Where(p => p.OrganizationId == organizationId
                     && p.IsActive
                     && p.InventoryItem != null
                     && p.InventoryItem.OrganizationId == organizationId
                     && p.InventoryItem.IsActive)
            .ToListAsync(cancellationToken);

        return products
            .OrderBy(p => p.InventoryItem!.Name)
            .Select(p => new SprayProductLookupResponse(
                p.InventoryItemId,
                p.InventoryItem!.Name,
                p.InventoryItem.Sku,
                p.InventoryItem.StockUnitId,
                p.InventoryItem.StockUnit?.Name ?? string.Empty,
                p.InventoryItem.StockUnit?.Code,
                p.InventoryItem.StockUnit?.Symbol,
                p.ProductTypeId,
                p.ProductType?.Code ?? string.Empty,
                p.ProductType?.Name ?? string.Empty,
                p.ActiveIngredient,
                p.Manufacturer,
                p.Id))
            .ToArray();
    }

    public async Task<IReadOnlyList<SprayStorageLocationLookupResponse>> ListStorageLocationsLookupAsync(
        Guid organizationId,
        Guid farmId,
        Guid inventoryItemId,
        CancellationToken cancellationToken = default)
    {
        var locations = await (from sl in dbContext.StorageLocations.AsNoTracking()
                               join sb in dbContext.StockBalances.AsNoTracking()
                                   .Where(b => b.OrganizationId == organizationId && b.InventoryItemId == inventoryItemId)
                                   on sl.Id equals sb.StorageLocationId into balances
                               from sb in balances.DefaultIfEmpty()
                               where sl.OrganizationId == organizationId
                                  && sl.FarmId == farmId
                                  && sl.IsActive
                               select new
                               {
                                   sl.Id,
                                   sl.Name,
                                   CurrentStock = sb != null ? sb.QuantityOnHand : 0m
                               }).ToListAsync(cancellationToken);


        var item = await dbContext.InventoryItems.AsNoTracking()
            .Include(i => i.StockUnit)
            .FirstOrDefaultAsync(i => i.Id == inventoryItemId && i.OrganizationId == organizationId, cancellationToken);

        return locations
            .Select(l => new SprayStorageLocationLookupResponse(
                l.Id,
                l.Name,
                l.CurrentStock,
                l.CurrentStock > 0m,
                item?.StockUnitId,
                item?.StockUnit?.Name))
            .OrderByDescending(l => l.HasStock)
            .ThenByDescending(l => l.CurrentStock)
            .ThenBy(l => l.StorageLocationName)
            .ToArray();
    }

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
