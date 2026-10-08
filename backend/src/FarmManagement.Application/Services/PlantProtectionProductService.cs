using System.Text.Json;
using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.PlantProtection;
using FarmManagement.Application.Interfaces.PlantProtection;
using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Services;

public sealed class PlantProtectionProductService(IPlantProtectionProductStore store) : IPlantProtectionProductService
{
    private const int DefaultPageSize = 20;
    private const int MaximumPageSize = 100;

    public async Task<PagedResponse<PlantProtectionProductResponse>> ListAsync(
        PlantProtectionActor actor,
        int page,
        int pageSize,
        string? search,
        Guid? productTypeId,
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

        var totalCount = await store.CountAsync(
            actor.OrganizationId,
            normalizedSearch,
            productTypeId,
            isActive,
            cancellationToken);

        var items = await store.ListAsync(
            actor.OrganizationId,
            checked((page - 1) * pageSize),
            pageSize,
            normalizedSearch,
            productTypeId,
            isActive,
            cancellationToken);

        var responses = new List<PlantProtectionProductResponse>(items.Count);
        foreach (var item in items)
        {
            var hasUsage = await store.HasCompletedSprayUsageAsync(
                item.InventoryItemId,
                actor.OrganizationId,
                cancellationToken);

            responses.Add(ToResponse(item, hasUsage));
        }

        return new PagedResponse<PlantProtectionProductResponse>(responses, page, pageSize, totalCount);
    }

    public async Task<PlantProtectionProductResponse> GetAsync(
        PlantProtectionActor actor,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var product = await FindProductOrThrowAsync(actor, id, cancellationToken);
        var hasUsage = await store.HasCompletedSprayUsageAsync(
            product.InventoryItemId,
            actor.OrganizationId,
            cancellationToken);

        return ToResponse(product, hasUsage);
    }

    public async Task<PlantProtectionProductResponse> CreateAsync(
        PlantProtectionActor actor,
        CreatePlantProtectionProductRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ValidateRequest(request);

        var inventoryItem = await store.FindInventoryItemAsync(request.InventoryItemId, actor.OrganizationId, cancellationToken);
        if (inventoryItem is null)
        {
            throw new ResourceNotFoundException("The selected inventory item was not found.");
        }

        if (!inventoryItem.IsActive)
        {
            throw Validation("inventoryItemId", "The selected inventory item is inactive.");
        }

        var existingProfile = await store.FindByInventoryItemIdAsync(request.InventoryItemId, actor.OrganizationId, cancellationToken);
        if (existingProfile is not null)
        {
            throw new ConflictException($"A plant protection product profile already exists for inventory item '{inventoryItem.Name}'.");
        }

        var productType = await store.FindProductTypeAsync(request.ProductTypeId, actor.OrganizationId, cancellationToken);
        if (productType is null)
        {
            throw Validation("productTypeId", "The selected product type was not found or is inactive.");
        }

        var product = new PlantProtectionProduct(
            organizationId: actor.OrganizationId,
            inventoryItemId: request.InventoryItemId,
            productTypeId: request.ProductTypeId,
            createdBy: actor.UserId,
            activeIngredient: request.ActiveIngredient,
            manufacturer: request.Manufacturer,
            description: request.Description);

        store.Add(product);

        AddAudit(
            actor,
            product,
            "PlantProtectionProduct.Created",
            new
            {
                product.InventoryItemId,
                product.ProductTypeId,
                product.ActiveIngredient,
                product.Manufacturer
            },
            ipAddress);

        await store.SaveChangesAsync(cancellationToken);

        return ToResponse(product, inventoryItem, productType, hasCompletedSprayUsage: false);
    }

    public async Task<PlantProtectionProductResponse> UpdateAsync(
        PlantProtectionActor actor,
        Guid id,
        UpdatePlantProtectionProductRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ValidateRequest(request);

        var product = await FindProductOrThrowAsync(actor, id, cancellationToken);

        var productType = await store.FindProductTypeAsync(request.ProductTypeId, actor.OrganizationId, cancellationToken);
        if (productType is null)
        {
            throw Validation("productTypeId", "The selected product type was not found or is inactive.");
        }

        var hasCompletedSprayUsage = await store.HasCompletedSprayUsageAsync(
            product.InventoryItemId,
            actor.OrganizationId,
            cancellationToken);

        var now = DateTimeOffset.UtcNow;

        if (hasCompletedSprayUsage)
        {
            if (request.ProductTypeId != product.ProductTypeId)
            {
                throw Validation(
                    "productTypeId",
                    "Product Type cannot be changed because this plant protection product has already been used in a completed spray application.");
            }

            product.UpdateProfileOnly(
                activeIngredient: request.ActiveIngredient,
                manufacturer: request.Manufacturer,
                description: request.Description,
                now: now,
                updatedBy: actor.UserId);
        }
        else
        {
            product.Update(
                productTypeId: request.ProductTypeId,
                activeIngredient: request.ActiveIngredient,
                manufacturer: request.Manufacturer,
                description: request.Description,
                now: now,
                updatedBy: actor.UserId);
        }

        AddAudit(
            actor,
            product,
            "PlantProtectionProduct.Updated",
            new
            {
                product.ProductTypeId,
                product.ActiveIngredient,
                product.Manufacturer,
                hasCompletedSprayUsage
            },
            ipAddress);

        await store.SaveChangesAsync(cancellationToken);

        return ToResponse(product, hasCompletedSprayUsage);
    }

    public async Task<bool> ActivateAsync(
        PlantProtectionActor actor,
        Guid id,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var product = await FindProductOrThrowAsync(actor, id, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var changed = product.Activate(now, actor.UserId);
        if (!changed)
        {
            return false;
        }

        AddAudit(actor, product, "PlantProtectionProduct.Activated", null, ipAddress);
        await store.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeactivateAsync(
        PlantProtectionActor actor,
        Guid id,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var product = await FindProductOrThrowAsync(actor, id, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var changed = product.Deactivate(now, actor.UserId);
        if (!changed)
        {
            return false;
        }

        AddAudit(actor, product, "PlantProtectionProduct.Deactivated", null, ipAddress);
        await store.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<PlantProtectionProduct> FindProductOrThrowAsync(
        PlantProtectionActor actor,
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            throw new ResourceNotFoundException("The plant protection product was not found.");
        }

        var product = await store.FindAsync(id, actor.OrganizationId, cancellationToken);
        if (product is null)
        {
            throw new ResourceNotFoundException("The plant protection product was not found.");
        }

        return product;
    }

    private static PlantProtectionProductResponse ToResponse(PlantProtectionProduct product, bool hasCompletedSprayUsage) =>
        ToResponse(product, product.InventoryItem, product.ProductType, hasCompletedSprayUsage);

    private static PlantProtectionProductResponse ToResponse(
        PlantProtectionProduct product,
        InventoryItem? inventoryItem,
        ProductType? productType,
        bool hasCompletedSprayUsage) =>
        new(
            Id: product.Id,
            OrganizationId: product.OrganizationId,
            InventoryItemId: product.InventoryItemId,
            InventoryItemName: inventoryItem?.Name ?? string.Empty,
            InventoryItemSku: inventoryItem?.Sku,
            StockUnitId: inventoryItem?.StockUnitId ?? Guid.Empty,
            StockUnitCode: inventoryItem?.StockUnit?.Code ?? string.Empty,
            StockUnitName: inventoryItem?.StockUnit?.Name ?? string.Empty,
            StockUnitSymbol: inventoryItem?.StockUnit?.Symbol ?? string.Empty,
            ProductTypeId: product.ProductTypeId,
            ProductTypeCode: productType?.Code ?? string.Empty,
            ProductTypeName: productType?.Name ?? string.Empty,
            ActiveIngredient: product.ActiveIngredient,
            Manufacturer: product.Manufacturer,
            Description: product.Description,
            IsActive: product.IsActive,
            HasCompletedSprayUsage: hasCompletedSprayUsage,
            CreatedAt: product.CreatedAt,
            CreatedBy: product.CreatedBy,
            UpdatedAt: product.UpdatedAt,
            UpdatedBy: product.UpdatedBy);

    private static void ValidateActor(PlantProtectionActor actor)
    {
        if (actor.UserId == Guid.Empty || actor.OrganizationId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("The access token does not contain a valid user scope.");
        }
    }

    private static void ValidateRequest(CreatePlantProtectionProductRequest request)
    {
        if (request is null)
        {
            throw Validation("request", "A request body is required.");
        }

        if (request.InventoryItemId == Guid.Empty)
        {
            throw Validation("inventoryItemId", "An inventory item is required.");
        }

        if (request.ProductTypeId == Guid.Empty)
        {
            throw Validation("productTypeId", "A product type is required.");
        }
    }

    private static void ValidateRequest(UpdatePlantProtectionProductRequest request)
    {
        if (request is null)
        {
            throw Validation("request", "A request body is required.");
        }

        if (request.ProductTypeId == Guid.Empty)
        {
            throw Validation("productTypeId", "A product type is required.");
        }
    }

    private static int NormalizePageSize(int pageSize) => pageSize switch
    {
        < 1 => DefaultPageSize,
        > MaximumPageSize => MaximumPageSize,
        _ => pageSize
    };

    private void AddAudit(PlantProtectionActor actor, PlantProtectionProduct product, string action, object? details, string? ipAddress) =>
        store.AddAuditLog(new AuditLog(
            action,
            product.OrganizationId,
            actor.UserId,
            entityType: "PlantProtectionProduct",
            entityId: product.Id,
            details: details is null ? null : JsonSerializer.SerializeToDocument(details),
            ipAddress: ipAddress));

    private static ValidationException Validation(string propertyName, string message) =>
        new(message, new Dictionary<string, string[]> { [propertyName] = [message] });
}
