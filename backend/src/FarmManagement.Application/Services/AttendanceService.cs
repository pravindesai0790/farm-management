using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Labor;
using FarmManagement.Application.Interfaces.Labor;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.Services;

public sealed class AttendanceService(
    IAttendanceStore store,
    IAttendanceEarningsIntegration earningsIntegration) : IAttendanceService
{
    private const int DefaultPageSize = 20;
    private const int MaximumPageSize = 100;

    public async Task<PagedResponse<AttendanceEligibleWorkerResponse>> GetEligibleWorkersAsync(
        AttendanceActor actor,
        Guid farmId,
        DateOnly attendanceDate,
        string? search = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);

        if (farmId == Guid.Empty)
        {
            throw Validation("farmId", "Farm is required.");
        }

        if (attendanceDate == default)
        {
            throw Validation("attendanceDate", "Attendance date is required.");
        }

        if (page < 1)
        {
            throw Validation("page", "Page must be at least 1.");
        }

        pageSize = NormalizePageSize(pageSize);

        var farm = await store.FindFarmAsync(farmId, actor.OrganizationId, cancellationToken);
        if (farm is null)
        {
            throw new ResourceNotFoundException("The farm was not found.");
        }

        if (!farm.IsActive)
        {
            throw Validation("farmId", "Cannot load attendance for an inactive farm.");
        }

        var normalizedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        var totalCount = await store.CountEligibleWorkersAsync(
            actor.OrganizationId,
            farmId,
            attendanceDate,
            normalizedSearch,
            cancellationToken);

        if (totalCount == 0)
        {
            return new PagedResponse<AttendanceEligibleWorkerResponse>(
                [],
                page,
                pageSize,
                0);
        }

        var skip = (page - 1) * pageSize;
        var items = await store.ListEligibleWorkersAsync(
            actor.OrganizationId,
            farmId,
            attendanceDate,
            normalizedSearch,
            skip,
            pageSize,
            cancellationToken);

        return new PagedResponse<AttendanceEligibleWorkerResponse>(
            items,
            page,
            pageSize,
            totalCount);
    }

    public async Task<DailyAttendanceResponse> GetDailyAttendanceAsync(
        AttendanceActor actor,
        Guid farmId,
        DateOnly attendanceDate,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);

        if (farmId == Guid.Empty)
        {
            throw Validation("farmId", "Farm is required.");
        }

        if (attendanceDate == default)
        {
            throw Validation("attendanceDate", "Attendance date is required.");
        }

        var farm = await store.FindFarmAsync(farmId, actor.OrganizationId, cancellationToken);
        if (farm is null)
        {
            throw new ResourceNotFoundException("The farm was not found.");
        }

        if (!farm.IsActive)
        {
            throw Validation("farmId", "Cannot load attendance for an inactive farm.");
        }

        var records = await store.ListDailyAttendanceAsync(actor.OrganizationId, farmId, attendanceDate, cancellationToken);

        var summary = BuildDailyAttendanceSummary(records);
        var mappedRecords = records.Select(r => MapToResponse(r, farm.Name)).ToArray();

        return new DailyAttendanceResponse(
            FarmId: farm.Id,
            FarmName: farm.Name,
            AttendanceDate: attendanceDate,
            Summary: summary,
            Records: mappedRecords);
    }

    public async Task<AttendanceRecordResponse> CreateDraftAsync(
        AttendanceActor actor,
        CreateDraftAttendanceRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ArgumentNullException.ThrowIfNull(request);

        if (request.FarmId == Guid.Empty)
        {
            throw Validation("farmId", "Farm is required.");
        }

        if (request.WorkerId == Guid.Empty)
        {
            throw Validation("workerId", "Worker is required.");
        }

        if (request.AttendanceDate == default)
        {
            throw Validation("attendanceDate", "Attendance date is required.");
        }

        var attendanceType = ParseAttendanceType(request.AttendanceType);
        ValidateWorkingHours(attendanceType, request.WorkingHours);

        var farm = await store.FindFarmAsync(request.FarmId, actor.OrganizationId, cancellationToken);
        if (farm is null)
        {
            throw new ResourceNotFoundException("The farm was not found.");
        }

        if (!farm.IsActive)
        {
            throw Validation("farmId", "Cannot record attendance for an inactive farm.");
        }

        var worker = await store.FindWorkerWithAssignmentAsync(
            actor.OrganizationId,
            request.WorkerId,
            request.FarmId,
            request.AttendanceDate,
            cancellationToken);

        if (worker is null)
        {
            throw Validation("workerId", "The worker does not have a valid active farm assignment for this farm on the selected date.");
        }

        var existingAttendance = await store.FindAttendanceByWorkerAndDateAsync(
            actor.OrganizationId,
            request.WorkerId,
            request.AttendanceDate,
            cancellationToken);

        if (existingAttendance is not null)
        {
            throw Validation("workerId", "An attendance record already exists for this worker on the selected date.");
        }

        decimal? calculatedRate = null;
        decimal? calculatedAmount = 0m;
        Guid? currencyId = null;

        if (attendanceType != AttendanceType.NotWorked)
        {
            var quantity = attendanceType == AttendanceType.Hourly ? request.WorkingHours!.Value : 1m;
            var previewResult = await earningsIntegration.CalculateAttendanceEarningsAsync(
                new EarningsActor(actor.UserId, actor.OrganizationId),
                new CalculateAttendanceEarningsRequest(
                    WorkerId: request.WorkerId,
                    AttendanceDate: request.AttendanceDate,
                    AttendanceType: FormatAttendanceType(attendanceType),
                    Quantity: quantity),
                cancellationToken);

            calculatedRate = previewResult.WageRate;
            calculatedAmount = previewResult.GrossAmount;
            currencyId = previewResult.CurrencyId;
        }

        var attendance = LaborAttendance.CreateDraft(
            organizationId: actor.OrganizationId,
            farmId: request.FarmId,
            workerId: request.WorkerId,
            attendanceDate: request.AttendanceDate,
            attendanceType: attendanceType,
            createdBy: actor.UserId,
            workingHours: attendanceType == AttendanceType.Hourly ? request.WorkingHours : null,
            calculatedRate: calculatedRate,
            calculatedAmount: calculatedAmount,
            currencyId: currencyId,
            notes: request.Notes);

        store.AddAttendance(attendance);
        await store.SaveChangesAsync(cancellationToken);

        var loaded = await store.FindAttendanceByIdAsync(attendance.Id, actor.OrganizationId, cancellationToken);
        return MapToResponse(loaded ?? attendance, farm.Name);
    }

    public async Task<AttendanceRecordResponse> UpdateDraftAsync(
        AttendanceActor actor,
        Guid id,
        UpdateDraftAttendanceRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ArgumentNullException.ThrowIfNull(request);

        if (id == Guid.Empty)
        {
            throw Validation("id", "Attendance identifier is required.");
        }

        var attendance = await store.FindAttendanceByIdAsync(id, actor.OrganizationId, cancellationToken);
        if (attendance is null)
        {
            throw new ResourceNotFoundException("The attendance record was not found.");
        }

        if (attendance.Status != AttendanceStatus.Draft)
        {
            throw Validation("status", "Only draft attendance records can be modified.");
        }

        var attendanceType = ParseAttendanceType(request.AttendanceType);
        ValidateWorkingHours(attendanceType, request.WorkingHours);

        var worker = await store.FindWorkerWithAssignmentAsync(
            actor.OrganizationId,
            attendance.WorkerId,
            attendance.FarmId,
            attendance.AttendanceDate,
            cancellationToken);

        if (worker is null)
        {
            throw Validation("workerId", "The worker does not have a valid active farm assignment for this farm on the selected date.");
        }

        decimal? calculatedRate = null;
        decimal? calculatedAmount = 0m;
        Guid? currencyId = null;

        if (attendanceType != AttendanceType.NotWorked)
        {
            var quantity = attendanceType == AttendanceType.Hourly ? request.WorkingHours!.Value : 1m;
            var previewResult = await earningsIntegration.CalculateAttendanceEarningsAsync(
                new EarningsActor(actor.UserId, actor.OrganizationId),
                new CalculateAttendanceEarningsRequest(
                    WorkerId: attendance.WorkerId,
                    AttendanceDate: attendance.AttendanceDate,
                    AttendanceType: FormatAttendanceType(attendanceType),
                    Quantity: quantity),
                cancellationToken);

            calculatedRate = previewResult.WageRate;
            calculatedAmount = previewResult.GrossAmount;
            currencyId = previewResult.CurrencyId;
        }

        attendance.Update(
            attendanceType,
            attendanceType == AttendanceType.Hourly ? request.WorkingHours : null,
            request.Notes,
            DateTimeOffset.UtcNow,
            actor.UserId);

        attendance.SetCalculatedEarnings(
            calculatedRate,
            calculatedAmount,
            currencyId,
            DateTimeOffset.UtcNow,
            actor.UserId);

        await store.SaveChangesAsync(cancellationToken);

        var loaded = await store.FindAttendanceByIdAsync(attendance.Id, actor.OrganizationId, cancellationToken);
        return MapToResponse(loaded ?? attendance, attendance.Farm?.Name ?? string.Empty);
    }

    public async Task DeleteDraftAsync(
        AttendanceActor actor,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);

        if (id == Guid.Empty)
        {
            throw Validation("id", "Attendance identifier is required.");
        }

        var attendance = await store.FindAttendanceByIdAsync(id, actor.OrganizationId, cancellationToken);
        if (attendance is null)
        {
            throw new ResourceNotFoundException("The attendance record was not found.");
        }

        if (attendance.Status != AttendanceStatus.Draft)
        {
            throw Validation("status", "Only draft attendance records can be removed.");
        }

        store.RemoveAttendance(attendance);
        await store.SaveChangesAsync(cancellationToken);
    }

    public async Task<DailyAttendanceResponse> SaveDailyDraftBatchAsync(
        AttendanceActor actor,
        SaveDailyDraftAttendanceBatchRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ArgumentNullException.ThrowIfNull(request);

        if (request.FarmId == Guid.Empty)
        {
            throw Validation("farmId", "Farm is required.");
        }

        if (request.AttendanceDate == default)
        {
            throw Validation("attendanceDate", "Attendance date is required.");
        }

        if (request.Items is null)
        {
            throw Validation("items", "Items list is required.");
        }

        var duplicateWorkers = request.Items
            .GroupBy(i => i.WorkerId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateWorkers.Count > 0)
        {
            throw Validation("items", "Duplicate workers found in the draft attendance batch.");
        }

        var farm = await store.FindFarmAsync(request.FarmId, actor.OrganizationId, cancellationToken);
        if (farm is null)
        {
            throw new ResourceNotFoundException("The farm was not found.");
        }

        if (!farm.IsActive)
        {
            throw Validation("farmId", "Cannot record attendance for an inactive farm.");
        }

        return await store.ExecuteInTransactionAsync(async ct =>
        {
            var existingDaily = await store.ListDailyAttendanceAsync(
                actor.OrganizationId,
                request.FarmId,
                request.AttendanceDate,
                ct);

            var requestedWorkerIds = request.Items.Select(i => i.WorkerId).ToHashSet();

            if (request.RemoveOmittedDrafts)
            {
                var draftsToDelete = existingDaily
                    .Where(r => !requestedWorkerIds.Contains(r.WorkerId) && r.Status == AttendanceStatus.Draft)
                    .ToList();

                foreach (var draftToDelete in draftsToDelete)
                {
                    store.RemoveAttendance(draftToDelete);
                }
            }

            foreach (var item in request.Items)
            {
                if (item.WorkerId == Guid.Empty)
                {
                    throw Validation("workerId", "A valid worker ID is required for each draft attendance item.");
                }

                var attendanceType = ParseAttendanceType(item.AttendanceType);
                ValidateWorkingHours(attendanceType, item.WorkingHours);

                var worker = await store.FindWorkerWithAssignmentAsync(
                    actor.OrganizationId,
                    item.WorkerId,
                    request.FarmId,
                    request.AttendanceDate,
                    ct);

                if (worker is null)
                {
                    throw Validation("workerId", $"Worker '{item.WorkerId}' does not have a valid active farm assignment for this farm on {request.AttendanceDate:yyyy-MM-dd}.");
                }

                var existingForWorker = await store.FindAttendanceByWorkerAndDateAsync(
                    actor.OrganizationId,
                    item.WorkerId,
                    request.AttendanceDate,
                    ct);

                if (existingForWorker is not null)
                {
                    if (existingForWorker.FarmId != request.FarmId)
                    {
                        throw Validation("workerId", $"Worker '{item.WorkerId}' already has an attendance record on {request.AttendanceDate:yyyy-MM-dd} in another farm.");
                    }

                    if (existingForWorker.Status == AttendanceStatus.Finalized)
                    {
                        throw Validation("status", $"Attendance for worker '{item.WorkerId}' is already finalized and cannot be modified.");
                    }
                }

                decimal? calculatedRate = null;
                decimal? calculatedAmount = 0m;
                Guid? currencyId = null;

                if (attendanceType != AttendanceType.NotWorked)
                {
                    var quantity = attendanceType == AttendanceType.Hourly ? item.WorkingHours!.Value : 1m;
                    var previewResult = await earningsIntegration.CalculateAttendanceEarningsAsync(
                        new EarningsActor(actor.UserId, actor.OrganizationId),
                        new CalculateAttendanceEarningsRequest(
                            WorkerId: item.WorkerId,
                            AttendanceDate: request.AttendanceDate,
                            AttendanceType: FormatAttendanceType(attendanceType),
                            Quantity: quantity),
                        ct);

                    calculatedRate = previewResult.WageRate;
                    calculatedAmount = previewResult.GrossAmount;
                    currencyId = previewResult.CurrencyId;
                }

                if (existingForWorker is not null)
                {
                    existingForWorker.Update(
                        attendanceType,
                        attendanceType == AttendanceType.Hourly ? item.WorkingHours : null,
                        item.Notes,
                        DateTimeOffset.UtcNow,
                        actor.UserId);

                    existingForWorker.SetCalculatedEarnings(
                        calculatedRate,
                        calculatedAmount,
                        currencyId,
                        DateTimeOffset.UtcNow,
                        actor.UserId);
                }
                else
                {
                    var newDraft = LaborAttendance.CreateDraft(
                        organizationId: actor.OrganizationId,
                        farmId: request.FarmId,
                        workerId: item.WorkerId,
                        attendanceDate: request.AttendanceDate,
                        attendanceType: attendanceType,
                        createdBy: actor.UserId,
                        workingHours: attendanceType == AttendanceType.Hourly ? item.WorkingHours : null,
                        calculatedRate: calculatedRate,
                        calculatedAmount: calculatedAmount,
                        currencyId: currencyId,
                        notes: item.Notes);

                    store.AddAttendance(newDraft);
                }
            }

            await store.SaveChangesAsync(ct);

            return await GetDailyAttendanceAsync(actor, request.FarmId, request.AttendanceDate, ct);
        }, cancellationToken);
    }

    public async Task<AttendanceWagePreviewResponse> PreviewWageAsync(
        AttendanceActor actor,
        AttendanceWagePreviewRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ArgumentNullException.ThrowIfNull(request);

        if (request.WorkerId == Guid.Empty)
        {
            throw Validation("workerId", "A valid worker ID is required.");
        }

        if (request.AttendanceDate == default)
        {
            throw Validation("attendanceDate", "Attendance date is required.");
        }

        var attendanceType = ParseAttendanceType(request.AttendanceType);
        ValidateWorkingHours(attendanceType, request.WorkingHours);

        if (request.FarmId.HasValue && request.FarmId.Value != Guid.Empty)
        {
            var farm = await store.FindFarmAsync(request.FarmId.Value, actor.OrganizationId, cancellationToken);
            if (farm is null)
            {
                throw new ResourceNotFoundException("The farm was not found.");
            }

            var workerWithAssignment = await store.FindWorkerWithAssignmentAsync(
                actor.OrganizationId,
                request.WorkerId,
                request.FarmId.Value,
                request.AttendanceDate,
                cancellationToken);

            if (workerWithAssignment is null)
            {
                throw Validation("workerId", "The worker does not have a valid active farm assignment for this farm on the selected date.");
            }
        }

        var quantity = attendanceType switch
        {
            AttendanceType.Hourly => request.WorkingHours!.Value,
            AttendanceType.NotWorked => 0m,
            _ => 1m
        };

        var previewResult = await earningsIntegration.CalculateAttendanceEarningsAsync(
            new EarningsActor(actor.UserId, actor.OrganizationId),
            new CalculateAttendanceEarningsRequest(
                WorkerId: request.WorkerId,
                AttendanceDate: request.AttendanceDate,
                AttendanceType: FormatAttendanceType(attendanceType),
                Quantity: quantity),
            cancellationToken);

        return new AttendanceWagePreviewResponse(
            WorkerId: previewResult.WorkerId,
            WorkerDisplayName: previewResult.WorkerDisplayName,
            Gender: previewResult.Gender.ToString().ToUpperInvariant(),
            AttendanceDate: request.AttendanceDate,
            AttendanceType: FormatAttendanceType(attendanceType),
            ResolvedWageType: previewResult.WageType,
            WageType: previewResult.WageType,
            Rate: previewResult.WageRate,
            Quantity: previewResult.Quantity,
            WorkingHours: attendanceType == AttendanceType.Hourly ? request.WorkingHours : null,
            CalculatedAmount: previewResult.GrossAmount,
            CurrencyId: previewResult.CurrencyId,
            CurrencyCode: previewResult.CurrencyCode,
            CurrencySymbol: previewResult.CurrencySymbol,
            Currency: previewResult.CurrencyCode,
            IsEarningEligible: previewResult.IsEarningEligible,
            IsWorkerEligible: previewResult.IsWorkerEligible,
            IneligibilityReason: previewResult.IneligibilityReason);
    }

    public async Task<AttendanceWagePreviewBatchResponse> PreviewWageBatchAsync(
        AttendanceActor actor,
        AttendanceWagePreviewBatchRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ArgumentNullException.ThrowIfNull(request);

        if (request.AttendanceDate == default)
        {
            throw Validation("attendanceDate", "Attendance date is required.");
        }

        if (request.Items is null)
        {
            throw Validation("items", "Items list is required.");
        }

        var previewItems = new List<AttendanceWagePreviewResponse>(request.Items.Count);
        var fullDayCount = 0;
        var halfDayCount = 0;
        var hourlyCount = 0;
        var notWorkedCount = 0;
        var totalEstimatedEarnings = 0m;

        foreach (var item in request.Items)
        {
            var singleRequest = new AttendanceWagePreviewRequest(
                WorkerId: item.WorkerId,
                AttendanceDate: request.AttendanceDate,
                AttendanceType: item.AttendanceType,
                WorkingHours: item.WorkingHours,
                FarmId: request.FarmId);

            var preview = await PreviewWageAsync(actor, singleRequest, cancellationToken);
            previewItems.Add(preview);

            switch (preview.AttendanceType)
            {
                case "FULL_DAY":
                    fullDayCount++;
                    break;
                case "HALF_DAY":
                    halfDayCount++;
                    break;
                case "HOURLY":
                    hourlyCount++;
                    break;
                case "NOT_WORKED":
                    notWorkedCount++;
                    break;
            }

            totalEstimatedEarnings += preview.CalculatedAmount;
        }

        var workedCount = fullDayCount + halfDayCount + hourlyCount;

        return new AttendanceWagePreviewBatchResponse(
            AttendanceDate: request.AttendanceDate,
            TotalCount: previewItems.Count,
            WorkedCount: workedCount,
            FullDayCount: fullDayCount,
            HalfDayCount: halfDayCount,
            HourlyCount: hourlyCount,
            NotWorkedCount: notWorkedCount,
            TotalEstimatedEarnings: totalEstimatedEarnings,
            Items: previewItems);
    }

    public async Task<FinalizeAttendanceResponse> FinalizeAttendanceAsync(
        AttendanceActor actor,
        FinalizeAttendanceRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ArgumentNullException.ThrowIfNull(request);

        if (request.FarmId == Guid.Empty)
        {
            throw Validation("farmId", "Farm is required.");
        }

        if (request.AttendanceDate == default)
        {
            throw Validation("attendanceDate", "Attendance date is required.");
        }

        var farm = await store.FindFarmAsync(request.FarmId, actor.OrganizationId, cancellationToken);
        if (farm is null)
        {
            throw new ResourceNotFoundException("The farm was not found.");
        }

        if (!farm.IsActive)
        {
            throw Validation("farmId", "Cannot finalize attendance for an inactive farm.");
        }

        return await store.ExecuteInTransactionAsync(async ct =>
        {
            // 2. Load the target draft attendance rows (tracked)
            var dailyRecords = await store.ListDailyAttendanceTrackedAsync(
                actor.OrganizationId,
                request.FarmId,
                request.AttendanceDate,
                ct);

            var requestedIds = request.AttendanceIds != null && request.AttendanceIds.Count > 0
                ? request.AttendanceIds.ToHashSet()
                : null;

            var targetRecords = requestedIds != null
                ? dailyRecords.Where(r => requestedIds.Contains(r.Id)).ToList()
                : dailyRecords.ToList();

            if (targetRecords.Count == 0)
            {
                throw Validation("attendance", "No attendance records found to finalize for this farm and date.");
            }

            if (requestedIds != null && targetRecords.Count != requestedIds.Count)
            {
                var missingIds = requestedIds.Except(targetRecords.Select(r => r.Id)).ToList();
                throw Validation("attendanceIds", $"The following attendance records were not found for this farm and date: {string.Join(", ", missingIds)}");
            }

            var alreadyFinalized = targetRecords.Where(r => r.Status == AttendanceStatus.Finalized).ToList();
            if (alreadyFinalized.Count > 0)
            {
                throw Validation("status", $"Cannot finalize attendance because {alreadyFinalized.Count} record(s) are already finalized.");
            }

            var draftsToFinalize = targetRecords.Where(r => r.Status == AttendanceStatus.Draft).ToList();
            if (draftsToFinalize.Count == 0)
            {
                throw Validation("status", "There are no draft attendance records to finalize.");
            }

            var now = DateTimeOffset.UtcNow;
            var paidCount = 0;
            var notWorkedCount = 0;
            var totalEarnings = 0m;

            // Phase 1: Validate all rows before creating any earnings entries
            var preparedRows = new List<(LaborAttendance Attendance, Worker Worker, WageType? WageType, decimal Quantity)>();

            foreach (var attendance in draftsToFinalize)
            {
                // 3. Validate organization scope
                if (attendance.OrganizationId != actor.OrganizationId)
                {
                    throw Validation("organizationId", "The attendance record belongs to a different organization.");
                }

                // 4. Validate farm matches
                if (attendance.FarmId != request.FarmId)
                {
                    throw Validation("farmId", "The attendance record belongs to a different farm.");
                }

                // 5 & 6. Validate worker & farm assignment for attendance_date
                var workerWithAssignment = await store.FindWorkerWithAssignmentAsync(
                    actor.OrganizationId,
                    attendance.WorkerId,
                    request.FarmId,
                    request.AttendanceDate,
                    ct);

                if (workerWithAssignment is null)
                {
                    var workerName = attendance.Worker?.DisplayName ?? attendance.WorkerId.ToString();
                    throw Validation("workerId", $"Worker '{workerName}' does not have a valid active farm assignment for this farm on {request.AttendanceDate:yyyy-MM-dd}.");
                }

                if (!workerWithAssignment.IsActive)
                {
                    throw Validation("workerId", $"Worker '{workerWithAssignment.DisplayName}' is inactive.");
                }

                if (!workerWithAssignment.IsEligibleForAttendance(request.AttendanceDate, out var ineligibilityReason))
                {
                    throw Validation("workerId", ineligibilityReason!);
                }

                // 7. Validate attendance type & hours
                ValidateWorkingHours(attendance.AttendanceType, attendance.WorkingHours);

                if (attendance.AttendanceType == AttendanceType.NotWorked)
                {
                    preparedRows.Add((attendance, workerWithAssignment, null, 0m));
                }
                else
                {
                    // 8. Resolve gender (from worker)
                    // 9. Map attendance type to wage type
                    var wageType = attendance.AttendanceType switch
                    {
                        AttendanceType.FullDay => WageType.FullDay,
                        AttendanceType.HalfDay => WageType.HalfDay,
                        AttendanceType.Hourly => WageType.Hourly,
                        _ => throw Validation("attendanceType", $"Unsupported paid attendance type: {attendance.AttendanceType}")
                    };

                    var quantity = attendance.AttendanceType == AttendanceType.Hourly
                        ? attendance.WorkingHours!.Value
                        : 1m;

                    preparedRows.Add((attendance, workerWithAssignment, wageType, quantity));
                }
            }

            // Phase 2: Create earnings and finalize attendance
            foreach (var (attendance, workerWithAssignment, wageType, quantity) in preparedRows)
            {
                if (attendance.AttendanceType == AttendanceType.NotWorked || wageType is null)
                {
                    // 14. Do not create an earnings entry for NOT_WORKED.
                    // 15 & 16. Mark attendance FINALIZED with zero rate/amount.
                    attendance.Finalize(now, actor.UserId, finalRate: null, finalAmount: 0m, currencyId: null);
                    notWorkedCount++;
                }
                else
                {
                    // 10, 11, 12, 13: Resolve effective wage rate, calculate earnings, snapshot rate/currency, and create earnings ledger entry
                    var finalizedRecord = new FinalizedAttendanceRecord(
                        AttendanceId: attendance.Id,
                        OrganizationId: actor.OrganizationId,
                        WorkerId: attendance.WorkerId,
                        AttendanceDate: attendance.AttendanceDate,
                        WageType: wageType.Value,
                        Quantity: quantity,
                        Description: $"Daily attendance at {farm.Name} on {attendance.AttendanceDate:yyyy-MM-dd}",
                        AutoApprove: true);

                    var earningResult = await earningsIntegration.CreateEarningFromFinalizedAttendanceAsync(
                        new EarningsActor(actor.UserId, actor.OrganizationId),
                        finalizedRecord,
                        ct);

                    if (earningResult is null)
                    {
                        throw Validation("earnings", $"Failed to generate earnings for worker '{workerWithAssignment.DisplayName}'.");
                    }

                    // 15 & 16. Mark attendance FINALIZED with the authoritative snapshotted rate, amount, and currency
                    attendance.Finalize(
                        now,
                        actor.UserId,
                        finalRate: earningResult.WageRate,
                        finalAmount: earningResult.GrossAmount,
                        currencyId: earningResult.CurrencyId);

                    paidCount++;
                    totalEarnings += earningResult.GrossAmount;
                }
            }

            // 17. Commit transaction after saving changes
            await store.SaveChangesAsync(ct);

            // Re-fetch daily attendance for updated response
            var updatedDaily = await store.ListDailyAttendanceAsync(
                actor.OrganizationId,
                request.FarmId,
                request.AttendanceDate,
                ct);

            var summary = BuildDailyAttendanceSummary(updatedDaily);
            var records = updatedDaily.Select(a => MapToResponse(a, farm.Name)).ToList();

            return new FinalizeAttendanceResponse(
                FarmId: farm.Id,
                FarmName: farm.Name,
                AttendanceDate: request.AttendanceDate,
                FinalizedCount: draftsToFinalize.Count,
                PaidCount: paidCount,
                NotWorkedCount: notWorkedCount,
                TotalEarnings: totalEarnings,
                Summary: summary,
                Records: records);
        }, cancellationToken);
    }

    public async Task<AttendanceRecordResponse> FinalizeSingleAttendanceAsync(
        AttendanceActor actor,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);

        if (id == Guid.Empty)
        {
            throw Validation("id", "Attendance identifier is required.");
        }

        var attendance = await store.FindAttendanceByIdAsync(id, actor.OrganizationId, cancellationToken);
        if (attendance is null)
        {
            throw new ResourceNotFoundException("The attendance record was not found.");
        }

        if (attendance.Status == AttendanceStatus.Finalized)
        {
            throw Validation("status", "The attendance record is already finalized.");
        }

        var finalizeRequest = new FinalizeAttendanceRequest(
            attendance.FarmId,
            attendance.AttendanceDate,
            [attendance.Id]);

        var result = await FinalizeAttendanceAsync(actor, finalizeRequest, cancellationToken);
        var record = result.Records.FirstOrDefault(r => r.Id == id);
        return record ?? throw new ResourceNotFoundException("The finalized attendance record was not found.");
    }

    private static DailyAttendanceSummaryResponse BuildDailyAttendanceSummary(IReadOnlyList<LaborAttendance> records)
    {
        var totalCount = records.Count;
        var workedCount = records.Count(r => r.AttendanceType != AttendanceType.NotWorked);
        var fullDayCount = records.Count(r => r.AttendanceType == AttendanceType.FullDay);
        var halfDayCount = records.Count(r => r.AttendanceType == AttendanceType.HalfDay);
        var hourlyCount = records.Count(r => r.AttendanceType == AttendanceType.Hourly);
        var notWorkedCount = records.Count(r => r.AttendanceType == AttendanceType.NotWorked);
        var estimatedEarnings = records.Sum(r => r.CalculatedAmount ?? 0m);
        var status = totalCount == 0 ? "DRAFT" : records.All(r => r.Status == AttendanceStatus.Finalized) ? "FINALIZED" : "DRAFT";

        return new DailyAttendanceSummaryResponse(
            TotalCount: totalCount,
            WorkedCount: workedCount,
            FullDayCount: fullDayCount,
            HalfDayCount: halfDayCount,
            HourlyCount: hourlyCount,
            NotWorkedCount: notWorkedCount,
            EstimatedEarnings: estimatedEarnings,
            Status: status);
    }

    private static AttendanceType ParseAttendanceType(string attendanceType) =>
        string.IsNullOrWhiteSpace(attendanceType)
            ? throw Validation("attendanceType", "Attendance type is required.")
            : attendanceType.Trim().ToUpperInvariant() switch
            {
                "FULL_DAY" or "FULLDAY" => AttendanceType.FullDay,
                "HALF_DAY" or "HALFDAY" => AttendanceType.HalfDay,
                "HOURLY" => AttendanceType.Hourly,
                "NOT_WORKED" or "NOTWORKED" => AttendanceType.NotWorked,
                _ => throw Validation("attendanceType", $"The attendance type '{attendanceType}' is invalid. Supported values: FULL_DAY, HALF_DAY, HOURLY, NOT_WORKED.")
            };

    private static string FormatAttendanceType(AttendanceType type) => type switch
    {
        AttendanceType.FullDay => "FULL_DAY",
        AttendanceType.HalfDay => "HALF_DAY",
        AttendanceType.Hourly => "HOURLY",
        AttendanceType.NotWorked => "NOT_WORKED",
        _ => type.ToString().ToUpperInvariant()
    };

    private static string FormatAttendanceStatus(AttendanceStatus status) => status switch
    {
        AttendanceStatus.Draft => "DRAFT",
        AttendanceStatus.Finalized => "FINALIZED",
        _ => status.ToString().ToUpperInvariant()
    };

    private static void ValidateWorkingHours(AttendanceType attendanceType, decimal? workingHours)
    {
        if (attendanceType == AttendanceType.Hourly)
        {
            if (!workingHours.HasValue || workingHours.Value <= 0)
            {
                throw Validation("workingHours", "Working hours are required and must be greater than zero for hourly attendance.");
            }
        }
        else
        {
            if (workingHours.HasValue && workingHours.Value > 0)
            {
                throw Validation("workingHours", $"Working hours cannot be specified for {attendanceType} attendance.");
            }
        }
    }

    private static AttendanceRecordResponse MapToResponse(LaborAttendance a, string farmName) =>
        new(
            Id: a.Id,
            OrganizationId: a.OrganizationId,
            FarmId: a.FarmId,
            FarmName: a.Farm?.Name ?? farmName,
            WorkerId: a.WorkerId,
            WorkerDisplayName: a.Worker?.DisplayName ?? string.Empty,
            WorkerFirstName: a.Worker?.FirstName ?? string.Empty,
            WorkerLastName: a.Worker?.LastName,
            Gender: a.Worker != null ? a.Worker.Gender.ToString().ToUpperInvariant() : string.Empty,
            LaborCategoryId: a.Worker?.LaborCategoryId,
            LaborCategoryName: a.Worker?.LaborCategory?.Name,
            EmploymentType: a.Worker != null ? ConvertEmploymentTypeToString(a.Worker.EmploymentType) : string.Empty,
            AttendanceDate: a.AttendanceDate,
            AttendanceType: FormatAttendanceType(a.AttendanceType),
            WorkingHours: a.WorkingHours,
            CalculatedRate: a.CalculatedRate,
            CalculatedAmount: a.CalculatedAmount,
            CurrencyId: a.CurrencyId,
            CurrencyCode: a.Currency?.Code ?? "INR",
            CurrencySymbol: a.Currency?.Symbol ?? "₹",
            Status: FormatAttendanceStatus(a.Status),
            Notes: a.Notes,
            FinalizedAt: a.FinalizedAt,
            FinalizedBy: a.FinalizedBy,
            CreatedAt: a.CreatedAt,
            CreatedBy: a.CreatedBy,
            UpdatedAt: a.UpdatedAt,
            UpdatedBy: a.UpdatedBy);

    private static string ConvertEmploymentTypeToString(EmploymentType type) => type switch
    {
        EmploymentType.Permanent => "PERMANENT",
        EmploymentType.Seasonal => "SEASONAL",
        EmploymentType.DailyWage => "DAILY_WAGE",
        EmploymentType.Contract => "CONTRACT",
        _ => type.ToString().ToUpperInvariant()
    };

    private static int NormalizePageSize(int pageSize) =>
        pageSize == 0 ? DefaultPageSize : pageSize is < 1 or > MaximumPageSize
            ? throw Validation("pageSize", $"Page size must be between 1 and {MaximumPageSize}.")
            : pageSize;

    private static void ValidateActor(AttendanceActor actor)
    {
        if (actor.UserId == Guid.Empty || actor.OrganizationId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("The access token does not contain a valid user scope.");
        }
    }

    private static ValidationException Validation(string propertyName, string message) =>
        new("Validation failed.", new Dictionary<string, string[]> { [propertyName] = [message] });
}
