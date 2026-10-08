using FarmManagement.Application.Interfaces.PlantProtection;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FarmManagement.Infrastructure.Persistence;

public sealed class PlantProtectionProductStore(ApplicationDbContext dbContext) : IPlantProtectionProductStore
{
    public Task<PlantProtectionProduct?> FindAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.PlantProtectionProducts
            .Include(p => p.InventoryItem)
                .ThenInclude(i => i!.StockUnit)
            .Include(p => p.ProductType)
            .SingleOrDefaultAsync(p => p.Id == id && p.OrganizationId == organizationId, cancellationToken);

    public Task<PlantProtectionProduct?> FindByInventoryItemIdAsync(
        Guid inventoryItemId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.PlantProtectionProducts
            .Include(p => p.InventoryItem)
                .ThenInclude(i => i!.StockUnit)
            .Include(p => p.ProductType)
            .SingleOrDefaultAsync(
                p => p.InventoryItemId == inventoryItemId && p.OrganizationId == organizationId,
                cancellationToken);

    public Task<InventoryItem?> FindInventoryItemAsync(
        Guid inventoryItemId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.InventoryItems
            .Include(i => i.StockUnit)
            .SingleOrDefaultAsync(
                i => i.Id == inventoryItemId && i.OrganizationId == organizationId,
                cancellationToken);

    public Task<ProductType?> FindProductTypeAsync(
        Guid productTypeId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.ProductTypes.SingleOrDefaultAsync(
            pt => pt.Id == productTypeId && pt.IsActive &&
                  ((pt.IsSystem && pt.OrganizationId == null) || pt.OrganizationId == organizationId),
            cancellationToken);

    public async Task<int> CountAsync(
        Guid organizationId,
        string? search,
        Guid? productTypeId,
        bool? isActive,
        CancellationToken cancellationToken = default) =>
        await BuildQuery(organizationId, search, productTypeId, isActive).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<PlantProtectionProduct>> ListAsync(
        Guid organizationId,
        int skip,
        int take,
        string? search,
        Guid? productTypeId,
        bool? isActive,
        CancellationToken cancellationToken = default) =>
        await BuildQuery(organizationId, search, productTypeId, isActive)
            .AsNoTracking()
            .Include(p => p.InventoryItem)
                .ThenInclude(i => i!.StockUnit)
            .Include(p => p.ProductType)
            .OrderBy(p => p.InventoryItem!.Name)
            .ThenBy(p => p.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<bool> HasCompletedSprayUsageAsync(
        Guid inventoryItemId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        await dbContext.SprayProducts
            .AnyAsync(
                sp => sp.InventoryItemId == inventoryItemId &&
                      sp.Spray != null &&
                      sp.Spray.OrganizationId == organizationId &&
                      sp.Spray.Status == SprayStatus.Completed,
                cancellationToken);

    public void Add(PlantProtectionProduct product) => dbContext.PlantProtectionProducts.Add(product);

    public void AddAuditLog(AuditLog auditLog) => dbContext.AuditLogs.Add(auditLog);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);

    private IQueryable<PlantProtectionProduct> BuildQuery(
        Guid organizationId,
        string? search,
        Guid? productTypeId,
        bool? isActive)
    {
        var query = dbContext.PlantProtectionProducts
            .Where(p => p.OrganizationId == organizationId);

        if (isActive.HasValue)
        {
            query = query.Where(p => p.IsActive == isActive.Value);
        }

        if (productTypeId.HasValue && productTypeId.Value != Guid.Empty)
        {
            query = query.Where(p => p.ProductTypeId == productTypeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim().ToLower()}%";
            query = query.Where(p =>
                (p.InventoryItem != null && EF.Functions.ILike(p.InventoryItem.Name, pattern)) ||
                (p.InventoryItem != null && p.InventoryItem.Sku != null && EF.Functions.ILike(p.InventoryItem.Sku, pattern)) ||
                (p.ActiveIngredient != null && EF.Functions.ILike(p.ActiveIngredient, pattern)) ||
                (p.Manufacturer != null && EF.Functions.ILike(p.Manufacturer, pattern)));
        }

        return query;
    }
}
