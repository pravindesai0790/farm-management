using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Interfaces.Inventory;

public interface IInventoryItemStore
{
    Task<InventoryItem?> FindAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default);
    Task<InventoryItem?> FindBySkuAsync(string sku, Guid organizationId, CancellationToken cancellationToken = default);
    Task<Unit?> FindUnitAsync(Guid unitId, Guid organizationId, CancellationToken cancellationToken = default);
    Task<int> CountAsync(Guid organizationId, string? search, string? category, bool? isActive, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryItem>> ListAsync(Guid organizationId, int skip, int take, string? search, string? category, bool? isActive, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the sum of all positive on-hand stock quantities for an inventory item across all storage locations in an organization.
    /// Used to prevent deactivation of items with active physical stock.
    /// </summary>
    Task<decimal> GetTotalQuantityOnHandAsync(Guid itemId, Guid organizationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether any stock movement ledger entries exist for a given inventory item.
    /// Used to enforce unit-of-measure immutability once transactions have occurred.
    /// </summary>
    Task<bool> HasMovementsAsync(Guid itemId, Guid organizationId, CancellationToken cancellationToken = default);

    void Add(InventoryItem item);
    void AddAuditLog(AuditLog auditLog);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
