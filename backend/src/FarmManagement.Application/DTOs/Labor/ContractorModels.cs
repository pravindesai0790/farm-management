namespace FarmManagement.Application.DTOs.Labor;

public sealed record ContractorResponse(
    Guid Id,
    Guid OrganizationId,
    string Name,
    string? ContactPerson,
    string? PhoneNumber,
    string? Email,
    string? Address,
    string? Notes,
    bool IsActive,
    int WorkerCount,
    DateTimeOffset CreatedAt,
    Guid CreatedBy,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedBy);

public sealed record CreateContractorRequest(
    string? Name,
    string? ContactPerson,
    string? PhoneNumber,
    string? Email,
    string? Address,
    string? Notes);

public sealed record UpdateContractorRequest(
    string? Name,
    string? ContactPerson,
    string? PhoneNumber,
    string? Email,
    string? Address,
    string? Notes);

public sealed record ContractorActor(Guid UserId, Guid OrganizationId);
