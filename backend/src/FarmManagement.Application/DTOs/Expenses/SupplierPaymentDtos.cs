using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.DTOs.Expenses;

public sealed record RecordSupplierPaymentRequest(
    Guid SupplierId,
    DateOnly PaymentDate,
    decimal Amount,
    Guid CurrencyId,
    PaymentMethod PaymentMethod,
    IReadOnlyList<SupplierPaymentAllocationRequest> Allocations,
    string? ReferenceNumber = null,
    string? Notes = null,
    string? IdempotencyKey = null);

public sealed record SupplierPaymentAllocationRequest(
    Guid PurchaseInvoiceId,
    decimal AllocatedAmount);

public sealed record ReverseSupplierPaymentRequest(
    string Reason);

public sealed record SupplierPaymentFilter(
    DateOnly? From = null,
    DateOnly? To = null,
    Guid? SupplierId = null,
    Guid? InvoiceId = null,
    string? Status = null,
    string? Search = null);

public sealed record SupplierPaymentResponse(
    Guid Id,
    Guid OrganizationId,
    Guid SupplierId,
    string SupplierName,
    DateOnly PaymentDate,
    decimal Amount,
    Guid CurrencyId,
    string CurrencyCode,
    string CurrencySymbol,
    string PaymentMethod,
    string Status,
    string? ReferenceNumber,
    string? Notes,
    DateTimeOffset? ReversedAt,
    Guid? ReversedBy,
    string? ReversalReason,
    DateTimeOffset CreatedAt,
    Guid CreatedBy,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedBy,
    IReadOnlyList<SupplierPaymentAllocationResponse> Allocations);

public sealed record SupplierPaymentAllocationResponse(
    Guid Id,
    Guid SupplierPaymentId,
    Guid PurchaseInvoiceId,
    string SupplierInvoiceNumber,
    DateOnly InvoiceDate,
    decimal InvoiceTotalAmount,
    decimal AllocatedAmount);

public sealed record UnpaidPurchaseInvoiceSummaryResponse(
    Guid Id,
    string SupplierInvoiceNumber,
    DateOnly InvoiceDate,
    DateOnly? DueDate,
    Guid CurrencyId,
    string CurrencyCode,
    string CurrencySymbol,
    decimal TotalAmount,
    decimal AmountPaid,
    decimal OutstandingBalance,
    string PaymentStatus,
    string DueStatus);

public sealed record InvoicePaymentAllocationSummaryResponse(
    Guid Id,
    Guid SupplierPaymentId,
    DateOnly PaymentDate,
    decimal PaymentTotalAmount,
    decimal AllocatedAmount,
    string PaymentMethod,
    string Status,
    string? ReferenceNumber);
