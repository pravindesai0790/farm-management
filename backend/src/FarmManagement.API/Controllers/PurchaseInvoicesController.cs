using FarmManagement.API.Helpers;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Expenses;
using FarmManagement.Application.Interfaces.Expenses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmManagement.API.Controllers;

[ApiController]
[Route("api/purchase-invoices")]
[Authorize]
public sealed class PurchaseInvoicesController(IPurchaseInvoiceService purchaseInvoiceService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Permission:PurchaseInvoice.View")]
    [ProducesResponseType(typeof(PagedResponse<PurchaseInvoiceResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<PurchaseInvoiceResponse>>> List(
        [FromQuery] string? search,
        [FromQuery] Guid? supplierId,
        [FromQuery] Guid? farmId,
        [FromQuery] string? status,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? paymentStatus,
        [FromQuery] string? dueStatus,
        [FromQuery] string? receiptStatus,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var filter = new PurchaseInvoiceFilter(
            Search: search,
            SupplierId: supplierId,
            FarmId: farmId,
            Status: status,
            From: from,
            To: to,
            PaymentStatus: paymentStatus,
            DueStatus: dueStatus,
            ReceiptStatus: receiptStatus);

        var result = await purchaseInvoiceService.ListAsync(GetUserContext(), filter, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Permission:PurchaseInvoice.View")]
    [ProducesResponseType(typeof(PurchaseInvoiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PurchaseInvoiceResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await purchaseInvoiceService.GetAsync(GetUserContext(), id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "Permission:PurchaseInvoice.Create")]
    [ProducesResponseType(typeof(PurchaseInvoiceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PurchaseInvoiceResponse>> Create(
        [FromBody] CreatePurchaseInvoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await purchaseInvoiceService.CreateDraftAsync(GetUserContext(), request, GetIpAddress(), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Permission:PurchaseInvoice.UpdateDraft")]
    [ProducesResponseType(typeof(PurchaseInvoiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PurchaseInvoiceResponse>> Update(
        Guid id,
        [FromBody] UpdatePurchaseInvoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await purchaseInvoiceService.UpdateDraftAsync(GetUserContext(), id, request, GetIpAddress(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/post")]
    [Authorize(Policy = "Permission:PurchaseInvoice.Post")]
    [ProducesResponseType(typeof(PurchaseInvoiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PurchaseInvoiceResponse>> Post(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await purchaseInvoiceService.PostAsync(GetUserContext(), id, GetIpAddress(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/reverse")]
    [Authorize(Policy = "Permission:PurchaseInvoice.Reverse")]
    [ProducesResponseType(typeof(PurchaseInvoiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PurchaseInvoiceResponse>> Reverse(
        Guid id,
        [FromBody] ReversePurchaseInvoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await purchaseInvoiceService.ReverseAsync(GetUserContext(), id, request, GetIpAddress(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/receipt-summary")]
    [Authorize(Policy = "Permission:PurchaseInvoice.View")]
    [ProducesResponseType(typeof(PurchaseInvoiceReceiptSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PurchaseInvoiceReceiptSummaryResponse>> GetReceiptSummary(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await purchaseInvoiceService.GetReceiptSummaryAsync(GetUserContext(), id, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/lines/remaining-to-receive")]
    [Authorize(Policy = "Permission:PurchaseInvoice.View")]
    [ProducesResponseType(typeof(IReadOnlyList<PurchaseInvoiceRemainingLineResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<PurchaseInvoiceRemainingLineResponse>>> GetRemainingToReceive(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await purchaseInvoiceService.GetRemainingToReceiveAsync(GetUserContext(), id, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/receipts")]
    [Authorize(Policy = "Permission:PurchaseInvoice.View")]
    [ProducesResponseType(typeof(IReadOnlyList<PurchaseInvoiceReceiptGroupResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<PurchaseInvoiceReceiptGroupResponse>>> GetReceiptHistory(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await purchaseInvoiceService.GetReceiptHistoryAsync(GetUserContext(), id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/receipts")]
    [Authorize(Policy = "Permission:PurchaseInvoice.ReceiveItems")]
    [ProducesResponseType(typeof(PurchaseInvoiceReceiptGroupResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PurchaseInvoiceReceiptGroupResponse>> ReceiveItems(
        Guid id,
        [FromBody] ReceivePurchaseInvoiceItemsRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await purchaseInvoiceService.ReceiveItemsAsync(GetUserContext(), id, request, GetIpAddress(), cancellationToken);
        return CreatedAtAction(nameof(GetReceiptHistory), new { id }, result);
    }

    private ExpenseActor GetUserContext() => UserContextHelper.GetUserContext<ExpenseActor>(User);

    private string? GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
