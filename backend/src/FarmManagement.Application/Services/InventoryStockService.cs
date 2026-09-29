using System.Text.Json;
using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Inventory;
using FarmManagement.Application.Interfaces.Inventory;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.Services;

public sealed class InventoryStockService(IInventoryStockStore store) : IInventoryStockService
{
    private const int DefaultPageSize = 20;
    private const int MaximumPageSize = 100;

    public async Task<PagedResponse<StockBalanceResponse>> GetOverviewAsync(
        InventoryActor actor,
        int page,
        int pageSize,
        Guid? farmId,
        Guid? storageLocationId,
        Guid? inventoryItemId,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        if (page < 1) throw Validation("page", "Page must be at least 1.");
        pageSize = NormalizePageSize(pageSize);

        var totalCount = await store.CountBalancesAsync(actor.OrganizationId, farmId, storageLocationId, inventoryItemId, cancellationToken);
        var balances = await store.ListBalancesAsync(
            actor.OrganizationId, farmId, storageLocationId, inventoryItemId,
            checked((page - 1) * pageSize), pageSize, cancellationToken);

        return new PagedResponse<StockBalanceResponse>(balances.Select(ToBalanceResponse).ToArray(), page, pageSize, totalCount);
    }

    public async Task<PagedResponse<StockMovementResponse>> GetLedgerAsync(
        InventoryActor actor,
        int page,
        int pageSize,
        Guid? farmId,
        Guid? storageLocationId,
        Guid? inventoryItemId,
        StockMovementType? movementType,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        if (page < 1) throw Validation("page", "Page must be at least 1.");
        pageSize = NormalizePageSize(pageSize);

        var totalCount = await store.CountMovementsAsync(actor.OrganizationId, farmId, storageLocationId, inventoryItemId, movementType, fromDate, toDate, cancellationToken);
        var movements = await store.ListMovementsAsync(
            actor.OrganizationId, farmId, storageLocationId, inventoryItemId, movementType, fromDate, toDate,
            checked((page - 1) * pageSize), pageSize, cancellationToken);

        return new PagedResponse<StockMovementResponse>(movements.Select(ToMovementResponse).ToArray(), page, pageSize, totalCount);
    }

    public async Task<StockMovementResponse> RecordOpeningStockAsync(
        InventoryActor actor,
        RecordOpeningStockRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        if (request is null) throw Validation("request", "Request body is required.");
        ValidateQuantity(request.Quantity);

        var (item, location) = await ValidateItemAndLocationAsync(actor, request.InventoryItemId, request.StorageLocationId, request.FarmId, cancellationToken);

        var hasOpening = await store.HasOpeningStockAsync(request.StorageLocationId, request.InventoryItemId, cancellationToken);
        if (hasOpening)
        {
            throw new ConflictException("Opening stock has already been recorded for this item at the selected storage location.");
        }

        return await store.ExecuteInTransactionAsync(async ct =>
        {
            var now = DateTimeOffset.UtcNow;
            var balance = await store.LockBalanceAsync(request.StorageLocationId, request.InventoryItemId, actor.OrganizationId, ct);
            if (balance is null)
            {
                balance = new StockBalance(actor.OrganizationId, request.FarmId, request.StorageLocationId, request.InventoryItemId, request.Quantity);
                store.AddBalance(balance);
            }
            else
            {
                balance.AddStock(request.Quantity, now, actor.UserId);
            }

            var movement = new StockMovement(
                actor.OrganizationId,
                StockMovementType.OpeningStock,
                request.InventoryItemId,
                request.FarmId,
                request.StorageLocationId,
                request.Quantity,
                item.StockUnitId,
                request.MovementDate,
                actor.UserId,
                notes: request.Notes);

            store.AddMovement(movement);
            AddAudit(actor, movement, "Stock.OpeningStockRecorded", new { item.Name, request.Quantity }, ipAddress);
            await store.SaveChangesAsync(ct);

            return ToMovementResponse(movement, item, location.Farm!, location, item.StockUnit!);
        }, cancellationToken);
    }

    public async Task<StockMovementResponse> RecordStockReceiptAsync(
        InventoryActor actor,
        RecordStockReceiptRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        if (request is null) throw Validation("request", "Request body is required.");
        ValidateQuantity(request.Quantity);

        var (item, location) = await ValidateItemAndLocationAsync(actor, request.InventoryItemId, request.StorageLocationId, request.FarmId, cancellationToken);

        return await store.ExecuteInTransactionAsync(async ct =>
        {
            var now = DateTimeOffset.UtcNow;
            var balance = await store.LockBalanceAsync(request.StorageLocationId, request.InventoryItemId, actor.OrganizationId, ct);
            if (balance is null)
            {
                balance = new StockBalance(actor.OrganizationId, request.FarmId, request.StorageLocationId, request.InventoryItemId, request.Quantity);
                store.AddBalance(balance);
            }
            else
            {
                balance.AddStock(request.Quantity, now, actor.UserId);
            }

            var movement = new StockMovement(
                actor.OrganizationId,
                StockMovementType.Receipt,
                request.InventoryItemId,
                request.FarmId,
                request.StorageLocationId,
                request.Quantity,
                item.StockUnitId,
                request.MovementDate,
                actor.UserId,
                referenceNumber: request.ReferenceNumber,
                notes: request.Notes);

            store.AddMovement(movement);
            AddAudit(actor, movement, "Stock.ReceiptRecorded", new { item.Name, request.Quantity }, ipAddress);
            await store.SaveChangesAsync(ct);

            return ToMovementResponse(movement, item, location.Farm!, location, item.StockUnit!);
        }, cancellationToken);
    }

    public async Task<StockMovementResponse> RecordStockIssueAsync(
        InventoryActor actor,
        RecordStockIssueRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        if (request is null) throw Validation("request", "Request body is required.");
        ValidateQuantity(request.Quantity);

        var (item, location) = await ValidateItemAndLocationAsync(actor, request.InventoryItemId, request.StorageLocationId, request.FarmId, cancellationToken);

        return await store.ExecuteInTransactionAsync(async ct =>
        {
            var now = DateTimeOffset.UtcNow;
            var balance = await store.LockBalanceAsync(request.StorageLocationId, request.InventoryItemId, actor.OrganizationId, ct);
            if (balance is null || balance.QuantityOnHand < request.Quantity)
            {
                var available = balance?.QuantityOnHand ?? 0m;
                throw Validation("quantity", $"Insufficient stock available ({available} {item.StockUnit?.Symbol}) at the selected storage location.");
            }

            balance.DeductStock(request.Quantity, now, actor.UserId);

            var movement = new StockMovement(
                actor.OrganizationId,
                StockMovementType.Issue,
                request.InventoryItemId,
                request.FarmId,
                request.StorageLocationId,
                request.Quantity,
                item.StockUnitId,
                request.MovementDate,
                actor.UserId,
                referenceNumber: request.ReferenceNumber,
                notes: request.PurposeNotes);

            store.AddMovement(movement);
            AddAudit(actor, movement, "Stock.IssueRecorded", new { item.Name, request.Quantity }, ipAddress);
            await store.SaveChangesAsync(ct);

            return ToMovementResponse(movement, item, location.Farm!, location, item.StockUnit!);
        }, cancellationToken);
    }

    public async Task<StockMovementResponse> RecordStockAdjustmentAsync(
        InventoryActor actor,
        RecordStockAdjustmentRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        if (request is null) throw Validation("request", "Request body is required.");
        ValidateQuantity(request.Quantity);

        if (request.AdjustmentType != StockMovementType.AdjustmentIn && request.AdjustmentType != StockMovementType.AdjustmentOut)
        {
            throw Validation("adjustmentType", "Adjustment type must be AdjustmentIn or AdjustmentOut.");
        }

        if (string.IsNullOrWhiteSpace(request.ReasonNotes))
        {
            throw Validation("reasonNotes", "A reason is required for stock adjustments.");
        }

        var (item, location) = await ValidateItemAndLocationAsync(actor, request.InventoryItemId, request.StorageLocationId, request.FarmId, cancellationToken);

        return await store.ExecuteInTransactionAsync(async ct =>
        {
            var now = DateTimeOffset.UtcNow;
            var balance = await store.LockBalanceAsync(request.StorageLocationId, request.InventoryItemId, actor.OrganizationId, ct);

            if (request.AdjustmentType == StockMovementType.AdjustmentIn)
            {
                if (balance is null)
                {
                    balance = new StockBalance(actor.OrganizationId, request.FarmId, request.StorageLocationId, request.InventoryItemId, request.Quantity);
                    store.AddBalance(balance);
                }
                else
                {
                    balance.AddStock(request.Quantity, now, actor.UserId);
                }
            }
            else
            {
                if (balance is null || balance.QuantityOnHand < request.Quantity)
                {
                    var available = balance?.QuantityOnHand ?? 0m;
                    throw Validation("quantity", $"Insufficient stock available ({available} {item.StockUnit?.Symbol}) for adjustment out.");
                }

                balance.DeductStock(request.Quantity, now, actor.UserId);
            }

            var movement = new StockMovement(
                actor.OrganizationId,
                request.AdjustmentType,
                request.InventoryItemId,
                request.FarmId,
                request.StorageLocationId,
                request.Quantity,
                item.StockUnitId,
                request.MovementDate,
                actor.UserId,
                notes: request.ReasonNotes.Trim());

            store.AddMovement(movement);
            AddAudit(actor, movement, "Stock.AdjustmentRecorded", new { item.Name, Type = request.AdjustmentType.ToString(), request.Quantity }, ipAddress);
            await store.SaveChangesAsync(ct);

            return ToMovementResponse(movement, item, location.Farm!, location, item.StockUnit!);
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<StockMovementResponse>> RecordStockTransferAsync(
        InventoryActor actor,
        RecordStockTransferRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        if (request is null) throw Validation("request", "Request body is required.");
        ValidateQuantity(request.Quantity);

        if (request.SourceStorageLocationId == request.DestinationStorageLocationId)
        {
            throw Validation("destinationStorageLocationId", "Source and destination storage locations must be different.");
        }

        var (item, sourceLoc) = await ValidateItemAndLocationAsync(actor, request.InventoryItemId, request.SourceStorageLocationId, request.SourceFarmId, cancellationToken);
        var destLoc = await store.FindLocationAsync(request.DestinationStorageLocationId, actor.OrganizationId, cancellationToken);

        if (destLoc is null || !destLoc.IsActive || destLoc.FarmId != request.DestinationFarmId)
        {
            throw Validation("destinationStorageLocationId", "The destination storage location was not found, is inactive, or does not match the destination farm.");
        }

        var destFarm = await store.FindFarmAsync(request.DestinationFarmId, actor.OrganizationId, cancellationToken);
        if (destFarm is null)
        {
            throw Validation("destinationFarmId", "The destination farm was not found.");
        }

        return await store.ExecuteInTransactionAsync(async ct =>
        {
            var now = DateTimeOffset.UtcNow;
            var parentTransactionId = Guid.NewGuid();

            var sourceBalance = await store.LockBalanceAsync(request.SourceStorageLocationId, request.InventoryItemId, actor.OrganizationId, ct);
            if (sourceBalance is null || sourceBalance.QuantityOnHand < request.Quantity)
            {
                var available = sourceBalance?.QuantityOnHand ?? 0m;
                throw Validation("quantity", $"Insufficient stock available ({available} {item.StockUnit?.Symbol}) at source location.");
            }

            sourceBalance.DeductStock(request.Quantity, now, actor.UserId);

            var destBalance = await store.LockBalanceAsync(request.DestinationStorageLocationId, request.InventoryItemId, actor.OrganizationId, ct);
            if (destBalance is null)
            {
                destBalance = new StockBalance(actor.OrganizationId, request.DestinationFarmId, request.DestinationStorageLocationId, request.InventoryItemId, request.Quantity);
                store.AddBalance(destBalance);
            }
            else
            {
                destBalance.AddStock(request.Quantity, now, actor.UserId);
            }

            var transferOut = new StockMovement(
                actor.OrganizationId,
                StockMovementType.TransferOut,
                request.InventoryItemId,
                request.SourceFarmId,
                request.SourceStorageLocationId,
                request.Quantity,
                item.StockUnitId,
                request.MovementDate,
                actor.UserId,
                referenceNumber: request.ReferenceNumber,
                notes: request.Notes,
                parentTransactionId: parentTransactionId);

            var transferIn = new StockMovement(
                actor.OrganizationId,
                StockMovementType.TransferIn,
                request.InventoryItemId,
                request.DestinationFarmId,
                request.DestinationStorageLocationId,
                request.Quantity,
                item.StockUnitId,
                request.MovementDate,
                actor.UserId,
                referenceNumber: request.ReferenceNumber,
                notes: request.Notes,
                parentTransactionId: parentTransactionId);

            store.AddMovement(transferOut);
            store.AddMovement(transferIn);
            AddAudit(actor, transferOut, "Stock.TransferRecorded", new { item.Name, request.Quantity, SourceLocation = sourceLoc.Name, DestinationLocation = destLoc.Name }, ipAddress);
            await store.SaveChangesAsync(ct);

            return new[]
            {
                ToMovementResponse(transferOut, item, sourceLoc.Farm!, sourceLoc, item.StockUnit!),
                ToMovementResponse(transferIn, item, destFarm, destLoc, item.StockUnit!)
            };
        }, cancellationToken);
    }

    private async Task<(InventoryItem Item, StorageLocation Location)> ValidateItemAndLocationAsync(
        InventoryActor actor,
        Guid itemId,
        Guid locationId,
        Guid farmId,
        CancellationToken cancellationToken)
    {
        var item = await store.FindItemAsync(itemId, actor.OrganizationId, cancellationToken);
        if (item is null || !item.IsActive)
        {
            throw Validation("inventoryItemId", "The selected inventory item was not found or is inactive.");
        }

        var location = await store.FindLocationAsync(locationId, actor.OrganizationId, cancellationToken);
        if (location is null || !location.IsActive || location.FarmId != farmId)
        {
            throw Validation("storageLocationId", "The selected storage location was not found, is inactive, or does not belong to the selected farm.");
        }

        return (item, location);
    }

    private void AddAudit(InventoryActor actor, StockMovement movement, string action, object? details, string? ipAddress) =>
        store.AddAuditLog(new AuditLog(
            action,
            movement.OrganizationId,
            actor.UserId,
            entityType: "StockMovement",
            entityId: movement.Id,
            details: details is null ? null : JsonSerializer.SerializeToDocument(details),
            ipAddress: ipAddress));

    private static StockBalanceResponse ToBalanceResponse(StockBalance b) =>
        new(
            b.Id,
            b.OrganizationId,
            b.FarmId,
            b.Farm?.Name ?? string.Empty,
            b.StorageLocationId,
            b.StorageLocation?.Name ?? string.Empty,
            b.InventoryItemId,
            b.InventoryItem?.Name ?? string.Empty,
            b.InventoryItem?.Sku,
            b.InventoryItem?.Category,
            b.InventoryItem?.StockUnitId ?? Guid.Empty,
            b.InventoryItem?.StockUnit?.Code ?? string.Empty,
            b.InventoryItem?.StockUnit?.Name ?? string.Empty,
            b.InventoryItem?.StockUnit?.Symbol ?? string.Empty,
            b.QuantityOnHand,
            b.CreatedAt,
            b.UpdatedAt);

    private static StockMovementResponse ToMovementResponse(StockMovement m) =>
        ToMovementResponse(m, m.InventoryItem!, m.Farm!, m.StorageLocation!, m.StockUnit!);

    private static StockMovementResponse ToMovementResponse(
        StockMovement m, InventoryItem item, Farm farm, StorageLocation location, Unit unit) =>
        new(
            m.Id,
            m.OrganizationId,
            m.MovementType,
            m.MovementType.ToString(),
            m.InventoryItemId,
            item.Name,
            item.Sku,
            m.FarmId,
            farm.Name,
            m.StorageLocationId,
            location.Name,
            m.Quantity,
            m.StockUnitId,
            unit.Code,
            unit.Symbol,
            m.MovementDate,
            m.ReferenceNumber,
            m.Notes,
            m.ParentTransactionId,
            m.CreatedAt,
            m.CreatedBy);

    private static void ValidateActor(InventoryActor actor)
    {
        if (actor.UserId == Guid.Empty || actor.OrganizationId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("The access token does not contain a valid user scope.");
        }
    }

    private static void ValidateQuantity(decimal quantity)
    {
        if (quantity <= 0m)
        {
            throw Validation("quantity", "Quantity must be greater than zero.");
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
