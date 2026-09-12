using FarmManagement.API.Helpers;
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
        Ok(await masterDataService.ListUnitsAsync(GetUserContext(), category, cancellationToken));

    [HttpGet("farm-ownership-types")]
    [Authorize(Policy = "Permission:Farm.View")]
    public async Task<IActionResult> ListFarmOwnershipTypes(CancellationToken cancellationToken) =>
        Ok(await masterDataService.ListFarmOwnershipTypesAsync(GetUserContext(), cancellationToken));

    [HttpGet("plantation-end-reasons")]
    [Authorize(Policy = "Permission:PlantationEndReason.View")]
    public async Task<IActionResult> ListPlantationEndReasons(CancellationToken cancellationToken) =>
        Ok(await masterDataService.ListPlantationEndReasonsAsync(GetUserContext(), cancellationToken));

    [HttpGet("cycle-cancellation-reasons")]
    [Authorize(Policy = "Permission:CropCycle.View")]
    public async Task<IActionResult> ListCycleCancellationReasons(CancellationToken cancellationToken) =>
        Ok(await masterDataService.ListCycleCancellationReasonsAsync(GetUserContext(), cancellationToken));

    [HttpGet("currencies")]
    public async Task<IActionResult> ListCurrencies(CancellationToken cancellationToken) =>
        Ok(await masterDataService.ListCurrenciesAsync(GetUserContext(), cancellationToken));

    private MasterDataActor GetUserContext() => UserContextHelper.GetUserContext<MasterDataActor>(User);
}
