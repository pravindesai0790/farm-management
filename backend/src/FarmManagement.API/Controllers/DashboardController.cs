using FarmManagement.API.Helpers;
using FarmManagement.Application.DTOs.Dashboard;
using FarmManagement.Application.Interfaces.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmManagement.API.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public sealed class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet("summary")]
    [Authorize(Policy = "Permission:Farm.View")]
    public async Task<ActionResult<DashboardSummaryResponse>> GetSummary(
        [FromQuery] Guid? farmId = null,
        CancellationToken cancellationToken = default)
    {
        return Ok(await dashboardService.GetSummaryAsync(GetUserContext(), farmId, cancellationToken));
    }

    private DashboardActor GetUserContext() => UserContextHelper.GetUserContext<DashboardActor>(User);
}
