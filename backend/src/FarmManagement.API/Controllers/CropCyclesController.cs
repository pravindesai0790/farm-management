using FarmManagement.API.Helpers;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.CropCycles;
using FarmManagement.Application.Interfaces.CropCycles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmManagement.API.Controllers;

[ApiController]
[Route("api/crop-cycles")]
[Authorize]
public sealed class CropCyclesController(ICropCycleService cycleService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Permission:CropCycle.View")]
    public async Task<ActionResult<PagedResponse<CropCycleResponse>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? farmId = null,
        [FromQuery] Guid? farmAreaId = null,
        [FromQuery] Guid? plantationId = null,
        [FromQuery] string? status = null,
        [FromQuery] int? seasonYear = null,
        CancellationToken cancellationToken = default) =>
        Ok(await cycleService.ListAsync(GetUserContext(), page, pageSize, farmId, farmAreaId, plantationId, status, seasonYear, cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Permission:CropCycle.View")]
    public async Task<ActionResult<CropCycleResponse>> Get(Guid id, CancellationToken cancellationToken = default) =>
        Ok(await cycleService.GetAsync(GetUserContext(), id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = "Permission:CropCycle.Create")]
    public async Task<ActionResult<CropCycleResponse>> Create(
        [FromBody] CreateCropCycleRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await cycleService.CreateAsync(GetUserContext(), request, GetIpAddress(), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Permission:CropCycle.Update")]
    public async Task<ActionResult<CropCycleResponse>> Update(
        Guid id,
        [FromBody] UpdateCropCycleRequest request,
        CancellationToken cancellationToken = default) =>
        Ok(await cycleService.UpdateAsync(GetUserContext(), id, request, GetIpAddress(), cancellationToken));

    [HttpPost("{id:guid}/start")]
    [Authorize(Policy = "Permission:CropCycle.Start")]
    public async Task<IActionResult> Start(
        Guid id,
        [FromBody] StartCropCycleRequest request,
        CancellationToken cancellationToken = default)
    {
        await cycleService.StartAsync(GetUserContext(), id, request, GetIpAddress(), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/harvest")]
    [Authorize(Policy = "Permission:CropCycle.Complete")]
    public async Task<IActionResult> Harvest(
        Guid id,
        [FromBody] HarvestCropCycleRequest request,
        CancellationToken cancellationToken = default)
    {
        await cycleService.HarvestAsync(GetUserContext(), id, request, GetIpAddress(), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/complete")]
    [Authorize(Policy = "Permission:CropCycle.Complete")]
    public async Task<IActionResult> Complete(
        Guid id,
        [FromBody] CompleteCropCycleRequest? request,
        CancellationToken cancellationToken = default)
    {
        await cycleService.CompleteAsync(GetUserContext(), id, request, GetIpAddress(), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = "Permission:CropCycle.Cancel")]
    public async Task<IActionResult> Cancel(
        Guid id,
        [FromBody] CancelCropCycleRequest request,
        CancellationToken cancellationToken = default)
    {
        await cycleService.CancelAsync(GetUserContext(), id, request, GetIpAddress(), cancellationToken);
        return NoContent();
    }

    private CropCycleActor GetUserContext() => UserContextHelper.GetUserContext<CropCycleActor>(User);

    private string? GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
