using System.Text.Json;
using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Expenses;
using FarmManagement.Application.Interfaces.Expenses;
using FarmManagement.Application.Interfaces.Inventory;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.Services;

public sealed class PurchaseInvoiceService(IPurchaseInvoiceStore store, IInventoryStockStore stockStore) : IPurchaseInvoiceService
{
    private const int DefaultPageSize = 20;
    private const int MaximumPageSize = 100;

    public async Task<PagedResponse<PurchaseInvoiceResponse>> ListAsync(
        ExpenseActor actor,
        PurchaseInvoiceFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var normalizedPage = Math.Max(1, page);
        var normalizedPageSize = Math.Clamp(pageSize <= 0 ? DefaultPageSize : pageSize, 1, MaximumPageSize);

        var totalCount = await store.CountAsync(actor.OrganizationId, filter, cancellationToken);
        var skip = (normalizedPage - 1) * normalizedPageSize;
        var invoices = await store.ListAsync(actor.OrganizationId, filter, skip, normalizedPageSize, cancellationToken);

        var responses = invoices.Select(MapToResponse).ToList();
        return new PagedResponse<PurchaseInvoiceResponse>(responses, normalizedPage, normalizedPageSize, totalCount);
    }

    public async Task<PurchaseInvoiceResponse> GetAsync(
        ExpenseActor actor,
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var invoice = await FindInvoiceOrThrowAsync(actor, invoiceId, cancellationToken);
        return MapToResponse(invoice);
    }

    public async Task<PurchaseInvoiceResponse> CreateDraftAsync(
        ExpenseActor actor,
        CreatePurchaseInvoiceRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        await ValidateHeaderDataAsync(actor.OrganizationId, request.SupplierId, request.FarmId, request.CurrencyId, request.SupplierInvoiceNumber, null, request.InvoiceDate, request.DueDate, cancellationToken);
        await ValidateLinesDataAsync(actor.OrganizationId, request.FarmId, request.Lines, cancellationToken);

        var invoice = PurchaseInvoice.CreateDraft(
            actor.OrganizationId,
            request.SupplierId,
            request.FarmId,
            request.SupplierInvoiceNumber,
            request.InvoiceDate,
            request.CurrencyId,
            actor.UserId,
            request.DueDate,
            request.PaymentTerms,
            request.TaxAmount,
            request.OtherCharges,
            request.DiscountAmount,
            request.Notes,
            request.AttachmentReference);

        AddLinesToInvoice(invoice, actor.OrganizationId, request.Lines);
        invoice.RecalculateTotals();

        await store.AddAsync(invoice, cancellationToken);
        AddAudit(actor, invoice, "PurchaseInvoice.CreateDraft", new { invoice.SupplierInvoiceNumber, invoice.TotalAmount, LineCount = invoice.Lines.Count }, ipAddress);

        var created = await store.FindAsync(invoice.Id, actor.OrganizationId, cancellationToken);
        return MapToResponse(created!);
    }

    public async Task<PurchaseInvoiceResponse> UpdateDraftAsync(
        ExpenseActor actor,
        Guid invoiceId,
        UpdatePurchaseInvoiceRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var invoice = await FindInvoiceOrThrowAsync(actor, invoiceId, cancellationToken);

        if (invoice.Status != PurchaseInvoiceStatus.Draft)
        {
            throw new ValidationException($"Only draft invoices can be updated. Invoice '{invoiceId}' is currently in '{invoice.Status}' status.");
        }

        await ValidateHeaderDataAsync(actor.OrganizationId, request.SupplierId, request.FarmId, request.CurrencyId, request.SupplierInvoiceNumber, invoiceId, request.InvoiceDate, request.DueDate, cancellationToken);
        await ValidateLinesDataAsync(actor.OrganizationId, request.FarmId, request.Lines, cancellationToken);

        invoice.UpdateDraft(
            request.SupplierId,
            request.FarmId,
            request.SupplierInvoiceNumber,
            request.InvoiceDate,
            request.CurrencyId,
            actor.UserId,
            request.DueDate,
            request.PaymentTerms,
            request.TaxAmount,
            request.OtherCharges,
            request.DiscountAmount,
            request.Notes,
            request.AttachmentReference);

        invoice.Lines.Clear();
        AddLinesToInvoice(invoice, actor.OrganizationId, request.Lines);
        invoice.RecalculateTotals();

        await store.UpdateAsync(invoice, cancellationToken);
        AddAudit(actor, invoice, "PurchaseInvoice.UpdateDraft", new { invoice.SupplierInvoiceNumber, invoice.TotalAmount, LineCount = invoice.Lines.Count }, ipAddress);

        var updated = await store.FindAsync(invoice.Id, actor.OrganizationId, cancellationToken);
        return MapToResponse(updated!);
    }

    public async Task<PurchaseInvoiceResponse> PostAsync(
        ExpenseActor actor,
        Guid invoiceId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var invoice = await FindInvoiceOrThrowAsync(actor, invoiceId, cancellationToken);

        if (invoice.Status != PurchaseInvoiceStatus.Draft)
        {
            throw new ValidationException($"Only draft invoices can be posted. Invoice '{invoiceId}' is currently in '{invoice.Status}' status.");
        }

        if (invoice.Lines.Count == 0)
        {
            throw new ValidationException("An invoice must contain at least one line item before posting.");
        }

        if (await store.InvoiceNumberExistsAsync(actor.OrganizationId, invoice.SupplierId, invoice.SupplierInvoiceNumber, invoice.Id, cancellationToken))
        {
            throw new ValidationException($"An active supplier invoice with number '{invoice.SupplierInvoiceNumber}' already exists for this supplier.");
        }

        invoice.Post(actor.UserId);
        await store.UpdateAsync(invoice, cancellationToken);
        AddAudit(actor, invoice, "PurchaseInvoice.Post", new { invoice.SupplierInvoiceNumber, invoice.TotalAmount, Status = invoice.Status.ToString() }, ipAddress);

        var posted = await store.FindAsync(invoice.Id, actor.OrganizationId, cancellationToken);
        return MapToResponse(posted!);
    }

    public async Task<PurchaseInvoiceResponse> ReverseAsync(
        ExpenseActor actor,
        Guid invoiceId,
        ReversePurchaseInvoiceRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var invoice = await FindInvoiceOrThrowAsync(actor, invoiceId, cancellationToken);

        if (invoice.Status != PurchaseInvoiceStatus.Posted)
        {
            throw new ValidationException($"Only posted invoices can be reversed. Invoice '{invoiceId}' is currently in '{invoice.Status}' status.");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ValidationException("A reversal reason is required.");
        }

        if (invoice.ReceiptLines.Any(rl => rl.StockMovement != null && !rl.StockMovement.IsReversed))
        {
            throw new ValidationException("Cannot reverse a purchase invoice that has active, non-reversed stock receipts. Reverse the linked stock movements first.");
        }

        invoice.Reverse(request.Reason, actor.UserId);
        await store.UpdateAsync(invoice, cancellationToken);
        AddAudit(actor, invoice, "PurchaseInvoice.Reverse", new { invoice.SupplierInvoiceNumber, Reason = request.Reason.Trim(), Status = invoice.Status.ToString() }, ipAddress);

        var reversed = await store.FindAsync(invoice.Id, actor.OrganizationId, cancellationToken);
        return MapToResponse(reversed!);
    }

    public async Task<PurchaseInvoiceReceiptSummaryResponse> GetReceiptSummaryAsync(
        ExpenseActor actor,
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var invoice = await FindInvoiceOrThrowAsync(actor, invoiceId, cancellationToken);

        var inventoryLines = invoice.Lines.Where(l => l.LineType == InvoiceLineType.InventoryItem).ToList();
        var hasInventoryLines = inventoryLines.Count > 0;
        var activeReceiptLines = invoice.ReceiptLines.Where(rl => rl.StockMovement != null && !rl.StockMovement.IsReversed).ToList();

        var totalInvoicedQuantity = inventoryLines.Sum(l => l.Quantity ?? 0m);
        var totalReceivedQuantity = activeReceiptLines.Sum(rl => rl.ReceivedQuantity);
        var totalRemainingQuantity = Math.Max(0m, totalInvoicedQuantity - totalReceivedQuantity);

        int fullyReceivedCount = 0;
        int partiallyReceivedCount = 0;
        int unreceivedCount = 0;

        foreach (var line in inventoryLines)
        {
            var lineReceived = activeReceiptLines.Where(rl => rl.PurchaseInvoiceLineId == line.Id).Sum(rl => rl.ReceivedQuantity);
            var lineInvoiced = line.Quantity ?? 0m;

            if (lineReceived == 0m)
            {
                unreceivedCount++;
            }
            else if (lineReceived >= lineInvoiced)
            {
                fullyReceivedCount++;
            }
            else
            {
                partiallyReceivedCount++;
            }
        }

        var receiptStatus = !hasInventoryLines
            ? "NotApplicable"
            : totalReceivedQuantity == 0m
                ? "NotReceived"
                : totalReceivedQuantity >= totalInvoicedQuantity
                    ? "FullyReceived"
                    : "PartiallyReceived";

        var receiptsCount = invoice.ReceiptLines.Select(rl => rl.ReceiptGroupId).Distinct().Count();

        return new PurchaseInvoiceReceiptSummaryResponse(
            invoice.Id,
            invoice.SupplierInvoiceNumber,
            invoice.Supplier?.Name ?? "Unknown Supplier",
            invoice.FarmId,
            invoice.Farm?.Name ?? "Unknown Farm",
            receiptStatus,
            inventoryLines.Count,
            fullyReceivedCount,
            partiallyReceivedCount,
            unreceivedCount,
            totalInvoicedQuantity,
            totalReceivedQuantity,
            totalRemainingQuantity,
            receiptsCount);
    }

    public async Task<IReadOnlyList<PurchaseInvoiceRemainingLineResponse>> GetRemainingToReceiveAsync(
        ExpenseActor actor,
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var invoice = await FindInvoiceOrThrowAsync(actor, invoiceId, cancellationToken);

        var result = new List<PurchaseInvoiceRemainingLineResponse>();
        var inventoryLines = invoice.Lines
            .Where(l => l.LineType == InvoiceLineType.InventoryItem)
            .OrderBy(l => l.SortOrder)
            .ThenBy(l => l.Id);

        foreach (var line in inventoryLines)
        {
            var activeReceived = invoice.ReceiptLines
                .Where(rl => rl.PurchaseInvoiceLineId == line.Id && rl.StockMovement != null && !rl.StockMovement.IsReversed)
                .Sum(rl => rl.ReceivedQuantity);
            var invoiced = line.Quantity ?? 0m;
            var remaining = Math.Max(0m, invoiced - activeReceived);
            var isEligible = invoice.Status == PurchaseInvoiceStatus.Posted && remaining > 0m;

            result.Add(new PurchaseInvoiceRemainingLineResponse(
                line.Id,
                line.InventoryItemId!.Value,
                line.InventoryItem?.Name ?? "Unknown Item",
                line.InventoryItem?.Sku,
                line.StockUnitId!.Value,
                line.StockUnit?.Code ?? "",
                line.StockUnit?.Name ?? "",
                invoiced,
                activeReceived,
                remaining,
                line.UnitPrice,
                line.FarmAreaId,
                line.FarmArea?.Name,
                line.PlantationId,
                line.Plantation?.PlantationName,
                line.CropCycleId,
                line.CropCycle?.CycleName,
                line.CropCycleStageId,
                line.CropCycleStage?.StageName,
                isEligible));
        }

        return result;
    }

    public async Task<IReadOnlyList<PurchaseInvoiceReceiptGroupResponse>> GetReceiptHistoryAsync(
        ExpenseActor actor,
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        await FindInvoiceOrThrowAsync(actor, invoiceId, cancellationToken);

        var receiptLines = await store.GetReceiptLinesByInvoiceAsync(invoiceId, actor.OrganizationId, cancellationToken);
        if (receiptLines.Count == 0)
        {
            return Array.Empty<PurchaseInvoiceReceiptGroupResponse>();
        }

        var grouped = receiptLines
            .GroupBy(rl => rl.ReceiptGroupId)
            .OrderByDescending(g => g.First().CreatedAt);

        var response = new List<PurchaseInvoiceReceiptGroupResponse>();
        foreach (var group in grouped)
        {
            var first = group.First();
            var items = group.Select(rl => new PurchaseInvoiceReceiptItemResponse(
                rl.Id,
                rl.PurchaseInvoiceLineId,
                rl.StockMovementId,
                rl.PurchaseInvoiceLine?.InventoryItemId ?? Guid.Empty,
                rl.PurchaseInvoiceLine?.InventoryItem?.Name ?? "Unknown Item",
                rl.PurchaseInvoiceLine?.InventoryItem?.Sku,
                rl.ReceivedQuantity,
                rl.PurchaseInvoiceLine?.StockUnit?.Code ?? "",
                rl.StockMovement?.IsReversed ?? false,
                rl.StockMovement?.ReversalReason,
                rl.StockMovement?.IsReversed == true ? rl.StockMovement.CreatedAt : null)).ToList();

            response.Add(new PurchaseInvoiceReceiptGroupResponse(
                first.ReceiptGroupId,
                first.PurchaseInvoiceId,
                first.StockMovement?.MovementDate ?? DateOnly.FromDateTime(first.CreatedAt.DateTime),
                first.StockMovement?.ReferenceNumber,
                first.StockMovement?.Notes,
                first.StockMovement?.StorageLocationId ?? Guid.Empty,
                first.StockMovement?.StorageLocation?.Name ?? "Unknown Location",
                first.CreatedAt,
                first.CreatedBy,
                first.IdempotencyKey,
                items));
        }

        return response;
    }

    public async Task<PurchaseInvoiceReceiptGroupResponse> ReceiveItemsAsync(
        ExpenseActor actor,
        Guid invoiceId,
        ReceivePurchaseInvoiceItemsRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var invoice = await FindInvoiceOrThrowAsync(actor, invoiceId, cancellationToken);

        if (invoice.Status != PurchaseInvoiceStatus.Posted)
        {
            throw new ValidationException($"Items can only be received for posted invoices. Invoice '{invoiceId}' is currently in '{invoice.Status}' status.");
        }

        if (request.StorageLocationId == Guid.Empty || !await store.StorageLocationBelongsToFarmAndActiveAsync(request.StorageLocationId, invoice.FarmId, actor.OrganizationId, cancellationToken))
        {
            throw new ValidationException($"Storage location '{request.StorageLocationId}' does not exist, is inactive, or does not belong to the invoice's farm.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (request.MovementDate > today)
        {
            throw new ValidationException("Receipt date cannot be in the future.");
        }

        if (request.Lines == null || request.Lines.Count == 0)
        {
            throw new ValidationException("At least one line item must be specified for receiving.");
        }

        // Check Idempotency Key
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existingLines = await store.FindReceiptGroupByInvoiceAndIdempotencyKeyAsync(invoiceId, actor.OrganizationId, request.IdempotencyKey, cancellationToken);
            if (existingLines.Count > 0)
            {
                var history = await GetReceiptHistoryAsync(actor, invoiceId, cancellationToken);
                var existingGroup = history.FirstOrDefault(g => g.ReceiptGroupId == existingLines[0].ReceiptGroupId);
                if (existingGroup != null)
                {
                    return existingGroup;
                }
            }
        }

        // Validate each item request line and calculate quantities
        foreach (var item in request.Lines)
        {
            var line = invoice.Lines.FirstOrDefault(l => l.Id == item.PurchaseInvoiceLineId);
            if (line == null)
            {
                throw new ValidationException($"Purchase invoice line '{item.PurchaseInvoiceLineId}' was not found on this invoice.");
            }

            if (line.LineType != InvoiceLineType.InventoryItem)
            {
                throw new ValidationException($"Line #{line.SortOrder} is a non-inventory expense line and cannot be received into stock.");
            }

            if (item.Quantity <= 0m)
            {
                throw new ValidationException($"Quantity received must be greater than zero for line #{line.SortOrder}.");
            }

            var activeReceived = invoice.ReceiptLines
                .Where(rl => rl.PurchaseInvoiceLineId == line.Id && rl.StockMovement != null && !rl.StockMovement.IsReversed)
                .Sum(rl => rl.ReceivedQuantity);

            var remainingToReceive = Math.Max(0m, (line.Quantity ?? 0m) - activeReceived);

            if (item.Quantity > remainingToReceive)
            {
                throw new ValidationException($"Cannot receive {item.Quantity:N2} for line #{line.SortOrder}. Maximum remaining quantity to receive is {remainingToReceive:N2}.");
            }
        }

        // Perform stock updates and receipt creation atomically in a database transaction
        var receiptGroupId = Guid.NewGuid();
        await stockStore.ExecuteInTransactionAsync(async ct =>
        {
            var linesToSave = new List<PurchaseInvoiceReceiptLine>();

            foreach (var item in request.Lines)
            {
                var line = invoice.Lines.First(l => l.Id == item.PurchaseInvoiceLineId);
                var itemId = line.InventoryItemId!.Value;

                // Acquire PostgreSQL advisory lock for storage location + inventory item
                await stockStore.AcquireAdvisoryLockAsync(request.StorageLocationId, itemId, ct);

                // Lock stock balance row for update
                var balance = await stockStore.LockBalanceAsync(request.StorageLocationId, itemId, actor.OrganizationId, ct);
                if (balance == null)
                {
                    balance = new StockBalance(actor.OrganizationId, invoice.FarmId, request.StorageLocationId, itemId, item.Quantity);
                    stockStore.AddBalance(balance);
                }
                else
                {
                    balance.AddStock(item.Quantity, DateTimeOffset.UtcNow, actor.UserId);
                }

                // Create Phase 3.5 StockMovement record
                var movement = new StockMovement(
                    organizationId: actor.OrganizationId,
                    movementType: StockMovementType.Receipt,
                    inventoryItemId: itemId,
                    farmId: invoice.FarmId,
                    storageLocationId: request.StorageLocationId,
                    quantity: item.Quantity,
                    stockUnitId: line.StockUnitId!.Value,
                    movementDate: request.MovementDate,
                    createdBy: actor.UserId,
                    referenceNumber: request.ReferenceNumber,
                    notes: request.Notes,
                    parentTransactionId: null,
                    cropCycleId: line.CropCycleId,
                    cropCycleStageId: line.CropCycleStageId,
                    plantationId: line.PlantationId,
                    farmAreaId: line.FarmAreaId,
                    laborActivityId: null);

                stockStore.AddMovement(movement);

                // Create Phase 3.6 PurchaseInvoiceReceiptLine linking invoice line to stock movement
                var receiptLine = PurchaseInvoiceReceiptLine.Create(
                    organizationId: actor.OrganizationId,
                    purchaseInvoiceId: invoice.Id,
                    purchaseInvoiceLineId: line.Id,
                    stockMovementId: movement.Id,
                    receivedQuantity: item.Quantity,
                    createdBy: actor.UserId,
                    receiptGroupId: receiptGroupId,
                    idempotencyKey: request.IdempotencyKey);

                linesToSave.Add(receiptLine);
            }

            await stockStore.SaveChangesAsync(ct);
            await store.AddReceiptLinesAsync(linesToSave, ct);

            AddAudit(actor, invoice, "PurchaseInvoice.ReceiveItems", new
            {
                ReceiptGroupId = receiptGroupId,
                request.StorageLocationId,
                request.MovementDate,
                request.ReferenceNumber,
                LineCount = request.Lines.Count
            }, ipAddress);

            return true;
        }, cancellationToken);

        var historyList = await GetReceiptHistoryAsync(actor, invoiceId, cancellationToken);
        return historyList.First(g => g.ReceiptGroupId == receiptGroupId);
    }

    private async Task<PurchaseInvoice> FindInvoiceOrThrowAsync(
        ExpenseActor actor,
        Guid invoiceId,
        CancellationToken cancellationToken)
    {
        var invoice = await store.FindAsync(invoiceId, actor.OrganizationId, cancellationToken);
        if (invoice == null)
        {
            throw new ResourceNotFoundException($"Purchase invoice '{invoiceId}' was not found.");
        }

        return invoice;
    }

    private async Task ValidateHeaderDataAsync(
        Guid organizationId,
        Guid supplierId,
        Guid farmId,
        Guid currencyId,
        string supplierInvoiceNumber,
        Guid? excludeInvoiceId,
        DateOnly invoiceDate,
        DateOnly? dueDate,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (invoiceDate > today)
        {
            throw new ValidationException("Invoice date cannot be in the future.");
        }

        if (dueDate.HasValue && dueDate.Value < invoiceDate)
        {
            throw new ValidationException("Due date cannot be earlier than invoice date.");
        }

        if (!await store.FarmBelongsToOrganizationAsync(farmId, organizationId, cancellationToken))
        {
            throw new ValidationException($"Farm with ID '{farmId}' does not exist or does not belong to the active organization.");
        }

        if (!await store.SupplierBelongsToOrganizationAndActiveAsync(supplierId, organizationId, cancellationToken))
        {
            throw new ValidationException($"Supplier with ID '{supplierId}' does not exist, is inactive, or does not belong to the active organization.");
        }

        if (!await store.CurrencyExistsAndActiveAsync(currencyId, cancellationToken))
        {
            throw new ValidationException($"Currency with ID '{currencyId}' does not exist or is inactive.");
        }

        if (await store.InvoiceNumberExistsAsync(organizationId, supplierId, supplierInvoiceNumber, excludeInvoiceId, cancellationToken))
        {
            throw new ValidationException($"An active supplier invoice with number '{supplierInvoiceNumber.Trim()}' already exists for this supplier.");
        }
    }

    private async Task ValidateLinesDataAsync(
        Guid organizationId,
        Guid farmId,
        IReadOnlyList<CreatePurchaseInvoiceLineRequest> lines,
        CancellationToken cancellationToken)
    {
        if (lines == null || lines.Count == 0)
        {
            return;
        }

        foreach (var (line, index) in lines.Select((l, i) => (l, i)))
        {
            var lineNum = index + 1;
            var isInventory = string.Equals(line.LineType, "InventoryItem", StringComparison.OrdinalIgnoreCase);
            var isNonInventory = string.Equals(line.LineType, "NonInventoryExpense", StringComparison.OrdinalIgnoreCase);

            if (!isInventory && !isNonInventory)
            {
                throw new ValidationException($"Line #{lineNum}: Invalid line type '{line.LineType}'. Must be 'InventoryItem' or 'NonInventoryExpense'.");
            }

            if (isInventory)
            {
                if (!line.InventoryItemId.HasValue || line.InventoryItemId.Value == Guid.Empty)
                {
                    throw new ValidationException($"Line #{lineNum}: An inventory item is required for inventory lines.");
                }

                if (!line.StockUnitId.HasValue || line.StockUnitId.Value == Guid.Empty)
                {
                    throw new ValidationException($"Line #{lineNum}: A stock unit is required for inventory lines.");
                }

                if (!line.Quantity.HasValue || line.Quantity.Value <= 0m)
                {
                    throw new ValidationException($"Line #{lineNum}: Quantity must be greater than zero.");
                }

                if (!line.UnitPrice.HasValue || line.UnitPrice.Value < 0m)
                {
                    throw new ValidationException($"Line #{lineNum}: Unit price cannot be negative.");
                }

                if (!await store.InventoryItemBelongsToOrganizationAndActiveAsync(line.InventoryItemId.Value, organizationId, cancellationToken))
                {
                    throw new ValidationException($"Line #{lineNum}: Inventory item does not exist, is inactive, or does not belong to the organization.");
                }

                if (!await store.StockUnitExistsAndActiveAsync(line.StockUnitId.Value, cancellationToken))
                {
                    throw new ValidationException($"Line #{lineNum}: Stock unit does not exist or is inactive.");
                }
            }
            else // NonInventoryExpense
            {
                if (!line.ExpenseCategoryId.HasValue || line.ExpenseCategoryId.Value == Guid.Empty)
                {
                    throw new ValidationException($"Line #{lineNum}: An expense category is required for non-inventory lines.");
                }

                if (!line.Amount.HasValue || line.Amount.Value <= 0m)
                {
                    throw new ValidationException($"Line #{lineNum}: Amount must be greater than zero.");
                }

                if (!await store.CategoryExistsAndActiveAsync(line.ExpenseCategoryId.Value, organizationId, cancellationToken))
                {
                    throw new ValidationException($"Line #{lineNum}: Expense category does not exist, is inactive, or does not belong to the organization.");
                }
            }

            // Operational linkages validation
            if (line.FarmAreaId.HasValue && line.FarmAreaId.Value != Guid.Empty)
            {
                if (!await store.AreaBelongsToFarmAsync(line.FarmAreaId.Value, farmId, organizationId, cancellationToken))
                {
                    throw new ValidationException($"Line #{lineNum}: Selected farm area does not belong to the designated farm.");
                }
            }

            if (line.PlantationId.HasValue && line.PlantationId.Value != Guid.Empty)
            {
                if (!await store.PlantationBelongsToFarmAsync(line.PlantationId.Value, farmId, organizationId, cancellationToken))
                {
                    throw new ValidationException($"Line #{lineNum}: Selected plantation does not belong to the designated farm.");
                }
            }

            if (line.CropCycleId.HasValue && line.CropCycleId.Value != Guid.Empty)
            {
                if (!await store.CycleBelongsToFarmAsync(line.CropCycleId.Value, farmId, organizationId, cancellationToken))
                {
                    throw new ValidationException($"Line #{lineNum}: Selected crop cycle does not belong to the designated farm.");
                }
            }

            if (line.CropCycleStageId.HasValue && line.CropCycleStageId.Value != Guid.Empty)
            {
                if (!line.CropCycleId.HasValue || line.CropCycleId.Value == Guid.Empty)
                {
                    throw new ValidationException($"Line #{lineNum}: Crop cycle stage requires a crop cycle to be selected.");
                }

                if (!await store.StageBelongsToCropCycleAsync(line.CropCycleStageId.Value, line.CropCycleId.Value, cancellationToken))
                {
                    throw new ValidationException($"Line #{lineNum}: Selected crop cycle stage does not belong to the designated crop cycle.");
                }
            }
        }
    }

    private static void AddLinesToInvoice(
        PurchaseInvoice invoice,
        Guid organizationId,
        IReadOnlyList<CreatePurchaseInvoiceLineRequest> requestLines)
    {
        if (requestLines == null || requestLines.Count == 0) return;

        foreach (var lineReq in requestLines)
        {
            if (string.Equals(lineReq.LineType, "InventoryItem", StringComparison.OrdinalIgnoreCase))
            {
                var inventoryLine = PurchaseInvoiceLine.CreateInventoryLine(
                    organizationId,
                    invoice.Id,
                    lineReq.InventoryItemId!.Value,
                    lineReq.StockUnitId!.Value,
                    lineReq.Quantity!.Value,
                    lineReq.UnitPrice!.Value,
                    lineReq.Description,
                    lineReq.FarmAreaId,
                    lineReq.PlantationId,
                    lineReq.CropCycleId,
                    lineReq.CropCycleStageId,
                    lineReq.SortOrder);

                invoice.Lines.Add(inventoryLine);
            }
            else
            {
                var nonInventoryLine = PurchaseInvoiceLine.CreateNonInventoryLine(
                    organizationId,
                    invoice.Id,
                    lineReq.ExpenseCategoryId!.Value,
                    lineReq.Amount!.Value,
                    lineReq.Description,
                    lineReq.FarmAreaId,
                    lineReq.PlantationId,
                    lineReq.CropCycleId,
                    lineReq.CropCycleStageId,
                    lineReq.SortOrder);

                invoice.Lines.Add(nonInventoryLine);
            }
        }
    }

    private void AddAudit(ExpenseActor actor, PurchaseInvoice invoice, string action, object details, string? ipAddress)
    {
        store.AddAuditLogAsync(new AuditLog(
            action,
            actor.OrganizationId,
            actor.UserId,
            nameof(PurchaseInvoice),
            invoice.Id,
            JsonSerializer.SerializeToDocument(details),
            ipAddress));
    }

    private static void ValidateActor(ExpenseActor actor)
    {
        if (actor is null || actor.UserId == Guid.Empty || actor.OrganizationId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("The authenticated user context is invalid.");
        }
    }

    private static PurchaseInvoiceResponse MapToResponse(PurchaseInvoice invoice)
    {
        var amountPaid = 0m; // Payments implemented in Phase 6
        var outstandingBalance = Math.Max(0m, invoice.TotalAmount - amountPaid);

        // Derive Payment Status
        var paymentStatus = invoice.Status switch
        {
            PurchaseInvoiceStatus.Draft => "Draft",
            PurchaseInvoiceStatus.Reversed => "Draft",
            _ => outstandingBalance == 0m ? "Paid" : amountPaid > 0m ? "PartiallyPaid" : "Unpaid"
        };

        // Derive Due Status
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var dueStatus = "None";
        if (invoice.Status == PurchaseInvoiceStatus.Posted && outstandingBalance > 0m && invoice.DueDate.HasValue)
        {
            if (invoice.DueDate.Value < today) dueStatus = "Overdue";
            else if (invoice.DueDate.Value == today) dueStatus = "DueToday";
            else dueStatus = "Upcoming";
        }

        // Derive Receipt Status (Phase 5)
        var hasInventoryLines = invoice.Lines.Any(l => l.LineType == InvoiceLineType.InventoryItem);
        string receiptStatus;
        if (!hasInventoryLines)
        {
            receiptStatus = "NotApplicable";
        }
        else
        {
            var totalOrdered = invoice.Lines
                .Where(l => l.LineType == InvoiceLineType.InventoryItem)
                .Sum(l => l.Quantity ?? 0m);
            var totalReceived = invoice.ReceiptLines
                .Where(rl => rl.StockMovement != null && !rl.StockMovement.IsReversed)
                .Sum(rl => rl.ReceivedQuantity);

            if (totalReceived == 0m)
            {
                receiptStatus = "NotReceived";
            }
            else if (totalReceived >= totalOrdered)
            {
                receiptStatus = "FullyReceived";
            }
            else
            {
                receiptStatus = "PartiallyReceived";
            }
        }

        var lineDtos = invoice.Lines
            .OrderBy(l => l.SortOrder)
            .ThenBy(l => l.Id)
            .Select(l => new PurchaseInvoiceLineResponse(
                l.Id,
                l.PurchaseInvoiceId,
                l.LineType == InvoiceLineType.InventoryItem ? "InventoryItem" : "NonInventoryExpense",
                l.InventoryItemId,
                l.InventoryItem?.Name,
                l.InventoryItem?.Sku,
                l.ExpenseCategoryId,
                l.ExpenseCategory?.Name,
                l.Description,
                l.Quantity,
                l.StockUnitId,
                l.StockUnit?.Code,
                l.StockUnit?.Name,
                l.UnitPrice,
                l.LineAmount,
                l.FarmAreaId,
                l.FarmArea?.Name,
                l.PlantationId,
                l.Plantation?.PlantationName,
                l.CropCycleId,
                l.CropCycle?.CycleName,
                l.CropCycleStageId,
                l.CropCycleStage?.StageName,
                l.SortOrder))
            .ToList();

        return new PurchaseInvoiceResponse(
            invoice.Id,
            invoice.OrganizationId,
            invoice.SupplierId,
            invoice.Supplier?.Name ?? "Unknown Supplier",
            invoice.FarmId,
            invoice.Farm?.Name ?? "Unknown Farm",
            invoice.SupplierInvoiceNumber,
            invoice.InvoiceDate,
            invoice.DueDate,
            invoice.CurrencyId,
            invoice.Currency?.Code ?? "INR",
            invoice.Currency?.Symbol ?? "₹",
            invoice.PaymentTerms,
            invoice.Subtotal,
            invoice.TaxAmount,
            invoice.OtherCharges,
            invoice.DiscountAmount,
            invoice.TotalAmount,
            amountPaid,
            outstandingBalance,
            invoice.Status.ToString(),
            paymentStatus,
            dueStatus,
            receiptStatus,
            invoice.Notes,
            invoice.AttachmentReference,
            invoice.PostedAt,
            invoice.PostedBy,
            invoice.ReversedAt,
            invoice.ReversedBy,
            invoice.ReversalReason,
            invoice.CreatedAt,
            invoice.CreatedBy,
            invoice.UpdatedAt,
            lineDtos);
    }
}
