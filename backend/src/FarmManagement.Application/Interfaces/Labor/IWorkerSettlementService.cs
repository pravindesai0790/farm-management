using FarmManagement.Application.DTOs.Labor;

namespace FarmManagement.Application.Interfaces.Labor;

public interface IWorkerSettlementService
{
    Task<WorkerSettlementCalculationResponse> CalculateSettlementAsync(
        SettlementActor actor,
        WorkerSettlementQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkerSettlementCalculationResponse>> CalculateBatchSettlementAsync(
        SettlementActor actor,
        BatchWorkerSettlementQuery query,
        CancellationToken cancellationToken = default);
}
