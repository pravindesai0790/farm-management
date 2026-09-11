using FarmManagement.API.Helpers;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Farms;
using FarmManagement.Application.Interfaces.Farms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmManagement.API.Controllers;

[ApiController]
[Authorize]
public sealed class FarmAreasController(IFarmAreaService farmAreaService) : ControllerBase
{
    [HttpGet("api/farm-areas")]
    [Authorize(Policy = "Permission:FarmArea.View")]
    public async Task<ActionResult<PagedResponse<FarmAreaResponse>>> ListAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? farmId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        return Ok(await farmAreaService.ListPagedAsync(GetUserContext(), page, pageSize, farmId, isActive, search, cancellationToken));
    }

    [HttpGet("api/farms/{farmId:guid}/areas")]
    [Authorize(Policy = "Permission:FarmArea.View")]
    public async Task<ActionResult<PagedResponse<FarmAreaResponse>>> List(
        Guid farmId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        return Ok(await farmAreaService.ListAsync(GetUserContext(), farmId, page, pageSize, isActive, search, cancellationToken));
    }

    [HttpGet("api/farm-areas/{id:guid}")]
    [Authorize(Policy = "Permission:FarmArea.View")]
    public async Task<ActionResult<FarmAreaResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await farmAreaService.GetAsync(GetUserContext(), id, cancellationToken));
    }

    [HttpPost("api/farm-areas")]
    [Authorize(Policy = "Permission:FarmArea.Create")]
    public async Task<ActionResult<FarmAreaResponse>> Create(
        [FromBody] CreateFarmAreaRequest request,
        CancellationToken cancellationToken)
    {
        var result = await farmAreaService.CreateAsync(GetUserContext(), request, GetIpAddress(), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("api/farm-areas/{id:guid}")]
    [Authorize(Policy = "Permission:FarmArea.Update")]
    public async Task<ActionResult<FarmAreaResponse>> Update(
        Guid id,
        [FromBody] UpdateFarmAreaRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await farmAreaService.UpdateAsync(GetUserContext(), id, request, GetIpAddress(), cancellationToken));
    }

    [HttpPatch("api/farm-areas/{id:guid}/activate")]
    [Authorize(Policy = "Permission:FarmArea.Activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        await farmAreaService.ActivateAsync(GetUserContext(), id, GetIpAddress(), cancellationToken);
        return NoContent();
    }

    [HttpPatch("api/farm-areas/{id:guid}/deactivate")]
    [Authorize(Policy = "Permission:FarmArea.Deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await farmAreaService.DeactivateAsync(GetUserContext(), id, GetIpAddress(), cancellationToken);
        return NoContent();
    }

    [HttpGet("api/farm-areas/{id:guid}/availability")]
    [Authorize(Policy = "Permission:FarmArea.View")]
    public async Task<ActionResult<FarmAreaAvailabilityResponse>> GetAvailability(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await farmAreaService.GetAvailabilityAsync(GetUserContext(), id, cancellationToken));
    }

    private FarmActor GetUserContext() => UserContextHelper.GetUserContext<FarmActor>(User);

    private string? GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
