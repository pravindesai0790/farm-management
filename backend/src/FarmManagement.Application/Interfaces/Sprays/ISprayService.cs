using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Sprays;

namespace FarmManagement.Application.Interfaces.Sprays;

public interface ISprayService
{
    Task<PagedResponse<SprayListItemResponse>> ListAsync(
        SprayActor actor,
        SprayListQuery query,
        CancellationToken cancellationToken = default);

    Task<SprayDetailsResponse> GetAsync(
        SprayActor actor,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<SprayDetailsResponse> CreateDraftAsync(
        SprayActor actor,
        CreateSprayDraftRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<SprayDetailsResponse> UpdateDraftAsync(
        SprayActor actor,
        Guid id,
        UpdateSprayDraftRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);
}
