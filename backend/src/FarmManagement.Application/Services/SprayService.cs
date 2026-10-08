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
