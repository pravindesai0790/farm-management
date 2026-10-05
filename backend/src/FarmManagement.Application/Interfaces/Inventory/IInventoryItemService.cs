using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Inventory;

namespace FarmManagement.Application.Interfaces.Inventory;

public interface IInventoryItemService
{
    Task<PagedResponse<InventoryItemResponse>> ListAsync(
        InventoryActor actor,
        int page,
        int pageSize,
        string? search,
        Guid? categoryId,
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<InventoryItemResponse> GetAsync(
        InventoryActor actor,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<InventoryItemResponse> CreateAsync(
        InventoryActor actor,
        CreateInventoryItemRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<InventoryItemResponse> UpdateAsync(
        InventoryActor actor,
        Guid id,
        UpdateInventoryItemRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<bool> ActivateAsync(
        InventoryActor actor,
        Guid id,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(
        InventoryActor actor,
        Guid id,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryCategoryResponse>> GetCategoriesAsync(
        InventoryActor actor,
        CancellationToken cancellationToken = default);
}
