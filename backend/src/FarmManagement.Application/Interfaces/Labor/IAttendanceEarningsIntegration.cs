using FarmManagement.Application.DTOs.Labor;

namespace FarmManagement.Application.Interfaces.Labor;

/// <summary>
/// Interface boundary for Attendance (Phase 3.2) to calculate and generate worker earnings.
/// Attendance is the sole source of earned labor wages. This service completely encapsulates
/// worker eligibility, rate resolution, snapshotting, gross amount computation, and ledger
/// state transitions so that Attendance code never duplicates rate selection or formulas,
/// and never depends on Labor Activity.
/// </summary>
public interface IAttendanceEarningsIntegration
{
    /// <summary>
    /// Pure calculation and eligibility preview for Attendance UI/entry without writing to the ledger.
    /// Resolves worker, validates date eligibility, resolves gender, determines applicable wage type,
    /// and looks up the applicable wage rate snapshot to compute gross earnings.
    /// </summary>
    Task<AttendanceEarningsCalculationResult> CalculateAttendanceEarningsAsync(
        EarningsActor actor,
        CalculateAttendanceEarningsRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes earnings for an attendance record upon draft save or finalization.
    /// Supports new earnings, updating unapproved earnings, reversing prior earnings when
    /// attendance changes to non-earning (Absent/Leave), or recording approved earnings.
    /// Returns null if the attendance record is non-earning and has no ledger entries.
    /// </summary>
    Task<WorkerEarningsLedgerResponse?> ProcessAttendanceEarningsAsync(
        EarningsActor actor,
        ProcessAttendanceEarningsRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an earning entry from an already finalized attendance record.
    /// </summary>
    Task<WorkerEarningsLedgerResponse> CreateEarningFromFinalizedAttendanceAsync(
        EarningsActor actor,
        FinalizedAttendanceRecord attendance,
        CancellationToken cancellationToken = default);
}
