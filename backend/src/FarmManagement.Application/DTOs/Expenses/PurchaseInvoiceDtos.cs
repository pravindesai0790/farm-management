namespace FarmManagement.Application.DTOs.Expenses;

public sealed record PurchaseInvoiceResponse(
    Guid Id,
    Guid OrganizationId,
    Guid SupplierId,
    string SupplierName,
    Guid FarmId,
    string FarmName,
    string SupplierInvoiceNumber,
    DateOnly InvoiceDate,
    DateOnly? DueDate,
    Guid CurrencyId,
    string CurrencyCode,
    string CurrencySymbol,
    string? PaymentTerms,
    decimal Subtotal,
    decimal TaxAmount,
    decimal OtherCharges,
    decimal DiscountAmount,
    decimal TotalAmount,
    decimal AmountPaid,
    decimal OutstandingBalance,
    string Status,
    string PaymentStatus,
    string DueStatus,
    string ReceiptStatus,
    string? Notes,
    string? AttachmentReference,
    DateTimeOffset? PostedAt,
    Guid? PostedBy,
    DateTimeOffset? ReversedAt,
    Guid? ReversedBy,
    string? ReversalReason,
    DateTimeOffset CreatedAt,
    Guid CreatedBy,
    DateTimeOffset? UpdatedAt,
    IReadOnlyList<PurchaseInvoiceLineResponse> Lines,
    IReadOnlyList<InvoicePaymentAllocationSummaryResponse>? PaymentAllocations = null);

public sealed record PurchaseInvoiceLineResponse(
    Guid Id,
    Guid PurchaseInvoiceId,
    string LineType,
    Guid? InventoryItemId,
    string? InventoryItemName,
    string? InventoryItemSku,
    Guid? ExpenseCategoryId,
    string? ExpenseCategoryName,
    string? Description,
    decimal? Quantity,
    Guid? StockUnitId,
    string? StockUnitCode,
    string? StockUnitName,
    decimal UnitPrice,
    decimal LineAmount,
    Guid? FarmAreaId,
    string? FarmAreaName,
    Guid? PlantationId,
    string? PlantationName,
    Guid? CropCycleId,
    string? CropCycleName,
    Guid? CropCycleStageId,
    string? CropCycleStageName,
    int SortOrder);

public sealed record CreatePurchaseInvoiceRequest(
    Guid SupplierId,
    Guid FarmId,
    string SupplierInvoiceNumber,
    DateOnly InvoiceDate,
    DateOnly? DueDate,
    Guid CurrencyId,
    string? PaymentTerms,
    decimal TaxAmount,
    decimal OtherCharges,
    decimal DiscountAmount,
    string? Notes,
    string? AttachmentReference,
    IReadOnlyList<CreatePurchaseInvoiceLineRequest> Lines);

public sealed record CreatePurchaseInvoiceLineRequest(
    string LineType,
    Guid? InventoryItemId,
    Guid? StockUnitId,
    decimal? Quantity,
    decimal? UnitPrice,
    Guid? ExpenseCategoryId,
    decimal? Amount,
    string? Description,
    Guid? FarmAreaId,
    Guid? PlantationId,
    Guid? CropCycleId,
    Guid? CropCycleStageId,
    int SortOrder = 0);

public sealed record UpdatePurchaseInvoiceRequest(
    Guid SupplierId,
    Guid FarmId,
    string SupplierInvoiceNumber,
    DateOnly InvoiceDate,
    DateOnly? DueDate,
    Guid CurrencyId,
    string? PaymentTerms,
    decimal TaxAmount,
    decimal OtherCharges,
    decimal DiscountAmount,
    string? Notes,
    string? AttachmentReference,
    IReadOnlyList<CreatePurchaseInvoiceLineRequest> Lines);

public sealed record ReversePurchaseInvoiceRequest(
    string Reason);

public sealed record PurchaseInvoiceFilter(
    string? Search = null,
    Guid? SupplierId = null,
    Guid? FarmId = null,
    string? Status = null,
    DateOnly? From = null,
    DateOnly? To = null,
    string? PaymentStatus = null,
    string? DueStatus = null,
    string? ReceiptStatus = null);

// --- Phase 5 Receipt DTOs ---

/// <summary>
/// Payload to receive inventory items from a posted purchase invoice into a farm storage location.
/// Supports partial delivery across one or more inventory lines.
/// </summary>
public sealed record ReceivePurchaseInvoiceItemsRequest(
    Guid StorageLocationId,
    DateOnly MovementDate,
    IReadOnlyList<ReceivePurchaseInvoiceItemLineRequest> Lines,
    string? ReferenceNumber = null,
    string? Notes = null,
    string? IdempotencyKey = null);

/// <summary>
/// Individual line item quantity payload for a receipt delivery.
/// </summary>
public sealed record ReceivePurchaseInvoiceItemLineRequest(
    Guid PurchaseInvoiceLineId,
    decimal Quantity);

/// <summary>
/// High-level summary of receipt progress for a purchase invoice.
/// </summary>
public sealed record PurchaseInvoiceReceiptSummaryResponse(
    Guid InvoiceId,
    string SupplierInvoiceNumber,
    string SupplierName,
    Guid FarmId,
    string FarmName,
    string ReceiptStatus,
    int TotalInventoryLinesCount,
    int FullyReceivedLinesCount,
    int PartiallyReceivedLinesCount,
    int UnreceivedLinesCount,
    decimal TotalInvoicedQuantity,
    decimal TotalReceivedQuantity,
    decimal TotalRemainingQuantity,
    int ReceiptsCount);

/// <summary>
/// Line-level details showing invoiced, received, and remaining quantities eligible to be received.
/// </summary>
public sealed record PurchaseInvoiceRemainingLineResponse(
    Guid PurchaseInvoiceLineId,
    Guid InventoryItemId,
    string InventoryItemName,
    string? InventoryItemSku,
    Guid StockUnitId,
    string StockUnitCode,
    string StockUnitName,
    decimal InvoicedQuantity,
    decimal ReceivedQuantity,
    decimal RemainingQuantity,
    decimal UnitPrice,
    Guid? FarmAreaId,
    string? FarmAreaName,
    Guid? PlantationId,
    string? PlantationName,
    Guid? CropCycleId,
    string? CropCycleName,
    Guid? CropCycleStageId,
    string? CropCycleStageName,
    bool IsEligibleForReceipt);

/// <summary>
/// Grouped delivery event response containing all items received together in a single batch.
/// </summary>
public sealed record PurchaseInvoiceReceiptGroupResponse(
    Guid ReceiptGroupId,
    Guid PurchaseInvoiceId,
    DateOnly MovementDate,
    string? ReferenceNumber,
    string? Notes,
    Guid StorageLocationId,
    string StorageLocationName,
    DateTimeOffset CreatedAt,
    Guid CreatedBy,
    string? IdempotencyKey,
    IReadOnlyList<PurchaseInvoiceReceiptItemResponse> Items);

/// <summary>
/// Item-level receipt record linked to an inventory stock movement.
/// </summary>
public sealed record PurchaseInvoiceReceiptItemResponse(
    Guid ReceiptLineId,
    Guid PurchaseInvoiceLineId,
    Guid StockMovementId,
    Guid InventoryItemId,
    string InventoryItemName,
    string? InventoryItemSku,
    decimal ReceivedQuantity,
    string StockUnitCode,
    bool IsReversed,
    string? ReversalReason,
    DateTimeOffset? ReversedAt);
