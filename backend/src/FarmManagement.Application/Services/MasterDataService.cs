using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.MasterData;
using FarmManagement.Application.Interfaces;

namespace FarmManagement.Application.Services;

public sealed class MasterDataService(IMasterDataStore store) : IMasterDataService
{
    public async Task<IReadOnlyList<UnitResponse>> ListUnitsAsync(MasterDataActor actor, string? category, CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        return (await store.ListUnitsAsync(actor.OrganizationId, category, cancellationToken))
            .Select(unit => new UnitResponse(unit.Id, unit.Code, unit.Name, unit.Symbol, unit.UnitCategory.ToString().ToUpperInvariant(), unit.IsSystem, unit.IsActive))
            .ToArray();
    }

    public async Task<IReadOnlyList<FarmOwnershipTypeResponse>> ListFarmOwnershipTypesAsync(MasterDataActor actor, CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        return (await store.ListFarmOwnershipTypesAsync(cancellationToken))
            .Select(type => new FarmOwnershipTypeResponse(type.Id, type.Code, type.Name, type.IsSystem, type.IsActive))
            .ToArray();
    }

    public async Task<IReadOnlyList<PlantationEndReasonResponse>> ListPlantationEndReasonsAsync(MasterDataActor actor, CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        return (await store.ListPlantationEndReasonsAsync(actor.OrganizationId, cancellationToken))
            .Select(reason => new PlantationEndReasonResponse(reason.Id, reason.Code, reason.Name, reason.Description, reason.IsSystem, reason.IsActive))
            .ToArray();
    }

    public Task<IReadOnlyList<PlantationEndReasonResponse>> ListCycleCancellationReasonsAsync(MasterDataActor actor, CancellationToken cancellationToken = default) =>
        ListPlantationEndReasonsAsync(actor, cancellationToken);

    private static void ValidateActor(MasterDataActor actor)
    {
        if (actor.UserId == Guid.Empty || actor.OrganizationId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("The access token does not contain a valid user scope.");
        }
    }
}
