using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Labor;

namespace FarmManagement.Application.Interfaces.Labor;

public interface IAttendanceService
{
    Task<PagedResponse<AttendanceEligibleWorkerResponse>> GetEligibleWorkersAsync(
        AttendanceActor actor,
        Guid farmId,
        DateOnly attendanceDate,
        string? search = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<DailyAttendanceResponse> GetDailyAttendanceAsync(
        AttendanceActor actor,
        Guid farmId,
        DateOnly attendanceDate,
        CancellationToken cancellationToken = default);

    Task<AttendanceRecordResponse> CreateDraftAsync(
        AttendanceActor actor,
        CreateDraftAttendanceRequest request,
        CancellationToken cancellationToken = default);

    Task<AttendanceRecordResponse> UpdateDraftAsync(
        AttendanceActor actor,
        Guid id,
        UpdateDraftAttendanceRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteDraftAsync(
        AttendanceActor actor,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<DailyAttendanceResponse> SaveDailyDraftBatchAsync(
        AttendanceActor actor,
        SaveDailyDraftAttendanceBatchRequest request,
        CancellationToken cancellationToken = default);
}
