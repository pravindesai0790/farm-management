namespace FarmManagement.Application.DTOs.PlantProtection;

public sealed record CreatePlantProtectionProductRequest(
    Guid InventoryItemId,
    Guid ProductTypeId,
    string? ActiveIngredient = null,
    string? Manufacturer = null,
    string? Description = null);

public sealed record UpdatePlantProtectionProductRequest(
    Guid ProductTypeId,
    string? ActiveIngredient = null,
    string? Manufacturer = null,
    string? Description = null);

public sealed record PlantProtectionProductResponse(
    Guid Id,
    Guid OrganizationId,
    Guid InventoryItemId,
    string InventoryItemName,
    string? InventoryItemSku,
    Guid StockUnitId,
    string StockUnitCode,
    string StockUnitName,
    string StockUnitSymbol,
    Guid ProductTypeId,
    string ProductTypeCode,
    string ProductTypeName,
    string? ActiveIngredient,
    string? Manufacturer,
    string? Description,
    bool IsActive,
    bool HasCompletedSprayUsage,
    DateTimeOffset CreatedAt,
    Guid CreatedBy,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedBy);
