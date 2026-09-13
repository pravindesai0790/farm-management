namespace FarmManagement.Application.DTOs.Dashboard;

public sealed record LaborCategoryMixDto(
    string CategoryName,
    int WorkerCount
);

public sealed record DailyLaborTrendDto(
    DateOnly Date,
    int WorkedCount,
    decimal WageExpense,
    string Status
);

public sealed record FarmLaborStatusSummaryDto(
    Guid FarmId,
    string FarmName,
    string FarmCode,
    int AssignedCount,
    int WorkedCount,
    decimal TodayWageExpense,
    string Status
);

public sealed record FarmLaborDashboardDto(
    string TodayStatus,
    int AssignedWorkersCount,
    int WorkedWorkersCount,
    int NotWorkedCount,
    int FullDayCount,
    int HalfDayCount,
    int HourlyCount,
    decimal TotalWorkingHours,
    decimal TodayWageExpense,
    string CurrencySymbol,
    DateTimeOffset? FinalizedAt,
    IReadOnlyList<LaborCategoryMixDto> SkillMix,
    IReadOnlyList<DailyLaborTrendDto> RecentDaysTrend,
    IReadOnlyList<FarmLaborStatusSummaryDto>? FarmSummaries
);

public sealed record RawLaborAttendanceItem(
    Guid FarmId,
    DateOnly AttendanceDate,
    string AttendanceType,
    decimal? WorkingHours,
    decimal? CalculatedAmount,
    string Status,
    DateTimeOffset? FinalizedAt,
    string CategoryName,
    string CurrencySymbol
);

public sealed record LaborDashboardRawData(
    IReadOnlyList<RawLaborAttendanceItem> AttendanceItems,
    IReadOnlyDictionary<Guid, int> FarmAssignedCounts,
    int TotalAssignedCount
);
