using FarmManagement.Application.DTOs.Crops;

namespace FarmManagement.Application.Interfaces.Crops;

public sealed record CropActor(Guid UserId, Guid OrganizationId, bool IsGlobalAdmin = false);

public interface ICropService
{
    Task<CropListResponse> ListCropsAsync(CropActor actor, int page, int pageSize, string? search, bool? isActive, CancellationToken cancellationToken = default);
    Task<CropResponse> GetCropAsync(CropActor actor, Guid cropId, CancellationToken cancellationToken = default);
    Task<CropResponse> CreateCropAsync(CropActor actor, CreateCropRequest request, string? ipAddress, CancellationToken cancellationToken = default);
    Task<CropResponse> UpdateCropAsync(CropActor actor, Guid cropId, UpdateCropRequest request, string? ipAddress, CancellationToken cancellationToken = default);
    Task<bool> ActivateCropAsync(CropActor actor, Guid cropId, string? ipAddress, CancellationToken cancellationToken = default);
    Task<bool> DeactivateCropAsync(CropActor actor, Guid cropId, string? ipAddress, CancellationToken cancellationToken = default);

    Task<CropVarietyListResponse> ListVarietiesAsync(CropActor actor, Guid cropId, int page, int pageSize, bool? isActive, CancellationToken cancellationToken = default);
    Task<CropVarietyResponse> GetVarietyAsync(CropActor actor, Guid varietyId, CancellationToken cancellationToken = default);
    Task<CropVarietyResponse> CreateVarietyAsync(CropActor actor, CreateCropVarietyRequest request, string? ipAddress, CancellationToken cancellationToken = default);
    Task<CropVarietyResponse> UpdateVarietyAsync(CropActor actor, Guid varietyId, UpdateCropVarietyRequest request, string? ipAddress, CancellationToken cancellationToken = default);
    Task<bool> ActivateVarietyAsync(CropActor actor, Guid varietyId, string? ipAddress, CancellationToken cancellationToken = default);
    Task<bool> DeactivateVarietyAsync(CropActor actor, Guid varietyId, string? ipAddress, CancellationToken cancellationToken = default);
}
