using FarmManagement.Application.DTOs.Dashboard;
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

    public async Task<LaborDashboardRawData> GetLaborDashboardDataAsync(
        Guid organizationId,
        Guid? farmId,
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        var startDate = today.AddDays(-6);

        var assignmentQuery = dbContext.WorkerFarmAssignments
            .Where(a => a.OrganizationId == organizationId &&
                        a.IsActive &&
                        a.AssignedFrom <= today &&
                        (a.AssignedTo == null || a.AssignedTo >= today));

        if (farmId.HasValue)
        {
            assignmentQuery = assignmentQuery.Where(a => a.FarmId == farmId.Value);
        }

        var assignedCountsList = await assignmentQuery
            .AsNoTracking()
            .GroupBy(a => a.FarmId)
            .Select(g => new { FarmId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var farmAssignedCounts = assignedCountsList.ToDictionary(x => x.FarmId, x => x.Count);
        var totalAssignedCount = farmAssignedCounts.Values.Sum();

        var attendanceQuery = dbContext.LaborAttendances
            .Where(a => a.OrganizationId == organizationId &&
                        a.AttendanceDate >= startDate &&
                        a.AttendanceDate <= today);

        if (farmId.HasValue)
        {
            attendanceQuery = attendanceQuery.Where(a => a.FarmId == farmId.Value);
        }

        var attendanceItems = await attendanceQuery
            .AsNoTracking()
            .Select(a => new RawLaborAttendanceItem(
                a.FarmId,
                a.AttendanceDate,
                a.AttendanceType.ToString().ToUpperInvariant(),
                a.WorkingHours,
                a.CalculatedAmount,
                a.Status.ToString().ToUpperInvariant(),
                a.FinalizedAt,
                a.Worker != null && a.Worker.LaborCategory != null ? a.Worker.LaborCategory.Name : "General Labor",
                a.Currency != null ? a.Currency.Symbol : "₹"
            ))
            .ToListAsync(cancellationToken);

        return new LaborDashboardRawData(
            AttendanceItems: attendanceItems,
            FarmAssignedCounts: farmAssignedCounts,
            TotalAssignedCount: totalAssignedCount
        );
    }
}
