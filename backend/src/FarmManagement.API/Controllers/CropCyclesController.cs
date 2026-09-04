using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FarmManagement.Application.Common.Constants;
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
    public async Task<ActionResult<CropCycleListResponse>> List(
        [FromQuery] Guid? farmId,
        [FromQuery] Guid? farmAreaId,
        [FromQuery] Guid? plantationId,
        [FromQuery] string? status,
        [FromQuery] int? seasonYear,
        CancellationToken cancellationToken = default) =>
        Ok(await cycleService.ListAsync(GetActor(), farmId, farmAreaId, plantationId, status, seasonYear, cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Permission:CropCycle.View")]
    public async Task<ActionResult<CropCycleResponse>> Get(Guid id, CancellationToken cancellationToken = default) =>
        Ok(await cycleService.GetAsync(GetActor(), id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = "Permission:CropCycle.Create")]
    public async Task<ActionResult<CropCycleResponse>> Create(
        [FromBody] CreateCropCycleRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await cycleService.CreateAsync(GetActor(), request, GetIpAddress(), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Permission:CropCycle.Update")]
    public async Task<ActionResult<CropCycleResponse>> Update(
        Guid id,
        [FromBody] UpdateCropCycleRequest request,
        CancellationToken cancellationToken = default) =>
        Ok(await cycleService.UpdateAsync(GetActor(), id, request, GetIpAddress(), cancellationToken));

    [HttpPost("{id:guid}/start")]
    [Authorize(Policy = "Permission:CropCycle.Start")]
    public async Task<IActionResult> Start(
        Guid id,
        [FromBody] StartCropCycleRequest request,
        CancellationToken cancellationToken = default)
    {
        await cycleService.StartAsync(GetActor(), id, request, GetIpAddress(), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/harvest")]
    [Authorize(Policy = "Permission:CropCycle.Complete")]
    public async Task<IActionResult> Harvest(
        Guid id,
        [FromBody] HarvestCropCycleRequest request,
        CancellationToken cancellationToken = default)
    {
        await cycleService.HarvestAsync(GetActor(), id, request, GetIpAddress(), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/complete")]
    [Authorize(Policy = "Permission:CropCycle.Complete")]
    public async Task<IActionResult> Complete(
        Guid id,
        [FromBody] CompleteCropCycleRequest? request,
        CancellationToken cancellationToken = default)
    {
        await cycleService.CompleteAsync(GetActor(), id, request, GetIpAddress(), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = "Permission:CropCycle.Cancel")]
    public async Task<IActionResult> Cancel(
        Guid id,
        [FromBody] CancelCropCycleRequest request,
        CancellationToken cancellationToken = default)
    {
        await cycleService.CancelAsync(GetActor(), id, request, GetIpAddress(), cancellationToken);
        return NoContent();
    }

    private CropCycleActor GetActor()
    {
        var userIdValue = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        var organizationIdValue = User.FindFirstValue(AuthorizationConstants.OrganizationIdClaimType);
        if (!Guid.TryParse(userIdValue, out var userId) || !Guid.TryParse(organizationIdValue, out var organizationId))
        {
            throw new UnauthorizedAccessException("The access token is invalid.");
        }

        return new CropCycleActor(userId, organizationId);
    }

    private string? GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
