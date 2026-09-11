using FarmManagement.API.Helpers;
using FarmManagement.Application.DTOs.Organizations;
using FarmManagement.Application.Interfaces.Organizations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmManagement.API.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public sealed class OrganizationController(IOrganizationService organizationService) : ControllerBase
{
    [HttpGet("organizations")]
    [Authorize(Policy = "Permission:Organization.View")]
    [ProducesResponseType(typeof(OrganizationListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrganizationListResponse>> List(CancellationToken cancellationToken)
    {
        return Ok(await organizationService.ListAsync(GetUserContext(), cancellationToken));
    }

    [HttpPost("organizations")]
    [Authorize(Policy = "Permission:Organization.Create")]
    [ProducesResponseType(typeof(OrganizationResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<OrganizationResponse>> Create(
        [FromBody] CreateOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await organizationService.CreateAsync(
            GetUserContext(),
            request,
            GetIpAddress(),
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpGet("organization")]
    [Authorize(Policy = "Permission:Organization.View")]
    [ProducesResponseType(typeof(OrganizationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrganizationResponse>> Get(CancellationToken cancellationToken)
    {
        return Ok(await organizationService.GetAsync(GetUserContext(), cancellationToken));
    }

    [HttpPut("organization")]
    [Authorize(Policy = "Permission:Organization.Update")]
    [ProducesResponseType(typeof(OrganizationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrganizationResponse>> Update(
        [FromBody] UpdateOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await organizationService.UpdateAsync(
            GetUserContext(),
            request,
            GetIpAddress(),
            cancellationToken));
    }

    [HttpPatch("organization/activate")]
    [Authorize(Policy = "Permission:Organization.Activate")]
    public async Task<IActionResult> Activate(CancellationToken cancellationToken)
    {
        await organizationService.ActivateAsync(GetUserContext(), GetIpAddress(), cancellationToken);
        return NoContent();
    }

    [HttpPatch("organization/deactivate")]
    [Authorize(Policy = "Permission:Organization.Deactivate")]
    public async Task<IActionResult> Deactivate(CancellationToken cancellationToken)
    {
        await organizationService.DeactivateAsync(GetUserContext(), GetIpAddress(), cancellationToken);
        return NoContent();
    }

    private OrganizationActor GetUserContext() => UserContextHelper.GetUserContext<OrganizationActor>(User);

    private string? GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
