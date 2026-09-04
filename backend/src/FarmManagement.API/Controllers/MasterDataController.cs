using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FarmManagement.Application.Common.Constants;
using FarmManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmManagement.API.Controllers;

[ApiController]
[Route("api/master-data")]
[Authorize]
public sealed class MasterDataController(IMasterDataService masterDataService) : ControllerBase
{
    [HttpGet("units")]
    [Authorize(Policy = "Permission:Unit.View")]
    public async Task<IActionResult> ListUnits([FromQuery] string? category, CancellationToken cancellationToken) =>
        Ok(await masterDataService.ListUnitsAsync(GetActor(), category, cancellationToken));

    [HttpGet("farm-ownership-types")]
    [Authorize(Policy = "Permission:Farm.View")]
    public async Task<IActionResult> ListFarmOwnershipTypes(CancellationToken cancellationToken) =>
        Ok(await masterDataService.ListFarmOwnershipTypesAsync(GetActor(), cancellationToken));

    [HttpGet("plantation-end-reasons")]
    [Authorize(Policy = "Permission:PlantationEndReason.View")]
    public async Task<IActionResult> ListPlantationEndReasons(CancellationToken cancellationToken) =>
        Ok(await masterDataService.ListPlantationEndReasonsAsync(GetActor(), cancellationToken));

    [HttpGet("cycle-cancellation-reasons")]
    [Authorize(Policy = "Permission:CropCycle.View")]
    public async Task<IActionResult> ListCycleCancellationReasons(CancellationToken cancellationToken) =>
        Ok(await masterDataService.ListCycleCancellationReasonsAsync(GetActor(), cancellationToken));

    private MasterDataActor GetActor()
    {
        var userIdValue = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        var organizationIdValue = User.FindFirstValue(AuthorizationConstants.OrganizationIdClaimType);
        if (!Guid.TryParse(userIdValue, out var userId) || !Guid.TryParse(organizationIdValue, out var organizationId))
        {
            throw new UnauthorizedAccessException("The access token is invalid.");
        }
        return new MasterDataActor(userId, organizationId);
    }
}
