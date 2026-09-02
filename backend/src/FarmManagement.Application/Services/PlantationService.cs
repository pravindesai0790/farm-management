using System.Text.Json;
using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.Plantations;
using FarmManagement.Application.Interfaces.Plantations;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.Services;

public sealed class PlantationService(IPlantationStore store) : IPlantationService
{
    public async Task<PlantationListResponse> ListAsync(
        PlantationActor actor,
        Guid? farmId,
        Guid? farmAreaId,
        string? status,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var parsedStatus = ParseStatus(status);
        var plantations = await store.ListAsync(actor.OrganizationId, farmId, farmAreaId, parsedStatus, cancellationToken);
        return new PlantationListResponse(plantations.Select(ToResponse).ToArray(), plantations.Count);
    }

    public async Task<PlantationResponse> GetAsync(
        PlantationActor actor, Guid plantationId, CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        return ToResponse(await FindPlantationOrThrowAsync(actor, plantationId, cancellationToken));
    }

    public async Task<PlantationResponse> CreateAsync(
        PlantationActor actor,
        CreatePlantationRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var values = ReadValues(request);

        return await store.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            var area = await FindLockedAreaOrThrowAsync(actor, values.FarmAreaId, transactionCancellationToken);
            EnsureAreaIsAvailable(area);
            var references = await ValidateReferencesAsync(actor, area, values, transactionCancellationToken);
            ValidateAllocation(area, values.AllocatedArea, references.AreaUnit,
                await store.ListActiveAllocationsAsync(area.Id, cancellationToken: transactionCancellationToken),
                await store.ListActiveChildrenAsync(area.Id, transactionCancellationToken));
            await EnsureCodeIsAvailableAsync(actor, values.PlantationCode, null, transactionCancellationToken);

            // Planned plantations do not consume area. The allocation is checked again atomically when activated.
            var plantation = new CropPlantation(
                actor.OrganizationId,
                area.FarmId,
                area.Id,
                references.Crop.Id,
                references.Variety?.Id,
                references.LifecycleTemplate?.Id,
                values.PlantationCode,
                values.PlantationName,
                values.AllocatedArea,
                references.AreaUnit.Id,
                values.PlantingDate,
                values.ExpectedEndDate,
                actor.UserId);

            store.Add(plantation);
            AddAudit(actor, plantation, "Plantation.Created", new { plantation.PlantationCode, plantation.PlantationName, plantation.AllocatedArea, Status = plantation.Status.ToString().ToUpperInvariant() }, ipAddress);
            await store.SaveChangesAsync(transactionCancellationToken);
            return ToResponse(plantation, area, references.Crop, references.Variety, references.LifecycleTemplate, references.AreaUnit, null);
        }, cancellationToken);
    }

    public async Task<PlantationResponse> UpdateAsync(
        PlantationActor actor,
        Guid plantationId,
        UpdatePlantationRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var values = ReadValues(request);

        return await store.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            var current = await FindPlantationOrThrowAsync(actor, plantationId, transactionCancellationToken);
            if (current.Status is PlantationStatus.Terminated or PlantationStatus.Archived)
            {
                throw new ConflictException("A terminated or archived plantation cannot be modified.");
            }

            var area = await FindLockedAreaOrThrowAsync(actor, values.FarmAreaId, transactionCancellationToken);
            EnsureAreaIsAvailable(area);
            if (area.FarmId != current.FarmId)
            {
                throw Validation("farmAreaId", "The farm area must belong to the plantation's farm.");
            }
            var references = await ValidateReferencesAsync(actor, area, values, transactionCancellationToken);
            await EnsureCodeIsAvailableAsync(actor, values.PlantationCode, current.Id, transactionCancellationToken);

            ValidateAllocation(area, values.AllocatedArea, references.AreaUnit,
                await store.ListActiveAllocationsAsync(area.Id, current.Status == PlantationStatus.Active ? current.Id : null, transactionCancellationToken),
                await store.ListActiveChildrenAsync(area.Id, transactionCancellationToken));

            var previous = new { current.FarmAreaId, current.CropId, current.VarietyId, current.PlantationCode, current.AllocatedArea, current.AreaUnitId, current.Status };
            current.Update(area.Id, references.Crop.Id, references.Variety?.Id, references.LifecycleTemplate?.Id,
                values.PlantationCode, values.PlantationName, values.AllocatedArea, references.AreaUnit.Id,
                values.PlantingDate, values.ExpectedEndDate, DateTimeOffset.UtcNow, actor.UserId);
            AddAudit(actor, current, "Plantation.Updated", new { previous, current = new { current.FarmAreaId, current.CropId, current.VarietyId, current.PlantationCode, current.AllocatedArea, current.AreaUnitId, current.Status } }, ipAddress);
            await store.SaveChangesAsync(transactionCancellationToken);
            return ToResponse(current, area, references.Crop, references.Variety, references.LifecycleTemplate, references.AreaUnit, current.EndReason);
        }, cancellationToken);
    }

    public async Task<bool> ActivateAsync(
        PlantationActor actor, Guid plantationId, string? ipAddress, CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        return await store.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            var current = await FindPlantationOrThrowAsync(actor, plantationId, transactionCancellationToken);
            var area = await FindLockedAreaOrThrowAsync(actor, current.FarmAreaId, transactionCancellationToken);
            EnsureAreaIsAvailable(area);
            if (current.Status != PlantationStatus.Planned)
            {
                throw new ConflictException("Only a planned plantation can be activated.");
            }

            var areaUnit = await FindAreaUnitOrThrowAsync(actor, current.AreaUnitId, transactionCancellationToken);
            ValidateAllocation(area, current.AllocatedArea, areaUnit,
                await store.ListActiveAllocationsAsync(area.Id, current.Id, transactionCancellationToken),
                await store.ListActiveChildrenAsync(area.Id, transactionCancellationToken));
            if (!current.Activate(DateTimeOffset.UtcNow, actor.UserId))
            {
                return false;
            }

            AddAudit(actor, current, "Plantation.Activated", new { PreviousStatus = "PLANNED", NewStatus = "ACTIVE" }, ipAddress);
            await store.SaveChangesAsync(transactionCancellationToken);
            return true;
        }, cancellationToken);
    }

    public async Task<bool> TerminateAsync(
        PlantationActor actor,
        Guid plantationId,
        TerminatePlantationRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        if (request is null) throw Validation("request", "A request body is required.");
        if (request.TerminationDate is null) throw Validation("terminationDate", "Termination date is required.");
        if (request.EndReasonId is null || request.EndReasonId == Guid.Empty) throw Validation("endReasonId", "Termination reason is required.");

        return await store.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            var current = await FindPlantationOrThrowAsync(actor, plantationId, transactionCancellationToken);
            var area = await FindLockedAreaOrThrowAsync(actor, current.FarmAreaId, transactionCancellationToken);
            EnsureAreaIsAvailable(area);
            if (current.Status != PlantationStatus.Active)
            {
                throw new ConflictException("Only an active plantation can be terminated.");
            }

            var endReason = await store.FindEndReasonAsync(request.EndReasonId.Value, actor.OrganizationId, transactionCancellationToken)
                ?? throw Validation("endReasonId", "The termination reason was not found or is inactive.");
            var previousStatus = current.Status;
            if (!current.Terminate(request.TerminationDate.Value, endReason.Id, request.Notes, DateTimeOffset.UtcNow, actor.UserId))
            {
                return false;
            }

            AddAudit(actor, current, "Plantation.Terminated", new { PreviousStatus = previousStatus.ToString().ToUpperInvariant(), NewStatus = "TERMINATED", current.ActualEndDate, current.EndReasonId, current.EndNotes }, ipAddress);
            await store.SaveChangesAsync(transactionCancellationToken);
            return true;
        }, cancellationToken);
    }

    public async Task<bool> ArchiveAsync(
        PlantationActor actor, Guid plantationId, string? ipAddress, CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        return await store.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            var current = await FindPlantationOrThrowAsync(actor, plantationId, transactionCancellationToken);
            var area = await FindLockedAreaOrThrowAsync(actor, current.FarmAreaId, transactionCancellationToken);
            EnsureAreaIsAvailable(area);
            if (current.Status != PlantationStatus.Terminated)
            {
                throw new ConflictException("Only a terminated plantation can be archived.");
            }

            if (!current.Archive(DateTimeOffset.UtcNow, actor.UserId)) return false;
            AddAudit(actor, current, "Plantation.Archived", new { PreviousStatus = "TERMINATED", NewStatus = "ARCHIVED" }, ipAddress);
            await store.SaveChangesAsync(transactionCancellationToken);
            return true;
        }, cancellationToken);
    }

    private async Task<CropPlantation> FindPlantationOrThrowAsync(PlantationActor actor, Guid plantationId, CancellationToken cancellationToken) =>
        plantationId == Guid.Empty
            ? throw new ResourceNotFoundException("The plantation was not found.")
            : await store.FindAsync(plantationId, actor.OrganizationId, cancellationToken)
                ?? throw new ResourceNotFoundException("The plantation was not found.");

    private async Task<FarmArea> FindLockedAreaOrThrowAsync(PlantationActor actor, Guid farmAreaId, CancellationToken cancellationToken) =>
        farmAreaId == Guid.Empty
            ? throw Validation("farmAreaId", "Farm area is required.")
            : await store.LockFarmAreaAsync(farmAreaId, actor.OrganizationId, cancellationToken)
                ?? throw new ResourceNotFoundException("The farm area was not found.");

    private async Task<PlantationReferences> ValidateReferencesAsync(
        PlantationActor actor,
        FarmArea area,
        PlantationValues values,
        CancellationToken cancellationToken)
    {
        if (area.Farm is null || area.Farm.OrganizationId != actor.OrganizationId) throw new ResourceNotFoundException("The farm was not found.");
        if (!area.Farm.IsActive) throw Validation("farmAreaId", "The farm must be active.");

        var crop = await store.FindCropAsync(values.CropId, actor.OrganizationId, cancellationToken)
            ?? throw Validation("cropId", "The crop was not found or is inactive.");
        var variety = values.VarietyId is null
            ? null
            : await store.FindVarietyAsync(values.VarietyId.Value, actor.OrganizationId, cancellationToken)
                ?? throw Validation("varietyId", "The variety was not found or is inactive.");
        if (variety is not null && variety.CropId != crop.Id) throw Validation("varietyId", "The variety must belong to the selected crop.");

        var lifecycleTemplate = values.LifecycleTemplateId is null
            ? null
            : await store.FindLifecycleTemplateAsync(values.LifecycleTemplateId.Value, actor.OrganizationId, cancellationToken)
                ?? throw Validation("lifecycleTemplateId", "The lifecycle template was not found or is inactive.");
        if (lifecycleTemplate is not null && lifecycleTemplate.CropId != crop.Id) throw Validation("lifecycleTemplateId", "The lifecycle template must belong to the selected crop.");

        var areaUnit = await FindAreaUnitOrThrowAsync(actor, values.AreaUnitId, cancellationToken);
        return new PlantationReferences(crop, variety, lifecycleTemplate, areaUnit);
    }

    private async Task<Unit> FindAreaUnitOrThrowAsync(PlantationActor actor, Guid areaUnitId, CancellationToken cancellationToken) =>
        await store.FindAreaUnitAsync(areaUnitId, actor.OrganizationId, cancellationToken)
            ?? throw Validation("areaUnitId", "The area unit was not found or is inactive.");

    private async Task EnsureCodeIsAvailableAsync(PlantationActor actor, string code, Guid? excludingId, CancellationToken cancellationToken)
    {
        if (await store.CodeExistsAsync(actor.OrganizationId, code, excludingId, cancellationToken))
        {
            throw new ConflictException("A plantation with this code already exists in the organization.");
        }
    }

    private void ValidateAllocation(FarmArea area, decimal allocatedArea, Unit areaUnit, IReadOnlyList<CropPlantation> activePlantations, IReadOnlyList<FarmArea> activeChildren)
    {
        var totalBaseArea = ToBaseArea(area.TotalArea, RequireAreaUnit(area.AreaUnit));
        var childBaseArea = activeChildren.Sum(child => ToBaseArea(child.TotalArea, RequireAreaUnit(child.AreaUnit)));
        var allocatedBaseArea = activePlantations.Sum(plantation => ToBaseArea(plantation.AllocatedArea, RequireAreaUnit(plantation.AreaUnit)));
        var availableBaseArea = totalBaseArea - childBaseArea - allocatedBaseArea;
        if (ToBaseArea(allocatedArea, areaUnit) > availableBaseArea)
        {
            throw Validation("allocatedArea", $"Allocated area exceeds the farm area's available area ({FromBaseArea(Math.Max(0, availableBaseArea), areaUnit):0.####} {areaUnit.Code}).");
        }
    }

    private static void EnsureAreaIsAvailable(FarmArea area)
    {
        if (!area.IsActive) throw Validation("farmAreaId", "The farm area must be active.");
    }

    private static PlantationStatus? ParseStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return null;
        if (!Enum.TryParse<PlantationStatus>(status.Trim(), true, out var parsed))
        {
            throw Validation("status", "Status must be PLANNED, ACTIVE, TERMINATED, or ARCHIVED.");
        }
        return parsed;
    }

    private void AddAudit(PlantationActor actor, CropPlantation plantation, string action, object details, string? ipAddress) =>
        store.AddAuditLog(new AuditLog(action, plantation.OrganizationId, actor.UserId, "Plantation", plantation.Id, JsonSerializer.SerializeToDocument(details), ipAddress));

    private static PlantationResponse ToResponse(CropPlantation plantation) =>
        ToResponse(plantation, plantation.FarmArea!, plantation.Crop!, plantation.Variety, plantation.LifecycleTemplate, plantation.AreaUnit!, plantation.EndReason);

    private static PlantationResponse ToResponse(CropPlantation plantation, FarmArea area, Crop crop, CropVariety? variety, CropLifecycleTemplate? lifecycleTemplate, Unit areaUnit, PlantationEndReason? endReason) =>
        new(
            plantation.Id, plantation.FarmId, plantation.FarmAreaId, area.Code, area.Name, plantation.CropId, crop.Code, crop.Name,
            plantation.VarietyId, variety?.Code, variety?.Name, plantation.LifecycleTemplateId, lifecycleTemplate?.Name,
            plantation.PlantationCode, plantation.PlantationName, plantation.AllocatedArea, plantation.AreaUnitId,
            areaUnit.Code, areaUnit.Name, areaUnit.Symbol, plantation.PlantingDate, plantation.ExpectedEndDate,
            plantation.ActualEndDate, plantation.Status.ToString().ToUpperInvariant(), plantation.EndReasonId, endReason?.Code, endReason?.Name,
            plantation.EndNotes, plantation.IsActive, plantation.CreatedAt, plantation.CreatedBy, plantation.UpdatedAt, plantation.UpdatedBy);

    private static PlantationValues ReadValues(CreatePlantationRequest? request) =>
        request is null ? throw Validation("request", "A request body is required.") : ReadValues(request.FarmAreaId, request.CropId, request.VarietyId, request.LifecycleTemplateId, request.PlantationCode, request.PlantationName, request.AllocatedArea, request.AreaUnitId, request.PlantingDate, request.ExpectedEndDate);

    private static PlantationValues ReadValues(UpdatePlantationRequest? request) =>
        request is null ? throw Validation("request", "A request body is required.") : ReadValues(request.FarmAreaId, request.CropId, request.VarietyId, request.LifecycleTemplateId, request.PlantationCode, request.PlantationName, request.AllocatedArea, request.AreaUnitId, request.PlantingDate, request.ExpectedEndDate);

    private static PlantationValues ReadValues(Guid? farmAreaId, Guid? cropId, Guid? varietyId, Guid? lifecycleTemplateId, string? code, string? name, decimal? allocatedArea, Guid? areaUnitId, DateOnly? plantingDate, DateOnly? expectedEndDate)
    {
        if (farmAreaId is null || farmAreaId == Guid.Empty) throw Validation("farmAreaId", "Farm area is required.");
        if (cropId is null || cropId == Guid.Empty) throw Validation("cropId", "Crop is required.");
        if (varietyId == Guid.Empty) throw Validation("varietyId", "The variety must be valid.");
        if (lifecycleTemplateId == Guid.Empty) throw Validation("lifecycleTemplateId", "The lifecycle template must be valid.");
        if (string.IsNullOrWhiteSpace(code)) throw Validation("plantationCode", "Plantation code is required.");
        if (code.Trim().Length > 50) throw Validation("plantationCode", "Plantation code cannot exceed 50 characters.");
        if (string.IsNullOrWhiteSpace(name)) throw Validation("plantationName", "Plantation name is required.");
        if (name.Trim().Length > 200) throw Validation("plantationName", "Plantation name cannot exceed 200 characters.");
        if (allocatedArea is null or <= 0) throw Validation("allocatedArea", "Allocated area must be greater than zero.");
        if (areaUnitId is null || areaUnitId == Guid.Empty) throw Validation("areaUnitId", "Area unit is required.");
        if (plantingDate is null) throw Validation("plantingDate", "Planting date is required.");
        if (expectedEndDate is not null && expectedEndDate < plantingDate) throw Validation("expectedEndDate", "Expected end date cannot be before planting date.");
        return new PlantationValues(farmAreaId.Value, cropId.Value, varietyId, lifecycleTemplateId, code.Trim().ToUpperInvariant(), name.Trim(), allocatedArea.Value, areaUnitId.Value, plantingDate.Value, expectedEndDate);
    }

    private static decimal ToBaseArea(decimal value, Unit unit) => value * (unit.ConversionFactor ?? throw new InvalidOperationException($"Area unit '{unit.Code}' has no conversion factor."));
    private static decimal FromBaseArea(decimal value, Unit unit) => value / (unit.ConversionFactor ?? throw new InvalidOperationException($"Area unit '{unit.Code}' has no conversion factor."));
    private static Unit RequireAreaUnit(Unit? unit) => unit ?? throw new InvalidOperationException("A plantation references a missing area unit.");
    private static void ValidateActor(PlantationActor actor)
    {
        if (actor.UserId == Guid.Empty || actor.OrganizationId == Guid.Empty) throw new UnauthorizedAccessException("The access token does not contain a valid user scope.");
    }
    private static ValidationException Validation(string fieldName, string message) => new("Validation failed", new Dictionary<string, string[]> { [fieldName] = [message] });

    private sealed record PlantationValues(Guid FarmAreaId, Guid CropId, Guid? VarietyId, Guid? LifecycleTemplateId, string PlantationCode, string PlantationName, decimal AllocatedArea, Guid AreaUnitId, DateOnly PlantingDate, DateOnly? ExpectedEndDate);
    private sealed record PlantationReferences(Crop Crop, CropVariety? Variety, CropLifecycleTemplate? LifecycleTemplate, Unit AreaUnit);
}
