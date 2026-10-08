using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.PlantProtection;

namespace FarmManagement.Application.Interfaces.PlantProtection;

public interface IPlantProtectionProductService
{
    Task<PagedResponse<PlantProtectionProductResponse>> ListAsync(
        PlantProtectionActor actor,
        int page,
        int pageSize,
        string? search,
        Guid? productTypeId,
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<PlantProtectionProductResponse> GetAsync(
        PlantProtectionActor actor,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PlantProtectionProductResponse> CreateAsync(
        PlantProtectionActor actor,
        CreatePlantProtectionProductRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<PlantProtectionProductResponse> UpdateAsync(
        PlantProtectionActor actor,
        Guid id,
        UpdatePlantProtectionProductRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<bool> ActivateAsync(
        PlantProtectionActor actor,
        Guid id,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(
        PlantProtectionActor actor,
        Guid id,
        string? ipAddress,
        CancellationToken cancellationToken = default);
}
