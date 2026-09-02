using FarmManagement.Application.Interfaces.Crops;
using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FarmManagement.Infrastructure.Persistence;

public sealed class CropStore(ApplicationDbContext dbContext) : ICropStore
{
    public Task<int> CountCropsAsync(Guid organizationId, string? search, bool? isActive, CancellationToken cancellationToken = default) =>
        BuildCropQuery(organizationId, search, isActive).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<Crop>> ListCropsAsync(
        Guid organizationId, int skip, int take, string? search, bool? isActive, CancellationToken cancellationToken = default) =>
        await BuildCropQuery(organizationId, search, isActive)
            .AsNoTracking()
            .OrderBy(crop => crop.Name)
            .ThenBy(crop => crop.Code)
            .ThenBy(crop => crop.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task<Crop?> FindCropAsync(Guid cropId, Guid organizationId, CancellationToken cancellationToken = default) =>
        BuildCropQuery(organizationId, null, null).SingleOrDefaultAsync(crop => crop.Id == cropId, cancellationToken);

    public Task<bool> CropCodeExistsAsync(
        Guid organizationId, string code, Guid? excludingCropId = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Crops.Where(crop =>
            (crop.IsSystem && crop.OrganizationId == null) || crop.OrganizationId == organizationId);
        query = query.Where(crop => crop.Code == code);
        if (excludingCropId is not null) query = query.Where(crop => crop.Id != excludingCropId.Value);
        return query.AnyAsync(cancellationToken);
    }

    public Task<int> CountVarietiesAsync(Guid organizationId, Guid cropId, bool? isActive, CancellationToken cancellationToken = default) =>
        BuildVarietyQuery(organizationId, cropId, isActive).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<CropVariety>> ListVarietiesAsync(
        Guid organizationId, Guid cropId, int skip, int take, bool? isActive, CancellationToken cancellationToken = default) =>
        await BuildVarietyQuery(organizationId, cropId, isActive)
            .AsNoTracking()
            .OrderBy(variety => variety.Name)
            .ThenBy(variety => variety.Code)
            .ThenBy(variety => variety.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task<CropVariety?> FindVarietyAsync(Guid varietyId, Guid organizationId, CancellationToken cancellationToken = default) =>
        BuildVarietyQuery(organizationId, null, null).SingleOrDefaultAsync(variety => variety.Id == varietyId, cancellationToken);

    public Task<bool> VarietyCodeExistsAsync(
        Guid organizationId, Guid cropId, string code, Guid? excludingVarietyId = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.CropVarieties.Where(variety =>
            variety.CropId == cropId &&
            ((variety.IsSystem && variety.OrganizationId == null) || variety.OrganizationId == organizationId) &&
            variety.Code == code);
        if (excludingVarietyId is not null) query = query.Where(variety => variety.Id != excludingVarietyId.Value);
        return query.AnyAsync(cancellationToken);
    }

    public void Add(Crop crop) => dbContext.Crops.Add(crop);
    public void Add(CropVariety variety) => dbContext.CropVarieties.Add(variety);
    public void AddAuditLog(AuditLog auditLog) => dbContext.AuditLogs.Add(auditLog);
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => dbContext.SaveChangesAsync(cancellationToken);

    private IQueryable<Crop> BuildCropQuery(Guid organizationId, string? search, bool? isActive)
    {
        var query = dbContext.Crops.Where(crop =>
            (crop.IsSystem && crop.OrganizationId == null) || crop.OrganizationId == organizationId);
        if (isActive is not null) query = query.Where(crop => crop.IsActive == isActive.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLowerInvariant();
            query = query.Where(crop => crop.Code.ToLower().Contains(normalizedSearch) || crop.Name.ToLower().Contains(normalizedSearch));
        }
        return query;
    }

    private IQueryable<CropVariety> BuildVarietyQuery(Guid organizationId, Guid? cropId, bool? isActive)
    {
        var query = dbContext.CropVarieties
            .Include(variety => variety.Crop)
            .Where(variety =>
                ((variety.IsSystem && variety.OrganizationId == null) || variety.OrganizationId == organizationId) &&
                ((variety.Crop.IsSystem && variety.Crop.OrganizationId == null) || variety.Crop.OrganizationId == organizationId));
        if (cropId is not null) query = query.Where(variety => variety.CropId == cropId.Value);
        if (isActive is not null) query = query.Where(variety => variety.IsActive == isActive.Value);
        return query;
    }
}
