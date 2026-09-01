using FarmManagement.Application.DTOs.Roles;

namespace FarmManagement.Application.Interfaces.Roles;

public sealed record RoleAdministrationActor(Guid UserId, Guid OrganizationId);

public interface IRoleAdministrationService
{
    Task<IReadOnlyList<RoleResponse>> ListRolesAsync(
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<RoleResponse> GetRoleAsync(
        Guid roleId,
        CancellationToken cancellationToken = default);

    Task<RoleResponse> CreateRoleAsync(
        RoleAdministrationActor actor,
        CreateRoleRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<RoleResponse> UpdateRoleAsync(
        RoleAdministrationActor actor,
        Guid roleId,
        UpdateRoleRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<bool> ActivateRoleAsync(
        RoleAdministrationActor actor,
        Guid roleId,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateRoleAsync(
        RoleAdministrationActor actor,
        Guid roleId,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<RoleResponse> UpdateRolePermissionsAsync(
        RoleAdministrationActor actor,
        Guid roleId,
        UpdateRolePermissionsRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PermissionResponse>> ListPermissionsAsync(
        CancellationToken cancellationToken = default);
}
