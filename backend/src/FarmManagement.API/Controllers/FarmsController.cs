using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FarmManagement.Application.Common.Constants;
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
    public async Task<ActionResult<FarmListResponse>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        return Ok(await farmService.ListAsync(GetActor(), page, pageSize, search, isActive, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Permission:Farm.View")]
    public async Task<ActionResult<FarmResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await farmService.GetAsync(GetActor(), id, cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = "Permission:Farm.Create")]
    public async Task<ActionResult<FarmResponse>> Create(
        [FromBody] CreateFarmRequest request,
        CancellationToken cancellationToken)
    {
        var result = await farmService.CreateAsync(GetActor(), request, GetIpAddress(), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Permission:Farm.Update")]
    public async Task<ActionResult<FarmResponse>> Update(
        Guid id,
        [FromBody] UpdateFarmRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await farmService.UpdateAsync(GetActor(), id, request, GetIpAddress(), cancellationToken));
    }

    [HttpPatch("{id:guid}/activate")]
    [Authorize(Policy = "Permission:Farm.Activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        await farmService.ActivateAsync(GetActor(), id, GetIpAddress(), cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:guid}/deactivate")]
    [Authorize(Policy = "Permission:Farm.Deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await farmService.DeactivateAsync(GetActor(), id, GetIpAddress(), cancellationToken);
        return NoContent();
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
