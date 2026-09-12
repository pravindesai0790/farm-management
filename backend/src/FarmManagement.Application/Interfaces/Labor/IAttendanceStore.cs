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

    Task<LaborAttendance?> FindAttendanceByIdAsync(
        Guid id,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<LaborAttendance?> FindAttendanceByWorkerAndDateAsync(
        Guid organizationId,
        Guid workerId,
        DateOnly attendanceDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LaborAttendance>> ListDailyAttendanceAsync(
        Guid organizationId,
        Guid farmId,
        DateOnly attendanceDate,
        CancellationToken cancellationToken = default);

    Task<Worker?> FindWorkerWithAssignmentAsync(
        Guid organizationId,
        Guid workerId,
        Guid farmId,
        DateOnly attendanceDate,
        CancellationToken cancellationToken = default);

    void AddAttendance(LaborAttendance attendance);

    void RemoveAttendance(LaborAttendance attendance);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);
}
