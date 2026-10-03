using System.Text.Json;
using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Expenses;
using FarmManagement.Application.Interfaces.Expenses;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.Services;

public sealed class SupplierPaymentService(
    ISupplierPaymentStore store,
    IPurchaseInvoiceStore invoiceStore) : ISupplierPaymentService
{
    public async Task<PagedResponse<SupplierPaymentResponse>> ListAsync(
        ExpenseActor actor,
        SupplierPaymentFilter filter,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);

        var normalizedPage = Math.Max(1, page);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
        var skip = (normalizedPage - 1) * normalizedPageSize;

        var totalCount = await store.CountAsync(actor.OrganizationId, filter, cancellationToken);
        var payments = await store.ListAsync(actor.OrganizationId, filter, skip, normalizedPageSize, cancellationToken);

        var responses = payments.Select(MapToResponse).ToList();
        return new PagedResponse<SupplierPaymentResponse>(responses, normalizedPage, normalizedPageSize, totalCount);
    }

    public async Task<SupplierPaymentResponse> GetByIdAsync(
        ExpenseActor actor,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var payment = await store.FindByIdWithDetailsAsync(id, actor.OrganizationId, cancellationToken);
        if (payment == null)
        {
            throw new KeyNotFoundException($"Supplier payment '{id}' was not found.");
        }

        return MapToResponse(payment);
    }

    public async Task<SupplierPaymentResponse> RecordPaymentAsync(
        ExpenseActor actor,
        RecordSupplierPaymentRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);

        // Check Idempotency Key
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existing = await store.FindByIdempotencyKeyAsync(actor.OrganizationId, request.IdempotencyKey, cancellationToken);
            if (existing != null)
            {
                return MapToResponse(existing);
            }
        }

        // Basic Header Validations
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (request.PaymentDate > today)
        {
            throw new ValidationException("Payment date cannot be in the future.");
        }

        if (request.Amount <= 0m)
        {
            throw new ValidationException("Payment amount must be greater than zero.");
        }

        if (!await invoiceStore.SupplierBelongsToOrganizationAndActiveAsync(request.SupplierId, actor.OrganizationId, cancellationToken))
        {
            throw new ValidationException($"Supplier '{request.SupplierId}' does not exist, is inactive, or does not belong to your organization.");
        }

        if (!await invoiceStore.CurrencyExistsAndActiveAsync(request.CurrencyId, cancellationToken))
        {
            throw new ValidationException($"Currency '{request.CurrencyId}' does not exist or is inactive.");
        }

        if (request.Allocations == null || request.Allocations.Count == 0)
        {
            throw new ValidationException("At least one invoice allocation must be specified for the payment.");
        }

        // Check duplicate invoice IDs in request
        var invoiceIds = request.Allocations.Select(a => a.PurchaseInvoiceId).ToList();
        if (invoiceIds.Count != invoiceIds.Distinct().Count())
        {
            throw new ValidationException("Duplicate invoice allocations specified in payment request.");
        }

        // Check sum of allocations equals payment amount
        var roundedAmount = Math.Round(request.Amount, 2, MidpointRounding.AwayFromZero);
        var totalAllocated = request.Allocations.Sum(a => Math.Round(a.AllocatedAmount, 2, MidpointRounding.AwayFromZero));
        if (totalAllocated != roundedAmount)
        {
            throw new ValidationException(
                $"Total allocated amount ({totalAllocated:N2}) must equal payment amount ({roundedAmount:N2}). Full allocation of payment is required.");
        }

        // Load target invoices
        var invoices = await invoiceStore.GetInvoicesWithAllocationsAsync(invoiceIds, actor.OrganizationId, cancellationToken);
        if (invoices.Count != invoiceIds.Count)
        {
            throw new ValidationException("One or more allocated purchase invoices were not found in your organization.");
        }

        // Validate each invoice allocation
        foreach (var allocReq in request.Allocations)
        {
            var invoice = invoices.First(i => i.Id == allocReq.PurchaseInvoiceId);

            if (invoice.SupplierId != request.SupplierId)
            {
                throw new ValidationException($"Invoice '{invoice.SupplierInvoiceNumber}' belongs to a different supplier.");
            }

            if (invoice.Status != PurchaseInvoiceStatus.Posted)
            {
                throw new ValidationException($"Invoice '{invoice.SupplierInvoiceNumber}' is in '{invoice.Status}' status. Payments can only be allocated to posted invoices.");
            }

            if (invoice.CurrencyId != request.CurrencyId)
            {
                throw new ValidationException($"Invoice '{invoice.SupplierInvoiceNumber}' currency does not match payment currency.");
            }

            var allocAmount = Math.Round(allocReq.AllocatedAmount, 2, MidpointRounding.AwayFromZero);
            if (allocAmount <= 0m)
            {
                throw new ValidationException($"Allocated amount for invoice '{invoice.SupplierInvoiceNumber}' must be greater than zero.");
            }

            var existingPaid = invoice.PaymentAllocations
                .Where(a => a.SupplierPayment != null && a.SupplierPayment.Status == SupplierPaymentStatus.Completed)
                .Sum(a => a.AllocatedAmount);

            var outstanding = Math.Max(0m, invoice.TotalAmount - existingPaid);
            if (allocAmount > outstanding)
            {
                throw new ValidationException(
                    $"Allocated amount ({allocAmount:N2}) exceeds outstanding balance ({outstanding:N2}) for invoice '{invoice.SupplierInvoiceNumber}'.");
            }
        }

        // Execute payment creation in transaction
        SupplierPayment? createdPayment = null;
        await store.ExecuteInTransactionAsync(async ct =>
        {
            var payment = SupplierPayment.Create(
                organizationId: actor.OrganizationId,
                supplierId: request.SupplierId,
                paymentDate: request.PaymentDate,
                amount: roundedAmount,
                currencyId: request.CurrencyId,
                paymentMethod: request.PaymentMethod,
                createdBy: actor.UserId,
                referenceNumber: request.ReferenceNumber,
                notes: request.Notes,
                idempotencyKey: request.IdempotencyKey);

            foreach (var allocReq in request.Allocations)
            {
                var alloc = SupplierPaymentAllocation.Create(
                    organizationId: actor.OrganizationId,
                    supplierPaymentId: payment.Id,
                    purchaseInvoiceId: allocReq.PurchaseInvoiceId,
                    allocatedAmount: Math.Round(allocReq.AllocatedAmount, 2, MidpointRounding.AwayFromZero),
                    createdBy: actor.UserId);

                payment.AddAllocation(alloc);
            }

            await store.AddPaymentWithAllocationsAsync(payment, payment.Allocations, ct);

            // Add Audit Log
            var auditDetails = new
            {
                payment.SupplierId,
                payment.PaymentDate,
                payment.Amount,
                payment.CurrencyId,
                PaymentMethod = payment.PaymentMethod.ToString(),
                AllocationCount = payment.Allocations.Count,
                payment.ReferenceNumber
            };

            await store.AddAuditLogAsync(
                new AuditLog(
                    "SupplierPayment.Create",
                    actor.OrganizationId,
                    actor.UserId,
                    nameof(SupplierPayment),
                    payment.Id,
                    JsonSerializer.SerializeToDocument(auditDetails),
                    ipAddress), ct);

            createdPayment = payment;
        }, cancellationToken);

        var result = await store.FindByIdWithDetailsAsync(createdPayment!.Id, actor.OrganizationId, cancellationToken);
        return MapToResponse(result!);
    }

    public async Task<SupplierPaymentResponse> ReversePaymentAsync(
        ExpenseActor actor,
        Guid id,
        ReverseSupplierPaymentRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);

        if (string.IsNullOrWhiteSpace(request?.Reason) || request.Reason.Trim().Length < 5)
        {
            throw new ArgumentException("A valid reversal reason of at least 5 characters is required.", nameof(request));
        }

        var payment = await store.FindByIdWithDetailsAsync(id, actor.OrganizationId, cancellationToken);
        if (payment == null)
        {
            throw new KeyNotFoundException($"Supplier payment '{id}' was not found.");
        }

        if (payment.Status != SupplierPaymentStatus.Completed)
        {
            throw new ValidationException($"Only completed payments can be reversed. Payment '{id}' is currently in '{payment.Status}' status.");
        }

        payment.Reverse(request.Reason.Trim(), actor.UserId);
        await store.UpdatePaymentAsync(payment, cancellationToken);

        var auditDetails = new
        {
            payment.Amount,
            payment.SupplierId,
            ReversalReason = payment.ReversalReason
        };

        await store.AddAuditLogAsync(
            new AuditLog(
                "SupplierPayment.Reverse",
                actor.OrganizationId,
                actor.UserId,
                nameof(SupplierPayment),
                payment.Id,
                JsonSerializer.SerializeToDocument(auditDetails),
                ipAddress), cancellationToken);

        return MapToResponse(payment);
    }

    public async Task<IReadOnlyList<UnpaidPurchaseInvoiceSummaryResponse>> GetUnpaidInvoicesAsync(
        ExpenseActor actor,
        Guid supplierId,
        Guid? currencyId = null,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);

        if (!await invoiceStore.SupplierBelongsToOrganizationAndActiveAsync(supplierId, actor.OrganizationId, cancellationToken))
        {
            throw new ValidationException($"Supplier '{supplierId}' does not exist or is inactive.");
        }

        var invoices = await invoiceStore.GetUnpaidInvoicesForSupplierAsync(supplierId, actor.OrganizationId, currencyId, cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return invoices.Select(inv =>
        {
            var amountPaid = inv.PaymentAllocations
                .Where(a => a.SupplierPayment != null && a.SupplierPayment.Status == SupplierPaymentStatus.Completed)
                .Sum(a => a.AllocatedAmount);

            var outstanding = Math.Max(0m, inv.TotalAmount - amountPaid);
            var paymentStatus = outstanding == 0m ? "Paid" : amountPaid > 0m ? "PartiallyPaid" : "Unpaid";

            var dueStatus = "None";
            if (outstanding > 0m && inv.DueDate.HasValue)
            {
                if (inv.DueDate.Value < today) dueStatus = "Overdue";
                else if (inv.DueDate.Value == today) dueStatus = "DueToday";
                else dueStatus = "Upcoming";
            }

            return new UnpaidPurchaseInvoiceSummaryResponse(
                inv.Id,
                inv.SupplierInvoiceNumber,
                inv.InvoiceDate,
                inv.DueDate,
                inv.CurrencyId,
                inv.Currency?.Code ?? "INR",
                inv.Currency?.Symbol ?? "₹",
                inv.TotalAmount,
                amountPaid,
                outstanding,
                paymentStatus,
                dueStatus);
        }).ToList();
    }

    private static void ValidateActor(ExpenseActor actor)
    {
        if (actor is null || actor.UserId == Guid.Empty || actor.OrganizationId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("The authenticated user context is invalid.");
        }
    }

    private static SupplierPaymentResponse MapToResponse(SupplierPayment payment)
    {
        var allocationDtos = payment.Allocations
            .Select(a => new SupplierPaymentAllocationResponse(
                a.Id,
                a.SupplierPaymentId,
                a.PurchaseInvoiceId,
                a.PurchaseInvoice?.SupplierInvoiceNumber ?? "Unknown",
                a.PurchaseInvoice?.InvoiceDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
                a.PurchaseInvoice?.TotalAmount ?? 0m,
                a.AllocatedAmount))
            .ToList();

        return new SupplierPaymentResponse(
            payment.Id,
            payment.OrganizationId,
            payment.SupplierId,
            payment.Supplier?.Name ?? "Unknown Supplier",
            payment.PaymentDate,
            payment.Amount,
            payment.CurrencyId,
            payment.Currency?.Code ?? "INR",
            payment.Currency?.Symbol ?? "₹",
            payment.PaymentMethod.ToString(),
            payment.Status.ToString(),
            payment.ReferenceNumber,
            payment.Notes,
            payment.ReversedAt,
            payment.ReversedBy,
            payment.ReversalReason,
            payment.CreatedAt,
            payment.CreatedBy,
            payment.UpdatedAt,
            payment.UpdatedBy,
            allocationDtos);
    }
}
