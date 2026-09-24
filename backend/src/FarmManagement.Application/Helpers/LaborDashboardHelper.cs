using FarmManagement.Application.DTOs.Dashboard;
using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Helpers;

public static class LaborDashboardHelper
{
    public static FarmLaborDashboardDto BuildLaborDashboard(
        LaborDashboardRawData rawData,
        IReadOnlyList<Farm> farms,
        Guid? farmId,
        DateOnly today)
    {
        var items = rawData.AttendanceItems;
        var todayItems = items.Where(i => i.AttendanceDate == today).ToList();

        var todayStatus = ResolveStatus(todayItems);
        var assignedCount = farmId.HasValue
            ? (rawData.FarmAssignedCounts.TryGetValue(farmId.Value, out var count) ? count : 0)
            : rawData.TotalAssignedCount;

        var workedWorkersCount = todayItems.Count(i => !IsNotWorked(i.AttendanceType));
        var notWorkedCount = todayItems.Count(i => IsNotWorked(i.AttendanceType));
        var fullDayCount = todayItems.Count(i => IsFullDay(i.AttendanceType));
        var halfDayCount = todayItems.Count(i => IsHalfDay(i.AttendanceType));
        var hourlyCount = todayItems.Count(i => IsHourly(i.AttendanceType));
        var totalWorkingHours = todayItems.Where(i => IsHourly(i.AttendanceType)).Sum(i => i.WorkingHours ?? 0m);
        var todayWageExpense = todayItems.Sum(i => i.CalculatedAmount ?? 0m);
        var currencySymbol = todayItems.FirstOrDefault(i => !string.IsNullOrEmpty(i.CurrencySymbol))?.CurrencySymbol
                             ?? items.FirstOrDefault(i => !string.IsNullOrEmpty(i.CurrencySymbol))?.CurrencySymbol
                             ?? "₹";

        DateTimeOffset? finalizedAt = todayItems.Where(i => i.FinalizedAt.HasValue)
            .Max(i => i.FinalizedAt);

        var skillMix = todayItems
            .Where(i => !IsNotWorked(i.AttendanceType))
            .GroupBy(i => string.IsNullOrWhiteSpace(i.CategoryName) ? "General Labor" : i.CategoryName)
            .Select(g => new LaborCategoryMixDto(g.Key, g.Count()))
            .OrderByDescending(c => c.WorkerCount)
            .ToList();

        var recentDaysTrend = Build7DayTrend(items, today);

        IReadOnlyList<FarmLaborStatusSummaryDto>? farmSummaries = null;
        if (!farmId.HasValue)
        {
            farmSummaries = BuildFarmSummaries(items, rawData.FarmAssignedCounts, farms, today);
        }

        return new FarmLaborDashboardDto(
            TodayStatus: todayStatus,
            AssignedWorkersCount: assignedCount,
            WorkedWorkersCount: workedWorkersCount,
            NotWorkedCount: notWorkedCount,
            FullDayCount: fullDayCount,
            HalfDayCount: halfDayCount,
            HourlyCount: hourlyCount,
            TotalWorkingHours: totalWorkingHours,
            TodayWageExpense: todayWageExpense,
            CurrencySymbol: currencySymbol,
            FinalizedAt: finalizedAt,
            SkillMix: skillMix,
            RecentDaysTrend: recentDaysTrend,
            FarmSummaries: farmSummaries
        );
    }

    private static IReadOnlyList<DailyLaborTrendDto> Build7DayTrend(
        IReadOnlyList<RawLaborAttendanceItem> items,
        DateOnly today)
    {
        var trend = new List<DailyLaborTrendDto>(7);
        for (var i = 6; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            var dayItems = items.Where(x => x.AttendanceDate == date).ToList();
            var workedCount = dayItems.Count(x => !IsNotWorked(x.AttendanceType));
            var wages = dayItems.Sum(x => x.CalculatedAmount ?? 0m);
            var status = ResolveStatus(dayItems);

            trend.Add(new DailyLaborTrendDto(date, workedCount, wages, status));
        }

        return trend;
    }

    private static IReadOnlyList<FarmLaborStatusSummaryDto> BuildFarmSummaries(
        IReadOnlyList<RawLaborAttendanceItem> items,
        IReadOnlyDictionary<Guid, int> assignedCounts,
        IReadOnlyList<Farm> farms,
        DateOnly today)
    {
        return farms.Select(f =>
        {
            var farmTodayItems = items.Where(x => x.FarmId == f.Id && x.AttendanceDate == today).ToList();
            var assigned = assignedCounts.TryGetValue(f.Id, out var c) ? c : 0;
            var worked = farmTodayItems.Count(x => !IsNotWorked(x.AttendanceType));
            var wages = farmTodayItems.Sum(x => x.CalculatedAmount ?? 0m);
            var status = ResolveStatus(farmTodayItems);

            return new FarmLaborStatusSummaryDto(
                FarmId: f.Id,
                FarmName: f.Name,
                AssignedCount: assigned,
                WorkedCount: worked,
                TodayWageExpense: wages,
                Status: status
            );
        }).OrderByDescending(x => x.WorkedCount).ThenBy(x => x.FarmName).ToList();
    }

    private static string ResolveStatus(IReadOnlyList<RawLaborAttendanceItem> items)
    {
        if (items.Count == 0)
        {
            return "NOT_RECORDED";
        }

        if (items.All(i => string.Equals(i.Status, "FINALIZED", StringComparison.OrdinalIgnoreCase)))
        {
            return "FINALIZED";
        }

        return "DRAFT";
    }

    private static bool IsNotWorked(string type) =>
        string.Equals(type, "NOT_WORKED", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(type, "NOTWORKED", StringComparison.OrdinalIgnoreCase);

    private static bool IsFullDay(string type) =>
        string.Equals(type, "FULL_DAY", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(type, "FULLDAY", StringComparison.OrdinalIgnoreCase);

    private static bool IsHalfDay(string type) =>
        string.Equals(type, "HALF_DAY", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(type, "HALFDAY", StringComparison.OrdinalIgnoreCase);

    private static bool IsHourly(string type) =>
        string.Equals(type, "HOURLY", StringComparison.OrdinalIgnoreCase);
}
