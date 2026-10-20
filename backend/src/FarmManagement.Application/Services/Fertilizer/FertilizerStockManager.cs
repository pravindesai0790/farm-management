using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.Fertilizer;
using FarmManagement.Application.Interfaces.Fertilizer;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.Services.Fertilizer;

public sealed class FertilizerStockManager(IFertilizerApplicationStore store) : IFertilizerStockManager
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public async Task<FertilizerOperationIdempotencyRecord?> CheckIdempotencyAsync(
        Guid organizationId,
        string operationType,
        string? idempotencyKey,
        object? requestPayload,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return null;
        }

        var trimmedKey = idempotencyKey.Trim();
        var existing = await store.FindIdempotencyRecordAsync(organizationId, operationType, trimmedKey, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var requestHash = ComputeRequestHash(requestPayload);
        if (string.Equals(existing.RequestHash, requestHash, StringComparison.OrdinalIgnoreCase))
        {
            return existing;
        }

        throw new ConflictException($"An operation '{operationType}' with idempotency key '{trimmedKey}' already exists with different request parameters.");
    }

    public void RecordIdempotency(
        Guid organizationId,
        string operationType,
        string idempotencyKey,
        Guid fertilizerApplicationId,
        object? requestPayload)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return;
        }

        var requestHash = ComputeRequestHash(requestPayload);
        var record = new FertilizerOperationIdempotencyRecord(
            organizationId,
            operationType,
            idempotencyKey.Trim(),
            fertilizerApplicationId,
            requestHash);

        store.AddIdempotencyRecord(record);
    }

    public async Task<FertilizerStockConsumptionResult> ConsumeStockAsync(
        FertilizerActor actor,
        FertilizerApplication application,
        IReadOnlyList<FertilizerStockConsumptionItem> itemsToIssue,
        DateOnly movementDate,
        string operationType,
        string? idempotencyKey,
        object? requestPayloadForHashing,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(itemsToIssue);

        if (itemsToIssue.Count == 0)
        {
            return new FertilizerStockConsumptionResult(false, [], []);
        }

        // Validate negative quantities
        foreach (var item in itemsToIssue)
        {
            if (item.ActualQuantity < 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(item.ActualQuantity), "Actual quantity cannot be negative.");
            }
        }

        // Check idempotency replay before initiating transaction
        var replay = await CheckIdempotencyAsync(actor.OrganizationId, operationType, idempotencyKey, requestPayloadForHashing, cancellationToken);
        if (replay is not null)
        {
            return new FertilizerStockConsumptionResult(true, [], []);
        }

        // Filter out zero quantity items (zero creates no movement and no deduction)
        var positiveItems = itemsToIssue.Where(i => i.ActualQuantity > 0m).ToList();
        if (positiveItems.Count == 0)
        {
            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                RecordIdempotency(actor.OrganizationId, operationType, idempotencyKey, application.Id, requestPayloadForHashing);
                await store.SaveChangesAsync(cancellationToken);
            }

            return new FertilizerStockConsumptionResult(false, [], []);
        }

        // Deterministic lock ordering by StorageLocationId ASC, then InventoryItemId ASC
        var orderedItems = positiveItems
            .OrderBy(i => i.StorageLocationId)
            .ThenBy(i => i.InventoryItemId)
            .ToList();

        return await store.ExecuteInTransactionAsync(async ct =>
        {
            var lockedBalances = new List<(StockBalance Balance, decimal Quantity, InventoryItem Item, StorageLocation Location, FertilizerStockConsumptionItem RequestItem)>();
            var shortages = new List<FertilizerStockShortageDto>();
            var shortageErrors = new Dictionary<string, string[]>();

            for (var i = 0; i < orderedItems.Count; i++)
            {
                var reqItem = orderedItems[i];
                await store.AcquireAdvisoryLockAsync(reqItem.StorageLocationId, reqItem.InventoryItemId, ct);

                var balance = await store.LockBalanceAsync(reqItem.StorageLocationId, reqItem.InventoryItemId, actor.OrganizationId, ct);
                var available = balance?.QuantityOnHand ?? 0m;

                var itemEntity = await store.FindInventoryItemAsync(reqItem.InventoryItemId, actor.OrganizationId, ct)
                    ?? throw new ResourceNotFoundException("The selected inventory item was not found.");
                var locationEntity = await store.FindStorageLocationAsync(reqItem.StorageLocationId, actor.OrganizationId, ct)
                    ?? throw new ResourceNotFoundException("The selected storage location was not found.");

                if (available < reqItem.ActualQuantity)
                {
                    var shortageQty = reqItem.ActualQuantity - available;
                    var unitSymbol = itemEntity.StockUnit?.Symbol ?? string.Empty;

                    shortages.Add(new FertilizerStockShortageDto(
                        itemEntity.Id,
                        itemEntity.Name,
                        itemEntity.Sku,
                        locationEntity.Id,
                        locationEntity.Name,
                        reqItem.ActualQuantity,
                        available,
                        shortageQty,
                        unitSymbol));

                    shortageErrors[$"products[{i}]"] =
                    [
                        $"Insufficient stock for '{itemEntity.Name}'. Required: {reqItem.ActualQuantity}, Available: {available}, Shortage: {shortageQty} at '{locationEntity.Name}'."
                    ];
                }
                else if (balance is not null)
                {
                    lockedBalances.Add((balance, reqItem.ActualQuantity, itemEntity, locationEntity, reqItem));
                }
                else
                {
                    // Balance record does not exist -> available is 0
                    var shortageQty = reqItem.ActualQuantity;
                    var unitSymbol = itemEntity.StockUnit?.Symbol ?? string.Empty;

                    shortages.Add(new FertilizerStockShortageDto(
                        itemEntity.Id,
                        itemEntity.Name,
                        itemEntity.Sku,
                        locationEntity.Id,
                        locationEntity.Name,
                        reqItem.ActualQuantity,
                        0m,
                        shortageQty,
                        unitSymbol));

                    shortageErrors[$"products[{i}]"] =
                    [
                        $"Insufficient stock for '{itemEntity.Name}'. Required: {reqItem.ActualQuantity}, Available: 0, Shortage: {shortageQty} at '{locationEntity.Name}'."
                    ];
                }
            }

            if (shortages.Count > 0)
            {
                throw new ValidationException("One or more product lines have insufficient stock.", shortageErrors);
            }

            var now = DateTimeOffset.UtcNow;
            var movements = new List<StockMovement>();
            var deductions = new List<FertilizerStockDeductionDetail>();

            foreach (var (balance, quantity, itemEntity, _, reqItem) in lockedBalances)
            {
                balance.DeductStock(quantity, now, actor.UserId);

                var defaultNotes = $"Applied during fertilizer application {application.ReferenceNumber}";
                var movement = new StockMovement(
                    organizationId: actor.OrganizationId,
                    movementType: StockMovementType.Issue,
                    inventoryItemId: reqItem.InventoryItemId,
                    farmId: application.FarmId!.Value,
                    storageLocationId: reqItem.StorageLocationId,
                    quantity: quantity,
                    stockUnitId: itemEntity.StockUnitId,
                    movementDate: movementDate,
                    createdBy: actor.UserId,
                    referenceNumber: application.ReferenceNumber,
                    notes: notes ?? defaultNotes,
                    cropCycleId: application.CropCycleId,
                    cropCycleStageId: application.CropCycleStageId,
                    plantationId: application.PlantationId,
                    farmAreaId: application.FarmAreaId,
                    laborActivityId: null,
                    sprayId: null,
                    fertilizerApplicationId: application.Id);

                store.AddMovement(movement);
                movements.Add(movement);
                deductions.Add(new FertilizerStockDeductionDetail(reqItem.InventoryItemId, reqItem.StorageLocationId, quantity, movement.Id));

                var line = application.Products.FirstOrDefault(p => p.InventoryItemId == reqItem.InventoryItemId);
                if (line is not null)
                {
                    line.RecordActual(quantity, movement.Id, now, actor.UserId);
                }
            }

            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                RecordIdempotency(actor.OrganizationId, operationType, idempotencyKey, application.Id, requestPayloadForHashing);
            }

            await store.SaveChangesAsync(ct);
            return new FertilizerStockConsumptionResult(false, movements, deductions);
        }, cancellationToken);
    }

    public static string ComputeRequestHash(object? payload)
    {
        if (payload is null)
        {
            return "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
        }

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
