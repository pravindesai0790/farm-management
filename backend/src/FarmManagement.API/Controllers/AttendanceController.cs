using FarmManagement.API.Helpers;
using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Labor;
using FarmManagement.Application.Interfaces.Labor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmManagement.API.Controllers;

[ApiController]
[Route("api/attendance")]
[Route("api/labor/attendance")]
[Authorize]
public sealed class AttendanceController(
    IAttendanceService attendanceService) : ControllerBase
{
    [HttpGet("eligible-workers")]
    [Authorize(Policy = "Permission:Attendance.View")]
    public async Task<ActionResult<PagedResponse<AttendanceEligibleWorkerResponse>>> GetEligibleWorkers(
        [FromQuery] Guid farmId,
        [FromQuery] DateOnly? date = null,
        [FromQuery] DateOnly? attendanceDate = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var effectiveDate = date ?? attendanceDate;
        if (!effectiveDate.HasValue || effectiveDate.Value == default)
        {
            throw new ValidationException("Validation failed.", new Dictionary<string, string[]>
            {
                ["attendanceDate"] = ["Attendance date is required."]
            });
        }

        var result = await attendanceService.GetEligibleWorkersAsync(
            GetUserContext(),
            farmId,
            effectiveDate.Value,
            search,
            page,
            pageSize,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("daily")]
    [Authorize(Policy = "Permission:Attendance.View")]
    public async Task<ActionResult<DailyAttendanceResponse>> GetDailyAttendance(
        [FromQuery] Guid farmId,
        [FromQuery] DateOnly? date = null,
        [FromQuery] DateOnly? attendanceDate = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveDate = date ?? attendanceDate;
        if (!effectiveDate.HasValue || effectiveDate.Value == default)
        {
            throw new ValidationException("Validation failed.", new Dictionary<string, string[]>
            {
                ["attendanceDate"] = ["Attendance date is required."]
            });
        }

        var result = await attendanceService.GetDailyAttendanceAsync(
            GetUserContext(),
            farmId,
            effectiveDate.Value,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("draft")]
    [Authorize(Policy = "Permission:Attendance.Create")]
    public async Task<ActionResult<AttendanceRecordResponse>> CreateDraft(
        [FromBody] CreateDraftAttendanceRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await attendanceService.CreateDraftAsync(
            GetUserContext(),
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetDailyAttendance),
            new { farmId = result.FarmId, date = result.AttendanceDate.ToString("yyyy-MM-dd") },
            result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Permission:Attendance.Update")]
    public async Task<ActionResult<AttendanceRecordResponse>> UpdateDraft(
        [FromRoute] Guid id,
        [FromBody] UpdateDraftAttendanceRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await attendanceService.UpdateDraftAsync(
            GetUserContext(),
            id,
            request,
            cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Permission:Attendance.Update")]
    public async Task<IActionResult> DeleteDraft(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        await attendanceService.DeleteDraftAsync(
            GetUserContext(),
            id,
            cancellationToken);

        return NoContent();
    }

    [HttpPost("draft/batch")]
    [Authorize(Policy = "Permission:Attendance.Create")]
    public async Task<ActionResult<DailyAttendanceResponse>> SaveDailyDraftBatch(
        [FromBody] SaveDailyDraftAttendanceBatchRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await attendanceService.SaveDailyDraftBatchAsync(
            GetUserContext(),
            request,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("preview")]
    [Authorize(Policy = "Permission:Attendance.View")]
    public async Task<ActionResult<AttendanceWagePreviewResponse>> PreviewWage(
        [FromBody] AttendanceWagePreviewRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await attendanceService.PreviewWageAsync(
            GetUserContext(),
            request,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("preview-batch")]
    [HttpPost("preview/batch")]
    [Authorize(Policy = "Permission:Attendance.View")]
    public async Task<ActionResult<AttendanceWagePreviewBatchResponse>> PreviewWageBatch(
        [FromBody] AttendanceWagePreviewBatchRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await attendanceService.PreviewWageBatchAsync(
            GetUserContext(),
            request,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("finalize")]
    [Authorize(Policy = "Permission:Attendance.Finalize")]
    public async Task<ActionResult<FinalizeAttendanceResponse>> FinalizeAttendance(
        [FromBody] FinalizeAttendanceRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await attendanceService.FinalizeAttendanceAsync(
            GetUserContext(),
            request,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{id:guid}/finalize")]
    [Authorize(Policy = "Permission:Attendance.Finalize")]
    public async Task<ActionResult<AttendanceRecordResponse>> FinalizeSingleAttendance(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await attendanceService.FinalizeSingleAttendanceAsync(
            GetUserContext(),
            id,
            cancellationToken);

        return Ok(result);
    }

    private AttendanceActor GetUserContext() =>
        UserContextHelper.GetUserContext<AttendanceActor>(User);
}


