using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FarmManagement.Application.Common.Constants;
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
    public async Task<ActionResult<PlantationListResponse>> List(
        [FromQuery] Guid? farmId,
        [FromQuery] Guid? farmAreaId,
        [FromQuery] string? status,
        [FromQuery] Guid? cropId,
        CancellationToken cancellationToken)
    {
        return Ok(await plantationService.ListAsync(GetActor(), farmId, farmAreaId, status, cropId, cancellationToken));
    }

    [HttpGet("api/plantations/{id:guid}")]
    [Authorize(Policy = "Permission:Plantation.View")]
    public async Task<ActionResult<PlantationResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await plantationService.GetAsync(GetActor(), id, cancellationToken));
    }

    [HttpPost("api/plantations")]
    [Authorize(Policy = "Permission:Plantation.Create")]
    public async Task<ActionResult<PlantationResponse>> Create(
        [FromBody] CreatePlantationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await plantationService.CreateAsync(GetActor(), request, GetIpAddress(), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("api/plantations/{id:guid}")]
    [Authorize(Policy = "Permission:Plantation.Update")]
    public async Task<ActionResult<PlantationResponse>> Update(
        Guid id,
        [FromBody] UpdatePlantationRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await plantationService.UpdateAsync(GetActor(), id, request, GetIpAddress(), cancellationToken));
    }

    [HttpPost("api/plantations/{id:guid}/activate")]
    [Authorize(Policy = "Permission:Plantation.Activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        await plantationService.ActivateAsync(GetActor(), id, GetIpAddress(), cancellationToken);
        return NoContent();
    }

    [HttpPost("api/plantations/{id:guid}/terminate")]
    [Authorize(Policy = "Permission:Plantation.Terminate")]
    public async Task<IActionResult> Terminate(
        Guid id,
        [FromBody] TerminatePlantationRequest request,
        CancellationToken cancellationToken)
    {
        await plantationService.TerminateAsync(GetActor(), id, request, GetIpAddress(), cancellationToken);
        return NoContent();
    }

    [HttpPost("api/plantations/{id:guid}/archive")]
    [Authorize(Policy = "Permission:Plantation.Update")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        await plantationService.ArchiveAsync(GetActor(), id, GetIpAddress(), cancellationToken);
        return NoContent();
    }

    private PlantationActor GetActor()
    {
        var userIdValue = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        var organizationIdValue = User.FindFirstValue(AuthorizationConstants.OrganizationIdClaimType);
        if (!Guid.TryParse(userIdValue, out var userId) || !Guid.TryParse(organizationIdValue, out var organizationId))
        {
            throw new UnauthorizedAccessException("The access token is invalid.");
        }

        return new PlantationActor(userId, organizationId);
    }

    private string? GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
