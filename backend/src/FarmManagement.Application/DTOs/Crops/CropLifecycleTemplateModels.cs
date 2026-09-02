namespace FarmManagement.Application.DTOs.Crops;

public sealed record CropLifecycleStageResponse(
    Guid Id,
    Guid LifecycleTemplateId,
    string StageCode,
    string StageName,
    int SequenceNumber,
    string? Description,
    bool IsActive);

public sealed record CropLifecycleTemplateResponse(
    Guid Id,
    Guid? OrganizationId,
    Guid CropId,
    string CropCode,
    string CropName,
    string Name,
    string? Description,
    bool IsDefault,
    bool IsSystem,
    bool IsActive,
    DateTimeOffset CreatedAt,
    Guid? CreatedBy,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedBy,
    IReadOnlyList<CropLifecycleStageResponse> Stages);

public sealed record CreateCropLifecycleTemplateRequest(
    Guid? CropId,
    string? Name,
    string? Description,
    bool IsDefault = false);

public sealed record UpdateCropLifecycleTemplateRequest(
    Guid? CropId,
    string? Name,
    string? Description,
    bool IsDefault = false);

public sealed record CropLifecycleTemplateListResponse(
    IReadOnlyList<CropLifecycleTemplateResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record CreateCropLifecycleStageRequest(
    string? StageCode,
    string? StageName,
    int SequenceNumber,
    string? Description);

public sealed record UpdateCropLifecycleStageRequest(
    string? StageCode,
    string? StageName,
    int SequenceNumber,
    string? Description);
