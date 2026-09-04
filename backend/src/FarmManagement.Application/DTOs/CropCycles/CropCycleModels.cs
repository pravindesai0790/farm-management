namespace FarmManagement.Application.DTOs.CropCycles;

public sealed record CropCycleResponse(
    Guid Id,
    Guid PlantationId,
    string PlantationCode,
    string PlantationName,
    Guid FarmId,
    string FarmCode,
    string FarmName,
    Guid FarmAreaId,
    string FarmAreaCode,
    string FarmAreaName,
    Guid CropId,
    string CropCode,
    string CropName,
    string CropDurationType,
    string CycleCode,
    string CycleName,
    int SeasonYear,
    string? SeasonName,
    DateOnly PlannedStartDate,
    DateOnly? ActualStartDate,
    DateOnly? ExpectedEndDate,
    DateOnly? ActualEndDate,
    string Status,
    Guid? CancellationReasonId,
    string? CancellationReasonCode,
    string? CancellationReasonName,
    string? CancellationNotes,
    DateTimeOffset CreatedAt,
    Guid CreatedBy,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedBy);

public sealed record CreateCropCycleRequest(
    Guid? PlantationId,
    string? CycleCode,
    string? CycleName,
    int? SeasonYear,
    string? SeasonName,
    DateOnly? PlannedStartDate,
    DateOnly? ExpectedEndDate);

public sealed record UpdateCropCycleRequest(
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

public sealed record CropCycleListResponse(
    IReadOnlyList<CropCycleResponse> Items,
    int TotalCount);
