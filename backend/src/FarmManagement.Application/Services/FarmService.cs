using System.Text.Json;
using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.Farms;
using FarmManagement.Application.Interfaces.Farms;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.Services;

public sealed class FarmService(IFarmStore store) : IFarmService
{
    private const int DefaultPageSize = 20;
    private const int MaximumPageSize = 100;

    public async Task<FarmListResponse> ListAsync(
        FarmActor actor,
        int page,
        int pageSize,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        if (page < 1)
        {
            throw Validation("page", "Page must be at least 1.");
        }

        pageSize = NormalizePageSize(pageSize);
        var normalizedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        var totalCount = await store.CountAsync(actor.OrganizationId, normalizedSearch, isActive, cancellationToken);
        var farms = await store.ListAsync(
            actor.OrganizationId,
            checked((page - 1) * pageSize),
            pageSize,
            normalizedSearch,
            isActive,
            cancellationToken);

        return new FarmListResponse(farms.Select(ToResponse).ToArray(), page, pageSize, totalCount);
    }

    public async Task<FarmResponse> GetAsync(
        FarmActor actor,
        Guid farmId,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        return ToResponse(await FindFarmOrThrowAsync(actor, farmId, cancellationToken));
    }

    public async Task<FarmResponse> CreateAsync(
        FarmActor actor,
        CreateFarmRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ValidateRequest(request);
        var values = ReadValues(request);
        await ValidateReferencesAsync(actor, values.OwnershipTypeId, values.TotalArea, values.AreaUnitId, cancellationToken);

        if (await store.CodeExistsAsync(actor.OrganizationId, values.Code, cancellationToken: cancellationToken))
        {
            throw new ConflictException("A farm with this code already exists in the organization.");
        }

        var farm = new Farm(
            actor.OrganizationId,
            values.Code,
            values.Name,
            values.OwnershipTypeId,
            actor.UserId,
            values.TotalArea,
            values.AreaUnitId,
            values.Description,
            values.AddressLine1,
            values.AddressLine2,
            values.City,
            values.District,
            values.State,
            values.Country,
            values.PostalCode,
            values.Latitude,
            values.Longitude);

        store.Add(farm);
        AddAudit(actor, farm, "Farm.Created", new { farm.Code, farm.Name }, ipAddress);
        await store.SaveChangesAsync(cancellationToken);
        return ToResponse(farm);
    }

    public async Task<FarmResponse> UpdateAsync(
        FarmActor actor,
        Guid farmId,
        UpdateFarmRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ValidateRequest(request);
        var farm = await FindFarmOrThrowAsync(actor, farmId, cancellationToken);
        var values = ReadValues(request);
        await ValidateReferencesAsync(actor, values.OwnershipTypeId, values.TotalArea, values.AreaUnitId, cancellationToken);

        if (await store.CodeExistsAsync(actor.OrganizationId, values.Code, farm.Id, cancellationToken))
        {
            throw new ConflictException("A farm with this code already exists in the organization.");
        }

        var previous = new { farm.Code, farm.Name, farm.OwnershipTypeId, farm.TotalArea, farm.AreaUnitId, farm.IsActive };
        farm.Update(
            values.Code,
            values.Name,
            values.OwnershipTypeId,
            values.TotalArea,
            values.AreaUnitId,
            values.Description,
            values.AddressLine1,
            values.AddressLine2,
            values.City,
            values.District,
            values.State,
            values.Country,
            values.PostalCode,
            values.Latitude,
            values.Longitude,
            DateTimeOffset.UtcNow,
            actor.UserId);

        AddAudit(actor, farm, "Farm.Updated", new { previous, current = new { farm.Code, farm.Name, farm.OwnershipTypeId, farm.TotalArea, farm.AreaUnitId } }, ipAddress);
        await store.SaveChangesAsync(cancellationToken);
        return ToResponse(farm);
    }

    public Task<bool> ActivateAsync(FarmActor actor, Guid farmId, string? ipAddress, CancellationToken cancellationToken = default) =>
        SetActiveAsync(actor, farmId, true, ipAddress, cancellationToken);

    public Task<bool> DeactivateAsync(FarmActor actor, Guid farmId, string? ipAddress, CancellationToken cancellationToken = default) =>
        SetActiveAsync(actor, farmId, false, ipAddress, cancellationToken);

    private async Task<bool> SetActiveAsync(
        FarmActor actor,
        Guid farmId,
        bool active,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        ValidateActor(actor);
        var farm = await FindFarmOrThrowAsync(actor, farmId, cancellationToken);
        var changed = active
            ? farm.Activate(DateTimeOffset.UtcNow, actor.UserId)
            : farm.Deactivate(DateTimeOffset.UtcNow, actor.UserId);
        if (!changed)
        {
            return false;
        }

        AddAudit(actor, farm, active ? "Farm.Activated" : "Farm.Deactivated", null, ipAddress);
        await store.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task ValidateReferencesAsync(
        FarmActor actor,
        Guid ownershipTypeId,
        decimal? totalArea,
        Guid? areaUnitId,
        CancellationToken cancellationToken)
    {
        var ownershipType = await store.FindOwnershipTypeAsync(ownershipTypeId, cancellationToken);
        if (ownershipType is null || !ownershipType.IsActive)
        {
            throw Validation("ownershipTypeId", "The ownership type was not found or is inactive.");
        }

        if (totalArea is null)
        {
            return;
        }

        var unit = await store.FindAreaUnitAsync(areaUnitId!.Value, actor.OrganizationId, cancellationToken);
        if (unit is null || !unit.IsActive)
        {
            throw Validation("areaUnitId", "The area unit was not found or is inactive.");
        }
    }

    private async Task<Farm> FindFarmOrThrowAsync(FarmActor actor, Guid farmId, CancellationToken cancellationToken) =>
        farmId == Guid.Empty
            ? throw new ResourceNotFoundException("The farm was not found.")
            : await store.FindAsync(farmId, actor.OrganizationId, cancellationToken)
                ?? throw new ResourceNotFoundException("The farm was not found.");

    private void AddAudit(FarmActor actor, Farm farm, string action, object? details, string? ipAddress) =>
        store.AddAuditLog(new AuditLog(
            action,
            farm.OrganizationId,
            actor.UserId,
            entityType: "Farm",
            entityId: farm.Id,
            details: details is null ? null : JsonSerializer.SerializeToDocument(details),
            ipAddress: ipAddress));

    private static FarmResponse ToResponse(Farm farm) =>
        new(
            farm.Id,
            farm.Code,
            farm.Name,
            farm.Description,
            farm.OwnershipTypeId,
            farm.OwnershipType?.Code ?? string.Empty,
            farm.OwnershipType?.Name ?? string.Empty,
            farm.TotalArea,
            farm.AreaUnitId,
            farm.AreaUnit?.Code,
            farm.AreaUnit?.Name,
            farm.AreaUnit?.Symbol,
            farm.AddressLine1,
            farm.AddressLine2,
            farm.City,
            farm.District,
            farm.State,
            farm.Country,
            farm.PostalCode,
            farm.Latitude,
            farm.Longitude,
            farm.IsActive,
            farm.CreatedAt,
            farm.CreatedBy,
            farm.UpdatedAt,
            farm.UpdatedBy);

    private static FarmValues ReadValues(CreateFarmRequest request) =>
        ReadValues(request.Code, request.Name, request.Description, request.OwnershipTypeId, request.TotalArea, request.AreaUnitId, request.AddressLine1, request.AddressLine2, request.City, request.District, request.State, request.Country, request.PostalCode, request.Latitude, request.Longitude);

    private static FarmValues ReadValues(UpdateFarmRequest request) =>
        ReadValues(request.Code, request.Name, request.Description, request.OwnershipTypeId, request.TotalArea, request.AreaUnitId, request.AddressLine1, request.AddressLine2, request.City, request.District, request.State, request.Country, request.PostalCode, request.Latitude, request.Longitude);

    private static FarmValues ReadValues(
        string? code, string? name, string? description, Guid? ownershipTypeId, decimal? totalArea, Guid? areaUnitId,
        string? addressLine1, string? addressLine2, string? city, string? district, string? state, string? country,
        string? postalCode, decimal? latitude, decimal? longitude) =>
        new(code!.Trim().ToUpperInvariant(), name!.Trim(), description, ownershipTypeId!.Value, totalArea, areaUnitId, addressLine1, addressLine2, city, district, state, country, postalCode, latitude, longitude);

    private static void ValidateRequest(CreateFarmRequest? request)
    {
        if (request is null)
        {
            throw Validation("request", "A request body is required.");
        }

        ValidateFields(request.Code, request.Name, request.OwnershipTypeId, request.TotalArea, request.AreaUnitId, request.Latitude, request.Longitude);
    }

    private static void ValidateRequest(UpdateFarmRequest? request)
    {
        if (request is null)
        {
            throw Validation("request", "A request body is required.");
        }

        ValidateFields(request.Code, request.Name, request.OwnershipTypeId, request.TotalArea, request.AreaUnitId, request.Latitude, request.Longitude);
    }

    private static void ValidateFields(string? code, string? name, Guid? ownershipTypeId, decimal? totalArea, Guid? areaUnitId, decimal? latitude, decimal? longitude)
    {
        if (string.IsNullOrWhiteSpace(code)) throw Validation("code", "Code is required.");
        if (code.Trim().Length > 50) throw Validation("code", "Code cannot exceed 50 characters.");
        if (string.IsNullOrWhiteSpace(name)) throw Validation("name", "Name is required.");
        if (name.Trim().Length > 200) throw Validation("name", "Name cannot exceed 200 characters.");
        if (ownershipTypeId is null || ownershipTypeId == Guid.Empty) throw Validation("ownershipTypeId", "Ownership type is required.");
        if (totalArea is <= 0) throw Validation("totalArea", "Total area must be greater than zero.");
        if ((totalArea is null) != (areaUnitId is null)) throw Validation("areaUnitId", "Total area and area unit must be provided together.");
        if (areaUnitId == Guid.Empty) throw Validation("areaUnitId", "Area unit must be valid.");
        if (latitude is < -90 or > 90) throw Validation("latitude", "Latitude must be between -90 and 90.");
        if (longitude is < -180 or > 180) throw Validation("longitude", "Longitude must be between -180 and 180.");
    }

    private static int NormalizePageSize(int pageSize) =>
        pageSize == 0 ? DefaultPageSize : pageSize is < 1 or > MaximumPageSize
            ? throw Validation("pageSize", $"Page size must be between 1 and {MaximumPageSize}.")
            : pageSize;

    private static void ValidateActor(FarmActor actor)
    {
        if (actor.UserId == Guid.Empty || actor.OrganizationId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("The access token does not contain a valid user scope.");
        }
    }

    private static ValidationException Validation(string fieldName, string message) =>
        new("Validation failed", new Dictionary<string, string[]> { [fieldName] = [message] });

    private sealed record FarmValues(
        string Code, string Name, string? Description, Guid OwnershipTypeId, decimal? TotalArea, Guid? AreaUnitId,
        string? AddressLine1, string? AddressLine2, string? City, string? District, string? State, string? Country,
        string? PostalCode, decimal? Latitude, decimal? Longitude);
}
