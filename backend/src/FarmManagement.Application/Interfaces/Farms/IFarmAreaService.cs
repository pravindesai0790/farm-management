using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Farms;

namespace FarmManagement.Application.Interfaces.Farms;

public interface IFarmAreaService
{
    Task<PagedResponse<FarmAreaResponse>> ListPagedAsync(
        FarmActor actor,
        int page,
        int pageSize,
        Guid? farmId,
        bool? isActive,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<PagedResponse<FarmAreaResponse>> ListAsync(
        FarmActor actor,
        Guid farmId,
        int page,
        int pageSize,
        bool? isActive,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<FarmAreaResponse> GetAsync(
        FarmActor actor,
        Guid farmAreaId,
        CancellationToken cancellationToken = default);

    Task<FarmAreaResponse> CreateAsync(
        FarmActor actor,
        CreateFarmAreaRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<FarmAreaResponse> UpdateAsync(
        FarmActor actor,
        Guid farmAreaId,
        UpdateFarmAreaRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<bool> ActivateAsync(
        FarmActor actor,
        Guid farmAreaId,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(
        FarmActor actor,
        Guid farmAreaId,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<FarmAreaAvailabilityResponse> GetAvailabilityAsync(
        FarmActor actor,
        Guid farmAreaId,
        CancellationToken cancellationToken = default);
}
