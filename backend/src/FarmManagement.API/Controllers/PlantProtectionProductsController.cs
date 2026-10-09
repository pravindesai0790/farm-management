using FarmManagement.API.Helpers;
using FarmManagement.Application.DTOs.PlantProtection;
using FarmManagement.Application.DTOs.Sprays;
using FarmManagement.Application.Interfaces.PlantProtection;
using FarmManagement.Application.Interfaces.Sprays;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace FarmManagement.API.Controllers;

[ApiController]
[Route("api/plant-protection-products")]
[Authorize]
public sealed class PlantProtectionProductsController(IPlantProtectionProductService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Permission:PlantProtectionProduct.View")]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] Guid? productTypeId = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var result = await service.ListAsync(
            GetUserContext(),
            page,
            pageSize,
            search,
            productTypeId,
            isActive,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("lookup")]
    [Authorize(Policy = "Permission:PlantProtectionProduct.View")]
    public async Task<IActionResult> Lookup([FromServices] ISprayService sprayService, CancellationToken cancellationToken)
    {
        var actor = UserContextHelper.GetUserContext<SprayActor>(User);
        var result = await sprayService.ListProductsLookupAsync(actor, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]

    [Authorize(Policy = "Permission:PlantProtectionProduct.View")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(GetUserContext(), id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "Permission:PlantProtectionProduct.Create")]
    public async Task<IActionResult> Create(
        [FromBody] CreatePlantProtectionProductRequest request,
        CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await service.CreateAsync(
            GetUserContext(),
            request,
            ipAddress,
            cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Permission:PlantProtectionProduct.Update")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdatePlantProtectionProductRequest request,
        CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await service.UpdateAsync(
            GetUserContext(),
            id,
            request,
            ipAddress,
            cancellationToken);

        return Ok(result);
    }

    [HttpPatch("{id:guid}/activate")]
    [Authorize(Policy = "Permission:PlantProtectionProduct.Activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await service.ActivateAsync(
            GetUserContext(),
            id,
            ipAddress,
            cancellationToken);

        return result ? NoContent() : BadRequest(new { message = "Plant protection product is already active." });
    }

    [HttpPatch("{id:guid}/deactivate")]
    [Authorize(Policy = "Permission:PlantProtectionProduct.Deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await service.DeactivateAsync(
            GetUserContext(),
            id,
            ipAddress,
            cancellationToken);

        return result ? NoContent() : BadRequest(new { message = "Plant protection product is already inactive." });
    }

    private PlantProtectionActor GetUserContext() => UserContextHelper.GetUserContext<PlantProtectionActor>(User);
}
