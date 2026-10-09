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

    public async Task<IReadOnlyList<CurrencyResponse>> ListCurrenciesAsync(MasterDataActor actor, CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        return (await store.ListCurrenciesAsync(cancellationToken))
            .Select(c => new CurrencyResponse(c.Id, c.Code, c.Name, c.Symbol, c.IsSystem, c.IsActive, c.DisplayOrder))
            .ToArray();
    }

    public async Task<IReadOnlyList<ProductTypeResponse>> ListProductTypesAsync(MasterDataActor actor, CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        return (await store.ListProductTypesAsync(actor.OrganizationId, cancellationToken))
            .Select(pt => new ProductTypeResponse(pt.Id, pt.Code, pt.Name, pt.Description, pt.DisplayOrder, pt.IsSystem, pt.IsActive))
            .ToArray();
    }

    public async Task<IReadOnlyList<TargetResponse>> ListTargetsAsync(MasterDataActor actor, string? type = null, CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);

        Domain.Enums.TargetType? parsedTargetType = null;
        if (!string.IsNullOrWhiteSpace(type))
        {
            if (Enum.TryParse<Domain.Enums.TargetType>(type.Trim(), true, out var targetType))
            {
                parsedTargetType = targetType;
            }
        }

        return (await store.ListTargetsAsync(actor.OrganizationId, parsedTargetType, cancellationToken))
            .Select(t => new TargetResponse(t.Id, t.Code, t.Name, t.TargetType.ToString(), t.Description, t.DisplayOrder, t.IsSystem, t.IsActive))
            .ToArray();
    }

    public async Task<IReadOnlyList<ApplicationMethodResponse>> ListApplicationMethodsAsync(MasterDataActor actor, CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        return (await store.ListApplicationMethodsAsync(actor.OrganizationId, cancellationToken))
            .Select(am => new ApplicationMethodResponse(am.Id, am.Code, am.Name, am.Description, am.DisplayOrder, am.IsSystem, am.IsActive))
            .ToArray();
    }


    private static void ValidateActor(MasterDataActor actor)
    {
        if (actor.UserId == Guid.Empty || actor.OrganizationId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("The access token does not contain a valid user scope.");
        }
    }
}
