using System.Data;
using FarmManagement.Application.Interfaces.Inventory;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FarmManagement.Infrastructure.Persistence;

public sealed class InventoryStockStore(ApplicationDbContext dbContext) : IInventoryStockStore
{
    public Task<InventoryItem?> FindItemAsync(Guid itemId, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.InventoryItems
            .Include(item => item.StockUnit)
            .SingleOrDefaultAsync(item => item.Id == itemId && item.OrganizationId == organizationId, cancellationToken);

    public Task<StorageLocation?> FindLocationAsync(Guid locationId, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.StorageLocations
            .Include(location => location.Farm)
            .SingleOrDefaultAsync(location => location.Id == locationId && location.OrganizationId == organizationId, cancellationToken);

    public Task<Farm?> FindFarmAsync(Guid farmId, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.Farms.SingleOrDefaultAsync(farm => farm.Id == farmId && farm.OrganizationId == organizationId, cancellationToken);

    public Task<CropCycle?> FindCropCycleAsync(Guid cycleId, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.CropCycles
            .Include(c => c.Plantation)
            .SingleOrDefaultAsync(c => c.Id == cycleId && c.OrganizationId == organizationId, cancellationToken);

    public Task<CropCycleStage?> FindCropCycleStageAsync(Guid stageId, CancellationToken cancellationToken = default) =>
        dbContext.CropCycleStages.SingleOrDefaultAsync(s => s.Id == stageId, cancellationToken);

    public Task<CropPlantation?> FindPlantationAsync(Guid plantationId, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.CropPlantations.SingleOrDefaultAsync(p => p.Id == plantationId && p.OrganizationId == organizationId, cancellationToken);

    public Task<FarmArea?> FindFarmAreaAsync(Guid areaId, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.FarmAreas.SingleOrDefaultAsync(a => a.Id == areaId && a.OrganizationId == organizationId, cancellationToken);

    public Task<LaborActivity?> FindLaborActivityAsync(Guid activityId, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.LaborActivities
            .Include(a => a.LaborActivityType)
            .SingleOrDefaultAsync(a => a.Id == activityId && a.OrganizationId == organizationId, cancellationToken);

    public Task<StockMovement?> FindMovementAsync(Guid movementId, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.StockMovements
            .Include(m => m.InventoryItem)
                .ThenInclude(i => i!.StockUnit)
            .Include(m => m.StorageLocation)
                .ThenInclude(l => l!.Farm)
            .Include(m => m.Farm)
            .Include(m => m.CropCycle)
            .Include(m => m.CropCycleStage)
            .Include(m => m.Plantation)
            .Include(m => m.FarmArea)
            .Include(m => m.LaborActivity)
                .ThenInclude(a => a!.LaborActivityType)
            .Include(m => m.ReversalMovement)
            .Include(m => m.ReversedMovement)
            .SingleOrDefaultAsync(m => m.Id == movementId && m.OrganizationId == organizationId, cancellationToken);

    public async Task<IReadOnlyList<StockMovement>> FindMovementsByParentTransactionIdAsync(Guid parentTransactionId, Guid organizationId, CancellationToken cancellationToken = default)
    {
        var items = await dbContext.StockMovements
            .Include(m => m.InventoryItem)
                .ThenInclude(i => i!.StockUnit)
            .Include(m => m.StorageLocation)
                .ThenInclude(l => l!.Farm)
            .Include(m => m.Farm)
            .Include(m => m.CropCycle)
            .Include(m => m.CropCycleStage)
            .Include(m => m.Plantation)
            .Include(m => m.FarmArea)
            .Include(m => m.LaborActivity)
                .ThenInclude(a => a!.LaborActivityType)
            .Include(m => m.ReversalMovement)
            .Include(m => m.ReversedMovement)
            .Where(m => m.ParentTransactionId == parentTransactionId && m.OrganizationId == organizationId)
            .ToListAsync(cancellationToken);

        return items;
    }

    // Find a stock balance with explicit multi-tenant organization isolation
    public Task<StockBalance?> FindBalanceAsync(Guid locationId, Guid itemId, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.StockBalances
            .Include(b => b.Farm)
            .Include(b => b.StorageLocation)
            .Include(b => b.InventoryItem)
                .ThenInclude(i => i!.StockUnit)
            .SingleOrDefaultAsync(b => b.StorageLocationId == locationId && b.InventoryItemId == itemId && b.OrganizationId == organizationId, cancellationToken);

    // Lock an existing stock balance row for update
    public Task<StockBalance?> LockBalanceAsync(Guid locationId, Guid itemId, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.StockBalances
            .FromSqlInterpolated($"SELECT * FROM stock_balances WHERE storage_location_id = {locationId} AND inventory_item_id = {itemId} AND organization_id = {organizationId} FOR UPDATE")
            .Include(b => b.Farm)
            .Include(b => b.StorageLocation)
            .Include(b => b.InventoryItem)
                .ThenInclude(i => i!.StockUnit)
            .SingleOrDefaultAsync(cancellationToken);

    // Acquire PostgreSQL transaction-level advisory lock on (storageLocationId, inventoryItemId)
    // Serializes concurrent requests for non-existent balance rows to prevent phantom lock collisions
    public async Task AcquireAdvisoryLockAsync(Guid locationId, Guid itemId, CancellationToken cancellationToken = default)
    {
        if (dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
        {
            var locStr = locationId.ToString();
            var itemStr = itemId.ToString();
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtext({locStr}), hashtext({itemStr}))",
                cancellationToken);
        }
    }

    public Task<bool> HasOpeningStockAsync(Guid locationId, Guid itemId, CancellationToken cancellationToken = default) =>
        dbContext.StockMovements
            .AnyAsync(m => m.StorageLocationId == locationId && m.InventoryItemId == itemId && m.MovementType == StockMovementType.OpeningStock, cancellationToken);

    public async Task<int> CountBalancesAsync(
        Guid organizationId,
        Guid? farmId,
        Guid? locationId,
        Guid? itemId,
        CancellationToken cancellationToken = default) =>
        await BuildBalanceQuery(organizationId, farmId, locationId, itemId).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<StockBalance>> ListBalancesAsync(
        Guid organizationId,
        Guid? farmId,
        Guid? locationId,
        Guid? itemId,
        int skip,
        int take,
        CancellationToken cancellationToken = default) =>
        await BuildBalanceQuery(organizationId, farmId, locationId, itemId)
            .AsNoTracking()
            .OrderBy(b => b.StorageLocation!.Name)
            .ThenBy(b => b.InventoryItem!.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<int> CountMovementsAsync(
        Guid organizationId,
        Guid? farmId,
        Guid? locationId,
        Guid? itemId,
        StockMovementType? movementType,
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? cropCycleId = null,
        CancellationToken cancellationToken = default) =>
        await BuildMovementQuery(organizationId, farmId, locationId, itemId, movementType, fromDate, toDate, cropCycleId).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<StockMovement>> ListMovementsAsync(
        Guid organizationId,
        Guid? farmId,
        Guid? locationId,
        Guid? itemId,
        StockMovementType? movementType,
        DateOnly? fromDate,
        DateOnly? toDate,
        int skip,
        int take,
        Guid? cropCycleId = null,
        CancellationToken cancellationToken = default) =>
        await BuildMovementQuery(organizationId, farmId, locationId, itemId, movementType, fromDate, toDate, cropCycleId)
            .AsNoTracking()
            .OrderByDescending(m => m.MovementDate)
            .ThenByDescending(m => m.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

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

    public void AddBalance(StockBalance balance) => dbContext.StockBalances.Add(balance);
    public void AddMovement(StockMovement movement) => dbContext.StockMovements.Add(movement);
    public void AddAuditLog(AuditLog auditLog) => dbContext.AuditLogs.Add(auditLog);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);

    private IQueryable<StockBalance> BuildBalanceQuery(
        Guid organizationId,
        Guid? farmId,
        Guid? locationId,
        Guid? itemId)
    {
        var query = dbContext.StockBalances
            .Include(b => b.Farm)
            .Include(b => b.StorageLocation)
            .Include(b => b.InventoryItem)
                .ThenInclude(i => i!.StockUnit)
            .Include(b => b.InventoryItem)
                .ThenInclude(i => i!.Category)
            .Where(b => b.OrganizationId == organizationId);

        if (farmId.HasValue) query = query.Where(b => b.FarmId == farmId.Value);
        if (locationId.HasValue) query = query.Where(b => b.StorageLocationId == locationId.Value);
        if (itemId.HasValue) query = query.Where(b => b.InventoryItemId == itemId.Value);

        return query;
    }

    private IQueryable<StockMovement> BuildMovementQuery(
        Guid organizationId,
        Guid? farmId,
        Guid? locationId,
        Guid? itemId,
        StockMovementType? movementType,
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? cropCycleId = null)
    {
        var query = dbContext.StockMovements
            .Include(m => m.Farm)
            .Include(m => m.StorageLocation)
            .Include(m => m.InventoryItem)
                .ThenInclude(i => i!.Category)
            .Include(m => m.InventoryItem)
                .ThenInclude(i => i!.StockUnit)
            .Include(m => m.StockUnit)
            .Include(m => m.CropCycle)
            .Include(m => m.CropCycleStage)
            .Include(m => m.Plantation)
            .Include(m => m.FarmArea)
            .Include(m => m.LaborActivity)
                .ThenInclude(a => a!.LaborActivityType)
            .Where(m => m.OrganizationId == organizationId);

        if (farmId.HasValue) query = query.Where(m => m.FarmId == farmId.Value);
        if (locationId.HasValue) query = query.Where(m => m.StorageLocationId == locationId.Value);
        if (itemId.HasValue) query = query.Where(m => m.InventoryItemId == itemId.Value);
        if (movementType.HasValue) query = query.Where(m => m.MovementType == movementType.Value);
        if (fromDate.HasValue) query = query.Where(m => m.MovementDate >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(m => m.MovementDate <= toDate.Value);
        if (cropCycleId.HasValue) query = query.Where(m => m.CropCycleId == cropCycleId.Value);

        return query;
    }
}
