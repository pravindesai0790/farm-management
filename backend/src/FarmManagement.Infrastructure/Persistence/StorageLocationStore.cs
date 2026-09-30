using FarmManagement.Application.Interfaces.Inventory;
using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FarmManagement.Infrastructure.Persistence;

public sealed class StorageLocationStore(ApplicationDbContext dbContext) : IStorageLocationStore
{
    public Task<StorageLocation?> FindAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.StorageLocations
            .Include(location => location.Farm)
            .SingleOrDefaultAsync(location => location.Id == id && location.OrganizationId == organizationId, cancellationToken);

    public Task<Farm?> FindFarmAsync(Guid farmId, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.Farms.SingleOrDefaultAsync(farm => farm.Id == farmId && farm.OrganizationId == organizationId, cancellationToken);

    public async Task<int> CountAsync(
        Guid organizationId,
        Guid? farmId,
        bool? isActive,
        CancellationToken cancellationToken = default) =>
        await BuildQuery(organizationId, farmId, isActive).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<StorageLocation>> ListAsync(
        Guid organizationId,
        Guid? farmId,
        int skip,
        int take,
        bool? isActive,
        CancellationToken cancellationToken = default) =>
        await BuildQuery(organizationId, farmId, isActive)
            .AsNoTracking()
            .OrderBy(location => location.Name)
            .ThenBy(location => location.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<bool> ExistsNameInFarmAsync(
        Guid farmId,
        string name,
        Guid? excludeLocationId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim().ToLower();
        var query = dbContext.StorageLocations
            .Where(location => location.FarmId == farmId && location.Name.ToLower() == normalizedName);

        if (excludeLocationId.HasValue)
        {
            query = query.Where(location => location.Id != excludeLocationId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// Counts distinct inventory items holding positive on-hand stock (> 0) in the specified storage location.
    /// Used by StorageLocationService to prevent deactivating storage locations that contain active inventory.
    /// </summary>
    public async Task<int> CountItemsWithStockAsync(
        Guid locationId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        await dbContext.StockBalances
            .Where(b => b.StorageLocationId == locationId && b.OrganizationId == organizationId && b.QuantityOnHand > 0m)
            .CountAsync(cancellationToken);

    public void Add(StorageLocation location) => dbContext.StorageLocations.Add(location);

    public void AddAuditLog(AuditLog auditLog) => dbContext.AuditLogs.Add(auditLog);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);

    private IQueryable<StorageLocation> BuildQuery(
        Guid organizationId,
        Guid? farmId,
        bool? isActive)
    {
        var query = dbContext.StorageLocations
            .Include(location => location.Farm)
            .Where(location => location.OrganizationId == organizationId);

        if (farmId.HasValue)
        {
            query = query.Where(location => location.FarmId == farmId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(location => location.IsActive == isActive.Value);
        }

        return query;
    }
}
