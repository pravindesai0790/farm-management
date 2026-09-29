using FarmManagement.API.Helpers;
using FarmManagement.Application.DTOs.Inventory;
using FarmManagement.Application.Interfaces.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmManagement.API.Controllers;

[ApiController]
[Route("api/inventory/storage-locations")]
[Authorize]
public sealed class StorageLocationsController(IStorageLocationService storageLocationService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Permission:StorageLocation.View")]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? farmId = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var result = await storageLocationService.ListAsync(
            GetUserContext(),
            page,
            pageSize,
            farmId,
            isActive,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Permission:StorageLocation.View")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await storageLocationService.GetAsync(GetUserContext(), id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "Permission:StorageLocation.Create")]
    public async Task<IActionResult> Create(
        [FromBody] CreateStorageLocationRequest request,
        CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await storageLocationService.CreateAsync(
            GetUserContext(),
            request,
            ipAddress,
            cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Permission:StorageLocation.Update")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateStorageLocationRequest request,
        CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await storageLocationService.UpdateAsync(
            GetUserContext(),
            id,
            request,
            ipAddress,
            cancellationToken);

        return Ok(result);
    }

    [HttpPatch("{id:guid}/activate")]
    [Authorize(Policy = "Permission:StorageLocation.Activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await storageLocationService.ActivateAsync(
            GetUserContext(),
            id,
            ipAddress,
            cancellationToken);

        return result ? NoContent() : BadRequest(new { message = "Storage location is already active." });
    }

    [HttpPatch("{id:guid}/deactivate")]
    [Authorize(Policy = "Permission:StorageLocation.Deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await storageLocationService.DeactivateAsync(
            GetUserContext(),
            id,
            ipAddress,
            cancellationToken);

        return result ? NoContent() : BadRequest(new { message = "Storage location is already inactive." });
    }

    private InventoryActor GetUserContext() => UserContextHelper.GetUserContext<InventoryActor>(User);
}
