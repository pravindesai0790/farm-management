namespace FarmManagement.Application.DTOs.Labor;

public sealed record AttendanceActor(Guid UserId, Guid OrganizationId);

public sealed record AttendanceEligibleWorkerResponse(
    Guid WorkerId,
    string DisplayName,
    string FirstName,
    string? LastName,
    string Gender,
    string EmploymentType,
    string? MobileNumber,
    Guid? LaborCategoryId,
    string? LaborCategoryName,
    Guid? ContractorId,
    string? ContractorName,
    DateOnly? JoiningDate,
    DateOnly? LeavingDate,
    Guid AssignmentId,
    Guid FarmId,
    DateOnly AssignedFrom,
    DateOnly? AssignedTo);
