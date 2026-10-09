using FarmManagement.Application.Interfaces;
using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FarmManagement.Infrastructure.Persistence;

public sealed class MasterDataStore(ApplicationDbContext dbContext) : IMasterDataStore
{
    public async Task<IReadOnlyList<Unit>> ListUnitsAsync(Guid organizationId, string? category, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Units.AsNoTracking()
            .Where(unit => unit.IsActive && (unit.IsSystem || unit.OrganizationId == organizationId));
        if (!string.IsNullOrWhiteSpace(category) && Enum.TryParse<Domain.Enums.UnitCategory>(category, true, out var parsedCategory))
        {
            query = query.Where(unit => unit.UnitCategory == parsedCategory);
        }
        return await query.OrderBy(unit => unit.UnitCategory).ThenBy(unit => unit.DisplayOrder).ThenBy(unit => unit.Name).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FarmOwnershipType>> ListFarmOwnershipTypesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.FarmOwnershipTypes.AsNoTracking().Where(type => type.IsActive).OrderBy(type => type.Name).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PlantationEndReason>> ListPlantationEndReasonsAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
        await dbContext.PlantationEndReasons.AsNoTracking()
            .Where(reason => reason.IsActive && (reason.IsSystem || reason.OrganizationId == organizationId))
            .OrderBy(reason => reason.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Currency>> ListCurrenciesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Currencies.AsNoTracking()
            .Where(currency => currency.IsActive)
            .OrderBy(currency => currency.DisplayOrder)
            .ThenBy(currency => currency.Code)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ProductType>> ListProductTypesAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
        await dbContext.ProductTypes.AsNoTracking()
            .Where(pt => pt.IsActive && (pt.IsSystem || pt.OrganizationId == organizationId))
            .OrderBy(pt => pt.DisplayOrder)
            .ThenBy(pt => pt.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Target>> ListTargetsAsync(Guid organizationId, Domain.Enums.TargetType? targetType = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Targets.AsNoTracking()
            .Where(t => t.IsActive && (t.IsSystem || t.OrganizationId == organizationId));

        if (targetType.HasValue)
        {
            query = query.Where(t => t.TargetType == targetType.Value);
        }

        return await query
            .OrderBy(t => t.DisplayOrder)
            .ThenBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ApplicationMethod>> ListApplicationMethodsAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
        await dbContext.ApplicationMethods.AsNoTracking()
            .Where(am => am.IsActive && (am.IsSystem || am.OrganizationId == organizationId))
            .OrderBy(am => am.DisplayOrder)
            .ThenBy(am => am.Name)
            .ToListAsync(cancellationToken);
}

