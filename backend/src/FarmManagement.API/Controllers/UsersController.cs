using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FarmManagement.Application.Common.Constants;
using FarmManagement.Application.DTOs.Users;
using FarmManagement.Application.Interfaces.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmManagement.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController(IUserAdministrationService userAdministrationService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Permission:Users.View")]
    public async Task<ActionResult<PagedResponse<UserResponse>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var result = await userAdministrationService.ListAsync(
            GetActor(),
            page,
            pageSize,
            search,
            isActive,
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Permission:Users.View")]
    public async Task<ActionResult<UserResponse>> Get(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await userAdministrationService.GetAsync(GetActor(), id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "Permission:Users.Create")]
    public async Task<ActionResult<UserResponse>> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await userAdministrationService.CreateAsync(
            GetActor(),
            request,
            GetIpAddress(),
            cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Permission:Users.Update")]
    public async Task<ActionResult<UserResponse>> Update(
        Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await userAdministrationService.UpdateAsync(
            GetActor(),
            id,
            request,
            GetIpAddress(),
            cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/activate")]
    [Authorize(Policy = "Permission:Users.Activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        await userAdministrationService.ActivateAsync(
            GetActor(),
            id,
            GetIpAddress(),
            cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = "Permission:Users.Deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await userAdministrationService.DeactivateAsync(
            GetActor(),
            id,
            GetIpAddress(),
            cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/unlock")]
    [Authorize(Policy = "Permission:Users.Unlock")]
    public async Task<IActionResult> Unlock(Guid id, CancellationToken cancellationToken)
    {
        await userAdministrationService.UnlockAsync(
            GetActor(),
            id,
            GetIpAddress(),
            cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/roles")]
    [Authorize(Policy = "Permission:Users.ManageRoles")]
    public async Task<ActionResult<UserResponse>> AssignRoles(
        Guid id,
        [FromBody] AssignUserRolesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await userAdministrationService.AssignRolesAsync(
            GetActor(),
            id,
            request,
            GetIpAddress(),
            cancellationToken);
        return Ok(result);
    }

    private UserAdministrationActor GetActor()
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
        return new UserAdministrationActor(userId, organizationId, canManageAllOrganizations);
    }

    private string? GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
