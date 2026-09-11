using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Labor;

namespace FarmManagement.Application.Interfaces.Labor;

public interface IContractorService
{
    Task<PagedResponse<ContractorResponse>> ListAsync(
        ContractorActor actor,
        int page,
        int pageSize,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<ContractorResponse> GetAsync(
        ContractorActor actor,
        Guid contractorId,
        CancellationToken cancellationToken = default);

    Task<ContractorResponse> CreateAsync(
        ContractorActor actor,
        CreateContractorRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<ContractorResponse> UpdateAsync(
        ContractorActor actor,
        Guid contractorId,
        UpdateContractorRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<bool> ActivateAsync(
        ContractorActor actor,
        Guid contractorId,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(
        ContractorActor actor,
        Guid contractorId,
        string? ipAddress,
        CancellationToken cancellationToken = default);
}
