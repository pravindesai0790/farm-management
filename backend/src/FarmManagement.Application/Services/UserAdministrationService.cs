using System.Text.Json;
using FarmManagement.Application.Common;
using FarmManagement.Application.Common.Constants;
using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.Users;
using FarmManagement.Application.Interfaces.Authentication;
using FarmManagement.Application.Interfaces.Users;
using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Services;

public sealed class UserAdministrationService(
    IUserAdministrationStore store,
    IPasswordService passwordService,
    IRefreshTokenService refreshTokenService) : IUserAdministrationService
{
    private const int DefaultPageSize = 20;
    private const int MaximumPageSize = 100;

    public async Task<PagedResponse<UserResponse>> ListAsync(
        UserAdministrationActor actor,
        int page,
        int pageSize,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        if (page < 1)
        {
            throw Validation("page", "Page must be at least 1.");
        }

        if (pageSize == 0)
        {
            pageSize = DefaultPageSize;
        }
        else if (pageSize < 1 || pageSize > MaximumPageSize)
        {
            throw Validation("pageSize", $"Page size must be between 1 and {MaximumPageSize}.");
        }

        var organizationId = actor.CanManageAllOrganizations ? (Guid?)null : actor.OrganizationId;
        var normalizedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        var totalCount = await store.CountUsersAsync(
            organizationId,
            normalizedSearch,
            isActive,
            cancellationToken);
        var users = await store.ListUsersAsync(
            organizationId,
            checked((page - 1) * pageSize),
            pageSize,
            normalizedSearch,
            isActive,
            cancellationToken);

        return new PagedResponse<UserResponse>(
            users.Select(ToResponse).ToArray(),
            page,
            pageSize,
            totalCount);
    }

    public async Task<UserResponse> GetAsync(
        UserAdministrationActor actor,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        return ToResponse(await FindUserOrThrowAsync(actor, userId, cancellationToken));
    }

    public async Task<UserResponse> CreateAsync(
        UserAdministrationActor actor,
        CreateUserRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ValidateCreateRequest(request);

        var firstName = request.FirstName!.Trim();
        var lastName = request.LastName!.Trim();
        var email = request.Email!.Trim().ToLowerInvariant();
        PasswordPolicy.ValidateNewPassword(request.Password, email, firstName, lastName, "password");

        if (await store.EmailExistsAsync(email, cancellationToken))
        {
            throw new ConflictException("A user with this email address already exists.");
        }

        var organizationId = actor.CanManageAllOrganizations
            ? request.OrganizationId
            : actor.OrganizationId;
        if (organizationId is null || organizationId == Guid.Empty)
        {
            throw Validation("organizationId", "An organization is required.");
        }

        var organization = await store.FindOrganizationAsync(organizationId.Value, cancellationToken);
        if (organization is null || !organization.IsActive)
        {
            throw new ResourceNotFoundException("The organization was not found or is inactive.");
        }

        var roles = await GetAssignableRolesAsync(actor, request.RoleIds, cancellationToken);
        var passwordUser = new User(organizationId.Value, firstName, lastName, email, "temporary");
        var passwordHash = passwordService.HashPassword(passwordUser, request.Password!);
        var user = new User(organizationId.Value, firstName, lastName, email, passwordHash);
        var now = DateTimeOffset.UtcNow;
        foreach (var role in roles)
        {
            user.UserRoles.Add(new UserRole(user.Id, role.Id, now, actor.UserId, role));
        }

        store.AddUser(user);
        AddAudit(
            actor,
            user,
            "User.Created",
            new { user.Email, user.FirstName, user.LastName, roleIds = roles.Select(role => role.Id) },
            ipAddress);
        if (roles.Count > 0)
        {
            AddAudit(
                actor,
                user,
                "User.RoleAssigned",
                new { roleIds = roles.Select(role => role.Id) },
                ipAddress);
        }

        await store.SaveChangesAsync(cancellationToken);
        return ToResponse(user);
    }

    public async Task<UserResponse> UpdateAsync(
        UserAdministrationActor actor,
        Guid userId,
        UpdateUserRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        if (request is null)
        {
            throw Validation("request", "A request body is required.");
        }

        ValidateName(request.FirstName, "firstName");
        ValidateName(request.LastName, "lastName");
        var user = await FindUserOrThrowAsync(actor, userId, cancellationToken);
        if (user.UpdateProfile(request.FirstName!, request.LastName!, DateTimeOffset.UtcNow))
        {
            AddAudit(actor, user, "User.Updated", new { user.FirstName, user.LastName }, ipAddress);
            await store.SaveChangesAsync(cancellationToken);
        }

        return ToResponse(user);
    }

    public async Task<bool> ActivateAsync(
        UserAdministrationActor actor,
        Guid userId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var user = await FindUserOrThrowAsync(actor, userId, cancellationToken);
        if (!user.Activate(DateTimeOffset.UtcNow))
        {
            return false;
        }

        AddAudit(actor, user, "User.Activated", null, ipAddress);
        await store.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeactivateAsync(
        UserAdministrationActor actor,
        Guid userId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var user = await FindUserOrThrowAsync(actor, userId, cancellationToken);
        if (!user.Deactivate(DateTimeOffset.UtcNow))
        {
            return false;
        }

        var revokedTokenCount = await refreshTokenService.RevokeAllForUserAsync(
            user.Id,
            ipAddress,
            cancellationToken);
        AddAudit(actor, user, "User.Deactivated", new { revokedTokenCount }, ipAddress);
        await store.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UnlockAsync(
        UserAdministrationActor actor,
        Guid userId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var user = await FindUserOrThrowAsync(actor, userId, cancellationToken);
        if (!user.Unlock(DateTimeOffset.UtcNow))
        {
            return false;
        }

        AddAudit(actor, user, "User.Unlocked", null, ipAddress);
        await store.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<UserResponse> AssignRolesAsync(
        UserAdministrationActor actor,
        Guid userId,
        AssignUserRolesRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        if (request is null)
        {
            throw Validation("request", "A request body is required.");
        }

        var user = await FindUserOrThrowAsync(actor, userId, cancellationToken);
        var roles = await GetAssignableRolesAsync(actor, request.RoleIds, cancellationToken);
        var existingRoleIds = user.UserRoles.Select(userRole => userRole.RoleId).ToHashSet();
        var requestedRoleIds = roles.Select(role => role.Id).ToHashSet();
        if (existingRoleIds.SetEquals(requestedRoleIds))
        {
            return ToResponse(user);
        }

        var previousRoleIds = existingRoleIds.ToArray();
        user.UserRoles.Clear();
        var now = DateTimeOffset.UtcNow;
        foreach (var role in roles)
        {
            user.UserRoles.Add(new UserRole(user.Id, role.Id, now, actor.UserId, role));
        }

        AddAudit(
            actor,
            user,
            "User.RoleAssigned",
            new { previousRoleIds, roleIds = requestedRoleIds },
            ipAddress);
        await store.SaveChangesAsync(cancellationToken);
        return ToResponse(user);
    }

    private async Task<User> FindUserOrThrowAsync(
        UserAdministrationActor actor,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            throw new ResourceNotFoundException("The user was not found.");
        }

        var organizationId = actor.CanManageAllOrganizations ? (Guid?)null : actor.OrganizationId;
        return await store.FindUserAsync(userId, organizationId, cancellationToken)
            ?? throw new ResourceNotFoundException("The user was not found.");
    }

    private async Task<IReadOnlyList<Role>> GetAssignableRolesAsync(
        UserAdministrationActor actor,
        IReadOnlyCollection<Guid>? roleIds,
        CancellationToken cancellationToken)
    {
        var requestedRoleIds = roleIds?.Distinct().ToArray() ?? [];
        if (requestedRoleIds.Any(roleId => roleId == Guid.Empty))
        {
            throw Validation("roleIds", "Role IDs must be valid.");
        }

        var roles = await store.FindActiveRolesAsync(requestedRoleIds, cancellationToken);
        if (roles.Count != requestedRoleIds.Length)
        {
            throw Validation("roleIds", "One or more roles were not found or are inactive.");
        }

        if (!actor.CanManageAllOrganizations && roles.Any(role => string.Equals(
                role.Name,
                AuthorizationConstants.SuperAdminRoleName,
                StringComparison.Ordinal)))
        {
            throw new ForbiddenException("Only a SuperAdmin can assign the SuperAdmin role.");
        }

        return roles;
    }

    private static void ValidateCreateRequest(CreateUserRequest? request)
    {
        if (request is null)
        {
            throw Validation("request", "A request body is required.");
        }

        ValidateName(request.FirstName, "firstName");
        ValidateName(request.LastName, "lastName");
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw Validation("email", "Email is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw Validation("password", "Password is required.");
        }
    }

    private static void ValidateName(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw Validation(fieldName, $"{fieldName} is required.");
        }
    }

    private static void ValidateActor(UserAdministrationActor actor)
    {
        if (actor.UserId == Guid.Empty || actor.OrganizationId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("The access token does not contain a valid user scope.");
        }
    }

    private void AddAudit(
        UserAdministrationActor actor,
        User target,
        string action,
        object? details,
        string? ipAddress)
    {
        store.AddAuditLog(new AuditLog(
            action,
            target.OrganizationId,
            actor.UserId,
            entityType: "User",
            entityId: target.Id,
            details: details is null ? null : JsonSerializer.SerializeToDocument(details),
            ipAddress: ipAddress));
    }

    private static UserResponse ToResponse(User user) =>
        new(
            user.Id,
            user.OrganizationId,
            user.FirstName,
            user.LastName,
            user.Email,
            user.IsActive,
            user.FailedLoginCount,
            user.LockoutEnd,
            user.LastLoginAt,
            user.CreatedAt,
            user.UpdatedAt,
            user.UserRoles
                .Where(userRole => userRole.Role is not null)
                .Select(userRole => new UserRoleResponse(
                    userRole.RoleId,
                    userRole.Role.Name,
                    userRole.Role.IsActive))
                .OrderBy(role => role.Name, StringComparer.Ordinal)
                .ToArray());

    private static ValidationException Validation(string fieldName, string message) =>
        new("Validation failed", new Dictionary<string, string[]> { [fieldName] = [message] });
}
