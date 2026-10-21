using System.Text;
using System.Text.Json;
using FarmManagement.Application.Common.Helpers;
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
    IFertilizerReferenceGenerator referenceGenerator,
    IFertilizerStockManager stockManager) : IFertilizerApplicationService
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

    public async Task<byte[]> ExportCsvAsync(
        FertilizerActor actor,
        FertilizerApplicationListQuery query,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);

        var items = await store.ListForExportAsync(actor.OrganizationId, query, cancellationToken);

        var userIds = items.Select(a => a.CreatedBy)
            .Concat(items.Where(a => a.StartedBy.HasValue).Select(a => a.StartedBy!.Value))
            .Concat(items.Where(a => a.CompletedBy.HasValue).Select(a => a.CompletedBy!.Value))
            .Concat(items.Where(a => a.CancelledBy.HasValue).Select(a => a.CancelledBy!.Value))
            .Distinct()
            .ToList();

        var userNames = await store.GetUserNamesAsync(userIds, cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", [
            "Reference Number",
            "Status",
            "Farm",
            "Farm Area",
            "Plantation",
            "Crop Cycle",
            "Crop Cycle Stage",
            "Application Method",
            "Other Method Description",
            "Irrigation Event ID",
            "Purpose / Reason",
            "Planned Date",
            "Scheduled Date",
            "Actual Application Date",
            "Planned Area",
            "Planned Area Unit",
            "Actual Area",
            "Actual Area Unit",
            "Product Name",
            "Product SKU",
            "Stock Unit",
            "Storage Location",
            "Planned Quantity",
            "Actual Quantity",
            "Variance Quantity",
            "Stock Movement Reference",
            "Cancellation Reason",
            "Created At",
            "Created By",
            "Started At",
            "Started By",
            "Completed At",
            "Completed By",
            "Cancelled At",
            "Cancelled By"
        ]));

        foreach (var app in items)
        {
            var refNum = CsvExportHelper.EscapeText(app.ReferenceNumber);
            var status = CsvExportHelper.EscapeText(app.Status.ToString().ToUpperInvariant());
            var farmName = CsvExportHelper.EscapeText(app.Farm?.Name);
            var areaName = CsvExportHelper.EscapeText(app.FarmArea?.Name);
            var plantationName = CsvExportHelper.EscapeText(app.Plantation?.PlantationName);
            var cycleName = CsvExportHelper.EscapeText(app.CropCycle?.CycleName);
            var stageName = CsvExportHelper.EscapeText(app.CropCycleStage?.StageName);
            var methodName = CsvExportHelper.EscapeText(app.ApplicationMethod?.Name);
            var otherMethodDesc = CsvExportHelper.EscapeText(app.OtherMethodDescription);
            var irrigationId = CsvExportHelper.EscapeText(app.IrrigationEventId?.ToString());
            var purpose = CsvExportHelper.EscapeText(app.PurposeReason);
            var plannedDate = CsvExportHelper.EscapeDate(app.PlannedDate);
            var scheduledDate = CsvExportHelper.EscapeDate(app.ScheduledDate);
            var actualDate = CsvExportHelper.EscapeDate(app.ActualApplicationDate);
            var plannedArea = CsvExportHelper.EscapeNumber(app.PlannedArea);
            var plannedAreaUnit = CsvExportHelper.EscapeText(app.PlannedAreaUnit?.Symbol ?? app.PlannedAreaUnit?.Name);
            var actualArea = CsvExportHelper.EscapeNumber(app.ActualArea);
            var actualAreaUnit = CsvExportHelper.EscapeText(app.ActualAreaUnit?.Symbol ?? app.ActualAreaUnit?.Name);
            var cancelReason = CsvExportHelper.EscapeText(app.CancellationReason);
            var createdAt = CsvExportHelper.EscapeDateTime(app.CreatedAt);
            var createdByName = CsvExportHelper.EscapeText(userNames.TryGetValue(app.CreatedBy, out var cb) ? cb : null);
            var startedAt = CsvExportHelper.EscapeDateTime(app.StartedAt);
            var startedByName = CsvExportHelper.EscapeText(app.StartedBy.HasValue && userNames.TryGetValue(app.StartedBy.Value, out var sbUser) ? sbUser : null);
            var completedAt = CsvExportHelper.EscapeDateTime(app.CompletedAt);
            var completedByName = CsvExportHelper.EscapeText(app.CompletedBy.HasValue && userNames.TryGetValue(app.CompletedBy.Value, out var cbUser) ? cbUser : null);
            var cancelledAt = CsvExportHelper.EscapeDateTime(app.CancelledAt);
            var cancelledByName = CsvExportHelper.EscapeText(app.CancelledBy.HasValue && userNames.TryGetValue(app.CancelledBy.Value, out var canUser) ? canUser : null);

            if (app.Products.Count == 0)
            {
                sb.AppendLine(string.Join(",", [
                    refNum, status, farmName, areaName, plantationName, cycleName, stageName,
                    methodName, otherMethodDesc, irrigationId, purpose,
                    plannedDate, scheduledDate, actualDate,
                    plannedArea, plannedAreaUnit, actualArea, actualAreaUnit,
                    "", "", "", "", "", "", "", "",
                    cancelReason, createdAt, createdByName,
                    startedAt, startedByName, completedAt, completedByName,
                    cancelledAt, cancelledByName
                ]));
            }
            else
            {
                foreach (var p in app.Products)
                {
                    var prodName = CsvExportHelper.EscapeText(p.InventoryItem?.Name);
                    var prodSku = CsvExportHelper.EscapeText(p.InventoryItem?.Sku);
                    var unitName = CsvExportHelper.EscapeText(p.InventoryItem?.StockUnit?.Symbol ?? p.InventoryItem?.StockUnit?.Name);
                    var locName = CsvExportHelper.EscapeText(p.StorageLocation?.Name);

                    var isDirectCompletedLine = app.Status == FertilizerApplicationStatus.Completed && !app.PlannedDate.HasValue && p.PlannedQuantity == 0m;
                    var plannedQty = isDirectCompletedLine ? "" : CsvExportHelper.EscapeNumber(p.PlannedQuantity);
                    var actualQty = CsvExportHelper.EscapeNumber(p.ActualQuantity);
                    var varianceQty = !isDirectCompletedLine && p.ActualQuantity.HasValue
                        ? CsvExportHelper.EscapeNumber(p.ActualQuantity.Value - p.PlannedQuantity)
                        : "";
                    var movRef = CsvExportHelper.EscapeText(p.StockMovement?.ReferenceNumber ?? (p.StockMovementId.HasValue ? p.StockMovementId.Value.ToString() : null));

                    sb.AppendLine(string.Join(",", [
                        refNum, status, farmName, areaName, plantationName, cycleName, stageName,
                        methodName, otherMethodDesc, irrigationId, purpose,
                        plannedDate, scheduledDate, actualDate,
                        plannedArea, plannedAreaUnit, actualArea, actualAreaUnit,
                        prodName, prodSku, unitName, locName,
                        plannedQty, actualQty, varianceQty, movRef,
                        cancelReason, createdAt, createdByName,
                        startedAt, startedByName, completedAt, completedByName,
                        cancelledAt, cancelledByName
                    ]));
                }
            }
        }

        return CsvExportHelper.CreateCsvBytes(sb.ToString());
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
        Guid? storageLocationId,
        Guid? farmId,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        return store.ListProductsLookupAsync(actor.OrganizationId, storageLocationId, farmId, cancellationToken);
    }

    public Task<IReadOnlyList<FertilizerProductLookupResponse>> ListProductsLookupAsync(
        FertilizerActor actor,
        CancellationToken cancellationToken = default)
        => ListProductsLookupAsync(actor, null, null, cancellationToken);

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

        var today = DateTimeOffset.UtcNow.Date;
        if (app.PlannedDate.Value.Date < today)
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

        var today = DateTimeOffset.UtcNow.Date;
        if (request.ScheduledDate.Date < today)
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

        var today = DateTimeOffset.UtcNow.Date;
        if (request.ScheduledDate.Date < today)
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

        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var replay = await stockManager.CheckIdempotencyAsync(
                actor.OrganizationId,
                "Start",
                request.IdempotencyKey,
                request,
                cancellationToken);

            if (replay is not null)
            {
                return await GetAsync(actor, replay.FertilizerApplicationId, cancellationToken);
            }
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

        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            stockManager.RecordIdempotency(
                actor.OrganizationId,
                "Start",
                request.IdempotencyKey,
                app.Id,
                request);
        }

        await store.SaveChangesAsync(cancellationToken);

        return await GetAsync(actor, app.Id, cancellationToken);
    }

    public async Task<FertilizerApplicationDetailsResponse> CompleteAsync(
        FertilizerActor actor,
        Guid id,
        CompleteFertilizerApplicationRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ArgumentNullException.ThrowIfNull(request);

        if (id == Guid.Empty)
        {
            throw new ResourceNotFoundException("The fertilizer application was not found.");
        }

        // 1. Idempotency check before loading / locking
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var replay = await stockManager.CheckIdempotencyAsync(
                actor.OrganizationId,
                "Complete",
                request.IdempotencyKey,
                request,
                cancellationToken);

            if (replay is not null)
            {
                return await GetAsync(actor, replay.FertilizerApplicationId, cancellationToken);
            }
        }

        var app = await store.FindAsync(id, actor.OrganizationId, cancellationToken)
            ?? throw new ResourceNotFoundException("The fertilizer application was not found.");

        if (app.Status != FertilizerApplicationStatus.InProgress)
        {
            throw new ConflictException("Only in-progress fertilizer applications can be completed.");
        }

        if (request.Version.HasValue && request.Version.Value != app.Version)
        {
            throw new ConflictException("The fertilizer application has been modified by another user. Please reload and try again.");
        }

        // 2. Validate date boundaries: required, past allowed, future rejected
        if (request.ActualApplicationDate == default)
        {
            throw Validation("actualApplicationDate", "Actual application date is required.");
        }

        var now = DateTimeOffset.UtcNow;
        if (request.ActualApplicationDate > now.AddMinutes(15))
        {
            throw Validation("actualApplicationDate", "Actual application date cannot be in the future.");
        }

        // 3. Validate optional Actual Area & Unit
        await validator.ValidateAreaAndUnitsAsync(
            actor.OrganizationId,
            null,
            null,
            request.ActualArea,
            request.ActualAreaUnitId,
            cancellationToken);

        // 4. Validate product lines
        if (app.Products.Count == 0)
        {
            throw Validation("products", "At least one product line is required to complete the application.");
        }

        if (request.Products is null || request.Products.Count == 0)
        {
            throw Validation("products", "Product line actual quantities are required to complete the application.");
        }

        if (request.Products.Count != app.Products.Count)
        {
            throw Validation("products", "Actual quantities must be provided for every product line.");
        }

        var productMap = new Dictionary<Guid, decimal>();
        foreach (var p in request.Products)
        {
            if (p.InventoryItemId == Guid.Empty)
            {
                throw Validation("products", "A valid inventory item is required for every product line.");
            }

            if (!productMap.TryAdd(p.InventoryItemId, p.ActualQuantity))
            {
                throw Validation("products", "Duplicate inventory item found in completion product lines.");
            }
        }

        var varianceErrors = new Dictionary<string, string[]>();
        var hasAnyVariance = false;
        var consumptionItems = new List<FertilizerStockConsumptionItem>();

        var appProducts = app.Products.ToList();
        for (var i = 0; i < appProducts.Count; i++)
        {
            var appLine = appProducts[i];
            if (!productMap.TryGetValue(appLine.InventoryItemId, out var actualQty))
            {
                var item = await store.FindInventoryItemAsync(appLine.InventoryItemId, actor.OrganizationId, cancellationToken);
                throw Validation("products", $"Missing actual quantity for product '{item?.Name ?? appLine.InventoryItemId.ToString()}'.");
            }

            if (actualQty <= 0m)
            {
                var item = await store.FindInventoryItemAsync(appLine.InventoryItemId, actor.OrganizationId, cancellationToken);
                throw Validation("products", $"Actual quantity must be greater than zero for product '{item?.Name ?? appLine.InventoryItemId.ToString()}'.");
            }

            if (!appLine.StorageLocationId.HasValue || appLine.StorageLocationId.Value == Guid.Empty)
            {
                throw Validation("products", $"Storage location is missing for product line '{appLine.InventoryItemId}'.");
            }

            var variance = actualQty - appLine.PlannedQuantity;
            if (variance != 0m)
            {
                hasAnyVariance = true;
                var item = await store.FindInventoryItemAsync(appLine.InventoryItemId, actor.OrganizationId, cancellationToken);
                var unitSymbol = item?.StockUnit?.Symbol ?? string.Empty;
                var diffStr = variance > 0 ? $"+{variance:0.##}" : $"{variance:0.##}";
                varianceErrors[$"products[{i}]"] =
                [
                    $"Product '{item?.Name}' has a quantity variance of {diffStr} {unitSymbol} (Planned: {appLine.PlannedQuantity:0.##} {unitSymbol}, Actual: {actualQty:0.##} {unitSymbol}). Explicit variance acknowledgement is required to complete."
                ];
            }

            consumptionItems.Add(new FertilizerStockConsumptionItem(
                appLine.InventoryItemId,
                appLine.StorageLocationId.Value,
                actualQty));
        }

        // 5. Require variance acknowledgement if non-zero variance exists
        if (hasAnyVariance && !request.AcknowledgeVariance)
        {
            throw new ValidationException(
                "One or more product lines have quantity variance compared to planned. Explicit variance acknowledgement is required.",
                varianceErrors);
        }

        // 6. Atomic stock issue, status transition, audit and idempotency record
        await stockManager.ConsumeStockAsync(
            actor,
            app,
            consumptionItems,
            DateOnly.FromDateTime(request.ActualApplicationDate.UtcDateTime),
            "Complete",
            request.IdempotencyKey,
            request,
            notes: $"Completed fertilizer application {app.ReferenceNumber}",
            beforeSave: now =>
            {
                app.Complete(
                    request.ActualApplicationDate,
                    request.ActualArea,
                    request.ActualAreaUnitId,
                    now,
                    actor.UserId);

                AddAudit(
                    actor,
                    app,
                    "Fertilizer.Completed",
                    new
                    {
                        app.Id,
                        app.ReferenceNumber,
                        Status = app.Status.ToString().ToUpperInvariant(),
                        request.ActualApplicationDate,
                        request.ActualArea,
                        request.ActualAreaUnitId,
                        CompletedAt = now,
                        CompletedBy = actor.UserId,
                        Lines = app.Products.Select(p => new
                        {
                            p.InventoryItemId,
                            p.StorageLocationId,
                            p.PlannedQuantity,
                            p.ActualQuantity,
                            p.StockMovementId,
                            Variance = (p.ActualQuantity ?? 0m) - p.PlannedQuantity
                        })
                    },
                    ipAddress);
            },
            cancellationToken: cancellationToken);

        return await GetAsync(actor, app.Id, cancellationToken);
    }



    public async Task<FertilizerApplicationDetailsResponse> CancelAsync(
        FertilizerActor actor,
        Guid id,
        CancelFertilizerApplicationRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ArgumentNullException.ThrowIfNull(request);

        if (id == Guid.Empty)
        {
            throw new ResourceNotFoundException("The fertilizer application was not found.");
        }

        // 1. Idempotency check before loading / locking
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var replay = await stockManager.CheckIdempotencyAsync(
                actor.OrganizationId,
                "Cancel",
                request.IdempotencyKey,
                request,
                cancellationToken);

            if (replay is not null)
            {
                return await GetAsync(actor, replay.FertilizerApplicationId, cancellationToken);
            }
        }

        var app = await store.FindAsync(id, actor.OrganizationId, cancellationToken)
            ?? throw new ResourceNotFoundException("The fertilizer application was not found.");

        // 2. State invariant: reject COMPLETED and CANCELLED; allow DRAFT, PLANNED, SCHEDULED, IN_PROGRESS
        if (app.Status == FertilizerApplicationStatus.Completed || app.Status == FertilizerApplicationStatus.Cancelled)
        {
            throw new ConflictException("Completed or already cancelled fertilizer applications cannot be cancelled.");
        }

        // 3. Optimistic concurrency check
        if (request.Version.HasValue && request.Version.Value != app.Version)
        {
            throw new ConflictException("The fertilizer application has been modified by another user. Please reload and try again.");
        }

        // 4. Cancellation reason: mandatory and nonblank
        if (string.IsNullOrWhiteSpace(request.CancellationReason))
        {
            throw Validation("cancellationReason", "A cancellation reason is required.");
        }

        // 5. Build product usage map and validate quantities
        var productMap = new Dictionary<Guid, (decimal ActualQuantity, Guid? StorageLocationId)>();
        if (request.Products is not null)
        {
            foreach (var p in request.Products)
            {
                if (p.InventoryItemId == Guid.Empty)
                {
                    throw Validation("products", "A valid inventory item is required for every product line.");
                }

                if (p.ActualQuantity < 0m)
                {
                    throw Validation("products", "Actual quantity cannot be negative.");
                }

                if (!productMap.TryAdd(p.InventoryItemId, (p.ActualQuantity, p.StorageLocationId)))
                {
                    throw Validation("products", "Duplicate inventory item found in cancellation product lines.");
                }
            }
        }

        // InProgress requires every line to be present if products list was submitted
        if (app.Status == FertilizerApplicationStatus.InProgress && request.Products is not null && request.Products.Count > 0)
        {
            if (request.Products.Count != app.Products.Count)
            {
                throw Validation("products", "Actual quantities must be provided for every product line when cancelling an in-progress application.");
            }
        }

        // Map and validate usage across application product lines
        var hasPositiveUsage = false;
        var consumptionItems = new List<FertilizerStockConsumptionItem>();
        var lineUsageList = new List<(FertilizerApplicationProduct AppLine, decimal ActualQuantity, Guid? StorageLocationId)>();

        foreach (var appLine in app.Products)
        {
            decimal actualQty = 0m;
            Guid? storageLocationId = appLine.StorageLocationId;

            if (productMap.TryGetValue(appLine.InventoryItemId, out var reqUsage))
            {
                actualQty = reqUsage.ActualQuantity;
                if (app.Status != FertilizerApplicationStatus.InProgress && reqUsage.StorageLocationId.HasValue)
                {
                    storageLocationId = reqUsage.StorageLocationId.Value;
                }
            }

            if (actualQty < 0m)
            {
                throw Validation("products", "Actual quantity cannot be negative.");
            }

            if (actualQty > 0m)
            {
                hasPositiveUsage = true;

                // Validate location for positive line
                if (!storageLocationId.HasValue || storageLocationId.Value == Guid.Empty)
                {
                    var item = await store.FindInventoryItemAsync(appLine.InventoryItemId, actor.OrganizationId, cancellationToken);
                    throw Validation("products", $"A storage location is required for product line '{item?.Name ?? appLine.InventoryItemId.ToString()}' with positive usage.");
                }

                // If pre-start, validate location belongs to Farm & Org
                if (app.Status != FertilizerApplicationStatus.InProgress)
                {
                    if (!app.FarmId.HasValue || app.FarmId.Value == Guid.Empty)
                    {
                        throw Validation("farmId", "A farm is required to record positive fertilizer usage.");
                    }

                    await validator.ValidateStorageLocationAsync(
                        actor.OrganizationId,
                        app.FarmId.Value,
                        storageLocationId.Value,
                        isPreStart: true,
                        cancellationToken: cancellationToken);

                    // Update storage location on appLine if changed
                    if (appLine.StorageLocationId != storageLocationId.Value)
                    {
                        appLine.SetStorageLocation(storageLocationId.Value, DateTimeOffset.UtcNow, actor.UserId);
                    }
                }

                consumptionItems.Add(new FertilizerStockConsumptionItem(
                    appLine.InventoryItemId,
                    storageLocationId.Value,
                    actualQty));
            }

            lineUsageList.Add((appLine, actualQty, storageLocationId));
        }

        // 6. Date validation for positive usage
        DateTimeOffset? actualDate = null;
        if (hasPositiveUsage)
        {
            if (!request.ActualApplicationDate.HasValue || request.ActualApplicationDate.Value == default)
            {
                throw Validation("actualApplicationDate", "Actual application date is required when fertilizer usage is recorded.");
            }

            var now = DateTimeOffset.UtcNow;
            if (request.ActualApplicationDate.Value > now.AddMinutes(15))
            {
                throw Validation("actualApplicationDate", "Actual application date cannot be in the future.");
            }

            actualDate = request.ActualApplicationDate.Value;
        }

        // 7. Atomic consumption (or zero usage save), status transition, and audit
        var priorStatusName = app.Status.ToString().ToUpperInvariant();
        await stockManager.ConsumeStockAsync(
            actor,
            app,
            consumptionItems,
            DateOnly.FromDateTime((actualDate ?? DateTimeOffset.UtcNow).UtcDateTime),
            "Cancel",
            request.IdempotencyKey,
            request,
            notes: $"Cancelled fertilizer application {app.ReferenceNumber}: {request.CancellationReason.Trim()}",
            beforeSave: now =>
            {
                app.Cancel(
                    request.CancellationReason,
                    now,
                    actor.UserId,
                    actualDate);

                // Record actual quantity for zero-usage lines
                foreach (var (line, qty, _) in lineUsageList)
                {
                    if (qty == 0m)
                    {
                        line.RecordActual(0m, null, now, actor.UserId);
                    }
                }

                AddAudit(
                    actor,
                    app,
                    "Fertilizer.Cancelled",
                    new
                    {
                        app.Id,
                        app.ReferenceNumber,
                        PreviousStatus = priorStatusName,
                        Status = "CANCELLED",
                        Reason = request.CancellationReason.Trim(),
                        ActualApplicationDate = actualDate,
                        CancelledAt = now,
                        CancelledBy = actor.UserId,
                        HasPositiveUsage = hasPositiveUsage,
                        Lines = lineUsageList.Select(x => new
                        {
                            x.AppLine.InventoryItemId,
                            x.StorageLocationId,
                            x.AppLine.PlannedQuantity,
                            ActualQuantity = x.ActualQuantity,
                            x.AppLine.StockMovementId
                        })
                    },
                    ipAddress);
            },
            cancellationToken: cancellationToken);

        return await GetAsync(actor, app.Id, cancellationToken);
    }

    public async Task<FertilizerApplicationDetailsResponse> RecordCompletedAsync(
        FertilizerActor actor,
        RecordCompletedFertilizerApplicationRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ArgumentNullException.ThrowIfNull(request);

        // 1. Check idempotency replay before initiating any validation or transaction
        var replay = await stockManager.CheckIdempotencyAsync(
            actor.OrganizationId,
            "RecordCompleted",
            request.IdempotencyKey,
            request,
            cancellationToken);

        if (replay is not null)
        {
            var existing = await store.FindAsync(replay.FertilizerApplicationId, actor.OrganizationId, cancellationToken);
            if (existing is not null)
            {
                return await GetAsync(actor, existing.Id, cancellationToken);
            }
        }

        // 2. Validate mandatory Farm and FarmArea
        if (request.FarmId == Guid.Empty)
        {
            throw Validation("farmId", "A farm is required to record a completed fertilizer application.");
        }

        if (request.FarmAreaId == Guid.Empty)
        {
            throw Validation("farmAreaId", "A farm area is required to record a completed fertilizer application.");
        }

        if (request.ApplicationMethodId == Guid.Empty)
        {
            throw Validation("applicationMethodId", "An application method is required.");
        }

        // 3. Hierarchy validation
        await validator.ValidateHierarchyAsync(
            actor.OrganizationId,
            request.FarmId,
            request.FarmAreaId,
            request.PlantationId,
            request.CropCycleId,
            request.CropCycleStageId,
            isPreStart: true,
            cancellationToken: cancellationToken);

        // 4. Method validation
        await validator.ValidateApplicationMethodAsync(
            actor.OrganizationId,
            request.ApplicationMethodId,
            request.OtherMethodDescription,
            cancellationToken);

        // 5. Irrigation event validation (allowed ONLY if method is Fertigation)
        await validator.ValidateIrrigationLinkAsync(
            actor.OrganizationId,
            request.ApplicationMethodId,
            request.IrrigationEventId,
            request.FarmId,
            request.FarmAreaId,
            cancellationToken);

        // 6. Actual Area and Unit validation
        await validator.ValidateAreaAndUnitsAsync(
            actor.OrganizationId,
            plannedArea: null,
            plannedAreaUnitId: null,
            actualArea: request.ActualArea,
            actualAreaUnitId: request.ActualAreaUnitId,
            cancellationToken: cancellationToken);

        // 7. Date validation & retrospective warning check
        var now = DateTimeOffset.UtcNow;
        if (request.ActualApplicationDate > now.AddMinutes(15))
        {
            throw Validation("actualApplicationDate", "Actual application date cannot be in the future.");
        }

        if (request.ActualApplicationDate.Date < now.Date && !request.AcknowledgeRetrospectiveStockDeduction)
        {
            throw Validation("actualApplicationDate", "Application date is in the past. Inventory will be deducted from current on-hand stock rather than reconstructed historical stock. Explicit acknowledgement is required to proceed.");
        }

        // 8. Product lines validation
        if (request.Products is null || request.Products.Count == 0)
        {
            throw Validation("products", "At least one product line is required to record a completed fertilizer application.");
        }

        var duplicates = request.Products
            .GroupBy(p => p.InventoryItemId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Count > 0)
        {
            throw Validation("products", "Duplicate inventory items are not allowed in fertilizer product lines.");
        }

        var validatedProducts = new List<(RecordCompletedFertilizerProductLineRequest Line, InventoryItem Item, StorageLocation Location)>();

        foreach (var line in request.Products)
        {
            if (line.InventoryItemId == Guid.Empty)
            {
                throw Validation("products", "An inventory item is required for every product line.");
            }

            if (line.StorageLocationId == Guid.Empty)
            {
                throw Validation("products", "A storage location is required for every product line.");
            }

            if (line.ActualQuantity <= 0m)
            {
                throw Validation("products", "Actual quantity must be greater than zero for all product lines.");
            }

            await validator.ValidateProductEligibilityAsync(
                actor.OrganizationId,
                line.InventoryItemId,
                isPreStart: true,
                cancellationToken: cancellationToken);

            await validator.ValidateStorageLocationAsync(
                actor.OrganizationId,
                request.FarmId,
                line.StorageLocationId,
                isPreStart: true,
                cancellationToken: cancellationToken);

            var itemEntity = await store.FindInventoryItemAsync(line.InventoryItemId, actor.OrganizationId, cancellationToken)
                ?? throw new ResourceNotFoundException("The selected inventory item was not found.");

            var locEntity = await store.FindStorageLocationAsync(line.StorageLocationId, actor.OrganizationId, cancellationToken)
                ?? throw new ResourceNotFoundException("The selected storage location was not found.");

            validatedProducts.Add((line, itemEntity, locEntity));
        }

        // 9. Atomic stock verification, lock acquisition, shortage check, reference allocation, and creation
        var orderedProducts = validatedProducts
            .OrderBy(p => p.Line.StorageLocationId)
            .ThenBy(p => p.Line.InventoryItemId)
            .ToList();

        var createdApplication = await store.ExecuteInTransactionAsync(async ct =>
        {
            var lockedBalances = new List<(StockBalance Balance, decimal Quantity, InventoryItem Item, StorageLocation Location, RecordCompletedFertilizerProductLineRequest Line)>();
            var shortages = new List<FertilizerStockShortageDto>();
            var shortageErrors = new Dictionary<string, string[]>();

            for (var i = 0; i < orderedProducts.Count; i++)
            {
                var (line, itemEntity, locEntity) = orderedProducts[i];

                await store.AcquireAdvisoryLockAsync(line.StorageLocationId, line.InventoryItemId, ct);

                var balance = await store.LockBalanceAsync(line.StorageLocationId, line.InventoryItemId, actor.OrganizationId, ct);
                var available = balance?.QuantityOnHand ?? 0m;

                if (balance is null || available < line.ActualQuantity)
                {
                    var shortageQty = line.ActualQuantity - available;
                    var unitSymbol = itemEntity.StockUnit?.Symbol ?? string.Empty;

                    shortages.Add(new FertilizerStockShortageDto(
                        itemEntity.Id,
                        itemEntity.Name,
                        itemEntity.Sku,
                        locEntity.Id,
                        locEntity.Name,
                        line.ActualQuantity,
                        available,
                        shortageQty,
                        unitSymbol));

                    shortageErrors[$"products[{i}]"] =
                    [
                        $"Insufficient stock for '{itemEntity.Name}'. Required: {line.ActualQuantity}, Available: {available}, Shortage: {shortageQty} at '{locEntity.Name}'."
                    ];
                }
                else
                {
                    lockedBalances.Add((balance, line.ActualQuantity, itemEntity, locEntity, line));
                }
            }

            if (shortages.Count > 0)
            {
                throw new ValidationException("One or more product lines have insufficient stock.", shortageErrors);
            }

            // Allocate reference number only on successful stock verification
            var referenceNumber = await referenceGenerator.NextReferenceNumberAsync(actor.OrganizationId, request.ActualApplicationDate.Year, ct);

            var app = FertilizerApplication.RecordCompletedDirect(
                actor.OrganizationId,
                referenceNumber,
                request.FarmId,
                request.FarmAreaId,
                request.ApplicationMethodId,
                request.ActualApplicationDate,
                actor.UserId,
                request.PlantationId,
                request.CropCycleId,
                request.CropCycleStageId,
                request.OtherMethodDescription,
                request.IrrigationEventId,
                request.ActualArea,
                request.ActualAreaUnitId,
                request.PurposeReason,
                request.IdempotencyKey);

            var now = DateTimeOffset.UtcNow;
            var movementDate = DateOnly.FromDateTime(request.ActualApplicationDate.UtcDateTime);

            foreach (var (balance, quantity, itemEntity, _, line) in lockedBalances)
            {
                balance.DeductStock(quantity, now, actor.UserId);

                var movement = new StockMovement(
                    organizationId: actor.OrganizationId,
                    movementType: StockMovementType.Issue,
                    inventoryItemId: line.InventoryItemId,
                    farmId: request.FarmId,
                    storageLocationId: line.StorageLocationId,
                    quantity: quantity,
                    stockUnitId: itemEntity.StockUnitId,
                    movementDate: movementDate,
                    createdBy: actor.UserId,
                    referenceNumber: referenceNumber,
                    notes: $"Applied during fertilizer application {referenceNumber}",
                    cropCycleId: request.CropCycleId,
                    cropCycleStageId: request.CropCycleStageId,
                    plantationId: request.PlantationId,
                    farmAreaId: request.FarmAreaId,
                    laborActivityId: null,
                    sprayId: null,
                    fertilizerApplicationId: app.Id);

                store.AddMovement(movement);

                var product = new FertilizerApplicationProduct(
                    app.Id,
                    line.InventoryItemId,
                    plannedQuantity: 0m,
                    createdBy: actor.UserId,
                    storageLocationId: line.StorageLocationId,
                    actualQuantity: quantity,
                    stockMovementId: movement.Id);

                app.AddDirectProduct(product);
            }

            store.Add(app);

            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                stockManager.RecordIdempotency(actor.OrganizationId, "RecordCompleted", request.IdempotencyKey, app.Id, request);
            }

            AddAudit(
                actor,
                app,
                "Fertilizer.RecordCompleted",
                new
                {
                    app.Id,
                    app.ReferenceNumber,
                    app.FarmId,
                    app.FarmAreaId,
                    app.ApplicationMethodId,
                    app.ActualApplicationDate,
                    Products = app.Products.Select(p => new
                    {
                        p.InventoryItemId,
                        p.StorageLocationId,
                        p.ActualQuantity,
                        p.StockMovementId
                    })
                },
                ipAddress);

            await store.SaveChangesAsync(ct);
            return app;
        }, cancellationToken);

        return await GetAsync(actor, createdApplication.Id, cancellationToken);
    }

    public Task<FertilizerStockConsumptionResult> ConsumeStockAsync(
        FertilizerActor actor,
        FertilizerApplication application,
        IReadOnlyList<FertilizerStockConsumptionItem> itemsToIssue,
        DateOnly movementDate,
        string operationType,
        string? idempotencyKey,
        object? requestPayloadForHashing,
        string? notes = null,
        Action<DateTimeOffset>? beforeSave = null,
        CancellationToken cancellationToken = default) =>
        stockManager.ConsumeStockAsync(
            actor,
            application,
            itemsToIssue,
            movementDate,
            operationType,
            idempotencyKey,
            requestPayloadForHashing,
            notes,
            beforeSave,
            cancellationToken);

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
        var summaries = app.Products.Select(p =>
        {
            var isDirectCompletedLine = app.Status == FertilizerApplicationStatus.Completed && !app.PlannedDate.HasValue && p.PlannedQuantity == 0m;
            decimal? plannedQty = isDirectCompletedLine ? null : p.PlannedQuantity;

            return new FertilizerApplicationItemSummaryDto(
                p.InventoryItemId,
                p.InventoryItem?.Name ?? string.Empty,
                p.InventoryItem?.Sku,
                plannedQty,
                p.ActualQuantity,
                p.InventoryItem?.StockUnit?.Symbol ?? string.Empty);
        }).ToArray();

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
            var isDirectCompletedLine = app.Status == FertilizerApplicationStatus.Completed && !app.PlannedDate.HasValue && p.PlannedQuantity == 0m;
            decimal? plannedQty = isDirectCompletedLine ? null : p.PlannedQuantity;
            decimal? variance = p.ActualQuantity.HasValue && !isDirectCompletedLine
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
                plannedQty,
                p.ActualQuantity,
                variance,
                p.StockMovementId,
                p.StockMovement?.ReferenceNumber);
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
