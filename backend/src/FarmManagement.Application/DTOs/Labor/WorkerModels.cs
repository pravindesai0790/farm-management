namespace FarmManagement.Application.DTOs.Labor;

public sealed record NamedReferenceResponse(Guid Id, string Name);

public sealed record WorkerResponse(
    Guid Id,
    Guid OrganizationId,
    string FirstName,
    string? LastName,
    string DisplayName,
    string Gender,
    string EmploymentType,
    string? MobileNumber,
    string? AlternateMobileNumber,
    Guid? LaborCategoryId,
    string? LaborCategoryName,
    Guid? ContractorId,
    string? ContractorName,
    DateOnly? JoiningDate,
    DateOnly? LeavingDate,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record WorkerDetailResponse(
    Guid Id,
    Guid OrganizationId,
    string FirstName,
    string? LastName,
    string DisplayName,
    string Gender,
    string EmploymentType,
    string? MobileNumber,
    string? AlternateMobileNumber,
    Guid? LaborCategoryId,
    string? LaborCategoryName,
    Guid? ContractorId,
    string? ContractorName,
    DateOnly? JoiningDate,
    DateOnly? LeavingDate,
    string? Notes,
    bool IsActive,
    DateTimeOffset CreatedAt,
    Guid CreatedBy,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedBy);

public sealed record CreateWorkerRequest(
    string? FirstName,
    string? LastName,
    string? DisplayName,
    string? Gender,
    string? EmploymentType,
    string? MobileNumber,
    string? AlternateMobileNumber,
    Guid? LaborCategoryId,
    Guid? ContractorId,
    DateOnly? JoiningDate,
    DateOnly? LeavingDate,
    string? Notes);

public sealed record UpdateWorkerRequest(
    string? FirstName,
    string? LastName,
    string? DisplayName,
    string? Gender,
    string? EmploymentType,
    string? MobileNumber,
    string? AlternateMobileNumber,
    Guid? LaborCategoryId,
    Guid? ContractorId,
    DateOnly? JoiningDate,
    DateOnly? LeavingDate,
    string? Notes);

public sealed record WorkerActor(Guid UserId, Guid OrganizationId);
