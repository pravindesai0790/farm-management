using FarmManagement.Application.DTOs.MasterData;

namespace FarmManagement.Application.Interfaces;

public sealed record MasterDataActor(Guid UserId, Guid OrganizationId, bool CanManageAllOrganizations = false);

public interface IMasterDataService
{
    Task<IReadOnlyList<UnitResponse>> ListUnitsAsync(MasterDataActor actor, string? category, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FarmOwnershipTypeResponse>> ListFarmOwnershipTypesAsync(MasterDataActor actor, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PlantationEndReasonResponse>> ListPlantationEndReasonsAsync(MasterDataActor actor, CancellationToken cancellationToken = default);
}
