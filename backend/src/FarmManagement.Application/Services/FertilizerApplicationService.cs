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

    public async Task<FertilizerApplicationDetailsResponse> PlanAsync(
        FertilizerActor actor,
        Guid id,
        PlanFertilizerApplicationRequest request,
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
            throw new ConflictException("Only draft fertilizer applications can be planned.");
        }

        if (request.Version.HasValue && request.Version.Value != app.Version)
        {
            throw new ConflictException("The fertilizer application has been modified by another user. Please reload and try again.");
        }

        // If request provides updated fields, apply them
        var farmId = request.FarmId ?? app.FarmId;
        var farmAreaId = request.FarmAreaId ?? app.FarmAreaId;
        var plantationId = request.PlantationId ?? app.PlantationId;
        var cropCycleId = request.CropCycleId ?? app.CropCycleId;
        var cropCycleStageId = request.CropCycleStageId ?? app.CropCycleStageId;
        var applicationMethodId = request.ApplicationMethodId ?? app.ApplicationMethodId;
        var otherMethodDescription = request.OtherMethodDescription ?? app.OtherMethodDescription;
        var irrigationEventId = request.IrrigationEventId ?? app.IrrigationEventId;
        var plannedDate = request.PlannedDate ?? app.PlannedDate;
        var scheduledDate = request.ScheduledDate ?? app.ScheduledDate;
        var plannedArea = request.PlannedArea ?? app.PlannedArea;
        var plannedAreaUnitId = request.PlannedAreaUnitId ?? app.PlannedAreaUnitId;
        var purposeReason = request.PurposeReason ?? app.PurposeReason;

        var now = DateTimeOffset.UtcNow;

        // Synchronize products if provided in request
        if (request.Products is not null)
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
            }

            var toRemove = app.Products.Where(p => !seenItemIds.Contains(p.InventoryItemId)).ToList();
            foreach (var product in toRemove)
            {
                app.RemoveProduct(product.InventoryItemId);
                store.RemoveProduct(product);
            }

            foreach (var p in request.Products)
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
        }

        app.UpdateDraft(
            farmId: farmId,
            farmAreaId: farmAreaId,
            plantationId: plantationId,
            cropCycleId: cropCycleId,
            cropCycleStageId: cropCycleStageId,
            applicationMethodId: applicationMethodId,
            otherMethodDescription: otherMethodDescription,
            irrigationEventId: irrigationEventId,
            plannedDate: plannedDate,
            scheduledDate: scheduledDate,
            plannedArea: plannedArea,
            plannedAreaUnitId: plannedAreaUnitId,
            purposeReason: purposeReason,
            now: now,
            updatedBy: actor.UserId);

        // Required field validations for Plan transition
        if (!app.FarmId.HasValue || app.FarmId.Value == Guid.Empty)
        {
            throw Validation("farmId", "A farm is required to plan a fertilizer application.");
        }

        if (!app.FarmAreaId.HasValue || app.FarmAreaId.Value == Guid.Empty)
        {
            throw Validation("farmAreaId", "A farm area is required to plan a fertilizer application.");
        }

        if (!app.ApplicationMethodId.HasValue || app.ApplicationMethodId.Value == Guid.Empty)
        {
            throw Validation("applicationMethodId", "An application method is required to plan a fertilizer application.");
        }

        if (!app.PlannedDate.HasValue)
        {
            throw Validation("plannedDate", "A planned date is required to plan a fertilizer application.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (app.PlannedDate.Value < today)
        {
            throw Validation("plannedDate", "Planned date must be today or in the future.");
        }

        if (app.Products.Count == 0)
        {
            throw Validation("products", "At least one product line is required to plan a fertilizer application.");
        }

        // Validate hierarchy
        await validator.ValidateHierarchyAsync(
            actor.OrganizationId,
            app.FarmId.Value,
            app.FarmAreaId,
            app.PlantationId,
            app.CropCycleId,
            app.CropCycleStageId,
            isPreStart: true,
            cancellationToken);

        // Validate method and irrigation link
        await validator.ValidateApplicationMethodAsync(
            actor.OrganizationId,
            app.ApplicationMethodId,
            app.OtherMethodDescription,
            cancellationToken);

        await validator.ValidateIrrigationLinkAsync(
            actor.OrganizationId,
            app.ApplicationMethodId,
            app.IrrigationEventId,
            app.FarmId,
            app.FarmAreaId,
            cancellationToken);

        // Validate area if entered
        await validator.ValidateAreaAndUnitsAsync(
            actor.OrganizationId,
            app.PlannedArea,
            app.PlannedAreaUnitId,
            null,
            null,
            cancellationToken);

        // Validate product lines: positive planned quantity, eligible item, valid farm storage location
        var duplicateCheck = new HashSet<Guid>();
        foreach (var p in app.Products)
        {
            if (!duplicateCheck.Add(p.InventoryItemId))
            {
                throw Validation("products", $"Duplicate inventory item '{p.InventoryItemId}' is not allowed.");
            }

            if (p.PlannedQuantity <= 0m)
            {
                throw Validation("products", "Planned quantity must be greater than zero for all products to plan.");
            }

            if (!p.StorageLocationId.HasValue || p.StorageLocationId.Value == Guid.Empty)
            {
                throw Validation("storageLocationId", "A storage location is required for all products to plan.");
            }

            await validator.ValidateProductEligibilityAsync(
                actor.OrganizationId,
                p.InventoryItemId,
                isPreStart: true,
                cancellationToken);

            await validator.ValidateStorageLocationAsync(
                actor.OrganizationId,
                app.FarmId.Value,
                p.StorageLocationId.Value,
                isPreStart: true,
                cancellationToken);
        }

        // Execute plan transition on domain entity
        app.Plan(now, actor.UserId);

        AddAudit(
            actor,
            app,
            "Fertilizer.Planned",
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
                Status = app.Status.ToString().ToUpperInvariant()
            },
            ipAddress);

        await store.SaveChangesAsync(cancellationToken);

        return await GetAsync(actor, app.Id, cancellationToken);
    }

    public async Task<FertilizerApplicationDetailsResponse> ScheduleAsync(
        FertilizerActor actor,
        Guid id,
        ScheduleFertilizerApplicationRequest request,
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

        if (app.Status != FertilizerApplicationStatus.Planned && app.Status != FertilizerApplicationStatus.Draft)
        {
            throw new ConflictException("Only draft or planned fertilizer applications can be scheduled.");
        }

        if (request.Version.HasValue && request.Version.Value != app.Version)
        {
            throw new ConflictException("The fertilizer application has been modified by another user. Please reload and try again.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (request.ScheduledDate < today)
        {
            throw Validation("scheduledDate", "Scheduled date must be today or in the future.");
        }

        var now = DateTimeOffset.UtcNow;

        // If coming from Draft, run planning validation first
        if (app.Status == FertilizerApplicationStatus.Draft)
        {
            if (!app.PlannedDate.HasValue)
            {
                app.UpdateDraft(
                    app.FarmId, app.FarmAreaId, app.PlantationId, app.CropCycleId, app.CropCycleStageId,
                    app.ApplicationMethodId, app.OtherMethodDescription, app.IrrigationEventId,
                    request.ScheduledDate, request.ScheduledDate, app.PlannedArea, app.PlannedAreaUnitId,
                    app.PurposeReason, now, actor.UserId);
            }

            await PlanAsync(actor, id, new PlanFertilizerApplicationRequest(Version: app.Version), ipAddress, cancellationToken);
            app = await store.FindAsync(id, actor.OrganizationId, cancellationToken)
                ?? throw new ResourceNotFoundException("The fertilizer application was not found.");
        }

        app.Schedule(request.ScheduledDate, now, actor.UserId);

        AddAudit(
            actor,
            app,
            "Fertilizer.Scheduled",
            new
            {
                app.Id,
                app.ReferenceNumber,
                ScheduledDate = request.ScheduledDate,
                Status = app.Status.ToString().ToUpperInvariant()
            },
            ipAddress);

        await store.SaveChangesAsync(cancellationToken);

        return await GetAsync(actor, app.Id, cancellationToken);
    }

    public async Task<FertilizerApplicationDetailsResponse> RescheduleAsync(
        FertilizerActor actor,
        Guid id,
        RescheduleFertilizerApplicationRequest request,
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

        if (app.Status != FertilizerApplicationStatus.Planned && app.Status != FertilizerApplicationStatus.Scheduled)
        {
            throw new ConflictException("Only planned or scheduled fertilizer applications can be rescheduled.");
        }

        if (request.Version.HasValue && request.Version.Value != app.Version)
        {
            throw new ConflictException("The fertilizer application has been modified by another user. Please reload and try again.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (request.ScheduledDate < today)
        {
            throw Validation("scheduledDate", "Scheduled date must be today or in the future.");
        }

        var originalScheduledDate = app.ScheduledDate;
        var now = DateTimeOffset.UtcNow;

        app.Reschedule(request.ScheduledDate, now, actor.UserId);

        AddAudit(
            actor,
            app,
            "Fertilizer.Rescheduled",
            new
            {
                app.Id,
                app.ReferenceNumber,
                OriginalScheduledDate = originalScheduledDate,
                NewScheduledDate = request.ScheduledDate,
                RescheduledAt = now,
                RescheduledBy = actor.UserId,
                Status = app.Status.ToString().ToUpperInvariant()
            },
            ipAddress);

        await store.SaveChangesAsync(cancellationToken);

        return await GetAsync(actor, app.Id, cancellationToken);
    }

    public async Task<FertilizerApplicationDetailsResponse> StartAsync(
        FertilizerActor actor,
        Guid id,
        StartFertilizerApplicationRequest request,
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

        if (app.Status != FertilizerApplicationStatus.Planned && app.Status != FertilizerApplicationStatus.Scheduled)
        {
            throw new ConflictException("Only planned or scheduled fertilizer applications can be started.");
        }

        if (request.Version.HasValue && request.Version.Value != app.Version)
        {
            throw new ConflictException("The fertilizer application has been modified by another user. Please reload and try again.");
        }

        if (app.Products.Count == 0)
        {
            throw Validation("products", "At least one product line is required to start a fertilizer application.");
        }

        // 1. Revalidate context before start
        await validator.ValidateHierarchyAsync(
            actor.OrganizationId,
            app.FarmId,
            app.FarmAreaId,
            app.PlantationId,
            app.CropCycleId,
            app.CropCycleStageId,
            isPreStart: true,
            cancellationToken);

        await validator.ValidateApplicationMethodAsync(
            actor.OrganizationId,
            app.ApplicationMethodId,
            app.OtherMethodDescription,
            cancellationToken);

        await validator.ValidateIrrigationLinkAsync(
            actor.OrganizationId,
            app.ApplicationMethodId,
            app.IrrigationEventId,
            app.FarmId,
            app.FarmAreaId,
            cancellationToken);

        // 2. Revalidate product eligibility & locations, and perform stock availability gate check
        var shortages = new List<FertilizerStockShortageDto>();
        var errors = new Dictionary<string, string[]>();

        for (var i = 0; i < app.Products.Count; i++)
        {
            var p = app.Products.ElementAt(i);

            if (p.PlannedQuantity <= 0m)
            {
                throw Validation("products", "Planned quantity must be greater than zero for all products to start.");
            }

            if (!p.StorageLocationId.HasValue || p.StorageLocationId.Value == Guid.Empty)
            {
                throw Validation("storageLocationId", "A storage location is required for all products to start.");
            }

            await validator.ValidateProductEligibilityAsync(
                actor.OrganizationId,
                p.InventoryItemId,
                isPreStart: true,
                cancellationToken);

            await validator.ValidateStorageLocationAsync(
                actor.OrganizationId,
                app.FarmId!.Value,
                p.StorageLocationId.Value,
                isPreStart: true,
                cancellationToken);

            var balance = await store.FindStockBalanceAsync(
                p.StorageLocationId.Value,
                p.InventoryItemId,
                actor.OrganizationId,
                cancellationToken);

            var available = balance?.QuantityOnHand ?? 0m;
            if (available < p.PlannedQuantity)
            {
                var item = await store.FindInventoryItemAsync(p.InventoryItemId, actor.OrganizationId, cancellationToken);
                var loc = await store.FindStorageLocationAsync(p.StorageLocationId.Value, actor.OrganizationId, cancellationToken);
                var itemName = item?.Name ?? "Unknown Item";
                var locName = loc?.Name ?? "Unknown Location";
                var unitSymbol = item?.StockUnit?.Symbol ?? string.Empty;
                var shortageQty = p.PlannedQuantity - available;

                shortages.Add(new FertilizerStockShortageDto(
                    p.InventoryItemId,
                    itemName,
                    item?.Sku,
                    p.StorageLocationId.Value,
                    locName,
                    p.PlannedQuantity,
                    available,
                    shortageQty,
                    unitSymbol));

                errors[$"products[{i}]"] = [
                    $"Insufficient stock for '{itemName}' at '{locName}'. Required: {p.PlannedQuantity}, Available: {available}, Shortage: {shortageQty} {unitSymbol}."
                ];
            }
        }

        // If ANY line has shortage, block entire transition and return all shortages
        if (shortages.Count > 0)
        {
            throw new ValidationException(
                "One or more product lines have insufficient stock to start the application.",
                errors);
        }

        var now = DateTimeOffset.UtcNow;
        app.Start(now, actor.UserId);

        AddAudit(
            actor,
            app,
            "Fertilizer.Started",
            new
            {
                app.Id,
                app.ReferenceNumber,
                Status = app.Status.ToString().ToUpperInvariant(),
                StartedAt = now,
                StartedBy = actor.UserId,
                ProductCount = app.Products.Count
            },
            ipAddress);

        await store.SaveChangesAsync(cancellationToken);

        return await GetAsync(actor, app.Id, cancellationToken);
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
