using System.Text.Json;
using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Labor;
using FarmManagement.Application.Interfaces.Labor;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.Services;

public sealed class WorkerEarningsLedgerService(IWorkerEarningsLedgerStore store) : IWorkerEarningsLedgerService, IAttendanceEarningsIntegration
{
    public async Task<AttendanceEarningsCalculationResult> CalculateAttendanceEarningsAsync(
        EarningsActor actor,
        CalculateAttendanceEarningsRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ArgumentNullException.ThrowIfNull(request);

        if (request.WorkerId == Guid.Empty)
        {
            throw new ValidationException("A valid worker ID is required.");
        }

        if (string.IsNullOrWhiteSpace(request.AttendanceType))
        {
            throw new ValidationException("An attendance type is required.");
        }

        // 1. Load the worker
        var worker = await store.FindWorkerAsync(request.WorkerId, actor.OrganizationId, cancellationToken);
        if (worker is null)
        {
            throw new ResourceNotFoundException("The worker was not found.");
        }

        // 2. Validate worker is eligible for attendance on that date
        if (!worker.IsEligibleForAttendance(request.AttendanceDate, out var ineligibilityReason))
        {
            throw new ValidationException(ineligibilityReason!);
        }

        // 3. Determine worker gender
        var gender = worker.Gender;

        // 4. Determine applicable wage type
        if (!TryResolveWageTypeFromAttendanceType(request.AttendanceType, out var wageType, out var isEarningEligible))
        {
            throw new ValidationException(
                $"The attendance type '{request.AttendanceType}' is invalid. Supported values: FULL_DAY, HALF_DAY, HOURLY, MONTHLY, ABSENT, LEAVE, NOT_WORKED.");
        }

        if (!isEarningEligible || wageType is null)
        {
            return new AttendanceEarningsCalculationResult(
                WorkerId: worker.Id,
                WorkerDisplayName: worker.DisplayName,
                Gender: gender,
                AttendanceType: request.AttendanceType.Trim().ToUpperInvariant(),
                WageType: null,
                Quantity: request.Quantity > 0 ? request.Quantity : 0m,
                WageRate: 0m,
                GrossAmount: 0m,
                CurrencyId: null,
                CurrencyCode: "INR",
                CurrencySymbol: "₹",
                IsEarningEligible: false,
                IsWorkerEligible: true,
                IneligibilityReason: null);
        }

        if (request.Quantity <= 0)
        {
            throw new ValidationException("The attendance quantity must be greater than zero.");
        }

        // 5. Resolve wage rate by organization + gender + wage type + attendance date
        var wageRate = await store.FindApplicableWageRateAsync(
            actor.OrganizationId,
            gender,
            wageType.Value,
            request.AttendanceDate,
            cancellationToken);

        if (wageRate is null)
        {
            throw new ValidationException(
                $"No active labor wage rate found for gender '{gender}' and wage type '{FormatWageType(wageType.Value)}' on {request.AttendanceDate:yyyy-MM-dd}.");
        }

        // 6. Snapshot wage rate
        var snappedRate = wageRate.WageRate;

        // 7. Calculate gross earnings
        var grossAmount = Math.Round(request.Quantity * snappedRate, 2);

        return new AttendanceEarningsCalculationResult(
            WorkerId: worker.Id,
            WorkerDisplayName: worker.DisplayName,
            Gender: gender,
            AttendanceType: request.AttendanceType.Trim().ToUpperInvariant(),
            WageType: FormatWageType(wageType.Value),
            Quantity: request.Quantity,
            WageRate: snappedRate,
            GrossAmount: grossAmount,
            CurrencyId: wageRate.CurrencyId,
            CurrencyCode: wageRate.Currency?.Code ?? "INR",
            CurrencySymbol: wageRate.Currency?.Symbol ?? "₹",
            IsEarningEligible: true,
            IsWorkerEligible: true,
            IneligibilityReason: null);
    }

    public async Task<WorkerEarningsLedgerResponse?> ProcessAttendanceEarningsAsync(
        EarningsActor actor,
        ProcessAttendanceEarningsRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ArgumentNullException.ThrowIfNull(request);

        if (request.AttendanceId == Guid.Empty)
        {
            throw new ValidationException("A valid attendance ID is required.");
        }

        if (request.WorkerId == Guid.Empty)
        {
            throw new ValidationException("A valid worker ID is required.");
        }

        if (string.IsNullOrWhiteSpace(request.AttendanceType))
        {
            throw new ValidationException("An attendance type is required.");
        }

        // 1. Load the worker
        var worker = await store.FindWorkerAsync(request.WorkerId, actor.OrganizationId, cancellationToken);
        if (worker is null)
        {
            throw new ResourceNotFoundException("The worker was not found.");
        }

        // 2. Validate worker is eligible for attendance on that date
        if (!worker.IsEligibleForAttendance(request.AttendanceDate, out var ineligibilityReason))
        {
            throw new ValidationException(ineligibilityReason!);
        }

        // 3. Determine worker gender
        var gender = worker.Gender;

        // 4. Determine applicable wage type
        if (!TryResolveWageTypeFromAttendanceType(request.AttendanceType, out var wageType, out var isEarningEligible))
        {
            throw new ValidationException(
                $"The attendance type '{request.AttendanceType}' is invalid. Supported values: FULL_DAY, HALF_DAY, HOURLY, MONTHLY, ABSENT, LEAVE, NOT_WORKED.");
        }

        var existingEntry = await store.FindByAttendanceIdAsync(actor.OrganizationId, request.AttendanceId, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        // Non-earning attendance (ABSENT, LEAVE)
        if (!isEarningEligible || wageType is null)
        {
            if (existingEntry is null)
            {
                return null;
            }

            if (existingEntry.Status == EarningsLedgerStatus.Calculated)
            {
                existingEntry.MarkReversed(now, actor.UserId);
                AddAudit(
                    actor,
                    existingEntry,
                    "WorkerEarnings.CancelledFromAttendance",
                    new
                    {
                        AttendanceId = request.AttendanceId,
                        WorkerId = request.WorkerId,
                        Reason = $"Attendance updated to non-earning type '{request.AttendanceType}'"
                    },
                    ipAddress: null);

                await store.SaveChangesAsync(cancellationToken);
                return MapToResponse(existingEntry, worker.DisplayName, existingEntry.Currency?.Code ?? "INR", existingEntry.Currency?.Symbol ?? "₹");
            }

            if (existingEntry.Status == EarningsLedgerStatus.Approved)
            {
                var reversal = WorkerEarningsLedger.CreateReversal(
                    existingEntry,
                    actor.UserId,
                    now,
                    $"Attendance changed to '{request.AttendanceType}'");

                existingEntry.MarkReversed(now, actor.UserId);
                store.Add(reversal);

                AddAudit(
                    actor,
                    reversal,
                    "WorkerEarnings.ReversedFromAttendance",
                    new
                    {
                        AttendanceId = request.AttendanceId,
                        WorkerId = request.WorkerId,
                        ReversalLedgerId = reversal.Id,
                        Reason = $"Attendance updated to non-earning type '{request.AttendanceType}'"
                    },
                    ipAddress: null);

                await store.SaveChangesAsync(cancellationToken);
                return null;
            }

            return null;
        }

        if (request.Quantity <= 0)
        {
            throw new ValidationException("The attendance quantity must be greater than zero.");
        }

        // 5. Resolve wage rate by organization + gender + wage type + attendance date
        var wageRate = await store.FindApplicableWageRateAsync(
            actor.OrganizationId,
            gender,
            wageType.Value,
            request.AttendanceDate,
            cancellationToken);

        if (wageRate is null)
        {
            throw new ValidationException(
                $"No active labor wage rate found for gender '{gender}' and wage type '{FormatWageType(wageType.Value)}' on {request.AttendanceDate:yyyy-MM-dd}.");
        }

        // 6. Snapshot wage rate
        var snappedRate = wageRate.WageRate;

        // 7. Calculate gross earnings
        var grossAmount = Math.Round(request.Quantity * snappedRate, 2);

        // 8. Create/update appropriate earnings state according to attendance finalization rules
        if (existingEntry is null)
        {
            var newEntry = WorkerEarningsLedger.CreateEarning(
                organizationId: actor.OrganizationId,
                workerId: request.WorkerId,
                earningsDate: request.AttendanceDate,
                wageType: wageType.Value,
                quantity: request.Quantity,
                wageRate: snappedRate,
                currencyId: wageRate.CurrencyId,
                createdBy: actor.UserId,
                attendanceId: request.AttendanceId,
                grossAmount: grossAmount,
                approveImmediately: request.AutoApprove,
                referenceLedgerId: null,
                description: request.Description);

            store.Add(newEntry);

            AddAudit(
                actor,
                newEntry,
                "WorkerEarnings.CreatedFromAttendance",
                new
                {
                    AttendanceId = request.AttendanceId,
                    WorkerId = request.WorkerId,
                    EarningsDate = request.AttendanceDate,
                    WageType = FormatWageType(wageType.Value),
                    Quantity = request.Quantity,
                    WageRate = snappedRate,
                    CurrencyId = wageRate.CurrencyId,
                    GrossAmount = grossAmount,
                    Status = FormatStatus(newEntry.Status)
                },
                ipAddress: null);

            await store.SaveChangesAsync(cancellationToken);

            return MapToResponse(newEntry, worker.DisplayName, wageRate.Currency?.Code ?? "INR", wageRate.Currency?.Symbol ?? "₹");
        }

        if (existingEntry.Status == EarningsLedgerStatus.Calculated)
        {
            existingEntry.UpdateAttendanceEarning(
                earningsDate: request.AttendanceDate,
                wageType: wageType.Value,
                quantity: request.Quantity,
                wageRate: snappedRate,
                grossAmount: grossAmount,
                currencyId: wageRate.CurrencyId,
                description: request.Description,
                now: now,
                updatedBy: actor.UserId);

            if (request.AutoApprove)
            {
                existingEntry.Approve(now, actor.UserId);
            }

            AddAudit(
                actor,
                existingEntry,
                "WorkerEarnings.UpdatedFromAttendance",
                new
                {
                    AttendanceId = request.AttendanceId,
                    WorkerId = request.WorkerId,
                    EarningsDate = request.AttendanceDate,
                    WageType = FormatWageType(wageType.Value),
                    Quantity = request.Quantity,
                    WageRate = snappedRate,
                    GrossAmount = grossAmount,
                    Status = FormatStatus(existingEntry.Status)
                },
                ipAddress: null);

            await store.SaveChangesAsync(cancellationToken);

            return MapToResponse(existingEntry, worker.DisplayName, wageRate.Currency?.Code ?? "INR", wageRate.Currency?.Symbol ?? "₹");
        }

        if (existingEntry.Status == EarningsLedgerStatus.Approved)
        {
            if (existingEntry.WageType == wageType.Value &&
                existingEntry.Quantity == request.Quantity &&
                existingEntry.WageRate == snappedRate &&
                existingEntry.EarningsDate == request.AttendanceDate)
            {
                return MapToResponse(existingEntry, worker.DisplayName, wageRate.Currency?.Code ?? "INR", wageRate.Currency?.Symbol ?? "₹");
            }

            var reversal = WorkerEarningsLedger.CreateReversal(
                existingEntry,
                actor.UserId,
                now,
                "Reversal due to attendance adjustment");

            existingEntry.MarkReversed(now, actor.UserId);
            store.Add(reversal);

            var replacementEntry = WorkerEarningsLedger.CreateEarning(
                organizationId: actor.OrganizationId,
                workerId: request.WorkerId,
                earningsDate: request.AttendanceDate,
                wageType: wageType.Value,
                quantity: request.Quantity,
                wageRate: snappedRate,
                currencyId: wageRate.CurrencyId,
                createdBy: actor.UserId,
                attendanceId: request.AttendanceId,
                grossAmount: grossAmount,
                approveImmediately: request.AutoApprove,
                referenceLedgerId: existingEntry.Id,
                description: request.Description);

            store.Add(replacementEntry);

            AddAudit(
                actor,
                replacementEntry,
                "WorkerEarnings.AdjustedFromAttendance",
                new
                {
                    AttendanceId = request.AttendanceId,
                    PreviousLedgerId = existingEntry.Id,
                    ReversalLedgerId = reversal.Id,
                    NewLedgerId = replacementEntry.Id,
                    GrossAmount = grossAmount
                },
                ipAddress: null);

            await store.SaveChangesAsync(cancellationToken);

            return MapToResponse(replacementEntry, worker.DisplayName, wageRate.Currency?.Code ?? "INR", wageRate.Currency?.Symbol ?? "₹");
        }

        // If existingEntry was already reversed, create a fresh entry
        var reactivatedEntry = WorkerEarningsLedger.CreateEarning(
            organizationId: actor.OrganizationId,
            workerId: request.WorkerId,
            earningsDate: request.AttendanceDate,
            wageType: wageType.Value,
            quantity: request.Quantity,
            wageRate: snappedRate,
            currencyId: wageRate.CurrencyId,
            createdBy: actor.UserId,
            attendanceId: request.AttendanceId,
            grossAmount: grossAmount,
            approveImmediately: request.AutoApprove,
            referenceLedgerId: null,
            description: request.Description);

        store.Add(reactivatedEntry);
        await store.SaveChangesAsync(cancellationToken);
        return MapToResponse(reactivatedEntry, worker.DisplayName, wageRate.Currency?.Code ?? "INR", wageRate.Currency?.Symbol ?? "₹");
    }

    public async Task<WorkerEarningsLedgerResponse> CreateEarningFromFinalizedAttendanceAsync(
        EarningsActor actor,
        FinalizedAttendanceRecord attendance,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ArgumentNullException.ThrowIfNull(attendance);

        if (attendance.OrganizationId != actor.OrganizationId)
        {
            throw new ValidationException("The attendance record belongs to a different organization.");
        }

        var request = new ProcessAttendanceEarningsRequest(
            AttendanceId: attendance.AttendanceId,
            WorkerId: attendance.WorkerId,
            AttendanceDate: attendance.AttendanceDate,
            AttendanceType: FormatWageType(attendance.WageType),
            Quantity: attendance.Quantity,
            Description: attendance.Description,
            AutoApprove: attendance.AutoApprove);

        var result = await ProcessAttendanceEarningsAsync(actor, request, cancellationToken);
        return result ?? throw new ValidationException("Failed to generate earnings for finalized attendance.");
    }

    public async Task<WorkerEarningsLedgerResponse> ReverseEarningAsync(
        EarningsActor actor,
        Guid ledgerEntryId,
        ReverseEarningsRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);

        var original = await store.FindAsync(ledgerEntryId, actor.OrganizationId, cancellationToken);
        if (original is null)
        {
            throw new ResourceNotFoundException("The ledger entry was not found.");
        }

        if (original.Status == EarningsLedgerStatus.Reversed)
        {
            throw new ValidationException("The ledger entry has already been reversed.");
        }

        if (original.EntryType == EarningsEntryType.Reversal)
        {
            throw new ValidationException("A reversal entry cannot be reversed.");
        }

        var hasExistingReversal = await store.HasActiveReversalAsync(ledgerEntryId, actor.OrganizationId, cancellationToken);
        if (hasExistingReversal)
        {
            throw new ConflictException("A reversal entry already exists for this ledger entry.");
        }

        var now = DateTimeOffset.UtcNow;
        var reversal = WorkerEarningsLedger.CreateReversal(
            original,
            actor.UserId,
            now,
            request.Reason);

        original.MarkReversed(now, actor.UserId);

        store.Add(reversal);

        AddAudit(
            actor,
            reversal,
            "WorkerEarnings.Reversed",
            new
            {
                OriginalLedgerId = original.Id,
                ReversalLedgerId = reversal.Id,
                WorkerId = original.WorkerId,
                ReversedAmount = reversal.GrossAmount,
                Reason = request.Reason
            },
            ipAddress);

        await store.SaveChangesAsync(cancellationToken);

        return MapToResponse(
            reversal,
            original.Worker?.DisplayName ?? string.Empty,
            original.Currency?.Code ?? "INR",
            original.Currency?.Symbol ?? "₹");
    }

    public async Task<WorkerEarningsLedgerResponse> CreateAdjustmentAsync(
        EarningsActor actor,
        CreateEarningsAdjustmentRequest request,
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

        if (request.Quantity <= 0)
        {
            throw new ValidationException("The quantity must be greater than zero.");
        }

        if (request.GrossAmount == 0)
        {
            throw new ValidationException("An adjustment amount cannot be zero.");
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

        var wageType = ParseWageType(request.WageType);

        if (request.ReferenceLedgerId.HasValue && request.ReferenceLedgerId.Value != Guid.Empty)
        {
            var refEntry = await store.FindAsync(request.ReferenceLedgerId.Value, actor.OrganizationId, cancellationToken);
            if (refEntry is null)
            {
                throw new ValidationException("The referenced ledger entry was not found.");
            }

            if (refEntry.WorkerId != request.WorkerId)
            {
                throw new ValidationException("The referenced ledger entry belongs to a different worker.");
            }
        }

        var calculatedWageRate = request.WageRate ?? Math.Abs(Math.Round(request.GrossAmount / request.Quantity, 2));
        if (calculatedWageRate <= 0)
        {
            calculatedWageRate = Math.Abs(request.GrossAmount);
        }

        var adjustment = WorkerEarningsLedger.CreateAdjustment(
            organizationId: actor.OrganizationId,
            workerId: request.WorkerId,
            earningsDate: request.EarningsDate,
            wageType: wageType,
            quantity: request.Quantity,
            wageRate: calculatedWageRate,
            grossAmount: request.GrossAmount,
            currencyId: request.CurrencyId,
            createdBy: actor.UserId,
            approveImmediately: request.ApproveImmediately,
            referenceLedgerId: request.ReferenceLedgerId,
            description: request.Description);

        store.Add(adjustment);

        AddAudit(
            actor,
            adjustment,
            "WorkerEarnings.AdjustmentCreated",
            new
            {
                WorkerId = request.WorkerId,
                EarningsDate = request.EarningsDate,
                GrossAmount = request.GrossAmount,
                CurrencyId = request.CurrencyId,
                ReferenceLedgerId = request.ReferenceLedgerId,
                Status = FormatStatus(adjustment.Status)
            },
            ipAddress);

        await store.SaveChangesAsync(cancellationToken);

        return MapToResponse(adjustment, worker.DisplayName, currency.Code, currency.Symbol);
    }

    public async Task<WorkerEarningsLedgerResponse> ApproveEarningAsync(
        EarningsActor actor,
        Guid ledgerEntryId,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);

        var entry = await store.FindAsync(ledgerEntryId, actor.OrganizationId, cancellationToken);
        if (entry is null)
        {
            throw new ResourceNotFoundException("The ledger entry was not found.");
        }

        if (entry.Status == EarningsLedgerStatus.Reversed)
        {
            throw new ValidationException("Cannot approve a reversed ledger entry.");
        }

        if (entry.Status == EarningsLedgerStatus.Approved)
        {
            return MapToResponse(
                entry,
                entry.Worker?.DisplayName ?? string.Empty,
                entry.Currency?.Code ?? "INR",
                entry.Currency?.Symbol ?? "₹");
        }

        var now = DateTimeOffset.UtcNow;
        entry.Approve(now, actor.UserId);

        AddAudit(
            actor,
            entry,
            "WorkerEarnings.Approved",
            new
            {
                LedgerId = entry.Id,
                WorkerId = entry.WorkerId,
                GrossAmount = entry.GrossAmount,
                FinalizedAt = now
            },
            ipAddress);

        await store.SaveChangesAsync(cancellationToken);

        return MapToResponse(
            entry,
            entry.Worker?.DisplayName ?? string.Empty,
            entry.Currency?.Code ?? "INR",
            entry.Currency?.Symbol ?? "₹");
    }

    public async Task<WorkerGrossEarnedBalanceResponse> GetWorkerGrossEarnedBalanceAsync(
        EarningsActor actor,
        Guid workerId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);

        if (fromDate.HasValue && toDate.HasValue && toDate.Value < fromDate.Value)
        {
            throw new ValidationException("The to date cannot be earlier than from date.");
        }

        var worker = await store.FindWorkerAsync(workerId, actor.OrganizationId, cancellationToken);
        if (worker is null)
        {
            throw new ResourceNotFoundException("The worker was not found.");
        }

        var entries = await store.ListForBalanceCalculationAsync(
            actor.OrganizationId,
            workerId,
            fromDate,
            toDate,
            cancellationToken);

        var approvedEarnings = entries
            .Where(e => e.Status == EarningsLedgerStatus.Approved && e.EntryType == EarningsEntryType.Earning)
            .Sum(e => e.GrossAmount);

        var totalReversals = entries
            .Where(e => e.EntryType == EarningsEntryType.Reversal)
            .Sum(e => e.GrossAmount);

        var totalAdjustments = entries
            .Where(e => e.Status == EarningsLedgerStatus.Approved && e.EntryType == EarningsEntryType.Adjustment)
            .Sum(e => e.GrossAmount);

        var totalPendingCalculated = entries
            .Where(e => e.Status == EarningsLedgerStatus.Calculated)
            .Sum(e => e.GrossAmount);

        var approvedCount = entries.Count(e => e.Status == EarningsLedgerStatus.Approved);
        var reversedCount = entries.Count(e => e.Status == EarningsLedgerStatus.Reversed);
        var calculatedCount = entries.Count(e => e.Status == EarningsLedgerStatus.Calculated);

        // Net gross earned = sum of approved earnings + approved adjustments
        // Note: Any reversed earning has status = Reversed (thus excluded from approvedEarnings),
        // and its paired reversal row has gross_amount = -original.
        var grossEarned = approvedEarnings + totalAdjustments;

        var currency = entries.FirstOrDefault()?.Currency;

        return new WorkerGrossEarnedBalanceResponse(
            WorkerId: workerId,
            OrganizationId: actor.OrganizationId,
            FromDate: fromDate,
            ToDate: toDate,
            GrossEarned: grossEarned,
            TotalApprovedEarnings: approvedEarnings,
            TotalReversals: totalReversals,
            TotalAdjustments: totalAdjustments,
            TotalPendingCalculated: totalPendingCalculated,
            ApprovedEntryCount: approvedCount,
            ReversedEntryCount: reversedCount,
            CalculatedEntryCount: calculatedCount,
            CurrencyId: currency?.Id,
            CurrencyCode: currency?.Code ?? "INR",
            CurrencySymbol: currency?.Symbol ?? "₹");
    }

    public async Task<PagedResponse<WorkerEarningsLedgerResponse>> ListWorkerEarningsAsync(
        EarningsActor actor,
        Guid workerId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        string? status = null,
        string? entryType = null,
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

        EarningsLedgerStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            parsedStatus = ParseStatus(status);
        }

        EarningsEntryType? parsedEntryType = null;
        if (!string.IsNullOrWhiteSpace(entryType))
        {
            parsedEntryType = ParseEntryType(entryType);
        }

        var paged = await store.ListByWorkerAsync(
            actor.OrganizationId,
            workerId,
            fromDate,
            toDate,
            parsedStatus,
            parsedEntryType,
            page,
            pageSize,
            cancellationToken);

        var items = paged.Items.Select(e => MapToResponse(
            e,
            e.Worker?.DisplayName ?? string.Empty,
            e.Currency?.Code ?? "INR",
            e.Currency?.Symbol ?? "₹")).ToList();

        return new PagedResponse<WorkerEarningsLedgerResponse>(items, paged.TotalCount, paged.Page, paged.PageSize);
    }

    public async Task<WorkerEarningsLedgerResponse> GetByIdAsync(
        EarningsActor actor,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);

        var entry = await store.FindAsync(id, actor.OrganizationId, cancellationToken);
        if (entry is null)
        {
            throw new ResourceNotFoundException("The ledger entry was not found.");
        }

        return MapToResponse(
            entry,
            entry.Worker?.DisplayName ?? string.Empty,
            entry.Currency?.Code ?? "INR",
            entry.Currency?.Symbol ?? "₹");
    }

    private void AddAudit(
        EarningsActor actor,
        WorkerEarningsLedger entry,
        string action,
        object? details,
        string? ipAddress) =>
        store.AddAuditLog(new AuditLog(
            action: action,
            organizationId: actor.OrganizationId,
            userId: actor.UserId,
            entityType: nameof(WorkerEarningsLedger),
            entityId: entry.Id,
            details: details is null ? null : JsonSerializer.SerializeToDocument(details),
            ipAddress: ipAddress));

    private static WorkerEarningsLedgerResponse MapToResponse(
        WorkerEarningsLedger entry,
        string workerDisplayName,
        string currencyCode,
        string currencySymbol) =>
        new(
            Id: entry.Id,
            OrganizationId: entry.OrganizationId,
            WorkerId: entry.WorkerId,
            WorkerDisplayName: workerDisplayName,
            AttendanceId: entry.AttendanceId,
            EarningsDate: entry.EarningsDate,
            WageType: FormatWageType(entry.WageType),
            Quantity: entry.Quantity,
            WageRate: entry.WageRate,
            CurrencyId: entry.CurrencyId,
            CurrencyCode: currencyCode,
            CurrencySymbol: currencySymbol,
            GrossAmount: entry.GrossAmount,
            EntryType: FormatEntryType(entry.EntryType),
            Status: FormatStatus(entry.Status),
            ReferenceLedgerId: entry.ReferenceLedgerId,
            Description: entry.Description,
            FinalizedAt: entry.FinalizedAt,
            FinalizedBy: entry.FinalizedBy,
            CreatedAt: entry.CreatedAt,
            CreatedBy: entry.CreatedBy,
            UpdatedAt: entry.UpdatedAt,
            UpdatedBy: entry.UpdatedBy);

    public static bool TryResolveWageTypeFromAttendanceType(
        string attendanceTypeOrWageType,
        out WageType? wageType,
        out bool isEarningEligible)
    {
        var normalized = attendanceTypeOrWageType.Trim().ToUpperInvariant();
        switch (normalized)
        {
            case "FULL_DAY":
            case "FULLDAY":
            case "PRESENT":
                wageType = WageType.FullDay;
                isEarningEligible = true;
                return true;
            case "HALF_DAY":
            case "HALFDAY":
                wageType = WageType.HalfDay;
                isEarningEligible = true;
                return true;
            case "HOURLY":
                wageType = WageType.Hourly;
                isEarningEligible = true;
                return true;
            case "MONTHLY":
                wageType = WageType.Monthly;
                isEarningEligible = true;
                return true;
            case "ABSENT":
            case "LEAVE":
            case "NOT_WORKED":
            case "NOTWORKED":
                wageType = null;
                isEarningEligible = false;
                return true;
            default:
                wageType = null;
                isEarningEligible = false;
                return false;
        }
    }

    public static WageType ParseWageType(string value) => value.Trim().ToUpperInvariant() switch
    {
        "FULL_DAY" or "FULLDAY" => WageType.FullDay,
        "HALF_DAY" or "HALFDAY" => WageType.HalfDay,
        "HOURLY" => WageType.Hourly,
        "MONTHLY" => WageType.Monthly,
        _ => throw new ValidationException($"The wage type '{value}' is invalid. Supported values: FULL_DAY, HALF_DAY, HOURLY, MONTHLY.")
    };

    public static string FormatWageType(WageType wageType) => wageType switch
    {
        WageType.FullDay => "FULL_DAY",
        WageType.HalfDay => "HALF_DAY",
        WageType.Hourly => "HOURLY",
        WageType.Monthly => "MONTHLY",
        _ => wageType.ToString().ToUpperInvariant()
    };

    public static EarningsEntryType ParseEntryType(string value) => value.Trim().ToUpperInvariant() switch
    {
        "EARNING" => EarningsEntryType.Earning,
        "REVERSAL" => EarningsEntryType.Reversal,
        "ADJUSTMENT" => EarningsEntryType.Adjustment,
        _ => throw new ValidationException($"The entry type '{value}' is invalid. Supported values: EARNING, REVERSAL, ADJUSTMENT.")
    };

    public static string FormatEntryType(EarningsEntryType entryType) => entryType switch
    {
        EarningsEntryType.Earning => "EARNING",
        EarningsEntryType.Reversal => "REVERSAL",
        EarningsEntryType.Adjustment => "ADJUSTMENT",
        _ => entryType.ToString().ToUpperInvariant()
    };

    public static EarningsLedgerStatus ParseStatus(string value) => value.Trim().ToUpperInvariant() switch
    {
        "CALCULATED" => EarningsLedgerStatus.Calculated,
        "APPROVED" => EarningsLedgerStatus.Approved,
        "REVERSED" => EarningsLedgerStatus.Reversed,
        _ => throw new ValidationException($"The status '{value}' is invalid. Supported values: CALCULATED, APPROVED, REVERSED.")
    };

    public static string FormatStatus(EarningsLedgerStatus status) => status switch
    {
        EarningsLedgerStatus.Calculated => "CALCULATED",
        EarningsLedgerStatus.Approved => "APPROVED",
        EarningsLedgerStatus.Reversed => "REVERSED",
        _ => status.ToString().ToUpperInvariant()
    };

    private static void ValidateActor(EarningsActor actor)
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
