using FarmManagement.Application.Interfaces.Dashboard;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FarmManagement.Infrastructure.Persistence;

public sealed class DashboardStore(ApplicationDbContext dbContext) : IDashboardStore
{
    public async Task<IReadOnlyList<Farm>> GetFarmsWithAreasAsync(
        Guid organizationId,
        Guid? farmId,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Farms
            .Include(f => f.AreaUnit)
            .Where(f => f.OrganizationId == organizationId && f.IsActive);

        if (farmId.HasValue)
        {
            query = query.Where(f => f.Id == farmId.Value);
        }

        return await query
            .AsNoTracking()
            .OrderBy(f => f.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<int> GetActiveFarmAreasCountAsync(
        Guid organizationId,
        Guid? farmId,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.FarmAreas
            .Where(a => a.OrganizationId == organizationId && a.IsActive);

        if (farmId.HasValue)
        {
            query = query.Where(a => a.FarmId == farmId.Value);
        }

        return query.CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CropPlantation>> GetPlantationsAsync(
        Guid organizationId,
        Guid? farmId,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.CropPlantations
            .Include(p => p.Crop)
            .Include(p => p.Variety)
            .Include(p => p.AreaUnit)
            .Include(p => p.Farm)
            .Include(p => p.FarmArea)
            .Where(p => p.OrganizationId == organizationId && p.IsActive);

        if (farmId.HasValue)
        {
            query = query.Where(p => p.FarmId == farmId.Value);
        }

        return await query
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CropCycle>> GetCyclesAsync(
        Guid organizationId,
        Guid? farmId,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.CropCycles
            .Include(c => c.Plantation)
                .ThenInclude(p => p!.Crop)
            .Include(c => c.Plantation)
                .ThenInclude(p => p!.Variety)
            .Include(c => c.Plantation)
                .ThenInclude(p => p!.Farm)
            .Include(c => c.Plantation)
                .ThenInclude(p => p!.FarmArea)
            .Include(c => c.Plantation)
                .ThenInclude(p => p!.AreaUnit)
            .Where(c => c.OrganizationId == organizationId);

        if (farmId.HasValue)
        {
            query = query.Where(c => c.Plantation != null && c.Plantation.FarmId == farmId.Value);
        }

        return await query
            .AsNoTracking()
            .AsSplitQuery()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Unit>> GetAreaUnitsAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Units
            .Where(u => u.UnitCategory == UnitCategory.Area && u.IsActive)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
