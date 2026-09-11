namespace FarmManagement.Application.DTOs.Labor;

public sealed record WorkerFarmAssignmentResponse(
    Guid Id,
    Guid OrganizationId,
    Guid WorkerId,
    string WorkerName,
    Guid FarmId,
    string FarmCode,
    string FarmName,
    DateOnly AssignedFrom,
    DateOnly? AssignedTo,
    bool IsActive,
    string? Notes,
    DateTimeOffset CreatedAt,
    Guid CreatedBy,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedBy);

public sealed record CreateWorkerFarmAssignmentRequest(
    Guid? FarmId,
    DateOnly? AssignedFrom,
    DateOnly? AssignedTo,
    string? Notes);

public sealed record UpdateWorkerFarmAssignmentRequest(
    DateOnly? AssignedFrom,
    DateOnly? AssignedTo,
    string? Notes);

public sealed record EndWorkerFarmAssignmentRequest(
    DateOnly? EndDate = null,
    string? Notes = null);
