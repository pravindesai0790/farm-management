using FarmManagement.Application.DTOs.Roles;
using FarmManagement.Application.Interfaces.Roles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmManagement.API.Controllers;

[ApiController]
[Route("api/permissions")]
[Authorize]
public sealed class PermissionsController(
    IRoleAdministrationService roleAdministrationService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Permission:Permissions.View")]
    public async Task<ActionResult<IReadOnlyList<PermissionResponse>>> List(
        CancellationToken cancellationToken)
    {
        var result = await roleAdministrationService.ListPermissionsAsync(cancellationToken);
        return Ok(result);
    }
}
