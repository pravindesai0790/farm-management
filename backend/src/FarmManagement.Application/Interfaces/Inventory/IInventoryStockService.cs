using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Inventory;
using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.Interfaces.Inventory;

public interface IInventoryStockService
{
    Task<PagedResponse<StockBalanceResponse>> GetOverviewAsync(
        InventoryActor actor,
        int page,
        int pageSize,
        Guid? farmId,
        Guid? storageLocationId,
        Guid? inventoryItemId,
        CancellationToken cancellationToken = default);

    Task<PagedResponse<StockMovementResponse>> GetLedgerAsync(
        InventoryActor actor,
        int page,
        int pageSize,
        Guid? farmId,
        Guid? storageLocationId,
        Guid? inventoryItemId,
        StockMovementType? movementType,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken = default);

    Task<StockMovementResponse> RecordOpeningStockAsync(
        InventoryActor actor,
        RecordOpeningStockRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<StockMovementResponse> RecordStockReceiptAsync(
        InventoryActor actor,
        RecordStockReceiptRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<StockMovementResponse> RecordStockIssueAsync(
        InventoryActor actor,
        RecordStockIssueRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<StockMovementResponse> RecordStockAdjustmentAsync(
        InventoryActor actor,
        RecordStockAdjustmentRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockMovementResponse>> RecordStockTransferAsync(
        InventoryActor actor,
        RecordStockTransferRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);
}
