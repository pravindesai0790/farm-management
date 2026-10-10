using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Irrigation;

namespace FarmManagement.Application.Interfaces.Irrigation;

public interface IIrrigationService
{
    Task<PagedResponse<IrrigationListItemResponse>> ListAsync(
        IrrigationActor actor,
        IrrigationListQuery query,
        CancellationToken cancellationToken = default);

    Task<IrrigationSummaryCountsResponse> GetSummaryCountsAsync(
        IrrigationActor actor,
        Guid? farmId,
        CancellationToken cancellationToken = default);

    Task<IrrigationDetailsResponse> GetAsync(
        IrrigationActor actor,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IrrigationDetailsResponse> CreateDraftAsync(
        IrrigationActor actor,
        CreateIrrigationDraftRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<IrrigationDetailsResponse> UpdateDraftAsync(
        IrrigationActor actor,
        Guid id,
        UpdateIrrigationDraftRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<IrrigationDetailsResponse> ScheduleAsync(
        IrrigationActor actor,
        Guid id,
        ScheduleIrrigationRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<IrrigationDetailsResponse> RescheduleAsync(
        IrrigationActor actor,
        Guid id,
        RescheduleIrrigationRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<IrrigationDetailsResponse> StartAsync(
        IrrigationActor actor,
        Guid id,
        StartIrrigationRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<IrrigationDetailsResponse> CompleteAsync(
        IrrigationActor actor,
        Guid id,
        CompleteIrrigationRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<IrrigationDetailsResponse> RecordCompletedAsync(
        IrrigationActor actor,
        RecordCompletedIrrigationRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<IrrigationDetailsResponse> CancelAsync(
        IrrigationActor actor,
        Guid id,
        CancelIrrigationRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IrrigationMethodDto>> ListMethodsAsync(
        IrrigationActor actor,
        bool activeOnly = true,
        CancellationToken cancellationToken = default);
}
