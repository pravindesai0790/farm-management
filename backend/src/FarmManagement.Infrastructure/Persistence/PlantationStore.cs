using System.Data;
using FarmManagement.Application.Interfaces.Plantations;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FarmManagement.Infrastructure.Persistence;

public sealed class PlantationStore(ApplicationDbContext dbContext) : IPlantationStore
{
    public async Task<IReadOnlyList<CropPlantation>> ListAsync(
        Guid organizationId,
        Guid? farmId,
        Guid? farmAreaId,
        PlantationStatus? status,
        CancellationToken cancellationToken = default) =>
        await BuildQuery(organizationId, farmId, farmAreaId, status)
            .AsNoTracking()
            .OrderBy(plantation => plantation.PlantationName)
            .ThenBy(plantation => plantation.PlantationCode)
            .ThenBy(plantation => plantation.Id)
            .ToListAsync(cancellationToken);

    public Task<CropPlantation?> FindAsync(Guid plantationId, Guid organizationId, CancellationToken cancellationToken = default) =>
        BuildQuery(organizationId, null, null, null)
            .SingleOrDefaultAsync(plantation => plantation.Id == plantationId, cancellationToken);

    public Task<FarmArea?> FindFarmAreaAsync(Guid farmAreaId, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.FarmAreas
            .Include(area => area.AreaUnit)
            .Include(area => area.Farm)
                .ThenInclude(farm => farm!.AreaUnit)
            .SingleOrDefaultAsync(area => area.Id == farmAreaId && area.OrganizationId == organizationId, cancellationToken);

    public Task<FarmArea?> LockFarmAreaAsync(Guid farmAreaId, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.FarmAreas
            .FromSqlInterpolated($"SELECT * FROM farm_areas WHERE id = {farmAreaId} AND organization_id = {organizationId} FOR UPDATE")
            .Include(area => area.AreaUnit)
            .Include(area => area.Farm)
                .ThenInclude(farm => farm!.AreaUnit)
            .SingleOrDefaultAsync(cancellationToken);

    public Task<Crop?> FindCropAsync(Guid cropId, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.Crops.SingleOrDefaultAsync(
            crop => crop.Id == cropId && crop.IsActive &&
                    ((crop.IsSystem && crop.OrganizationId == null) || crop.OrganizationId == organizationId),
            cancellationToken);

    public Task<CropVariety?> FindVarietyAsync(Guid varietyId, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.CropVarieties
            .Include(variety => variety.Crop)
            .SingleOrDefaultAsync(
                variety => variety.Id == varietyId && variety.IsActive &&
                           ((variety.IsSystem && variety.OrganizationId == null) || variety.OrganizationId == organizationId),
                cancellationToken);

    public Task<CropLifecycleTemplate?> FindLifecycleTemplateAsync(Guid templateId, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.CropLifecycleTemplates
            .Include(template => template.Crop)
            .SingleOrDefaultAsync(
                template => template.Id == templateId && template.IsActive &&
                            ((template.IsSystem && template.OrganizationId == null) || template.OrganizationId == organizationId),
                cancellationToken);

    public Task<PlantationEndReason?> FindEndReasonAsync(Guid endReasonId, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.PlantationEndReasons.SingleOrDefaultAsync(
            reason => reason.Id == endReasonId && reason.IsActive &&
                      ((reason.IsSystem && reason.OrganizationId == null) || reason.OrganizationId == organizationId),
            cancellationToken);

    public Task<Unit?> FindAreaUnitAsync(Guid unitId, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.Units.SingleOrDefaultAsync(
            unit => unit.Id == unitId && unit.IsActive && unit.UnitCategory == UnitCategory.Area &&
                    (unit.OrganizationId == null || unit.OrganizationId == organizationId),
            cancellationToken);

    public Task<bool> CodeExistsAsync(
        Guid organizationId, string code, Guid? excludingPlantationId = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.CropPlantations.Where(plantation =>
            plantation.OrganizationId == organizationId && plantation.PlantationCode == code);
        if (excludingPlantationId is not null)
        {
            query = query.Where(plantation => plantation.Id != excludingPlantationId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CropPlantation>> ListActiveAllocationsAsync(
        Guid farmAreaId,
        Guid? excludingPlantationId = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.CropPlantations
            .Include(plantation => plantation.AreaUnit)
            .Where(plantation => plantation.FarmAreaId == farmAreaId && plantation.Status == PlantationStatus.Active);
        if (excludingPlantationId is not null)
        {
            query = query.Where(plantation => plantation.Id != excludingPlantationId.Value);
        }

        return await query.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FarmArea>> ListActiveChildrenAsync(Guid parentFarmAreaId, CancellationToken cancellationToken = default) =>
        await dbContext.FarmAreas
            .Include(area => area.AreaUnit)
            .Where(area => area.ParentFarmAreaId == parentFarmAreaId && area.IsActive)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        try
        {
            var result = await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public void Add(CropPlantation plantation) => dbContext.CropPlantations.Add(plantation);
    public void AddAuditLog(AuditLog auditLog) => dbContext.AuditLogs.Add(auditLog);
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => dbContext.SaveChangesAsync(cancellationToken);

    private IQueryable<CropPlantation> BuildQuery(Guid organizationId, Guid? farmId, Guid? farmAreaId, PlantationStatus? status)
    {
        var query = dbContext.CropPlantations
            .Include(plantation => plantation.FarmArea)
            .Include(plantation => plantation.Crop)
            .Include(plantation => plantation.Variety)
            .Include(plantation => plantation.LifecycleTemplate)
            .Include(plantation => plantation.AreaUnit)
            .Include(plantation => plantation.EndReason)
            .Where(plantation => plantation.OrganizationId == organizationId);

        if (farmId is not null) query = query.Where(plantation => plantation.FarmId == farmId.Value);
        if (farmAreaId is not null) query = query.Where(plantation => plantation.FarmAreaId == farmAreaId.Value);
        if (status is not null) query = query.Where(plantation => plantation.Status == status.Value);
        return query;
    }
}
