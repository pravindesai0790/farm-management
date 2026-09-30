using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.DTOs.Inventory;

public sealed record StockBalanceResponse(
    Guid Id,
    Guid OrganizationId,
    Guid FarmId,
    string FarmName,
    Guid StorageLocationId,
    string StorageLocationName,
    Guid InventoryItemId,
    string InventoryItemName,
    string? InventoryItemSku,
    string? InventoryItemCategory,
    Guid StockUnitId,
    string StockUnitCode,
    string StockUnitName,
    string StockUnitSymbol,
    decimal QuantityOnHand,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt = null);

public sealed record StockMovementResponse(
    Guid Id,
    Guid OrganizationId,
    StockMovementType MovementType,
    string MovementTypeName,
    Guid InventoryItemId,
    string InventoryItemName,
    string? InventoryItemSku,
    Guid FarmId,
    string FarmName,
    Guid StorageLocationId,
    string StorageLocationName,
    decimal Quantity,
    Guid StockUnitId,
    string StockUnitCode,
    string StockUnitSymbol,
    DateOnly MovementDate,
    string? ReferenceNumber,
    string? Notes,
    Guid? ParentTransactionId,
    DateTimeOffset CreatedAt,
    Guid CreatedBy,
    Guid? CropCycleId = null,
    string? CropCycleTitle = null,
    Guid? CropCycleStageId = null,
    string? CropCycleStageName = null,
    Guid? PlantationId = null,
    string? PlantationName = null,
    Guid? FarmAreaId = null,
    string? FarmAreaName = null,
    Guid? LaborActivityId = null,
    string? LaborActivityTypeName = null,
    bool IsReversed = false,
    Guid? ReversalMovementId = null,
    Guid? ReversedMovementId = null,
    string? ReversalReason = null,
    string? InventoryItemCategory = null);

public sealed record RecordOpeningStockRequest(
    Guid FarmId,
    Guid StorageLocationId,
    Guid InventoryItemId,
    decimal Quantity,
    DateOnly MovementDate,
    string? Notes = null);

public sealed record RecordStockReceiptRequest(
    Guid FarmId,
    Guid StorageLocationId,
    Guid InventoryItemId,
    decimal Quantity,
    DateOnly MovementDate,
    string? ReferenceNumber = null,
    string? Notes = null);

public sealed record RecordStockIssueRequest(
    Guid FarmId,
    Guid StorageLocationId,
    Guid InventoryItemId,
    decimal Quantity,
    DateOnly MovementDate,
    string? ReferenceNumber = null,
    string? PurposeNotes = null,
    Guid? CropCycleId = null,
    Guid? CropCycleStageId = null,
    Guid? PlantationId = null,
    Guid? FarmAreaId = null,
    Guid? LaborActivityId = null);

public sealed record RecordStockAdjustmentRequest(
    Guid FarmId,
    Guid StorageLocationId,
    Guid InventoryItemId,
    StockMovementType AdjustmentType, // AdjustmentIn or AdjustmentOut
    decimal Quantity,
    DateOnly MovementDate,
    string ReasonNotes);

public sealed record RecordStockTransferRequest(
    Guid SourceFarmId,
    Guid SourceStorageLocationId,
    Guid DestinationFarmId,
    Guid DestinationStorageLocationId,
    Guid InventoryItemId,
    decimal Quantity,
    DateOnly MovementDate,
    string? ReferenceNumber = null,
    string? Notes = null);

public sealed record ReverseStockMovementRequest(
    string Reason,
    DateOnly? ReversalDate = null);
