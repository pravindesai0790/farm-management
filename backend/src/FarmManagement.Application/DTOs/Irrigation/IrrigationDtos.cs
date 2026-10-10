using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.DTOs.Irrigation;

public sealed record IrrigationListQuery(
    Guid? FarmId = null,
    Guid? FarmAreaId = null,
    Guid? PlantationId = null,
    Guid? CropCycleId = null,
    Guid? CropCycleStageId = null,
    Guid? IrrigationMethodId = null,
    string? Status = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    string? Search = null,
    bool IncludeOverdue = false,
    int PageNumber = 1,
    int PageSize = 20,
    string? SortBy = null,
    string? SortDirection = null);

public sealed record IrrigationSummaryCountsResponse(
    int TotalCount,
    int DraftCount,
    int ScheduledCount,
    int InProgressCount,
    int CompletedCount,
    int CancelledCount,
    int OverdueCount);

public sealed record IrrigationListItemResponse(
    Guid Id,
    Guid FarmId,
    string FarmName,
    Guid FarmAreaId,
    string FarmAreaName,
    Guid? PlantationId,
    string? PlantationName,
    Guid? CropCycleId,
    string? CropCycleName,
    Guid? CropCycleStageId,
    string? CropCycleStageName,
    IrrigationStatus Status,
    string StatusName,
    bool IsOverdue,
    DateTimeOffset? PlannedAt,
    DateTimeOffset? ScheduledAt,
    DateTimeOffset? ActualStartedAt,
    DateTimeOffset? ActualEndedAt,
    int? ActualDurationMinutes,
    Guid? IrrigationMethodId,
    string? IrrigationMethodName,
    decimal? PlannedWaterQuantity,
    Guid? PlannedWaterUnitId,
    string? PlannedWaterUnitName,
    decimal? ActualWaterQuantity,
    Guid? ActualWaterUnitId,
    string? ActualWaterUnitName,
    DateTimeOffset? CompletedAt,
    DateTimeOffset CreatedAt);

public sealed record IrrigationDetailsResponse(
    Guid Id,
    Guid OrganizationId,
    Guid FarmId,
    string FarmName,
    Guid FarmAreaId,
    string FarmAreaName,
    Guid? PlantationId,
    string? PlantationName,
    Guid? CropCycleId,
    string? CropCycleName,
    Guid? CropCycleStageId,
    string? CropCycleStageName,
    IrrigationStatus Status,
    string StatusName,
    bool IsOverdue,
    DateTimeOffset? PlannedAt,
    DateTimeOffset? ScheduledAt,
    DateTimeOffset? ActualStartedAt,
    DateTimeOffset? ActualEndedAt,
    int? ActualDurationMinutes,
    Guid? IrrigationMethodId,
    string? IrrigationMethodName,
    decimal? PlannedWaterQuantity,
    Guid? PlannedWaterUnitId,
    string? PlannedWaterUnitName,
    string? PlannedWaterUnitSymbol,
    decimal? ActualWaterQuantity,
    Guid? ActualWaterUnitId,
    string? ActualWaterUnitName,
    string? ActualWaterUnitSymbol,
    string? Notes,
    string? CancellationReason,
    DateTimeOffset? CompletedAt,
    DateTimeOffset CreatedAt,
    Guid CreatedBy,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedBy,
    string? CreatedByName = null,
    string? UpdatedByName = null,
    string? ConcurrencyToken = null);

public sealed record CreateIrrigationDraftRequest(
    Guid FarmId,
    Guid FarmAreaId,
    Guid? PlantationId = null,
    Guid? CropCycleId = null,
    Guid? CropCycleStageId = null,
    Guid? IrrigationMethodId = null,
    DateTimeOffset? PlannedAt = null,
    DateTimeOffset? ScheduledAt = null,
    decimal? PlannedWaterQuantity = null,
    Guid? PlannedWaterUnitId = null,
    string? Notes = null);

public sealed record UpdateIrrigationDraftRequest(
    Guid FarmId,
    Guid FarmAreaId,
    Guid? PlantationId = null,
    Guid? CropCycleId = null,
    Guid? CropCycleStageId = null,
    Guid? IrrigationMethodId = null,
    DateTimeOffset? PlannedAt = null,
    decimal? PlannedWaterQuantity = null,
    Guid? PlannedWaterUnitId = null,
    string? Notes = null,
    string? ConcurrencyToken = null);

public sealed record ScheduleIrrigationRequest(
    DateTimeOffset ScheduledAt,
    string? ConcurrencyToken = null);

public sealed record RescheduleIrrigationRequest(
    DateTimeOffset ScheduledAt,
    string? ConcurrencyToken = null);

public sealed record StartIrrigationRequest(
    DateTimeOffset? ActualStartedAt = null,
    string? ConcurrencyToken = null);

public sealed record CompleteIrrigationRequest(
    Guid IrrigationMethodId,
    DateTimeOffset? ActualStartedAt = null,
    DateTimeOffset? ActualEndedAt = null,
    int? ActualDurationMinutes = null,
    decimal? ActualWaterQuantity = null,
    Guid? ActualWaterUnitId = null,
    string? Notes = null,
    string? ConcurrencyToken = null);

public sealed record RecordCompletedIrrigationRequest(
    Guid FarmId,
    Guid FarmAreaId,
    Guid IrrigationMethodId,
    Guid? PlantationId = null,
    Guid? CropCycleId = null,
    Guid? CropCycleStageId = null,
    DateTimeOffset? ActualStartedAt = null,
    DateTimeOffset? ActualEndedAt = null,
    int? ActualDurationMinutes = null,
    decimal? ActualWaterQuantity = null,
    Guid? ActualWaterUnitId = null,
    string? Notes = null);

public sealed record CancelIrrigationRequest(
    string CancellationReason,
    string? ConcurrencyToken = null);

public sealed record IrrigationMethodDto(
    Guid Id,
    Guid? OrganizationId,
    string Code,
    string Name,
    string? Description,
    int DisplayOrder,
    bool IsSystem,
    bool IsActive);
