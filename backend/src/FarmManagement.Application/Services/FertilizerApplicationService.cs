using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Fertilizer;
using FarmManagement.Application.Interfaces.Fertilizer;
using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Services;

public sealed class FertilizerApplicationService(IFertilizerApplicationStore store) : IFertilizerApplicationService
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
