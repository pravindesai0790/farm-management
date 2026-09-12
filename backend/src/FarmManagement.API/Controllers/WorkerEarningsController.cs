using FarmManagement.API.Helpers;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Labor;
using FarmManagement.Application.Interfaces.Labor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmManagement.API.Controllers;

[ApiController]
[Route("api/labor/workers/{workerId:guid}/earnings")]
[Authorize]
public sealed class WorkerEarningsController(IWorkerEarningsLedgerService earningsService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Permission:WorkerEarnings.View")]
    public async Task<ActionResult<PagedResponse<WorkerEarningsLedgerResponse>>> List(
        Guid workerId,
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        [FromQuery] string? status = null,
        [FromQuery] string? entryType = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await earningsService.ListWorkerEarningsAsync(
            GetUserContext(),
            workerId,
            fromDate,
            toDate,
            status,
            entryType,
            page,
            pageSize,
            cancellationToken));

    [HttpGet("balance")]
    [Authorize(Policy = "Permission:WorkerEarnings.View")]
    public async Task<ActionResult<WorkerGrossEarnedBalanceResponse>> GetBalance(
        Guid workerId,
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        CancellationToken cancellationToken = default) =>
        Ok(await earningsService.GetWorkerGrossEarnedBalanceAsync(
            GetUserContext(),
            workerId,
            fromDate,
            toDate,
            cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Permission:WorkerEarnings.View")]
    public async Task<ActionResult<WorkerEarningsLedgerResponse>> GetById(
        Guid workerId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var response = await earningsService.GetByIdAsync(GetUserContext(), id, cancellationToken);
        if (response.WorkerId != workerId)
        {
            return NotFound();
        }

        return Ok(response);
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = "Permission:WorkerEarnings.Approve")]
    public async Task<ActionResult<WorkerEarningsLedgerResponse>> Approve(
        Guid workerId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var existing = await earningsService.GetByIdAsync(GetUserContext(), id, cancellationToken);
        if (existing.WorkerId != workerId)
        {
            return NotFound();
        }

        var response = await earningsService.ApproveEarningAsync(
            GetUserContext(),
            id,
            GetIpAddress(),
            cancellationToken);

        return Ok(response);
    }

    [HttpPost("{id:guid}/reverse")]
    [Authorize(Policy = "Permission:WorkerEarnings.Reverse")]
    public async Task<ActionResult<WorkerEarningsLedgerResponse>> Reverse(
        Guid workerId,
        Guid id,
        [FromBody] ReverseEarningsRequest request,
        CancellationToken cancellationToken = default)
    {
        var existing = await earningsService.GetByIdAsync(GetUserContext(), id, cancellationToken);
        if (existing.WorkerId != workerId)
        {
            return NotFound();
        }

        var response = await earningsService.ReverseEarningAsync(
            GetUserContext(),
            id,
            request,
            GetIpAddress(),
            cancellationToken);

        return Ok(response);
    }

    [HttpPost("adjustments")]
    [Authorize(Policy = "Permission:WorkerEarnings.Approve")]
    public async Task<ActionResult<WorkerEarningsLedgerResponse>> CreateAdjustment(
        Guid workerId,
        [FromBody] CreateEarningsAdjustmentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.WorkerId != workerId)
        {
            request = request with { WorkerId = workerId };
        }

        var response = await earningsService.CreateAdjustmentAsync(
            GetUserContext(),
            request,
            GetIpAddress(),
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { workerId, id = response.Id },
            response);
    }

    private EarningsActor GetUserContext() => UserContextHelper.GetUserContext<EarningsActor>(User);

    private string? GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
