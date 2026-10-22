namespace FarmManagement.Application.DTOs.Dashboard;

public sealed record DashboardActor(Guid UserId, Guid OrganizationId);

public sealed record DashboardSummaryResponse(
    KpiSummaryDto Kpi,
    IReadOnlyList<CropAllocationSummaryDto> CropAllocations,
    IReadOnlyList<ActiveCycleSummaryDto> ActiveCycles,
    IReadOnlyList<FarmUtilizationSummaryDto> FarmUtilizations,
    string? CurrentSeason,
    FarmLaborDashboardDto? Labor = null,
    DashboardAlertCountsDto? AlertCounts = null
);

public sealed record DashboardAlertCountsDto(
    int OverdueActivitiesCount,
    int TodayActivitiesCount,
    int InProgressActivitiesCount,
    int ActiveReiAlertsCount,
    int OverdueInvoicesCount,
    int OutOfStockItemsCount,
    string? AttendanceStatus
);

public sealed record KpiSummaryDto(
    int TotalFarms,
    int TotalAreas,
    decimal TotalArea,
    decimal AllocatedArea,
    decimal AvailableArea,
    decimal UtilizationPercentage,
    string AreaUnitSymbol,
    int ActivePlantationsCount,
    int PlannedPlantationsCount,
    int ActiveCyclesCount
);

public sealed record CropAllocationSummaryDto(
    Guid CropId,
    string CropName,
    decimal TotalAllocatedArea,
    string AreaUnitSymbol,
    decimal PercentageOfAllocated,
    IReadOnlyList<VarietyAllocationSummaryDto> Varieties
);

public sealed record VarietyAllocationSummaryDto(
    Guid? VarietyId,
    string VarietyName,
    decimal AllocatedArea,
    string AreaUnitSymbol,
    decimal PercentageOfCrop
);

public sealed record ActiveCycleStageSummaryDto(
    Guid StageId,
    string StageName,
    int SequenceNumber,
    string Status,
    DateOnly? PlannedStartDate,
    DateOnly? PlannedEndDate,
    DateOnly? ActualStartDate,
    DateOnly? ActualEndDate
);

public sealed record ActiveCycleSummaryDto(
    Guid CycleId,
    string CycleName,
    int SeasonYear,
    string? SeasonName,
    Guid PlantationId,
    string PlantationName,
    string FarmName,
    string FarmAreaName,
    string CropName,
    string? VarietyName,
    decimal AllocatedArea,
    string AreaUnitSymbol,
    DateOnly StartDate,
    DateOnly? ExpectedEndDate,
    int? ProgressPercentage,
    string Status,
    string? CurrentStageName = null,
    int? CurrentStageSequence = null,
    int TotalStagesCount = 0,
    int CompletedStagesCount = 0,
    IReadOnlyList<ActiveCycleStageSummaryDto>? Stages = null
);

public sealed record FarmUtilizationSummaryDto(
    Guid FarmId,
    string FarmName,
    decimal TotalArea,
    decimal AllocatedArea,
    decimal AvailableArea,
    decimal UtilizationPercentage,
    string AreaUnitSymbol,
    int ActivePlantationsCount
);

// --- Section B: Priorities ---
public sealed record DashboardPrioritiesResponse(
    int TotalAlertsCount,
    IReadOnlyList<PriorityActivityItemDto> OverdueActivities,
    IReadOnlyList<PriorityActivityItemDto> TodayActivities,
    IReadOnlyList<PriorityActivityItemDto> InProgressActivities,
    IReadOnlyList<ReiSafetyAlertDto> ActiveReiAlerts,
    LaborAttendanceAlertDto? AttendanceAlert,
    int OverdueInvoicesCount,
    decimal OverdueInvoicesTotalAmount,
    string? OverdueInvoicesCurrencySymbol
);

public sealed record PriorityActivityItemDto(
    Guid Id,
    string ActivityType, // "SPRAY", "IRRIGATION", "FERTILIZER", "LABOR"
    string Title,
    Guid FarmId,
    string FarmName,
    string? FarmAreaName,
    string? CropCycleName,
    string? ScheduledDisplay,
    int? DaysOverdue,
    string Status,
    string RouteUrl
);

public sealed record ReiSafetyAlertDto(
    Guid SprayId,
    string TargetPest,
    Guid FarmId,
    string FarmName,
    string? FarmAreaName,
    DateTimeOffset ExpiresAt,
    int RemainingMinutes,
    string RouteUrl
);

public sealed record LaborAttendanceAlertDto(
    string Status, // "NOT_RECORDED", "DRAFT", "FINALIZED"
    int WorkedCount,
    int AssignedCount,
    decimal WageExpense,
    string CurrencySymbol
);

// --- Section C: Upcoming Work ---
public sealed record DashboardUpcomingWorkResponse(
    IReadOnlyList<UpcomingWorkItemDto> Items
);

public sealed record UpcomingWorkItemDto(
    Guid Id,
    string ActivityType, // "SPRAY", "IRRIGATION", "FERTILIZER", "LABOR"
    string Title,
    Guid FarmId,
    string FarmName,
    string? FarmAreaName,
    string? CropCycleName,
    DateOnly ScheduledDate,
    string? ScheduledTime,
    string Status,
    string? Details,
    string RouteUrl
);

// --- Section F: Inventory Summary ---
public sealed record DashboardInventorySummaryResponse(
    int OutOfStockCount,
    IReadOnlyList<OutOfStockItemDto> OutOfStockItems,
    IReadOnlyList<RecentStockConsumptionDto> RecentConsumptions,
    int TotalItemsCount,
    int TotalLocationsCount
);

public sealed record OutOfStockItemDto(
    Guid InventoryItemId,
    string ItemName,
    string? Sku,
    string? CategoryName,
    Guid StorageLocationId,
    string StorageLocationName,
    Guid FarmId,
    string FarmName,
    string StockUnitSymbol
);

public sealed record RecentStockConsumptionDto(
    Guid InventoryItemId,
    string ItemName,
    string? CategoryName,
    decimal TotalConsumedQuantity,
    string StockUnitSymbol
);

// --- Section G: Financial Summary ---
public sealed record DashboardFinancialSummaryResponse(
    IReadOnlyList<CurrencyFinancialSummaryDto> CurrencySummaries
);

public sealed record CurrencyFinancialSummaryDto(
    Guid CurrencyId,
    string CurrencyCode,
    string CurrencySymbol,
    decimal CurrentMonthDirectExpenses,
    decimal CurrentMonthInvoiced,
    decimal TotalOutstandingPayables,
    int OverdueInvoicesCount,
    decimal OverdueInvoicesAmount
);

// --- Section H: Recent Activity Feed ---
public sealed record DashboardRecentActivityResponse(
    IReadOnlyList<RecentActivityItemDto> Items
);

public sealed record RecentActivityItemDto(
    Guid Id,
    string EventType, // "SPRAY_COMPLETED", "IRRIGATION_COMPLETED", "FERTILIZER_COMPLETED", "ATTENDANCE_FINALIZED", "STOCK_RECEIVED"
    string Title,
    string Description,
    DateTimeOffset Timestamp,
    Guid FarmId,
    string FarmName,
    string? PerformedBy,
    string RouteUrl
);
