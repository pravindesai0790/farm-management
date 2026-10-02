using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Expenses;

namespace FarmManagement.Application.Interfaces.Expenses;

public interface IExpenseCategoryService
{
    Task<PagedResponse<ExpenseCategoryResponse>> ListAsync(
        ExpenseActor actor,
        int page,
        int pageSize,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<ExpenseCategoryResponse> GetAsync(
        ExpenseActor actor,
        Guid categoryId,
        CancellationToken cancellationToken = default);

    Task<ExpenseCategoryResponse> CreateAsync(
        ExpenseActor actor,
        CreateExpenseCategoryRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<ExpenseCategoryResponse> UpdateAsync(
        ExpenseActor actor,
        Guid categoryId,
        UpdateExpenseCategoryRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<bool> ActivateAsync(
        ExpenseActor actor,
        Guid categoryId,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(
        ExpenseActor actor,
        Guid categoryId,
        string? ipAddress,
        CancellationToken cancellationToken = default);
}
