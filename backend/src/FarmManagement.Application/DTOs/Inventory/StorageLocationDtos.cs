namespace FarmManagement.Application.DTOs.Inventory;

public sealed record CreateStorageLocationRequest(
    Guid FarmId,
    string Name,
    string? Description = null);

public sealed record UpdateStorageLocationRequest(
    string Name,
    string? Description = null);

public sealed record StorageLocationResponse(
    Guid Id,
    Guid OrganizationId,
    Guid FarmId,
    string FarmName,
    string Name,
    string? Description,
    bool IsActive,
    DateTimeOffset CreatedAt,
    Guid CreatedBy,
    DateTimeOffset? UpdatedAt = null,
    Guid? UpdatedBy = null);
