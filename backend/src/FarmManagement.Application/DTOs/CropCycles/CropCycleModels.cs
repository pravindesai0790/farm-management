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
    string? LifecycleTemplateName = null);

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

