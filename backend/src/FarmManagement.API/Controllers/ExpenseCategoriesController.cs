using FarmManagement.API.Helpers;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Expenses;
using FarmManagement.Application.Interfaces.Expenses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmManagement.API.Controllers;

[ApiController]
[Route("api/expense-categories")]
[Authorize]
public sealed class ExpenseCategoriesController(IExpenseCategoryService expenseCategoryService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Permission:ExpenseCategory.View")]
    public async Task<ActionResult<PagedResponse<ExpenseCategoryResponse>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default) =>
        Ok(await expenseCategoryService.ListAsync(
            GetUserContext(),
            page,
            pageSize,
            search,
            isActive,
            cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Permission:ExpenseCategory.View")]
    public async Task<ActionResult<ExpenseCategoryResponse>> Get(
        Guid id,
        CancellationToken cancellationToken = default) =>
        Ok(await expenseCategoryService.GetAsync(GetUserContext(), id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = "Permission:ExpenseCategory.Manage")]
    public async Task<ActionResult<ExpenseCategoryResponse>> Create(
        [FromBody] CreateExpenseCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await expenseCategoryService.CreateAsync(GetUserContext(), request, GetIpAddress(), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Permission:ExpenseCategory.Manage")]
    public async Task<ActionResult<ExpenseCategoryResponse>> Update(
        Guid id,
        [FromBody] UpdateExpenseCategoryRequest request,
        CancellationToken cancellationToken = default) =>
        Ok(await expenseCategoryService.UpdateAsync(GetUserContext(), id, request, GetIpAddress(), cancellationToken));

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = "Permission:ExpenseCategory.Manage")]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateExpenseCategoryStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var changed = request.IsActive
            ? await expenseCategoryService.ActivateAsync(GetUserContext(), id, GetIpAddress(), cancellationToken)
            : await expenseCategoryService.DeactivateAsync(GetUserContext(), id, GetIpAddress(), cancellationToken);

        return changed ? NoContent() : BadRequest(new { message = $"Category is already {(request.IsActive ? "active" : "inactive")}." });
    }

    [HttpPost("{id:guid}/activate")]
    [HttpPatch("{id:guid}/activate")]
    [Authorize(Policy = "Permission:ExpenseCategory.Manage")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken = default)
    {
        var changed = await expenseCategoryService.ActivateAsync(GetUserContext(), id, GetIpAddress(), cancellationToken);
        return changed ? NoContent() : BadRequest(new { message = "Category is already active." });
    }

    [HttpPost("{id:guid}/deactivate")]
    [HttpPatch("{id:guid}/deactivate")]
    [Authorize(Policy = "Permission:ExpenseCategory.Manage")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken = default)
    {
        var changed = await expenseCategoryService.DeactivateAsync(GetUserContext(), id, GetIpAddress(), cancellationToken);
        return changed ? NoContent() : BadRequest(new { message = "Category is already inactive." });
    }

    private ExpenseActor GetUserContext() => UserContextHelper.GetUserContext<ExpenseActor>(User);

    private string? GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
