using System.Text.Json;
using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Inventory;
using FarmManagement.Application.Interfaces.Inventory;
using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Services;

public sealed class StorageLocationService(IStorageLocationStore store) : IStorageLocationService
{
    private const int DefaultPageSize = 20;
    private const int MaximumPageSize = 100;

    public async Task<PagedResponse<StorageLocationResponse>> ListAsync(
        InventoryActor actor,
        int page,
        int pageSize,
        Guid? farmId,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        if (page < 1)
        {
            throw Validation("page", "Page must be at least 1.");
        }

        pageSize = NormalizePageSize(pageSize);

        if (farmId.HasValue)
        {
            var farm = await store.FindFarmAsync(farmId.Value, actor.OrganizationId, cancellationToken);
            if (farm is null)
            {
                throw new ResourceNotFoundException("The farm was not found.");
            }
        }

        var totalCount = await store.CountAsync(actor.OrganizationId, farmId, isActive, cancellationToken);
        var locations = await store.ListAsync(
            actor.OrganizationId,
            farmId,
            checked((page - 1) * pageSize),
            pageSize,
            isActive,
            cancellationToken);

        return new PagedResponse<StorageLocationResponse>(locations.Select(ToResponse).ToArray(), page, pageSize, totalCount);
    }

    public async Task<StorageLocationResponse> GetAsync(
        InventoryActor actor,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        return ToResponse(await FindLocationOrThrowAsync(actor, id, cancellationToken));
    }

    public async Task<StorageLocationResponse> CreateAsync(
        InventoryActor actor,
        CreateStorageLocationRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ValidateRequest(request);

        var farm = await store.FindFarmAsync(request.FarmId, actor.OrganizationId, cancellationToken);
        if (farm is null || !farm.IsActive)
        {
            throw Validation("farmId", "The selected farm was not found or is inactive.");
        }

        var nameExists = await store.ExistsNameInFarmAsync(request.FarmId, request.Name, cancellationToken: cancellationToken);
        if (nameExists)
        {
            throw new ConflictException($"A storage location with name '{request.Name.Trim()}' already exists in this farm.");
        }

        var location = new StorageLocation(
            actor.OrganizationId,
            request.FarmId,
            request.Name,
            actor.UserId,
            request.Description);

        store.Add(location);
        AddAudit(actor, location, "StorageLocation.Created", new { location.Name, location.FarmId }, ipAddress);
        await store.SaveChangesAsync(cancellationToken);

        return ToResponse(location, farm);
    }

    public async Task<StorageLocationResponse> UpdateAsync(
        InventoryActor actor,
        Guid id,
        UpdateStorageLocationRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ValidateRequest(request);

        var location = await FindLocationOrThrowAsync(actor, id, cancellationToken);

        var nameExists = await store.ExistsNameInFarmAsync(location.FarmId, request.Name, excludeLocationId: location.Id, cancellationToken: cancellationToken);
        if (nameExists)
        {
            throw new ConflictException($"A storage location with name '{request.Name.Trim()}' already exists in this farm.");
        }

        var previous = new { location.Name, location.Description };
        location.Update(
            request.Name,
            request.Description,
            DateTimeOffset.UtcNow,
            actor.UserId);

        AddAudit(actor, location, "StorageLocation.Updated", new { previous, current = new { location.Name, location.Description } }, ipAddress);
        await store.SaveChangesAsync(cancellationToken);

        return ToResponse(location);
    }

    public Task<bool> ActivateAsync(InventoryActor actor, Guid id, string? ipAddress, CancellationToken cancellationToken = default) =>
        SetActiveAsync(actor, id, true, ipAddress, cancellationToken);

    public Task<bool> DeactivateAsync(InventoryActor actor, Guid id, string? ipAddress, CancellationToken cancellationToken = default) =>
        SetActiveAsync(actor, id, false, ipAddress, cancellationToken);

    private async Task<bool> SetActiveAsync(
        InventoryActor actor,
        Guid id,
        bool active,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        ValidateActor(actor);
        var location = await FindLocationOrThrowAsync(actor, id, cancellationToken);

        // Prevent deactivating a storage location if it currently contains positive on-hand stock for any inventory item
        if (!active)
        {
            var itemsWithStockCount = await store.CountItemsWithStockAsync(location.Id, actor.OrganizationId, cancellationToken);
            if (itemsWithStockCount > 0)
            {
                throw Validation("isActive", $"Cannot deactivate storage location '{location.Name}' because it currently holds active stock for {itemsWithStockCount} item(s). All stock must be transferred or adjusted to zero before deactivation.");
            }
        }

        var now = DateTimeOffset.UtcNow;
        var changed = active ? location.Activate(now, actor.UserId) : location.Deactivate(now, actor.UserId);

        if (!changed)
        {
            return false;
        }

        AddAudit(actor, location, active ? "StorageLocation.Activated" : "StorageLocation.Deactivated", null, ipAddress);
        await store.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<StorageLocation> FindLocationOrThrowAsync(InventoryActor actor, Guid id, CancellationToken cancellationToken) =>
        id == Guid.Empty
            ? throw new ResourceNotFoundException("The storage location was not found.")
            : await store.FindAsync(id, actor.OrganizationId, cancellationToken)
                ?? throw new ResourceNotFoundException("The storage location was not found.");

    private void AddAudit(InventoryActor actor, StorageLocation location, string action, object? details, string? ipAddress) =>
        store.AddAuditLog(new AuditLog(
            action,
            location.OrganizationId,
            actor.UserId,
            entityType: "StorageLocation",
            entityId: location.Id,
            details: details is null ? null : JsonSerializer.SerializeToDocument(details),
            ipAddress: ipAddress));

    private static StorageLocationResponse ToResponse(StorageLocation location) =>
        ToResponse(location, location.Farm);

    private static StorageLocationResponse ToResponse(StorageLocation location, Farm? farm) =>
        new(
            location.Id,
            location.OrganizationId,
            location.FarmId,
            farm?.Name ?? string.Empty,
            location.Name,
            location.Description,
            location.IsActive,
            location.CreatedAt,
            location.CreatedBy,
            location.UpdatedAt,
            location.UpdatedBy);

    private static void ValidateActor(InventoryActor actor)
    {
        if (actor.UserId == Guid.Empty || actor.OrganizationId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("The access token does not contain a valid user scope.");
        }
    }

    private static void ValidateRequest(CreateStorageLocationRequest request)
    {
        if (request is null)
        {
            throw Validation("request", "A request body is required.");
        }

        if (request.FarmId == Guid.Empty)
        {
            throw Validation("farmId", "A farm is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw Validation("name", "A storage location name is required.");
        }
    }

    private static void ValidateRequest(UpdateStorageLocationRequest request)
    {
        if (request is null)
        {
            throw Validation("request", "A request body is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw Validation("name", "A storage location name is required.");
        }
    }

    private static int NormalizePageSize(int pageSize) => pageSize switch
    {
        < 1 => DefaultPageSize,
        > MaximumPageSize => MaximumPageSize,
        _ => pageSize
    };

    private static ValidationException Validation(string propertyName, string message) =>
        new(message, new Dictionary<string, string[]> { [propertyName] = [message] });
}
