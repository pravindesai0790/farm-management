using FarmManagement.Application.Interfaces.Inventory;
using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FarmManagement.Infrastructure.Persistence;

public sealed class InventoryItemStore(ApplicationDbContext dbContext) : IInventoryItemStore
{
    public Task<InventoryItem?> FindAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.InventoryItems
            .Include(item => item.StockUnit)
            .SingleOrDefaultAsync(item => item.Id == id && item.OrganizationId == organizationId, cancellationToken);

    public Task<InventoryItem?> FindBySkuAsync(string sku, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.InventoryItems
            .SingleOrDefaultAsync(
                item => item.OrganizationId == organizationId && item.Sku != null && item.Sku.ToLower() == sku.Trim().ToLower(),
                cancellationToken);

    public Task<Unit?> FindUnitAsync(Guid unitId, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.Units.SingleOrDefaultAsync(
            unit => unit.Id == unitId && unit.IsActive &&
                    ((unit.IsSystem && unit.OrganizationId == null) || unit.OrganizationId == organizationId),
            cancellationToken);

    public async Task<int> CountAsync(
        Guid organizationId,
        string? search,
        string? category,
        bool? isActive,
        CancellationToken cancellationToken = default) =>
        await BuildQuery(organizationId, search, category, isActive).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<InventoryItem>> ListAsync(
        Guid organizationId,
        int skip,
        int take,
        string? search,
        string? category,
        bool? isActive,
        CancellationToken cancellationToken = default) =>
        await BuildQuery(organizationId, search, category, isActive)
            .AsNoTracking()
            .OrderBy(item => item.Name)
            .ThenBy(item => item.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

    public void Add(InventoryItem item) => dbContext.InventoryItems.Add(item);

    public void AddAuditLog(AuditLog auditLog) => dbContext.AuditLogs.Add(auditLog);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);

    private IQueryable<InventoryItem> BuildQuery(
        Guid organizationId,
        string? search,
        string? category,
        bool? isActive)
    {
        var query = dbContext.InventoryItems
            .Include(item => item.StockUnit)
            .Where(item => item.OrganizationId == organizationId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(item =>
                EF.Functions.ILike(item.Name, pattern) ||
                (item.Sku != null && EF.Functions.ILike(item.Sku, pattern)) ||
                (item.Category != null && EF.Functions.ILike(item.Category, pattern)));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(item => item.Category != null && item.Category.ToLower() == category.Trim().ToLower());
        }

        if (isActive.HasValue)
        {
            query = query.Where(item => item.IsActive == isActive.Value);
        }

        return query;
    }
}
