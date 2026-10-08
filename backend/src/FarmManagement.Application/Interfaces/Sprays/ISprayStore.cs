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

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
