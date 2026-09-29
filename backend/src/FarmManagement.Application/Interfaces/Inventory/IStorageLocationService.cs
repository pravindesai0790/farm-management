using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Inventory;

namespace FarmManagement.Application.Interfaces.Inventory;

public interface IStorageLocationService
{
    Task<PagedResponse<StorageLocationResponse>> ListAsync(
        InventoryActor actor,
        int page,
        int pageSize,
        Guid? farmId,
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<StorageLocationResponse> GetAsync(
        InventoryActor actor,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<StorageLocationResponse> CreateAsync(
        InventoryActor actor,
        CreateStorageLocationRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<StorageLocationResponse> UpdateAsync(
        InventoryActor actor,
        Guid id,
        UpdateStorageLocationRequest request,
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
}
