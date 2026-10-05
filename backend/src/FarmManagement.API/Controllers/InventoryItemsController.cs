using FarmManagement.API.Helpers;
using FarmManagement.Application.DTOs.Inventory;
using FarmManagement.Application.Interfaces.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmManagement.API.Controllers;

[ApiController]
[Route("api/inventory/items")]
[Authorize]
public sealed class InventoryItemsController(IInventoryItemService inventoryItemService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Permission:InventoryItem.View")]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var result = await inventoryItemService.ListAsync(
            GetUserContext(),
            page,
            pageSize,
            search,
            categoryId,
            isActive,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("categories")]
    [Authorize(Policy = "Permission:InventoryItem.View")]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        var result = await inventoryItemService.GetCategoriesAsync(GetUserContext(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Permission:InventoryItem.View")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await inventoryItemService.GetAsync(GetUserContext(), id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "Permission:InventoryItem.Create")]
    public async Task<IActionResult> Create(
        [FromBody] CreateInventoryItemRequest request,
        CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await inventoryItemService.CreateAsync(
            GetUserContext(),
            request,
            ipAddress,
            cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Permission:InventoryItem.Update")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateInventoryItemRequest request,
        CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await inventoryItemService.UpdateAsync(
            GetUserContext(),
            id,
            request,
            ipAddress,
            cancellationToken);

        return Ok(result);
    }

    [HttpPatch("{id:guid}/activate")]
    [Authorize(Policy = "Permission:InventoryItem.Activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await inventoryItemService.ActivateAsync(
            GetUserContext(),
            id,
            ipAddress,
            cancellationToken);

        return result ? NoContent() : BadRequest(new { message = "Item is already active." });
    }

    [HttpPatch("{id:guid}/deactivate")]
    [Authorize(Policy = "Permission:InventoryItem.Deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await inventoryItemService.DeactivateAsync(
            GetUserContext(),
            id,
            ipAddress,
            cancellationToken);

        return result ? NoContent() : BadRequest(new { message = "Item is already inactive." });
    }

    private InventoryActor GetUserContext() => UserContextHelper.GetUserContext<InventoryActor>(User);
}
