using FarmManagement.Application.DTOs.Users;

namespace FarmManagement.Application.Interfaces.Users;

public sealed record UserAdministrationActor(
    Guid UserId,
    Guid OrganizationId,
    bool CanManageAllOrganizations);

public interface IUserAdministrationService
{
    Task<PagedResponse<UserResponse>> ListAsync(
        UserAdministrationActor actor,
        int page,
        int pageSize,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<UserResponse> GetAsync(
        UserAdministrationActor actor,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<UserResponse> CreateAsync(
        UserAdministrationActor actor,
        CreateUserRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<UserResponse> UpdateAsync(
        UserAdministrationActor actor,
        Guid userId,
        UpdateUserRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<bool> ActivateAsync(
        UserAdministrationActor actor,
        Guid userId,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(
        UserAdministrationActor actor,
        Guid userId,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<bool> UnlockAsync(
        UserAdministrationActor actor,
        Guid userId,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<UserResponse> AssignRolesAsync(
        UserAdministrationActor actor,
        Guid userId,
        AssignUserRolesRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);
}
