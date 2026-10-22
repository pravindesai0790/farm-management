namespace FarmManagement.Application.DTOs.Dashboard;

public sealed record RawPrioritySpray(
    Guid Id,
    string? TargetName,
    Guid FarmId,
    string FarmName,
    string? FarmAreaName,
    string? CropCycleName,
    DateOnly? PlannedDate,
    DateTimeOffset? ScheduledDateTime,
    DateTimeOffset? ActualApplicationDateTime,
    string Status,
    DateTimeOffset? ReEntryIntervalExpiresAt);

public sealed record RawPriorityIrrigation(
    Guid Id,
    Guid FarmId,
    string FarmName,
    string? FarmAreaName,
    string? CropCycleName,
    DateTimeOffset? PlannedAt,
    DateTimeOffset? ScheduledAt,
    DateTimeOffset? ActualStartedAt,
    string Status,
    decimal? WaterQuantity,
    string? WaterUnitSymbol);

public sealed record RawPriorityFertilizer(
    Guid Id,
    string ReferenceNumber,
    Guid FarmId,
    string FarmName,
    string? FarmAreaName,
    string? CropCycleName,
    DateTimeOffset? PlannedDate,
    DateTimeOffset? ScheduledDate,
    DateTimeOffset? ActualApplicationDate,
    string Status,
    string? ApplicationMethodName);

public sealed record RawPriorityLaborActivity(
    Guid Id,
    string ActivityTypeName,
    Guid FarmId,
    string FarmName,
    string? FarmAreaName,
    string? CropCycleName,
    DateOnly ActivityDate,
    string Status);

public sealed record RawPriorityOverdueInvoice(
    Guid Id,
    string SupplierInvoiceNumber,
    string SupplierName,
    Guid FarmId,
    string FarmName,
    DateOnly DueDate,
    decimal TotalAmount,
    decimal AmountPaid,
    decimal OutstandingBalance,
    string CurrencySymbol,
    string CurrencyCode);

public sealed record DashboardPrioritiesRawData(
    IReadOnlyList<RawPrioritySpray> Sprays,
    IReadOnlyList<RawPriorityIrrigation> Irrigations,
    IReadOnlyList<RawPriorityFertilizer> Fertilizers,
    IReadOnlyList<RawPriorityLaborActivity> LaborActivities,
    IReadOnlyList<RawPriorityOverdueInvoice> OverdueInvoices,
    LaborDashboardRawData LaborRawData);

public sealed record RawUpcomingActivity(
    Guid Id,
    string ActivityType,
    string Title,
    Guid FarmId,
    string FarmName,
    string? FarmAreaName,
    string? CropCycleName,
    DateOnly ScheduledDate,
    string? ScheduledTime,
    string Status,
    string? Details);

public sealed record RawOutOfStockItem(
    Guid InventoryItemId,
    string ItemName,
    string? Sku,
    string? CategoryName,
    Guid StorageLocationId,
    string StorageLocationName,
    Guid FarmId,
    string FarmName,
    string StockUnitSymbol,
    decimal QuantityOnHand);

public sealed record RawStockConsumption(
    Guid InventoryItemId,
    string ItemName,
    string? CategoryName,
    decimal Quantity,
    string StockUnitSymbol);

public sealed record DashboardInventoryRawData(
    IReadOnlyList<RawOutOfStockItem> OutOfStockItems,
    IReadOnlyList<RawStockConsumption> RecentConsumptions,
    int TotalItemsCount,
    int TotalLocationsCount);

public sealed record RawDirectExpenseItem(
    Guid CurrencyId,
    string CurrencyCode,
    string CurrencySymbol,
    decimal Amount);

public sealed record RawInvoiceFinancialItem(
    Guid CurrencyId,
    string CurrencyCode,
    string CurrencySymbol,
    decimal TotalAmount,
    decimal OutstandingBalance,
    bool IsOverdue,
    bool IsThisMonth);

public sealed record DashboardFinancialRawData(
    IReadOnlyList<RawDirectExpenseItem> DirectExpenses,
    IReadOnlyList<RawInvoiceFinancialItem> Invoices);

public sealed record RawRecentActivity(
    Guid Id,
    string EventType,
    string Title,
    string Description,
    DateTimeOffset Timestamp,
    Guid FarmId,
    string FarmName,
    string? PerformedBy);
