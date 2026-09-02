using FarmManagement.Application.DTOs.Farms;

namespace FarmManagement.Application.Interfaces.Farms;

public sealed record FarmActor(Guid UserId, Guid OrganizationId);

public interface IFarmService
{
    Task<FarmListResponse> ListAsync(
        FarmActor actor,
        int page,
        int pageSize,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<FarmResponse> GetAsync(
        FarmActor actor,
        Guid farmId,
        CancellationToken cancellationToken = default);

    Task<FarmResponse> CreateAsync(
        FarmActor actor,
        CreateFarmRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<FarmResponse> UpdateAsync(
        FarmActor actor,
        Guid farmId,
        UpdateFarmRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<bool> ActivateAsync(
        FarmActor actor,
        Guid farmId,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(
        FarmActor actor,
        Guid farmId,
        string? ipAddress,
        CancellationToken cancellationToken = default);
}
