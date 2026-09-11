using FarmManagement.API.Helpers;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.LaborActivities;
using FarmManagement.Application.Interfaces.LaborActivities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmManagement.API.Controllers;

[ApiController]
[Route("api/labor-activities")]
[Authorize]
public sealed class LaborActivitiesController(ILaborActivityService activityService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Permission:LaborActivity.View")]
    public async Task<ActionResult<PagedResponse<LaborActivityResponse>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? farmId = null,
        [FromQuery] Guid? farmAreaId = null,
        [FromQuery] Guid? plantationId = null,
        [FromQuery] Guid? cropCycleId = null,
        [FromQuery] Guid? activityTypeId = null,
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default) =>
        Ok(await activityService.ListAsync(
            GetUserContext(),
            page,
            pageSize,
            farmId,
            farmAreaId,
            plantationId,
            cropCycleId,
            activityTypeId,
            fromDate,
            toDate,
            status,
            cancellationToken));

    [HttpGet("types")]
    [Authorize(Policy = "Permission:LaborActivity.View")]
    public async Task<ActionResult<IReadOnlyList<NamedReferenceResponse>>> ListTypes(
        CancellationToken cancellationToken = default) =>
        Ok(await activityService.ListTypesAsync(GetUserContext(), cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Permission:LaborActivity.View")]
    public async Task<ActionResult<LaborActivityResponse>> Get(
        Guid id,
        CancellationToken cancellationToken = default) =>
        Ok(await activityService.GetAsync(GetUserContext(), id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = "Permission:LaborActivity.Create")]
    public async Task<ActionResult<LaborActivityResponse>> Create(
        [FromBody] CreateLaborActivityRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await activityService.CreateAsync(GetUserContext(), request, GetIpAddress(), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Permission:LaborActivity.Update")]
    public async Task<ActionResult<LaborActivityResponse>> Update(
        Guid id,
        [FromBody] UpdateLaborActivityRequest request,
        CancellationToken cancellationToken = default) =>
        Ok(await activityService.UpdateAsync(GetUserContext(), id, request, GetIpAddress(), cancellationToken));

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = "Permission:LaborActivity.Cancel")]
    public async Task<IActionResult> Cancel(
        Guid id,
        [FromBody] CancelLaborActivityRequest request,
        CancellationToken cancellationToken = default)
    {
        await activityService.CancelAsync(GetUserContext(), id, request, GetIpAddress(), cancellationToken);
        return NoContent();
    }

    private LaborActivityActor GetUserContext() => UserContextHelper.GetUserContext<LaborActivityActor>(User);

    private string? GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
