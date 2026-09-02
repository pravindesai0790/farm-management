using System.Text.Json;
using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.Crops;
using FarmManagement.Application.Interfaces.Crops;
using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Services;

public sealed class CropLifecycleTemplateService(ICropLifecycleTemplateStore store) : ICropLifecycleTemplateService
{
    private const int DefaultPageSize = 20;
    private const int MaximumPageSize = 100;

    public async Task<CropLifecycleTemplateListResponse> ListAsync(
        CropLifecycleTemplateActor actor,
        int page,
        int pageSize,
        Guid? cropId,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        pageSize = NormalizePageSize(pageSize);
        if (page < 1) throw Validation("page", "Page must be at least 1.");
        if (cropId == Guid.Empty) throw Validation("cropId", "Crop must be valid.");

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

        return new CropLifecycleTemplateListResponse(
            templates.Select(ToResponse).ToArray(), page, pageSize, totalCount);
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
        var values = ReadTemplateValues(request);
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
        AddAudit(actor, template, "CropLifecycleTemplate.Created", new { template.Name, template.CropId }, ipAddress);
        await store.SaveChangesAsync(cancellationToken);
        return ToResponse(template);
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
        var values = ReadTemplateValues(request);
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
        return ToResponse(template);
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
        var values = ReadStageValues(request);
        await EnsureSequenceIsAvailableAsync(template, values.SequenceNumber, null, cancellationToken);

        var stage = new CropLifecycleStage(template.Id, values.StageCode, values.StageName, values.SequenceNumber, values.Description);
        store.Add(stage);
        AddAudit(actor, stage, "CropLifecycleStage.Created", new { stage.StageCode, stage.StageName, stage.SequenceNumber }, ipAddress);
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
        var values = ReadStageValues(request);
        await EnsureSequenceIsAvailableAsync(template, values.SequenceNumber, stage.Id, cancellationToken);

        var previous = new { stage.StageCode, stage.StageName, stage.SequenceNumber, stage.IsActive };
        stage.Update(values.StageCode, values.StageName, values.SequenceNumber, values.Description);
        AddAudit(actor, stage, "CropLifecycleStage.Updated", new
        {
            previous,
            current = new { stage.StageCode, stage.StageName, stage.SequenceNumber }
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
        if (!crop.IsActive) throw Validation("cropId", "The crop was not found or is inactive.");
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
        if (!template.IsActive) throw Validation("templateId", "The lifecycle template is inactive.");
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

    private static CropLifecycleTemplateResponse ToResponse(CropLifecycleTemplate template) =>
        new(
            template.Id,
            template.OrganizationId,
            template.CropId,
            template.Crop.Code,
            template.Crop.Name,
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
        new(stage.Id, stage.LifecycleTemplateId, stage.StageCode, stage.StageName, stage.SequenceNumber, stage.Description, stage.IsActive);

    private static TemplateValues ReadTemplateValues(CreateCropLifecycleTemplateRequest? request) =>
        request is null
            ? throw Validation("request", "A request body is required.")
            : ReadTemplateValues(request.CropId, request.Name, request.Description, request.IsDefault);

    private static TemplateValues ReadTemplateValues(UpdateCropLifecycleTemplateRequest? request) =>
        request is null
            ? throw Validation("request", "A request body is required.")
            : ReadTemplateValues(request.CropId, request.Name, request.Description, request.IsDefault);

    private static TemplateValues ReadTemplateValues(Guid? cropId, string? name, string? description, bool isDefault)
    {
        if (cropId is null || cropId == Guid.Empty) throw Validation("cropId", "Crop is required.");
        if (string.IsNullOrWhiteSpace(name)) throw Validation("name", "Name is required.");
        if (name.Trim().Length > 150) throw Validation("name", "Name cannot exceed 150 characters.");
        if (description?.Trim().Length > 2000) throw Validation("description", "Description cannot exceed 2000 characters.");
        return new(cropId.Value, name.Trim(), NormalizeOptional(description), isDefault);
    }

    private static StageValues ReadStageValues(CreateCropLifecycleStageRequest? request) =>
        request is null
            ? throw Validation("request", "A request body is required.")
            : ReadStageValues(request.StageCode, request.StageName, request.SequenceNumber, request.Description);

    private static StageValues ReadStageValues(UpdateCropLifecycleStageRequest? request) =>
        request is null
            ? throw Validation("request", "A request body is required.")
            : ReadStageValues(request.StageCode, request.StageName, request.SequenceNumber, request.Description);

    private static StageValues ReadStageValues(string? stageCode, string? stageName, int sequenceNumber, string? description)
    {
        if (string.IsNullOrWhiteSpace(stageCode)) throw Validation("stageCode", "Stage code is required.");
        if (stageCode.Trim().Length > 50) throw Validation("stageCode", "Stage code cannot exceed 50 characters.");
        if (string.IsNullOrWhiteSpace(stageName)) throw Validation("stageName", "Stage name is required.");
        if (stageName.Trim().Length > 150) throw Validation("stageName", "Stage name cannot exceed 150 characters.");
        if (sequenceNumber <= 0) throw Validation("sequenceNumber", "Sequence number must be greater than zero.");
        if (description?.Trim().Length > 2000) throw Validation("description", "Description cannot exceed 2000 characters.");
        return new(stageCode.Trim().ToUpperInvariant(), stageName.Trim(), sequenceNumber, NormalizeOptional(description));
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static int NormalizePageSize(int value) => value == 0
        ? DefaultPageSize
        : value is < 1 or > MaximumPageSize
            ? throw Validation("pageSize", $"Page size must be between 1 and {MaximumPageSize}.")
            : value;

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

    private static ValidationException Validation(string fieldName, string message) =>
        new("Validation failed", new Dictionary<string, string[]> { [fieldName] = [message] });

    private sealed record TemplateValues(Guid CropId, string Name, string? Description, bool IsDefault);
    private sealed record StageValues(string StageCode, string StageName, int SequenceNumber, string? Description);
}
