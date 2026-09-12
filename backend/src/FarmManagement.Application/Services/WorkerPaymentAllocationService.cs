using System.Text.Json;
using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Labor;
using FarmManagement.Application.Interfaces.Labor;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.Services;

public sealed class WorkerPaymentAllocationService(
    IWorkerPaymentAllocationStore store) : IWorkerPaymentAllocationService
{
    public async Task<WorkerPaymentAllocationResponse> CreateAllocationAsync(
        PaymentActor actor,
        CreateWorkerPaymentAllocationRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ArgumentNullException.ThrowIfNull(request);

        if (request.WorkerPaymentId == Guid.Empty)
        {
            throw new ValidationException("A payment is required.");
        }

        if (request.AllocatedAmount <= 0)
        {
            throw new ValidationException("The allocated amount must be greater than zero.");
        }

        var payment = await store.FindPaymentAsync(request.WorkerPaymentId, actor.OrganizationId, cancellationToken);
        if (payment is null)
        {
            throw new ResourceNotFoundException("The payment was not found.");
        }

        if (payment.Status == PaymentStatus.Cancelled)
        {
            throw new ValidationException("Cannot allocate against a cancelled payment.");
        }

        var allocationType = ParseAllocationType(request.AllocationType);

        // Check payment available unallocated amount
        var totalAllocated = await store.GetTotalAllocatedForPaymentAsync(payment.Id, actor.OrganizationId, cancellationToken);
        var remainingPaymentAmount = payment.Amount - totalAllocated;
        if (remainingPaymentAmount <= 0)
        {
            throw new ValidationException("The payment is already fully allocated.");
        }

        if (request.AllocatedAmount > remainingPaymentAmount)
        {
            throw new ValidationException($"The allocated amount ({request.AllocatedAmount:N2}) exceeds the remaining unallocated payment amount ({remainingPaymentAmount:N2}).");
        }

        WorkerEarningsLedger? earning = null;
        if (request.WorkerEarningsLedgerId.HasValue && request.WorkerEarningsLedgerId.Value != Guid.Empty)
        {
            earning = await store.FindEarningAsync(request.WorkerEarningsLedgerId.Value, actor.OrganizationId, cancellationToken);
            if (earning is null)
            {
                throw new ResourceNotFoundException("The earnings ledger entry was not found.");
            }

            if (earning.WorkerId != payment.WorkerId)
            {
                throw new ValidationException("The payment and earnings ledger entry belong to different workers.");
            }

            if (earning.Status == EarningsLedgerStatus.Reversed)
            {
                throw new ValidationException("Cannot allocate against a reversed earnings ledger entry.");
            }

            var totalSettled = await store.GetTotalSettledForEarningAsync(earning.Id, actor.OrganizationId, cancellationToken);
            var remainingEarningAmount = earning.GrossAmount - totalSettled;
            if (remainingEarningAmount <= 0)
            {
                throw new ValidationException("The earnings ledger entry is already fully settled.");
            }

            if (request.AllocatedAmount > remainingEarningAmount)
            {
                throw new ValidationException($"The allocated amount ({request.AllocatedAmount:N2}) exceeds the remaining unpaid earning amount ({remainingEarningAmount:N2}).");
            }
        }
        else
        {
            if (allocationType is PaymentAllocationType.EarningSettlement or PaymentAllocationType.AdvanceOffset)
            {
                throw new ValidationException("An earnings ledger entry is required for this allocation type.");
            }
        }

        var allocation = WorkerPaymentAllocation.Create(
            organizationId: actor.OrganizationId,
            workerPaymentId: payment.Id,
            workerEarningsLedgerId: earning?.Id,
            allocatedAmount: request.AllocatedAmount,
            allocationType: allocationType,
            allocationDate: request.AllocationDate,
            createdBy: actor.UserId,
            notes: request.Notes);

        store.Add(allocation);

        AddAudit(
            actor,
            allocation,
            "WorkerPaymentAllocation.Created",
            new
            {
                AllocationId = allocation.Id,
                PaymentId = allocation.WorkerPaymentId,
                EarningId = allocation.WorkerEarningsLedgerId,
                Amount = allocation.AllocatedAmount,
                AllocationType = FormatAllocationType(allocation.AllocationType),
                AllocationDate = allocation.AllocationDate
            },
            ipAddress);

        await store.SaveChangesAsync(cancellationToken);

        return MapToResponse(allocation, payment, earning);
    }

    public async Task<IReadOnlyList<WorkerPaymentAllocationResponse>> AutoAllocatePaymentAsync(
        PaymentActor actor,
        AutoAllocatePaymentRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ArgumentNullException.ThrowIfNull(request);

        if (request.WorkerPaymentId == Guid.Empty)
        {
            throw new ValidationException("A payment is required.");
        }

        var payment = await store.FindPaymentAsync(request.WorkerPaymentId, actor.OrganizationId, cancellationToken);
        if (payment is null)
        {
            throw new ResourceNotFoundException("The payment was not found.");
        }

        if (payment.Status == PaymentStatus.Cancelled)
        {
            throw new ValidationException("Cannot allocate against a cancelled payment.");
        }

        var totalAllocated = await store.GetTotalAllocatedForPaymentAsync(payment.Id, actor.OrganizationId, cancellationToken);
        var remainingPaymentAmount = payment.Amount - totalAllocated;
        if (remainingPaymentAmount <= 0)
        {
            throw new ValidationException("The payment is already fully allocated.");
        }

        PaymentAllocationType allocationType;
        if (!string.IsNullOrWhiteSpace(request.AllocationType))
        {
            allocationType = ParseAllocationType(request.AllocationType);
        }
        else
        {
            allocationType = payment.PaymentType switch
            {
                PaymentType.Advance => PaymentAllocationType.AdvanceOffset,
                PaymentType.Payout => PaymentAllocationType.EarningSettlement,
                _ => PaymentAllocationType.AdjustmentSettlement
            };
        }

        var cutoffDate = payment.PaymentPeriodTo ?? payment.PaymentDate;
        var eligibleEarnings = await store.ListUnsettledEarningsForWorkerAsync(
            actor.OrganizationId,
            payment.WorkerId,
            cutoffDate,
            cancellationToken);

        var batchSettled = await store.GetSettledTotalsForEarningsBatchAsync(
            eligibleEarnings.Select(e => e.Id),
            actor.OrganizationId,
            cancellationToken);

        var createdAllocations = new List<WorkerPaymentAllocation>();
        var remainingToAllocate = remainingPaymentAmount;
        var allocationDate = request.AllocationDate ?? payment.PaymentDate;

        foreach (var earning in eligibleEarnings)
        {
            if (remainingToAllocate <= 0)
            {
                break;
            }

            var alreadySettled = batchSettled.TryGetValue(earning.Id, out var settled) ? settled : 0m;
            var unpaid = earning.GrossAmount - alreadySettled;
            if (unpaid <= 0)
            {
                continue;
            }

            var allocateAmount = Math.Min(remainingToAllocate, unpaid);
            var allocation = WorkerPaymentAllocation.Create(
                organizationId: actor.OrganizationId,
                workerPaymentId: payment.Id,
                workerEarningsLedgerId: earning.Id,
                allocatedAmount: allocateAmount,
                allocationType: allocationType,
                allocationDate: allocationDate,
                createdBy: actor.UserId,
                notes: request.Notes);

            createdAllocations.Add(allocation);
            remainingToAllocate -= allocateAmount;
        }

        if (createdAllocations.Count == 0)
        {
            throw new ValidationException("No eligible unsettled earnings found for allocation.");
        }

        store.AddRange(createdAllocations);

        foreach (var alloc in createdAllocations)
        {
            AddAudit(
                actor,
                alloc,
                "WorkerPaymentAllocation.AutoAllocated",
                new
                {
                    AllocationId = alloc.Id,
                    PaymentId = alloc.WorkerPaymentId,
                    EarningId = alloc.WorkerEarningsLedgerId,
                    Amount = alloc.AllocatedAmount,
                    AllocationType = FormatAllocationType(alloc.AllocationType),
                    AllocationDate = alloc.AllocationDate
                },
                ipAddress);
        }

        await store.SaveChangesAsync(cancellationToken);

        var earningLookup = eligibleEarnings.ToDictionary(e => e.Id);
        return createdAllocations
            .Select(a => MapToResponse(a, payment, a.WorkerEarningsLedgerId.HasValue && earningLookup.TryGetValue(a.WorkerEarningsLedgerId.Value, out var e) ? e : null))
            .ToList();
    }

    public async Task<WorkerPaymentAllocationResponse> GetByIdAsync(
        PaymentActor actor,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);

        var allocation = await store.FindAsync(id, actor.OrganizationId, cancellationToken);
        if (allocation is null)
        {
            throw new ResourceNotFoundException("The payment allocation was not found.");
        }

        return MapToResponse(allocation, allocation.WorkerPayment, allocation.WorkerEarningsLedger);
    }

    public async Task<IReadOnlyList<WorkerPaymentAllocationResponse>> ListByPaymentAsync(
        PaymentActor actor,
        Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);

        var allocations = await store.ListByPaymentAsync(paymentId, actor.OrganizationId, cancellationToken);
        return allocations.Select(a => MapToResponse(a, a.WorkerPayment, a.WorkerEarningsLedger)).ToList();
    }

    public async Task<IReadOnlyList<WorkerPaymentAllocationResponse>> ListByEarningAsync(
        PaymentActor actor,
        Guid earningId,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);

        var allocations = await store.ListByEarningAsync(earningId, actor.OrganizationId, cancellationToken);
        return allocations.Select(a => MapToResponse(a, a.WorkerPayment, a.WorkerEarningsLedger)).ToList();
    }

    public async Task<IReadOnlyList<WorkerPaymentAllocationResponse>> ListByWorkerAsync(
        PaymentActor actor,
        Guid workerId,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);

        var allocations = await store.ListByWorkerAsync(workerId, actor.OrganizationId, cancellationToken);
        return allocations.Select(a => MapToResponse(a, a.WorkerPayment, a.WorkerEarningsLedger)).ToList();
    }

    public async Task DeleteAllocationAsync(
        PaymentActor actor,
        Guid id,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);

        var allocation = await store.FindAsync(id, actor.OrganizationId, cancellationToken);
        if (allocation is null)
        {
            throw new ResourceNotFoundException("The payment allocation was not found.");
        }

        store.Remove(allocation);

        AddAudit(
            actor,
            allocation,
            "WorkerPaymentAllocation.Deleted",
            new
            {
                AllocationId = allocation.Id,
                PaymentId = allocation.WorkerPaymentId,
                EarningId = allocation.WorkerEarningsLedgerId,
                Amount = allocation.AllocatedAmount
            },
            ipAddress);

        await store.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EarningUnpaidBalanceResponse>> GetUnsettledEarningsForWorkerAsync(
        PaymentActor actor,
        Guid workerId,
        DateOnly? upToDate = null,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);

        var earnings = await store.ListUnsettledEarningsForWorkerAsync(actor.OrganizationId, workerId, upToDate, cancellationToken);
        var batchSettled = await store.GetSettledTotalsForEarningsBatchAsync(
            earnings.Select(e => e.Id),
            actor.OrganizationId,
            cancellationToken);

        var result = new List<EarningUnpaidBalanceResponse>();
        foreach (var earning in earnings)
        {
            var settled = batchSettled.TryGetValue(earning.Id, out var s) ? s : 0m;
            var unpaid = earning.GrossAmount - settled;
            if (unpaid > 0)
            {
                result.Add(new EarningUnpaidBalanceResponse(
                    WorkerEarningsLedgerId: earning.Id,
                    WorkerId: earning.WorkerId,
                    EarningsDate: earning.EarningsDate,
                    EntryType: earning.EntryType.ToString().ToUpperInvariant(),
                    GrossAmount: earning.GrossAmount,
                    AllocatedAmount: settled,
                    RemainingUnpaidAmount: unpaid));
            }
        }

        return result;
    }

    public async Task<PaymentUnallocatedBalanceResponse> GetPaymentUnallocatedBalanceAsync(
        PaymentActor actor,
        Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);

        var payment = await store.FindPaymentAsync(paymentId, actor.OrganizationId, cancellationToken);
        if (payment is null)
        {
            throw new ResourceNotFoundException("The payment was not found.");
        }

        var allocated = await store.GetTotalAllocatedForPaymentAsync(payment.Id, actor.OrganizationId, cancellationToken);
        var remaining = payment.Status == PaymentStatus.Cancelled ? 0m : Math.Max(0m, payment.Amount - allocated);

        return new PaymentUnallocatedBalanceResponse(
            WorkerPaymentId: payment.Id,
            WorkerId: payment.WorkerId,
            PaymentType: payment.PaymentType.ToString().ToUpperInvariant(),
            PaymentDate: payment.PaymentDate,
            Amount: payment.Amount,
            AllocatedAmount: allocated,
            RemainingUnallocatedAmount: remaining);
    }

    private static void ValidateActor(PaymentActor actor)
    {
        ArgumentNullException.ThrowIfNull(actor);
        if (actor.UserId == Guid.Empty)
        {
            throw new ValidationException("An active user is required.");
        }

        if (actor.OrganizationId == Guid.Empty)
        {
            throw new ValidationException("An organization context is required.");
        }
    }

    private static PaymentAllocationType ParseAllocationType(string type) =>
        type.Trim().ToUpperInvariant() switch
        {
            "EARNING_SETTLEMENT" or "EARNINGSETTLEMENT" => PaymentAllocationType.EarningSettlement,
            "ADVANCE_OFFSET" or "ADVANCEOFFSET" => PaymentAllocationType.AdvanceOffset,
            "ADJUSTMENT_SETTLEMENT" or "ADJUSTMENTSETTLEMENT" => PaymentAllocationType.AdjustmentSettlement,
            _ => throw new ValidationException($"Unsupported allocation type '{type}'.")
        };

    private static string FormatAllocationType(PaymentAllocationType type) =>
        type switch
        {
            PaymentAllocationType.EarningSettlement => "EARNING_SETTLEMENT",
            PaymentAllocationType.AdvanceOffset => "ADVANCE_OFFSET",
            PaymentAllocationType.AdjustmentSettlement => "ADJUSTMENT_SETTLEMENT",
            _ => type.ToString().ToUpperInvariant()
        };

    private static WorkerPaymentAllocationResponse MapToResponse(
        WorkerPaymentAllocation allocation,
        WorkerPayment? payment,
        WorkerEarningsLedger? earning) =>
        new(
            Id: allocation.Id,
            OrganizationId: allocation.OrganizationId,
            WorkerPaymentId: allocation.WorkerPaymentId,
            WorkerEarningsLedgerId: allocation.WorkerEarningsLedgerId,
            AllocatedAmount: allocation.AllocatedAmount,
            AllocationType: FormatAllocationType(allocation.AllocationType),
            AllocationDate: allocation.AllocationDate,
            Notes: allocation.Notes,
            CreatedAt: allocation.CreatedAt,
            CreatedBy: allocation.CreatedBy,
            PaymentType: payment?.PaymentType.ToString().ToUpperInvariant(),
            PaymentAmount: payment?.Amount,
            EarningDate: earning?.EarningsDate,
            EarningGrossAmount: earning?.GrossAmount);

    private void AddAudit(
        PaymentActor actor,
        WorkerPaymentAllocation allocation,
        string action,
        object? details,
        string? ipAddress) =>
        store.AddAuditLog(new AuditLog(
            action: action,
            organizationId: actor.OrganizationId,
            userId: actor.UserId,
            entityType: nameof(WorkerPaymentAllocation),
            entityId: allocation.Id,
            details: details is null ? null : JsonSerializer.SerializeToDocument(details),
            ipAddress: ipAddress));
}
