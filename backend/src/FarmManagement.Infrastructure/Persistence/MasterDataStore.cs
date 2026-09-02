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
}
