namespace FarmManagement.Application.DTOs.Labor;

public sealed record AttendanceActor(Guid UserId, Guid OrganizationId);

public sealed record AttendanceEligibleWorkerResponse(
    Guid WorkerId,
    string DisplayName,
    string FirstName,
    string? LastName,
    string Gender,
    string EmploymentType,
    string? MobileNumber,
    Guid? LaborCategoryId,
    string? LaborCategoryName,
    Guid? ContractorId,
    string? ContractorName,
    DateOnly? JoiningDate,
    DateOnly? LeavingDate,
    Guid AssignmentId,
    Guid FarmId,
    DateOnly AssignedFrom,
    DateOnly? AssignedTo);

public sealed record CreateDraftAttendanceRequest(
    Guid FarmId,
    Guid WorkerId,
    DateOnly AttendanceDate,
    string AttendanceType,
    decimal? WorkingHours = null,
    string? Notes = null);

public sealed record UpdateDraftAttendanceRequest(
    string AttendanceType,
    decimal? WorkingHours = null,
    string? Notes = null);

public sealed record DailyDraftAttendanceItemRequest(
    Guid? Id,
    Guid WorkerId,
    string AttendanceType,
    decimal? WorkingHours = null,
    string? Notes = null);

public sealed record SaveDailyDraftAttendanceBatchRequest(
    Guid FarmId,
    DateOnly AttendanceDate,
    IReadOnlyList<DailyDraftAttendanceItemRequest> Items,
    bool RemoveOmittedDrafts = false);

public sealed record AttendanceRecordResponse(
    Guid Id,
    Guid OrganizationId,
    Guid FarmId,
    string FarmName,
    Guid WorkerId,
    string WorkerDisplayName,
    string WorkerFirstName,
    string? WorkerLastName,
    string Gender,
    Guid? LaborCategoryId,
    string? LaborCategoryName,
    string EmploymentType,
    DateOnly AttendanceDate,
    string AttendanceType,
    decimal? WorkingHours,
    decimal? CalculatedRate,
    decimal? CalculatedAmount,
    Guid? CurrencyId,
    string? CurrencyCode,
    string? CurrencySymbol,
    string Status,
    string? Notes,
    DateTimeOffset? FinalizedAt,
    Guid? FinalizedBy,
    DateTimeOffset CreatedAt,
    Guid CreatedBy,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedBy);

public sealed record DailyAttendanceSummaryResponse(
    int TotalCount,
    int WorkedCount,
    int FullDayCount,
    int HalfDayCount,
    int HourlyCount,
    int NotWorkedCount,
    decimal EstimatedEarnings,
    string Status);

public sealed record DailyAttendanceResponse(
    Guid FarmId,
    string FarmName,
    DateOnly AttendanceDate,
    DailyAttendanceSummaryResponse Summary,
    IReadOnlyList<AttendanceRecordResponse> Records);

public sealed record AttendanceWagePreviewRequest(
    Guid WorkerId,
    DateOnly AttendanceDate,
    string AttendanceType,
    decimal? WorkingHours = null,
    Guid? FarmId = null);

public sealed record AttendanceWagePreviewResponse(
    Guid WorkerId,
    string WorkerDisplayName,
    string Gender,
    DateOnly AttendanceDate,
    string AttendanceType,
    string? ResolvedWageType,
    string? WageType,
    decimal Rate,
    decimal Quantity,
    decimal? WorkingHours,
    decimal CalculatedAmount,
    Guid? CurrencyId,
    string CurrencyCode,
    string CurrencySymbol,
    string Currency,
    bool IsEarningEligible,
    bool IsWorkerEligible,
    string? IneligibilityReason = null);

public sealed record AttendanceWagePreviewBatchItemRequest(
    Guid WorkerId,
    string AttendanceType,
    decimal? WorkingHours = null);

public sealed record AttendanceWagePreviewBatchRequest(
    DateOnly AttendanceDate,
    IReadOnlyList<AttendanceWagePreviewBatchItemRequest> Items,
    Guid? FarmId = null);

public sealed record AttendanceWagePreviewBatchResponse(
    DateOnly AttendanceDate,
    int TotalCount,
    int WorkedCount,
    int FullDayCount,
    int HalfDayCount,
    int HourlyCount,
    int NotWorkedCount,
    decimal TotalEstimatedEarnings,
    IReadOnlyList<AttendanceWagePreviewResponse> Items);

public sealed record FinalizeAttendanceRequest(
    Guid FarmId,
    DateOnly AttendanceDate,
    IReadOnlyList<Guid>? AttendanceIds = null);

public sealed record FinalizeAttendanceResponse(
    Guid FarmId,
    string FarmName,
    DateOnly AttendanceDate,
    int FinalizedCount,
    int PaidCount,
    int NotWorkedCount,
    decimal TotalEarnings,
    DailyAttendanceSummaryResponse Summary,
    IReadOnlyList<AttendanceRecordResponse> Records);

