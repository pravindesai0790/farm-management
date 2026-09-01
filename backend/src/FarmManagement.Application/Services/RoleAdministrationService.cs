using System.Text.Json;
using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.Roles;
using FarmManagement.Application.Interfaces.Roles;
using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Services;

public sealed class RoleAdministrationService(
    IRoleAdministrationStore store) : IRoleAdministrationService
{
    private const int MaximumNameLength = 100;
    private const int MaximumDescriptionLength = 500;

    public async Task<IReadOnlyList<RoleResponse>> ListRolesAsync(
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        var roles = await store.ListRolesAsync(isActive, cancellationToken);
        return roles.Select(ToResponse).ToArray();
    }

    public async Task<RoleResponse> GetRoleAsync(
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        return ToResponse(await FindRoleOrThrowAsync(roleId, cancellationToken));
    }

    public async Task<RoleResponse> CreateRoleAsync(
        RoleAdministrationActor actor,
        CreateRoleRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ValidateRoleRequest(request);

        var name = request.Name!.Trim();
        if (await store.RoleNameExistsAsync(name, cancellationToken: cancellationToken))
        {
            throw new ConflictException("A role with this name already exists.");
        }

        var role = new Role(name, NormalizeDescription(request.Description));
        store.AddRole(role);
        AddAudit(
            actor,
            role,
            "Role.Created",
            new { role.Name, role.Description, role.IsSystemRole },
            ipAddress);
        await store.SaveChangesAsync(cancellationToken);
        return ToResponse(role);
    }

    public async Task<RoleResponse> UpdateRoleAsync(
        RoleAdministrationActor actor,
        Guid roleId,
        UpdateRoleRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ValidateRoleRequest(request);

        var role = await FindRoleOrThrowAsync(roleId, cancellationToken);
        var name = request.Name!.Trim();
        if (!string.Equals(role.Name, name, StringComparison.OrdinalIgnoreCase) &&
            await store.RoleNameExistsAsync(name, role.Id, cancellationToken))
        {
            throw new ConflictException("A role with this name already exists.");
        }

        if (role.UpdateDetails(name, request.Description, DateTimeOffset.UtcNow))
        {
            AddAudit(
                actor,
                role,
                "Role.Updated",
                new { role.Name, role.Description },
                ipAddress);
            await store.SaveChangesAsync(cancellationToken);
        }

        return ToResponse(role);
    }

    public async Task<bool> ActivateRoleAsync(
        RoleAdministrationActor actor,
        Guid roleId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var role = await FindRoleOrThrowAsync(roleId, cancellationToken);
        if (!role.Activate(DateTimeOffset.UtcNow))
        {
            return false;
        }

        AddAudit(actor, role, "Role.Activated", null, ipAddress);
        await store.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeactivateRoleAsync(
        RoleAdministrationActor actor,
        Guid roleId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var role = await FindRoleOrThrowAsync(roleId, cancellationToken);
        if (!role.Deactivate(DateTimeOffset.UtcNow))
        {
            return false;
        }

        AddAudit(actor, role, "Role.Deactivated", null, ipAddress);
        await store.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<RoleResponse> UpdateRolePermissionsAsync(
        RoleAdministrationActor actor,
        Guid roleId,
        UpdateRolePermissionsRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        if (request is null)
        {
            throw Validation("request", "A request body is required.");
        }

        var permissionIds = request.PermissionIds?.Distinct().ToArray() ?? [];
        if (permissionIds.Any(permissionId => permissionId == Guid.Empty))
        {
            throw Validation("permissionIds", "Permission IDs must be valid.");
        }

        var role = await FindRoleOrThrowAsync(roleId, cancellationToken);
        var permissions = await store.FindPermissionsAsync(permissionIds, cancellationToken);
        if (permissions.Count != permissionIds.Length)
        {
            throw Validation("permissionIds", "One or more permissions were not found.");
        }

        var previousPermissionIds = role.RolePermissions
            .Select(rolePermission => rolePermission.PermissionId)
            .ToHashSet();
        var requestedPermissionIds = permissionIds.ToHashSet();
        if (previousPermissionIds.SetEquals(requestedPermissionIds))
        {
            return ToResponse(role);
        }

        role.RolePermissions.Clear();
        foreach (var permission in permissions.OrderBy(permission => permission.Name, StringComparer.Ordinal))
        {
            role.RolePermissions.Add(new RolePermission(role.Id, permission.Id, permission));
        }

        AddAudit(
            actor,
            role,
            "Role.PermissionUpdated",
            new
            {
                previousPermissionIds = previousPermissionIds.OrderBy(id => id),
                permissionIds = requestedPermissionIds.OrderBy(id => id)
            },
            ipAddress);
        await store.SaveChangesAsync(cancellationToken);
        return ToResponse(role);
    }

    public async Task<IReadOnlyList<PermissionResponse>> ListPermissionsAsync(
        CancellationToken cancellationToken = default)
    {
        var permissions = await store.ListPermissionsAsync(cancellationToken);
        return permissions.Select(ToResponse).ToArray();
    }

    private async Task<Role> FindRoleOrThrowAsync(
        Guid roleId,
        CancellationToken cancellationToken)
    {
        if (roleId == Guid.Empty)
        {
            throw new ResourceNotFoundException("The role was not found.");
        }

        return await store.FindRoleAsync(roleId, cancellationToken)
            ?? throw new ResourceNotFoundException("The role was not found.");
    }

    private void AddAudit(
        RoleAdministrationActor actor,
        Role role,
        string action,
        object? details,
        string? ipAddress)
    {
        store.AddAuditLog(new AuditLog(
            action,
            actor.OrganizationId,
            actor.UserId,
            entityType: "Role",
            entityId: role.Id,
            details: details is null ? null : JsonSerializer.SerializeToDocument(details),
            ipAddress: ipAddress));
    }

    private static void ValidateActor(RoleAdministrationActor actor)
    {
        if (actor.UserId == Guid.Empty || actor.OrganizationId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("The access token does not contain a valid user scope.");
        }
    }

    private static void ValidateRoleRequest(CreateRoleRequest? request)
    {
        if (request is null)
        {
            throw Validation("request", "A request body is required.");
        }

        ValidateRoleFields(request.Name, request.Description);
    }

    private static void ValidateRoleRequest(UpdateRoleRequest? request)
    {
        if (request is null)
        {
            throw Validation("request", "A request body is required.");
        }

        ValidateRoleFields(request.Name, request.Description);
    }

    private static void ValidateRoleFields(string? name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw Validation("name", "Role name is required.");
        }

        if (name.Trim().Length > MaximumNameLength)
        {
            throw Validation("name", $"Role name must be {MaximumNameLength} characters or fewer.");
        }

        if (description?.Trim().Length > MaximumDescriptionLength)
        {
            throw Validation(
                "description",
                $"Role description must be {MaximumDescriptionLength} characters or fewer.");
        }
    }

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    private static RoleResponse ToResponse(Role role) =>
        new(
            role.Id,
            role.Name,
            role.Description,
            role.IsSystemRole,
            role.IsActive,
            role.CreatedAt,
            role.UpdatedAt,
            role.RolePermissions
                .Where(rolePermission => rolePermission.Permission is not null)
                .Select(rolePermission => ToResponse(rolePermission.Permission))
                .OrderBy(permission => permission.Name, StringComparer.Ordinal)
                .ToArray());

    private static PermissionResponse ToResponse(Permission permission) =>
        new(
            permission.Id,
            permission.Name,
            permission.Description,
            permission.Module,
            permission.CreatedAt);

    private static ValidationException Validation(string fieldName, string message) =>
        new("Validation failed", new Dictionary<string, string[]> { [fieldName] = [message] });
}
