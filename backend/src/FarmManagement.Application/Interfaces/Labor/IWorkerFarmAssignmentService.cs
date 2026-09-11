using FarmManagement.Application.DTOs.Labor;

namespace FarmManagement.Application.Interfaces.Labor;

public interface IWorkerFarmAssignmentService
{
    Task<IReadOnlyList<WorkerFarmAssignmentResponse>> ListByWorkerAsync(
        WorkerActor actor,
        Guid workerId,
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkerFarmAssignmentResponse>> ListByFarmAsync(
        WorkerActor actor,
        Guid farmId,
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<WorkerFarmAssignmentResponse> GetAsync(
        WorkerActor actor,
        Guid workerId,
        Guid assignmentId,
        CancellationToken cancellationToken = default);

    Task<WorkerFarmAssignmentResponse> CreateAsync(
        WorkerActor actor,
        Guid workerId,
        CreateWorkerFarmAssignmentRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<WorkerFarmAssignmentResponse> UpdateAsync(
        WorkerActor actor,
        Guid workerId,
        Guid assignmentId,
        UpdateWorkerFarmAssignmentRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<WorkerFarmAssignmentResponse> EndAssignmentAsync(
        WorkerActor actor,
        Guid workerId,
        Guid assignmentId,
        EndWorkerFarmAssignmentRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(
        WorkerActor actor,
        Guid workerId,
        Guid assignmentId,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<bool> ActivateAsync(
        WorkerActor actor,
        Guid workerId,
        Guid assignmentId,
        string? ipAddress,
        CancellationToken cancellationToken = default);
}
