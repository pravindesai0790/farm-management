using FarmManagement.API.Helpers;
using FarmManagement.Application.Common.Models;
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
    public async Task<ActionResult<PagedResponse<CropLifecycleTemplateResponse>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? cropId = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default) =>
        Ok(await lifecycleService.ListAsync(GetUserContext(), page, pageSize, cropId, isActive, cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Permission:CropLifecycleTemplate.View")]
    public async Task<ActionResult<CropLifecycleTemplateResponse>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await lifecycleService.GetAsync(GetUserContext(), id, cancellationToken));

    [HttpGet("{templateId:guid}/stages/{stageId:guid}")]
    [Authorize(Policy = "Permission:CropLifecycleTemplate.View")]
    public async Task<ActionResult<CropLifecycleStageResponse>> GetStage(
        Guid templateId,
        Guid stageId,
        CancellationToken cancellationToken) =>
        Ok(await lifecycleService.GetStageAsync(GetUserContext(), templateId, stageId, cancellationToken));

    [HttpPost]
    [Authorize(Policy = "Permission:CropLifecycleTemplate.Create")]
    public async Task<ActionResult<CropLifecycleTemplateResponse>> Create(
        [FromBody] CreateCropLifecycleTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await lifecycleService.CreateAsync(GetUserContext(), request, GetIpAddress(), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Permission:CropLifecycleTemplate.Update")]
    public async Task<ActionResult<CropLifecycleTemplateResponse>> Update(
        Guid id,
        [FromBody] UpdateCropLifecycleTemplateRequest request,
        CancellationToken cancellationToken) =>
        Ok(await lifecycleService.UpdateAsync(GetUserContext(), id, request, GetIpAddress(), cancellationToken));

    [HttpPatch("{id:guid}/activate")]
    [Authorize(Policy = "Permission:CropLifecycleTemplate.Activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        await lifecycleService.ActivateAsync(GetUserContext(), id, GetIpAddress(), cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:guid}/deactivate")]
    [Authorize(Policy = "Permission:CropLifecycleTemplate.Deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await lifecycleService.DeactivateAsync(GetUserContext(), id, GetIpAddress(), cancellationToken);
        return NoContent();
    }

    [HttpPost("{templateId:guid}/stages")]
    [Authorize(Policy = "Permission:CropLifecycleTemplate.Update")]
    public async Task<ActionResult<CropLifecycleStageResponse>> CreateStage(
        Guid templateId,
        [FromBody] CreateCropLifecycleStageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await lifecycleService.CreateStageAsync(GetUserContext(), templateId, request, GetIpAddress(), cancellationToken);
        return CreatedAtAction(nameof(GetStage), new { templateId, stageId = result.Id }, result);
    }

    [HttpPut("{templateId:guid}/stages/{stageId:guid}")]
    [Authorize(Policy = "Permission:CropLifecycleTemplate.Update")]
    public async Task<ActionResult<CropLifecycleStageResponse>> UpdateStage(
        Guid templateId,
        Guid stageId,
        [FromBody] UpdateCropLifecycleStageRequest request,
        CancellationToken cancellationToken) =>
        Ok(await lifecycleService.UpdateStageAsync(GetUserContext(), templateId, stageId, request, GetIpAddress(), cancellationToken));

    [HttpPatch("{templateId:guid}/stages/{stageId:guid}/activate")]
    [Authorize(Policy = "Permission:CropLifecycleTemplate.Activate")]
    public async Task<IActionResult> ActivateStage(Guid templateId, Guid stageId, CancellationToken cancellationToken)
    {
        await lifecycleService.ActivateStageAsync(GetUserContext(), templateId, stageId, GetIpAddress(), cancellationToken);
        return NoContent();
    }

    [HttpPatch("{templateId:guid}/stages/{stageId:guid}/deactivate")]
    [Authorize(Policy = "Permission:CropLifecycleTemplate.Deactivate")]
    public async Task<IActionResult> DeactivateStage(Guid templateId, Guid stageId, CancellationToken cancellationToken)
    {
        await lifecycleService.DeactivateStageAsync(GetUserContext(), templateId, stageId, GetIpAddress(), cancellationToken);
        return NoContent();
    }

    private CropLifecycleTemplateActor GetUserContext() => UserContextHelper.GetUserContext<CropLifecycleTemplateActor>(User);

    private string? GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
