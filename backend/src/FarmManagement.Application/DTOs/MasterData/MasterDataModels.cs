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
