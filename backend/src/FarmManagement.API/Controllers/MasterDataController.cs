using FarmManagement.API.Helpers;
using FarmManagement.Application.DTOs.MasterData;
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

    [HttpGet("product-types")]
    [Authorize(Policy = "Permission:ProductType.View")]
    public async Task<IActionResult> ListProductTypes([FromQuery] bool includeInactive = false, CancellationToken cancellationToken = default) =>
        Ok(await masterDataService.ListProductTypesAsync(GetUserContext(), includeInactive, cancellationToken));

    [HttpPost("product-types")]
    [Authorize(Policy = "Permission:ProductType.Create")]
    public async Task<IActionResult> CreateProductType([FromBody] CreateProductTypeRequest request, CancellationToken cancellationToken)
    {
        var result = await masterDataService.CreateProductTypeAsync(GetUserContext(), request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("product-types/{id:guid}")]
    [Authorize(Policy = "Permission:ProductType.Update")]
    public async Task<IActionResult> UpdateProductType(Guid id, [FromBody] UpdateProductTypeRequest request, CancellationToken cancellationToken) =>
        Ok(await masterDataService.UpdateProductTypeAsync(GetUserContext(), id, request, cancellationToken));

    [HttpPost("product-types/{id:guid}/activate")]
    [HttpPatch("product-types/{id:guid}/activate")]
    [Authorize(Policy = "Permission:ProductType.Activate")]
    public async Task<IActionResult> ActivateProductType(Guid id, CancellationToken cancellationToken)
    {
        await masterDataService.ActivateProductTypeAsync(GetUserContext(), id, cancellationToken);
        return NoContent();
    }

    [HttpPost("product-types/{id:guid}/deactivate")]
    [HttpPatch("product-types/{id:guid}/deactivate")]
    [Authorize(Policy = "Permission:ProductType.Deactivate")]
    public async Task<IActionResult> DeactivateProductType(Guid id, CancellationToken cancellationToken)
    {
        await masterDataService.DeactivateProductTypeAsync(GetUserContext(), id, cancellationToken);
        return NoContent();
    }

    [HttpGet("targets")]
    [Authorize(Policy = "Permission:Target.View")]
    public async Task<IActionResult> ListTargets([FromQuery] string? type, [FromQuery] bool includeInactive = false, CancellationToken cancellationToken = default) =>
        Ok(await masterDataService.ListTargetsAsync(GetUserContext(), type, includeInactive, cancellationToken));

    [HttpPost("targets")]
    [Authorize(Policy = "Permission:Target.Create")]
    public async Task<IActionResult> CreateTarget([FromBody] CreateTargetRequest request, CancellationToken cancellationToken)
    {
        var result = await masterDataService.CreateTargetAsync(GetUserContext(), request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("targets/{id:guid}")]
    [Authorize(Policy = "Permission:Target.Update")]
    public async Task<IActionResult> UpdateTarget(Guid id, [FromBody] UpdateTargetRequest request, CancellationToken cancellationToken) =>
        Ok(await masterDataService.UpdateTargetAsync(GetUserContext(), id, request, cancellationToken));

    [HttpPost("targets/{id:guid}/activate")]
    [HttpPatch("targets/{id:guid}/activate")]
    [Authorize(Policy = "Permission:Target.Activate")]
    public async Task<IActionResult> ActivateTarget(Guid id, CancellationToken cancellationToken)
    {
        await masterDataService.ActivateTargetAsync(GetUserContext(), id, cancellationToken);
        return NoContent();
    }

    [HttpPost("targets/{id:guid}/deactivate")]
    [HttpPatch("targets/{id:guid}/deactivate")]
    [Authorize(Policy = "Permission:Target.Deactivate")]
    public async Task<IActionResult> DeactivateTarget(Guid id, CancellationToken cancellationToken)
    {
        await masterDataService.DeactivateTargetAsync(GetUserContext(), id, cancellationToken);
        return NoContent();
    }

    [HttpGet("application-methods")]
    [Authorize(Policy = "Permission:ApplicationMethod.View")]
    public async Task<IActionResult> ListApplicationMethods([FromQuery] bool includeInactive = false, CancellationToken cancellationToken = default) =>
        Ok(await masterDataService.ListApplicationMethodsAsync(GetUserContext(), includeInactive, cancellationToken));

    [HttpPost("application-methods")]
    [Authorize(Policy = "Permission:ApplicationMethod.Create")]
    public async Task<IActionResult> CreateApplicationMethod([FromBody] CreateApplicationMethodRequest request, CancellationToken cancellationToken)
    {
        var result = await masterDataService.CreateApplicationMethodAsync(GetUserContext(), request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("application-methods/{id:guid}")]
    [Authorize(Policy = "Permission:ApplicationMethod.Update")]
    public async Task<IActionResult> UpdateApplicationMethod(Guid id, [FromBody] UpdateApplicationMethodRequest request, CancellationToken cancellationToken) =>
        Ok(await masterDataService.UpdateApplicationMethodAsync(GetUserContext(), id, request, cancellationToken));

    [HttpPost("application-methods/{id:guid}/activate")]
    [HttpPatch("application-methods/{id:guid}/activate")]
    [Authorize(Policy = "Permission:ApplicationMethod.Activate")]
    public async Task<IActionResult> ActivateApplicationMethod(Guid id, CancellationToken cancellationToken)
    {
        await masterDataService.ActivateApplicationMethodAsync(GetUserContext(), id, cancellationToken);
        return NoContent();
    }

    [HttpPost("application-methods/{id:guid}/deactivate")]
    [HttpPatch("application-methods/{id:guid}/deactivate")]
    [Authorize(Policy = "Permission:ApplicationMethod.Deactivate")]
    public async Task<IActionResult> DeactivateApplicationMethod(Guid id, CancellationToken cancellationToken)
    {
        await masterDataService.DeactivateApplicationMethodAsync(GetUserContext(), id, cancellationToken);
        return NoContent();
    }

    private MasterDataActor GetUserContext() => UserContextHelper.GetUserContext<MasterDataActor>(User);

}
