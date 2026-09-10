using FarmManagement.Application.Interfaces.Farms;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FarmManagement.Infrastructure.Persistence;

public sealed class FarmAreaStore(ApplicationDbContext dbContext) : IFarmAreaStore
{
    public async Task<IReadOnlyList<FarmArea>> ListAsync(
        Guid farmId,
        Guid organizationId,
        bool? isActive,
        CancellationToken cancellationToken = default) =>
        await BuildQuery(farmId, organizationId, isActive)
            .AsNoTracking()
            .OrderBy(area => area.ParentFarmAreaId)
            .ThenBy(area => area.Name)
            .ThenBy(area => area.Code)
            .ThenBy(area => area.Id)
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<FarmArea> Items, int TotalCount)> ListPagedAsync(
        Guid organizationId,
        Guid? farmId,
        bool? isActive,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = BuildPagedQuery(organizationId, farmId, isActive, search);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .AsNoTracking()
            .OrderBy(area => area.Farm != null ? area.Farm.Name : string.Empty)
            .ThenBy(area => area.ParentFarmAreaId)
            .ThenBy(area => area.Name)
            .ThenBy(area => area.Code)
            .ThenBy(area => area.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<FarmArea?> FindAsync(Guid farmAreaId, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.FarmAreas
            .Include(area => area.Farm)
            .Include(area => area.AreaUnit)
            .Include(area => area.ParentFarmArea)
            .SingleOrDefaultAsync(area => area.Id == farmAreaId && area.OrganizationId == organizationId, cancellationToken);

    public Task<Farm?> FindFarmAsync(Guid farmId, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.Farms
            .Include(farm => farm.AreaUnit)
            .SingleOrDefaultAsync(farm => farm.Id == farmId && farm.OrganizationId == organizationId, cancellationToken);

    public Task<FarmArea?> FindParentAsync(Guid parentFarmAreaId, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.FarmAreas
            .Include(area => area.AreaUnit)
            .SingleOrDefaultAsync(area => area.Id == parentFarmAreaId && area.OrganizationId == organizationId, cancellationToken);

    public Task<Unit?> FindAreaUnitAsync(Guid unitId, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.Units.SingleOrDefaultAsync(
            unit => unit.Id == unitId &&
                    unit.IsActive &&
                    unit.UnitCategory == UnitCategory.Area &&
                    (unit.OrganizationId == null || unit.OrganizationId == organizationId),
            cancellationToken);

    public Task<bool> CodeExistsAsync(
        Guid farmId,
        string code,
        Guid? excludingFarmAreaId = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.FarmAreas.Where(area => area.FarmId == farmId && area.Code == code);
        if (excludingFarmAreaId is not null)
        {
            query = query.Where(area => area.Id != excludingFarmAreaId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FarmArea>> ListActiveChildrenAsync(
        Guid parentFarmAreaId,
        Guid? excludingFarmAreaId = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.FarmAreas
            .Include(area => area.AreaUnit)
            .Where(area => area.ParentFarmAreaId == parentFarmAreaId && area.IsActive);
        if (excludingFarmAreaId is not null)
        {
            query = query.Where(area => area.Id != excludingFarmAreaId.Value);
        }

        return await query.AsNoTracking().ToListAsync(cancellationToken);
    }

    public Task<bool> HasChildrenAsync(Guid parentFarmAreaId, CancellationToken cancellationToken = default) =>
        dbContext.FarmAreas.AnyAsync(area => area.ParentFarmAreaId == parentFarmAreaId, cancellationToken);

    public async Task<IReadOnlyList<CropPlantation>> ListActivePlantationsAsync(
        Guid farmAreaId,
        CancellationToken cancellationToken = default) =>
        await dbContext.CropPlantations
            .Include(plantation => plantation.AreaUnit)
            .Where(plantation => plantation.FarmAreaId == farmAreaId && plantation.Status == Domain.Enums.PlantationStatus.Active)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public void Add(FarmArea farmArea) => dbContext.FarmAreas.Add(farmArea);

    public void AddAuditLog(AuditLog auditLog) => dbContext.AuditLogs.Add(auditLog);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => dbContext.SaveChangesAsync(cancellationToken);

    private IQueryable<FarmArea> BuildQuery(Guid farmId, Guid organizationId, bool? isActive)
    {
        var query = dbContext.FarmAreas
            .Include(area => area.Farm)
            .Include(area => area.AreaUnit)
            .Where(area => area.FarmId == farmId && area.OrganizationId == organizationId);

        return isActive is null ? query : query.Where(area => area.IsActive == isActive.Value);
    }

    private IQueryable<FarmArea> BuildPagedQuery(Guid organizationId, Guid? farmId, bool? isActive, string? search)
    {
        var query = dbContext.FarmAreas
            .Include(area => area.Farm)
            .Include(area => area.AreaUnit)
            .Where(area => area.OrganizationId == organizationId);

        if (farmId.HasValue)
        {
            query = query.Where(area => area.FarmId == farmId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(area => area.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(area =>
                EF.Functions.ILike(area.Name, pattern) ||
                EF.Functions.ILike(area.Code, pattern) ||
                (area.Farm != null && EF.Functions.ILike(area.Farm.Name, pattern)));
        }

        return query;
    }
}
