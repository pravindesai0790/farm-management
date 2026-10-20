using FarmManagement.API.Helpers;
using FarmManagement.Application.DTOs.Fertilizer;
using FarmManagement.Application.Interfaces.Fertilizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmManagement.API.Controllers;

[ApiController]
[Route("api/fertilizer-applications")]
[Authorize]
public sealed class FertilizerApplicationsController(IFertilizerApplicationService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Permission:Fertilizer.View")]
    public async Task<IActionResult> List(
        [FromQuery] FertilizerApplicationListQuery query,
        CancellationToken cancellationToken = default)
    {
        var actor = UserContextHelper.GetUserContext<FertilizerActor>(User);
        var result = await service.ListAsync(actor, query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Permission:Fertilizer.View")]
    public async Task<IActionResult> Get(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var actor = UserContextHelper.GetUserContext<FertilizerActor>(User);
        var result = await service.GetAsync(actor, id, cancellationToken);
        return Ok(result);
    }

    [HttpGet("lookups/products")]
    [HttpGet("lookup/products")]
    [Authorize(Policy = "Permission:Fertilizer.View")]
    public async Task<IActionResult> ListProductsLookup(CancellationToken cancellationToken = default)
    {
        var actor = UserContextHelper.GetUserContext<FertilizerActor>(User);
        var result = await service.ListProductsLookupAsync(actor, cancellationToken);
        return Ok(result);
    }

    [HttpGet("lookups/storage-locations")]
    [HttpGet("lookup/storage-locations")]
    [Authorize(Policy = "Permission:Fertilizer.View")]
    public async Task<IActionResult> ListStorageLocationsLookup(
        [FromQuery] Guid farmId,
        [FromQuery] Guid? inventoryItemId,
        CancellationToken cancellationToken = default)
    {
        var actor = UserContextHelper.GetUserContext<FertilizerActor>(User);
        var result = await service.ListStorageLocationsLookupAsync(actor, farmId, inventoryItemId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("lookups/application-methods")]
    [HttpGet("lookup/application-methods")]
    [Authorize(Policy = "Permission:Fertilizer.View")]
    public async Task<IActionResult> ListApplicationMethodsLookup(CancellationToken cancellationToken = default)
    {
        var actor = UserContextHelper.GetUserContext<FertilizerActor>(User);
        var result = await service.ListApplicationMethodsLookupAsync(actor, cancellationToken);
        return Ok(result);
    }

    [HttpGet("lookups/irrigations")]
    [HttpGet("lookup/irrigations")]
    [Authorize(Policy = "Permission:Fertilizer.View")]
    public async Task<IActionResult> ListIrrigationsLookup(
        [FromQuery] Guid? farmId,
        [FromQuery] Guid? farmAreaId,
        CancellationToken cancellationToken = default)
    {
        var actor = UserContextHelper.GetUserContext<FertilizerActor>(User);
        var result = await service.ListIrrigationsLookupAsync(actor, farmId, farmAreaId, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [HttpPost("drafts")]
    [Authorize(Policy = "Permission:Fertilizer.Create")]
    public async Task<IActionResult> Create(
        [FromBody] CreateFertilizerDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        var actor = UserContextHelper.GetUserContext<FertilizerActor>(User);
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await service.CreateDraftAsync(actor, request, ipAddress, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [HttpPut("drafts/{id:guid}")]
    [Authorize(Policy = "Permission:Fertilizer.Update")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateFertilizerDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        var actor = UserContextHelper.GetUserContext<FertilizerActor>(User);
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await service.UpdateDraftAsync(actor, id, request, ipAddress, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [HttpDelete("drafts/{id:guid}")]
    [Authorize(Policy = "Permission:Fertilizer.DeleteDraft")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var actor = UserContextHelper.GetUserContext<FertilizerActor>(User);
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        await service.DeleteDraftAsync(actor, id, ipAddress, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/copy")]
    [Authorize(Policy = "Permission:Fertilizer.Create")]
    public async Task<IActionResult> Copy(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var actor = UserContextHelper.GetUserContext<FertilizerActor>(User);
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await service.CopyAsNewDraftAsync(actor, id, ipAddress, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPost("{id:guid}/plan")]
    [Authorize(Policy = "Permission:Fertilizer.Update")]
    public async Task<IActionResult> Plan(
        Guid id,
        [FromBody] PlanFertilizerApplicationRequest request,
        CancellationToken cancellationToken = default)
    {
        var actor = UserContextHelper.GetUserContext<FertilizerActor>(User);
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await service.PlanAsync(actor, id, request, ipAddress, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/schedule")]
    [Authorize(Policy = "Permission:Fertilizer.Update")]
    public async Task<IActionResult> Schedule(
        Guid id,
        [FromBody] ScheduleFertilizerApplicationRequest request,
        CancellationToken cancellationToken = default)
    {
        var actor = UserContextHelper.GetUserContext<FertilizerActor>(User);
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await service.ScheduleAsync(actor, id, request, ipAddress, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/reschedule")]
    [Authorize(Policy = "Permission:Fertilizer.Update")]
    public async Task<IActionResult> Reschedule(
        Guid id,
        [FromBody] RescheduleFertilizerApplicationRequest request,
        CancellationToken cancellationToken = default)
    {
        var actor = UserContextHelper.GetUserContext<FertilizerActor>(User);
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await service.RescheduleAsync(actor, id, request, ipAddress, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/start")]
    [Authorize(Policy = "Permission:Fertilizer.Start")]
    public async Task<IActionResult> Start(
        Guid id,
        [FromBody] StartFertilizerApplicationRequest? request,
        CancellationToken cancellationToken = default)
    {
        var actor = UserContextHelper.GetUserContext<FertilizerActor>(User);
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await service.StartAsync(actor, id, request ?? new StartFertilizerApplicationRequest(), ipAddress, cancellationToken);
        return Ok(result);
    }
}
