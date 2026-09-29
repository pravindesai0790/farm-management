using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Interfaces.Inventory;

public interface IStorageLocationStore
{
    Task<StorageLocation?> FindAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default);
    Task<Farm?> FindFarmAsync(Guid farmId, Guid organizationId, CancellationToken cancellationToken = default);
    Task<int> CountAsync(Guid organizationId, Guid? farmId, bool? isActive, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StorageLocation>> ListAsync(Guid organizationId, Guid? farmId, int skip, int take, bool? isActive, CancellationToken cancellationToken = default);
    Task<bool> ExistsNameInFarmAsync(Guid farmId, string name, Guid? excludeLocationId = null, CancellationToken cancellationToken = default);
    void Add(StorageLocation location);
    void AddAuditLog(AuditLog auditLog);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
