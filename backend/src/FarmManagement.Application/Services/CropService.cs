using System.Text.Json;
using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.Crops;
using FarmManagement.Application.Interfaces.Crops;
using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Services;

public sealed class CropService(ICropStore store) : ICropService
{
    private const int DefaultPageSize = 20;
    private const int MaximumPageSize = 100;
    private static readonly IReadOnlySet<string> CropDurationTypes =
        new HashSet<string>(StringComparer.Ordinal) { "ANNUAL", "PERENNIAL", "SEASONAL", "OTHER" };

    public async Task<CropListResponse> ListCropsAsync(
        CropActor actor, int page, int pageSize, string? search, bool? isActive,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        pageSize = NormalizePageSize(pageSize);
        if (page < 1) throw Validation("page", "Page must be at least 1.");

        var normalizedSearch = NormalizeSearch(search);
        var totalCount = await store.CountCropsAsync(actor.OrganizationId, normalizedSearch, isActive, cancellationToken);
        var crops = await store.ListCropsAsync(
            actor.OrganizationId, checked((page - 1) * pageSize), pageSize, normalizedSearch, isActive, cancellationToken);

        return new CropListResponse(crops.Select(ToResponse).ToArray(), page, pageSize, totalCount);
    }

    public async Task<CropResponse> GetCropAsync(CropActor actor, Guid cropId, CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        return ToResponse(await FindCropOrThrowAsync(actor, cropId, cancellationToken));
    }

    public async Task<CropResponse> CreateCropAsync(
        CropActor actor, CreateCropRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var values = ReadValues(request);
        await EnsureCropCodeIsAvailableAsync(actor, values.Code, null, cancellationToken);

        var crop = new Crop(
            actor.OrganizationId, values.Code, values.Name, values.CropType, values.CropDurationType,
            scientificName: values.ScientificName, description: values.Description, createdBy: actor.UserId);

        store.Add(crop);
        AddAudit(actor, crop, "Crop.Created", new { crop.Code, crop.Name }, ipAddress);
        await store.SaveChangesAsync(cancellationToken);
        return ToResponse(crop);
    }

    public async Task<CropResponse> UpdateCropAsync(
        CropActor actor, Guid cropId, UpdateCropRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var crop = await FindCropOrThrowAsync(actor, cropId, cancellationToken);
        EnsureCanModify(crop.IsSystem, actor);
        var values = ReadValues(request);
        await EnsureCropCodeIsAvailableAsync(actor, values.Code, crop.Id, cancellationToken);

        var previous = new { crop.Code, crop.Name, crop.CropType, crop.CropDurationType, crop.IsActive };
        crop.Update(values.Code, values.Name, values.CropType, values.CropDurationType,
            values.ScientificName, values.Description, DateTimeOffset.UtcNow, actor.UserId);
        AddAudit(actor, crop, "Crop.Updated", new { previous, current = new { crop.Code, crop.Name, crop.CropType, crop.CropDurationType } }, ipAddress);
        await store.SaveChangesAsync(cancellationToken);
        return ToResponse(crop);
    }

    public Task<bool> ActivateCropAsync(CropActor actor, Guid cropId, string? ipAddress, CancellationToken cancellationToken = default) =>
        SetCropActiveAsync(actor, cropId, true, ipAddress, cancellationToken);

    public Task<bool> DeactivateCropAsync(CropActor actor, Guid cropId, string? ipAddress, CancellationToken cancellationToken = default) =>
        SetCropActiveAsync(actor, cropId, false, ipAddress, cancellationToken);

    public async Task<CropVarietyListResponse> ListVarietiesAsync(
        CropActor actor, Guid cropId, int page, int pageSize, bool? isActive,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        pageSize = NormalizePageSize(pageSize);
        if (page < 1) throw Validation("page", "Page must be at least 1.");
        _ = await FindCropOrThrowAsync(actor, cropId, cancellationToken);

        var totalCount = await store.CountVarietiesAsync(actor.OrganizationId, cropId, isActive, cancellationToken);
        var varieties = await store.ListVarietiesAsync(
            actor.OrganizationId, cropId, checked((page - 1) * pageSize), pageSize, isActive, cancellationToken);
        return new CropVarietyListResponse(varieties.Select(variety => ToResponse(variety)).ToArray(), page, pageSize, totalCount);
    }

    public async Task<CropVarietyResponse> GetVarietyAsync(CropActor actor, Guid varietyId, CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        return ToResponse(await FindVarietyOrThrowAsync(actor, varietyId, cancellationToken));
    }

    public async Task<CropVarietyResponse> CreateVarietyAsync(
        CropActor actor, CreateCropVarietyRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var values = ReadValues(request);
        var crop = await FindActiveCropOrThrowAsync(actor, values.CropId, cancellationToken);
        if (await store.VarietyCodeExistsAsync(actor.OrganizationId, crop.Id, values.Code, cancellationToken: cancellationToken))
        {
            throw new ConflictException("A variety with this code already exists for the crop.");
        }

        var variety = new CropVariety(
            actor.OrganizationId, crop.Id, values.Code, values.Name, description: values.Description, createdBy: actor.UserId);
        store.Add(variety);
        AddAudit(actor, variety, "CropVariety.Created", new { variety.Code, variety.Name, variety.CropId }, ipAddress);
        await store.SaveChangesAsync(cancellationToken);
        return ToResponse(variety, crop);
    }

    public async Task<CropVarietyResponse> UpdateVarietyAsync(
        CropActor actor, Guid varietyId, UpdateCropVarietyRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var variety = await FindVarietyOrThrowAsync(actor, varietyId, cancellationToken);
        EnsureCanModify(variety.IsSystem, actor);
        var values = ReadValues(request);
        var crop = await FindActiveCropOrThrowAsync(actor, values.CropId, cancellationToken);
        if (variety.IsSystem && !crop.IsSystem)
        {
            throw new ForbiddenException("A system variety must belong to a system crop.");
        }
        if (await store.VarietyCodeExistsAsync(actor.OrganizationId, crop.Id, values.Code, variety.Id, cancellationToken))
        {
            throw new ConflictException("A variety with this code already exists for the crop.");
        }

        var previous = new { variety.CropId, variety.Code, variety.Name, variety.IsActive };
        variety.Update(crop.Id, values.Code, values.Name, values.Description, DateTimeOffset.UtcNow, actor.UserId);
        AddAudit(actor, variety, "CropVariety.Updated", new { previous, current = new { variety.CropId, variety.Code, variety.Name } }, ipAddress);
        await store.SaveChangesAsync(cancellationToken);
        return ToResponse(variety, crop);
    }

    public Task<bool> ActivateVarietyAsync(CropActor actor, Guid varietyId, string? ipAddress, CancellationToken cancellationToken = default) =>
        SetVarietyActiveAsync(actor, varietyId, true, ipAddress, cancellationToken);

    public Task<bool> DeactivateVarietyAsync(CropActor actor, Guid varietyId, string? ipAddress, CancellationToken cancellationToken = default) =>
        SetVarietyActiveAsync(actor, varietyId, false, ipAddress, cancellationToken);

    private async Task<bool> SetCropActiveAsync(CropActor actor, Guid cropId, bool active, string? ipAddress, CancellationToken cancellationToken)
    {
        ValidateActor(actor);
        var crop = await FindCropOrThrowAsync(actor, cropId, cancellationToken);
        EnsureCanModify(crop.IsSystem, actor);
        var changed = active ? crop.Activate(DateTimeOffset.UtcNow, actor.UserId) : crop.Deactivate(DateTimeOffset.UtcNow, actor.UserId);
        if (!changed) return false;
        AddAudit(actor, crop, active ? "Crop.Activated" : "Crop.Deactivated", null, ipAddress);
        await store.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<bool> SetVarietyActiveAsync(CropActor actor, Guid varietyId, bool active, string? ipAddress, CancellationToken cancellationToken)
    {
        ValidateActor(actor);
        var variety = await FindVarietyOrThrowAsync(actor, varietyId, cancellationToken);
        EnsureCanModify(variety.IsSystem, actor);
        var changed = active ? variety.Activate(DateTimeOffset.UtcNow, actor.UserId) : variety.Deactivate(DateTimeOffset.UtcNow, actor.UserId);
        if (!changed) return false;
        AddAudit(actor, variety, active ? "CropVariety.Activated" : "CropVariety.Deactivated", null, ipAddress);
        await store.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<Crop> FindCropOrThrowAsync(CropActor actor, Guid cropId, CancellationToken cancellationToken) =>
        cropId == Guid.Empty
            ? throw new ResourceNotFoundException("The crop was not found.")
            : await store.FindCropAsync(cropId, actor.OrganizationId, cancellationToken)
                ?? throw new ResourceNotFoundException("The crop was not found.");

    private async Task<Crop> FindActiveCropOrThrowAsync(CropActor actor, Guid cropId, CancellationToken cancellationToken)
    {
        var crop = await FindCropOrThrowAsync(actor, cropId, cancellationToken);
        if (!crop.IsActive) throw Validation("cropId", "The crop was not found or is inactive.");
        return crop;
    }

    private async Task<CropVariety> FindVarietyOrThrowAsync(CropActor actor, Guid varietyId, CancellationToken cancellationToken) =>
        varietyId == Guid.Empty
            ? throw new ResourceNotFoundException("The crop variety was not found.")
            : await store.FindVarietyAsync(varietyId, actor.OrganizationId, cancellationToken)
                ?? throw new ResourceNotFoundException("The crop variety was not found.");

    private async Task EnsureCropCodeIsAvailableAsync(CropActor actor, string code, Guid? excludingCropId, CancellationToken cancellationToken)
    {
        if (await store.CropCodeExistsAsync(actor.OrganizationId, code, excludingCropId, cancellationToken))
        {
            throw new ConflictException("A crop with this code already exists in the available crop catalog.");
        }
    }

    private void AddAudit(CropActor actor, Crop crop, string action, object? details, string? ipAddress) =>
        store.AddAuditLog(new AuditLog(action, crop.OrganizationId ?? actor.OrganizationId, actor.UserId,
            entityType: "Crop", entityId: crop.Id,
            details: details is null ? null : JsonSerializer.SerializeToDocument(details), ipAddress: ipAddress));

    private void AddAudit(CropActor actor, CropVariety variety, string action, object? details, string? ipAddress) =>
        store.AddAuditLog(new AuditLog(action, variety.OrganizationId ?? actor.OrganizationId, actor.UserId,
            entityType: "CropVariety", entityId: variety.Id,
            details: details is null ? null : JsonSerializer.SerializeToDocument(details), ipAddress: ipAddress));

    private static CropResponse ToResponse(Crop crop) =>
        new(crop.Id, crop.OrganizationId, crop.Code, crop.Name, crop.ScientificName, crop.CropType,
            crop.CropDurationType, crop.Description, crop.IsSystem, crop.IsActive, crop.CreatedAt,
            crop.CreatedBy, crop.UpdatedAt, crop.UpdatedBy);

    private static CropVarietyResponse ToResponse(CropVariety variety, Crop? crop = null) =>
        new(variety.Id, variety.OrganizationId, variety.CropId, (crop ?? variety.Crop)?.Code ?? string.Empty,
            (crop ?? variety.Crop)?.Name ?? string.Empty, variety.Code, variety.Name, variety.Description,
            variety.IsSystem, variety.IsActive, variety.CreatedAt, variety.CreatedBy, variety.UpdatedAt, variety.UpdatedBy);

    private static CropValues ReadValues(CreateCropRequest? request) =>
        request is null ? throw Validation("request", "A request body is required.") : ReadValues(
            request.Code, request.Name, request.ScientificName, request.CropType, request.CropDurationType, request.Description);

    private static CropValues ReadValues(UpdateCropRequest? request) =>
        request is null ? throw Validation("request", "A request body is required.") : ReadValues(
            request.Code, request.Name, request.ScientificName, request.CropType, request.CropDurationType, request.Description);

    private static CropValues ReadValues(string? code, string? name, string? scientificName, string? cropType, string? cropDurationType, string? description)
    {
        if (string.IsNullOrWhiteSpace(code)) throw Validation("code", "Code is required.");
        if (code.Trim().Length > 50) throw Validation("code", "Code cannot exceed 50 characters.");
        if (string.IsNullOrWhiteSpace(name)) throw Validation("name", "Name is required.");
        if (name.Trim().Length > 150) throw Validation("name", "Name cannot exceed 150 characters.");
        if (scientificName?.Trim().Length > 200) throw Validation("scientificName", "Scientific name cannot exceed 200 characters.");
        if (string.IsNullOrWhiteSpace(cropType)) throw Validation("cropType", "Crop type is required.");
        if (cropType.Trim().Length > 50) throw Validation("cropType", "Crop type cannot exceed 50 characters.");
        if (string.IsNullOrWhiteSpace(cropDurationType)) throw Validation("cropDurationType", "Crop duration type is required.");
        var normalizedDuration = cropDurationType.Trim().ToUpperInvariant();
        if (!CropDurationTypes.Contains(normalizedDuration)) throw Validation("cropDurationType", "Crop duration type is invalid.");
        if (description?.Trim().Length > 2000) throw Validation("description", "Description cannot exceed 2000 characters.");
        return new(code.Trim().ToUpperInvariant(), name.Trim(), NormalizeOptional(scientificName), cropType.Trim(), normalizedDuration, NormalizeOptional(description));
    }

    private static VarietyValues ReadValues(CreateCropVarietyRequest? request) =>
        request is null ? throw Validation("request", "A request body is required.") : ReadValues(request.CropId, request.Code, request.Name, request.Description);

    private static VarietyValues ReadValues(UpdateCropVarietyRequest? request) =>
        request is null ? throw Validation("request", "A request body is required.") : ReadValues(request.CropId, request.Code, request.Name, request.Description);

    private static VarietyValues ReadValues(Guid? cropId, string? code, string? name, string? description)
    {
        if (cropId is null || cropId == Guid.Empty) throw Validation("cropId", "Crop is required.");
        if (string.IsNullOrWhiteSpace(code)) throw Validation("code", "Code is required.");
        if (code.Trim().Length > 50) throw Validation("code", "Code cannot exceed 50 characters.");
        if (string.IsNullOrWhiteSpace(name)) throw Validation("name", "Name is required.");
        if (name.Trim().Length > 150) throw Validation("name", "Name cannot exceed 150 characters.");
        if (description?.Trim().Length > 2000) throw Validation("description", "Description cannot exceed 2000 characters.");
        return new(cropId.Value, code.Trim().ToUpperInvariant(), name.Trim(), NormalizeOptional(description));
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string? NormalizeSearch(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static int NormalizePageSize(int value) => value == 0 ? DefaultPageSize : value is < 1 or > MaximumPageSize
        ? throw Validation("pageSize", $"Page size must be between 1 and {MaximumPageSize}.") : value;

    private static void EnsureCanModify(bool isSystem, CropActor actor)
    {
        if (isSystem && !actor.IsGlobalAdmin)
        {
            throw new ForbiddenException("System crops and varieties can only be managed by a global administrator.");
        }
    }

    private static void ValidateActor(CropActor actor)
    {
        if (actor.UserId == Guid.Empty || actor.OrganizationId == Guid.Empty)
            throw new UnauthorizedAccessException("The access token does not contain a valid user scope.");
    }

    private static ValidationException Validation(string fieldName, string message) =>
        new("Validation failed", new Dictionary<string, string[]> { [fieldName] = [message] });

    private sealed record CropValues(string Code, string Name, string? ScientificName, string CropType, string CropDurationType, string? Description);
    private sealed record VarietyValues(Guid CropId, string Code, string Name, string? Description);
}
