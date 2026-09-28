namespace FarmManagement.Application.DTOs.CropCycles;

public sealed record CropCycleResponse(
    Guid Id,
    Guid PlantationId,
    string PlantationName,
    string? FarmName,
    string? FarmAreaName,
    Guid CropId,
    string CropName,
    string CycleName,
    int SeasonYear,
    string? SeasonName,
    DateOnly PlannedStartDate,
    DateOnly? ActualStartDate,
    DateOnly? ExpectedEndDate,
    string Status,
    Guid? LifecycleTemplateId = null,
    string? LifecycleTemplateName = null,
    string? CropDurationType = null);

public sealed record CompleteCropCycleResponse(
    Guid CycleId,
    Guid PlantationId,
    string PlantationName,
    Guid CropId,
    string CropName,
    string CropDurationType,
    bool RequiresPlantationTerminationPrompt);

public sealed record CreateCropCycleRequest(
    Guid? PlantationId,
    string? CycleName,
    int? SeasonYear,
    string? SeasonName,
    DateOnly? PlannedStartDate,
    DateOnly? ExpectedEndDate,
    Guid? LifecycleTemplateId = null);

public sealed record UpdateCropCycleRequest(
    Guid? PlantationId,
    string? CycleName,
    int? SeasonYear,
    string? SeasonName,
    DateOnly? PlannedStartDate,
    DateOnly? ExpectedEndDate,
    Guid? LifecycleTemplateId = null);

public sealed record StartCropCycleRequest(DateOnly? StartDate);

public sealed record HarvestCropCycleRequest(DateOnly? HarvestDate);

public sealed record CompleteCropCycleRequest(DateOnly? CompletionDate);

public sealed record CancelCropCycleRequest(
    DateOnly? CancellationDate,
    Guid? CancellationReasonId,
    string? Notes);

public sealed record CropCycleLifecycleResponse(
    Guid CropCycleId,
    string CycleName,
    string OverallStatus,
    Guid? LifecycleTemplateId,
    string? LifecycleTemplateName,
    string? CurrentStageName,
    int? CurrentStageSequence,
    int TotalStagesCount,
    int CompletedStagesCount,
    int ProgressPercentage,
    bool HasGeneratedStages,
    IReadOnlyList<CropCycleStageResponse> Stages);

public sealed record CropCycleStageResponse(
    Guid Id,
    Guid CropCycleId,
    Guid LifecycleTemplateStageId,
    string StageName,
    int SequenceNumber,
    int? ExpectedDurationDays,
    DateOnly? PlannedStartDate,
    DateOnly? PlannedEndDate,
    DateOnly? ActualStartDate,
    DateOnly? ActualEndDate,
    string Status,
    string? Notes);

public sealed record CompleteCropCycleStageRequest(
    DateOnly? ActualEndDate = null,
    string? Notes = null);

public sealed record SkipCropCycleStageRequest(
    string? Reason,
    DateOnly? SkipDate = null);

public sealed record ReopenCropCycleStageRequest(
    string? Reason);

public sealed record OverrideCropCycleStageRequest(
    string? TargetStatus,
    DateOnly? ActualStartDate,
    DateOnly? ActualEndDate,
    string? Reason);

public sealed record UpdateCropCycleStagePlannedDatesRequest(
    DateOnly? PlannedStartDate,
    DateOnly? PlannedEndDate);


