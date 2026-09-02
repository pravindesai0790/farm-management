using System.Text.Json;
using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.Organizations;
using FarmManagement.Application.Interfaces.Organizations;
using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Services;

public sealed class OrganizationService(IOrganizationStore store) : IOrganizationService
{
    private const int MaximumNameLength = 200;
    private const int MaximumCodeLength = 50;

    public async Task<OrganizationListResponse> ListAsync(
        OrganizationActor actor,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var organizations = actor.CanManageAllOrganizations
            ? await store.ListAsync(cancellationToken)
            : [await FindOrganizationOrThrowAsync(actor, cancellationToken)];
        return new OrganizationListResponse(organizations.Select(ToResponse).ToArray());
    }

    public async Task<OrganizationResponse> CreateAsync(
        OrganizationActor actor,
        CreateOrganizationRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        if (!actor.CanManageAllOrganizations)
        {
            throw new ForbiddenException("Only a platform administrator can create organizations.");
        }

        ValidateCreateRequest(request);
        var name = request.Name!.Trim();
        var code = request.Code!.Trim();
        if (await store.CodeExistsAsync(code, cancellationToken: cancellationToken))
        {
            throw new ConflictException("An organization with this code already exists.");
        }

        var organization = new Organization(name, code);
        store.Add(organization);
        AddAudit(
            actor,
            organization,
            "Organization.Created",
            new { organization.Name, organization.Code },
            ipAddress);
        await store.SaveChangesAsync(cancellationToken);
        return ToResponse(organization);
    }

    public async Task<OrganizationResponse> GetAsync(
        OrganizationActor actor,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        return ToResponse(await FindOrganizationOrThrowAsync(actor, cancellationToken));
    }

    public async Task<OrganizationResponse> UpdateAsync(
        OrganizationActor actor,
        UpdateOrganizationRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ValidateUpdateRequest(request);

        var organization = await FindOrganizationOrThrowAsync(actor, cancellationToken);
        var name = request.Name!.Trim();
        var code = request.Code!.Trim();

        if (!string.Equals(organization.Code, code, StringComparison.OrdinalIgnoreCase) &&
            await store.CodeExistsAsync(code, organization.Id, cancellationToken))
        {
            throw new ConflictException("An organization with this code already exists.");
        }

        var previous = new { organization.Name, organization.Code };
        if (!organization.UpdateProfile(name, code, DateTimeOffset.UtcNow))
        {
            return ToResponse(organization);
        }

        AddAudit(
            actor,
            organization,
            "Organization.Updated",
            new
            {
                previous,
                current = new { organization.Name, organization.Code }
            },
            ipAddress);

        await store.SaveChangesAsync(cancellationToken);
        return ToResponse(organization);
    }

    public async Task<bool> ActivateAsync(
        OrganizationActor actor,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var organization = await FindOrganizationOrThrowAsync(actor, cancellationToken);
        if (!organization.Activate(DateTimeOffset.UtcNow))
        {
            return false;
        }

        AddAudit(actor, organization, "Organization.Activated", null, ipAddress);
        await store.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeactivateAsync(
        OrganizationActor actor,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var organization = await FindOrganizationOrThrowAsync(actor, cancellationToken);
        if (!organization.Deactivate(DateTimeOffset.UtcNow))
        {
            return false;
        }

        AddAudit(actor, organization, "Organization.Deactivated", null, ipAddress);
        await store.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<Organization> FindOrganizationOrThrowAsync(
        OrganizationActor actor,
        CancellationToken cancellationToken) =>
        await store.FindAsync(actor.OrganizationId, cancellationToken)
        ?? throw new ResourceNotFoundException("The organization was not found.");

    private void AddAudit(
        OrganizationActor actor,
        Organization organization,
        string action,
        object? details,
        string? ipAddress) =>
        store.AddAuditLog(new AuditLog(
            action,
            organization.Id,
            actor.UserId,
            entityType: "Organization",
            entityId: organization.Id,
            details: details is null ? null : JsonSerializer.SerializeToDocument(details),
            ipAddress: ipAddress));

    private static OrganizationResponse ToResponse(Organization organization) =>
        new(
            organization.Id,
            organization.Name,
            organization.Code,
            organization.IsActive,
            organization.CreatedAt,
            organization.UpdatedAt);

    private static void ValidateUpdateRequest(UpdateOrganizationRequest? request)
    {
        if (request is null)
        {
            throw Validation("request", "A request body is required.");
        }

        ValidateField(request.Name, "name", MaximumNameLength);
        ValidateField(request.Code, "code", MaximumCodeLength);
    }

    private static void ValidateCreateRequest(CreateOrganizationRequest? request)
    {
        if (request is null)
        {
            throw Validation("request", "A request body is required.");
        }

        ValidateField(request.Name, "name", MaximumNameLength);
        ValidateField(request.Code, "code", MaximumCodeLength);
    }

    private static void ValidateField(string? value, string fieldName, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw Validation(fieldName, $"{fieldName} is required.");
        }

        if (value.Trim().Length > maximumLength)
        {
            throw Validation(fieldName, $"{fieldName} must be {maximumLength} characters or fewer.");
        }
    }

    private static void ValidateActor(OrganizationActor actor)
    {
        if (actor.UserId == Guid.Empty || actor.OrganizationId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("The access token does not contain a valid organization scope.");
        }
    }

    private static ValidationException Validation(string fieldName, string message) =>
        new(
            "Validation failed",
            new Dictionary<string, string[]> { [fieldName] = [message] });
}
