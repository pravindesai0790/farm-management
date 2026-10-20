using FarmManagement.Application.DTOs.Fertilizer;
using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Interfaces.Fertilizer;

public interface IFertilizerApplicationStore
{
    Task<FertilizerApplication?> FindAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        Guid organizationId,
        FertilizerApplicationListQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FertilizerApplication>> ListAsync(
        Guid organizationId,
        FertilizerApplicationListQuery query,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FertilizerProductLookupResponse>> ListProductsLookupAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FertilizerStorageLocationLookupResponse>> ListStorageLocationsLookupAsync(
        Guid organizationId,
        Guid farmId,
        Guid? inventoryItemId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FertilizerApplicationMethodLookupResponse>> ListApplicationMethodsLookupAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FertilizerIrrigationLookupResponse>> ListIrrigationsLookupAsync(
        Guid organizationId,
        Guid? farmId,
        Guid? farmAreaId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, string>> GetUserNamesAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default);
}
