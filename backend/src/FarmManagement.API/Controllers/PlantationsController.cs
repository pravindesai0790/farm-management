using FarmManagement.API.Helpers;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Plantations;
using FarmManagement.Application.Interfaces.Plantations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmManagement.API.Controllers;

[ApiController]
[Authorize]
public sealed class PlantationsController(IPlantationService plantationService) : ControllerBase
{
    [HttpGet("api/plantations")]
    [Authorize(Policy = "Permission:Plantation.View")]
    public async Task<ActionResult<PagedResponse<PlantationResponse>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? farmId = null,
        [FromQuery] Guid? farmAreaId = null,
        [FromQuery] string? status = null,
        [FromQuery] Guid? cropId = null,
        [FromQuery] int? availableForSeasonYear = null,
        [FromQuery] Guid? currentPlantationId = null,
        CancellationToken cancellationToken = default)
    {
        return Ok(await plantationService.ListAsync(
            GetUserContext(), page, pageSize, farmId, farmAreaId, status, cropId, availableForSeasonYear, currentPlantationId, cancellationToken));
    }

    [HttpGet("api/plantations/{id:guid}")]
    [Authorize(Policy = "Permission:Plantation.View")]
    public async Task<ActionResult<PlantationResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await plantationService.GetAsync(GetUserContext(), id, cancellationToken));
    }

    [HttpPost("api/plantations")]
    [Authorize(Policy = "Permission:Plantation.Create")]
    public async Task<ActionResult<PlantationResponse>> Create(
        [FromBody] CreatePlantationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await plantationService.CreateAsync(GetUserContext(), request, GetIpAddress(), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("api/plantations/{id:guid}")]
    [Authorize(Policy = "Permission:Plantation.Update")]
    public async Task<ActionResult<PlantationResponse>> Update(
        Guid id,
        [FromBody] UpdatePlantationRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await plantationService.UpdateAsync(GetUserContext(), id, request, GetIpAddress(), cancellationToken));
    }

    [HttpPost("api/plantations/{id:guid}/activate")]
    [Authorize(Policy = "Permission:Plantation.Activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        await plantationService.ActivateAsync(GetUserContext(), id, GetIpAddress(), cancellationToken);
        return NoContent();
    }

    [HttpPost("api/plantations/{id:guid}/terminate")]
    [Authorize(Policy = "Permission:Plantation.Terminate")]
    public async Task<IActionResult> Terminate(
        Guid id,
        [FromBody] TerminatePlantationRequest request,
        CancellationToken cancellationToken)
    {
        await plantationService.TerminateAsync(GetUserContext(), id, request, GetIpAddress(), cancellationToken);
        return NoContent();
    }

    [HttpPost("api/plantations/{id:guid}/archive")]
    [Authorize(Policy = "Permission:Plantation.Update")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        await plantationService.ArchiveAsync(GetUserContext(), id, GetIpAddress(), cancellationToken);
        return NoContent();
    }

    private PlantationActor GetUserContext() => UserContextHelper.GetUserContext<PlantationActor>(User);

    private string? GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
