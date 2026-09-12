using FarmManagement.API.Helpers;
using FarmManagement.Application.DTOs.Labor;
using FarmManagement.Application.Interfaces.Labor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmManagement.API.Controllers;

[ApiController]
[Route("api/labor/workers/{workerId:guid}")]
[Authorize]
public sealed class WorkerPaymentAllocationsController(
    IWorkerPaymentAllocationService allocationService,
    IWorkerPaymentService paymentService) : ControllerBase
{
    [HttpGet("payments/{paymentId:guid}/allocations")]
    [Authorize(Policy = "Permission:WorkerPayment.View")]
    public async Task<ActionResult<IReadOnlyList<WorkerPaymentAllocationResponse>>> ListByPayment(
        Guid workerId,
        Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        var payment = await paymentService.GetByIdAsync(GetUserContext(), paymentId, cancellationToken);
        if (payment.WorkerId != workerId)
        {
            return NotFound();
        }

        return Ok(await allocationService.ListByPaymentAsync(GetUserContext(), paymentId, cancellationToken));
    }

    [HttpGet("payments/{paymentId:guid}/allocations/{id:guid}")]
    [Authorize(Policy = "Permission:WorkerPayment.View")]
    public async Task<ActionResult<WorkerPaymentAllocationResponse>> GetById(
        Guid workerId,
        Guid paymentId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var payment = await paymentService.GetByIdAsync(GetUserContext(), paymentId, cancellationToken);
        if (payment.WorkerId != workerId)
        {
            return NotFound();
        }

        var allocation = await allocationService.GetByIdAsync(GetUserContext(), id, cancellationToken);
        if (allocation.WorkerPaymentId != paymentId)
        {
            return NotFound();
        }

        return Ok(allocation);
    }

    [HttpGet("payments/{paymentId:guid}/unallocated-balance")]
    [Authorize(Policy = "Permission:WorkerPayment.View")]
    public async Task<ActionResult<PaymentUnallocatedBalanceResponse>> GetPaymentBalance(
        Guid workerId,
        Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        var payment = await paymentService.GetByIdAsync(GetUserContext(), paymentId, cancellationToken);
        if (payment.WorkerId != workerId)
        {
            return NotFound();
        }

        return Ok(await allocationService.GetPaymentUnallocatedBalanceAsync(GetUserContext(), paymentId, cancellationToken));
    }

    [HttpPost("payments/{paymentId:guid}/allocations")]
    [Authorize(Policy = "Permission:WorkerPayment.Create")]
    public async Task<ActionResult<WorkerPaymentAllocationResponse>> CreateAllocation(
        Guid workerId,
        Guid paymentId,
        [FromBody] CreateWorkerPaymentAllocationRequest request,
        CancellationToken cancellationToken = default)
    {
        var payment = await paymentService.GetByIdAsync(GetUserContext(), paymentId, cancellationToken);
        if (payment.WorkerId != workerId)
        {
            return NotFound();
        }

        if (request.WorkerPaymentId != paymentId)
        {
            request = request with { WorkerPaymentId = paymentId };
        }

        var response = await allocationService.CreateAllocationAsync(
            GetUserContext(),
            request,
            GetIpAddress(),
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { workerId, paymentId, id = response.Id },
            response);
    }

    [HttpPost("payments/{paymentId:guid}/allocations/auto")]
    [Authorize(Policy = "Permission:WorkerPayment.Create")]
    public async Task<ActionResult<IReadOnlyList<WorkerPaymentAllocationResponse>>> AutoAllocate(
        Guid workerId,
        Guid paymentId,
        [FromBody] AutoAllocatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var payment = await paymentService.GetByIdAsync(GetUserContext(), paymentId, cancellationToken);
        if (payment.WorkerId != workerId)
        {
            return NotFound();
        }

        if (request.WorkerPaymentId != paymentId)
        {
            request = request with { WorkerPaymentId = paymentId };
        }

        var response = await allocationService.AutoAllocatePaymentAsync(
            GetUserContext(),
            request,
            GetIpAddress(),
            cancellationToken);

        return Ok(response);
    }

    [HttpDelete("payments/{paymentId:guid}/allocations/{id:guid}")]
    [Authorize(Policy = "Permission:WorkerPayment.Create")]
    public async Task<IActionResult> DeleteAllocation(
        Guid workerId,
        Guid paymentId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var payment = await paymentService.GetByIdAsync(GetUserContext(), paymentId, cancellationToken);
        if (payment.WorkerId != workerId)
        {
            return NotFound();
        }

        var allocation = await allocationService.GetByIdAsync(GetUserContext(), id, cancellationToken);
        if (allocation.WorkerPaymentId != paymentId)
        {
            return NotFound();
        }

        await allocationService.DeleteAllocationAsync(
            GetUserContext(),
            id,
            GetIpAddress(),
            cancellationToken);

        return NoContent();
    }

    [HttpGet("unsettled-earnings")]
    [Authorize(Policy = "Permission:WorkerPayment.View")]
    public async Task<ActionResult<IReadOnlyList<EarningUnpaidBalanceResponse>>> GetUnsettledEarnings(
        Guid workerId,
        [FromQuery] DateOnly? upToDate = null,
        CancellationToken cancellationToken = default) =>
        Ok(await allocationService.GetUnsettledEarningsForWorkerAsync(
            GetUserContext(),
            workerId,
            upToDate,
            cancellationToken));

    [HttpGet("allocations")]
    [Authorize(Policy = "Permission:WorkerPayment.View")]
    public async Task<ActionResult<IReadOnlyList<WorkerPaymentAllocationResponse>>> ListByWorker(
        Guid workerId,
        CancellationToken cancellationToken = default) =>
        Ok(await allocationService.ListByWorkerAsync(
            GetUserContext(),
            workerId,
            cancellationToken));

    private PaymentActor GetUserContext() => UserContextHelper.GetUserContext<PaymentActor>(User);

    private string? GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
