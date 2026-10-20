using FarmManagement.Application.DTOs.Fertilizer;
using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Interfaces.Fertilizer;

public interface IFertilizerStockManager
{
    Task<FertilizerStockConsumptionResult> ConsumeStockAsync(
        FertilizerActor actor,
        FertilizerApplication application,
        IReadOnlyList<FertilizerStockConsumptionItem> itemsToIssue,
        DateOnly movementDate,
        string operationType,
        string? idempotencyKey,
        object? requestPayloadForHashing,
        string? notes = null,
        CancellationToken cancellationToken = default);

    Task<FertilizerOperationIdempotencyRecord?> CheckIdempotencyAsync(
        Guid organizationId,
        string operationType,
        string? idempotencyKey,
        object? requestPayload,
        CancellationToken cancellationToken = default);

    void RecordIdempotency(
        Guid organizationId,
        string operationType,
        string idempotencyKey,
        Guid fertilizerApplicationId,
        object? requestPayload);
}
