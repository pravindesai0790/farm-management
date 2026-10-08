using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Interfaces.PlantProtection;

public interface IPlantProtectionProductStore
{
    Task<PlantProtectionProduct?> FindAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default);

    Task<PlantProtectionProduct?> FindByInventoryItemIdAsync(Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default);

    Task<InventoryItem?> FindInventoryItemAsync(Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default);

    Task<ProductType?> FindProductTypeAsync(Guid productTypeId, Guid organizationId, CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        Guid organizationId,
        string? search,
        Guid? productTypeId,
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlantProtectionProduct>> ListAsync(
        Guid organizationId,
        int skip,
        int take,
        string? search,
        Guid? productTypeId,
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<bool> HasCompletedSprayUsageAsync(
        Guid inventoryItemId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    void Add(PlantProtectionProduct product);

    void AddAuditLog(AuditLog auditLog);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
