using FarmManagement.API.Helpers;
using FarmManagement.Application.DTOs.Labor;
using FarmManagement.Application.Interfaces.Labor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmManagement.API.Controllers;

[ApiController]
[Route("api/labor")]
[Authorize]
public sealed class WorkerSettlementsController(IWorkerSettlementService settlementService) : ControllerBase
{
    [HttpGet("workers/{workerId:guid}/settlement")]
    [Authorize(Policy = "Permission:WorkerPayment.View")]
    public async Task<ActionResult<WorkerSettlementCalculationResponse>> GetSettlement(
        Guid workerId,
        [FromQuery] DateOnly? periodFrom = null,
        [FromQuery] DateOnly? periodTo = null,
        [FromQuery] DateOnly? asOfDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = new WorkerSettlementQuery(
            WorkerId: workerId,
            PeriodFrom: periodFrom,
            PeriodTo: periodTo,
            AsOfDate: asOfDate);

        var result = await settlementService.CalculateSettlementAsync(
            GetUserContext(),
            query,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("settlements/batch")]
    [Authorize(Policy = "Permission:WorkerPayment.View")]
    public async Task<ActionResult<IReadOnlyList<WorkerSettlementCalculationResponse>>> GetBatchSettlement(
        [FromBody] BatchWorkerSettlementQuery query,
        CancellationToken cancellationToken = default)
    {
        var result = await settlementService.CalculateBatchSettlementAsync(
            GetUserContext(),
            query,
            cancellationToken);

        return Ok(result);
    }

    private SettlementActor GetUserContext() => UserContextHelper.GetUserContext<SettlementActor>(User);
}
