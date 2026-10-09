using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Sprays;

namespace FarmManagement.Application.Interfaces.Sprays;

public interface ISprayService
{
    Task<PagedResponse<SprayListItemResponse>> ListAsync(
        SprayActor actor,
        SprayListQuery query,
        CancellationToken cancellationToken = default);

    Task<SprayDetailsResponse> GetAsync(
        SprayActor actor,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<SprayDetailsResponse> CreateDraftAsync(
        SprayActor actor,
        CreateSprayDraftRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<SprayDetailsResponse> UpdateDraftAsync(
        SprayActor actor,
        Guid id,
        UpdateSprayDraftRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<SprayDetailsResponse> ScheduleAsync(
        SprayActor actor,
        Guid id,
        ScheduleSprayRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<SprayDetailsResponse> RescheduleAsync(
        SprayActor actor,
        Guid id,
        RescheduleSprayRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<SprayDetailsResponse> StartAsync(
        SprayActor actor,
        Guid id,
        StartSprayRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<SprayDetailsResponse> SaveExecutionAsync(
        SprayActor actor,
        Guid id,
        UpdateSprayExecutionRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<SprayDetailsResponse> CompleteAsync(
        SprayActor actor,
        Guid id,
        CompleteSprayRequest? request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<SprayDetailsResponse> CancelAsync(
        SprayActor actor,
        Guid id,
        CancelSprayRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<SprayDetailsResponse> RecordCompletedAsync(
        SprayActor actor,
        RecordCompletedSprayRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SprayProductLookupResponse>> ListProductsLookupAsync(
        SprayActor actor,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SprayStorageLocationLookupResponse>> ListStorageLocationsLookupAsync(
        SprayActor actor,
        Guid farmId,
        Guid inventoryItemId,
        CancellationToken cancellationToken = default);
}

