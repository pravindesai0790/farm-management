namespace FarmManagement.Application.DTOs.Plantations;

public sealed record PlantationResponse(
    Guid Id,
    Guid FarmId,
    Guid FarmAreaId,
    string FarmAreaCode,
    string FarmAreaName,
    Guid CropId,
    string CropCode,
    string CropName,
    Guid? VarietyId,
    string? VarietyCode,
    string? VarietyName,
    Guid? LifecycleTemplateId,
    string? LifecycleTemplateName,
    string PlantationCode,
    string PlantationName,
    decimal AllocatedArea,
    Guid AreaUnitId,
    string AreaUnitCode,
    string AreaUnitName,
    string AreaUnitSymbol,
    DateOnly PlantingDate,
    DateOnly? ExpectedEndDate,
    DateOnly? ActualEndDate,
    string Status,
    Guid? EndReasonId,
    string? EndReasonCode,
    string? EndReasonName,
    string? EndNotes,
    bool IsActive,
    DateTimeOffset CreatedAt,
    Guid CreatedBy,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedBy);

public sealed record CreatePlantationRequest(
    Guid? FarmAreaId,
    Guid? CropId,
    Guid? VarietyId,
    Guid? LifecycleTemplateId,
    string? PlantationCode,
    string? PlantationName,
    decimal? AllocatedArea,
    Guid? AreaUnitId,
    DateOnly? PlantingDate,
    DateOnly? ExpectedEndDate);

public sealed record UpdatePlantationRequest(
    Guid? FarmAreaId,
    Guid? CropId,
    Guid? VarietyId,
    Guid? LifecycleTemplateId,
    string? PlantationCode,
    string? PlantationName,
    decimal? AllocatedArea,
    Guid? AreaUnitId,
    DateOnly? PlantingDate,
    DateOnly? ExpectedEndDate);

public sealed record TerminatePlantationRequest(
    DateOnly? TerminationDate,
    Guid? EndReasonId,
    string? Notes,
    bool CancelActiveCycles = false);

public sealed record PlantationListResponse(
    IReadOnlyList<PlantationResponse> Items,
    int TotalCount);
