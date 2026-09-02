using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Interfaces.Organizations;

public interface IOrganizationStore
{
    Task<Organization?> FindAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(
        string code,
        Guid? excludingOrganizationId = null,
        CancellationToken cancellationToken = default);

    void Add(Organization organization);

    void AddAuditLog(AuditLog auditLog);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
