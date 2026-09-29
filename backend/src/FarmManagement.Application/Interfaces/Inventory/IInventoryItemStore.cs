using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Interfaces.Inventory;

public interface IInventoryItemStore
{
    Task<InventoryItem?> FindAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default);
    Task<InventoryItem?> FindBySkuAsync(string sku, Guid organizationId, CancellationToken cancellationToken = default);
    Task<Unit?> FindUnitAsync(Guid unitId, Guid organizationId, CancellationToken cancellationToken = default);
    Task<int> CountAsync(Guid organizationId, string? search, string? category, bool? isActive, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryItem>> ListAsync(Guid organizationId, int skip, int take, string? search, string? category, bool? isActive, CancellationToken cancellationToken = default);
    void Add(InventoryItem item);
    void AddAuditLog(AuditLog auditLog);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
