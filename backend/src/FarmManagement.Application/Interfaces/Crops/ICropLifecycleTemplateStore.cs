using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Interfaces.Crops;

public interface ICropLifecycleTemplateStore
{
    Task<int> CountAsync(
        Guid organizationId,
        Guid? cropId,
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CropLifecycleTemplate>> ListAsync(
        Guid organizationId,
        Guid? cropId,
        int skip,
        int take,
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<CropLifecycleTemplate?> FindAsync(
        Guid templateId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<Crop?> FindCropAsync(
        Guid cropId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<bool> SequenceExistsAsync(
        Guid templateId,
        int sequenceNumber,
        Guid? excludingStageId = null,
        CancellationToken cancellationToken = default);

    Task ClearDefaultAsync(
        Guid? organizationId,
        Guid cropId,
        Guid excludingTemplateId,
        DateTimeOffset now,
        Guid updatedBy,
        CancellationToken cancellationToken = default);

    void Add(CropLifecycleTemplate template);
    void Add(CropLifecycleStage stage);
    void AddAuditLog(AuditLog auditLog);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
