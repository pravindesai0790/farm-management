using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Interfaces.Fertilizer;

public interface IFertilizerValidator
{
    Task ValidateHierarchyAsync(
        Guid organizationId,
        Guid? farmId,
        Guid? farmAreaId,
        Guid? plantationId,
        Guid? cropCycleId,
        Guid? cropCycleStageId,
        bool isPreStart = true,
        CancellationToken cancellationToken = default);

    Task ValidateProductEligibilityAsync(
        Guid organizationId,
        Guid inventoryItemId,
        bool isPreStart = true,
        CancellationToken cancellationToken = default);

    Task ValidateStorageLocationAsync(
        Guid organizationId,
        Guid farmId,
        Guid storageLocationId,
        bool isPreStart = true,
        CancellationToken cancellationToken = default);

    Task ValidateApplicationMethodAsync(
        Guid organizationId,
        Guid? applicationMethodId,
        string? otherMethodDescription,
        CancellationToken cancellationToken = default);

    Task ValidateIrrigationLinkAsync(
        Guid organizationId,
        Guid? applicationMethodId,
        Guid? irrigationEventId,
        Guid? farmId,
        Guid? farmAreaId,
        CancellationToken cancellationToken = default);

    Task ValidateAreaAndUnitsAsync(
        Guid organizationId,
        decimal? plannedArea,
        Guid? plannedAreaUnitId,
        decimal? actualArea,
        Guid? actualAreaUnitId,
        CancellationToken cancellationToken = default);

    void ValidateFreezeRules(
        FertilizerApplication application,
        Guid? newFarmId,
        Guid? newFarmAreaId,
        Guid? newPlantationId,
        Guid? newCropCycleId,
        Guid? newCropCycleStageId,
        Guid? newApplicationMethodId,
        string? newOtherMethodDescription,
        Guid? newIrrigationEventId,
        DateOnly? newPlannedDate,
        DateOnly? newScheduledDate,
        decimal? newPlannedArea,
        Guid? newPlannedAreaUnitId,
        string? newPurposeReason);
}
