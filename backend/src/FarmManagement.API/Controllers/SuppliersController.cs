using FarmManagement.API.Helpers;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Expenses;
using FarmManagement.Application.Interfaces.Expenses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmManagement.API.Controllers;

[ApiController]
[Route("api/suppliers")]
[Authorize]
public sealed class SuppliersController(ISupplierService supplierService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Permission:Supplier.View")]
    public async Task<ActionResult<PagedResponse<SupplierResponse>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default) =>
        Ok(await supplierService.ListAsync(
            GetUserContext(),
            page,
            pageSize,
            search,
            isActive,
            cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Permission:Supplier.View")]
    public async Task<ActionResult<SupplierResponse>> Get(
        Guid id,
        CancellationToken cancellationToken = default) =>
        Ok(await supplierService.GetAsync(GetUserContext(), id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = "Permission:Supplier.Create")]
    public async Task<ActionResult<SupplierResponse>> Create(
        [FromBody] CreateSupplierRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await supplierService.CreateAsync(GetUserContext(), request, GetIpAddress(), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Permission:Supplier.Update")]
    public async Task<ActionResult<SupplierResponse>> Update(
        Guid id,
        [FromBody] UpdateSupplierRequest request,
        CancellationToken cancellationToken = default) =>
        Ok(await supplierService.UpdateAsync(GetUserContext(), id, request, GetIpAddress(), cancellationToken));

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = "Permission:Supplier.Deactivate")]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateSupplierStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var changed = request.IsActive
            ? await supplierService.ActivateAsync(GetUserContext(), id, GetIpAddress(), cancellationToken)
            : await supplierService.DeactivateAsync(GetUserContext(), id, GetIpAddress(), cancellationToken);

        return changed ? NoContent() : BadRequest(new { message = $"Supplier is already {(request.IsActive ? "active" : "inactive")}." });
    }

    [HttpPost("{id:guid}/activate")]
    [HttpPatch("{id:guid}/activate")]
    [Authorize(Policy = "Permission:Supplier.Deactivate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken = default)
    {
        var changed = await supplierService.ActivateAsync(GetUserContext(), id, GetIpAddress(), cancellationToken);
        return changed ? NoContent() : BadRequest(new { message = "Supplier is already active." });
    }

    [HttpPost("{id:guid}/deactivate")]
    [HttpPatch("{id:guid}/deactivate")]
    [Authorize(Policy = "Permission:Supplier.Deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken = default)
    {
        var changed = await supplierService.DeactivateAsync(GetUserContext(), id, GetIpAddress(), cancellationToken);
        return changed ? NoContent() : BadRequest(new { message = "Supplier is already inactive." });
    }

    private ExpenseActor GetUserContext() => UserContextHelper.GetUserContext<ExpenseActor>(User);

    private string? GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
