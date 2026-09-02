using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FarmManagement.Application.Common.Constants;
using FarmManagement.Application.DTOs.Crops;
using FarmManagement.Application.Interfaces.Crops;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmManagement.API.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public sealed class CropsController(ICropService cropService) : ControllerBase
{
    [HttpGet("crops")]
    [Authorize(Policy = "Permission:Crop.View")]
    public async Task<ActionResult<CropListResponse>> ListCrops(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null, CancellationToken cancellationToken = default) =>
        Ok(await cropService.ListCropsAsync(GetActor(), page, pageSize, search, isActive, cancellationToken));

    [HttpGet("crops/{id:guid}")]
    [Authorize(Policy = "Permission:Crop.View")]
    public async Task<ActionResult<CropResponse>> GetCrop(Guid id, CancellationToken cancellationToken) =>
        Ok(await cropService.GetCropAsync(GetActor(), id, cancellationToken));

    [HttpPost("crops")]
    [Authorize(Policy = "Permission:Crop.Create")]
    public async Task<ActionResult<CropResponse>> CreateCrop([FromBody] CreateCropRequest request, CancellationToken cancellationToken)
    {
        var result = await cropService.CreateCropAsync(GetActor(), request, GetIpAddress(), cancellationToken);
        return CreatedAtAction(nameof(GetCrop), new { id = result.Id }, result);
    }

    [HttpPut("crops/{id:guid}")]
    [Authorize(Policy = "Permission:Crop.Update")]
    public async Task<ActionResult<CropResponse>> UpdateCrop(Guid id, [FromBody] UpdateCropRequest request, CancellationToken cancellationToken) =>
        Ok(await cropService.UpdateCropAsync(GetActor(), id, request, GetIpAddress(), cancellationToken));

    [HttpPatch("crops/{id:guid}/activate")]
    [Authorize(Policy = "Permission:Crop.Activate")]
    public async Task<IActionResult> ActivateCrop(Guid id, CancellationToken cancellationToken)
    {
        await cropService.ActivateCropAsync(GetActor(), id, GetIpAddress(), cancellationToken);
        return NoContent();
    }

    [HttpPatch("crops/{id:guid}/deactivate")]
    [Authorize(Policy = "Permission:Crop.Deactivate")]
    public async Task<IActionResult> DeactivateCrop(Guid id, CancellationToken cancellationToken)
    {
        await cropService.DeactivateCropAsync(GetActor(), id, GetIpAddress(), cancellationToken);
        return NoContent();
    }

    [HttpGet("crops/{cropId:guid}/varieties")]
    [Authorize(Policy = "Permission:CropVariety.View")]
    public async Task<ActionResult<CropVarietyListResponse>> ListVarieties(
        Guid cropId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default) =>
        Ok(await cropService.ListVarietiesAsync(GetActor(), cropId, page, pageSize, isActive, cancellationToken));

    [HttpGet("crop-varieties/{id:guid}")]
    [Authorize(Policy = "Permission:CropVariety.View")]
    public async Task<ActionResult<CropVarietyResponse>> GetVariety(Guid id, CancellationToken cancellationToken) =>
        Ok(await cropService.GetVarietyAsync(GetActor(), id, cancellationToken));

    [HttpPost("crop-varieties")]
    [Authorize(Policy = "Permission:CropVariety.Create")]
    public async Task<ActionResult<CropVarietyResponse>> CreateVariety([FromBody] CreateCropVarietyRequest request, CancellationToken cancellationToken)
    {
        var result = await cropService.CreateVarietyAsync(GetActor(), request, GetIpAddress(), cancellationToken);
        return CreatedAtAction(nameof(GetVariety), new { id = result.Id }, result);
    }

    [HttpPut("crop-varieties/{id:guid}")]
    [Authorize(Policy = "Permission:CropVariety.Update")]
    public async Task<ActionResult<CropVarietyResponse>> UpdateVariety(Guid id, [FromBody] UpdateCropVarietyRequest request, CancellationToken cancellationToken) =>
        Ok(await cropService.UpdateVarietyAsync(GetActor(), id, request, GetIpAddress(), cancellationToken));

    [HttpPatch("crop-varieties/{id:guid}/activate")]
    [Authorize(Policy = "Permission:CropVariety.Activate")]
    public async Task<IActionResult> ActivateVariety(Guid id, CancellationToken cancellationToken)
    {
        await cropService.ActivateVarietyAsync(GetActor(), id, GetIpAddress(), cancellationToken);
        return NoContent();
    }

    [HttpPatch("crop-varieties/{id:guid}/deactivate")]
    [Authorize(Policy = "Permission:CropVariety.Deactivate")]
    public async Task<IActionResult> DeactivateVariety(Guid id, CancellationToken cancellationToken)
    {
        await cropService.DeactivateVarietyAsync(GetActor(), id, GetIpAddress(), cancellationToken);
        return NoContent();
    }

    private CropActor GetActor()
    {
        var userIdValue = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        var organizationIdValue = User.FindFirstValue(AuthorizationConstants.OrganizationIdClaimType);
        if (!Guid.TryParse(userIdValue, out var userId) || !Guid.TryParse(organizationIdValue, out var organizationId))
            throw new UnauthorizedAccessException("The access token is invalid.");

        return new CropActor(
            userId,
            organizationId,
            User.Claims.Any(claim => claim.Type == AuthorizationConstants.RoleClaimType &&
                claim.Value == AuthorizationConstants.SuperAdminRoleName));
    }

    private string? GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
