using System.Text.Json;
using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.Farms;
using FarmManagement.Application.Interfaces.Farms;
using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Services;

public sealed class FarmAreaService(IFarmAreaStore store) : IFarmAreaService
{
    public async Task<IReadOnlyList<FarmAreaResponse>> ListAsync(
        FarmActor actor,
        Guid farmId,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        if (farmId == Guid.Empty)
        {
            throw new ResourceNotFoundException("The farm was not found.");
        }

        if (await store.FindFarmAsync(farmId, actor.OrganizationId, cancellationToken) is null)
        {
            throw new ResourceNotFoundException("The farm was not found.");
        }

        var areas = await store.ListAsync(farmId, actor.OrganizationId, isActive, cancellationToken);
        return areas.Select(ToResponse).ToArray();
    }

    public async Task<FarmAreaResponse> GetAsync(
        FarmActor actor,
        Guid farmAreaId,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        return ToResponse(await FindAreaOrThrowAsync(actor, farmAreaId, cancellationToken));
    }

    public async Task<FarmAreaResponse> CreateAsync(
        FarmActor actor,
        CreateFarmAreaRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ValidateRequest(request);

        var farmId = request.FarmId!.Value;
        var farm = await FindFarmOrThrowAsync(actor, farmId, cancellationToken);
        EnsureFarmIsActive(farm);
        var values = ReadValues(request);
        var areaUnit = await FindAreaUnitOrThrowAsync(actor, values.AreaUnitId, cancellationToken);
        var parent = await ValidateParentAsync(actor, farm, values.ParentFarmAreaId, values.TotalArea, areaUnit, null, cancellationToken);
        await ValidateFarmAllocationAsync(actor, farm, values.TotalArea, areaUnit, null, values.ParentFarmAreaId, cancellationToken);

        if (await store.CodeExistsAsync(farm.Id, values.Code, cancellationToken: cancellationToken))
        {
            throw new ConflictException("A farm area with this code already exists in the farm.");
        }

        var farmArea = new FarmArea(
            actor.OrganizationId,
            farm.Id,
            parent?.Id,
            values.Code,
            values.Name,
            values.TotalArea,
            areaUnit.Id,
            actor.UserId,
            values.Description);

        store.Add(farmArea);
        AddAudit(actor, farmArea, "FarmArea.Created", new { farmArea.Code, farmArea.Name, farmArea.ParentFarmAreaId }, ipAddress);
        await store.SaveChangesAsync(cancellationToken);
        return ToResponse(farmArea, areaUnit);
    }

    public async Task<FarmAreaResponse> UpdateAsync(
        FarmActor actor,
        Guid farmAreaId,
        UpdateFarmAreaRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ValidateRequest(request);
        var farmArea = await FindAreaOrThrowAsync(actor, farmAreaId, cancellationToken);
        var farm = await FindFarmOrThrowAsync(actor, farmArea.FarmId, cancellationToken);
        var values = ReadValues(request);
        var areaUnit = await FindAreaUnitOrThrowAsync(actor, values.AreaUnitId, cancellationToken);
        var parent = await ValidateParentAsync(actor, farm, values.ParentFarmAreaId, values.TotalArea, areaUnit, farmArea.Id, cancellationToken);

        if (values.ParentFarmAreaId is not null && await store.HasChildrenAsync(farmArea.Id, cancellationToken))
        {
            throw Validation("parentFarmAreaId", "An area with child areas cannot become a child area.");
        }

        await ValidateOwnChildrenAllocationAsync(farmArea, values.TotalArea, areaUnit, cancellationToken);
        await ValidateFarmAllocationAsync(actor, farm, values.TotalArea, areaUnit, farmArea.Id, values.ParentFarmAreaId, cancellationToken);

        if (await store.CodeExistsAsync(farmArea.FarmId, values.Code, farmArea.Id, cancellationToken))
        {
            throw new ConflictException("A farm area with this code already exists in the farm.");
        }

        var previous = new
        {
            farmArea.Code,
            farmArea.Name,
            farmArea.ParentFarmAreaId,
            farmArea.TotalArea,
            farmArea.AreaUnitId,
            farmArea.IsActive
        };
        farmArea.Update(
            parent?.Id,
            values.Code,
            values.Name,
            values.TotalArea,
            areaUnit.Id,
            values.Description,
            DateTimeOffset.UtcNow,
            actor.UserId);

        AddAudit(actor, farmArea, "FarmArea.Updated", new { previous, current = new { farmArea.Code, farmArea.Name, farmArea.ParentFarmAreaId, farmArea.TotalArea, farmArea.AreaUnitId } }, ipAddress);
        await store.SaveChangesAsync(cancellationToken);
        return ToResponse(farmArea, areaUnit);
    }

    public Task<bool> ActivateAsync(FarmActor actor, Guid farmAreaId, string? ipAddress, CancellationToken cancellationToken = default) =>
        SetActiveAsync(actor, farmAreaId, true, ipAddress, cancellationToken);

    public Task<bool> DeactivateAsync(FarmActor actor, Guid farmAreaId, string? ipAddress, CancellationToken cancellationToken = default) =>
        SetActiveAsync(actor, farmAreaId, false, ipAddress, cancellationToken);

    public async Task<FarmAreaAvailabilityResponse> GetAvailabilityAsync(
        FarmActor actor,
        Guid farmAreaId,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var farmArea = await FindAreaOrThrowAsync(actor, farmAreaId, cancellationToken);
        var areaUnit = farmArea.AreaUnit ?? await FindAreaUnitOrThrowAsync(actor, farmArea.AreaUnitId, cancellationToken);
        var children = await store.ListActiveChildrenAsync(farmArea.Id, cancellationToken: cancellationToken);
        var plantations = await store.ListActivePlantationsAsync(farmArea.Id, cancellationToken);
        var allocatedBaseArea = children.Sum(child => ToBaseArea(child.TotalArea, RequireAreaUnit(child.AreaUnit))) +
            plantations.Sum(plantation => ToBaseArea(plantation.AllocatedArea, RequireAreaUnit(plantation.AreaUnit)));
        var totalBaseArea = ToBaseArea(farmArea.TotalArea, areaUnit);
        var availableBaseArea = totalBaseArea - allocatedBaseArea;

        return new FarmAreaAvailabilityResponse(
            farmArea.Id,
            farmArea.TotalArea,
            FromBaseArea(allocatedBaseArea, areaUnit),
            FromBaseArea(Math.Max(0, availableBaseArea), areaUnit),
            areaUnit.Code);
    }

    private async Task<bool> SetActiveAsync(
        FarmActor actor,
        Guid farmAreaId,
        bool active,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        ValidateActor(actor);
        var farmArea = await FindAreaOrThrowAsync(actor, farmAreaId, cancellationToken);
        if (active && !farmArea.IsActive)
        {
            var farm = await FindFarmOrThrowAsync(actor, farmArea.FarmId, cancellationToken);
            EnsureFarmIsActive(farm);
            var areaUnit = farmArea.AreaUnit ?? await FindAreaUnitOrThrowAsync(actor, farmArea.AreaUnitId, cancellationToken);
            await ValidateParentAsync(actor, farm, farmArea.ParentFarmAreaId, farmArea.TotalArea, areaUnit, farmArea.Id, cancellationToken);
            await ValidateFarmAllocationAsync(actor, farm, farmArea.TotalArea, areaUnit, farmArea.Id, farmArea.ParentFarmAreaId, cancellationToken);
        }

        var changed = active
            ? farmArea.Activate(DateTimeOffset.UtcNow, actor.UserId)
            : farmArea.Deactivate(DateTimeOffset.UtcNow, actor.UserId);
        if (!changed)
        {
            return false;
        }

        AddAudit(actor, farmArea, active ? "FarmArea.Activated" : "FarmArea.Deactivated", null, ipAddress);
        await store.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<FarmArea?> ValidateParentAsync(
        FarmActor actor,
        Farm farm,
        Guid? parentFarmAreaId,
        decimal totalArea,
        Unit areaUnit,
        Guid? excludingFarmAreaId,
        CancellationToken cancellationToken)
    {
        if (parentFarmAreaId is null)
        {
            return null;
        }

        if (parentFarmAreaId == Guid.Empty || parentFarmAreaId == excludingFarmAreaId)
        {
            throw Validation("parentFarmAreaId", "The parent area must be valid and cannot be the area itself.");
        }

        var parent = await store.FindParentAsync(parentFarmAreaId.Value, actor.OrganizationId, cancellationToken);
        if (parent is null || parent.FarmId != farm.Id)
        {
            throw Validation("parentFarmAreaId", "The parent area must belong to the same farm and organization.");
        }

        if (parent.ParentFarmAreaId is not null)
        {
            throw Validation("parentFarmAreaId", "Nested child areas are not allowed.");
        }

        if (!parent.IsActive)
        {
            throw Validation("parentFarmAreaId", "The parent area must be active.");
        }

        var requestedBaseArea = ToBaseArea(totalArea, areaUnit);
        var parentBaseArea = ToBaseArea(parent.TotalArea, RequireAreaUnit(parent.AreaUnit));
        var existingChildrenBaseArea = await SumActiveChildrenAsync(parent.Id, excludingFarmAreaId, cancellationToken);
        if (existingChildrenBaseArea + requestedBaseArea > parentBaseArea)
        {
            throw Validation("totalArea", "The area allocation exceeds the parent area's available area.");
        }

        return parent;
    }

    private async Task ValidateFarmAllocationAsync(
        FarmActor actor,
        Farm farm,
        decimal totalArea,
        Unit areaUnit,
        Guid? excludingFarmAreaId,
        Guid? parentFarmAreaId,
        CancellationToken cancellationToken)
    {
        if (parentFarmAreaId is not null || farm.TotalArea is null || farm.AreaUnit is null)
        {
            return;
        }

        var rootAreas = await store.ListAsync(farm.Id, actor.OrganizationId, isActive: true, cancellationToken);
        var allocatedBaseArea = rootAreas
            .Where(area => area.ParentFarmAreaId is null && area.Id != excludingFarmAreaId)
            .Sum(area => ToBaseArea(area.TotalArea, RequireAreaUnit(area.AreaUnit)));
        var farmBaseArea = ToBaseArea(farm.TotalArea.Value, farm.AreaUnit);
        if (allocatedBaseArea + ToBaseArea(totalArea, areaUnit) > farmBaseArea)
        {
            throw Validation("totalArea", "The area allocation exceeds the farm's available area.");
        }
    }

    private async Task ValidateOwnChildrenAllocationAsync(
        FarmArea farmArea,
        decimal totalArea,
        Unit areaUnit,
        CancellationToken cancellationToken)
    {
        var childrenBaseArea = await SumActiveChildrenAsync(farmArea.Id, null, cancellationToken);
        if (childrenBaseArea > ToBaseArea(totalArea, areaUnit))
        {
            throw Validation("totalArea", "The total area cannot be less than the active child area allocation.");
        }
    }

    private async Task<decimal> SumActiveChildrenAsync(
        Guid parentFarmAreaId,
        Guid? excludingFarmAreaId,
        CancellationToken cancellationToken)
    {
        var children = await store.ListActiveChildrenAsync(parentFarmAreaId, excludingFarmAreaId, cancellationToken);
        return children.Sum(child => ToBaseArea(child.TotalArea, RequireAreaUnit(child.AreaUnit)));
    }

    private async Task<FarmArea> FindAreaOrThrowAsync(FarmActor actor, Guid farmAreaId, CancellationToken cancellationToken) =>
        farmAreaId == Guid.Empty
            ? throw new ResourceNotFoundException("The farm area was not found.")
            : await store.FindAsync(farmAreaId, actor.OrganizationId, cancellationToken)
                ?? throw new ResourceNotFoundException("The farm area was not found.");

    private async Task<Farm> FindFarmOrThrowAsync(FarmActor actor, Guid farmId, CancellationToken cancellationToken) =>
        farmId == Guid.Empty
            ? throw new ResourceNotFoundException("The farm was not found.")
            : await store.FindFarmAsync(farmId, actor.OrganizationId, cancellationToken)
                ?? throw new ResourceNotFoundException("The farm was not found.");

    private async Task<Unit> FindAreaUnitOrThrowAsync(FarmActor actor, Guid areaUnitId, CancellationToken cancellationToken) =>
        await store.FindAreaUnitAsync(areaUnitId, actor.OrganizationId, cancellationToken)
            ?? throw Validation("areaUnitId", "The area unit was not found or is inactive.");

    private static void EnsureFarmIsActive(Farm farm)
    {
        if (!farm.IsActive)
        {
            throw Validation("farmId", "The farm must be active.");
        }
    }

    private void AddAudit(FarmActor actor, FarmArea farmArea, string action, object? details, string? ipAddress) =>
        store.AddAuditLog(new AuditLog(
            action,
            farmArea.OrganizationId,
            actor.UserId,
            entityType: "FarmArea",
            entityId: farmArea.Id,
            details: details is null ? null : JsonSerializer.SerializeToDocument(details),
            ipAddress: ipAddress));

    private static FarmAreaResponse ToResponse(FarmArea farmArea) =>
        ToResponse(farmArea, farmArea.AreaUnit!);

    private static FarmAreaResponse ToResponse(FarmArea farmArea, Unit areaUnit) =>
        new(
            farmArea.Id,
            farmArea.FarmId,
            farmArea.ParentFarmAreaId,
            farmArea.Code,
            farmArea.Name,
            farmArea.Description,
            farmArea.TotalArea,
            farmArea.AreaUnitId,
            areaUnit.Code,
            areaUnit.Name,
            areaUnit.Symbol,
            farmArea.IsActive,
            farmArea.CreatedAt,
            farmArea.CreatedBy,
            farmArea.UpdatedAt,
            farmArea.UpdatedBy);

    private static decimal ToBaseArea(decimal value, Unit unit) =>
        value * (unit.ConversionFactor ?? throw new InvalidOperationException($"Area unit '{unit.Code}' has no conversion factor."));

    private static decimal FromBaseArea(decimal value, Unit unit) =>
        value / (unit.ConversionFactor ?? throw new InvalidOperationException($"Area unit '{unit.Code}' has no conversion factor."));

    private static Unit RequireAreaUnit(Unit? unit) =>
        unit ?? throw new InvalidOperationException("The farm area references a missing area unit.");

    private static FarmAreaValues ReadValues(CreateFarmAreaRequest request) =>
        new(request.ParentFarmAreaId, request.Code!.Trim().ToUpperInvariant(), request.Name!.Trim(), request.Description, request.TotalArea!.Value, request.AreaUnitId!.Value);

    private static FarmAreaValues ReadValues(UpdateFarmAreaRequest request) =>
        new(request.ParentFarmAreaId, request.Code!.Trim().ToUpperInvariant(), request.Name!.Trim(), request.Description, request.TotalArea!.Value, request.AreaUnitId!.Value);

    private static void ValidateRequest(CreateFarmAreaRequest? request)
    {
        if (request is null)
        {
            throw Validation("request", "A request body is required.");
        }

        if (request.FarmId is null || request.FarmId == Guid.Empty) throw Validation("farmId", "Farm is required.");
        ValidateFields(request.Code, request.Name, request.TotalArea, request.AreaUnitId, request.ParentFarmAreaId);
    }

    private static void ValidateRequest(UpdateFarmAreaRequest? request)
    {
        if (request is null)
        {
            throw Validation("request", "A request body is required.");
        }

        ValidateFields(request.Code, request.Name, request.TotalArea, request.AreaUnitId, request.ParentFarmAreaId);
    }

    private static void ValidateFields(string? code, string? name, decimal? totalArea, Guid? areaUnitId, Guid? parentFarmAreaId)
    {
        if (string.IsNullOrWhiteSpace(code)) throw Validation("code", "Code is required.");
        if (code.Trim().Length > 50) throw Validation("code", "Code cannot exceed 50 characters.");
        if (string.IsNullOrWhiteSpace(name)) throw Validation("name", "Name is required.");
        if (name.Trim().Length > 200) throw Validation("name", "Name cannot exceed 200 characters.");
        if (totalArea is null or <= 0) throw Validation("totalArea", "Total area must be greater than zero.");
        if (areaUnitId is null || areaUnitId == Guid.Empty) throw Validation("areaUnitId", "Area unit is required.");
        if (parentFarmAreaId == Guid.Empty) throw Validation("parentFarmAreaId", "Parent area must be valid.");
    }

    private static void ValidateActor(FarmActor actor)
    {
        if (actor.UserId == Guid.Empty || actor.OrganizationId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("The access token does not contain a valid user scope.");
        }
    }

    private static ValidationException Validation(string fieldName, string message) =>
        new("Validation failed", new Dictionary<string, string[]> { [fieldName] = [message] });

    private sealed record FarmAreaValues(
        Guid? ParentFarmAreaId,
        string Code,
        string Name,
        string? Description,
        decimal TotalArea,
        Guid AreaUnitId);
}
