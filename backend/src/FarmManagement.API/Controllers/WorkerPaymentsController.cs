using FarmManagement.API.Helpers;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Labor;
using FarmManagement.Application.Interfaces.Labor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmManagement.API.Controllers;

[ApiController]
[Route("api/labor/workers/{workerId:guid}/payments")]
[Authorize]
public sealed class WorkerPaymentsController(IWorkerPaymentService paymentService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Permission:WorkerPayment.View")]
    public async Task<ActionResult<PagedResponse<WorkerPaymentResponse>>> List(
        Guid workerId,
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        [FromQuery] string? paymentType = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await paymentService.ListByWorkerAsync(
            GetUserContext(),
            workerId,
            fromDate,
            toDate,
            paymentType,
            status,
            page,
            pageSize,
            cancellationToken));

    [HttpGet("summary")]
    [Authorize(Policy = "Permission:WorkerPayment.View")]
    public async Task<ActionResult<WorkerFinancialSummaryResponse>> GetSummary(
        Guid workerId,
        [FromQuery] DateOnly? asOfDate = null,
        CancellationToken cancellationToken = default) =>
        Ok(await paymentService.GetWorkerFinancialSummaryAsync(
            GetUserContext(),
            workerId,
            asOfDate,
            cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Permission:WorkerPayment.View")]
    public async Task<ActionResult<WorkerPaymentResponse>> GetById(
        Guid workerId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var response = await paymentService.GetByIdAsync(GetUserContext(), id, cancellationToken);
        if (response.WorkerId != workerId)
        {
            return NotFound();
        }

        return Ok(response);
    }

    [HttpPost]
    [Authorize(Policy = "Permission:WorkerPayment.Create")]
    public async Task<ActionResult<WorkerPaymentResponse>> Record(
        Guid workerId,
        [FromBody] RecordWorkerPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.WorkerId != workerId)
        {
            request = request with { WorkerId = workerId };
        }

        var response = await paymentService.RecordPaymentAsync(
            GetUserContext(),
            request,
            GetIpAddress(),
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { workerId, id = response.Id },
            response);
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = "Permission:WorkerPayment.Cancel")]
    public async Task<ActionResult<WorkerPaymentResponse>> Cancel(
        Guid workerId,
        Guid id,
        [FromBody] CancelWorkerPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var existing = await paymentService.GetByIdAsync(GetUserContext(), id, cancellationToken);
        if (existing.WorkerId != workerId)
        {
            return NotFound();
        }

        var response = await paymentService.CancelPaymentAsync(
            GetUserContext(),
            id,
            request,
            GetIpAddress(),
            cancellationToken);

        return Ok(response);
    }

    private PaymentActor GetUserContext() => UserContextHelper.GetUserContext<PaymentActor>(User);

    private string? GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
