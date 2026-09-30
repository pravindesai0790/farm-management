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
        CancellationToken cancellationToken = default) =>
        await BuildMovementQuery(organizationId, farmId, locationId, itemId, movementType, fromDate, toDate).CountAsync(cancellationToken);

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
        CancellationToken cancellationToken = default) =>
        await BuildMovementQuery(organizationId, farmId, locationId, itemId, movementType, fromDate, toDate)
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
        DateOnly? toDate)
    {
        var query = dbContext.StockMovements
            .Include(m => m.Farm)
            .Include(m => m.StorageLocation)
            .Include(m => m.InventoryItem)
            .Include(m => m.StockUnit)
            .Where(m => m.OrganizationId == organizationId);

        if (farmId.HasValue) query = query.Where(m => m.FarmId == farmId.Value);
        if (locationId.HasValue) query = query.Where(m => m.StorageLocationId == locationId.Value);
        if (itemId.HasValue) query = query.Where(m => m.InventoryItemId == itemId.Value);
        if (movementType.HasValue) query = query.Where(m => m.MovementType == movementType.Value);
        if (fromDate.HasValue) query = query.Where(m => m.MovementDate >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(m => m.MovementDate <= toDate.Value);

        return query;
    }
}
