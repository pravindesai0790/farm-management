using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FarmManagement.Application.Common.Constants;
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
    [HttpPost("organizations")]
    [Authorize(Policy = "Permission:Organization.Create")]
    [ProducesResponseType(typeof(OrganizationResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<OrganizationResponse>> Create(
        [FromBody] CreateOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await organizationService.CreateAsync(
            GetActor(),
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
        return Ok(await organizationService.GetAsync(GetActor(), cancellationToken));
    }

    [HttpPut("organization")]
    [Authorize(Policy = "Permission:Organization.Update")]
    [ProducesResponseType(typeof(OrganizationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrganizationResponse>> Update(
        [FromBody] UpdateOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await organizationService.UpdateAsync(
            GetActor(),
            request,
            GetIpAddress(),
            cancellationToken));
    }

    [HttpPatch("organization/activate")]
    [Authorize(Policy = "Permission:Organization.Activate")]
    public async Task<IActionResult> Activate(CancellationToken cancellationToken)
    {
        await organizationService.ActivateAsync(GetActor(), GetIpAddress(), cancellationToken);
        return NoContent();
    }

    [HttpPatch("organization/deactivate")]
    [Authorize(Policy = "Permission:Organization.Deactivate")]
    public async Task<IActionResult> Deactivate(CancellationToken cancellationToken)
    {
        await organizationService.DeactivateAsync(GetActor(), GetIpAddress(), cancellationToken);
        return NoContent();
    }

    private OrganizationActor GetActor()
    {
        var userIdValue = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        var organizationIdValue = User.FindFirstValue(AuthorizationConstants.OrganizationIdClaimType);
        if (!Guid.TryParse(userIdValue, out var userId) ||
            !Guid.TryParse(organizationIdValue, out var organizationId))
        {
            throw new UnauthorizedAccessException("The access token is invalid.");
        }

        var canManageAllOrganizations = User.Claims.Any(claim =>
            claim.Type == AuthorizationConstants.OrganizationScopeClaimType &&
            string.Equals(
                claim.Value,
                AuthorizationConstants.AllOrganizationsScope,
                StringComparison.Ordinal));

        return new OrganizationActor(userId, organizationId, canManageAllOrganizations);
    }

    private string? GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
