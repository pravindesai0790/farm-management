using FarmManagement.API.Helpers;
using FarmManagement.Application.DTOs.CropCycles;
using FarmManagement.Application.Interfaces.CropCycles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmManagement.API.Controllers;

[ApiController]
[Route("api/crop-cycle-stages")]
[Authorize]
public sealed class CropCycleStagesController(ICropCycleService cycleService) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Permission:CropCycleLifecycle.View")]
    public async Task<ActionResult<CropCycleStageResponse>> Get(Guid id, CancellationToken cancellationToken = default) =>
        Ok(await cycleService.GetStageAsync(GetUserContext(), id, cancellationToken));

    [HttpPost("{id:guid}/complete")]
    [Authorize(Policy = "Permission:CropCycleLifecycle.UpdateStage")]
    public async Task<ActionResult<CropCycleStageResponse>> Complete(
        Guid id,
        [FromBody] CompleteCropCycleStageRequest? request,
        CancellationToken cancellationToken = default) =>
        Ok(await cycleService.CompleteStageAsync(GetUserContext(), id, request, GetIpAddress(), cancellationToken));

    [HttpPost("{id:guid}/skip")]
    [Authorize(Policy = "Permission:CropCycleLifecycle.SkipStage")]
    public async Task<ActionResult<CropCycleStageResponse>> Skip(
        Guid id,
        [FromBody] SkipCropCycleStageRequest request,
        CancellationToken cancellationToken = default) =>
        Ok(await cycleService.SkipStageAsync(GetUserContext(), id, request, GetIpAddress(), cancellationToken));

    [HttpPost("{id:guid}/reopen")]
    [Authorize(Policy = "Permission:CropCycleLifecycle.ReopenStage")]
    public async Task<ActionResult<CropCycleStageResponse>> Reopen(
        Guid id,
        [FromBody] ReopenCropCycleStageRequest request,
        CancellationToken cancellationToken = default) =>
        Ok(await cycleService.ReopenStageAsync(GetUserContext(), id, request, GetIpAddress(), cancellationToken));

    [HttpPost("{id:guid}/override")]
    [Authorize(Policy = "Permission:CropCycleLifecycle.OverrideStage")]
    public async Task<ActionResult<CropCycleStageResponse>> Override(
        Guid id,
        [FromBody] OverrideCropCycleStageRequest request,
        CancellationToken cancellationToken = default) =>
        Ok(await cycleService.OverrideStageAsync(GetUserContext(), id, request, GetIpAddress(), cancellationToken));

    [HttpPut("{id:guid}/planned-dates")]
    [Authorize(Policy = "Permission:CropCycleLifecycle.UpdateStage")]
    public async Task<ActionResult<CropCycleStageResponse>> UpdatePlannedDates(
        Guid id,
        [FromBody] UpdateCropCycleStagePlannedDatesRequest request,
        CancellationToken cancellationToken = default) =>
        Ok(await cycleService.UpdateStagePlannedDatesAsync(GetUserContext(), id, request, GetIpAddress(), cancellationToken));

    private CropCycleActor GetUserContext() => UserContextHelper.GetUserContext<CropCycleActor>(User);

    private string? GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
