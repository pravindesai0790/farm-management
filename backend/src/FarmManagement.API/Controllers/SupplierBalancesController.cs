using FarmManagement.API.Helpers;
using FarmManagement.Application.DTOs.Expenses;
using FarmManagement.Application.Interfaces.Expenses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmManagement.API.Controllers;

[ApiController]
[Route("api/supplier-balances")]
[Authorize]
public sealed class SupplierBalancesController(ISupplierBalanceService balanceService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Permission:SupplierBalance.View")]
    public async Task<ActionResult<SupplierBalanceSummaryResponse>> GetSupplierBalances(
        [FromQuery] DateOnly? asOfDate = null,
        [FromQuery] Guid? supplierId = null,
        [FromQuery] Guid? farmId = null,
        [FromQuery] Guid? currencyId = null,
        CancellationToken cancellationToken = default)
    {
        var (_, organizationId) = UserContextHelper.GetUserIds(User);
        var filter = new SupplierBalanceFilter(asOfDate, supplierId, farmId, currencyId);
        var result = await balanceService.GetSupplierBalancesAsync(organizationId, filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("overdue")]
    [Authorize(Policy = "Permission:SupplierBalance.View")]
    public async Task<ActionResult<IReadOnlyList<OverdueInvoiceItem>>> GetOverdueInvoices(
        [FromQuery] DateOnly? asOfDate = null,
        [FromQuery] Guid? supplierId = null,
        [FromQuery] Guid? farmId = null,
        [FromQuery] Guid? currencyId = null,
        CancellationToken cancellationToken = default)
    {
        var (_, organizationId) = UserContextHelper.GetUserIds(User);
        var filter = new SupplierBalanceFilter(asOfDate, supplierId, farmId, currencyId);
        var result = await balanceService.GetOverdueInvoicesAsync(organizationId, filter, cancellationToken);
        return Ok(result);
    }
}
