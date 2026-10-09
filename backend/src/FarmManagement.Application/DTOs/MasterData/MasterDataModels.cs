namespace FarmManagement.Application.DTOs.MasterData;

public sealed record UnitResponse(
    Guid Id,
    string Code,
    string Name,
    string Symbol,
    string UnitCategory,
    bool IsSystem,
    bool IsActive);

public sealed record FarmOwnershipTypeResponse(
    Guid Id,
    string Code,
    string Name,
    bool IsSystem,
    bool IsActive);

public sealed record PlantationEndReasonResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsSystem,
    bool IsActive);

public sealed record CurrencyResponse(
    Guid Id,
    string Code,
    string Name,
    string Symbol,
    bool IsSystem,
    bool IsActive,
    int DisplayOrder);

public sealed record ProductTypeResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    int DisplayOrder,
    bool IsSystem,
    bool IsActive);

public sealed record TargetResponse(
    Guid Id,
    string Code,
    string Name,
    string TargetType,
    string? Description,
    int DisplayOrder,
    bool IsSystem,
    bool IsActive);

public sealed record ApplicationMethodResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    int DisplayOrder,
    bool IsSystem,
    bool IsActive);

public sealed record CreateProductTypeRequest(
    string Code,
    string Name,
    string? Description = null,
    int DisplayOrder = 0);

public sealed record UpdateProductTypeRequest(
    string Name,
    string? Description = null,
    int DisplayOrder = 0);

public sealed record CreateTargetRequest(
    string Code,
    string Name,
    string TargetType,
    string? Description = null,
    int DisplayOrder = 0);

public sealed record UpdateTargetRequest(
    string Name,
    string TargetType,
    string? Description = null,
    int DisplayOrder = 0);

public sealed record CreateApplicationMethodRequest(
    string Code,
    string Name,
    string? Description = null,
    int DisplayOrder = 0);

public sealed record UpdateApplicationMethodRequest(
    string Name,
    string? Description = null,
    int DisplayOrder = 0);


