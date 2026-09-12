using FarmManagement.Application.DTOs.Labor;

namespace FarmManagement.Application.Interfaces.Labor;

/// <summary>
/// Interface boundary for Attendance (Phase 3.2) to generate worker earnings.
/// Attendance is the sole source of earned labor wages.
/// </summary>
public interface IAttendanceEarningsIntegration
{
    Task<WorkerEarningsLedgerResponse> CreateEarningFromFinalizedAttendanceAsync(
        EarningsActor actor,
        FinalizedAttendanceRecord attendance,
        CancellationToken cancellationToken = default);
}
