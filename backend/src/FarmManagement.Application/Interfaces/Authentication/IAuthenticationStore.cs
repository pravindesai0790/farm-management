using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Interfaces.Authentication;

public interface IAuthenticationStore
{
    Task<User?> FindUserByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<User?> FindUserByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    void AddAuditLog(AuditLog auditLog);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
