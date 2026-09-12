using System.Text.Json;
using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Labor;
using FarmManagement.Application.Interfaces.Labor;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.Services;

public sealed class WorkerPaymentService(
    IWorkerPaymentStore store,
    IWorkerEarningsLedgerStore earningsStore) : IWorkerPaymentService
{
    public async Task<WorkerPaymentResponse> RecordPaymentAsync(
        PaymentActor actor,
        RecordWorkerPaymentRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ArgumentNullException.ThrowIfNull(request);

        if (request.WorkerId == Guid.Empty)
        {
            throw new ValidationException("A worker is required.");
        }

        if (request.CurrencyId == Guid.Empty)
        {
            throw new ValidationException("A currency is required.");
        }

        if (request.Amount <= 0)
        {
            throw new ValidationException("The payment amount must be greater than zero.");
        }

        if (request.PaymentPeriodFrom.HasValue && request.PaymentPeriodTo.HasValue &&
            request.PaymentPeriodTo.Value < request.PaymentPeriodFrom.Value)
        {
            throw new ValidationException("The payment period to date cannot be earlier than from date.");
        }

        var worker = await store.FindWorkerAsync(request.WorkerId, actor.OrganizationId, cancellationToken);
        if (worker is null)
        {
            throw new ResourceNotFoundException("The worker was not found.");
        }

        var currency = await store.FindCurrencyAsync(request.CurrencyId, cancellationToken);
        if (currency is null || !currency.IsActive)
        {
            throw new ValidationException("The selected currency is invalid or inactive.");
        }

        var paymentType = ParsePaymentType(request.PaymentType);
        var paymentMethod = ParsePaymentMethod(request.PaymentMethod);
        var status = ParsePaymentStatus(request.Status);

        if (status == PaymentStatus.Cancelled)
        {
            throw new ValidationException("A payment cannot be created with cancelled status.");
        }

        var payment = WorkerPayment.Create(
            organizationId: actor.OrganizationId,
            workerId: request.WorkerId,
            paymentDate: request.PaymentDate,
            paymentType: paymentType,
            amount: request.Amount,
            currencyId: request.CurrencyId,
            paymentMethod: paymentMethod,
            createdBy: actor.UserId,
            referenceNumber: request.ReferenceNumber,
            paymentPeriodFrom: request.PaymentPeriodFrom,
            paymentPeriodTo: request.PaymentPeriodTo,
            status: status,
            notes: request.Notes);

        store.Add(payment);

        AddAudit(
            actor,
            payment,
            "WorkerPayment.Created",
            new
            {
                PaymentId = payment.Id,
                WorkerId = payment.WorkerId,
                PaymentType = FormatPaymentType(payment.PaymentType),
                Amount = payment.Amount,
                CurrencyId = payment.CurrencyId,
                PaymentMethod = FormatPaymentMethod(payment.PaymentMethod),
                PaymentDate = payment.PaymentDate,
                Status = FormatPaymentStatus(payment.Status)
            },
            ipAddress);

        await store.SaveChangesAsync(cancellationToken);

        return MapToResponse(payment, worker.DisplayName, currency.Code, currency.Symbol);
    }

    public async Task<WorkerPaymentResponse> CancelPaymentAsync(
        PaymentActor actor,
        Guid paymentId,
        CancelWorkerPaymentRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);

        var payment = await store.FindAsync(paymentId, actor.OrganizationId, cancellationToken);
        if (payment is null)
        {
            throw new ResourceNotFoundException("The payment was not found.");
        }

        if (payment.Status == PaymentStatus.Cancelled)
        {
            throw new ValidationException("The payment has already been cancelled.");
        }

        var now = DateTimeOffset.UtcNow;
        payment.Cancel(now, actor.UserId, request.Reason);

        AddAudit(
            actor,
            payment,
            "WorkerPayment.Cancelled",
            new
            {
                PaymentId = payment.Id,
                WorkerId = payment.WorkerId,
                Amount = payment.Amount,
                Reason = request.Reason,
                CancelledAt = now
            },
            ipAddress);

        await store.SaveChangesAsync(cancellationToken);

        return MapToResponse(
            payment,
            payment.Worker?.DisplayName ?? string.Empty,
            payment.Currency?.Code ?? "INR",
            payment.Currency?.Symbol ?? "₹");
    }

    public async Task<WorkerPaymentResponse> GetByIdAsync(
        PaymentActor actor,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);

        var payment = await store.FindAsync(id, actor.OrganizationId, cancellationToken);
        if (payment is null)
        {
            throw new ResourceNotFoundException("The payment was not found.");
        }

        return MapToResponse(
            payment,
            payment.Worker?.DisplayName ?? string.Empty,
            payment.Currency?.Code ?? "INR",
            payment.Currency?.Symbol ?? "₹");
    }

    public async Task<PagedResponse<WorkerPaymentResponse>> ListByWorkerAsync(
        PaymentActor actor,
        Guid workerId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        string? paymentType = null,
        string? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        if (fromDate.HasValue && toDate.HasValue && toDate.Value < fromDate.Value)
        {
            throw new ValidationException("The to date cannot be earlier than from date.");
        }

        PaymentType? parsedType = null;
        if (!string.IsNullOrWhiteSpace(paymentType))
        {
            parsedType = ParsePaymentType(paymentType);
        }

        PaymentStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            parsedStatus = ParsePaymentStatus(status);
        }

        var paged = await store.ListByWorkerAsync(
            actor.OrganizationId,
            workerId,
            fromDate,
            toDate,
            parsedType,
            parsedStatus,
            page,
            pageSize,
            cancellationToken);

        var items = paged.Items.Select(p => MapToResponse(
            p,
            p.Worker?.DisplayName ?? string.Empty,
            p.Currency?.Code ?? "INR",
            p.Currency?.Symbol ?? "₹")).ToList();

        return new PagedResponse<WorkerPaymentResponse>(items, paged.TotalCount, paged.Page, paged.PageSize);
    }

    public async Task<WorkerFinancialSummaryResponse> GetWorkerFinancialSummaryAsync(
        PaymentActor actor,
        Guid workerId,
        DateOnly? asOfDate = null,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);

        var worker = await store.FindWorkerAsync(workerId, actor.OrganizationId, cancellationToken);
        if (worker is null)
        {
            throw new ResourceNotFoundException("The worker was not found.");
        }

        var earnings = await earningsStore.ListForBalanceCalculationAsync(
            actor.OrganizationId,
            workerId,
            fromDate: null,
            toDate: asOfDate,
            cancellationToken);

        var approvedEarnings = earnings
            .Where(e => e.Status == EarningsLedgerStatus.Approved && e.EntryType == EarningsEntryType.Earning)
            .Sum(e => e.GrossAmount);

        var approvedAdjustments = earnings
            .Where(e => e.Status == EarningsLedgerStatus.Approved && e.EntryType == EarningsEntryType.Adjustment)
            .Sum(e => e.GrossAmount);

        var totalGrossEarnings = approvedEarnings + approvedAdjustments;

        var payments = await store.ListPaymentsForFinancialSummaryAsync(
            actor.OrganizationId,
            workerId,
            asOfDate,
            cancellationToken);

        var totalAdvances = payments
            .Where(p => p.Status == PaymentStatus.Completed && p.PaymentType == PaymentType.Advance)
            .Sum(p => p.Amount);

        var totalPayouts = payments
            .Where(p => p.Status == PaymentStatus.Completed && p.PaymentType == PaymentType.Payout)
            .Sum(p => p.Amount);

        var totalPaymentAdjustments = payments
            .Where(p => p.Status == PaymentStatus.Completed && p.PaymentType == PaymentType.Adjustment)
            .Sum(p => p.Amount);

        var completedCount = payments.Count(p => p.Status == PaymentStatus.Completed);
        var cancelledCount = payments.Count(p => p.Status == PaymentStatus.Cancelled);

        // Spec Section 21 & 22:
        // Outstanding = Gross Earned - Applicable Advances - Previous Payouts +/- Valid Adjustments
        var netOutstanding = totalGrossEarnings - totalAdvances - totalPayouts + totalPaymentAdjustments;

        // If netOutstanding < 0, worker has received more than earned -> carry forward balance
        var carryForwardAdvance = netOutstanding < 0 ? Math.Abs(netOutstanding) : 0m;

        var currency = payments.FirstOrDefault()?.Currency ?? earnings.FirstOrDefault()?.Currency;

        return new WorkerFinancialSummaryResponse(
            WorkerId: workerId,
            OrganizationId: actor.OrganizationId,
            WorkerDisplayName: worker.DisplayName,
            AsOfDate: asOfDate,
            GrossEarnings: totalGrossEarnings,
            TotalAdvances: totalAdvances,
            TotalPayouts: totalPayouts,
            TotalAdjustments: approvedAdjustments + totalPaymentAdjustments,
            NetOutstanding: netOutstanding,
            CarryForwardAdvanceBalance: carryForwardAdvance,
            CompletedPaymentCount: completedCount,
            CancelledPaymentCount: cancelledCount,
            CurrencyId: currency?.Id,
            CurrencyCode: currency?.Code ?? "INR",
            CurrencySymbol: currency?.Symbol ?? "₹");
    }

    private void AddAudit(
        PaymentActor actor,
        WorkerPayment payment,
        string action,
        object? details,
        string? ipAddress) =>
        store.AddAuditLog(new AuditLog(
            action: action,
            organizationId: actor.OrganizationId,
            userId: actor.UserId,
            entityType: nameof(WorkerPayment),
            entityId: payment.Id,
            details: details is null ? null : JsonSerializer.SerializeToDocument(details),
            ipAddress: ipAddress));

    private static WorkerPaymentResponse MapToResponse(
        WorkerPayment payment,
        string workerDisplayName,
        string currencyCode,
        string currencySymbol) =>
        new(
            Id: payment.Id,
            OrganizationId: payment.OrganizationId,
            WorkerId: payment.WorkerId,
            WorkerDisplayName: workerDisplayName,
            PaymentDate: payment.PaymentDate,
            PaymentType: FormatPaymentType(payment.PaymentType),
            Amount: payment.Amount,
            CurrencyId: payment.CurrencyId,
            CurrencyCode: currencyCode,
            CurrencySymbol: currencySymbol,
            PaymentMethod: FormatPaymentMethod(payment.PaymentMethod),
            ReferenceNumber: payment.ReferenceNumber,
            PaymentPeriodFrom: payment.PaymentPeriodFrom,
            PaymentPeriodTo: payment.PaymentPeriodTo,
            Status: FormatPaymentStatus(payment.Status),
            Notes: payment.Notes,
            CancelledAt: payment.CancelledAt,
            CancelledBy: payment.CancelledBy,
            CancellationReason: payment.CancellationReason,
            CreatedAt: payment.CreatedAt,
            CreatedBy: payment.CreatedBy,
            UpdatedAt: payment.UpdatedAt,
            UpdatedBy: payment.UpdatedBy);

    public static PaymentType ParsePaymentType(string value) => value.Trim().ToUpperInvariant() switch
    {
        "ADVANCE" => PaymentType.Advance,
        "PAYOUT" => PaymentType.Payout,
        "ADJUSTMENT" => PaymentType.Adjustment,
        _ => throw new ValidationException($"The payment type '{value}' is invalid. Supported values: ADVANCE, PAYOUT, ADJUSTMENT.")
    };

    public static string FormatPaymentType(PaymentType type) => type switch
    {
        PaymentType.Advance => "ADVANCE",
        PaymentType.Payout => "PAYOUT",
        PaymentType.Adjustment => "ADJUSTMENT",
        _ => type.ToString().ToUpperInvariant()
    };

    public static PaymentMethod ParsePaymentMethod(string value) => value.Trim().ToUpperInvariant() switch
    {
        "CASH" => PaymentMethod.Cash,
        "BANK_TRANSFER" or "BANKTRANSFER" => PaymentMethod.BankTransfer,
        "UPI" => PaymentMethod.Upi,
        "CHEQUE" => PaymentMethod.Cheque,
        "OTHER" => PaymentMethod.Other,
        _ => throw new ValidationException($"The payment method '{value}' is invalid. Supported values: CASH, BANK_TRANSFER, UPI, CHEQUE, OTHER.")
    };

    public static string FormatPaymentMethod(PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "CASH",
        PaymentMethod.BankTransfer => "BANK_TRANSFER",
        PaymentMethod.Upi => "UPI",
        PaymentMethod.Cheque => "CHEQUE",
        PaymentMethod.Other => "OTHER",
        _ => method.ToString().ToUpperInvariant()
    };

    public static PaymentStatus ParsePaymentStatus(string value) => value.Trim().ToUpperInvariant() switch
    {
        "PENDING" => PaymentStatus.Pending,
        "COMPLETED" => PaymentStatus.Completed,
        "CANCELLED" => PaymentStatus.Cancelled,
        _ => throw new ValidationException($"The payment status '{value}' is invalid. Supported values: PENDING, COMPLETED, CANCELLED.")
    };

    public static string FormatPaymentStatus(PaymentStatus status) => status switch
    {
        PaymentStatus.Pending => "PENDING",
        PaymentStatus.Completed => "COMPLETED",
        PaymentStatus.Cancelled => "CANCELLED",
        _ => status.ToString().ToUpperInvariant()
    };

    private static void ValidateActor(PaymentActor actor)
    {
        if (actor.OrganizationId == Guid.Empty)
        {
            throw new ArgumentException("An organization context is required.", nameof(actor));
        }

        if (actor.UserId == Guid.Empty)
        {
            throw new ArgumentException("A user context is required.", nameof(actor));
        }
    }
}
