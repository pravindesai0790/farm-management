using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FarmManagement.Application.Common.Constants;
using FarmManagement.Application.DTOs.Crops;
using FarmManagement.Application.Interfaces.Crops;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmManagement.API.Controllers;

[ApiController]
[Route("api/crop-lifecycle-templates")]
[Authorize]
public sealed class CropLifecycleTemplatesController(ICropLifecycleTemplateService lifecycleService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Permission:CropLifecycleTemplate.View")]
    public async Task<ActionResult<CropLifecycleTemplateListResponse>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? cropId = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default) =>
        Ok(await lifecycleService.ListAsync(GetActor(), page, pageSize, cropId, isActive, cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Permission:CropLifecycleTemplate.View")]
    public async Task<ActionResult<CropLifecycleTemplateResponse>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await lifecycleService.GetAsync(GetActor(), id, cancellationToken));

    [HttpGet("{templateId:guid}/stages/{stageId:guid}")]
    [Authorize(Policy = "Permission:CropLifecycleTemplate.View")]
    public async Task<ActionResult<CropLifecycleStageResponse>> GetStage(
        Guid templateId,
        Guid stageId,
        CancellationToken cancellationToken) =>
        Ok(await lifecycleService.GetStageAsync(GetActor(), templateId, stageId, cancellationToken));

    [HttpPost]
    [Authorize(Policy = "Permission:CropLifecycleTemplate.Create")]
    public async Task<ActionResult<CropLifecycleTemplateResponse>> Create(
        [FromBody] CreateCropLifecycleTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await lifecycleService.CreateAsync(GetActor(), request, GetIpAddress(), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Permission:CropLifecycleTemplate.Update")]
    public async Task<ActionResult<CropLifecycleTemplateResponse>> Update(
        Guid id,
        [FromBody] UpdateCropLifecycleTemplateRequest request,
        CancellationToken cancellationToken) =>
        Ok(await lifecycleService.UpdateAsync(GetActor(), id, request, GetIpAddress(), cancellationToken));

    [HttpPatch("{id:guid}/activate")]
    [Authorize(Policy = "Permission:CropLifecycleTemplate.Activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        await lifecycleService.ActivateAsync(GetActor(), id, GetIpAddress(), cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:guid}/deactivate")]
    [Authorize(Policy = "Permission:CropLifecycleTemplate.Deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await lifecycleService.DeactivateAsync(GetActor(), id, GetIpAddress(), cancellationToken);
        return NoContent();
    }

    [HttpPost("{templateId:guid}/stages")]
    [Authorize(Policy = "Permission:CropLifecycleTemplate.Update")]
    public async Task<ActionResult<CropLifecycleStageResponse>> CreateStage(
        Guid templateId,
        [FromBody] CreateCropLifecycleStageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await lifecycleService.CreateStageAsync(GetActor(), templateId, request, GetIpAddress(), cancellationToken);
        return CreatedAtAction(nameof(GetStage), new { templateId, stageId = result.Id }, result);
    }

    [HttpPut("{templateId:guid}/stages/{stageId:guid}")]
    [Authorize(Policy = "Permission:CropLifecycleTemplate.Update")]
    public async Task<ActionResult<CropLifecycleStageResponse>> UpdateStage(
        Guid templateId,
        Guid stageId,
        [FromBody] UpdateCropLifecycleStageRequest request,
        CancellationToken cancellationToken) =>
        Ok(await lifecycleService.UpdateStageAsync(GetActor(), templateId, stageId, request, GetIpAddress(), cancellationToken));

    [HttpPatch("{templateId:guid}/stages/{stageId:guid}/activate")]
    [Authorize(Policy = "Permission:CropLifecycleTemplate.Activate")]
    public async Task<IActionResult> ActivateStage(Guid templateId, Guid stageId, CancellationToken cancellationToken)
    {
        await lifecycleService.ActivateStageAsync(GetActor(), templateId, stageId, GetIpAddress(), cancellationToken);
        return NoContent();
    }

    [HttpPatch("{templateId:guid}/stages/{stageId:guid}/deactivate")]
    [Authorize(Policy = "Permission:CropLifecycleTemplate.Deactivate")]
    public async Task<IActionResult> DeactivateStage(Guid templateId, Guid stageId, CancellationToken cancellationToken)
    {
        await lifecycleService.DeactivateStageAsync(GetActor(), templateId, stageId, GetIpAddress(), cancellationToken);
        return NoContent();
    }

    private CropLifecycleTemplateActor GetActor()
    {
        var userIdValue = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        var organizationIdValue = User.FindFirstValue(AuthorizationConstants.OrganizationIdClaimType);
        if (!Guid.TryParse(userIdValue, out var userId) || !Guid.TryParse(organizationIdValue, out var organizationId))
        {
            throw new UnauthorizedAccessException("The access token is invalid.");
        }

        return new CropLifecycleTemplateActor(
            userId,
            organizationId,
            User.Claims.Any(claim => claim.Type == AuthorizationConstants.RoleClaimType &&
                claim.Value == AuthorizationConstants.SuperAdminRoleName));
    }

    private string? GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
