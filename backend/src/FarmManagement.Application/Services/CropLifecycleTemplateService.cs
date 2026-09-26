using System.Text.Json;
using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Crops;
using FarmManagement.Application.Interfaces.Crops;
using FarmManagement.Application.Services.Helpers;
using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Services;

public sealed class CropLifecycleTemplateService(ICropLifecycleTemplateStore store) : ICropLifecycleTemplateService
{
    public Task<PagedResponse<CropLifecycleTemplateResponse>> ListAsync(
        CropLifecycleTemplateActor actor,
        CropLifecycleTemplateQuery query,
        CancellationToken cancellationToken = default) =>
        ListAsync(actor, query.Page, query.PageSize, query.CropId, query.IsActive, cancellationToken);

    public async Task<PagedResponse<CropLifecycleTemplateResponse>> ListAsync(
        CropLifecycleTemplateActor actor,
        int page,
        int pageSize,
        Guid? cropId,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        pageSize = CropLifecycleTemplateValidationHelper.NormalizePageSize(pageSize);
        if (page < 1) throw CropLifecycleTemplateValidationHelper.Validation("page", "Page must be at least 1.");
        if (cropId == Guid.Empty) throw CropLifecycleTemplateValidationHelper.Validation("cropId", "Crop must be valid.");

        if (cropId is not null)
        {
            _ = await FindCropOrThrowAsync(actor, cropId.Value, cancellationToken);
        }

        var totalCount = await store.CountAsync(actor.OrganizationId, cropId, isActive, cancellationToken);
        var templates = await store.ListAsync(
            actor.OrganizationId,
            cropId,
            checked((page - 1) * pageSize),
            pageSize,
            isActive,
            cancellationToken);

        return new PagedResponse<CropLifecycleTemplateResponse>(
            templates.Select(t => ToResponse(t)).ToArray(), page, pageSize, totalCount);
    }

    public async Task<CropLifecycleTemplateResponse> GetAsync(
        CropLifecycleTemplateActor actor,
        Guid templateId,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        return ToResponse(await FindTemplateOrThrowAsync(actor, templateId, cancellationToken));
    }

    public async Task<CropLifecycleStageResponse> GetStageAsync(
        CropLifecycleTemplateActor actor,
        Guid templateId,
        Guid stageId,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var template = await FindTemplateOrThrowAsync(actor, templateId, cancellationToken);
        return ToResponse(FindStageOrThrow(template, stageId));
    }

    public async Task<CropLifecycleTemplateResponse> CreateAsync(
        CropLifecycleTemplateActor actor,
        CreateCropLifecycleTemplateRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var values = CropLifecycleTemplateValidationHelper.ReadTemplateValues(request);
        var batchStages = CropLifecycleTemplateValidationHelper.ReadBatchStageValues(request.Stages);
        var crop = await FindActiveCropOrThrowAsync(actor, values.CropId, cancellationToken);

        var template = new CropLifecycleTemplate(
            actor.OrganizationId,
            crop.Id,
            values.Name,
            values.IsDefault,
            description: values.Description,
            createdBy: actor.UserId);

        if (values.IsDefault)
        {
            await store.ClearDefaultAsync(
                actor.OrganizationId,
                crop.Id,
                template.Id,
                DateTimeOffset.UtcNow,
                actor.UserId,
                cancellationToken);
        }

        store.Add(template);

        foreach (var stageValues in batchStages)
        {
            var stage = new CropLifecycleStage(
                template.Id,
                stageValues.StageName,
                stageValues.SequenceNumber,
                stageValues.ExpectedDurationDays,
                stageValues.Description);

            store.Add(stage);
            template.Stages.Add(stage);
        }

        AddAudit(actor, template, "CropLifecycleTemplate.Created", new { template.Name, template.CropId, StageCount = batchStages.Count }, ipAddress);
        await store.SaveChangesAsync(cancellationToken);
        return ToResponse(template, crop.Name);
    }

    public async Task<CropLifecycleTemplateResponse> UpdateAsync(
        CropLifecycleTemplateActor actor,
        Guid templateId,
        UpdateCropLifecycleTemplateRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var template = await FindTemplateOrThrowAsync(actor, templateId, cancellationToken);
        EnsureCanModify(template.IsSystem, actor);
        var values = CropLifecycleTemplateValidationHelper.ReadTemplateValues(request);

        if (!template.IsActive && values.IsDefault)
        {
            throw CropLifecycleTemplateValidationHelper.Validation("isDefault", "An inactive template cannot be set as the default template.");
        }

        var crop = await FindActiveCropOrThrowAsync(actor, values.CropId, cancellationToken);
        if (template.IsSystem && !crop.IsSystem)
        {
            throw new ForbiddenException("A system lifecycle template must belong to a system crop.");
        }

        var previous = new { template.CropId, template.Name, template.IsDefault, template.IsActive };
        template.Update(crop.Id, values.Name, values.IsDefault, values.Description, DateTimeOffset.UtcNow, actor.UserId);
        if (values.IsDefault)
        {
            await store.ClearDefaultAsync(
                template.OrganizationId,
                crop.Id,
                template.Id,
                DateTimeOffset.UtcNow,
                actor.UserId,
                cancellationToken);
        }

        AddAudit(actor, template, "CropLifecycleTemplate.Updated", new
        {
            previous,
            current = new { template.CropId, template.Name, template.IsDefault }
        }, ipAddress);
        await store.SaveChangesAsync(cancellationToken);
        return ToResponse(template, crop.Name);
    }

    public Task<bool> ActivateAsync(
        CropLifecycleTemplateActor actor,
        Guid templateId,
        string? ipAddress,
        CancellationToken cancellationToken = default) =>
        SetTemplateActiveAsync(actor, templateId, true, ipAddress, cancellationToken);

    public Task<bool> DeactivateAsync(
        CropLifecycleTemplateActor actor,
        Guid templateId,
        string? ipAddress,
        CancellationToken cancellationToken = default) =>
        SetTemplateActiveAsync(actor, templateId, false, ipAddress, cancellationToken);

    public async Task<CropLifecycleStageResponse> CreateStageAsync(
        CropLifecycleTemplateActor actor,
        Guid templateId,
        CreateCropLifecycleStageRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var template = await FindTemplateOrThrowAsync(actor, templateId, cancellationToken);
        EnsureCanModify(template.IsSystem, actor);
        EnsureTemplateIsActive(template);
        var values = CropLifecycleTemplateValidationHelper.ReadStageValues(request);
        await EnsureSequenceIsAvailableAsync(template, values.SequenceNumber, null, cancellationToken);

        var stage = new CropLifecycleStage(template.Id, values.StageName, values.SequenceNumber, values.ExpectedDurationDays, values.Description);
        store.Add(stage);
        AddAudit(actor, stage, "CropLifecycleStage.Created", new { stage.StageName, stage.SequenceNumber, stage.ExpectedDurationDays }, ipAddress);
        await store.SaveChangesAsync(cancellationToken);
        return ToResponse(stage);
    }

    public async Task<CropLifecycleStageResponse> UpdateStageAsync(
        CropLifecycleTemplateActor actor,
        Guid templateId,
        Guid stageId,
        UpdateCropLifecycleStageRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var template = await FindTemplateOrThrowAsync(actor, templateId, cancellationToken);
        EnsureCanModify(template.IsSystem, actor);
        EnsureTemplateIsActive(template);
        var stage = FindStageOrThrow(template, stageId);
        var values = CropLifecycleTemplateValidationHelper.ReadStageValues(request);
        await EnsureSequenceIsAvailableAsync(template, values.SequenceNumber, stage.Id, cancellationToken);

        var previous = new { stage.StageName, stage.SequenceNumber, stage.ExpectedDurationDays, stage.IsActive };
        stage.Update(values.StageName, values.SequenceNumber, values.ExpectedDurationDays, values.Description);
        AddAudit(actor, stage, "CropLifecycleStage.Updated", new
        {
            previous,
            current = new { stage.StageName, stage.SequenceNumber, stage.ExpectedDurationDays }
        }, ipAddress);
        await store.SaveChangesAsync(cancellationToken);
        return ToResponse(stage);
    }

    public Task<bool> ActivateStageAsync(
        CropLifecycleTemplateActor actor,
        Guid templateId,
        Guid stageId,
        string? ipAddress,
        CancellationToken cancellationToken = default) =>
        SetStageActiveAsync(actor, templateId, stageId, true, ipAddress, cancellationToken);

    public Task<bool> DeactivateStageAsync(
        CropLifecycleTemplateActor actor,
        Guid templateId,
        Guid stageId,
        string? ipAddress,
        CancellationToken cancellationToken = default) =>
        SetStageActiveAsync(actor, templateId, stageId, false, ipAddress, cancellationToken);

    public async Task<IReadOnlyList<CropLifecycleStageResponse>> ReorderStagesAsync(
        CropLifecycleTemplateActor actor,
        Guid templateId,
        ReorderCropLifecycleStagesRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        if (request?.Stages is null || request.Stages.Count == 0)
        {
            throw CropLifecycleTemplateValidationHelper.Validation("stages", "Stage reorder request body with stage positions is required.");
        }

        var template = await FindTemplateOrThrowAsync(actor, templateId, cancellationToken);
        EnsureCanModify(template.IsSystem, actor);
        EnsureTemplateIsActive(template);

        var stageMap = template.Stages.ToDictionary(s => s.Id);
        if (request.Stages.Count != template.Stages.Count || request.Stages.Any(item => !stageMap.ContainsKey(item.StageId)))
        {
            throw CropLifecycleTemplateValidationHelper.Validation("stages", "Reorder stage list must include all existing stages for this template.");
        }

        var sequences = new HashSet<int>();
        foreach (var item in request.Stages)
        {
            if (item.SequenceNumber <= 0)
            {
                throw CropLifecycleTemplateValidationHelper.Validation("sequenceNumber", "Sequence numbers must be greater than zero.");
            }
            if (!sequences.Add(item.SequenceNumber))
            {
                throw CropLifecycleTemplateValidationHelper.Validation("sequenceNumber", $"Duplicate sequence number {item.SequenceNumber} in reorder request.");
            }
        }

        // Assign temporary positive offset sequence numbers to avoid unique index violation during EF Core save
        foreach (var item in request.Stages)
        {
            var stage = stageMap[item.StageId];
            stage.Update(stage.StageName, item.SequenceNumber + 10000, stage.ExpectedDurationDays, stage.Description);
        }

        foreach (var item in request.Stages)
        {
            var stage = stageMap[item.StageId];
            stage.Update(stage.StageName, item.SequenceNumber, stage.ExpectedDurationDays, stage.Description);
        }

        AddAudit(actor, template, "CropLifecycleTemplate.StagesReordered", new { StageCount = request.Stages.Count }, ipAddress);
        await store.SaveChangesAsync(cancellationToken);

        return template.Stages.OrderBy(s => s.SequenceNumber).Select(ToResponse).ToArray();
    }

    private async Task<bool> SetTemplateActiveAsync(
        CropLifecycleTemplateActor actor,
        Guid templateId,
        bool active,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        ValidateActor(actor);
        var template = await FindTemplateOrThrowAsync(actor, templateId, cancellationToken);
        EnsureCanModify(template.IsSystem, actor);

        if (!active && template.IsDefault)
        {
            throw CropLifecycleTemplateValidationHelper.Validation("isActive", "A default lifecycle template cannot be deactivated. Designate another active template as default first.");
        }

        var changed = active
            ? template.Activate(DateTimeOffset.UtcNow, actor.UserId)
            : template.Deactivate(DateTimeOffset.UtcNow, actor.UserId);
        if (!changed) return false;

        AddAudit(actor, template, active ? "CropLifecycleTemplate.Activated" : "CropLifecycleTemplate.Deactivated", null, ipAddress);
        await store.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<bool> SetStageActiveAsync(
        CropLifecycleTemplateActor actor,
        Guid templateId,
        Guid stageId,
        bool active,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        ValidateActor(actor);
        var template = await FindTemplateOrThrowAsync(actor, templateId, cancellationToken);
        EnsureCanModify(template.IsSystem, actor);
        var stage = FindStageOrThrow(template, stageId);
        var changed = active ? stage.Activate() : stage.Deactivate();
        if (!changed) return false;

        AddAudit(actor, stage, active ? "CropLifecycleStage.Activated" : "CropLifecycleStage.Deactivated", null, ipAddress);
        await store.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<CropLifecycleTemplate> FindTemplateOrThrowAsync(
        CropLifecycleTemplateActor actor,
        Guid templateId,
        CancellationToken cancellationToken) =>
        templateId == Guid.Empty
            ? throw new ResourceNotFoundException("The crop lifecycle template was not found.")
            : await store.FindAsync(templateId, actor.OrganizationId, cancellationToken)
                ?? throw new ResourceNotFoundException("The crop lifecycle template was not found.");

    private async Task<Crop> FindCropOrThrowAsync(
        CropLifecycleTemplateActor actor,
        Guid cropId,
        CancellationToken cancellationToken) =>
        cropId == Guid.Empty
            ? throw new ResourceNotFoundException("The crop was not found.")
            : await store.FindCropAsync(cropId, actor.OrganizationId, cancellationToken)
                ?? throw new ResourceNotFoundException("The crop was not found.");

    private async Task<Crop> FindActiveCropOrThrowAsync(
        CropLifecycleTemplateActor actor,
        Guid cropId,
        CancellationToken cancellationToken)
    {
        var crop = await FindCropOrThrowAsync(actor, cropId, cancellationToken);
        if (!crop.IsActive) throw CropLifecycleTemplateValidationHelper.Validation("cropId", "The crop was not found or is inactive.");
        return crop;
    }

    private async Task EnsureSequenceIsAvailableAsync(
        CropLifecycleTemplate template,
        int sequenceNumber,
        Guid? excludingStageId,
        CancellationToken cancellationToken)
    {
        if (await store.SequenceExistsAsync(template.Id, sequenceNumber, excludingStageId, cancellationToken))
        {
            throw new ConflictException("A lifecycle stage with this sequence number already exists in the template.");
        }
    }

    private static CropLifecycleStage FindStageOrThrow(CropLifecycleTemplate template, Guid stageId) =>
        stageId == Guid.Empty
            ? throw new ResourceNotFoundException("The crop lifecycle stage was not found.")
            : template.Stages.SingleOrDefault(stage => stage.Id == stageId)
                ?? throw new ResourceNotFoundException("The crop lifecycle stage was not found.");

    private static void EnsureTemplateIsActive(CropLifecycleTemplate template)
    {
        if (!template.IsActive) throw CropLifecycleTemplateValidationHelper.Validation("templateId", "The lifecycle template is inactive.");
    }

    private void AddAudit(
        CropLifecycleTemplateActor actor,
        CropLifecycleTemplate template,
        string action,
        object? details,
        string? ipAddress) =>
        store.AddAuditLog(new AuditLog(
            action,
            template.OrganizationId ?? actor.OrganizationId,
            actor.UserId,
            entityType: "CropLifecycleTemplate",
            entityId: template.Id,
            details: details is null ? null : JsonSerializer.SerializeToDocument(details),
            ipAddress: ipAddress));

    private void AddAudit(
        CropLifecycleTemplateActor actor,
        CropLifecycleStage stage,
        string action,
        object? details,
        string? ipAddress) =>
        store.AddAuditLog(new AuditLog(
            action,
            actor.OrganizationId,
            actor.UserId,
            entityType: "CropLifecycleStage",
            entityId: stage.Id,
            details: details is null ? null : JsonSerializer.SerializeToDocument(details),
            ipAddress: ipAddress));

    private static CropLifecycleTemplateResponse ToResponse(CropLifecycleTemplate template, string? cropName = null) =>
        new(
            template.Id,
            template.OrganizationId,
            template.CropId,
            cropName ?? template.Crop?.Name ?? string.Empty,
            template.Name,
            template.Description,
            template.IsDefault,
            template.IsSystem,
            template.IsActive,
            template.CreatedAt,
            template.CreatedBy,
            template.UpdatedAt,
            template.UpdatedBy,
            template.Stages.OrderBy(stage => stage.SequenceNumber).Select(ToResponse).ToArray());

    private static CropLifecycleStageResponse ToResponse(CropLifecycleStage stage) =>
        new(stage.Id, stage.LifecycleTemplateId, stage.StageName, stage.SequenceNumber, stage.ExpectedDurationDays, stage.Description, stage.IsActive);

    private static void EnsureCanModify(bool isSystem, CropLifecycleTemplateActor actor)
    {
        if (isSystem && !actor.IsGlobalAdmin)
        {
            throw new ForbiddenException("System lifecycle templates can only be managed by a global administrator.");
        }
    }

    private static void ValidateActor(CropLifecycleTemplateActor actor)
    {
        if (actor.UserId == Guid.Empty || actor.OrganizationId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("The access token does not contain a valid user scope.");
        }
    }
}
