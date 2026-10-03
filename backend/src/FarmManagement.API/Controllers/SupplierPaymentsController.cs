using FarmManagement.API.Helpers;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Expenses;
using FarmManagement.Application.Interfaces.Expenses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmManagement.API.Controllers;

[ApiController]
[Route("api/supplier-payments")]
[Authorize]
public sealed class SupplierPaymentsController(ISupplierPaymentService paymentService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Permission:SupplierPayment.View")]
    [ProducesResponseType(typeof(PagedResponse<SupplierPaymentResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<SupplierPaymentResponse>>> List(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] Guid? supplierId,
        [FromQuery] Guid? invoiceId,
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var filter = new SupplierPaymentFilter(
            From: from,
            To: to,
            SupplierId: supplierId,
            InvoiceId: invoiceId,
            Status: status,
            Search: search);

        var result = await paymentService.ListAsync(GetUserContext(), filter, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("unpaid-invoices")]
    [Authorize(Policy = "Permission:PurchaseInvoice.View")]
    [ProducesResponseType(typeof(IReadOnlyList<UnpaidPurchaseInvoiceSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UnpaidPurchaseInvoiceSummaryResponse>>> GetUnpaidInvoices(
        [FromQuery] Guid supplierId,
        [FromQuery] Guid? currencyId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await paymentService.GetUnpaidInvoicesAsync(GetUserContext(), supplierId, currencyId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Permission:SupplierPayment.View")]
    [ProducesResponseType(typeof(SupplierPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupplierPaymentResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await paymentService.GetByIdAsync(GetUserContext(), id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "Permission:SupplierPayment.Create")]
    [ProducesResponseType(typeof(SupplierPaymentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SupplierPaymentResponse>> RecordPayment(
        [FromBody] RecordSupplierPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await paymentService.RecordPaymentAsync(GetUserContext(), request, ipAddress, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPost("{id:guid}/reverse")]
    [Authorize(Policy = "Permission:SupplierPayment.Reverse")]
    [ProducesResponseType(typeof(SupplierPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupplierPaymentResponse>> Reverse(
        Guid id,
        [FromBody] ReverseSupplierPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await paymentService.ReversePaymentAsync(GetUserContext(), id, request, ipAddress, cancellationToken);
        return Ok(result);
    }

    private ExpenseActor GetUserContext() => UserContextHelper.GetUserContext<ExpenseActor>(User);
}
