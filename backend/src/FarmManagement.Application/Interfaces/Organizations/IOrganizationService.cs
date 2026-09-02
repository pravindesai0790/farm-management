using FarmManagement.Application.DTOs.Organizations;

namespace FarmManagement.Application.Interfaces.Organizations;

public sealed record OrganizationActor(
    Guid UserId,
    Guid OrganizationId,
    bool CanManageAllOrganizations);

public interface IOrganizationService
{
    Task<OrganizationResponse> CreateAsync(
        OrganizationActor actor,
        CreateOrganizationRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<OrganizationResponse> GetAsync(
        OrganizationActor actor,
        CancellationToken cancellationToken = default);

    Task<OrganizationResponse> UpdateAsync(
        OrganizationActor actor,
        UpdateOrganizationRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<bool> ActivateAsync(
        OrganizationActor actor,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(
        OrganizationActor actor,
        string? ipAddress,
        CancellationToken cancellationToken = default);
}
