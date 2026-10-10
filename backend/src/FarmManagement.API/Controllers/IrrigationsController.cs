using FarmManagement.API.Helpers;
using FarmManagement.Application.DTOs.Irrigation;
using FarmManagement.Application.Interfaces.Irrigation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmManagement.API.Controllers;

[ApiController]
[Route("api/irrigations")]
[Authorize]
public sealed class IrrigationsController(IIrrigationService irrigationService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Permission:Irrigation.View")]
    public async Task<IActionResult> List([FromQuery] IrrigationListQuery query, CancellationToken cancellationToken = default)
    {
        var actor = UserContextHelper.GetUserContext<IrrigationActor>(User);
        var result = await irrigationService.ListAsync(actor, query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("summary")]
    [Authorize(Policy = "Permission:Irrigation.View")]
    public async Task<IActionResult> Summary([FromQuery] Guid? farmId, CancellationToken cancellationToken = default)
    {
        var actor = UserContextHelper.GetUserContext<IrrigationActor>(User);
        var result = await irrigationService.GetSummaryCountsAsync(actor, farmId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("methods")]
    [Authorize(Policy = "Permission:IrrigationMethod.View")]
    public async Task<IActionResult> ListMethods([FromQuery] bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        var actor = UserContextHelper.GetUserContext<IrrigationActor>(User);
        var result = await irrigationService.ListMethodsAsync(actor, activeOnly, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Permission:Irrigation.View")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken = default)
    {
        var actor = UserContextHelper.GetUserContext<IrrigationActor>(User);
        var result = await irrigationService.GetAsync(actor, id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "Permission:Irrigation.Create")]
    public async Task<IActionResult> Create([FromBody] CreateIrrigationDraftRequest request, CancellationToken cancellationToken = default)
    {
        var actor = UserContextHelper.GetUserContext<IrrigationActor>(User);
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await irrigationService.CreateDraftAsync(actor, request, ipAddress, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Permission:Irrigation.Update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateIrrigationDraftRequest request, CancellationToken cancellationToken = default)
    {
        var actor = UserContextHelper.GetUserContext<IrrigationActor>(User);
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await irrigationService.UpdateDraftAsync(actor, id, request, ipAddress, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/schedule")]
    [Authorize(Policy = "Permission:Irrigation.Schedule")]
    public async Task<IActionResult> Schedule(Guid id, [FromBody] ScheduleIrrigationRequest request, CancellationToken cancellationToken = default)
    {
        var actor = UserContextHelper.GetUserContext<IrrigationActor>(User);
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await irrigationService.ScheduleAsync(actor, id, request, ipAddress, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/reschedule")]
    [Authorize(Policy = "Permission:Irrigation.Schedule")]
    public async Task<IActionResult> Reschedule(Guid id, [FromBody] RescheduleIrrigationRequest request, CancellationToken cancellationToken = default)
    {
        var actor = UserContextHelper.GetUserContext<IrrigationActor>(User);
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await irrigationService.RescheduleAsync(actor, id, request, ipAddress, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/start")]
    [Authorize(Policy = "Permission:Irrigation.Start")]
    public async Task<IActionResult> Start(Guid id, [FromBody] StartIrrigationRequest request, CancellationToken cancellationToken = default)
    {
        var actor = UserContextHelper.GetUserContext<IrrigationActor>(User);
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await irrigationService.StartAsync(actor, id, request, ipAddress, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/complete")]
    [Authorize(Policy = "Permission:Irrigation.Complete")]
    public async Task<IActionResult> Complete(Guid id, [FromBody] CompleteIrrigationRequest request, CancellationToken cancellationToken = default)
    {
        var actor = UserContextHelper.GetUserContext<IrrigationActor>(User);
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await irrigationService.CompleteAsync(actor, id, request, ipAddress, cancellationToken);
        return Ok(result);
    }

    [HttpPost("record-completed")]
    [Authorize(Policy = "Permission:Irrigation.Complete")]
    public async Task<IActionResult> RecordCompleted([FromBody] RecordCompletedIrrigationRequest request, CancellationToken cancellationToken = default)
    {
        var actor = UserContextHelper.GetUserContext<IrrigationActor>(User);
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await irrigationService.RecordCompletedAsync(actor, request, ipAddress, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = "Permission:Irrigation.Cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelIrrigationRequest request, CancellationToken cancellationToken = default)
    {
        var actor = UserContextHelper.GetUserContext<IrrigationActor>(User);
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await irrigationService.CancelAsync(actor, id, request, ipAddress, cancellationToken);
        return Ok(result);
    }
}
