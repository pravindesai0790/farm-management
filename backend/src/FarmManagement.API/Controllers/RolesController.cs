using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FarmManagement.Application.Common.Constants;
using FarmManagement.Application.DTOs.Roles;
using FarmManagement.Application.Interfaces.Roles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmManagement.API.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize]
public sealed class RolesController(IRoleAdministrationService roleAdministrationService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Permission:Roles.View")]
    public async Task<ActionResult<IReadOnlyList<RoleResponse>>> List(
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var result = await roleAdministrationService.ListRolesAsync(isActive, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Permission:Roles.View")]
    public async Task<ActionResult<RoleResponse>> Get(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await roleAdministrationService.GetRoleAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "Permission:Roles.Create")]
    public async Task<ActionResult<RoleResponse>> Create(
        [FromBody] CreateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await roleAdministrationService.CreateRoleAsync(
            GetActor(),
            request,
            GetIpAddress(),
            cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Permission:Roles.Update")]
    public async Task<ActionResult<RoleResponse>> Update(
        Guid id,
        [FromBody] UpdateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await roleAdministrationService.UpdateRoleAsync(
            GetActor(),
            id,
            request,
            GetIpAddress(),
            cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/activate")]
    [Authorize(Policy = "Permission:Roles.Activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        await roleAdministrationService.ActivateRoleAsync(
            GetActor(),
            id,
            GetIpAddress(),
            cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = "Permission:Roles.Deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await roleAdministrationService.DeactivateRoleAsync(
            GetActor(),
            id,
            GetIpAddress(),
            cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/permissions")]
    [Authorize(Policy = "Permission:Roles.ManagePermissions")]
    public async Task<ActionResult<RoleResponse>> UpdatePermissions(
        Guid id,
        [FromBody] UpdateRolePermissionsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await roleAdministrationService.UpdateRolePermissionsAsync(
            GetActor(),
            id,
            request,
            GetIpAddress(),
            cancellationToken);
        return Ok(result);
    }

    private RoleAdministrationActor GetActor()
    {
        var userIdValue = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        var organizationIdValue = User.FindFirstValue(AuthorizationConstants.OrganizationIdClaimType);
        if (!Guid.TryParse(userIdValue, out var userId) ||
            !Guid.TryParse(organizationIdValue, out var organizationId))
        {
            throw new UnauthorizedAccessException("The access token is invalid.");
        }

        return new RoleAdministrationActor(userId, organizationId);
    }

    private string? GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
