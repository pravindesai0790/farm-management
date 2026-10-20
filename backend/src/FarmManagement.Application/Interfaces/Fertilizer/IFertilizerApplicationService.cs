using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Fertilizer;

namespace FarmManagement.Application.Interfaces.Fertilizer;

public interface IFertilizerApplicationService
{
    Task<PagedResponse<FertilizerApplicationListItemResponse>> ListAsync(
        FertilizerActor actor,
        FertilizerApplicationListQuery query,
        CancellationToken cancellationToken = default);

    Task<FertilizerApplicationDetailsResponse> GetAsync(
        FertilizerActor actor,
        Guid id,
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
}
