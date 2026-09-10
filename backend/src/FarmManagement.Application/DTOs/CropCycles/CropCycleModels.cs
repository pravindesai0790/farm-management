namespace FarmManagement.Application.DTOs.CropCycles;

public sealed record CropCycleResponse(
    Guid Id,
    Guid PlantationId,
    string PlantationName,
    string? FarmCode,
    string? FarmName,
    string? FarmAreaCode,
    string? FarmAreaName,
    string CropName,
    string CycleCode,
    string CycleName,
    int SeasonYear,
    string? SeasonName,
    DateOnly PlannedStartDate,
    DateOnly? ActualStartDate,
    DateOnly? ExpectedEndDate,
    string Status);

public sealed record CreateCropCycleRequest(
    Guid? PlantationId,
    string? CycleCode,
    string? CycleName,
    int? SeasonYear,
    string? SeasonName,
    DateOnly? PlannedStartDate,
    DateOnly? ExpectedEndDate);

public sealed record UpdateCropCycleRequest(
    Guid? PlantationId,
    string? CycleCode,
    string? CycleName,
    int? SeasonYear,
    string? SeasonName,
    DateOnly? PlannedStartDate,
    DateOnly? ExpectedEndDate);

public sealed record StartCropCycleRequest(DateOnly? StartDate);

public sealed record HarvestCropCycleRequest(DateOnly? HarvestDate);

public sealed record CompleteCropCycleRequest(DateOnly? CompletionDate);

public sealed record CancelCropCycleRequest(
    DateOnly? CancellationDate,
    Guid? CancellationReasonId,
    string? Notes);

