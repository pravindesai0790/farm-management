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

    /// <summary>
    /// Fetches the current on-hand stock balance for a specific storage location and inventory item.
    /// Enables real-time stock availability preview in UI transaction dialogs.
    /// </summary>
    public async Task<StockBalanceResponse?> GetBalanceAsync(
        InventoryActor actor,
        Guid storageLocationId,
        Guid inventoryItemId,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        if (storageLocationId == Guid.Empty) throw Validation("storageLocationId", "Storage location is required.");
        if (inventoryItemId == Guid.Empty) throw Validation("inventoryItemId", "Inventory item is required.");

        var balance = await store.FindBalanceAsync(storageLocationId, inventoryItemId, actor.OrganizationId, cancellationToken);
        return balance is null ? null : ToBalanceResponse(balance);
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
        Guid? cropCycleId = null,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        if (page < 1) throw Validation("page", "Page must be at least 1.");
        pageSize = NormalizePageSize(pageSize);

        var totalCount = await store.CountMovementsAsync(actor.OrganizationId, farmId, storageLocationId, inventoryItemId, movementType, fromDate, toDate, cropCycleId, cancellationToken);
        var movements = await store.ListMovementsAsync(
            actor.OrganizationId, farmId, storageLocationId, inventoryItemId, movementType, fromDate, toDate,
            checked((page - 1) * pageSize), pageSize, cropCycleId, cancellationToken);

        return new PagedResponse<StockMovementResponse>(movements.Select(ToMovementResponse).ToArray(), page, pageSize, totalCount);
    }

    /// <summary>
    /// Records opening stock for an inventory item at a storage location.
    /// Performs atomic inside-transaction uniqueness verification and transaction-level advisory locking
    /// to prevent duplicate opening stock entries under concurrent requests.
    /// </summary>
    public async Task<StockMovementResponse> RecordOpeningStockAsync(
        InventoryActor actor,
        RecordOpeningStockRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        if (request is null) throw Validation("request", "Request body is required.");
        ValidateQuantity(request.Quantity);
        ValidateMovementDate(request.MovementDate);

        var (item, location) = await ValidateItemAndLocationAsync(actor, request.InventoryItemId, request.StorageLocationId, request.FarmId, cancellationToken);

        return await store.ExecuteInTransactionAsync(async ct =>
        {
            // Acquire PostgreSQL transaction-level advisory lock on (storageLocationId, inventoryItemId)
            // to serialize concurrent requests before locking or checking opening stock existence.
            await store.AcquireAdvisoryLockAsync(request.StorageLocationId, request.InventoryItemId, ct);

            // Atomic inside-transaction check for pre-existing opening stock record
            var hasOpening = await store.HasOpeningStockAsync(request.StorageLocationId, request.InventoryItemId, ct);
            if (hasOpening)
            {
                throw new ConflictException("Opening stock has already been recorded for this item at the selected storage location.");
            }

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

            return ToMovementResponse(movement, item, location.Farm, location, item.StockUnit);
        }, cancellationToken);
    }

    /// <summary>
    /// Records stock receipts into a storage location.
    /// Uses transaction-level advisory locking to prevent phantom lock collisions when creating initial balance rows.
    /// </summary>
    public async Task<StockMovementResponse> RecordStockReceiptAsync(
        InventoryActor actor,
        RecordStockReceiptRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        if (request is null) throw Validation("request", "Request body is required.");
        ValidateQuantity(request.Quantity);
        ValidateMovementDate(request.MovementDate);

        var (item, location) = await ValidateItemAndLocationAsync(actor, request.InventoryItemId, request.StorageLocationId, request.FarmId, cancellationToken);

        return await store.ExecuteInTransactionAsync(async ct =>
        {
            // Acquire advisory lock to serialize initial row insertions and updates safely
            await store.AcquireAdvisoryLockAsync(request.StorageLocationId, request.InventoryItemId, ct);

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

            return ToMovementResponse(movement, item, location.Farm, location, item.StockUnit);
        }, cancellationToken);
    }

    /// <summary>
    /// Records stock issues from a storage location, deducting from available balance.
    /// Enforces non-negative stock invariants.
    /// </summary>
    public async Task<StockMovementResponse> RecordStockIssueAsync(
        InventoryActor actor,
        RecordStockIssueRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        if (request is null) throw Validation("request", "Request body is required.");
        ValidateQuantity(request.Quantity);
        ValidateMovementDate(request.MovementDate);

        var (item, location) = await ValidateItemAndLocationAsync(actor, request.InventoryItemId, request.StorageLocationId, request.FarmId, cancellationToken);
        var (cycle, stage, plantation, area, activity) = await ValidateOperationalLinksAsync(
            actor, request.FarmId, request.CropCycleId, request.CropCycleStageId, request.PlantationId, request.FarmAreaId, request.LaborActivityId, cancellationToken);

        return await store.ExecuteInTransactionAsync(async ct =>
        {
            // Acquire advisory lock on target location and item
            await store.AcquireAdvisoryLockAsync(request.StorageLocationId, request.InventoryItemId, ct);

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
                notes: request.PurposeNotes,
                cropCycleId: request.CropCycleId,
                cropCycleStageId: request.CropCycleStageId,
                plantationId: request.PlantationId,
                farmAreaId: request.FarmAreaId,
                laborActivityId: request.LaborActivityId);

            store.AddMovement(movement);
            AddAudit(actor, movement, "Stock.IssueRecorded", new
            {
                item.Name,
                request.Quantity,
                request.CropCycleId,
                request.CropCycleStageId,
                request.PlantationId,
                request.FarmAreaId,
                request.LaborActivityId
            }, ipAddress);
            await store.SaveChangesAsync(ct);

            return ToMovementResponse(movement, item, location.Farm, location, item.StockUnit, cycle, stage, plantation, area, activity);
        }, cancellationToken);
    }

    /// <summary>
    /// Records manual stock adjustments (AdjustmentIn or AdjustmentOut) with mandatory audit reasoning.
    /// </summary>
    public async Task<StockMovementResponse> RecordStockAdjustmentAsync(
        InventoryActor actor,
        RecordStockAdjustmentRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        if (request is null) throw Validation("request", "Request body is required.");
        ValidateQuantity(request.Quantity);
        ValidateMovementDate(request.MovementDate);

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
            // Acquire advisory lock on target location and item
            await store.AcquireAdvisoryLockAsync(request.StorageLocationId, request.InventoryItemId, ct);

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

            return ToMovementResponse(movement, item, location.Farm, location, item.StockUnit);
        }, cancellationToken);
    }

    /// <summary>
    /// Records stock transfers between storage locations.
    /// Orders lock acquisitions deterministically to eliminate PostgreSQL 40P01 deadlocks when concurrent
    /// opposite-direction transfers occur between the same storage locations.
    /// </summary>
    public async Task<IReadOnlyList<StockMovementResponse>> RecordStockTransferAsync(
        InventoryActor actor,
        RecordStockTransferRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        if (request is null) throw Validation("request", "Request body is required.");
        ValidateQuantity(request.Quantity);
        ValidateMovementDate(request.MovementDate);

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

            // Determine deterministic lock order based on storage location GUIDs
            // Prevents deadlock when two concurrent transfers move stock in opposite directions (Loc A -> Loc B vs Loc B -> Loc A)
            var firstLocId = request.SourceStorageLocationId.CompareTo(request.DestinationStorageLocationId) < 0
                ? request.SourceStorageLocationId
                : request.DestinationStorageLocationId;

            var secondLocId = firstLocId == request.SourceStorageLocationId
                ? request.DestinationStorageLocationId
                : request.SourceStorageLocationId;

            // Step 1: Acquire advisory locks in strict deterministic order
            await store.AcquireAdvisoryLockAsync(firstLocId, request.InventoryItemId, ct);
            await store.AcquireAdvisoryLockAsync(secondLocId, request.InventoryItemId, ct);

            // Step 2: Lock balance rows in strict deterministic order
            var firstBalance = await store.LockBalanceAsync(firstLocId, request.InventoryItemId, actor.OrganizationId, ct);
            var secondBalance = await store.LockBalanceAsync(secondLocId, request.InventoryItemId, actor.OrganizationId, ct);

            // Map locks back to source and destination balances
            var sourceBalance = request.SourceStorageLocationId == firstLocId ? firstBalance : secondBalance;
            var destBalance = request.DestinationStorageLocationId == firstLocId ? firstBalance : secondBalance;

            // Validate and deduct from source location
            if (sourceBalance is null || sourceBalance.QuantityOnHand < request.Quantity)
            {
                var available = sourceBalance?.QuantityOnHand ?? 0m;
                throw Validation("quantity", $"Insufficient stock available ({available} {item.StockUnit?.Symbol}) at source location.");
            }

            sourceBalance.DeductStock(request.Quantity, now, actor.UserId);

            // Add stock to destination location balance
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
                ToMovementResponse(transferOut, item, sourceLoc.Farm, sourceLoc, item.StockUnit),
                ToMovementResponse(transferIn, item, destFarm, destLoc, item.StockUnit)
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

    private async Task<(CropCycle? Cycle, CropCycleStage? Stage, CropPlantation? Plantation, FarmArea? Area, LaborActivity? Activity)> ValidateOperationalLinksAsync(
        InventoryActor actor,
        Guid farmId,
        Guid? cropCycleId,
        Guid? cropCycleStageId,
        Guid? plantationId,
        Guid? farmAreaId,
        Guid? laborActivityId,
        CancellationToken cancellationToken)
    {
        CropCycle? cycle = null;
        CropCycleStage? stage = null;
        CropPlantation? plantation = null;
        FarmArea? area = null;
        LaborActivity? activity = null;

        if (cropCycleId.HasValue)
        {
            cycle = await store.FindCropCycleAsync(cropCycleId.Value, actor.OrganizationId, cancellationToken);
            if (cycle is null)
            {
                throw Validation("cropCycleId", "The selected crop cycle was not found in your organization.");
            }
            var cycleFarmId = cycle.Plantation?.FarmId;
            if (!cycleFarmId.HasValue)
            {
                var cyclePlantation = await store.FindPlantationAsync(cycle.PlantationId, actor.OrganizationId, cancellationToken);
                cycleFarmId = cyclePlantation?.FarmId;
            }
            if (cycleFarmId != farmId)
            {
                throw Validation("cropCycleId", "The selected crop cycle does not belong to the selected farm.");
            }
        }

        if (cropCycleStageId.HasValue)
        {
            if (!cropCycleId.HasValue)
            {
                throw Validation("cropCycleStageId", "Crop cycle stage cannot be selected without selecting a crop cycle.");
            }
            stage = await store.FindCropCycleStageAsync(cropCycleStageId.Value, cancellationToken);
            if (stage is null)
            {
                throw Validation("cropCycleStageId", "The selected crop cycle stage was not found.");
            }
            if (stage.CropCycleId != cropCycleId.Value)
            {
                throw Validation("cropCycleStageId", "The selected crop cycle stage does not belong to the selected crop cycle.");
            }
        }

        if (plantationId.HasValue)
        {
            plantation = await store.FindPlantationAsync(plantationId.Value, actor.OrganizationId, cancellationToken);
            if (plantation is null)
            {
                throw Validation("plantationId", "The selected plantation was not found in your organization.");
            }
            if (plantation.FarmId != farmId)
            {
                throw Validation("plantationId", "The selected plantation does not belong to the selected farm.");
            }
        }

        if (farmAreaId.HasValue)
        {
            area = await store.FindFarmAreaAsync(farmAreaId.Value, actor.OrganizationId, cancellationToken);
            if (area is null)
            {
                throw Validation("farmAreaId", "The selected farm area was not found in your organization.");
            }
            if (area.FarmId != farmId)
            {
                throw Validation("farmAreaId", "The selected farm area does not belong to the selected farm.");
            }
        }

        if (laborActivityId.HasValue)
        {
            activity = await store.FindLaborActivityAsync(laborActivityId.Value, actor.OrganizationId, cancellationToken);
            if (activity is null)
            {
                throw Validation("laborActivityId", "The selected labor activity was not found in your organization.");
            }
            if (activity.FarmId != farmId)
            {
                throw Validation("laborActivityId", "The selected labor activity does not belong to the selected farm.");
            }
        }

        return (cycle, stage, plantation, area, activity);
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
        ToMovementResponse(
            m,
            m.InventoryItem,
            m.Farm,
            m.StorageLocation,
            m.StockUnit,
            m.CropCycle,
            m.CropCycleStage,
            m.Plantation,
            m.FarmArea,
            m.LaborActivity);

    private static StockMovementResponse ToMovementResponse(
        StockMovement m,
        InventoryItem? item,
        Farm? farm,
        StorageLocation? location,
        Unit? unit,
        CropCycle? cycle = null,
        CropCycleStage? stage = null,
        CropPlantation? plantation = null,
        FarmArea? area = null,
        LaborActivity? activity = null) =>
        new(
            m.Id,
            m.OrganizationId,
            m.MovementType,
            m.MovementType.ToString(),
            m.InventoryItemId,
            item?.Name ?? string.Empty,
            item?.Sku,
            m.FarmId,
            farm?.Name ?? string.Empty,
            m.StorageLocationId,
            location?.Name ?? string.Empty,
            m.Quantity,
            m.StockUnitId,
            unit?.Code ?? string.Empty,
            unit?.Symbol ?? string.Empty,
            m.MovementDate,
            m.ReferenceNumber,
            m.Notes,
            m.ParentTransactionId,
            m.CreatedAt,
            m.CreatedBy,
            m.CropCycleId,
            cycle?.CycleName,
            m.CropCycleStageId,
            stage?.StageName,
            m.PlantationId,
            plantation?.PlantationName,
            m.FarmAreaId,
            area?.Name,
            m.LaborActivityId,
            activity?.LaborActivityType?.Name,
            m.IsReversed,
            m.ReversalMovementId,
            m.ReversedMovementId,
            m.ReversalReason);

    public async Task<StockMovementResponse> ReverseStockMovementAsync(
        InventoryActor actor,
        Guid movementId,
        ReverseStockMovementRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        if (request is null) throw Validation("request", "Request body is required.");
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw Validation("reason", "A reason is required to reverse an inventory transaction.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var reversalDate = request.ReversalDate ?? today;
        if (reversalDate > today)
        {
            throw Validation("reversalDate", "Reversal date cannot be in the future.");
        }

        var movement = await store.FindMovementAsync(movementId, actor.OrganizationId, cancellationToken);
        if (movement is null)
        {
            throw new KeyNotFoundException("The specified stock movement was not found.");
        }

        if (reversalDate < movement.MovementDate)
        {
            throw Validation("reversalDate", $"Reversal date ({reversalDate}) cannot precede the original movement date ({movement.MovementDate}).");
        }

        if (movement.IsReversed)
        {
            throw new InvalidOperationException("This transaction has already been reversed.");
        }

        if (StockMovement.IsReversalType(movement.MovementType) || movement.ReversedMovementId.HasValue)
        {
            throw Validation("movementId", "A reversal transaction cannot be reversed.");
        }

        return await store.ExecuteInTransactionAsync(async ct =>
        {
            var now = DateTimeOffset.UtcNow;

            if (movement.MovementType == StockMovementType.TransferOut || movement.MovementType == StockMovementType.TransferIn)
            {
                return await ReverseTransferMovementAsync(actor, movement, request.Reason.Trim(), reversalDate, now, ipAddress, ct);
            }

            return await ReverseStandardMovementAsync(actor, movement, request.Reason.Trim(), reversalDate, now, ipAddress, ct);
        }, cancellationToken);
    }

    private async Task<StockMovementResponse> ReverseStandardMovementAsync(
        InventoryActor actor,
        StockMovement movement,
        string reason,
        DateOnly reversalDate,
        DateTimeOffset now,
        string? ipAddress,
        CancellationToken ct)
    {
        var item = movement.InventoryItem ?? await store.FindItemAsync(movement.InventoryItemId, actor.OrganizationId, ct);
        var location = movement.StorageLocation ?? await store.FindLocationAsync(movement.StorageLocationId, actor.OrganizationId, ct);

        await store.AcquireAdvisoryLockAsync(movement.StorageLocationId, movement.InventoryItemId, ct);
        var balance = await store.LockBalanceAsync(movement.StorageLocationId, movement.InventoryItemId, actor.OrganizationId, ct);

        var (reversalType, isDeduction) = GetOpposingReversalType(movement.MovementType);

        if (isDeduction)
        {
            var onHand = balance?.QuantityOnHand ?? 0m;
            if (balance is null || onHand < movement.Quantity)
            {
                var unitSymbol = item?.StockUnit?.Symbol ?? item?.StockUnit?.Code ?? "";
                throw Validation("quantity", $"Cannot reverse {movement.MovementType}: {movement.Quantity} {unitSymbol} were recorded, but only {onHand} {unitSymbol} remain in {location?.Name ?? "storage location"}.");
            }

            balance.DeductStock(movement.Quantity, now, actor.UserId);
        }
        else
        {
            if (balance is null)
            {
                balance = new StockBalance(actor.OrganizationId, movement.FarmId, movement.StorageLocationId, movement.InventoryItemId, movement.Quantity);
                store.AddBalance(balance);
            }
            else
            {
                balance.AddStock(movement.Quantity, now, actor.UserId);
            }
        }

        var reversalMovement = new StockMovement(
            actor.OrganizationId,
            reversalType,
            movement.InventoryItemId,
            movement.FarmId,
            movement.StorageLocationId,
            movement.Quantity,
            movement.StockUnitId,
            reversalDate,
            actor.UserId,
            referenceNumber: movement.ReferenceNumber,
            notes: $"Reversal of {movement.MovementType} ({movement.Id}): {reason}",
            parentTransactionId: movement.ParentTransactionId,
            cropCycleId: movement.CropCycleId,
            cropCycleStageId: movement.CropCycleStageId,
            plantationId: movement.PlantationId,
            farmAreaId: movement.FarmAreaId,
            laborActivityId: movement.LaborActivityId,
            reversedMovementId: movement.Id);

        store.AddMovement(reversalMovement);
        movement.MarkAsReversed(reversalMovement.Id, reason);

        AddAudit(actor, movement, "Stock.MovementReversed", new
        {
            OriginalMovementId = movement.Id,
            ReversalMovementId = reversalMovement.Id,
            OriginalType = movement.MovementType.ToString(),
            ReversalType = reversalType.ToString(),
            movement.Quantity,
            Reason = reason
        }, ipAddress);

        await store.SaveChangesAsync(ct);

        return ToMovementResponse(
            reversalMovement,
            item,
            location?.Farm ?? movement.Farm,
            location,
            item?.StockUnit ?? movement.StockUnit,
            movement.CropCycle,
            movement.CropCycleStage,
            movement.Plantation,
            movement.FarmArea,
            movement.LaborActivity);
    }

    private async Task<StockMovementResponse> ReverseTransferMovementAsync(
        InventoryActor actor,
        StockMovement targetMovement,
        string reason,
        DateOnly reversalDate,
        DateTimeOffset now,
        string? ipAddress,
        CancellationToken ct)
    {
        if (!targetMovement.ParentTransactionId.HasValue)
        {
            throw Validation("parentTransactionId", "Transfer movement lacks a parent transaction identifier.");
        }

        var legs = await store.FindMovementsByParentTransactionIdAsync(targetMovement.ParentTransactionId.Value, actor.OrganizationId, ct);
        var outLeg = legs.FirstOrDefault(m => m.MovementType == StockMovementType.TransferOut);
        var inLeg = legs.FirstOrDefault(m => m.MovementType == StockMovementType.TransferIn);

        if (outLeg is null || inLeg is null)
        {
            throw Validation("parentTransactionId", "The transfer transaction legs could not be fully identified.");
        }

        if (outLeg.IsReversed || inLeg.IsReversed)
        {
            throw new InvalidOperationException("This transfer transaction has already been reversed.");
        }

        var item = outLeg.InventoryItem ?? await store.FindItemAsync(outLeg.InventoryItemId, actor.OrganizationId, ct);

        var firstLocId = outLeg.StorageLocationId.CompareTo(inLeg.StorageLocationId) < 0
            ? outLeg.StorageLocationId
            : inLeg.StorageLocationId;
        var secondLocId = firstLocId == outLeg.StorageLocationId
            ? inLeg.StorageLocationId
            : outLeg.StorageLocationId;

        await store.AcquireAdvisoryLockAsync(firstLocId, outLeg.InventoryItemId, ct);
        await store.AcquireAdvisoryLockAsync(secondLocId, outLeg.InventoryItemId, ct);

        var destBalance = await store.LockBalanceAsync(inLeg.StorageLocationId, inLeg.InventoryItemId, actor.OrganizationId, ct);
        var sourceBalance = await store.LockBalanceAsync(outLeg.StorageLocationId, outLeg.InventoryItemId, actor.OrganizationId, ct);

        var destOnHand = destBalance?.QuantityOnHand ?? 0m;
        if (destBalance is null || destOnHand < inLeg.Quantity)
        {
            var unitSymbol = item?.StockUnit?.Symbol ?? item?.StockUnit?.Code ?? "";
            var destLocName = inLeg.StorageLocation?.Name ?? "destination location";
            throw Validation("quantity", $"Cannot reverse transfer: destination '{destLocName}' only has {destOnHand} {unitSymbol} on hand, but {inLeg.Quantity} {unitSymbol} is required to reverse.");
        }

        destBalance.DeductStock(inLeg.Quantity, now, actor.UserId);

        if (sourceBalance is null)
        {
            sourceBalance = new StockBalance(actor.OrganizationId, outLeg.FarmId, outLeg.StorageLocationId, outLeg.InventoryItemId, outLeg.Quantity);
            store.AddBalance(sourceBalance);
        }
        else
        {
            sourceBalance.AddStock(outLeg.Quantity, now, actor.UserId);
        }

        var reversalParentTransactionId = Guid.NewGuid();

        var outReversal = new StockMovement(
            actor.OrganizationId,
            StockMovementType.TransferOutReversal,
            outLeg.InventoryItemId,
            outLeg.FarmId,
            outLeg.StorageLocationId,
            outLeg.Quantity,
            outLeg.StockUnitId,
            reversalDate,
            actor.UserId,
            referenceNumber: outLeg.ReferenceNumber,
            notes: $"Reversal of TransferOut ({outLeg.Id}): {reason}",
            parentTransactionId: reversalParentTransactionId,
            reversedMovementId: outLeg.Id);

        var inReversal = new StockMovement(
            actor.OrganizationId,
            StockMovementType.TransferInReversal,
            inLeg.InventoryItemId,
            inLeg.FarmId,
            inLeg.StorageLocationId,
            inLeg.Quantity,
            inLeg.StockUnitId,
            reversalDate,
            actor.UserId,
            referenceNumber: inLeg.ReferenceNumber,
            notes: $"Reversal of TransferIn ({inLeg.Id}): {reason}",
            parentTransactionId: reversalParentTransactionId,
            reversedMovementId: inLeg.Id);

        store.AddMovement(outReversal);
        store.AddMovement(inReversal);

        outLeg.MarkAsReversed(outReversal.Id, reason);
        inLeg.MarkAsReversed(inReversal.Id, reason);

        AddAudit(actor, targetMovement, "Stock.TransferReversed", new
        {
            ParentTransactionId = targetMovement.ParentTransactionId,
            ReversalParentTransactionId = reversalParentTransactionId,
            OutLegId = outLeg.Id,
            OutReversalId = outReversal.Id,
            InLegId = inLeg.Id,
            InReversalId = inReversal.Id,
            Reason = reason
        }, ipAddress);

        await store.SaveChangesAsync(ct);

        var returnMovement = targetMovement.Id == inLeg.Id ? inReversal : outReversal;
        var returnLoc = returnMovement.StorageLocationId == inLeg.StorageLocationId ? inLeg.StorageLocation : outLeg.StorageLocation;

        return ToMovementResponse(
            returnMovement,
            item,
            returnLoc?.Farm,
            returnLoc,
            item?.StockUnit);
    }

    private static (StockMovementType ReversalType, bool IsDeduction) GetOpposingReversalType(StockMovementType type) => type switch
    {
        StockMovementType.OpeningStock => (StockMovementType.OpeningStockReversal, true),
        StockMovementType.Receipt => (StockMovementType.ReceiptReversal, true),
        StockMovementType.Issue => (StockMovementType.IssueReversal, false),
        StockMovementType.AdjustmentIn => (StockMovementType.AdjustmentInReversal, true),
        StockMovementType.AdjustmentOut => (StockMovementType.AdjustmentOutReversal, false),
        _ => throw new InvalidOperationException($"Unsupported movement type for standard reversal: {type}")
    };

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

    /// <summary>
    /// Validates that movement date is not in the future.
    /// </summary>
    private static void ValidateMovementDate(DateOnly movementDate)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (movementDate > today)
        {
            throw Validation("movementDate", "Transaction date cannot be in the future.");
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
