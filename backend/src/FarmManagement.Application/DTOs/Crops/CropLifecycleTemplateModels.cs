namespace FarmManagement.Application.DTOs.Crops;

public sealed record CropLifecycleStageResponse(
    Guid Id,
    Guid LifecycleTemplateId,
    string StageName,
    int SequenceNumber,
    int? ExpectedDurationDays,
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

public sealed record CreateCropLifecycleStageRequest(
    string? StageName,
    int SequenceNumber,
    int? ExpectedDurationDays,
    string? Description);

public sealed record UpdateCropLifecycleStageRequest(
    string? StageName,
    int SequenceNumber,
    int? ExpectedDurationDays,
    string? Description);
