using System.Text.Json;
using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Inventory;
using FarmManagement.Application.Interfaces.Inventory;
using FarmManagement.Domain.Constants;
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
        Guid? categoryId,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        if (page < 1)
        {
            throw Validation("page", "Page must be at least 1.");
        }

        pageSize = NormalizePageSize(pageSize);
        var totalCount = await store.CountAsync(actor.OrganizationId, search, categoryId, isActive, cancellationToken);
        var items = await store.ListAsync(
            actor.OrganizationId,
            checked((page - 1) * pageSize),
            pageSize,
            search,
            categoryId,
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

        InventoryItemCategory? category = null;
        if (request.CategoryId.HasValue && request.CategoryId.Value != Guid.Empty)
        {
            category = await store.FindCategoryAsync(request.CategoryId.Value, actor.OrganizationId, cancellationToken);
            if (category is null)
            {
                throw Validation("categoryId", "The selected category was not found or is inactive.");
            }
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
            category?.Id);

        store.Add(item);
        AddAudit(actor, item, "InventoryItem.Created", new { item.Name, item.Sku, Category = category?.Name, StockUnit = unit.Code }, ipAddress);
        await store.SaveChangesAsync(cancellationToken);

        return ToResponse(item, unit, category);
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

        InventoryItemCategory? category = null;
        if (request.CategoryId.HasValue && request.CategoryId.Value != Guid.Empty)
        {
            category = await store.FindCategoryAsync(request.CategoryId.Value, actor.OrganizationId, cancellationToken);
            if (category is null)
            {
                throw Validation("categoryId", "The selected category was not found or is inactive.");
            }
        }

        // Enforce stock unit immutability once stock movements have been recorded to preserve transaction and balance integrity
        if (request.StockUnitId != item.StockUnitId)
        {
            var hasMovements = await store.HasMovementsAsync(item.Id, actor.OrganizationId, cancellationToken);
            if (hasMovements)
            {
                throw Validation("stockUnitId", "The stock unit of measurement cannot be changed once stock movements have been recorded for this item.");
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Sku))
        {
            var existingWithSku = await store.FindBySkuAsync(request.Sku, actor.OrganizationId, cancellationToken);
            if (existingWithSku is not null && existingWithSku.Id != item.Id)
            {
                throw new ConflictException($"An inventory item with SKU '{request.Sku.Trim()}' already exists.");
            }
        }

        var previous = new { item.Name, item.Sku, item.CategoryId, item.StockUnitId };
        item.Update(
            request.Name,
            request.StockUnitId,
            request.Sku,
            request.Description,
            category?.Id,
            DateTimeOffset.UtcNow,
            actor.UserId);

        AddAudit(actor, item, "InventoryItem.Updated", new { previous, current = new { item.Name, item.Sku, Category = category?.Name, item.StockUnitId } }, ipAddress);
        await store.SaveChangesAsync(cancellationToken);

        return ToResponse(item, unit, category);
    }

    public Task<bool> ActivateAsync(InventoryActor actor, Guid id, string? ipAddress, CancellationToken cancellationToken = default) =>
        SetActiveAsync(actor, id, true, ipAddress, cancellationToken);

    public Task<bool> DeactivateAsync(InventoryActor actor, Guid id, string? ipAddress, CancellationToken cancellationToken = default) =>
        SetActiveAsync(actor, id, false, ipAddress, cancellationToken);

    public async Task<IReadOnlyList<InventoryCategoryResponse>> GetCategoriesAsync(
        InventoryActor actor,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var categories = await store.ListCategoriesAsync(actor.OrganizationId, cancellationToken);

        if (categories.Count > 0)
        {
            return categories
                .Select(c => new InventoryCategoryResponse(
                    c.Id,
                    c.Name,
                    c.Code,
                    c.Description ?? string.Empty,
                    c.Examples ?? string.Empty,
                    c.Icon,
                    c.DisplayOrder,
                    c.IsSystem,
                    c.IsActive))
                .ToList();
        }

        // Fallback to in-memory definitions if table is not yet seeded (e.g. testing)
        return FarmInventoryCategories.All
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new InventoryCategoryResponse(
                c.Id,
                c.Name,
                c.Code,
                c.Description,
                c.Examples,
                c.Icon,
                c.DisplayOrder,
                true,
                true))
            .ToList();
    }

    private async Task<bool> SetActiveAsync(
        InventoryActor actor,
        Guid id,
        bool active,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        ValidateActor(actor);
        var item = await FindItemOrThrowAsync(actor, id, cancellationToken);

        // Prevent deactivating an inventory item if any storage location in the organization holds positive stock on hand
        if (!active)
        {
            var totalOnHand = await store.GetTotalQuantityOnHandAsync(item.Id, actor.OrganizationId, cancellationToken);
            if (totalOnHand > 0m)
            {
                var unitSymbol = item.StockUnit?.Symbol ?? string.Empty;
                throw Validation("isActive", $"Cannot deactivate inventory item '{item.Name}' because it currently has {totalOnHand:G29} {unitSymbol} on hand across storage locations. Stock must be issued, transferred, or adjusted to zero before deactivation.".Trim());
            }
        }

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
        ToResponse(item, item.StockUnit, item.Category);

    private static InventoryItemResponse ToResponse(InventoryItem item, Unit? unit, InventoryItemCategory? category) =>
        new(
            item.Id,
            item.OrganizationId,
            item.Name,
            item.Sku,
            item.Description,
            item.CategoryId,
            category?.Name,
            category?.Code,
            category?.Icon ?? "category",
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
