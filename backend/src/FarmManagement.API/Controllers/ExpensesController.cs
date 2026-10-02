using FarmManagement.API.Helpers;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Expenses;
using FarmManagement.Application.Interfaces.Expenses;
using FarmManagement.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmManagement.API.Controllers;

[ApiController]
[Route("api/expenses")]
[Authorize]
public sealed class ExpensesController(IExpenseService expenseService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Permission:Expense.View")]
    public async Task<ActionResult<PagedResponse<ExpenseResponse>>> List(
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] Guid? farmId = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] Guid? supplierId = null,
        [FromQuery] ExpenseStatus? status = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var filter = new ExpenseFilter(from, to, farmId, categoryId, supplierId, status, search);
        var result = await expenseService.ListAsync(
            GetUserContext(),
            filter,
            page,
            pageSize,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Permission:Expense.View")]
    public async Task<ActionResult<ExpenseResponse>> Get(
        Guid id,
        CancellationToken cancellationToken = default) =>
        Ok(await expenseService.GetAsync(GetUserContext(), id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = "Permission:Expense.Create")]
    public async Task<ActionResult<ExpenseResponse>> Create(
        [FromBody] CreateExpenseRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await expenseService.CreateDraftAsync(
            GetUserContext(),
            request,
            GetIpAddress(),
            cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Permission:Expense.UpdateDraft")]
    public async Task<ActionResult<ExpenseResponse>> Update(
        Guid id,
        [FromBody] UpdateExpenseRequest request,
        CancellationToken cancellationToken = default) =>
        Ok(await expenseService.UpdateDraftAsync(
            GetUserContext(),
            id,
            request,
            GetIpAddress(),
            cancellationToken));

    [HttpPost("{id:guid}/post")]
    [Authorize(Policy = "Permission:Expense.Post")]
    public async Task<ActionResult<ExpenseResponse>> Post(
        Guid id,
        CancellationToken cancellationToken = default) =>
        Ok(await expenseService.PostAsync(
            GetUserContext(),
            id,
            GetIpAddress(),
            cancellationToken));

    [HttpPost("{id:guid}/reverse")]
    [Authorize(Policy = "Permission:Expense.Reverse")]
    public async Task<ActionResult<ExpenseResponse>> Reverse(
        Guid id,
        [FromBody] ReverseExpenseRequest request,
        CancellationToken cancellationToken = default) =>
        Ok(await expenseService.ReverseAsync(
            GetUserContext(),
            id,
            request,
            GetIpAddress(),
            cancellationToken));

    private ExpenseActor GetUserContext() => UserContextHelper.GetUserContext<ExpenseActor>(User);

    private string? GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
