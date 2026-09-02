namespace FarmManagement.Application.DTOs.Crops;

public sealed record CropResponse(
    Guid Id,
    Guid? OrganizationId,
    string Code,
    string Name,
    string? ScientificName,
    string CropType,
    string CropDurationType,
    string? Description,
    bool IsSystem,
    bool IsActive,
    DateTimeOffset CreatedAt,
    Guid? CreatedBy,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedBy);

public sealed record CreateCropRequest(
    string? Code,
    string? Name,
    string? ScientificName,
    string? CropType,
    string? CropDurationType,
    string? Description);

public sealed record UpdateCropRequest(
    string? Code,
    string? Name,
    string? ScientificName,
    string? CropType,
    string? CropDurationType,
    string? Description);

public sealed record CropListResponse(
    IReadOnlyList<CropResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record CropVarietyResponse(
    Guid Id,
    Guid? OrganizationId,
    Guid CropId,
    string CropCode,
    string CropName,
    string Code,
    string Name,
    string? Description,
    bool IsSystem,
    bool IsActive,
    DateTimeOffset CreatedAt,
    Guid? CreatedBy,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedBy);

public sealed record CreateCropVarietyRequest(
    Guid? CropId,
    string? Code,
    string? Name,
    string? Description);

public sealed record UpdateCropVarietyRequest(
    Guid? CropId,
    string? Code,
    string? Name,
    string? Description);

public sealed record CropVarietyListResponse(
    IReadOnlyList<CropVarietyResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
