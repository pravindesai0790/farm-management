using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.Interfaces.Inventory;

public interface IInventoryStockStore
{
    Task<InventoryItem?> FindItemAsync(Guid itemId, Guid organizationId, CancellationToken cancellationToken = default);
    Task<StorageLocation?> FindLocationAsync(Guid locationId, Guid organizationId, CancellationToken cancellationToken = default);
    Task<Farm?> FindFarmAsync(Guid farmId, Guid organizationId, CancellationToken cancellationToken = default);

    // Find a stock balance with multi-tenant organization isolation
    Task<StockBalance?> FindBalanceAsync(Guid locationId, Guid itemId, Guid organizationId, CancellationToken cancellationToken = default);

    Task<CropCycle?> FindCropCycleAsync(Guid cycleId, Guid organizationId, CancellationToken cancellationToken = default);
    Task<CropCycleStage?> FindCropCycleStageAsync(Guid stageId, CancellationToken cancellationToken = default);
    Task<CropPlantation?> FindPlantationAsync(Guid plantationId, Guid organizationId, CancellationToken cancellationToken = default);
    Task<FarmArea?> FindFarmAreaAsync(Guid areaId, Guid organizationId, CancellationToken cancellationToken = default);
    Task<LaborActivity?> FindLaborActivityAsync(Guid activityId, Guid organizationId, CancellationToken cancellationToken = default);

    // Row-level lock on existing stock balance
    Task<StockBalance?> LockBalanceAsync(Guid locationId, Guid itemId, Guid organizationId, CancellationToken cancellationToken = default);

    // Acquire PostgreSQL transaction-level advisory lock on (storageLocationId, inventoryItemId)
    // Prevents phantom lock race conditions when creating initial stock balance rows
    Task AcquireAdvisoryLockAsync(Guid locationId, Guid itemId, CancellationToken cancellationToken = default);

    Task<bool> HasOpeningStockAsync(Guid locationId, Guid itemId, CancellationToken cancellationToken = default);

    Task<int> CountBalancesAsync(Guid organizationId, Guid? farmId, Guid? locationId, Guid? itemId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockBalance>> ListBalancesAsync(Guid organizationId, Guid? farmId, Guid? locationId, Guid? itemId, int skip, int take, CancellationToken cancellationToken = default);

    Task<int> CountMovementsAsync(Guid organizationId, Guid? farmId, Guid? locationId, Guid? itemId, StockMovementType? movementType, DateOnly? fromDate, DateOnly? toDate, Guid? cropCycleId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockMovement>> ListMovementsAsync(Guid organizationId, Guid? farmId, Guid? locationId, Guid? itemId, StockMovementType? movementType, DateOnly? fromDate, DateOnly? toDate, int skip, int take, Guid? cropCycleId = null, CancellationToken cancellationToken = default);

    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);

    void AddBalance(StockBalance balance);
    void AddMovement(StockMovement movement);
    void AddAuditLog(AuditLog auditLog);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
