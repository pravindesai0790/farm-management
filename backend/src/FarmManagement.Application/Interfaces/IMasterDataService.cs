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
    Task<IReadOnlyList<ProductTypeResponse>> ListProductTypesAsync(MasterDataActor actor, bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<ProductTypeResponse> CreateProductTypeAsync(MasterDataActor actor, CreateProductTypeRequest request, CancellationToken cancellationToken = default);
    Task<ProductTypeResponse> UpdateProductTypeAsync(MasterDataActor actor, Guid id, UpdateProductTypeRequest request, CancellationToken cancellationToken = default);
    Task ActivateProductTypeAsync(MasterDataActor actor, Guid id, CancellationToken cancellationToken = default);
    Task DeactivateProductTypeAsync(MasterDataActor actor, Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TargetResponse>> ListTargetsAsync(MasterDataActor actor, string? type = null, bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<TargetResponse> CreateTargetAsync(MasterDataActor actor, CreateTargetRequest request, CancellationToken cancellationToken = default);
    Task<TargetResponse> UpdateTargetAsync(MasterDataActor actor, Guid id, UpdateTargetRequest request, CancellationToken cancellationToken = default);
    Task ActivateTargetAsync(MasterDataActor actor, Guid id, CancellationToken cancellationToken = default);
    Task DeactivateTargetAsync(MasterDataActor actor, Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ApplicationMethodResponse>> ListApplicationMethodsAsync(MasterDataActor actor, bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<ApplicationMethodResponse> CreateApplicationMethodAsync(MasterDataActor actor, CreateApplicationMethodRequest request, CancellationToken cancellationToken = default);
    Task<ApplicationMethodResponse> UpdateApplicationMethodAsync(MasterDataActor actor, Guid id, UpdateApplicationMethodRequest request, CancellationToken cancellationToken = default);
    Task ActivateApplicationMethodAsync(MasterDataActor actor, Guid id, CancellationToken cancellationToken = default);
    Task DeactivateApplicationMethodAsync(MasterDataActor actor, Guid id, CancellationToken cancellationToken = default);
}

