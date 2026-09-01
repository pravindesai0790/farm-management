namespace FarmManagement.Application.DTOs.Roles;

public sealed record PermissionResponse(
    Guid Id,
    string Name,
    string? Description,
    string Module,
    DateTimeOffset CreatedAt);

public sealed record RoleResponse(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystemRole,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    IReadOnlyList<PermissionResponse> Permissions);

public sealed record CreateRoleRequest(string? Name, string? Description);

public sealed record UpdateRoleRequest(string? Name, string? Description);

public sealed record UpdateRolePermissionsRequest(IReadOnlyCollection<Guid>? PermissionIds);
