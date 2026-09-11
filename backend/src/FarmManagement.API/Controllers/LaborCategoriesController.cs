using FarmManagement.API.Helpers;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Labor;
using FarmManagement.Application.Interfaces.Labor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmManagement.API.Controllers;

[ApiController]
[Route("api/labor/categories")]
[Authorize]
public sealed class LaborCategoriesController(ILaborCategoryService categoryService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Permission:LaborCategory.View")]
    public async Task<ActionResult<PagedResponse<LaborCategoryResponse>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default) =>
        Ok(await categoryService.ListAsync(
            GetUserContext(),
            page,
            pageSize,
            search,
            isActive,
            cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Permission:LaborCategory.View")]
    public async Task<ActionResult<LaborCategoryResponse>> Get(
        Guid id,
        CancellationToken cancellationToken = default) =>
        Ok(await categoryService.GetAsync(GetUserContext(), id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = "Permission:LaborCategory.Create")]
    public async Task<ActionResult<LaborCategoryResponse>> Create(
        [FromBody] CreateLaborCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await categoryService.CreateAsync(GetUserContext(), request, GetIpAddress(), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Permission:LaborCategory.Update")]
    public async Task<ActionResult<LaborCategoryResponse>> Update(
        Guid id,
        [FromBody] UpdateLaborCategoryRequest request,
        CancellationToken cancellationToken = default) =>
        Ok(await categoryService.UpdateAsync(GetUserContext(), id, request, GetIpAddress(), cancellationToken));

    [HttpPost("{id:guid}/activate")]
    [HttpPatch("{id:guid}/activate")]
    [Authorize(Policy = "Permission:LaborCategory.Activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken = default)
    {
        await categoryService.ActivateAsync(GetUserContext(), id, GetIpAddress(), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    [HttpPatch("{id:guid}/deactivate")]
    [Authorize(Policy = "Permission:LaborCategory.Deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken = default)
    {
        await categoryService.DeactivateAsync(GetUserContext(), id, GetIpAddress(), cancellationToken);
        return NoContent();
    }

    private LaborCategoryActor GetUserContext() => UserContextHelper.GetUserContext<LaborCategoryActor>(User);

    private string? GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
