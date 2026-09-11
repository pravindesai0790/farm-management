using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Labor;

namespace FarmManagement.Application.Interfaces.Labor;

public interface IWorkerService
{
    Task<PagedResponse<WorkerResponse>> ListAsync(
        WorkerActor actor,
        int page,
        int pageSize,
        string? search,
        bool? isActive,
        Guid? contractorId,
        Guid? laborCategoryId,
        string? gender,
        string? employmentType,
        CancellationToken cancellationToken = default);

    Task<WorkerDetailResponse> GetAsync(
        WorkerActor actor,
        Guid workerId,
        CancellationToken cancellationToken = default);

    Task<WorkerDetailResponse> CreateAsync(
        WorkerActor actor,
        CreateWorkerRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<WorkerDetailResponse> UpdateAsync(
        WorkerActor actor,
        Guid workerId,
        UpdateWorkerRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<bool> ActivateAsync(
        WorkerActor actor,
        Guid workerId,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(
        WorkerActor actor,
        Guid workerId,
        string? ipAddress,
        CancellationToken cancellationToken = default);
}
