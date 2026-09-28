namespace FarmManagement.Application.DTOs.LaborActivities;

public sealed record NamedReferenceResponse(Guid Id, string Name);

public sealed record StageReferenceResponse(Guid Id, string Name, int SequenceNumber);

public sealed record LaborActivityResponse(
    Guid Id,
    DateOnly ActivityDate,
    NamedReferenceResponse Farm,
    NamedReferenceResponse? FarmArea,
    NamedReferenceResponse? Plantation,
    NamedReferenceResponse? CropCycle,
    StageReferenceResponse? CropCycleStage,
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
    Guid? CropCycleStageId,
    Guid? LaborActivityTypeId,
    string? Description,
    string? Status = null);

public sealed record UpdateLaborActivityRequest(
    DateOnly? ActivityDate,
    Guid? FarmId,
    Guid? FarmAreaId,
    Guid? PlantationId,
    Guid? CropCycleId,
    Guid? CropCycleStageId,
    Guid? LaborActivityTypeId,
    string? Description,
    string? Status = null);

public sealed record CancelLaborActivityRequest(string? Reason);

public sealed record LaborActivityActor(Guid UserId, Guid OrganizationId);
