using FarmManagement.API.Helpers;
using FarmManagement.Application.DTOs.Fertilizer;
using FarmManagement.Application.Interfaces.Fertilizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmManagement.API.Controllers;

[ApiController]
[Route("api/fertilizer-applications")]
[Authorize]
public sealed class FertilizerApplicationsController(IFertilizerApplicationService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Permission:Fertilizer.View")]
    public async Task<IActionResult> List(
        [FromQuery] FertilizerApplicationListQuery query,
        CancellationToken cancellationToken = default)
    {
        var actor = UserContextHelper.GetUserContext<FertilizerActor>(User);
        var result = await service.ListAsync(actor, query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Permission:Fertilizer.View")]
    public async Task<IActionResult> Get(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var actor = UserContextHelper.GetUserContext<FertilizerActor>(User);
        var result = await service.GetAsync(actor, id, cancellationToken);
        return Ok(result);
    }

    [HttpGet("lookups/products")]
    [HttpGet("lookup/products")]
    [Authorize(Policy = "Permission:Fertilizer.View")]
    public async Task<IActionResult> ListProductsLookup(CancellationToken cancellationToken = default)
    {
        var actor = UserContextHelper.GetUserContext<FertilizerActor>(User);
        var result = await service.ListProductsLookupAsync(actor, cancellationToken);
        return Ok(result);
    }

    [HttpGet("lookups/storage-locations")]
    [HttpGet("lookup/storage-locations")]
    [Authorize(Policy = "Permission:Fertilizer.View")]
    public async Task<IActionResult> ListStorageLocationsLookup(
        [FromQuery] Guid farmId,
        [FromQuery] Guid? inventoryItemId,
        CancellationToken cancellationToken = default)
    {
        var actor = UserContextHelper.GetUserContext<FertilizerActor>(User);
        var result = await service.ListStorageLocationsLookupAsync(actor, farmId, inventoryItemId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("lookups/application-methods")]
    [HttpGet("lookup/application-methods")]
    [Authorize(Policy = "Permission:Fertilizer.View")]
    public async Task<IActionResult> ListApplicationMethodsLookup(CancellationToken cancellationToken = default)
    {
        var actor = UserContextHelper.GetUserContext<FertilizerActor>(User);
        var result = await service.ListApplicationMethodsLookupAsync(actor, cancellationToken);
        return Ok(result);
    }

    [HttpGet("lookups/irrigations")]
    [HttpGet("lookup/irrigations")]
    [Authorize(Policy = "Permission:Fertilizer.View")]
    public async Task<IActionResult> ListIrrigationsLookup(
        [FromQuery] Guid? farmId,
        [FromQuery] Guid? farmAreaId,
        CancellationToken cancellationToken = default)
    {
        var actor = UserContextHelper.GetUserContext<FertilizerActor>(User);
        var result = await service.ListIrrigationsLookupAsync(actor, farmId, farmAreaId, cancellationToken);
        return Ok(result);
    }
}
