using System.Data;
using FarmManagement.Application.DTOs.Fertilizer;
using FarmManagement.Application.Interfaces.Fertilizer;
using FarmManagement.Domain.Constants;
using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FarmManagement.Infrastructure.Persistence;

public sealed class FertilizerApplicationStore(ApplicationDbContext dbContext) : IFertilizerApplicationStore
{
    public Task<FertilizerApplication?> FindAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.FertilizerApplications
            .Include(a => a.Farm)
            .Include(a => a.FarmArea)
            .Include(a => a.Plantation)
            .Include(a => a.CropCycle)
            .Include(a => a.CropCycleStage)
            .Include(a => a.PlannedAreaUnit)
            .Include(a => a.ActualAreaUnit)
            .Include(a => a.ApplicationMethod)
            .Include(a => a.IrrigationEvent)
            .Include(a => a.Products)
                .ThenInclude(p => p.InventoryItem)
                    .ThenInclude(i => i!.StockUnit)
            .Include(a => a.Products)
                .ThenInclude(p => p.StorageLocation)
            .Include(a => a.Products)
                .ThenInclude(p => p.StockMovement)
            .SingleOrDefaultAsync(a => a.Id == id && a.OrganizationId == organizationId, cancellationToken);

    public async Task<int> CountAsync(
        Guid organizationId,
        FertilizerApplicationListQuery query,
        CancellationToken cancellationToken = default) =>
        await BuildQuery(organizationId, query).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<FertilizerApplication>> ListAsync(
        Guid organizationId,
        FertilizerApplicationListQuery query,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var baseQuery = BuildQuery(organizationId, query)
            .AsNoTracking()
            .Include(a => a.Farm)
            .Include(a => a.FarmArea)
            .Include(a => a.Plantation)
            .Include(a => a.CropCycle)
            .Include(a => a.CropCycleStage)
            .Include(a => a.PlannedAreaUnit)
            .Include(a => a.ActualAreaUnit)
            .Include(a => a.ApplicationMethod)
            .Include(a => a.Products)
                .ThenInclude(p => p.InventoryItem)
                    .ThenInclude(i => i!.StockUnit);

        var isAscending = string.Equals(query.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);

        var sortedQuery = (query.SortBy?.Trim().ToLowerInvariant()) switch
        {
            "planneddate" => isAscending
                ? baseQuery.OrderBy(a => a.PlannedDate).ThenByDescending(a => a.CreatedAt)
                : baseQuery.OrderByDescending(a => a.PlannedDate).ThenByDescending(a => a.CreatedAt),

            "scheduleddate" => isAscending
                ? baseQuery.OrderBy(a => a.ScheduledDate).ThenByDescending(a => a.CreatedAt)
                : baseQuery.OrderByDescending(a => a.ScheduledDate).ThenByDescending(a => a.CreatedAt),

            "actualapplicationdate" or "actualdate" => isAscending
                ? baseQuery.OrderBy(a => a.ActualApplicationDate).ThenByDescending(a => a.CreatedAt)
                : baseQuery.OrderByDescending(a => a.ActualApplicationDate).ThenByDescending(a => a.CreatedAt),

            "status" => isAscending
                ? baseQuery.OrderBy(a => a.Status).ThenByDescending(a => a.CreatedAt)
                : baseQuery.OrderByDescending(a => a.Status).ThenByDescending(a => a.CreatedAt),

            "referencenumber" => isAscending
                ? baseQuery.OrderBy(a => a.ReferenceNumber).ThenByDescending(a => a.CreatedAt)
                : baseQuery.OrderByDescending(a => a.ReferenceNumber).ThenByDescending(a => a.CreatedAt),

            "farmname" => isAscending
                ? baseQuery.OrderBy(a => a.Farm != null ? a.Farm.Name : string.Empty).ThenByDescending(a => a.CreatedAt)
                : baseQuery.OrderByDescending(a => a.Farm != null ? a.Farm.Name : string.Empty).ThenByDescending(a => a.CreatedAt),

            _ => isAscending
                ? baseQuery.OrderBy(a => a.CreatedAt)
                : baseQuery.OrderByDescending(a => a.CreatedAt)
        };

        return await sortedQuery
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FertilizerApplication>> ListForExportAsync(
        Guid organizationId,
        FertilizerApplicationListQuery query,
        CancellationToken cancellationToken = default)
    {
        var baseQuery = BuildQuery(organizationId, query)
            .AsNoTracking()
            .Include(a => a.Farm)
            .Include(a => a.FarmArea)
            .Include(a => a.Plantation)
            .Include(a => a.CropCycle)
            .Include(a => a.CropCycleStage)
            .Include(a => a.PlannedAreaUnit)
            .Include(a => a.ActualAreaUnit)
            .Include(a => a.ApplicationMethod)
            .Include(a => a.IrrigationEvent)
            .Include(a => a.Products)
                .ThenInclude(p => p.InventoryItem)
                    .ThenInclude(i => i!.StockUnit)
            .Include(a => a.Products)
                .ThenInclude(p => p.StorageLocation)
            .Include(a => a.Products)
                .ThenInclude(p => p.StockMovement);

        var isAscending = string.Equals(query.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);

        var sortedQuery = (query.SortBy?.Trim().ToLowerInvariant()) switch
        {
            "planneddate" => isAscending
                ? baseQuery.OrderBy(a => a.PlannedDate).ThenByDescending(a => a.CreatedAt)
                : baseQuery.OrderByDescending(a => a.PlannedDate).ThenByDescending(a => a.CreatedAt),

            "scheduleddate" => isAscending
                ? baseQuery.OrderBy(a => a.ScheduledDate).ThenByDescending(a => a.CreatedAt)
                : baseQuery.OrderByDescending(a => a.ScheduledDate).ThenByDescending(a => a.CreatedAt),

            "actualapplicationdate" or "actualdate" => isAscending
                ? baseQuery.OrderBy(a => a.ActualApplicationDate).ThenByDescending(a => a.CreatedAt)
                : baseQuery.OrderByDescending(a => a.ActualApplicationDate).ThenByDescending(a => a.CreatedAt),

            "status" => isAscending
                ? baseQuery.OrderBy(a => a.Status).ThenByDescending(a => a.CreatedAt)
                : baseQuery.OrderByDescending(a => a.Status).ThenByDescending(a => a.CreatedAt),

            "referencenumber" => isAscending
                ? baseQuery.OrderBy(a => a.ReferenceNumber).ThenByDescending(a => a.CreatedAt)
                : baseQuery.OrderByDescending(a => a.ReferenceNumber).ThenByDescending(a => a.CreatedAt),

            "farmname" => isAscending
                ? baseQuery.OrderBy(a => a.Farm != null ? a.Farm.Name : string.Empty).ThenByDescending(a => a.CreatedAt)
                : baseQuery.OrderByDescending(a => a.Farm != null ? a.Farm.Name : string.Empty).ThenByDescending(a => a.CreatedAt),

            _ => isAscending
                ? baseQuery.OrderBy(a => a.CreatedAt)
                : baseQuery.OrderByDescending(a => a.CreatedAt)
        };

        return await sortedQuery.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FertilizerProductLookupResponse>> ListProductsLookupAsync(
        Guid organizationId,
        Guid? storageLocationId,
        Guid? farmId,
        CancellationToken cancellationToken = default)
    {
        var baseQuery = dbContext.InventoryItems.AsNoTracking()
            .Include(i => i.StockUnit)
            .Include(i => i.Category)
            .Where(i => i.OrganizationId == organizationId
                     && i.IsActive
                     && (i.CategoryId == FarmInventoryCategories.FertilizersSoilId || (i.Category != null && i.Category.Code == "FERTILIZERS_SOIL"))
                     && !dbContext.PlantProtectionProducts.Any(ppp => ppp.OrganizationId == organizationId && ppp.InventoryItemId == i.Id && ppp.IsActive));

        if (storageLocationId.HasValue && storageLocationId.Value != Guid.Empty)
        {
            var locId = storageLocationId.Value;
            var query = from item in baseQuery
                        join sb in dbContext.StockBalances.AsNoTracking()
                            .Where(b => b.OrganizationId == organizationId && b.StorageLocationId == locId)
                            on item.Id equals sb.InventoryItemId into balances
                        from sb in balances.DefaultIfEmpty()
                        orderby item.Name
                        select new FertilizerProductLookupResponse(
                            item.Id,
                            item.Name,
                            item.Sku,
                            item.StockUnitId,
                            item.StockUnit != null ? item.StockUnit.Name : string.Empty,
                            item.StockUnit != null ? item.StockUnit.Code : null,
                            item.StockUnit != null ? item.StockUnit.Symbol : null,
                            item.CategoryId,
                            item.Category != null ? item.Category.Name : null,
                            sb != null ? sb.QuantityOnHand : 0m,
                            sb != null && sb.QuantityOnHand > 0m);

            return await query.ToListAsync(cancellationToken);
        }

        var items = await baseQuery.OrderBy(i => i.Name).ToListAsync(cancellationToken);
        return items.Select(i => new FertilizerProductLookupResponse(
            i.Id,
            i.Name,
            i.Sku,
            i.StockUnitId,
            i.StockUnit?.Name ?? string.Empty,
            i.StockUnit?.Code,
            i.StockUnit?.Symbol,
            i.CategoryId,
            i.Category?.Name,
            0m,
            false)).ToArray();
    }

    public Task<IReadOnlyList<FertilizerProductLookupResponse>> ListProductsLookupAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
        => ListProductsLookupAsync(organizationId, null, null, cancellationToken);

    public async Task<IReadOnlyList<FertilizerStorageLocationLookupResponse>> ListStorageLocationsLookupAsync(
        Guid organizationId,
        Guid farmId,
        Guid? inventoryItemId,
        CancellationToken cancellationToken = default)
    {
        if (inventoryItemId.HasValue && inventoryItemId.Value != Guid.Empty)
        {
            var itemId = inventoryItemId.Value;
            var locations = await (from sl in dbContext.StorageLocations.AsNoTracking()
                                   join sb in dbContext.StockBalances.AsNoTracking()
                                       .Where(b => b.OrganizationId == organizationId && b.InventoryItemId == itemId)
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
                .FirstOrDefaultAsync(i => i.Id == itemId && i.OrganizationId == organizationId, cancellationToken);

            return locations
                .Select(l => new FertilizerStorageLocationLookupResponse(
                    l.Id,
                    l.Name,
                    l.CurrentStock,
                    l.CurrentStock > 0m,
                    item?.StockUnitId,
                    item?.StockUnit?.Name,
                    item?.StockUnit?.Symbol))
                .OrderByDescending(l => l.HasStock)
                .ThenByDescending(l => l.CurrentStock)
                .ThenBy(l => l.StorageLocationName)
                .ToArray();
        }
        else
        {
            var locations = await dbContext.StorageLocations.AsNoTracking()
                .Where(sl => sl.OrganizationId == organizationId && sl.FarmId == farmId && sl.IsActive)
                .OrderBy(sl => sl.Name)
                .ToListAsync(cancellationToken);

            return locations.Select(l => new FertilizerStorageLocationLookupResponse(
                l.Id,
                l.Name,
                0m,
                false,
                null,
                null,
                null)).ToArray();
        }
    }

    public async Task<IReadOnlyList<FertilizerApplicationMethodLookupResponse>> ListApplicationMethodsLookupAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var methods = await dbContext.ApplicationMethods.AsNoTracking()
            .Where(m => m.Module == "FERTILIZER"
                     && (m.IsSystem || m.OrganizationId == organizationId)
                     && m.IsActive)
            .OrderBy(m => m.DisplayOrder)
            .ThenBy(m => m.Name)
            .ToListAsync(cancellationToken);

        return methods.Select(m => new FertilizerApplicationMethodLookupResponse(
            m.Id,
            m.Code,
            m.Name,
            m.Description,
            m.DisplayOrder,
            string.Equals(m.Code, "OTHER", StringComparison.OrdinalIgnoreCase))).ToArray();
    }

    public async Task<IReadOnlyList<FertilizerIrrigationLookupResponse>> ListIrrigationsLookupAsync(
        Guid organizationId,
        Guid? farmId,
        Guid? farmAreaId,
        CancellationToken cancellationToken = default)
    {
        var q = dbContext.IrrigationEvents.AsNoTracking()
            .Include(e => e.FarmArea)
            .Include(e => e.CropCycle)
            .Include(e => e.IrrigationMethod)
            .Include(e => e.ActualWaterUnit)
            .Where(e => e.OrganizationId == organizationId);

        if (farmId.HasValue && farmId.Value != Guid.Empty)
        {
            q = q.Where(e => e.FarmId == farmId.Value);
        }

        if (farmAreaId.HasValue && farmAreaId.Value != Guid.Empty)
        {
            q = q.Where(e => e.FarmAreaId == farmAreaId.Value);
        }

        var irrigations = await q
            .OrderByDescending(e => e.ActualStartedAt ?? e.ScheduledAt ?? e.PlannedAt ?? e.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        return irrigations.Select(e => new FertilizerIrrigationLookupResponse(
            e.Id,
            e.FarmId,
            e.FarmAreaId,
            e.FarmArea?.Name ?? string.Empty,
            e.CropCycleId,
            e.CropCycle?.CycleName,
            e.Status.ToString(),
            e.PlannedAt,
            e.ScheduledAt,
            e.ActualStartedAt,
            e.ActualEndedAt,
            e.IrrigationMethod?.Name,
            e.ActualWaterQuantity,
            e.ActualWaterUnit?.Symbol)).ToArray();
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetUserNamesAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        var distinctIds = userIds.Distinct().ToList();
        if (distinctIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var users = await dbContext.Users.AsNoTracking()
            .Where(u => distinctIds.Contains(u.Id))
            .Select(u => new { u.Id, FullName = (u.FirstName + " " + u.LastName).Trim() })
            .ToListAsync(cancellationToken);

        return users.ToDictionary(u => u.Id, u => u.FullName);
    }

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

    public Task<ApplicationMethod?> FindApplicationMethodAsync(Guid applicationMethodId, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.ApplicationMethods.SingleOrDefaultAsync(am => am.Id == applicationMethodId && ((am.IsSystem && am.OrganizationId == null) || am.OrganizationId == organizationId), cancellationToken);

    public Task<Unit?> FindUnitAsync(Guid unitId, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.Units.SingleOrDefaultAsync(u => u.Id == unitId && ((u.IsSystem && u.OrganizationId == null) || u.OrganizationId == organizationId), cancellationToken);

    public Task<InventoryItem?> FindInventoryItemAsync(Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.InventoryItems
            .Include(i => i.StockUnit)
            .Include(i => i.Category)
            .SingleOrDefaultAsync(i => i.Id == inventoryItemId && i.OrganizationId == organizationId, cancellationToken);

    public Task<bool> HasActivePlantProtectionProfileAsync(Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.PlantProtectionProducts.AnyAsync(ppp => ppp.InventoryItemId == inventoryItemId && ppp.OrganizationId == organizationId && ppp.IsActive, cancellationToken);

    public Task<StorageLocation?> FindStorageLocationAsync(Guid storageLocationId, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.StorageLocations.SingleOrDefaultAsync(sl => sl.Id == storageLocationId && sl.OrganizationId == organizationId, cancellationToken);

    public Task<IrrigationEvent?> FindIrrigationEventAsync(Guid irrigationEventId, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.IrrigationEvents.SingleOrDefaultAsync(ie => ie.Id == irrigationEventId && ie.OrganizationId == organizationId, cancellationToken);

    public Task<StockBalance?> FindStockBalanceAsync(Guid storageLocationId, Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.StockBalances.SingleOrDefaultAsync(sb => sb.StorageLocationId == storageLocationId && sb.InventoryItemId == inventoryItemId && sb.OrganizationId == organizationId, cancellationToken);

    private IQueryable<FertilizerApplication> BuildQuery(Guid organizationId, FertilizerApplicationListQuery query)
    {
        var q = dbContext.FertilizerApplications.Where(a => a.OrganizationId == organizationId);

        if (query.FarmId.HasValue && query.FarmId.Value != Guid.Empty)
        {
            q = q.Where(a => a.FarmId == query.FarmId.Value);
        }

        if (query.FarmAreaId.HasValue && query.FarmAreaId.Value != Guid.Empty)
        {
            q = q.Where(a => a.FarmAreaId == query.FarmAreaId.Value);
        }

        if (query.PlantationId.HasValue && query.PlantationId.Value != Guid.Empty)
        {
            q = q.Where(a => a.PlantationId == query.PlantationId.Value);
        }

        if (query.CropCycleId.HasValue && query.CropCycleId.Value != Guid.Empty)
        {
            q = q.Where(a => a.CropCycleId == query.CropCycleId.Value);
        }

        if (query.CropCycleStageId.HasValue && query.CropCycleStageId.Value != Guid.Empty)
        {
            q = q.Where(a => a.CropCycleStageId == query.CropCycleStageId.Value);
        }

        if (query.Status.HasValue)
        {
            q = q.Where(a => a.Status == query.Status.Value);
        }

        if (query.InventoryItemId.HasValue && query.InventoryItemId.Value != Guid.Empty)
        {
            q = q.Where(a => a.Products.Any(p => p.InventoryItemId == query.InventoryItemId.Value));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            q = q.Where(a =>
                a.ReferenceNumber.ToLower().Contains(search) ||
                (a.PurposeReason != null && a.PurposeReason.ToLower().Contains(search)) ||
                (a.OtherMethodDescription != null && a.OtherMethodDescription.ToLower().Contains(search)) ||
                (a.Farm != null && a.Farm.Name.ToLower().Contains(search)) ||
                (a.FarmArea != null && a.FarmArea.Name.ToLower().Contains(search)) ||
                a.Products.Any(p => p.InventoryItem != null && p.InventoryItem.Name.ToLower().Contains(search)));
        }

        var hasRange = query.FromDate.HasValue || query.ToDate.HasValue;
        var dateType = query.DateType?.Trim().ToLowerInvariant();

        DateTimeOffset? fromOffset = query.FromDate.HasValue
            ? new DateTimeOffset(query.FromDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc))
            : null;
        DateTimeOffset? toOffset = query.ToDate.HasValue
            ? new DateTimeOffset(query.ToDate.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc))
            : null;

        switch (dateType)
        {
            case "planneddate":
                if (hasRange)
                {
                    q = q.Where(a => a.PlannedDate != null);
                    if (fromOffset.HasValue) q = q.Where(a => a.PlannedDate >= fromOffset.Value);
                    if (toOffset.HasValue) q = q.Where(a => a.PlannedDate <= toOffset.Value);
                }
                break;

            case "scheduleddate":
                if (hasRange)
                {
                    q = q.Where(a => a.ScheduledDate != null);
                    if (fromOffset.HasValue) q = q.Where(a => a.ScheduledDate >= fromOffset.Value);
                    if (toOffset.HasValue) q = q.Where(a => a.ScheduledDate <= toOffset.Value);
                }
                break;

            case "actualapplicationdate" or "actualdate":
                if (hasRange)
                {
                    q = q.Where(a => a.ActualApplicationDate != null);
                    if (fromOffset.HasValue) q = q.Where(a => a.ActualApplicationDate >= fromOffset.Value);
                    if (toOffset.HasValue) q = q.Where(a => a.ActualApplicationDate <= toOffset.Value);
                }
                break;

            case "createddate":
            default:
                if (hasRange)
                {
                    if (fromOffset.HasValue) q = q.Where(a => a.CreatedAt >= fromOffset.Value);
                    if (toOffset.HasValue) q = q.Where(a => a.CreatedAt <= toOffset.Value);
                }
                break;
        }

        return q;
    }

    public void Add(FertilizerApplication application) =>
        dbContext.FertilizerApplications.Add(application);

    public void Remove(FertilizerApplication application) =>
        dbContext.FertilizerApplications.Remove(application);

    public void AddProduct(FertilizerApplicationProduct product) =>
        dbContext.FertilizerApplicationProducts.Add(product);

    public void RemoveProduct(FertilizerApplicationProduct product) =>
        dbContext.FertilizerApplicationProducts.Remove(product);

    public void AddAuditLog(AuditLog auditLog) =>
        dbContext.AuditLogs.Add(auditLog);

    public Task<StockBalance?> LockBalanceAsync(Guid storageLocationId, Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.StockBalances
            .FromSqlInterpolated($"SELECT * FROM stock_balances WHERE storage_location_id = {storageLocationId} AND inventory_item_id = {inventoryItemId} AND organization_id = {organizationId} FOR UPDATE")
            .Include(b => b.Farm)
            .Include(b => b.StorageLocation)
            .Include(b => b.InventoryItem)
                .ThenInclude(i => i!.StockUnit)
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
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public void AddMovement(StockMovement movement) =>
        dbContext.StockMovements.Add(movement);

    public Task<FertilizerOperationIdempotencyRecord?> FindIdempotencyRecordAsync(
        Guid organizationId,
        string operationType,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        dbContext.FertilizerOperationIdempotencyRecords
            .FirstOrDefaultAsync(
                r => r.OrganizationId == organizationId
                  && r.OperationType == operationType
                  && r.IdempotencyKey == idempotencyKey,
                cancellationToken);

    public void AddIdempotencyRecord(FertilizerOperationIdempotencyRecord record) =>
        dbContext.FertilizerOperationIdempotencyRecords.Add(record);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
