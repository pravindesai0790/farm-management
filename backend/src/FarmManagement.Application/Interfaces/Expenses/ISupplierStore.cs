using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Interfaces.Expenses;

public interface ISupplierStore
{
    Task<int> CountAsync(
        Guid organizationId,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Supplier>> ListAsync(
        Guid organizationId,
        int skip,
        int take,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<Supplier?> FindAsync(
        Guid supplierId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(
        Guid organizationId,
        string name,
        Guid? excludingSupplierId = null,
        CancellationToken cancellationToken = default);

    Task<bool> HasHistoricalReferencesAsync(
        Guid supplierId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    void Add(Supplier supplier);

    void AddAuditLog(AuditLog auditLog);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
