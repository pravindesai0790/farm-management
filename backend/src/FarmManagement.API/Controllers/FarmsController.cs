using FarmManagement.API.Helpers;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Farms;
using FarmManagement.Application.Interfaces.Farms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmManagement.API.Controllers;

[ApiController]
[Route("api/farms")]
[Authorize]
public sealed class FarmsController(IFarmService farmService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Permission:Farm.View")]
    public async Task<ActionResult<PagedResponse<FarmResponse>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        return Ok(await farmService.ListAsync(GetUserContext(), page, pageSize, search, isActive, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Permission:Farm.View")]
    public async Task<ActionResult<FarmResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await farmService.GetAsync(GetUserContext(), id, cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = "Permission:Farm.Create")]
    public async Task<ActionResult<FarmResponse>> Create(
        [FromBody] CreateFarmRequest request,
        CancellationToken cancellationToken)
    {
        var result = await farmService.CreateAsync(GetUserContext(), request, GetIpAddress(), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Permission:Farm.Update")]
    public async Task<ActionResult<FarmResponse>> Update(
        Guid id,
        [FromBody] UpdateFarmRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await farmService.UpdateAsync(GetUserContext(), id, request, GetIpAddress(), cancellationToken));
    }

    [HttpPatch("{id:guid}/activate")]
    [Authorize(Policy = "Permission:Farm.Activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        await farmService.ActivateAsync(GetUserContext(), id, GetIpAddress(), cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:guid}/deactivate")]
    [Authorize(Policy = "Permission:Farm.Deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await farmService.DeactivateAsync(GetUserContext(), id, GetIpAddress(), cancellationToken);
        return NoContent();
    }

    private FarmActor GetUserContext() => UserContextHelper.GetUserContext<FarmActor>(User);

    private string? GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
