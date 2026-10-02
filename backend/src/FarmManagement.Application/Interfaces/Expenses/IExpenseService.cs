using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Expenses;

namespace FarmManagement.Application.Interfaces.Expenses;

public interface IExpenseService
{
    Task<PagedResponse<ExpenseResponse>> ListAsync(
        ExpenseActor actor,
        ExpenseFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ExpenseResponse> GetAsync(
        ExpenseActor actor,
        Guid expenseId,
        CancellationToken cancellationToken = default);

    Task<ExpenseResponse> CreateDraftAsync(
        ExpenseActor actor,
        CreateExpenseRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<ExpenseResponse> UpdateDraftAsync(
        ExpenseActor actor,
        Guid expenseId,
        UpdateExpenseRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<ExpenseResponse> PostAsync(
        ExpenseActor actor,
        Guid expenseId,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<ExpenseResponse> ReverseAsync(
        ExpenseActor actor,
        Guid expenseId,
        ReverseExpenseRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);
}
