namespace FarmManagement.Application.DTOs.LaborActivities;

public sealed record NamedReferenceResponse(Guid Id, string Name);

public sealed record LaborActivityResponse(
    Guid Id,
    DateOnly ActivityDate,
    NamedReferenceResponse Farm,
    NamedReferenceResponse? FarmArea,
    NamedReferenceResponse? Plantation,
    NamedReferenceResponse? CropCycle,
    NamedReferenceResponse ActivityType,
    string Status,
    string? Description,
    string? CancellationReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record CreateLaborActivityRequest(
    DateOnly? ActivityDate,
    Guid? FarmId,
    Guid? FarmAreaId,
    Guid? PlantationId,
    Guid? CropCycleId,
    Guid? LaborActivityTypeId,
    string? Description,
    int? WorkerCount,
    decimal? TotalWorkingHours,
    decimal? CostAmount,
    string? Status);

public sealed record UpdateLaborActivityRequest(
    DateOnly? ActivityDate,
    Guid? FarmId,
    Guid? FarmAreaId,
    Guid? PlantationId,
    Guid? CropCycleId,
    Guid? LaborActivityTypeId,
    string? Description,
    int? WorkerCount,
    decimal? TotalWorkingHours,
    decimal? CostAmount,
    string? Status);

public sealed record CancelLaborActivityRequest(string? Reason);

public sealed record LaborActivityActor(Guid UserId, Guid OrganizationId);
