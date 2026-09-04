using FarmManagement.Application.DTOs.CropCycles;

namespace FarmManagement.Application.Interfaces.CropCycles;

public sealed record CropCycleActor(Guid UserId, Guid OrganizationId);

public interface ICropCycleService
{
    Task<CropCycleListResponse> ListAsync(
        CropCycleActor actor,
        Guid? farmId,
        Guid? farmAreaId,
        Guid? plantationId,
        string? status,
        int? seasonYear,
        CancellationToken cancellationToken = default);

    Task<CropCycleResponse> GetAsync(
        CropCycleActor actor,
        Guid cycleId,
        CancellationToken cancellationToken = default);

    Task<CropCycleResponse> CreateAsync(
        CropCycleActor actor,
        CreateCropCycleRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<CropCycleResponse> UpdateAsync(
        CropCycleActor actor,
        Guid cycleId,
        UpdateCropCycleRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<bool> StartAsync(
        CropCycleActor actor,
        Guid cycleId,
        StartCropCycleRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<bool> HarvestAsync(
        CropCycleActor actor,
        Guid cycleId,
        HarvestCropCycleRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<bool> CompleteAsync(
        CropCycleActor actor,
        Guid cycleId,
        CompleteCropCycleRequest? request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<bool> CancelAsync(
        CropCycleActor actor,
        Guid cycleId,
        CancelCropCycleRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);
}
