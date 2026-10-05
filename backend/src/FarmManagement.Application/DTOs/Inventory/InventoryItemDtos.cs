namespace FarmManagement.Application.DTOs.Inventory;

public sealed record CreateInventoryItemRequest(
    string Name,
    Guid StockUnitId,
    Guid? CategoryId = null,
    string? Sku = null,
    string? Description = null);

public sealed record UpdateInventoryItemRequest(
    string Name,
    Guid StockUnitId,
    Guid? CategoryId = null,
    string? Sku = null,
    string? Description = null);

public sealed record InventoryItemResponse(
    Guid Id,
    Guid OrganizationId,
    string Name,
    string? Sku,
    string? Description,
    Guid? CategoryId,
    string? CategoryName,
    string? CategoryCode,
    string? CategoryIcon,
    Guid StockUnitId,
    string StockUnitCode,
    string StockUnitName,
    string StockUnitSymbol,
    bool IsActive,
    DateTimeOffset CreatedAt,
    Guid CreatedBy,
    DateTimeOffset? UpdatedAt = null,
    Guid? UpdatedBy = null);

public sealed record InventoryCategoryResponse(
    Guid Id,
    string Name,
    string Code,
    string Description,
    string Examples,
    string Icon,
    int DisplayOrder,
    bool IsSystem = true,
    bool IsActive = true);
