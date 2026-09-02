using FarmManagement.Application.DTOs.Crops;

namespace FarmManagement.Application.Interfaces.Crops;

public sealed record CropLifecycleTemplateActor(Guid UserId, Guid OrganizationId, bool IsGlobalAdmin = false);

public interface ICropLifecycleTemplateService
{
    Task<CropLifecycleTemplateListResponse> ListAsync(
        CropLifecycleTemplateActor actor,
        int page,
        int pageSize,
        Guid? cropId,
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<CropLifecycleTemplateResponse> GetAsync(
        CropLifecycleTemplateActor actor,
        Guid templateId,
        CancellationToken cancellationToken = default);

    Task<CropLifecycleStageResponse> GetStageAsync(
        CropLifecycleTemplateActor actor,
        Guid templateId,
        Guid stageId,
        CancellationToken cancellationToken = default);

    Task<CropLifecycleTemplateResponse> CreateAsync(
        CropLifecycleTemplateActor actor,
        CreateCropLifecycleTemplateRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<CropLifecycleTemplateResponse> UpdateAsync(
        CropLifecycleTemplateActor actor,
        Guid templateId,
        UpdateCropLifecycleTemplateRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<bool> ActivateAsync(
        CropLifecycleTemplateActor actor,
        Guid templateId,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(
        CropLifecycleTemplateActor actor,
        Guid templateId,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<CropLifecycleStageResponse> CreateStageAsync(
        CropLifecycleTemplateActor actor,
        Guid templateId,
        CreateCropLifecycleStageRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<CropLifecycleStageResponse> UpdateStageAsync(
        CropLifecycleTemplateActor actor,
        Guid templateId,
        Guid stageId,
        UpdateCropLifecycleStageRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<bool> ActivateStageAsync(
        CropLifecycleTemplateActor actor,
        Guid templateId,
        Guid stageId,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateStageAsync(
        CropLifecycleTemplateActor actor,
        Guid templateId,
        Guid stageId,
        string? ipAddress,
        CancellationToken cancellationToken = default);
}
