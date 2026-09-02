using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FarmManagement.Application.Common.Constants;
using FarmManagement.Application.DTOs.Farms;
using FarmManagement.Application.Interfaces.Farms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmManagement.API.Controllers;

[ApiController]
[Authorize]
public sealed class FarmAreasController(IFarmAreaService farmAreaService) : ControllerBase
{
    [HttpGet("api/farms/{farmId:guid}/areas")]
    [Authorize(Policy = "Permission:FarmArea.View")]
    public async Task<ActionResult<IReadOnlyList<FarmAreaResponse>>> List(
        Guid farmId,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        return Ok(await farmAreaService.ListAsync(GetActor(), farmId, isActive, cancellationToken));
    }

    [HttpGet("api/farm-areas/{id:guid}")]
    [Authorize(Policy = "Permission:FarmArea.View")]
    public async Task<ActionResult<FarmAreaResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await farmAreaService.GetAsync(GetActor(), id, cancellationToken));
    }

    [HttpPost("api/farm-areas")]
    [Authorize(Policy = "Permission:FarmArea.Create")]
    public async Task<ActionResult<FarmAreaResponse>> Create(
        [FromBody] CreateFarmAreaRequest request,
        CancellationToken cancellationToken)
    {
        var result = await farmAreaService.CreateAsync(GetActor(), request, GetIpAddress(), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("api/farm-areas/{id:guid}")]
    [Authorize(Policy = "Permission:FarmArea.Update")]
    public async Task<ActionResult<FarmAreaResponse>> Update(
        Guid id,
        [FromBody] UpdateFarmAreaRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await farmAreaService.UpdateAsync(GetActor(), id, request, GetIpAddress(), cancellationToken));
    }

    [HttpPatch("api/farm-areas/{id:guid}/activate")]
    [Authorize(Policy = "Permission:FarmArea.Activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        await farmAreaService.ActivateAsync(GetActor(), id, GetIpAddress(), cancellationToken);
        return NoContent();
    }

    [HttpPatch("api/farm-areas/{id:guid}/deactivate")]
    [Authorize(Policy = "Permission:FarmArea.Deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await farmAreaService.DeactivateAsync(GetActor(), id, GetIpAddress(), cancellationToken);
        return NoContent();
    }

    [HttpGet("api/farm-areas/{id:guid}/availability")]
    [Authorize(Policy = "Permission:FarmArea.View")]
    public async Task<ActionResult<FarmAreaAvailabilityResponse>> GetAvailability(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await farmAreaService.GetAvailabilityAsync(GetActor(), id, cancellationToken));
    }

    private FarmActor GetActor()
    {
        var userIdValue = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        var organizationIdValue = User.FindFirstValue(AuthorizationConstants.OrganizationIdClaimType);
        if (!Guid.TryParse(userIdValue, out var userId) ||
            !Guid.TryParse(organizationIdValue, out var organizationId))
        {
            throw new UnauthorizedAccessException("The access token is invalid.");
        }

        return new FarmActor(userId, organizationId);
    }

    private string? GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
