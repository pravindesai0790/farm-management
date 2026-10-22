using FarmManagement.Application.DTOs.Dashboard;
using FarmManagement.Application.Helpers;
using FarmManagement.Application.Interfaces.Dashboard;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.Services;

public sealed class DashboardService(IDashboardStore store) : IDashboardService
{
    public async Task<DashboardSummaryResponse> GetSummaryAsync(
        DashboardActor actor,
        Guid? farmId,
        CancellationToken cancellationToken = default)
    {
        if (actor.OrganizationId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("An organization is required.");
        }

        var farms = await store.GetFarmsWithAreasAsync(actor.OrganizationId, farmId, cancellationToken);
        var totalAreasCount = await store.GetActiveFarmAreasCountAsync(actor.OrganizationId, farmId, cancellationToken);
        var plantations = await store.GetPlantationsAsync(actor.OrganizationId, farmId, cancellationToken);
        var cycles = await store.GetCyclesAsync(actor.OrganizationId, farmId, cancellationToken);
        var areaUnits = await store.GetAreaUnitsAsync(cancellationToken);

        var targetUnit = DetermineTargetUnit(farms, plantations, areaUnits, farmId);
        var targetSymbol = targetUnit?.Symbol ?? "ac";

        var activePlantations = plantations.Where(p => p.Status == PlantationStatus.Active).ToList();
        var plannedPlantations = plantations.Where(p => p.Status == PlantationStatus.Planned).ToList();

        var totalArea = Math.Round(
            farms.Where(f => f.TotalArea.HasValue)
                 .Sum(f => ConvertArea(f.TotalArea!.Value, f.AreaUnit, targetUnit)),
            2,
            MidpointRounding.AwayFromZero);

        var allocatedArea = Math.Round(
            activePlantations.Sum(p => ConvertArea(p.AllocatedArea, p.AreaUnit, targetUnit)),
            2,
            MidpointRounding.AwayFromZero);

        var availableArea = Math.Max(0m, Math.Round(totalArea - allocatedArea, 2, MidpointRounding.AwayFromZero));
        var utilizationPercentage = totalArea > 0
            ? Math.Round((allocatedArea / totalArea) * 100m, 1, MidpointRounding.AwayFromZero)
            : 0m;

        var activeCyclesCount = cycles.Count(c => c.Status == CropCycleStatus.Active);

        var kpi = new KpiSummaryDto(
            TotalFarms: farms.Count,
            TotalAreas: totalAreasCount,
            TotalArea: totalArea,
            AllocatedArea: allocatedArea,
            AvailableArea: availableArea,
            UtilizationPercentage: utilizationPercentage,
            AreaUnitSymbol: targetSymbol,
            ActivePlantationsCount: activePlantations.Count,
            PlannedPlantationsCount: plannedPlantations.Count,
            ActiveCyclesCount: activeCyclesCount
        );

        var cropAllocations = BuildCropAllocations(activePlantations, targetUnit, targetSymbol, allocatedArea);
        var activeCycleSummaries = BuildActiveCycles(cycles, targetUnit, targetSymbol);
        var farmUtilizations = BuildFarmUtilizations(farms, activePlantations, targetUnit, targetSymbol);
        var currentSeason = DetermineCurrentSeason(cycles);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var now = DateTimeOffset.UtcNow;
        var laborRawData = await store.GetLaborDashboardDataAsync(actor.OrganizationId, farmId, today, cancellationToken);
        var laborDashboard = LaborDashboardHelper.BuildLaborDashboard(laborRawData, farms, farmId, today);

        // Compute high-level alert counts for dashboard triage strip
        DashboardAlertCountsDto? alertCounts = null;
        try
        {
            var priorityRaw = await store.GetPrioritiesRawDataAsync(actor.OrganizationId, farmId, today, now, cancellationToken);
            var overdueSprays = priorityRaw.Sprays.Count(s => IsOverdue(s.ScheduledDateTime, s.PlannedDate, s.Status, today));
            var overdueIrrigations = priorityRaw.Irrigations.Count(i => IsOverdue(i.ScheduledAt, i.PlannedAt, i.Status, today));
            var overdueFertilizers = priorityRaw.Fertilizers.Count(f => IsOverdue(f.ScheduledDate, f.PlannedDate, f.Status, today));
            var overdueLabor = priorityRaw.LaborActivities.Count(l => l.ActivityDate < today && string.Equals(l.Status, "DRAFT", StringComparison.OrdinalIgnoreCase));
            var totalOverdue = overdueSprays + overdueIrrigations + overdueFertilizers + overdueLabor;

            var todaySprays = priorityRaw.Sprays.Count(s => IsToday(s.ScheduledDateTime, s.PlannedDate, today));
            var todayIrrigations = priorityRaw.Irrigations.Count(i => IsToday(i.ScheduledAt, i.PlannedAt, today));
            var todayFertilizers = priorityRaw.Fertilizers.Count(f => IsToday(f.ScheduledDate, f.PlannedDate, today));
            var todayLabor = priorityRaw.LaborActivities.Count(l => l.ActivityDate == today);
            var totalToday = todaySprays + todayIrrigations + todayFertilizers + todayLabor;

            var inProgressCount = priorityRaw.Sprays.Count(s => string.Equals(s.Status, "IN_PROGRESS", StringComparison.OrdinalIgnoreCase))
                + priorityRaw.Irrigations.Count(i => string.Equals(i.Status, "IN_PROGRESS", StringComparison.OrdinalIgnoreCase))
                + priorityRaw.Fertilizers.Count(f => string.Equals(f.Status, "IN_PROGRESS", StringComparison.OrdinalIgnoreCase));

            var activeReiCount = priorityRaw.Sprays.Count(s => s.ReEntryIntervalExpiresAt.HasValue && s.ReEntryIntervalExpiresAt.Value > now);

            alertCounts = new DashboardAlertCountsDto(
                OverdueActivitiesCount: totalOverdue,
                TodayActivitiesCount: totalToday,
                InProgressActivitiesCount: inProgressCount,
                ActiveReiAlertsCount: activeReiCount,
                OverdueInvoicesCount: priorityRaw.OverdueInvoices.Count,
                OutOfStockItemsCount: 0,
                AttendanceStatus: laborDashboard.TodayStatus
            );
        }
        catch
        {
            // Non-blocking for summary endpoint
        }

        return new DashboardSummaryResponse(
            Kpi: kpi,
            CropAllocations: cropAllocations,
            ActiveCycles: activeCycleSummaries,
            FarmUtilizations: farmUtilizations,
            CurrentSeason: currentSeason,
            Labor: laborDashboard,
            AlertCounts: alertCounts
        );
    }

    public async Task<DashboardPrioritiesResponse> GetPrioritiesAsync(
        DashboardActor actor,
        Guid? farmId,
        CancellationToken cancellationToken = default)
    {
        if (actor.OrganizationId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("An organization is required.");
        }

        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.DateTime);

        var raw = await store.GetPrioritiesRawDataAsync(actor.OrganizationId, farmId, today, now, cancellationToken);

        var overdueList = new List<PriorityActivityItemDto>();
        var todayList = new List<PriorityActivityItemDto>();
        var inProgressList = new List<PriorityActivityItemDto>();
        var reiAlerts = new List<ReiSafetyAlertDto>();

        // 1. Sprays
        foreach (var s in raw.Sprays)
        {
            var title = string.IsNullOrWhiteSpace(s.TargetName) ? "Plant Protection Spray" : $"Spray: {s.TargetName}";
            var schedDisplay = FormatScheduledDisplay(s.ScheduledDateTime, s.PlannedDate);

            if (IsOverdue(s.ScheduledDateTime, s.PlannedDate, s.Status, today))
            {
                var daysOverdue = CalculateDaysOverdue(s.ScheduledDateTime, s.PlannedDate, today);
                overdueList.Add(new PriorityActivityItemDto(
                    s.Id, "SPRAY", title, s.FarmId, s.FarmName, s.FarmAreaName, s.CropCycleName, schedDisplay, daysOverdue, s.Status, ResolveActivityRoute("SPRAY", s.Id)));
            }
            else if (IsToday(s.ScheduledDateTime, s.PlannedDate, today) && !string.Equals(s.Status, "COMPLETED", StringComparison.OrdinalIgnoreCase) && !string.Equals(s.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase))
            {
                todayList.Add(new PriorityActivityItemDto(
                    s.Id, "SPRAY", title, s.FarmId, s.FarmName, s.FarmAreaName, s.CropCycleName, schedDisplay, null, s.Status, ResolveActivityRoute("SPRAY", s.Id)));
            }

            if (string.Equals(s.Status, "IN_PROGRESS", StringComparison.OrdinalIgnoreCase))
            {
                inProgressList.Add(new PriorityActivityItemDto(
                    s.Id, "SPRAY", title, s.FarmId, s.FarmName, s.FarmAreaName, s.CropCycleName, schedDisplay, null, s.Status, ResolveActivityRoute("SPRAY", s.Id)));
            }

            if (s.ReEntryIntervalExpiresAt.HasValue && s.ReEntryIntervalExpiresAt.Value > now)
            {
                var remainingMinutes = Math.Max(1, (int)(s.ReEntryIntervalExpiresAt.Value - now).TotalMinutes);
                reiAlerts.Add(new ReiSafetyAlertDto(
                    s.Id, s.TargetName ?? "General Pest/Disease", s.FarmId, s.FarmName, s.FarmAreaName, s.ReEntryIntervalExpiresAt.Value, remainingMinutes, ResolveActivityRoute("SPRAY", s.Id)));
            }
        }

        // 2. Irrigations
        foreach (var i in raw.Irrigations)
        {
            var title = i.WaterQuantity.HasValue && !string.IsNullOrWhiteSpace(i.WaterUnitSymbol)
                ? $"Irrigation ({i.WaterQuantity.Value:N0} {i.WaterUnitSymbol})"
                : "Irrigation Event";
            var schedDisplay = FormatScheduledDisplay(i.ScheduledAt, i.PlannedAt);

            if (IsOverdue(i.ScheduledAt, i.PlannedAt, i.Status, today))
            {
                var daysOverdue = CalculateDaysOverdue(i.ScheduledAt, i.PlannedAt, today);
                overdueList.Add(new PriorityActivityItemDto(
                    i.Id, "IRRIGATION", title, i.FarmId, i.FarmName, i.FarmAreaName, i.CropCycleName, schedDisplay, daysOverdue, i.Status, ResolveActivityRoute("IRRIGATION", i.Id)));
            }
            else if (IsToday(i.ScheduledAt, i.PlannedAt, today) && !string.Equals(i.Status, "COMPLETED", StringComparison.OrdinalIgnoreCase) && !string.Equals(i.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase))
            {
                todayList.Add(new PriorityActivityItemDto(
                    i.Id, "IRRIGATION", title, i.FarmId, i.FarmName, i.FarmAreaName, i.CropCycleName, schedDisplay, null, i.Status, ResolveActivityRoute("IRRIGATION", i.Id)));
            }

            if (string.Equals(i.Status, "IN_PROGRESS", StringComparison.OrdinalIgnoreCase))
            {
                inProgressList.Add(new PriorityActivityItemDto(
                    i.Id, "IRRIGATION", title, i.FarmId, i.FarmName, i.FarmAreaName, i.CropCycleName, schedDisplay, null, i.Status, ResolveActivityRoute("IRRIGATION", i.Id)));
            }
        }

        // 3. Fertilizers
        foreach (var f in raw.Fertilizers)
        {
            var title = !string.IsNullOrWhiteSpace(f.ApplicationMethodName)
                ? $"Fertilizer ({f.ApplicationMethodName}) #{f.ReferenceNumber}"
                : $"Fertilizer Application #{f.ReferenceNumber}";
            var schedDisplay = FormatScheduledDisplay(f.ScheduledDate, f.PlannedDate);

            if (IsOverdue(f.ScheduledDate, f.PlannedDate, f.Status, today))
            {
                var daysOverdue = CalculateDaysOverdue(f.ScheduledDate, f.PlannedDate, today);
                overdueList.Add(new PriorityActivityItemDto(
                    f.Id, "FERTILIZER", title, f.FarmId, f.FarmName, f.FarmAreaName, f.CropCycleName, schedDisplay, daysOverdue, f.Status, ResolveActivityRoute("FERTILIZER", f.Id)));
            }
            else if (IsToday(f.ScheduledDate, f.PlannedDate, today) && !string.Equals(f.Status, "COMPLETED", StringComparison.OrdinalIgnoreCase) && !string.Equals(f.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase))
            {
                todayList.Add(new PriorityActivityItemDto(
                    f.Id, "FERTILIZER", title, f.FarmId, f.FarmName, f.FarmAreaName, f.CropCycleName, schedDisplay, null, f.Status, ResolveActivityRoute("FERTILIZER", f.Id)));
            }

            if (string.Equals(f.Status, "IN_PROGRESS", StringComparison.OrdinalIgnoreCase))
            {
                inProgressList.Add(new PriorityActivityItemDto(
                    f.Id, "FERTILIZER", title, f.FarmId, f.FarmName, f.FarmAreaName, f.CropCycleName, schedDisplay, null, f.Status, ResolveActivityRoute("FERTILIZER", f.Id)));
            }
        }

        // 4. Labor Activities
        foreach (var l in raw.LaborActivities)
        {
            var title = $"Labor: {l.ActivityTypeName}";
            var schedDisplay = l.ActivityDate.ToString("dd-MMM-yyyy");

            if (l.ActivityDate < today && string.Equals(l.Status, "DRAFT", StringComparison.OrdinalIgnoreCase))
            {
                var daysOverdue = today.DayNumber - l.ActivityDate.DayNumber;
                overdueList.Add(new PriorityActivityItemDto(
                    l.Id, "LABOR", title, l.FarmId, l.FarmName, l.FarmAreaName, l.CropCycleName, schedDisplay, daysOverdue, l.Status, ResolveActivityRoute("LABOR", l.Id)));
            }
            else if (l.ActivityDate == today)
            {
                todayList.Add(new PriorityActivityItemDto(
                    l.Id, "LABOR", title, l.FarmId, l.FarmName, l.FarmAreaName, l.CropCycleName, schedDisplay, null, l.Status, ResolveActivityRoute("LABOR", l.Id)));
            }
        }

        // Attendance Alert
        var todayAttendance = raw.LaborRawData.AttendanceItems.Where(i => i.AttendanceDate == today).ToList();
        var attendanceStatus = todayAttendance.Count == 0
            ? "NOT_RECORDED"
            : todayAttendance.All(i => string.Equals(i.Status, "FINALIZED", StringComparison.OrdinalIgnoreCase))
                ? "FINALIZED"
                : "DRAFT";

        var workedWorkersCount = todayAttendance.Count(i => !string.Equals(i.AttendanceType, "NOT_WORKED", StringComparison.OrdinalIgnoreCase) && !string.Equals(i.AttendanceType, "NOTWORKED", StringComparison.OrdinalIgnoreCase));
        var todayWageExpense = todayAttendance.Sum(i => i.CalculatedAmount ?? 0m);
        var currencySymbol = todayAttendance.FirstOrDefault(i => !string.IsNullOrEmpty(i.CurrencySymbol))?.CurrencySymbol
            ?? raw.LaborRawData.AttendanceItems.FirstOrDefault(i => !string.IsNullOrEmpty(i.CurrencySymbol))?.CurrencySymbol
            ?? "₹";

        var attendanceAlert = new LaborAttendanceAlertDto(
            attendanceStatus,
            workedWorkersCount,
            raw.LaborRawData.TotalAssignedCount,
            todayWageExpense,
            currencySymbol
        );

        // Overdue Invoices
        var overdueInvoicesCount = raw.OverdueInvoices.Count;
        var overdueInvoicesTotal = raw.OverdueInvoices.Sum(i => i.OutstandingBalance);
        var invoiceCurrencySymbol = raw.OverdueInvoices.FirstOrDefault()?.CurrencySymbol ?? "₹";

        var totalAlerts = overdueList.Count
            + reiAlerts.Count
            + (attendanceStatus == "NOT_RECORDED" ? 1 : 0)
            + (overdueInvoicesCount > 0 ? 1 : 0);

        return new DashboardPrioritiesResponse(
            TotalAlertsCount: totalAlerts,
            OverdueActivities: overdueList.OrderByDescending(x => x.DaysOverdue ?? 0).ToList(),
            TodayActivities: todayList,
            InProgressActivities: inProgressList,
            ActiveReiAlerts: reiAlerts.OrderBy(r => r.RemainingMinutes).ToList(),
            AttendanceAlert: attendanceAlert,
            OverdueInvoicesCount: overdueInvoicesCount,
            OverdueInvoicesTotalAmount: overdueInvoicesTotal,
            OverdueInvoicesCurrencySymbol: invoiceCurrencySymbol
        );
    }

    public async Task<DashboardUpcomingWorkResponse> GetUpcomingWorkAsync(
        DashboardActor actor,
        Guid? farmId,
        int days = 7,
        CancellationToken cancellationToken = default)
    {
        if (actor.OrganizationId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("An organization is required.");
        }

        if (days < 1) days = 7;
        if (days > 30) days = 30;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var endDate = today.AddDays(days);

        var raw = await store.GetUpcomingActivitiesAsync(actor.OrganizationId, farmId, today, endDate, cancellationToken);

        var items = raw.Select(r => new UpcomingWorkItemDto(
            r.Id,
            r.ActivityType,
            r.Title,
            r.FarmId,
            r.FarmName,
            r.FarmAreaName,
            r.CropCycleName,
            r.ScheduledDate,
            r.ScheduledTime,
            r.Status,
            r.Details,
            ResolveActivityRoute(r.ActivityType, r.Id)
        )).OrderBy(r => r.ScheduledDate).ThenBy(r => r.ScheduledTime ?? string.Empty).ToList();

        return new DashboardUpcomingWorkResponse(items);
    }

    public async Task<DashboardInventorySummaryResponse> GetInventorySummaryAsync(
        DashboardActor actor,
        Guid? farmId,
        CancellationToken cancellationToken = default)
    {
        if (actor.OrganizationId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("An organization is required.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var consumptionStartDate = today.AddDays(-7);

        var raw = await store.GetInventoryRawDataAsync(actor.OrganizationId, farmId, consumptionStartDate, cancellationToken);

        var outOfStockDtos = raw.OutOfStockItems.Select(i => new OutOfStockItemDto(
            i.InventoryItemId,
            i.ItemName,
            i.Sku,
            i.CategoryName,
            i.StorageLocationId,
            i.StorageLocationName,
            i.FarmId,
            i.FarmName,
            i.StockUnitSymbol
        )).ToList();

        var consumptionDtos = raw.RecentConsumptions.Select(c => new RecentStockConsumptionDto(
            c.InventoryItemId,
            c.ItemName,
            c.CategoryName,
            c.Quantity,
            c.StockUnitSymbol
        )).ToList();

        return new DashboardInventorySummaryResponse(
            outOfStockDtos.Count,
            outOfStockDtos,
            consumptionDtos,
            raw.TotalItemsCount,
            raw.TotalLocationsCount
        );
    }

    public async Task<DashboardFinancialSummaryResponse> GetFinancialSummaryAsync(
        DashboardActor actor,
        Guid? farmId,
        CancellationToken cancellationToken = default)
    {
        if (actor.OrganizationId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("An organization is required.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monthStartDate = new DateOnly(today.Year, today.Month, 1);

        var raw = await store.GetFinancialRawDataAsync(actor.OrganizationId, farmId, monthStartDate, today, cancellationToken);

        var currencyIds = raw.DirectExpenses.Select(e => e.CurrencyId)
            .Union(raw.Invoices.Select(i => i.CurrencyId))
            .Distinct()
            .ToList();

        var currencySummaries = new List<CurrencyFinancialSummaryDto>();

        foreach (var currId in currencyIds)
        {
            var directExpenses = raw.DirectExpenses.Where(e => e.CurrencyId == currId).ToList();
            var invoices = raw.Invoices.Where(i => i.CurrencyId == currId).ToList();

            var code = directExpenses.FirstOrDefault()?.CurrencyCode ?? invoices.FirstOrDefault()?.CurrencyCode ?? "INR";
            var symbol = directExpenses.FirstOrDefault()?.CurrencySymbol ?? invoices.FirstOrDefault()?.CurrencySymbol ?? "₹";

            var monthDirect = directExpenses.Sum(e => e.Amount);
            var monthInvoiced = invoices.Where(i => i.IsThisMonth).Sum(i => i.TotalAmount);
            var openPayables = invoices.Sum(i => i.OutstandingBalance);
            var overdueList = invoices.Where(i => i.IsOverdue).ToList();

            currencySummaries.Add(new CurrencyFinancialSummaryDto(
                currId,
                code,
                symbol,
                monthDirect,
                monthInvoiced,
                openPayables,
                overdueList.Count,
                overdueList.Sum(i => i.OutstandingBalance)
            ));
        }

        return new DashboardFinancialSummaryResponse(currencySummaries);
    }

    public async Task<DashboardRecentActivityResponse> GetRecentActivityAsync(
        DashboardActor actor,
        Guid? farmId,
        int limit = 15,
        CancellationToken cancellationToken = default)
    {
        if (actor.OrganizationId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("An organization is required.");
        }

        if (limit < 1) limit = 15;
        if (limit > 50) limit = 50;

        var raw = await store.GetRecentActivitiesAsync(actor.OrganizationId, farmId, limit, cancellationToken);

        var items = raw.Select(r => new RecentActivityItemDto(
            r.Id,
            r.EventType,
            r.Title,
            r.Description,
            r.Timestamp,
            r.FarmId,
            r.FarmName,
            r.PerformedBy,
            ResolveRecentActivityRoute(r.EventType, r.Id)
        )).OrderByDescending(r => r.Timestamp).Take(limit).ToList();

        return new DashboardRecentActivityResponse(items);
    }

    private static string ResolveActivityRoute(string activityType, Guid id) => activityType.ToUpperInvariant() switch
    {
        "SPRAY" => $"/sprays/{id}",
        "IRRIGATION" => $"/irrigations/{id}",
        "FERTILIZER" => $"/fertilizer-applications/{id}",
        "LABOR" => $"/activities/labor-activities/{id}",
        _ => "/activities"
    };

    private static string ResolveRecentActivityRoute(string eventType, Guid id) => eventType switch
    {
        "SPRAY_COMPLETED" => $"/sprays/{id}",
        "IRRIGATION_COMPLETED" => $"/irrigations/{id}",
        "FERTILIZER_COMPLETED" => $"/fertilizer-applications/{id}",
        "ATTENDANCE_FINALIZED" => "/labor/attendance",
        "STOCK_RECEIVED" => "/inventory/ledger",
        _ => "/dashboard"
    };

    private static bool IsOverdue(DateTimeOffset? scheduled, DateTimeOffset? planned, string status, DateOnly today)
    {
        if (string.Equals(status, "COMPLETED", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, "CANCELLED", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (scheduled.HasValue)
        {
            return DateOnly.FromDateTime(scheduled.Value.DateTime) < today;
        }

        if (planned.HasValue)
        {
            return DateOnly.FromDateTime(planned.Value.DateTime) < today;
        }

        return false;
    }

    private static bool IsOverdue(DateTimeOffset? scheduled, DateOnly? planned, string status, DateOnly today)
    {
        if (string.Equals(status, "COMPLETED", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, "CANCELLED", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (scheduled.HasValue)
        {
            return DateOnly.FromDateTime(scheduled.Value.DateTime) < today;
        }

        if (planned.HasValue)
        {
            return planned.Value < today;
        }

        return false;
    }

    private static bool IsToday(DateTimeOffset? scheduled, DateTimeOffset? planned, DateOnly today)
    {
        if (scheduled.HasValue)
        {
            return DateOnly.FromDateTime(scheduled.Value.DateTime) == today;
        }

        if (planned.HasValue)
        {
            return DateOnly.FromDateTime(planned.Value.DateTime) == today;
        }

        return false;
    }

    private static bool IsToday(DateTimeOffset? scheduled, DateOnly? planned, DateOnly today)
    {
        if (scheduled.HasValue)
        {
            return DateOnly.FromDateTime(scheduled.Value.DateTime) == today;
        }

        if (planned.HasValue)
        {
            return planned.Value == today;
        }

        return false;
    }

    private static int CalculateDaysOverdue(DateTimeOffset? scheduled, DateTimeOffset? planned, DateOnly today)
    {
        if (scheduled.HasValue)
        {
            return Math.Max(1, today.DayNumber - DateOnly.FromDateTime(scheduled.Value.DateTime).DayNumber);
        }

        if (planned.HasValue)
        {
            return Math.Max(1, today.DayNumber - DateOnly.FromDateTime(planned.Value.DateTime).DayNumber);
        }

        return 1;
    }

    private static int CalculateDaysOverdue(DateTimeOffset? scheduled, DateOnly? planned, DateOnly today)
    {
        if (scheduled.HasValue)
        {
            return Math.Max(1, today.DayNumber - DateOnly.FromDateTime(scheduled.Value.DateTime).DayNumber);
        }

        if (planned.HasValue)
        {
            return Math.Max(1, today.DayNumber - planned.Value.DayNumber);
        }

        return 1;
    }

    private static string FormatScheduledDisplay(DateTimeOffset? scheduled, DateTimeOffset? planned)
    {
        if (scheduled.HasValue)
        {
            return scheduled.Value.ToString("dd-MMM-yyyy HH:mm");
        }

        if (planned.HasValue)
        {
            return planned.Value.ToString("dd-MMM-yyyy");
        }

        return "Unscheduled";
    }

    private static string FormatScheduledDisplay(DateTimeOffset? scheduled, DateOnly? planned)
    {
        if (scheduled.HasValue)
        {
            return scheduled.Value.ToString("dd-MMM-yyyy HH:mm");
        }

        if (planned.HasValue)
        {
            return planned.Value.ToString("dd-MMM-yyyy");
        }

        return "Unscheduled";
    }

    private static IReadOnlyList<CropAllocationSummaryDto> BuildCropAllocations(
        List<CropPlantation> activePlantations,
        Unit? targetUnit,
        string targetSymbol,
        decimal totalAllocatedArea)
    {
        return activePlantations
            .GroupBy(p => p.CropId)
            .Select(cg =>
            {
                var first = cg.First();
                var cropName = first.Crop?.Name ?? "Unknown Crop";
                var cropArea = Math.Round(
                    cg.Sum(p => ConvertArea(p.AllocatedArea, p.AreaUnit, targetUnit)),
                    2,
                    MidpointRounding.AwayFromZero);

                var cropPct = totalAllocatedArea > 0
                    ? Math.Round((cropArea / totalAllocatedArea) * 100m, 1, MidpointRounding.AwayFromZero)
                    : 0m;

                var varieties = cg
                    .GroupBy(p => p.VarietyId)
                    .Select(vg =>
                    {
                        var varFirst = vg.First();
                        var varietyName = varFirst.Variety?.Name ?? "Standard / Unspecified";
                        var varArea = Math.Round(
                            vg.Sum(p => ConvertArea(p.AllocatedArea, p.AreaUnit, targetUnit)),
                            2,
                            MidpointRounding.AwayFromZero);
                        var varPct = cropArea > 0
                            ? Math.Round((varArea / cropArea) * 100m, 1, MidpointRounding.AwayFromZero)
                            : 0m;

                        return new VarietyAllocationSummaryDto(
                            VarietyId: vg.Key,
                            VarietyName: varietyName,
                            AllocatedArea: varArea,
                            AreaUnitSymbol: targetSymbol,
                            PercentageOfCrop: varPct
                        );
                    })
                    .OrderByDescending(v => v.AllocatedArea)
                    .ToList();

                return new CropAllocationSummaryDto(
                    CropId: cg.Key,
                    CropName: cropName,
                    TotalAllocatedArea: cropArea,
                    AreaUnitSymbol: targetSymbol,
                    PercentageOfAllocated: cropPct,
                    Varieties: varieties
                );
            })
            .OrderByDescending(c => c.TotalAllocatedArea)
            .ToList();
    }

    private static IReadOnlyList<ActiveCycleSummaryDto> BuildActiveCycles(
        IReadOnlyList<CropCycle> cycles,
        Unit? targetUnit,
        string targetSymbol)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return cycles
            .Where(c => c.Status == CropCycleStatus.Active || c.Status == CropCycleStatus.Planned)
            .OrderBy(c => c.Status == CropCycleStatus.Active ? 0 : 1)
            .ThenByDescending(c => c.PlannedStartDate)
            .Take(8)
            .Select(c =>
            {
                var plantation = c.Plantation;
                var startDate = c.ActualStartDate ?? c.PlannedStartDate;
                var endDate = c.ActualEndDate ?? c.ExpectedEndDate;

                int? progressPct = null;
                if (endDate.HasValue && endDate.Value > startDate)
                {
                    var totalDays = endDate.Value.DayNumber - startDate.DayNumber;
                    var elapsedDays = today.DayNumber - startDate.DayNumber;
                    if (elapsedDays <= 0)
                    {
                        progressPct = 0;
                    }
                    else if (elapsedDays >= totalDays)
                    {
                        progressPct = 100;
                    }
                    else
                    {
                        progressPct = (int)Math.Round((decimal)elapsedDays / totalDays * 100m);
                    }
                }

                var plantationArea = plantation != null
                    ? Math.Round(ConvertArea(plantation.AllocatedArea, plantation.AreaUnit, targetUnit), 2, MidpointRounding.AwayFromZero)
                    : 0m;

                string? currentStageName = null;
                int? currentStageSequence = null;
                int totalStagesCount = 0;
                int completedStagesCount = 0;
                List<ActiveCycleStageSummaryDto> stageDtos = [];

                if (c.Stages != null && c.Stages.Count > 0)
                {
                    var sortedStages = c.Stages.OrderBy(s => s.SequenceNumber).ToList();
                    totalStagesCount = sortedStages.Count;
                    completedStagesCount = sortedStages.Count(s => s.Status == CropCycleStageStatus.Completed);

                    var inProgress = sortedStages.FirstOrDefault(s => s.Status == CropCycleStageStatus.InProgress);
                    var current = inProgress ?? sortedStages.FirstOrDefault(s => s.Status == CropCycleStageStatus.NotStarted);
                    currentStageName = current?.StageName;
                    currentStageSequence = current?.SequenceNumber;

                    stageDtos = sortedStages.Select(s => new ActiveCycleStageSummaryDto(
                        s.Id,
                        s.StageName,
                        s.SequenceNumber,
                        s.Status.ToString().ToUpperInvariant(),
                        s.PlannedStartDate,
                        s.PlannedEndDate,
                        s.ActualStartDate,
                        s.ActualEndDate
                    )).ToList();

                    if (totalStagesCount > 0)
                    {
                        progressPct = (int)Math.Round((double)completedStagesCount / totalStagesCount * 100);
                    }
                }
                else if (c.LifecycleTemplate?.Stages != null)
                {
                    var templateStages = c.LifecycleTemplate.Stages
                        .Where(s => s.IsActive)
                        .OrderBy(s => s.SequenceNumber)
                        .ToList();

                    if (templateStages.Count > 0)
                    {
                        totalStagesCount = templateStages.Count;
                        currentStageName = templateStages[0].StageName;
                        currentStageSequence = templateStages[0].SequenceNumber;
                        stageDtos = templateStages.Select(s => new ActiveCycleStageSummaryDto(
                            Guid.Empty,
                            s.StageName,
                            s.SequenceNumber,
                            "NOT_STARTED",
                            null,
                            null,
                            null,
                            null
                        )).ToList();
                    }
                }

                return new ActiveCycleSummaryDto(
                    CycleId: c.Id,
                    CycleName: c.CycleName,
                    SeasonYear: c.SeasonYear,
                    SeasonName: c.SeasonName,
                    PlantationId: c.PlantationId,
                    PlantationName: plantation?.PlantationName ?? "N/A",
                    FarmName: plantation?.Farm?.Name ?? "N/A",
                    FarmAreaName: plantation?.FarmArea?.Name ?? "N/A",
                    CropName: plantation?.Crop?.Name ?? "Unknown Crop",
                    VarietyName: plantation?.Variety?.Name,
                    AllocatedArea: plantationArea,
                    AreaUnitSymbol: targetSymbol,
                    StartDate: startDate,
                    ExpectedEndDate: c.ExpectedEndDate,
                    ProgressPercentage: progressPct,
                    Status: c.Status.ToString().ToUpperInvariant(),
                    CurrentStageName: currentStageName,
                    CurrentStageSequence: currentStageSequence,
                    TotalStagesCount: totalStagesCount,
                    CompletedStagesCount: completedStagesCount,
                    Stages: stageDtos
                );
            })
            .ToList();
    }

    private static IReadOnlyList<FarmUtilizationSummaryDto> BuildFarmUtilizations(
        IReadOnlyList<Farm> farms,
        List<CropPlantation> activePlantations,
        Unit? targetUnit,
        string targetSymbol)
    {
        return farms
            .Select(f =>
            {
                var farmTotal = f.TotalArea.HasValue
                    ? Math.Round(ConvertArea(f.TotalArea.Value, f.AreaUnit, targetUnit), 2, MidpointRounding.AwayFromZero)
                    : 0m;

                var farmPlantations = activePlantations.Where(p => p.FarmId == f.Id).ToList();
                var farmAllocated = Math.Round(
                    farmPlantations.Sum(p => ConvertArea(p.AllocatedArea, p.AreaUnit, targetUnit)),
                    2,
                    MidpointRounding.AwayFromZero);

                var farmAvailable = Math.Max(0m, Math.Round(farmTotal - farmAllocated, 2, MidpointRounding.AwayFromZero));
                var farmUtilizationPct = farmTotal > 0
                    ? Math.Round((farmAllocated / farmTotal) * 100m, 1, MidpointRounding.AwayFromZero)
                    : 0m;

                return new FarmUtilizationSummaryDto(
                    FarmId: f.Id,
                    FarmName: f.Name,
                    TotalArea: farmTotal,
                    AllocatedArea: farmAllocated,
                    AvailableArea: farmAvailable,
                    UtilizationPercentage: farmUtilizationPct,
                    AreaUnitSymbol: targetSymbol,
                    ActivePlantationsCount: farmPlantations.Count
                );
            })
            .OrderByDescending(f => f.TotalArea)
            .ToList();
    }

    private static string? DetermineCurrentSeason(IReadOnlyList<CropCycle> cycles)
    {
        var activeWithSeason = cycles
            .Where(c => c.Status == CropCycleStatus.Active && !string.IsNullOrWhiteSpace(c.SeasonName))
            .Select(c => c.SeasonName!.Trim())
            .FirstOrDefault();

        if (activeWithSeason is not null)
        {
            return activeWithSeason;
        }

        var anyWithSeason = cycles
            .Where(c => !string.IsNullOrWhiteSpace(c.SeasonName))
            .Select(c => c.SeasonName!.Trim())
            .FirstOrDefault();

        if (anyWithSeason is not null)
        {
            return anyWithSeason;
        }

        if (cycles.Any())
        {
            return $"Season {cycles.Max(c => c.SeasonYear)}";
        }

        return $"Season {DateTime.UtcNow.Year}";
    }

    private static Unit? DetermineTargetUnit(
        IReadOnlyList<Farm> farms,
        IReadOnlyList<CropPlantation> plantations,
        IReadOnlyList<Unit> areaUnits,
        Guid? farmId)
    {
        if (farmId.HasValue)
        {
            var selectedFarm = farms.FirstOrDefault(f => f.Id == farmId.Value);
            if (selectedFarm?.AreaUnit != null)
            {
                return selectedFarm.AreaUnit;
            }
        }

        var mostFrequentFarmUnit = farms
            .Where(f => f.AreaUnit != null)
            .GroupBy(f => f.AreaUnit!)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault();

        if (mostFrequentFarmUnit != null)
        {
            return mostFrequentFarmUnit;
        }

        var mostFrequentPlantationUnit = plantations
            .Where(p => p.AreaUnit != null)
            .GroupBy(p => p.AreaUnit!)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault();

        if (mostFrequentPlantationUnit != null)
        {
            return mostFrequentPlantationUnit;
        }

        return areaUnits.FirstOrDefault(u => u.Code == "ACRE")
            ?? areaUnits.FirstOrDefault();
    }

    private static decimal ConvertArea(decimal amount, Unit? sourceUnit, Unit? targetUnit)
    {
        if (sourceUnit is null || targetUnit is null || sourceUnit.Id == targetUnit.Id)
        {
            return amount;
        }

        if (sourceUnit.ConversionFactor is > 0 && targetUnit.ConversionFactor is > 0)
        {
            var baseSqMeters = amount * sourceUnit.ConversionFactor.Value;
            return baseSqMeters / targetUnit.ConversionFactor.Value;
        }

        return amount;
    }
}
