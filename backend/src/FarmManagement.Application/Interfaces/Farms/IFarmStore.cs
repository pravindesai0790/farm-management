using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Interfaces.Farms;

public interface IFarmStore
{
    Task<int> CountAsync(
        Guid organizationId,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Farm>> ListAsync(
        Guid organizationId,
        int skip,
        int take,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<Farm?> FindAsync(
        Guid farmId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(
        Guid organizationId,
        string code,
        Guid? excludingFarmId = null,
        CancellationToken cancellationToken = default);

    Task<FarmOwnershipType?> FindOwnershipTypeAsync(
        Guid ownershipTypeId,
        CancellationToken cancellationToken = default);

    Task<Unit?> FindAreaUnitAsync(
        Guid unitId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    void Add(Farm farm);

    void AddAuditLog(AuditLog auditLog);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
