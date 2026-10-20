using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.DTOs.Fertilizer;

public sealed record FertilizerApplicationListQuery(
    Guid? FarmId = null,
    Guid? FarmAreaId = null,
    Guid? PlantationId = null,
    Guid? CropCycleId = null,
    Guid? CropCycleStageId = null,
    FertilizerApplicationStatus? Status = null,
    Guid? InventoryItemId = null,
    string? DateType = null, // "CreatedDate", "PlannedDate", "ScheduledDate", "ActualApplicationDate" (default "CreatedDate")
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    string? Search = null,
    int PageNumber = 1,
    int PageSize = 20,
    string? SortBy = null,
    string? SortDirection = null);

public sealed record FertilizerApplicationListItemResponse(
    Guid Id,
    string ReferenceNumber,
    Guid? FarmId,
    string? FarmName,
    Guid? FarmAreaId,
    string? FarmAreaName,
    Guid? PlantationId,
    string? PlantationName,
    Guid? CropCycleId,
    string? CropCycleName,
    Guid? CropCycleStageId,
    string? CropCycleStageName,
    FertilizerApplicationStatus Status,
    string StatusName,
    Guid? ApplicationMethodId,
    string? ApplicationMethodName,
    string? OtherMethodDescription,
    Guid? IrrigationEventId,
    DateOnly? PlannedDate,
    DateOnly? ScheduledDate,
    DateOnly? ActualApplicationDate,
    decimal? PlannedArea,
    Guid? PlannedAreaUnitId,
    string? PlannedAreaUnitName,
    decimal? ActualArea,
    Guid? ActualAreaUnitId,
    string? ActualAreaUnitName,
    string? PurposeReason,
    int ProductCount,
    IReadOnlyList<FertilizerApplicationItemSummaryDto> ProductsSummary,
    DateTimeOffset CreatedAt,
    Guid CreatedBy,
    string? CreatedByName = null);

public sealed record FertilizerApplicationItemSummaryDto(
    Guid InventoryItemId,
    string ItemName,
    string? ItemSku,
    decimal? PlannedQuantity,
    decimal? ActualQuantity,
    string UnitSymbol);

public sealed record FertilizerApplicationProductDto(
    Guid Id,
    Guid FertilizerApplicationId,
    Guid InventoryItemId,
    string InventoryItemName,
    string? InventoryItemSku,
    Guid StockUnitId,
    string StockUnitName,
    string? StockUnitCode,
    string? StockUnitSymbol,
    Guid? StorageLocationId,
    string? StorageLocationName,
    decimal? PlannedQuantity,
    decimal? ActualQuantity,
    decimal? VarianceQuantity,
    Guid? StockMovementId,
    string? StockMovementReference = null);

public sealed record FertilizerApplicationDetailsResponse(
    Guid Id,
    Guid OrganizationId,
    string ReferenceNumber,
    FertilizerApplicationStatus Status,
    string StatusName,
    Guid? FarmId,
    string? FarmName,
    Guid? FarmAreaId,
    string? FarmAreaName,
    Guid? PlantationId,
    string? PlantationName,
    Guid? CropCycleId,
    string? CropCycleName,
    Guid? CropCycleStageId,
    string? CropCycleStageName,
    Guid? ApplicationMethodId,
    string? ApplicationMethodName,
    string? OtherMethodDescription,
    Guid? IrrigationEventId,
    string? IrrigationEventReference,
    DateOnly? PlannedDate,
    DateOnly? ScheduledDate,
    DateOnly? ActualApplicationDate,
    decimal? PlannedArea,
    Guid? PlannedAreaUnitId,
    string? PlannedAreaUnitName,
    decimal? ActualArea,
    Guid? ActualAreaUnitId,
    string? ActualAreaUnitName,
    string? PurposeReason,
    string? CancellationReason,
    IReadOnlyList<FertilizerApplicationProductDto> Products,
    DateTimeOffset CreatedAt,
    Guid CreatedBy,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedBy,
    DateTimeOffset? StartedAt,
    Guid? StartedBy,
    DateTimeOffset? CompletedAt,
    Guid? CompletedBy,
    DateTimeOffset? CancelledAt,
    Guid? CancelledBy,
    string? CreatedByName = null,
    string? UpdatedByName = null,
    string? StartedByName = null,
    string? CompletedByName = null,
    string? CancelledByName = null,
    uint Version = 1,
    string? ConcurrencyToken = null);

public sealed record FertilizerProductLookupResponse(
    Guid InventoryItemId,
    string ItemName,
    string? Sku,
    Guid StockUnitId,
    string StockUnitName,
    string? StockUnitCode,
    string? StockUnitSymbol,
    Guid? CategoryId,
    string? CategoryName);

public sealed record FertilizerStorageLocationLookupResponse(
    Guid StorageLocationId,
    string StorageLocationName,
    decimal CurrentStock,
    bool HasStock,
    Guid? StockUnitId,
    string? StockUnitName,
    string? StockUnitSymbol);

public sealed record FertilizerApplicationMethodLookupResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    int DisplayOrder,
    bool RequiresDescription);

public sealed record FertilizerIrrigationLookupResponse(
    Guid Id,
    Guid FarmId,
    Guid FarmAreaId,
    string FarmAreaName,
    Guid? CropCycleId,
    string? CropCycleName,
    string Status,
    DateTimeOffset? PlannedAt,
    DateTimeOffset? ScheduledAt,
    DateTimeOffset? ActualStartedAt,
    DateTimeOffset? ActualEndedAt,
    string? IrrigationMethodName,
    decimal? ActualWaterQuantity,
    string? WaterUnitSymbol);

public sealed record CreateFertilizerDraftRequest(
    Guid? FarmId = null,
    Guid? FarmAreaId = null,
    Guid? PlantationId = null,
    Guid? CropCycleId = null,
    Guid? CropCycleStageId = null,
    Guid? ApplicationMethodId = null,
    string? OtherMethodDescription = null,
    Guid? IrrigationEventId = null,
    DateOnly? PlannedDate = null,
    DateOnly? ScheduledDate = null,
    decimal? PlannedArea = null,
    Guid? PlannedAreaUnitId = null,
    string? PurposeReason = null,
    IReadOnlyList<FertilizerDraftProductLineRequest>? Products = null);

public sealed record UpdateFertilizerDraftRequest(
    Guid? FarmId = null,
    Guid? FarmAreaId = null,
    Guid? PlantationId = null,
    Guid? CropCycleId = null,
    Guid? CropCycleStageId = null,
    Guid? ApplicationMethodId = null,
    string? OtherMethodDescription = null,
    Guid? IrrigationEventId = null,
    DateOnly? PlannedDate = null,
    DateOnly? ScheduledDate = null,
    decimal? PlannedArea = null,
    Guid? PlannedAreaUnitId = null,
    string? PurposeReason = null,
    uint? Version = null,
    IReadOnlyList<FertilizerDraftProductLineRequest>? Products = null);

public sealed record FertilizerDraftProductLineRequest(
    Guid InventoryItemId,
    decimal PlannedQuantity = 0m,
    Guid? StorageLocationId = null);

public sealed record PlanFertilizerApplicationRequest(
    DateOnly? PlannedDate = null,
    DateOnly? ScheduledDate = null,
    Guid? FarmId = null,
    Guid? FarmAreaId = null,
    Guid? PlantationId = null,
    Guid? CropCycleId = null,
    Guid? CropCycleStageId = null,
    Guid? ApplicationMethodId = null,
    string? OtherMethodDescription = null,
    Guid? IrrigationEventId = null,
    decimal? PlannedArea = null,
    Guid? PlannedAreaUnitId = null,
    string? PurposeReason = null,
    uint? Version = null,
    IReadOnlyList<FertilizerDraftProductLineRequest>? Products = null);

public sealed record ScheduleFertilizerApplicationRequest(
    DateOnly ScheduledDate,
    uint? Version = null);

public sealed record RescheduleFertilizerApplicationRequest(
    DateOnly ScheduledDate,
    uint? Version = null);

public sealed record StartFertilizerApplicationRequest(
    uint? Version = null,
    string? IdempotencyKey = null);

public sealed record CompleteFertilizerApplicationRequest(
    DateOnly ActualApplicationDate,
    decimal? ActualArea = null,
    Guid? ActualAreaUnitId = null,
    uint? Version = null,
    string? IdempotencyKey = null,
    bool AcknowledgeVariance = false,
    IReadOnlyList<CompleteFertilizerApplicationProductLineRequest>? Products = null);

public sealed record CompleteFertilizerApplicationProductLineRequest(
    Guid InventoryItemId,
    decimal ActualQuantity);

public sealed record CancelFertilizerApplicationRequest(
    string CancellationReason,
    DateOnly? ActualApplicationDate = null,
    uint? Version = null,
    string? IdempotencyKey = null,
    IReadOnlyList<CancelFertilizerApplicationProductLineRequest>? Products = null);

public sealed record CancelFertilizerApplicationProductLineRequest(
    Guid InventoryItemId,
    decimal ActualQuantity,
    Guid? StorageLocationId = null);

public sealed record FertilizerStockShortageDto(
    Guid InventoryItemId,
    string ItemName,
    string? Sku,
    Guid StorageLocationId,
    string StorageLocationName,
    decimal RequiredQuantity,
    decimal AvailableQuantity,
    decimal ShortageQuantity,
    string UnitSymbol);
