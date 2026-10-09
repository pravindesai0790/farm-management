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

    public async Task<IReadOnlyList<ProductType>> ListProductTypesAsync(Guid organizationId, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = dbContext.ProductTypes.AsNoTracking()
            .Where(pt => pt.IsSystem || pt.OrganizationId == organizationId);

        if (!includeInactive)
        {
            query = query.Where(pt => pt.IsActive);
        }

        return await query
            .OrderBy(pt => pt.DisplayOrder)
            .ThenBy(pt => pt.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductType?> FindProductTypeByIdAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default) =>
        await dbContext.ProductTypes
            .FirstOrDefaultAsync(pt => pt.Id == id && (pt.IsSystem || pt.OrganizationId == organizationId), cancellationToken);

    public async Task<bool> ProductTypeCodeExistsAsync(string code, Guid organizationId, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        var query = dbContext.ProductTypes
            .Where(pt => (pt.IsSystem || pt.OrganizationId == organizationId) && pt.Code == normalizedCode);

        if (excludeId.HasValue)
        {
            query = query.Where(pt => pt.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public void AddProductType(ProductType productType) =>
        dbContext.ProductTypes.Add(productType);

    public async Task<IReadOnlyList<Target>> ListTargetsAsync(Guid organizationId, Domain.Enums.TargetType? targetType = null, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Targets.AsNoTracking()
            .Where(t => t.IsSystem || t.OrganizationId == organizationId);

        if (!includeInactive)
        {
            query = query.Where(t => t.IsActive);
        }

        if (targetType.HasValue)
        {
            query = query.Where(t => t.TargetType == targetType.Value);
        }

        return await query
            .OrderBy(t => t.DisplayOrder)
            .ThenBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Target?> FindTargetByIdAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default) =>
        await dbContext.Targets
            .FirstOrDefaultAsync(t => t.Id == id && (t.IsSystem || t.OrganizationId == organizationId), cancellationToken);

    public async Task<bool> TargetCodeExistsAsync(string code, Guid organizationId, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        var query = dbContext.Targets
            .Where(t => (t.IsSystem || t.OrganizationId == organizationId) && t.Code == normalizedCode);

        if (excludeId.HasValue)
        {
            query = query.Where(t => t.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public void AddTarget(Target target) =>
        dbContext.Targets.Add(target);

    public async Task<IReadOnlyList<ApplicationMethod>> ListApplicationMethodsAsync(Guid organizationId, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = dbContext.ApplicationMethods.AsNoTracking()
            .Where(am => am.IsSystem || am.OrganizationId == organizationId);

        if (!includeInactive)
        {
            query = query.Where(am => am.IsActive);
        }

        return await query
            .OrderBy(am => am.DisplayOrder)
            .ThenBy(am => am.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<ApplicationMethod?> FindApplicationMethodByIdAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default) =>
        await dbContext.ApplicationMethods
            .FirstOrDefaultAsync(am => am.Id == id && (am.IsSystem || am.OrganizationId == organizationId), cancellationToken);

    public async Task<bool> ApplicationMethodCodeExistsAsync(string code, Guid organizationId, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        var query = dbContext.ApplicationMethods
            .Where(am => (am.IsSystem || am.OrganizationId == organizationId) && am.Code == normalizedCode);

        if (excludeId.HasValue)
        {
            query = query.Where(am => am.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public void AddApplicationMethod(ApplicationMethod applicationMethod) =>
        dbContext.ApplicationMethods.Add(applicationMethod);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.SaveChangesAsync(cancellationToken);
}

