namespace FarmManagement.Application.DTOs.Inventory;

public sealed record CreateInventoryItemRequest(
    string Name,
    Guid StockUnitId,
    string? Sku = null,
    string? Description = null,
    string? Category = null);

public sealed record UpdateInventoryItemRequest(
    string Name,
    Guid StockUnitId,
    string? Sku = null,
    string? Description = null,
    string? Category = null);

public sealed record InventoryItemResponse(
    Guid Id,
    Guid OrganizationId,
    string Name,
    string? Sku,
    string? Description,
    string? Category,
    Guid StockUnitId,
    string StockUnitCode,
    string StockUnitName,
    string StockUnitSymbol,
    bool IsActive,
    DateTimeOffset CreatedAt,
    Guid CreatedBy,
    DateTimeOffset? UpdatedAt = null,
    Guid? UpdatedBy = null);
