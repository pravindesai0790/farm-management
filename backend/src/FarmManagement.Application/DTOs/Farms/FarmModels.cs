namespace FarmManagement.Application.DTOs.Farms;

public sealed record FarmResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    Guid OwnershipTypeId,
    string OwnershipTypeCode,
    string OwnershipTypeName,
    decimal? TotalArea,
    Guid? AreaUnitId,
    string? AreaUnitCode,
    string? AreaUnitName,
    string? AreaUnitSymbol,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? District,
    string? State,
    string? Country,
    string? PostalCode,
    decimal? Latitude,
    decimal? Longitude,
    bool IsActive,
    DateTimeOffset CreatedAt,
    Guid CreatedBy,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedBy);

public sealed record CreateFarmRequest(
    string? Code,
    string? Name,
    string? Description,
    Guid? OwnershipTypeId,
    decimal? TotalArea,
    Guid? AreaUnitId,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? District,
    string? State,
    string? Country,
    string? PostalCode,
    decimal? Latitude,
    decimal? Longitude);

public sealed record UpdateFarmRequest(
    string? Code,
    string? Name,
    string? Description,
    Guid? OwnershipTypeId,
    decimal? TotalArea,
    Guid? AreaUnitId,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? District,
    string? State,
    string? Country,
    string? PostalCode,
    decimal? Latitude,
    decimal? Longitude);

public sealed record FarmListResponse(
    IReadOnlyList<FarmResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
