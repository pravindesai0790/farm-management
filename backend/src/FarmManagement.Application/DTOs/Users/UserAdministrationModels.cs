namespace FarmManagement.Application.DTOs.Users;

public sealed record UserRoleResponse(Guid Id, string Name, bool IsActive);

public sealed record UserResponse(
    Guid Id,
    Guid OrganizationId,
    string FirstName,
    string LastName,
    string Email,
    bool IsActive,
    int FailedLoginCount,
    DateTimeOffset? LockoutEnd,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    IReadOnlyList<UserRoleResponse> Roles);

public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record CreateUserRequest(
    string? FirstName,
    string? LastName,
    string? Email,
    string? Password,
    Guid? OrganizationId,
    IReadOnlyCollection<Guid>? RoleIds);

public sealed record UpdateUserRequest(string? FirstName, string? LastName);

public sealed record AssignUserRolesRequest(IReadOnlyCollection<Guid>? RoleIds);
