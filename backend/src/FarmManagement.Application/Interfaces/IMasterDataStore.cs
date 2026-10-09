using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Interfaces;

public interface IMasterDataStore
{
    Task<IReadOnlyList<Unit>> ListUnitsAsync(Guid organizationId, string? category, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FarmOwnershipType>> ListFarmOwnershipTypesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PlantationEndReason>> ListPlantationEndReasonsAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Currency>> ListCurrenciesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductType>> ListProductTypesAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Target>> ListTargetsAsync(Guid organizationId, Domain.Enums.TargetType? targetType = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApplicationMethod>> ListApplicationMethodsAsync(Guid organizationId, CancellationToken cancellationToken = default);
}

