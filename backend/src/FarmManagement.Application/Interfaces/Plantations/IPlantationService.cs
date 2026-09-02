using FarmManagement.Application.DTOs.Plantations;

namespace FarmManagement.Application.Interfaces.Plantations;

public sealed record PlantationActor(Guid UserId, Guid OrganizationId);

public interface IPlantationService
{
    Task<PlantationListResponse> ListAsync(
        PlantationActor actor,
        Guid? farmId,
        Guid? farmAreaId,
        string? status,
        CancellationToken cancellationToken = default);

    Task<PlantationResponse> GetAsync(
        PlantationActor actor,
        Guid plantationId,
        CancellationToken cancellationToken = default);

    Task<PlantationResponse> CreateAsync(
        PlantationActor actor,
        CreatePlantationRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<PlantationResponse> UpdateAsync(
        PlantationActor actor,
        Guid plantationId,
        UpdatePlantationRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<bool> ActivateAsync(
        PlantationActor actor,
        Guid plantationId,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<bool> TerminateAsync(
        PlantationActor actor,
        Guid plantationId,
        TerminatePlantationRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<bool> ArchiveAsync(
        PlantationActor actor,
        Guid plantationId,
        string? ipAddress,
        CancellationToken cancellationToken = default);
}
