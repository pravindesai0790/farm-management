using FarmManagement.Application.Interfaces.Crops;
using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FarmManagement.Infrastructure.Persistence;

public sealed class CropLifecycleTemplateStore(ApplicationDbContext dbContext) : ICropLifecycleTemplateStore
{
    public Task<int> CountAsync(
        Guid organizationId,
        Guid? cropId,
        bool? isActive,
        CancellationToken cancellationToken = default) =>
        BuildQuery(organizationId, cropId, isActive).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<CropLifecycleTemplate>> ListAsync(
        Guid organizationId,
        Guid? cropId,
        int skip,
        int take,
        bool? isActive,
        CancellationToken cancellationToken = default) =>
        await BuildQuery(organizationId, cropId, isActive)
            .AsNoTracking()
            .AsSplitQuery()
            .OrderBy(template => template.Crop.Name)
            .ThenBy(template => template.Name)
            .ThenBy(template => template.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task<CropLifecycleTemplate?> FindAsync(
        Guid templateId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        BuildQuery(organizationId, null, null)
            .AsSplitQuery()
            .SingleOrDefaultAsync(template => template.Id == templateId, cancellationToken);

    public Task<Crop?> FindCropAsync(
        Guid cropId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.Crops.SingleOrDefaultAsync(
            crop => crop.Id == cropId &&
                    ((crop.IsSystem && crop.OrganizationId == null) || crop.OrganizationId == organizationId),
            cancellationToken);

    public Task<bool> SequenceExistsAsync(
        Guid templateId,
        int sequenceNumber,
        Guid? excludingStageId = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.CropLifecycleStages.Where(stage =>
            stage.LifecycleTemplateId == templateId && stage.SequenceNumber == sequenceNumber);

        if (excludingStageId is not null)
        {
            query = query.Where(stage => stage.Id != excludingStageId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public async Task ClearDefaultAsync(
        Guid? organizationId,
        Guid cropId,
        Guid excludingTemplateId,
        DateTimeOffset now,
        Guid updatedBy,
        CancellationToken cancellationToken = default)
    {
        var templates = await dbContext.CropLifecycleTemplates
            .Where(template => template.OrganizationId == organizationId &&
                               template.CropId == cropId &&
                               template.Id != excludingTemplateId &&
                               template.IsDefault)
            .ToListAsync(cancellationToken);

        foreach (var template in templates)
        {
            template.SetDefault(false, now, updatedBy);
        }
    }

    public void Add(CropLifecycleTemplate template) => dbContext.CropLifecycleTemplates.Add(template);

    public void Add(CropLifecycleStage stage) => dbContext.CropLifecycleStages.Add(stage);

    public void AddAuditLog(AuditLog auditLog) => dbContext.AuditLogs.Add(auditLog);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);

    private IQueryable<CropLifecycleTemplate> BuildQuery(Guid organizationId, Guid? cropId, bool? isActive)
    {
        var query = dbContext.CropLifecycleTemplates
            .Include(template => template.Crop)
            .Include(template => template.Stages)
            .Where(template =>
                ((template.IsSystem && template.OrganizationId == null) || template.OrganizationId == organizationId) &&
                ((template.Crop.IsSystem && template.Crop.OrganizationId == null) || template.Crop.OrganizationId == organizationId));

        if (cropId is not null)
        {
            query = query.Where(template => template.CropId == cropId.Value);
        }

        if (isActive is not null)
        {
            query = query.Where(template => template.IsActive == isActive.Value);
        }

        return query;
    }
}
