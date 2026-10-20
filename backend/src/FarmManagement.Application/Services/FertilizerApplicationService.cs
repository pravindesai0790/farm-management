using System.Text.Json;
using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Fertilizer;
using FarmManagement.Application.Interfaces.Fertilizer;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.Services;

public sealed class FertilizerApplicationService(
    IFertilizerApplicationStore store,
    IFertilizerValidator validator,
    IFertilizerReferenceGenerator referenceGenerator) : IFertilizerApplicationService
{
    private const int DefaultPageSize = 20;
    private const int MaximumPageSize = 100;

    public async Task<PagedResponse<FertilizerApplicationListItemResponse>> ListAsync(
        FertilizerActor actor,
        FertilizerApplicationListQuery query,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);

        var page = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = NormalizePageSize(query.PageSize);
        var normalizedQuery = query with { PageNumber = page, PageSize = pageSize };

        var totalCount = await store.CountAsync(actor.OrganizationId, normalizedQuery, cancellationToken);

        var items = await store.ListAsync(
            actor.OrganizationId,
            normalizedQuery,
            checked((page - 1) * pageSize),
            pageSize,
            cancellationToken);

        var userIds = items.Select(i => i.CreatedBy).Distinct().ToList();
        var userNames = await store.GetUserNamesAsync(userIds, cancellationToken);

        var responses = items.Select(item =>
        {
            userNames.TryGetValue(item.CreatedBy, out var createdByName);
            return ToListItemResponse(item, createdByName);
        }).ToArray();

        return new PagedResponse<FertilizerApplicationListItemResponse>(responses, page, pageSize, totalCount);
    }

    public async Task<FertilizerApplicationDetailsResponse> GetAsync(
        FertilizerActor actor,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);

        if (id == Guid.Empty)
        {
            throw new ResourceNotFoundException("The fertilizer application was not found.");
        }

        var app = await store.FindAsync(id, actor.OrganizationId, cancellationToken);
        if (app is null)
        {
            throw new ResourceNotFoundException("The fertilizer application was not found.");
        }

        var userIds = new List<Guid> { app.CreatedBy };
        if (app.UpdatedBy.HasValue) userIds.Add(app.UpdatedBy.Value);
        if (app.StartedBy.HasValue) userIds.Add(app.StartedBy.Value);
        if (app.CompletedBy.HasValue) userIds.Add(app.CompletedBy.Value);
        if (app.CancelledBy.HasValue) userIds.Add(app.CancelledBy.Value);

        var userNames = await store.GetUserNamesAsync(userIds.Distinct(), cancellationToken);

        userNames.TryGetValue(app.CreatedBy, out var createdByName);
        string? updatedByName = null;
        if (app.UpdatedBy.HasValue) userNames.TryGetValue(app.UpdatedBy.Value, out updatedByName);
        string? startedByName = null;
        if (app.StartedBy.HasValue) userNames.TryGetValue(app.StartedBy.Value, out startedByName);
        string? completedByName = null;
        if (app.CompletedBy.HasValue) userNames.TryGetValue(app.CompletedBy.Value, out completedByName);
        string? cancelledByName = null;
        if (app.CancelledBy.HasValue) userNames.TryGetValue(app.CancelledBy.Value, out cancelledByName);

        return ToDetailsResponse(
            app,
            createdByName,
            updatedByName,
            startedByName,
            completedByName,
            cancelledByName);
    }

    public Task<IReadOnlyList<FertilizerProductLookupResponse>> ListProductsLookupAsync(
        FertilizerActor actor,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        return store.ListProductsLookupAsync(actor.OrganizationId, cancellationToken);
    }

    public Task<IReadOnlyList<FertilizerStorageLocationLookupResponse>> ListStorageLocationsLookupAsync(
        FertilizerActor actor,
        Guid farmId,
        Guid? inventoryItemId,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        if (farmId == Guid.Empty)
        {
            throw new ArgumentException("A farm identifier is required.", nameof(farmId));
        }

        return store.ListStorageLocationsLookupAsync(actor.OrganizationId, farmId, inventoryItemId, cancellationToken);
    }

    public Task<IReadOnlyList<FertilizerApplicationMethodLookupResponse>> ListApplicationMethodsLookupAsync(
        FertilizerActor actor,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        return store.ListApplicationMethodsLookupAsync(actor.OrganizationId, cancellationToken);
    }

    public Task<IReadOnlyList<FertilizerIrrigationLookupResponse>> ListIrrigationsLookupAsync(
        FertilizerActor actor,
        Guid? farmId,
        Guid? farmAreaId,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        return store.ListIrrigationsLookupAsync(actor.OrganizationId, farmId, farmAreaId, cancellationToken);
    }

    public async Task<FertilizerApplicationDetailsResponse> CreateDraftAsync(
        FertilizerActor actor,
        CreateFertilizerDraftRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ArgumentNullException.ThrowIfNull(request);

        // 1. Hierarchy & Context validation if supplied
        await validator.ValidateHierarchyAsync(
            actor.OrganizationId,
            request.FarmId ?? Guid.Empty,
            request.FarmAreaId,
            request.PlantationId,
            request.CropCycleId,
            request.CropCycleStageId,
            isPreStart: true,
            cancellationToken);

        // 2. Method & Irrigation validation if supplied
        await validator.ValidateApplicationMethodAsync(
            actor.OrganizationId,
            request.ApplicationMethodId,
            request.OtherMethodDescription,
            cancellationToken);

        await validator.ValidateIrrigationLinkAsync(
            actor.OrganizationId,
            request.ApplicationMethodId,
            request.IrrigationEventId,
            request.FarmId,
            request.FarmAreaId,
            cancellationToken);

        // 3. Planned Area validation if supplied
        await validator.ValidateAreaAndUnitsAsync(
            actor.OrganizationId,
            request.PlannedArea,
            request.PlannedAreaUnitId,
            null,
            null,
            cancellationToken);

        // 4. Validate products if supplied
        if (request.Products is not null && request.Products.Count > 0)
        {
            var seenItemIds = new HashSet<Guid>();
            foreach (var p in request.Products)
            {
                if (p.InventoryItemId == Guid.Empty)
                {
                    throw Validation("products", "A valid inventory item is required.");
                }

                if (!seenItemIds.Add(p.InventoryItemId))
                {
                    throw Validation("products", $"Duplicate inventory item '{p.InventoryItemId}' is not allowed.");
                }

                if (p.PlannedQuantity < 0m)
                {
                    throw Validation("products", "Planned quantity cannot be negative.");
                }

                await validator.ValidateProductEligibilityAsync(
                    actor.OrganizationId,
                    p.InventoryItemId,
                    isPreStart: true,
                    cancellationToken);

                if (p.StorageLocationId.HasValue && p.StorageLocationId.Value != Guid.Empty)
                {
                    if (!request.FarmId.HasValue || request.FarmId.Value == Guid.Empty)
                    {
                        throw Validation("products", "Storage location cannot be assigned without a selected farm.");
                    }

                    await validator.ValidateStorageLocationAsync(
                        actor.OrganizationId,
                        request.FarmId.Value,
                        p.StorageLocationId.Value,
                        isPreStart: true,
                        cancellationToken);
                }
            }
        }

        // 5. Generate reference number FERT-YYYY-NNNNNN
        var now = DateTimeOffset.UtcNow;
        var referenceNumber = await referenceGenerator.NextReferenceNumberAsync(actor.OrganizationId, now.Year, cancellationToken);

        // 6. Create application entity
        var app = new FertilizerApplication(
            organizationId: actor.OrganizationId,
            referenceNumber: referenceNumber,
            createdBy: actor.UserId,
            farmId: request.FarmId,
            farmAreaId: request.FarmAreaId,
            plantationId: request.PlantationId,
            cropCycleId: request.CropCycleId,
            cropCycleStageId: request.CropCycleStageId,
            applicationMethodId: request.ApplicationMethodId,
            otherMethodDescription: request.OtherMethodDescription,
            irrigationEventId: request.IrrigationEventId,
            plannedDate: request.PlannedDate,
            scheduledDate: request.ScheduledDate,
            plannedArea: request.PlannedArea,
            plannedAreaUnitId: request.PlannedAreaUnitId,
            purposeReason: request.PurposeReason);

        // 7. Add product lines
        if (request.Products is not null)
        {
            foreach (var p in request.Products)
            {
                app.AddProduct(
                    inventoryItemId: p.InventoryItemId,
                    plannedQuantity: p.PlannedQuantity,
                    createdBy: actor.UserId,
                    storageLocationId: p.StorageLocationId);
            }
        }

        store.Add(app);

        // 8. Add audit
        AddAudit(
            actor,
            app,
            "Fertilizer.DraftCreated",
            new
            {
                app.Id,
                app.ReferenceNumber,
                app.FarmId,
                app.FarmAreaId,
                app.PlantationId,
                app.CropCycleId,
                app.CropCycleStageId,
                app.PlannedDate,
                ProductCount = app.Products.Count
            },
            ipAddress);

        await store.SaveChangesAsync(cancellationToken);

        return await GetAsync(actor, app.Id, cancellationToken);
    }

    public async Task<FertilizerApplicationDetailsResponse> UpdateDraftAsync(
        FertilizerActor actor,
        Guid id,
        UpdateFertilizerDraftRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ArgumentNullException.ThrowIfNull(request);

        if (id == Guid.Empty)
        {
            throw new ResourceNotFoundException("The fertilizer application was not found.");
        }

        var app = await store.FindAsync(id, actor.OrganizationId, cancellationToken)
            ?? throw new ResourceNotFoundException("The fertilizer application was not found.");

        if (app.Status != FertilizerApplicationStatus.Draft)
        {
            throw new ConflictException("Only draft fertilizer applications can be modified.");
        }

        // Optimistic Concurrency check
        if (request.Version.HasValue && request.Version.Value != app.Version)
        {
            throw new ConflictException("The fertilizer application has been modified by another user. Please reload and try again.");
        }

        // Hierarchy & Context validation if supplied
        await validator.ValidateHierarchyAsync(
            actor.OrganizationId,
            request.FarmId ?? Guid.Empty,
            request.FarmAreaId,
            request.PlantationId,
            request.CropCycleId,
            request.CropCycleStageId,
            isPreStart: true,
            cancellationToken);

        // Method & Irrigation validation if supplied
        await validator.ValidateApplicationMethodAsync(
            actor.OrganizationId,
            request.ApplicationMethodId,
            request.OtherMethodDescription,
            cancellationToken);

        await validator.ValidateIrrigationLinkAsync(
            actor.OrganizationId,
            request.ApplicationMethodId,
            request.IrrigationEventId,
            request.FarmId,
            request.FarmAreaId,
            cancellationToken);

        // Planned Area validation if supplied
        await validator.ValidateAreaAndUnitsAsync(
            actor.OrganizationId,
            request.PlannedArea,
            request.PlannedAreaUnitId,
            null,
            null,
            cancellationToken);

        // Validate products if supplied
        var incomingProducts = request.Products ?? [];
        var seenItemIds = new HashSet<Guid>();
        foreach (var p in incomingProducts)
        {
            if (p.InventoryItemId == Guid.Empty)
            {
                throw Validation("products", "A valid inventory item is required.");
            }

            if (!seenItemIds.Add(p.InventoryItemId))
            {
                throw Validation("products", $"Duplicate inventory item '{p.InventoryItemId}' is not allowed.");
            }

            if (p.PlannedQuantity < 0m)
            {
                throw Validation("products", "Planned quantity cannot be negative.");
            }

            await validator.ValidateProductEligibilityAsync(
                actor.OrganizationId,
                p.InventoryItemId,
                isPreStart: true,
                cancellationToken);

            if (p.StorageLocationId.HasValue && p.StorageLocationId.Value != Guid.Empty)
            {
                if (!request.FarmId.HasValue || request.FarmId.Value == Guid.Empty)
                {
                    throw Validation("products", "Storage location cannot be assigned without a selected farm.");
                }

                await validator.ValidateStorageLocationAsync(
                    actor.OrganizationId,
                    request.FarmId.Value,
                    p.StorageLocationId.Value,
                    isPreStart: true,
                    cancellationToken);
            }
        }

        var now = DateTimeOffset.UtcNow;

        // Update parent draft fields
        app.UpdateDraft(
            farmId: request.FarmId,
            farmAreaId: request.FarmAreaId,
            plantationId: request.PlantationId,
            cropCycleId: request.CropCycleId,
            cropCycleStageId: request.CropCycleStageId,
            applicationMethodId: request.ApplicationMethodId,
            otherMethodDescription: request.OtherMethodDescription,
            irrigationEventId: request.IrrigationEventId,
            plannedDate: request.PlannedDate,
            scheduledDate: request.ScheduledDate,
            plannedArea: request.PlannedArea,
            plannedAreaUnitId: request.PlannedAreaUnitId,
            purposeReason: request.PurposeReason,
            now: now,
            updatedBy: actor.UserId);

        // Synchronize product lines:
        // 1. Remove products not in incoming list
        var toRemove = app.Products.Where(p => !seenItemIds.Contains(p.InventoryItemId)).ToList();
        foreach (var product in toRemove)
        {
            app.RemoveProduct(product.InventoryItemId);
            store.RemoveProduct(product);
        }

        // 2. Update or add products
        foreach (var p in incomingProducts)
        {
            var existing = app.Products.FirstOrDefault(x => x.InventoryItemId == p.InventoryItemId);
            if (existing is not null)
            {
                existing.UpdatePlanned(p.PlannedQuantity, p.StorageLocationId, now, actor.UserId);
            }
            else
            {
                app.AddProduct(p.InventoryItemId, p.PlannedQuantity, actor.UserId, p.StorageLocationId);
            }
        }

        // Audit
        AddAudit(
            actor,
            app,
            "Fertilizer.DraftUpdated",
            new
            {
                app.Id,
                app.ReferenceNumber,
                app.FarmId,
                app.FarmAreaId,
                app.PlantationId,
                app.CropCycleId,
                app.CropCycleStageId,
                app.PlannedDate,
                ProductCount = app.Products.Count,
                Version = app.Version
            },
            ipAddress);

        await store.SaveChangesAsync(cancellationToken);

        return await GetAsync(actor, app.Id, cancellationToken);
    }

    public async Task DeleteDraftAsync(
        FertilizerActor actor,
        Guid id,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);

        if (id == Guid.Empty)
        {
            throw new ResourceNotFoundException("The fertilizer application was not found.");
        }

        var app = await store.FindAsync(id, actor.OrganizationId, cancellationToken)
            ?? throw new ResourceNotFoundException("The fertilizer application was not found.");

        if (app.Status != FertilizerApplicationStatus.Draft)
        {
            throw new ConflictException("Only draft fertilizer applications can be deleted.");
        }

        // Audit the deletion
        AddAudit(
            actor,
            app,
            "Fertilizer.DraftDeleted",
            new
            {
                app.Id,
                app.ReferenceNumber,
                app.FarmId,
                app.FarmAreaId,
                app.PlantationId,
                app.CropCycleId,
                app.CropCycleStageId,
                ProductCount = app.Products.Count
            },
            ipAddress);

        store.Remove(app);

        await store.SaveChangesAsync(cancellationToken);
    }

    public async Task<FertilizerApplicationDetailsResponse> CopyAsNewDraftAsync(
        FertilizerActor actor,
        Guid sourceId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);

        if (sourceId == Guid.Empty)
        {
            throw new ResourceNotFoundException("The fertilizer application was not found.");
        }

        var source = await store.FindAsync(sourceId, actor.OrganizationId, cancellationToken)
            ?? throw new ResourceNotFoundException("The fertilizer application was not found.");

        // Copy is allowed from ANY status: DRAFT, PLANNED, SCHEDULED, IN_PROGRESS, COMPLETED, CANCELLED.
        var now = DateTimeOffset.UtcNow;
        var newReferenceNumber = await referenceGenerator.NextReferenceNumberAsync(actor.OrganizationId, now.Year, cancellationToken);

        var newApp = new FertilizerApplication(
            organizationId: actor.OrganizationId,
            referenceNumber: newReferenceNumber,
            createdBy: actor.UserId,
            farmId: source.FarmId,
            farmAreaId: source.FarmAreaId,
            plantationId: source.PlantationId,
            cropCycleId: source.CropCycleId,
            cropCycleStageId: source.CropCycleStageId,
            applicationMethodId: source.ApplicationMethodId,
            otherMethodDescription: source.OtherMethodDescription,
            irrigationEventId: source.IrrigationEventId,
            plannedDate: null,       // Do not copy dates
            scheduledDate: null,     // Do not copy dates
            plannedArea: source.PlannedArea,
            plannedAreaUnitId: source.PlannedAreaUnitId,
            purposeReason: source.PurposeReason);

        foreach (var srcProd in source.Products)
        {
            // For Direct Completed sources with no planned quantities, copied line Planned Quantity is 0.
            var plannedQty = srcProd.PlannedQuantity > 0m ? srcProd.PlannedQuantity : 0m;

            // Revalidate storage location as a draft default: check if still exists, belongs to farm, and is active
            Guid? copiedLocationId = null;
            if (srcProd.StorageLocationId.HasValue && source.FarmId.HasValue)
            {
                var loc = await store.FindStorageLocationAsync(srcProd.StorageLocationId.Value, actor.OrganizationId, cancellationToken);
                if (loc is not null && loc.FarmId == source.FarmId.Value && loc.IsActive)
                {
                    copiedLocationId = loc.Id;
                }
            }

            newApp.AddProduct(
                inventoryItemId: srcProd.InventoryItemId,
                plannedQuantity: plannedQty,
                createdBy: actor.UserId,
                storageLocationId: copiedLocationId);
        }

        store.Add(newApp);

        AddAudit(
            actor,
            newApp,
            "Fertilizer.DraftCopied",
            new
            {
                SourceId = source.Id,
                SourceReferenceNumber = source.ReferenceNumber,
                SourceStatus = source.Status.ToString(),
                NewId = newApp.Id,
                NewReferenceNumber = newApp.ReferenceNumber,
                ProductCount = newApp.Products.Count
            },
            ipAddress);

        await store.SaveChangesAsync(cancellationToken);

        return await GetAsync(actor, newApp.Id, cancellationToken);
    }

    private void AddAudit(
        FertilizerActor actor,
        FertilizerApplication app,
        string action,
        object details,
        string? ipAddress) =>
        store.AddAuditLog(new AuditLog(
            action,
            app.OrganizationId,
            actor.UserId,
            entityType: "FertilizerApplication",
            entityId: app.Id,
            details: details is null ? null : JsonSerializer.SerializeToDocument(details),
            ipAddress: ipAddress));

    private static ValidationException Validation(string property, string message) =>
        new("One or more validation errors occurred.", new Dictionary<string, string[]>
        {
            [property] = [message]
        });

    private static void ValidateActor(FertilizerActor actor)
    {
        ArgumentNullException.ThrowIfNull(actor);
        if (actor.UserId == Guid.Empty || actor.OrganizationId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("The actor identity or organization is invalid.");
        }
    }

    private static int NormalizePageSize(int pageSize)
    {
        if (pageSize < 1) return DefaultPageSize;
        if (pageSize > MaximumPageSize) return MaximumPageSize;
        return pageSize;
    }

    private static FertilizerApplicationListItemResponse ToListItemResponse(
        FertilizerApplication app,
        string? createdByName)
    {
        var summaries = app.Products.Select(p => new FertilizerApplicationItemSummaryDto(
            p.InventoryItemId,
            p.InventoryItem?.Name ?? string.Empty,
            p.InventoryItem?.Sku,
            p.PlannedQuantity,
            p.ActualQuantity,
            p.InventoryItem?.StockUnit?.Symbol ?? string.Empty)).ToArray();

        return new FertilizerApplicationListItemResponse(
            app.Id,
            app.ReferenceNumber,
            app.FarmId,
            app.Farm?.Name,
            app.FarmAreaId,
            app.FarmArea?.Name,
            app.PlantationId,
            app.Plantation?.PlantationName,
            app.CropCycleId,
            app.CropCycle?.CycleName,
            app.CropCycleStageId,
            app.CropCycleStage?.StageName,
            app.Status,
            app.Status.ToString().ToUpperInvariant(),
            app.ApplicationMethodId,
            app.ApplicationMethod?.Name,
            app.OtherMethodDescription,
            app.IrrigationEventId,
            app.PlannedDate,
            app.ScheduledDate,
            app.ActualApplicationDate,
            app.PlannedArea,
            app.PlannedAreaUnitId,
            app.PlannedAreaUnit?.Name,
            app.ActualArea,
            app.ActualAreaUnitId,
            app.ActualAreaUnit?.Name,
            app.PurposeReason,
            app.Products.Count,
            summaries,
            app.CreatedAt,
            app.CreatedBy,
            createdByName);
    }

    private static FertilizerApplicationDetailsResponse ToDetailsResponse(
        FertilizerApplication app,
        string? createdByName,
        string? updatedByName,
        string? startedByName,
        string? completedByName,
        string? cancelledByName)
    {
        var products = app.Products.Select(p =>
        {
            decimal? variance = p.ActualQuantity.HasValue
                ? p.ActualQuantity.Value - p.PlannedQuantity
                : null;

            return new FertilizerApplicationProductDto(
                p.Id,
                p.FertilizerApplicationId,
                p.InventoryItemId,
                p.InventoryItem?.Name ?? string.Empty,
                p.InventoryItem?.Sku,
                p.InventoryItem?.StockUnitId ?? Guid.Empty,
                p.InventoryItem?.StockUnit?.Name ?? string.Empty,
                p.InventoryItem?.StockUnit?.Code,
                p.InventoryItem?.StockUnit?.Symbol,
                p.StorageLocationId,
                p.StorageLocation?.Name,
                p.PlannedQuantity,
                p.ActualQuantity,
                variance,
                p.StockMovementId,
                null);
        }).ToArray();

        return new FertilizerApplicationDetailsResponse(
            app.Id,
            app.OrganizationId,
            app.ReferenceNumber,
            app.Status,
            app.Status.ToString().ToUpperInvariant(),
            app.FarmId,
            app.Farm?.Name,
            app.FarmAreaId,
            app.FarmArea?.Name,
            app.PlantationId,
            app.Plantation?.PlantationName,
            app.CropCycleId,
            app.CropCycle?.CycleName,
            app.CropCycleStageId,
            app.CropCycleStage?.StageName,
            app.ApplicationMethodId,
            app.ApplicationMethod?.Name,
            app.OtherMethodDescription,
            app.IrrigationEventId,
            null, // Irrigation event reference if loaded
            app.PlannedDate,
            app.ScheduledDate,
            app.ActualApplicationDate,
            app.PlannedArea,
            app.PlannedAreaUnitId,
            app.PlannedAreaUnit?.Name,
            app.ActualArea,
            app.ActualAreaUnitId,
            app.ActualAreaUnit?.Name,
            app.PurposeReason,
            app.CancellationReason,
            products,
            app.CreatedAt,
            app.CreatedBy,
            app.UpdatedAt,
            app.UpdatedBy,
            app.StartedAt,
            app.StartedBy,
            app.CompletedAt,
            app.CompletedBy,
            app.CancelledAt,
            app.CancelledBy,
            createdByName,
            updatedByName,
            startedByName,
            completedByName,
            cancelledByName,
            app.Version,
            ComputeConcurrencyToken(app));
    }

    private static string ComputeConcurrencyToken(FertilizerApplication app) =>
        app.Version > 0
            ? app.Version.ToString()
            : (app.UpdatedAt ?? app.CreatedAt).ToString("O");
}
