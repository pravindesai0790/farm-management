using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Interfaces.Crops;

public interface ICropStore
{
    Task<int> CountCropsAsync(Guid organizationId, string? search, bool? isActive, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Crop>> ListCropsAsync(Guid organizationId, int skip, int take, string? search, bool? isActive, CancellationToken cancellationToken = default);
    Task<Crop?> FindCropAsync(Guid cropId, Guid organizationId, CancellationToken cancellationToken = default);
    Task<bool> CropCodeExistsAsync(Guid organizationId, string code, Guid? excludingCropId = null, CancellationToken cancellationToken = default);

    Task<int> CountVarietiesAsync(Guid organizationId, Guid cropId, bool? isActive, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CropVariety>> ListVarietiesAsync(Guid organizationId, Guid cropId, int skip, int take, bool? isActive, CancellationToken cancellationToken = default);
    Task<CropVariety?> FindVarietyAsync(Guid varietyId, Guid organizationId, CancellationToken cancellationToken = default);
    Task<bool> VarietyCodeExistsAsync(Guid organizationId, Guid cropId, string code, Guid? excludingVarietyId = null, CancellationToken cancellationToken = default);

    void Add(Crop crop);
    void Add(CropVariety variety);
    void AddAuditLog(AuditLog auditLog);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
