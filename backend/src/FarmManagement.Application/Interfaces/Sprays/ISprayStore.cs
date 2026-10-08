using FarmManagement.Application.DTOs.Sprays;
using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Interfaces.Sprays;

public interface ISprayStore
{
    Task<Spray?> FindAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        Guid organizationId,
        SprayListQuery query,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Spray>> ListAsync(
        Guid organizationId,
        SprayListQuery query,
        int skip,
        int take,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    void Add(Spray spray);

    void AddAuditLog(AuditLog auditLog);

    Task<Farm?> FindFarmAsync(Guid farmId, Guid organizationId, CancellationToken cancellationToken = default);

    Task<FarmArea?> FindFarmAreaAsync(Guid farmAreaId, Guid organizationId, CancellationToken cancellationToken = default);

    Task<CropPlantation?> FindPlantationAsync(Guid plantationId, Guid organizationId, CancellationToken cancellationToken = default);

    Task<CropCycle?> FindCropCycleAsync(Guid cropCycleId, Guid organizationId, CancellationToken cancellationToken = default);

    Task<CropCycleStage?> FindCropCycleStageAsync(Guid cropCycleStageId, Guid organizationId, CancellationToken cancellationToken = default);

    Task<Target?> FindTargetAsync(Guid targetId, Guid organizationId, CancellationToken cancellationToken = default);

    Task<ApplicationMethod?> FindApplicationMethodAsync(Guid applicationMethodId, Guid organizationId, CancellationToken cancellationToken = default);

    Task<Unit?> FindUnitAsync(Guid unitId, Guid organizationId, CancellationToken cancellationToken = default);

    Task<InventoryItem?> FindInventoryItemAsync(Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default);

    Task<bool> HasActivePlantProtectionProfileAsync(Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default);

    Task<StorageLocation?> FindStorageLocationAsync(Guid storageLocationId, Guid organizationId, CancellationToken cancellationToken = default);

    Task<StockBalance?> FindStockBalanceAsync(Guid storageLocationId, Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default);

    void RemoveSprayProduct(SprayProduct product);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
