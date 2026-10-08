using System.Text.Json;
using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Sprays;
using FarmManagement.Application.Interfaces.Sprays;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.Services;

public sealed class SprayService(ISprayStore store) : ISprayService
{
    private const int DefaultPageSize = 20;
    private const int MaximumPageSize = 100;

    public async Task<PagedResponse<SprayListItemResponse>> ListAsync(
        SprayActor actor,
        SprayListQuery query,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);

        var page = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = NormalizePageSize(query.PageSize);
        var normalizedQuery = query with { PageNumber = page, PageSize = pageSize };

        var now = DateTimeOffset.UtcNow;

        var totalCount = await store.CountAsync(
            actor.OrganizationId,
            normalizedQuery,
            now,
            cancellationToken);

        var items = await store.ListAsync(
            actor.OrganizationId,
            normalizedQuery,
            checked((page - 1) * pageSize),
            pageSize,
            now,
            cancellationToken);

        var responses = items.Select(item => ToListItemResponse(item, now)).ToArray();

        return new PagedResponse<SprayListItemResponse>(responses, page, pageSize, totalCount);
    }

    public async Task<SprayDetailsResponse> GetAsync(
        SprayActor actor,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);

        if (id == Guid.Empty)
        {
            throw new ResourceNotFoundException("The spray was not found.");
        }

        var spray = await store.FindAsync(id, actor.OrganizationId, cancellationToken);
        if (spray is null)
        {
            throw new ResourceNotFoundException("The spray was not found.");
        }

        var now = DateTimeOffset.UtcNow;
        return ToDetailsResponse(spray, now);
    }

    public async Task<SprayDetailsResponse> CreateDraftAsync(
        SprayActor actor,
        CreateSprayDraftRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ArgumentNullException.ThrowIfNull(request);

        if (request.FarmId == Guid.Empty)
        {
            throw Validation("farmId", "A farm is required.");
        }

        if (!request.PlannedDate.HasValue)
        {
            throw Validation("plannedDate", "A planned date is required for planned spray.");
        }

        await ValidateHierarchyAsync(
            actor.OrganizationId,
            request.FarmId,
            request.FarmAreaId,
            request.PlantationId,
            request.CropCycleId,
            request.CropCycleStageId,
            cancellationToken);

        await ValidateAreaAndUnitsAsync(actor.OrganizationId, request.PlannedArea, request.PlannedAreaUnitId, cancellationToken);
        await ValidateWaterAndUnitsAsync(actor.OrganizationId, request.WaterQuantity, request.WaterUnitId, cancellationToken);
        await ValidateTargetAsync(actor.OrganizationId, request.TargetId, cancellationToken);
        await ValidateApplicationMethodAsync(actor.OrganizationId, request.ApplicationMethodId, cancellationToken);
        await ValidateProductsAsync(actor.OrganizationId, request.Products, cancellationToken);

        var spray = new Spray(
            organizationId: actor.OrganizationId,
            farmId: request.FarmId,
            createdBy: actor.UserId,
            farmAreaId: request.FarmAreaId,
            plantationId: request.PlantationId,
            cropCycleId: request.CropCycleId,
            cropCycleStageId: request.CropCycleStageId,
            status: SprayStatus.Draft,
            plannedDate: request.PlannedDate,
            plannedArea: request.PlannedArea,
            plannedAreaUnitId: request.PlannedAreaUnitId,
            waterQuantity: request.WaterQuantity,
            waterUnitId: request.WaterUnitId,
            targetId: request.TargetId,
            applicationMethodId: request.ApplicationMethodId,
            purposeReason: request.PurposeReason);

        if (request.Products is not null)
        {
            foreach (var item in request.Products)
            {
                var product = new SprayProduct(
                    sprayId: spray.Id,
                    inventoryItemId: item.InventoryItemId,
                    createdBy: actor.UserId,
                    plannedQuantity: item.PlannedQuantity,
                    dosage: item.Dosage);
                spray.AddProduct(product);
            }
        }

        store.Add(spray);

        AddAudit(
            actor,
            spray,
            "Spray.Created",
            new
            {
                spray.Id,
                spray.FarmId,
                spray.FarmAreaId,
                spray.PlantationId,
                spray.CropCycleId,
                spray.CropCycleStageId,
                spray.PlannedDate,
                ProductCount = spray.Products.Count
            },
            ipAddress);

        await store.SaveChangesAsync(cancellationToken);

        return await GetAsync(actor, spray.Id, cancellationToken);
    }

    public async Task<SprayDetailsResponse> UpdateDraftAsync(
        SprayActor actor,
        Guid id,
        UpdateSprayDraftRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ArgumentNullException.ThrowIfNull(request);

        if (id == Guid.Empty)
        {
            throw new ResourceNotFoundException("The spray was not found.");
        }

        var spray = await store.FindAsync(id, actor.OrganizationId, cancellationToken)
            ?? throw new ResourceNotFoundException("The spray was not found.");

        if (spray.Status != SprayStatus.Draft)
        {
            throw new ConflictException("Only draft sprays can be modified.");
        }

        if (request.FarmId == Guid.Empty)
        {
            throw Validation("farmId", "A farm is required.");
        }

        if (!request.PlannedDate.HasValue)
        {
            throw Validation("plannedDate", "A planned date is required for planned spray.");
        }

        await ValidateHierarchyAsync(
            actor.OrganizationId,
            request.FarmId,
            request.FarmAreaId,
            request.PlantationId,
            request.CropCycleId,
            request.CropCycleStageId,
            cancellationToken);

        await ValidateAreaAndUnitsAsync(actor.OrganizationId, request.PlannedArea, request.PlannedAreaUnitId, cancellationToken);
        await ValidateWaterAndUnitsAsync(actor.OrganizationId, request.WaterQuantity, request.WaterUnitId, cancellationToken);
        await ValidateTargetAsync(actor.OrganizationId, request.TargetId, cancellationToken);
        await ValidateApplicationMethodAsync(actor.OrganizationId, request.ApplicationMethodId, cancellationToken);
        await ValidateProductsAsync(actor.OrganizationId, request.Products, cancellationToken);

        var now = DateTimeOffset.UtcNow;

        spray.UpdateDraft(
            farmId: request.FarmId,
            farmAreaId: request.FarmAreaId,
            plantationId: request.PlantationId,
            cropCycleId: request.CropCycleId,
            cropCycleStageId: request.CropCycleStageId,
            plannedDate: request.PlannedDate,
            plannedArea: request.PlannedArea,
            plannedAreaUnitId: request.PlannedAreaUnitId,
            waterQuantity: request.WaterQuantity,
            waterUnitId: request.WaterUnitId,
            targetId: request.TargetId,
            applicationMethodId: request.ApplicationMethodId,
            purposeReason: request.PurposeReason,
            now: now,
            updatedBy: actor.UserId);

        var incomingProductList = request.Products ?? [];
        var incomingItemIds = incomingProductList.Select(p => p.InventoryItemId).ToHashSet();

        // 1. Remove products not in incoming list
        var productsToRemove = spray.Products.Where(p => !incomingItemIds.Contains(p.InventoryItemId)).ToList();
        foreach (var product in productsToRemove)
        {
            spray.RemoveProduct(product.InventoryItemId);
            store.RemoveSprayProduct(product);
        }

        // 2. Update existing or add new
        foreach (var item in incomingProductList)
        {
            var existing = spray.Products.FirstOrDefault(p => p.InventoryItemId == item.InventoryItemId);
            if (existing is not null)
            {
                existing.UpdateDraft(item.PlannedQuantity, item.Dosage, now, actor.UserId);
            }
            else
            {
                var newProduct = new SprayProduct(
                    sprayId: spray.Id,
                    inventoryItemId: item.InventoryItemId,
                    createdBy: actor.UserId,
                    plannedQuantity: item.PlannedQuantity,
                    dosage: item.Dosage);
                spray.AddProduct(newProduct);
            }
        }

        AddAudit(
            actor,
            spray,
            "Spray.DraftUpdated",
            new
            {
                spray.Id,
                spray.FarmId,
                spray.FarmAreaId,
                spray.PlantationId,
                spray.CropCycleId,
                spray.CropCycleStageId,
                spray.PlannedDate,
                ProductCount = spray.Products.Count
            },
            ipAddress);

        await store.SaveChangesAsync(cancellationToken);

        return await GetAsync(actor, spray.Id, cancellationToken);
    }

    public async Task<SprayDetailsResponse> ScheduleAsync(
        SprayActor actor,
        Guid id,
        ScheduleSprayRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ArgumentNullException.ThrowIfNull(request);

        if (id == Guid.Empty)
        {
            throw new ResourceNotFoundException("The spray was not found.");
        }

        var spray = await store.FindAsync(id, actor.OrganizationId, cancellationToken)
            ?? throw new ResourceNotFoundException("The spray was not found.");

        if (spray.Status != SprayStatus.Draft)
        {
            throw new ConflictException("Only draft sprays can be scheduled.");
        }

        var effectivePlannedDate = request.PlannedDate ?? spray.PlannedDate;
        if (!effectivePlannedDate.HasValue)
        {
            throw Validation("plannedDate", "A planned date is required to schedule a spray.");
        }

        await ValidateHierarchyAsync(
            actor.OrganizationId,
            spray.FarmId,
            spray.FarmAreaId,
            spray.PlantationId,
            spray.CropCycleId,
            spray.CropCycleStageId,
            cancellationToken);

        var now = DateTimeOffset.UtcNow;
        spray.Schedule(request.ScheduledDateTime, effectivePlannedDate.Value, now, actor.UserId);

        AddAudit(
            actor,
            spray,
            "Spray.Scheduled",
            new
            {
                spray.Id,
                spray.FarmId,
                spray.PlannedDate,
                spray.ScheduledDateTime,
                Status = spray.Status.ToString().ToUpperInvariant()
            },
            ipAddress);

        await store.SaveChangesAsync(cancellationToken);

        return await GetAsync(actor, spray.Id, cancellationToken);
    }

    public async Task<SprayDetailsResponse> RescheduleAsync(
        SprayActor actor,
        Guid id,
        RescheduleSprayRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ArgumentNullException.ThrowIfNull(request);

        if (id == Guid.Empty)
        {
            throw new ResourceNotFoundException("The spray was not found.");
        }

        var spray = await store.FindAsync(id, actor.OrganizationId, cancellationToken)
            ?? throw new ResourceNotFoundException("The spray was not found.");

        if (spray.Status != SprayStatus.Scheduled)
        {
            throw new ConflictException("Only scheduled sprays can be rescheduled.");
        }

        var previousScheduledDateTime = spray.ScheduledDateTime;
        var now = DateTimeOffset.UtcNow;

        spray.Reschedule(request.ScheduledDateTime, now, actor.UserId);

        AddAudit(
            actor,
            spray,
            "Spray.Rescheduled",
            new
            {
                spray.Id,
                spray.FarmId,
                PreviousScheduledDateTime = previousScheduledDateTime,
                NewScheduledDateTime = spray.ScheduledDateTime
            },
            ipAddress);

        await store.SaveChangesAsync(cancellationToken);

        return await GetAsync(actor, spray.Id, cancellationToken);
    }

    public async Task<SprayDetailsResponse> StartAsync(
        SprayActor actor,
        Guid id,
        StartSprayRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ArgumentNullException.ThrowIfNull(request);

        if (id == Guid.Empty)
        {
            throw new ResourceNotFoundException("The spray was not found.");
        }

        var spray = await store.FindAsync(id, actor.OrganizationId, cancellationToken)
            ?? throw new ResourceNotFoundException("The spray was not found.");

        if (spray.Status != SprayStatus.Scheduled)
        {
            throw new ConflictException("Only scheduled sprays can be started.");
        }

        var now = DateTimeOffset.UtcNow;
        if (request.ActualApplicationDateTime > now)
        {
            throw Validation("actualApplicationDateTime", "Actual application date/time cannot be in the future.");
        }

        if (request.Products is null || request.Products.Count == 0)
        {
            throw Validation("products", "At least one product is required to start a spray.");
        }

        var duplicates = request.Products
            .GroupBy(p => p.InventoryItemId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Count > 0)
        {
            throw Validation("products", "Duplicate inventory items are not allowed in spray products.");
        }

        // Validate each product item and verify stock availability
        foreach (var item in request.Products)
        {
            if (item.InventoryItemId == Guid.Empty)
            {
                throw Validation("inventoryItemId", "An inventory item is required.");
            }

            if (item.StorageLocationId == Guid.Empty)
            {
                throw Validation("storageLocationId", "A storage location is required for each spray product.");
            }

            if (item.ActualQuantity <= 0)
            {
                throw Validation("actualQuantity", "Actual quantity must be greater than zero.");
            }

            var inventoryItem = await store.FindInventoryItemAsync(item.InventoryItemId, actor.OrganizationId, cancellationToken)
                ?? throw new ResourceNotFoundException("The selected inventory item was not found.");

            if (!inventoryItem.IsActive)
            {
                throw Validation("products", $"The inventory item '{inventoryItem.Name}' is inactive.");
            }

            var hasActiveProfile = await store.HasActivePlantProtectionProfileAsync(item.InventoryItemId, actor.OrganizationId, cancellationToken);
            if (!hasActiveProfile)
            {
                throw Validation("products", $"The inventory item '{inventoryItem.Name}' must have an active plant protection product profile.");
            }

            var storageLocation = await store.FindStorageLocationAsync(item.StorageLocationId, actor.OrganizationId, cancellationToken)
                ?? throw new ResourceNotFoundException("The selected storage location was not found.");

            if (!storageLocation.IsActive)
            {
                throw Validation("storageLocationId", $"The storage location '{storageLocation.Name}' is inactive.");
            }

            if (storageLocation.FarmId != spray.FarmId)
            {
                throw Validation("storageLocationId", $"The storage location '{storageLocation.Name}' does not belong to the spray farm.");
            }

            var balance = await store.FindStockBalanceAsync(item.StorageLocationId, item.InventoryItemId, actor.OrganizationId, cancellationToken);
            var available = balance?.QuantityOnHand ?? 0m;
            if (available < item.ActualQuantity)
            {
                throw Validation("products", $"Insufficient stock for '{inventoryItem.Name}'. Required: {item.ActualQuantity}, Available: {available} at '{storageLocation.Name}'.");
            }
        }

        // Apply product execution values
        foreach (var item in request.Products)
        {
            var existing = spray.Products.FirstOrDefault(p => p.InventoryItemId == item.InventoryItemId);
            if (existing is not null)
            {
                existing.SetExecution(item.StorageLocationId, item.ActualQuantity, item.Dosage, now, actor.UserId);
            }
            else
            {
                var newProduct = new SprayProduct(
                    sprayId: spray.Id,
                    inventoryItemId: item.InventoryItemId,
                    createdBy: actor.UserId,
                    storageLocationId: item.StorageLocationId,
                    actualQuantity: item.ActualQuantity,
                    dosage: item.Dosage);
                spray.AddProduct(newProduct);
            }
        }

        spray.Start(request.ActualApplicationDateTime, now, actor.UserId);

        AddAudit(
            actor,
            spray,
            "Spray.Started",
            new
            {
                spray.Id,
                spray.FarmId,
                spray.ActualApplicationDateTime,
                Status = spray.Status.ToString().ToUpperInvariant(),
                ProductCount = spray.Products.Count
            },
            ipAddress);

        await store.SaveChangesAsync(cancellationToken);

        return await GetAsync(actor, spray.Id, cancellationToken);
    }

    public async Task<SprayDetailsResponse> SaveExecutionAsync(
        SprayActor actor,
        Guid id,
        UpdateSprayExecutionRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ArgumentNullException.ThrowIfNull(request);

        if (id == Guid.Empty)
        {
            throw new ResourceNotFoundException("The spray was not found.");
        }

        var spray = await store.FindAsync(id, actor.OrganizationId, cancellationToken)
            ?? throw new ResourceNotFoundException("The spray was not found.");

        if (spray.Status != SprayStatus.InProgress)
        {
            throw new ConflictException("Execution details can only be saved while spray is in progress.");
        }

        var now = DateTimeOffset.UtcNow;
        if (request.ActualApplicationDateTime > now)
        {
            throw Validation("actualApplicationDateTime", "Actual application date/time cannot be in the future.");
        }

        await ValidateTreatedAreaAndUnitsAsync(actor.OrganizationId, request.ActualTreatedArea, request.ActualTreatedAreaUnitId, cancellationToken);
        await ValidateWaterAndUnitsAsync(actor.OrganizationId, request.WaterQuantity, request.WaterUnitId, cancellationToken);
        await ValidateTargetAsync(actor.OrganizationId, request.TargetId, cancellationToken);
        await ValidateApplicationMethodAsync(actor.OrganizationId, request.ApplicationMethodId, cancellationToken);

        if (request.Products is not null)
        {
            var duplicates = request.Products
                .GroupBy(p => p.InventoryItemId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicates.Count > 0)
            {
                throw Validation("products", "Duplicate inventory items are not allowed in spray products.");
            }

            foreach (var item in request.Products)
            {
                if (item.InventoryItemId == Guid.Empty)
                {
                    throw Validation("inventoryItemId", "An inventory item is required.");
                }

                if (item.ActualQuantity <= 0)
                {
                    throw Validation("actualQuantity", "Actual quantity must be greater than zero.");
                }

                var existing = spray.Products.FirstOrDefault(p => p.InventoryItemId == item.InventoryItemId);
                if (existing is null)
                {
                    throw Validation("products", "The product is not part of this spray and cannot be added after start.");
                }

                existing.UpdateActualQuantityAndDosage(item.ActualQuantity, item.Dosage, now, actor.UserId);
            }
        }

        spray.SaveExecution(
            request.ActualApplicationDateTime,
            request.ActualTreatedArea,
            request.ActualTreatedAreaUnitId,
            request.WaterQuantity,
            request.WaterUnitId,
            request.TargetId,
            request.ApplicationMethodId,
            request.PurposeReason,
            now,
            actor.UserId);

        AddAudit(
            actor,
            spray,
            "Spray.ExecutionSaved",
            new
            {
                spray.Id,
                spray.FarmId,
                spray.ActualApplicationDateTime,
                spray.ActualTreatedArea,
                spray.ActualTreatedAreaUnitId,
                spray.WaterQuantity,
                spray.WaterUnitId,
                spray.TargetId,
                spray.ApplicationMethodId,
                ProductCount = spray.Products.Count
            },
            ipAddress);

        await store.SaveChangesAsync(cancellationToken);

        return await GetAsync(actor, spray.Id, cancellationToken);
    }

    private async Task ValidateHierarchyAsync(
        Guid organizationId,
        Guid farmId,
        Guid? farmAreaId,
        Guid? plantationId,
        Guid? cropCycleId,
        Guid? cropCycleStageId,
        CancellationToken cancellationToken)
    {
        var farm = await store.FindFarmAsync(farmId, organizationId, cancellationToken)
            ?? throw new ResourceNotFoundException("The farm was not found.");

        if (!farm.IsActive)
        {
            throw Validation("farmId", "The selected farm is inactive.");
        }

        if (farmAreaId.HasValue && farmAreaId.Value != Guid.Empty)
        {
            var area = await store.FindFarmAreaAsync(farmAreaId.Value, organizationId, cancellationToken)
                ?? throw new ResourceNotFoundException("The farm area was not found.");

            if (area.FarmId != farm.Id)
            {
                throw Validation("farmAreaId", "The farm area does not belong to the selected farm.");
            }

            if (!area.IsActive)
            {
                throw Validation("farmAreaId", "The selected farm area is inactive.");
            }
        }

        if (plantationId.HasValue && plantationId.Value != Guid.Empty)
        {
            if (!farmAreaId.HasValue || farmAreaId.Value == Guid.Empty)
            {
                throw Validation("plantationId", "A farm area is required when specifying a plantation.");
            }

            var plantation = await store.FindPlantationAsync(plantationId.Value, organizationId, cancellationToken)
                ?? throw new ResourceNotFoundException("The plantation was not found.");

            if (plantation.FarmId != farm.Id)
            {
                throw Validation("plantationId", "The plantation does not belong to the selected farm.");
            }

            if (plantation.FarmAreaId != farmAreaId.Value)
            {
                throw Validation("plantationId", "The plantation does not belong to the selected farm area.");
            }
        }

        if (cropCycleId.HasValue && cropCycleId.Value != Guid.Empty)
        {
            if (!plantationId.HasValue || plantationId.Value == Guid.Empty)
            {
                throw Validation("cropCycleId", "A plantation is required when specifying a crop cycle.");
            }

            var cycle = await store.FindCropCycleAsync(cropCycleId.Value, organizationId, cancellationToken)
                ?? throw new ResourceNotFoundException("The crop cycle was not found.");

            if (cycle.PlantationId != plantationId.Value)
            {
                throw Validation("cropCycleId", "The crop cycle does not belong to the selected plantation.");
            }
        }

        if (cropCycleStageId.HasValue && cropCycleStageId.Value != Guid.Empty)
        {
            if (!cropCycleId.HasValue || cropCycleId.Value == Guid.Empty)
            {
                throw Validation("cropCycleStageId", "A crop cycle is required when specifying a crop cycle stage.");
            }

            var stage = await store.FindCropCycleStageAsync(cropCycleStageId.Value, organizationId, cancellationToken)
                ?? throw new ResourceNotFoundException("The crop cycle stage was not found.");

            if (stage.CropCycleId != cropCycleId.Value)
            {
                throw Validation("cropCycleStageId", "The crop cycle stage does not belong to the selected crop cycle.");
            }
        }
    }

    private async Task ValidateAreaAndUnitsAsync(
        Guid organizationId,
        decimal? plannedArea,
        Guid? plannedAreaUnitId,
        CancellationToken cancellationToken)
    {
        if (plannedArea.HasValue && plannedArea.Value <= 0)
        {
            throw Validation("plannedArea", "Planned area must be greater than zero.");
        }

        if (plannedArea.HasValue && !plannedAreaUnitId.HasValue)
        {
            throw Validation("plannedAreaUnitId", "An area unit is required when planned area is specified.");
        }

        if (!plannedArea.HasValue && plannedAreaUnitId.HasValue)
        {
            throw Validation("plannedArea", "A planned area is required when area unit is specified.");
        }

        if (plannedAreaUnitId.HasValue)
        {
            var unit = await store.FindUnitAsync(plannedAreaUnitId.Value, organizationId, cancellationToken)
                ?? throw new ResourceNotFoundException("The selected area unit was not found.");

            if (!unit.IsActive)
            {
                throw Validation("plannedAreaUnitId", "The selected area unit is inactive.");
            }
        }
    }

    private async Task ValidateTreatedAreaAndUnitsAsync(
        Guid organizationId,
        decimal? actualTreatedArea,
        Guid? actualTreatedAreaUnitId,
        CancellationToken cancellationToken)
    {
        if (actualTreatedArea.HasValue && actualTreatedArea.Value <= 0)
        {
            throw Validation("actualTreatedArea", "Actual treated area must be greater than zero.");
        }

        if (actualTreatedArea.HasValue && !actualTreatedAreaUnitId.HasValue)
        {
            throw Validation("actualTreatedAreaUnitId", "An area unit is required when actual treated area is specified.");
        }

        if (!actualTreatedArea.HasValue && actualTreatedAreaUnitId.HasValue)
        {
            throw Validation("actualTreatedArea", "An actual treated area is required when area unit is specified.");
        }

        if (actualTreatedAreaUnitId.HasValue)
        {
            var unit = await store.FindUnitAsync(actualTreatedAreaUnitId.Value, organizationId, cancellationToken)
                ?? throw new ResourceNotFoundException("The selected area unit was not found.");

            if (!unit.IsActive)
            {
                throw Validation("actualTreatedAreaUnitId", "The selected area unit is inactive.");
            }
        }
    }

    private async Task ValidateWaterAndUnitsAsync(
        Guid organizationId,
        decimal? waterQuantity,
        Guid? waterUnitId,
        CancellationToken cancellationToken)
    {
        if (waterQuantity.HasValue && waterQuantity.Value <= 0)
        {
            throw Validation("waterQuantity", "Water quantity must be greater than zero.");
        }

        if (waterQuantity.HasValue && !waterUnitId.HasValue)
        {
            throw Validation("waterUnitId", "A water unit is required when water quantity is specified.");
        }

        if (!waterQuantity.HasValue && waterUnitId.HasValue)
        {
            throw Validation("waterQuantity", "A water quantity is required when water unit is specified.");
        }

        if (waterUnitId.HasValue)
        {
            var unit = await store.FindUnitAsync(waterUnitId.Value, organizationId, cancellationToken)
                ?? throw new ResourceNotFoundException("The selected water unit was not found.");

            if (!unit.IsActive)
            {
                throw Validation("waterUnitId", "The selected water unit is inactive.");
            }
        }
    }

    private async Task ValidateTargetAsync(
        Guid organizationId,
        Guid? targetId,
        CancellationToken cancellationToken)
    {
        if (targetId.HasValue && targetId.Value != Guid.Empty)
        {
            var target = await store.FindTargetAsync(targetId.Value, organizationId, cancellationToken)
                ?? throw new ResourceNotFoundException("The selected target was not found.");

            if (!target.IsActive)
            {
                throw Validation("targetId", "The selected target is inactive.");
            }
        }
    }

    private async Task ValidateApplicationMethodAsync(
        Guid organizationId,
        Guid? applicationMethodId,
        CancellationToken cancellationToken)
    {
        if (applicationMethodId.HasValue && applicationMethodId.Value != Guid.Empty)
        {
            var method = await store.FindApplicationMethodAsync(applicationMethodId.Value, organizationId, cancellationToken)
                ?? throw new ResourceNotFoundException("The selected application method was not found.");

            if (!method.IsActive)
            {
                throw Validation("applicationMethodId", "The selected application method is inactive.");
            }
        }
    }

    private async Task ValidateProductsAsync(
        Guid organizationId,
        IReadOnlyList<SprayProductItemRequest>? products,
        CancellationToken cancellationToken)
    {
        if (products is null || products.Count == 0)
        {
            return;
        }

        var duplicates = products
            .GroupBy(p => p.InventoryItemId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Count > 0)
        {
            throw Validation("products", "Duplicate inventory items are not allowed in spray products.");
        }

        foreach (var item in products)
        {
            if (item.InventoryItemId == Guid.Empty)
            {
                throw Validation("inventoryItemId", "An inventory item is required.");
            }

            if (item.PlannedQuantity.HasValue && item.PlannedQuantity.Value <= 0)
            {
                throw Validation("plannedQuantity", "Planned quantity must be greater than zero.");
            }

            var inventoryItem = await store.FindInventoryItemAsync(item.InventoryItemId, organizationId, cancellationToken)
                ?? throw new ResourceNotFoundException("The selected inventory item was not found.");

            if (!inventoryItem.IsActive)
            {
                throw Validation("products", $"The inventory item '{inventoryItem.Name}' is inactive.");
            }

            var hasActiveProfile = await store.HasActivePlantProtectionProfileAsync(item.InventoryItemId, organizationId, cancellationToken);
            if (!hasActiveProfile)
            {
                throw Validation("products", $"The inventory item '{inventoryItem.Name}' must have an active plant protection product profile.");
            }
        }
    }

    private void AddAudit(
        SprayActor actor,
        Spray spray,
        string action,
        object details,
        string? ipAddress) =>
        store.AddAuditLog(new AuditLog(
            action,
            spray.OrganizationId,
            actor.UserId,
            entityType: "Spray",
            entityId: spray.Id,
            details: details is null ? null : JsonSerializer.SerializeToDocument(details),
            ipAddress: ipAddress));

    private static ValidationException Validation(string property, string message) =>
        new("One or more validation errors occurred.", new Dictionary<string, string[]>
        {
            [property] = [message]
        });

    private static SprayListItemResponse ToListItemResponse(Spray spray, DateTimeOffset now)
    {
        var isOverdue = ComputeIsOverdue(spray, now);

        return new SprayListItemResponse(
            Id: spray.Id,
            ReferenceNumber: FormatReferenceNumber(spray.Id),
            FarmId: spray.FarmId,
            FarmName: spray.Farm?.Name ?? string.Empty,
            FarmAreaId: spray.FarmAreaId,
            FarmAreaName: spray.FarmArea?.Name,
            PlantationId: spray.PlantationId,
            PlantationName: spray.Plantation?.PlantationName,
            CropCycleId: spray.CropCycleId,
            CropCycleName: spray.CropCycle?.CycleName,
            CropCycleStageId: spray.CropCycleStageId,
            CropCycleStageName: spray.CropCycleStage?.StageName,
            Status: spray.Status,
            StatusName: spray.Status.ToString().ToUpperInvariant(),
            IsOverdue: isOverdue,
            PlannedDate: spray.PlannedDate,
            ScheduledDateTime: spray.ScheduledDateTime,
            ActualApplicationDateTime: spray.ActualApplicationDateTime,
            TargetId: spray.TargetId,
            TargetName: spray.Target?.Name,
            ApplicationMethodId: spray.ApplicationMethodId,
            ApplicationMethodName: spray.ApplicationMethod?.Name,
            ProductCount: spray.Products.Count,
            CreatedAt: spray.CreatedAt);
    }

    private static SprayDetailsResponse ToDetailsResponse(Spray spray, DateTimeOffset now)
    {
        var isOverdue = ComputeIsOverdue(spray, now);

        var products = spray.Products
            .Select(p => new SprayProductDto(
                Id: p.Id,
                InventoryItemId: p.InventoryItemId,
                InventoryItemName: p.InventoryItem?.Name ?? string.Empty,
                InventoryItemSku: p.InventoryItem?.Sku,
                StockUnitId: p.InventoryItem?.StockUnitId ?? Guid.Empty,
                StockUnitName: p.InventoryItem?.StockUnit?.Name ?? string.Empty,
                StockUnitSymbol: p.InventoryItem?.StockUnit?.Symbol ?? string.Empty,
                StorageLocationId: p.StorageLocationId,
                StorageLocationName: p.StorageLocation?.Name,
                PlannedQuantity: p.PlannedQuantity,
                ActualQuantity: p.ActualQuantity,
                Dosage: p.Dosage))
            .ToArray();

        return new SprayDetailsResponse(
            Id: spray.Id,
            ReferenceNumber: FormatReferenceNumber(spray.Id),
            OrganizationId: spray.OrganizationId,
            FarmId: spray.FarmId,
            FarmName: spray.Farm?.Name ?? string.Empty,
            FarmAreaId: spray.FarmAreaId,
            FarmAreaName: spray.FarmArea?.Name,
            PlantationId: spray.PlantationId,
            PlantationName: spray.Plantation?.PlantationName,
            CropCycleId: spray.CropCycleId,
            CropCycleName: spray.CropCycle?.CycleName,
            CropCycleStageId: spray.CropCycleStageId,
            CropCycleStageName: spray.CropCycleStage?.StageName,
            Status: spray.Status,
            StatusName: spray.Status.ToString().ToUpperInvariant(),
            IsOverdue: isOverdue,
            PlannedDate: spray.PlannedDate,
            ScheduledDateTime: spray.ScheduledDateTime,
            ActualApplicationDateTime: spray.ActualApplicationDateTime,
            PlannedArea: spray.PlannedArea,
            PlannedAreaUnitId: spray.PlannedAreaUnitId,
            PlannedAreaUnitName: spray.PlannedAreaUnit?.Name,
            ActualTreatedArea: spray.ActualTreatedArea,
            ActualTreatedAreaUnitId: spray.ActualTreatedAreaUnitId,
            ActualTreatedAreaUnitName: spray.ActualTreatedAreaUnit?.Name,
            WaterQuantity: spray.WaterQuantity,
            WaterUnitId: spray.WaterUnitId,
            WaterUnitName: spray.WaterUnit?.Name,
            TargetId: spray.TargetId,
            TargetName: spray.Target?.Name,
            TargetType: spray.Target?.TargetType,
            ApplicationMethodId: spray.ApplicationMethodId,
            ApplicationMethodName: spray.ApplicationMethod?.Name,
            PurposeReason: spray.PurposeReason,
            CancellationReason: spray.CancellationReason,
            Products: products,
            CreatedAt: spray.CreatedAt,
            CreatedBy: spray.CreatedBy,
            UpdatedAt: spray.UpdatedAt,
            UpdatedBy: spray.UpdatedBy);
    }

    private static bool ComputeIsOverdue(Spray spray, DateTimeOffset now) =>
        spray.Status == SprayStatus.Scheduled &&
        spray.ScheduledDateTime.HasValue &&
        spray.ScheduledDateTime.Value < now;

    private static string FormatReferenceNumber(Guid id) =>
        $"SP-{id.ToString()[..8].ToUpperInvariant()}";

    private static void ValidateActor(SprayActor actor)
    {
        if (actor.UserId == Guid.Empty || actor.OrganizationId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("The access token does not contain a valid user scope.");
        }
    }

    private static int NormalizePageSize(int pageSize) => pageSize switch
    {
        < 1 => DefaultPageSize,
        > MaximumPageSize => MaximumPageSize,
        _ => pageSize
    };
}
