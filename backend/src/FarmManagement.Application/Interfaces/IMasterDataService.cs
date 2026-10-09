using FarmManagement.Application.DTOs.MasterData;

namespace FarmManagement.Application.Interfaces;

public sealed record MasterDataActor(Guid UserId, Guid OrganizationId, bool CanManageAllOrganizations = false);

public interface IMasterDataService
{
    Task<IReadOnlyList<UnitResponse>> ListUnitsAsync(MasterDataActor actor, string? category, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FarmOwnershipTypeResponse>> ListFarmOwnershipTypesAsync(MasterDataActor actor, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PlantationEndReasonResponse>> ListPlantationEndReasonsAsync(MasterDataActor actor, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PlantationEndReasonResponse>> ListCycleCancellationReasonsAsync(MasterDataActor actor, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CurrencyResponse>> ListCurrenciesAsync(MasterDataActor actor, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductTypeResponse>> ListProductTypesAsync(MasterDataActor actor, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TargetResponse>> ListTargetsAsync(MasterDataActor actor, string? type = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApplicationMethodResponse>> ListApplicationMethodsAsync(MasterDataActor actor, CancellationToken cancellationToken = default);
}

