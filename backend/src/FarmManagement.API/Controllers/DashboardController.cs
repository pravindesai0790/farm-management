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

    [HttpGet("priorities")]
    [Authorize(Policy = "Permission:Farm.View")]
    public async Task<ActionResult<DashboardPrioritiesResponse>> GetPriorities(
        [FromQuery] Guid? farmId = null,
        CancellationToken cancellationToken = default)
    {
        return Ok(await dashboardService.GetPrioritiesAsync(GetUserContext(), farmId, cancellationToken));
    }

    [HttpGet("upcoming-work")]
    [Authorize(Policy = "Permission:Farm.View")]
    public async Task<ActionResult<DashboardUpcomingWorkResponse>> GetUpcomingWork(
        [FromQuery] Guid? farmId = null,
        [FromQuery] int days = 7,
        CancellationToken cancellationToken = default)
    {
        return Ok(await dashboardService.GetUpcomingWorkAsync(GetUserContext(), farmId, days, cancellationToken));
    }

    [HttpGet("inventory-summary")]
    [Authorize(Policy = "Permission:InventoryStock.View")]
    public async Task<ActionResult<DashboardInventorySummaryResponse>> GetInventorySummary(
        [FromQuery] Guid? farmId = null,
        CancellationToken cancellationToken = default)
    {
        return Ok(await dashboardService.GetInventorySummaryAsync(GetUserContext(), farmId, cancellationToken));
    }

    [HttpGet("financial-summary")]
    [Authorize(Policy = "Permission:Farm.View")]
    public async Task<ActionResult<DashboardFinancialSummaryResponse>> GetFinancialSummary(
        [FromQuery] Guid? farmId = null,
        CancellationToken cancellationToken = default)
    {
        return Ok(await dashboardService.GetFinancialSummaryAsync(GetUserContext(), farmId, cancellationToken));
    }

    [HttpGet("recent-activity")]
    [Authorize(Policy = "Permission:Farm.View")]
    public async Task<ActionResult<DashboardRecentActivityResponse>> GetRecentActivity(
        [FromQuery] Guid? farmId = null,
        [FromQuery] int limit = 15,
        CancellationToken cancellationToken = default)
    {
        return Ok(await dashboardService.GetRecentActivityAsync(GetUserContext(), farmId, limit, cancellationToken));
    }

    private DashboardActor GetUserContext() => UserContextHelper.GetUserContext<DashboardActor>(User);
}
