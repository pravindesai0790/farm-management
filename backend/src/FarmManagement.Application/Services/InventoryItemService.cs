using System.Text.Json;
using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Inventory;
using FarmManagement.Application.Interfaces.Inventory;
using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Services;

public sealed class InventoryItemService(IInventoryItemStore store) : IInventoryItemService
{
    private const int DefaultPageSize = 20;
    private const int MaximumPageSize = 100;

    public async Task<PagedResponse<InventoryItemResponse>> ListAsync(
        InventoryActor actor,
        int page,
        int pageSize,
        string? search,
        string? category,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        if (page < 1)
        {
            throw Validation("page", "Page must be at least 1.");
        }

        pageSize = NormalizePageSize(pageSize);
        var totalCount = await store.CountAsync(actor.OrganizationId, search, category, isActive, cancellationToken);
        var items = await store.ListAsync(
            actor.OrganizationId,
            checked((page - 1) * pageSize),
            pageSize,
            search,
            category,
            isActive,
            cancellationToken);

        return new PagedResponse<InventoryItemResponse>(items.Select(ToResponse).ToArray(), page, pageSize, totalCount);
    }

    public async Task<InventoryItemResponse> GetAsync(
        InventoryActor actor,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        return ToResponse(await FindItemOrThrowAsync(actor, id, cancellationToken));
    }

    public async Task<InventoryItemResponse> CreateAsync(
        InventoryActor actor,
        CreateInventoryItemRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ValidateRequest(request);

        var unit = await store.FindUnitAsync(request.StockUnitId, actor.OrganizationId, cancellationToken);
        if (unit is null)
        {
            throw Validation("stockUnitId", "The selected stock unit was not found or is inactive.");
        }

        if (!string.IsNullOrWhiteSpace(request.Sku))
        {
            var existingWithSku = await store.FindBySkuAsync(request.Sku, actor.OrganizationId, cancellationToken);
            if (existingWithSku is not null)
            {
                throw new ConflictException($"An inventory item with SKU '{request.Sku.Trim()}' already exists.");
            }
        }

        var item = new InventoryItem(
            actor.OrganizationId,
            request.Name,
            request.StockUnitId,
            actor.UserId,
            request.Sku,
            request.Description,
            request.Category);

        store.Add(item);
        AddAudit(actor, item, "InventoryItem.Created", new { item.Name, item.Sku, item.Category, StockUnit = unit.Code }, ipAddress);
        await store.SaveChangesAsync(cancellationToken);

        return ToResponse(item, unit);
    }

    public async Task<InventoryItemResponse> UpdateAsync(
        InventoryActor actor,
        Guid id,
        UpdateInventoryItemRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ValidateRequest(request);

        var item = await FindItemOrThrowAsync(actor, id, cancellationToken);

        var unit = await store.FindUnitAsync(request.StockUnitId, actor.OrganizationId, cancellationToken);
        if (unit is null)
        {
            throw Validation("stockUnitId", "The selected stock unit was not found or is inactive.");
        }

        if (!string.IsNullOrWhiteSpace(request.Sku))
        {
            var existingWithSku = await store.FindBySkuAsync(request.Sku, actor.OrganizationId, cancellationToken);
            if (existingWithSku is not null && existingWithSku.Id != item.Id)
            {
                throw new ConflictException($"An inventory item with SKU '{request.Sku.Trim()}' already exists.");
            }
        }

        var previous = new { item.Name, item.Sku, item.Category, item.StockUnitId };
        item.Update(
            request.Name,
            request.StockUnitId,
            request.Sku,
            request.Description,
            request.Category,
            DateTimeOffset.UtcNow,
            actor.UserId);

        AddAudit(actor, item, "InventoryItem.Updated", new { previous, current = new { item.Name, item.Sku, item.Category, item.StockUnitId } }, ipAddress);
        await store.SaveChangesAsync(cancellationToken);

        return ToResponse(item, unit);
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
        var item = await FindItemOrThrowAsync(actor, id, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var changed = active ? item.Activate(now, actor.UserId) : item.Deactivate(now, actor.UserId);

        if (!changed)
        {
            return false;
        }

        AddAudit(actor, item, active ? "InventoryItem.Activated" : "InventoryItem.Deactivated", null, ipAddress);
        await store.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<InventoryItem> FindItemOrThrowAsync(InventoryActor actor, Guid id, CancellationToken cancellationToken) =>
        id == Guid.Empty
            ? throw new ResourceNotFoundException("The inventory item was not found.")
            : await store.FindAsync(id, actor.OrganizationId, cancellationToken)
                ?? throw new ResourceNotFoundException("The inventory item was not found.");

    private void AddAudit(InventoryActor actor, InventoryItem item, string action, object? details, string? ipAddress) =>
        store.AddAuditLog(new AuditLog(
            action,
            item.OrganizationId,
            actor.UserId,
            entityType: "InventoryItem",
            entityId: item.Id,
            details: details is null ? null : JsonSerializer.SerializeToDocument(details),
            ipAddress: ipAddress));

    private static InventoryItemResponse ToResponse(InventoryItem item) =>
        ToResponse(item, item.StockUnit);

    private static InventoryItemResponse ToResponse(InventoryItem item, Unit? unit) =>
        new(
            item.Id,
            item.OrganizationId,
            item.Name,
            item.Sku,
            item.Description,
            item.Category,
            item.StockUnitId,
            unit?.Code ?? string.Empty,
            unit?.Name ?? string.Empty,
            unit?.Symbol ?? string.Empty,
            item.IsActive,
            item.CreatedAt,
            item.CreatedBy,
            item.UpdatedAt,
            item.UpdatedBy);

    private static void ValidateActor(InventoryActor actor)
    {
        if (actor.UserId == Guid.Empty || actor.OrganizationId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("The access token does not contain a valid user scope.");
        }
    }

    private static void ValidateRequest(CreateInventoryItemRequest request)
    {
        if (request is null)
        {
            throw Validation("request", "A request body is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw Validation("name", "An inventory item name is required.");
        }

        if (request.StockUnitId == Guid.Empty)
        {
            throw Validation("stockUnitId", "A stock unit is required.");
        }
    }

    private static void ValidateRequest(UpdateInventoryItemRequest request)
    {
        if (request is null)
        {
            throw Validation("request", "A request body is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw Validation("name", "An inventory item name is required.");
        }

        if (request.StockUnitId == Guid.Empty)
        {
            throw Validation("stockUnitId", "A stock unit is required.");
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
