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
}
