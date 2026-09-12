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

    private AttendanceActor GetUserContext() =>
        UserContextHelper.GetUserContext<AttendanceActor>(User);
}
