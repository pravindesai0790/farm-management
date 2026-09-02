namespace FarmManagement.Application.DTOs.Farms;

public sealed record FarmAreaResponse(
    Guid Id,
    Guid FarmId,
    Guid? ParentFarmAreaId,
    string Code,
    string Name,
    string? Description,
    decimal TotalArea,
    Guid AreaUnitId,
    string AreaUnitCode,
    string AreaUnitName,
    string AreaUnitSymbol,
    bool IsActive,
    DateTimeOffset CreatedAt,
    Guid CreatedBy,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedBy);

public sealed record CreateFarmAreaRequest(
    Guid? FarmId,
    Guid? ParentFarmAreaId,
    string? Code,
    string? Name,
    string? Description,
    decimal? TotalArea,
    Guid? AreaUnitId);

public sealed record UpdateFarmAreaRequest(
    Guid? ParentFarmAreaId,
    string? Code,
    string? Name,
    string? Description,
    decimal? TotalArea,
    Guid? AreaUnitId);

public sealed record FarmAreaAvailabilityResponse(
    Guid FarmAreaId,
    decimal TotalArea,
    decimal AllocatedArea,
    decimal AvailableArea,
    string Unit);
