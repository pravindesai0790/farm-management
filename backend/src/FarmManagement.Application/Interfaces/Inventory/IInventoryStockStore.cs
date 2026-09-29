using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.Interfaces.Inventory;

public interface IInventoryStockStore
{
    Task<InventoryItem?> FindItemAsync(Guid itemId, Guid organizationId, CancellationToken cancellationToken = default);
    Task<StorageLocation?> FindLocationAsync(Guid locationId, Guid organizationId, CancellationToken cancellationToken = default);
    Task<Farm?> FindFarmAsync(Guid farmId, Guid organizationId, CancellationToken cancellationToken = default);

    Task<StockBalance?> FindBalanceAsync(Guid locationId, Guid itemId, CancellationToken cancellationToken = default);
    Task<StockBalance?> LockBalanceAsync(Guid locationId, Guid itemId, Guid organizationId, CancellationToken cancellationToken = default);

    Task<bool> HasOpeningStockAsync(Guid locationId, Guid itemId, CancellationToken cancellationToken = default);

    Task<int> CountBalancesAsync(Guid organizationId, Guid? farmId, Guid? locationId, Guid? itemId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockBalance>> ListBalancesAsync(Guid organizationId, Guid? farmId, Guid? locationId, Guid? itemId, int skip, int take, CancellationToken cancellationToken = default);

    Task<int> CountMovementsAsync(Guid organizationId, Guid? farmId, Guid? locationId, Guid? itemId, StockMovementType? movementType, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockMovement>> ListMovementsAsync(Guid organizationId, Guid? farmId, Guid? locationId, Guid? itemId, StockMovementType? movementType, DateOnly? fromDate, DateOnly? toDate, int skip, int take, CancellationToken cancellationToken = default);

    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);

    void AddBalance(StockBalance balance);
    void AddMovement(StockMovement movement);
    void AddAuditLog(AuditLog auditLog);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
