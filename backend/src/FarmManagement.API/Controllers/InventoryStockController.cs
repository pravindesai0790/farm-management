using FarmManagement.API.Helpers;
using FarmManagement.Application.DTOs.Inventory;
using FarmManagement.Application.Interfaces.Inventory;
using FarmManagement.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmManagement.API.Controllers;

[ApiController]
[Route("api/inventory/stock")]
[Authorize]
public sealed class InventoryStockController(IInventoryStockService stockService) : ControllerBase
{
    [HttpGet("overview")]
    [Authorize(Policy = "Permission:InventoryStock.View")]
    public async Task<IActionResult> GetOverview(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? farmId = null,
        [FromQuery] Guid? storageLocationId = null,
        [FromQuery] Guid? inventoryItemId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await stockService.GetOverviewAsync(
            GetUserContext(),
            page,
            pageSize,
            farmId,
            storageLocationId,
            inventoryItemId,
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Gets the current on-hand stock balance for a specific storage location and inventory item.
    /// </summary>
    [HttpGet("balance")]
    [Authorize(Policy = "Permission:InventoryStock.View")]
    public async Task<IActionResult> GetBalance(
        [FromQuery] Guid storageLocationId,
        [FromQuery] Guid inventoryItemId,
        CancellationToken cancellationToken = default)
    {
        var result = await stockService.GetBalanceAsync(
            GetUserContext(),
            storageLocationId,
            inventoryItemId,
            cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("ledger")]
    [Authorize(Policy = "Permission:InventoryStock.View")]
    public async Task<IActionResult> GetLedger(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? farmId = null,
        [FromQuery] Guid? storageLocationId = null,
        [FromQuery] Guid? inventoryItemId = null,
        [FromQuery] StockMovementType? movementType = null,
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        [FromQuery] Guid? cropCycleId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await stockService.GetLedgerAsync(
            GetUserContext(),
            page,
            pageSize,
            farmId,
            storageLocationId,
            inventoryItemId,
            movementType,
            fromDate,
            toDate,
            cropCycleId,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("opening-stock")]
    [Authorize(Policy = "Permission:InventoryTransaction.Create")]
    public async Task<IActionResult> RecordOpeningStock(
        [FromBody] RecordOpeningStockRequest request,
        CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await stockService.RecordOpeningStockAsync(
            GetUserContext(),
            request,
            ipAddress,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("receipts")]
    [Authorize(Policy = "Permission:InventoryTransaction.Create")]
    public async Task<IActionResult> RecordReceipt(
        [FromBody] RecordStockReceiptRequest request,
        CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await stockService.RecordStockReceiptAsync(
            GetUserContext(),
            request,
            ipAddress,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("issues")]
    [Authorize(Policy = "Permission:InventoryTransaction.Create")]
    public async Task<IActionResult> RecordIssue(
        [FromBody] RecordStockIssueRequest request,
        CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await stockService.RecordStockIssueAsync(
            GetUserContext(),
            request,
            ipAddress,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("adjustments")]
    [Authorize(Policy = "Permission:InventoryTransaction.Create")]
    public async Task<IActionResult> RecordAdjustment(
        [FromBody] RecordStockAdjustmentRequest request,
        CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await stockService.RecordStockAdjustmentAsync(
            GetUserContext(),
            request,
            ipAddress,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("transfers")]
    [Authorize(Policy = "Permission:InventoryTransaction.Create")]
    public async Task<IActionResult> RecordTransfer(
        [FromBody] RecordStockTransferRequest request,
        CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await stockService.RecordStockTransferAsync(
            GetUserContext(),
            request,
            ipAddress,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("movements/{id:guid}/reverse")]
    [Authorize(Policy = "Permission:InventoryTransaction.Reverse")]
    public async Task<IActionResult> ReverseMovement(
        Guid id,
        [FromBody] ReverseStockMovementRequest request,
        CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await stockService.ReverseStockMovementAsync(
            GetUserContext(),
            id,
            request,
            ipAddress,
            cancellationToken);

        return Ok(result);
    }

    private InventoryActor GetUserContext() => UserContextHelper.GetUserContext<InventoryActor>(User);
}
