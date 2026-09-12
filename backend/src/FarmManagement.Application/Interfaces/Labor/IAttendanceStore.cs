using FarmManagement.Application.DTOs.Labor;
using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Interfaces.Labor;

public interface IAttendanceStore
{
    Task<Farm?> FindFarmAsync(
        Guid farmId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<int> CountEligibleWorkersAsync(
        Guid organizationId,
        Guid farmId,
        DateOnly attendanceDate,
        string? search,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AttendanceEligibleWorkerResponse>> ListEligibleWorkersAsync(
        Guid organizationId,
        Guid farmId,
        DateOnly attendanceDate,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
}
