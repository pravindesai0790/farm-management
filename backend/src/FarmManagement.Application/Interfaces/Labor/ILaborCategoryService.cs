using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Labor;

namespace FarmManagement.Application.Interfaces.Labor;

public interface ILaborCategoryService
{
    Task<PagedResponse<LaborCategoryResponse>> ListAsync(
        LaborCategoryActor actor,
        int page,
        int pageSize,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<LaborCategoryResponse> GetAsync(
        LaborCategoryActor actor,
        Guid categoryId,
        CancellationToken cancellationToken = default);

    Task<LaborCategoryResponse> CreateAsync(
        LaborCategoryActor actor,
        CreateLaborCategoryRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<LaborCategoryResponse> UpdateAsync(
        LaborCategoryActor actor,
        Guid categoryId,
        UpdateLaborCategoryRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<bool> ActivateAsync(
        LaborCategoryActor actor,
        Guid categoryId,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(
        LaborCategoryActor actor,
        Guid categoryId,
        string? ipAddress,
        CancellationToken cancellationToken = default);
}
