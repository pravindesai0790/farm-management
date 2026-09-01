using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Interfaces.Users;

public interface IUserAdministrationStore
{
    Task<int> CountUsersAsync(
        Guid? organizationId,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> ListUsersAsync(
        Guid? organizationId,
        int skip,
        int take,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<User?> FindUserAsync(
        Guid userId,
        Guid? organizationId,
        CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default);

    Task<Organization?> FindOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Role>> FindActiveRolesAsync(
        IReadOnlyCollection<Guid> roleIds,
        CancellationToken cancellationToken = default);

    void AddUser(User user);

    void AddAuditLog(AuditLog auditLog);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
