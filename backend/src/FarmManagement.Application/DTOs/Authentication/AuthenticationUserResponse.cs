namespace FarmManagement.Application.DTOs.Authentication;

public sealed record AuthenticationUserResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    Guid OrganizationId,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);
