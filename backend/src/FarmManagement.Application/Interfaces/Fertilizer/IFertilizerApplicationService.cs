using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Fertilizer;

namespace FarmManagement.Application.Interfaces.Fertilizer;

public interface IFertilizerApplicationService
{
    Task<PagedResponse<FertilizerApplicationListItemResponse>> ListAsync(
        FertilizerActor actor,
        FertilizerApplicationListQuery query,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportCsvAsync(
        FertilizerActor actor,
        FertilizerApplicationListQuery query,
        CancellationToken cancellationToken = default);

    Task<FertilizerApplicationDetailsResponse> GetAsync(
        FertilizerActor actor,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FertilizerProductLookupResponse>> ListProductsLookupAsync(
        FertilizerActor actor,
        Guid? storageLocationId,
        Guid? farmId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FertilizerProductLookupResponse>> ListProductsLookupAsync(
        FertilizerActor actor,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FertilizerStorageLocationLookupResponse>> ListStorageLocationsLookupAsync(
        FertilizerActor actor,
        Guid farmId,
        Guid? inventoryItemId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FertilizerApplicationMethodLookupResponse>> ListApplicationMethodsLookupAsync(
        FertilizerActor actor,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FertilizerIrrigationLookupResponse>> ListIrrigationsLookupAsync(
        FertilizerActor actor,
        Guid? farmId,
        Guid? farmAreaId,
        CancellationToken cancellationToken = default);

    Task<FertilizerApplicationDetailsResponse> CreateDraftAsync(
        FertilizerActor actor,
        CreateFertilizerDraftRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<FertilizerApplicationDetailsResponse> UpdateDraftAsync(
        FertilizerActor actor,
        Guid id,
        UpdateFertilizerDraftRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task DeleteDraftAsync(
        FertilizerActor actor,
        Guid id,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<FertilizerApplicationDetailsResponse> CopyAsNewDraftAsync(
        FertilizerActor actor,
        Guid sourceId,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<FertilizerApplicationDetailsResponse> PlanAsync(
        FertilizerActor actor,
        Guid id,
        PlanFertilizerApplicationRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<FertilizerApplicationDetailsResponse> ScheduleAsync(
        FertilizerActor actor,
        Guid id,
        ScheduleFertilizerApplicationRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<FertilizerApplicationDetailsResponse> RescheduleAsync(
        FertilizerActor actor,
        Guid id,
        RescheduleFertilizerApplicationRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<FertilizerApplicationDetailsResponse> StartAsync(
        FertilizerActor actor,
        Guid id,
        StartFertilizerApplicationRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<FertilizerApplicationDetailsResponse> CompleteAsync(
        FertilizerActor actor,
        Guid id,
        CompleteFertilizerApplicationRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<FertilizerApplicationDetailsResponse> CancelAsync(
        FertilizerActor actor,
        Guid id,
        CancelFertilizerApplicationRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<FertilizerApplicationDetailsResponse> RecordCompletedAsync(
        FertilizerActor actor,
        RecordCompletedFertilizerApplicationRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);
}
