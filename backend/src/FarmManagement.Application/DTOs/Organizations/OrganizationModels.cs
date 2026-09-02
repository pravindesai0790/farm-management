namespace FarmManagement.Application.DTOs.Organizations;

public sealed record OrganizationResponse(
    Guid Id,
    string Name,
    string Code,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record CreateOrganizationRequest(
    string? Name,
    string? Code);

public sealed record UpdateOrganizationRequest(
    string? Name,
    string? Code);

public sealed record OrganizationListResponse(
    IReadOnlyList<OrganizationResponse> Items);
