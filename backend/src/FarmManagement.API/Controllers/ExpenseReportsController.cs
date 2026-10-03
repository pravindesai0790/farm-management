using FarmManagement.API.Helpers;
using FarmManagement.Application.DTOs.Expenses;
using FarmManagement.Application.Interfaces.Expenses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmManagement.API.Controllers;

[ApiController]
[Route("api/expense-reports")]
[Authorize]
public sealed class ExpenseReportsController(IExpenseReportService reportService) : ControllerBase
{
    [HttpGet("summary")]
    [Authorize(Policy = "Permission:Expense.Report.View")]
    public async Task<ActionResult<ExpenseReportSummaryResponse>> GetSummaryReport(
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] Guid? farmId = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] Guid? cropCycleId = null,
        [FromQuery] Guid? currencyId = null,
        CancellationToken cancellationToken = default)
    {
        var (_, organizationId) = UserContextHelper.GetUserIds(User);
        var filter = new ExpenseReportFilter(from, to, farmId, categoryId, cropCycleId, currencyId);
        var result = await reportService.GetSummaryReportAsync(organizationId, filter, cancellationToken);
        return Ok(result);
    }
}
