using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.DTOs.Fertilizer;

public sealed record FertilizerStockConsumptionItem(
    Guid InventoryItemId,
    Guid StorageLocationId,
    decimal ActualQuantity);

public sealed record FertilizerStockConsumptionResult(
    bool IsReplay,
    IReadOnlyList<StockMovement> Movements,
    IReadOnlyList<FertilizerStockDeductionDetail> Deductions);

public sealed record FertilizerStockDeductionDetail(
    Guid InventoryItemId,
    Guid StorageLocationId,
    decimal DeductedQuantity,
    Guid StockMovementId);
