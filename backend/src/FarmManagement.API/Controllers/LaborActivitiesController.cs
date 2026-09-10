using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FarmManagement.Application.Common.Constants;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.LaborActivities;
using FarmManagement.Application.Interfaces.LaborActivities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmManagement.API.Controllers;

[ApiController]
[Route("api/labor-activities")]
[Authorize]
public sealed class LaborActivitiesController(ILaborActivityService activityService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Permission:LaborActivity.View")]
    public async Task<ActionResult<PagedResponse<LaborActivityResponse>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? farmId = null,
        [FromQuery] Guid? farmAreaId = null,
        [FromQuery] Guid? plantationId = null,
        [FromQuery] Guid? cropCycleId = null,
        [FromQuery] Guid? activityTypeId = null,
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default) =>
        Ok(await activityService.ListAsync(
            GetActor(),
            page,
            pageSize,
            farmId,
            farmAreaId,
            plantationId,
            cropCycleId,
            activityTypeId,
            fromDate,
            toDate,
            status,
            cancellationToken));

    [HttpGet("types")]
    [Authorize(Policy = "Permission:LaborActivity.View")]
    public async Task<ActionResult<IReadOnlyList<NamedReferenceResponse>>> ListTypes(
        CancellationToken cancellationToken = default) =>
        Ok(await activityService.ListTypesAsync(GetActor(), cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Permission:LaborActivity.View")]
    public async Task<ActionResult<LaborActivityResponse>> Get(
        Guid id,
        CancellationToken cancellationToken = default) =>
        Ok(await activityService.GetAsync(GetActor(), id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = "Permission:LaborActivity.Create")]
    public async Task<ActionResult<LaborActivityResponse>> Create(
        [FromBody] CreateLaborActivityRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await activityService.CreateAsync(GetActor(), request, GetIpAddress(), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Permission:LaborActivity.Update")]
    public async Task<ActionResult<LaborActivityResponse>> Update(
        Guid id,
        [FromBody] UpdateLaborActivityRequest request,
        CancellationToken cancellationToken = default) =>
        Ok(await activityService.UpdateAsync(GetActor(), id, request, GetIpAddress(), cancellationToken));

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = "Permission:LaborActivity.Cancel")]
    public async Task<IActionResult> Cancel(
        Guid id,
        [FromBody] CancelLaborActivityRequest request,
        CancellationToken cancellationToken = default)
    {
        await activityService.CancelAsync(GetActor(), id, request, GetIpAddress(), cancellationToken);
        return NoContent();
    }

    private LaborActivityActor GetActor()
    {
        var userIdValue = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        var organizationIdValue = User.FindFirstValue(AuthorizationConstants.OrganizationIdClaimType);
        if (!Guid.TryParse(userIdValue, out var userId) || !Guid.TryParse(organizationIdValue, out var organizationId))
        {
            throw new UnauthorizedAccessException("The access token is invalid.");
        }

        return new LaborActivityActor(userId, organizationId);
    }

    private string? GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
