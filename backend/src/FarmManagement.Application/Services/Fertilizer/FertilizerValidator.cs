using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.Interfaces.Fertilizer;
using FarmManagement.Domain.Constants;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.Services.Fertilizer;

public sealed class FertilizerValidator(IFertilizerApplicationStore store) : IFertilizerValidator
{
    public async Task ValidateHierarchyAsync(
        Guid organizationId,
        Guid? farmId,
        Guid? farmAreaId,
        Guid? plantationId,
        Guid? cropCycleId,
        Guid? cropCycleStageId,
        bool isPreStart = true,
        CancellationToken cancellationToken = default)
    {
        if (farmId.HasValue && farmId.Value != Guid.Empty)
        {
            var farm = await store.FindFarmAsync(farmId.Value, organizationId, cancellationToken)
                ?? throw new ResourceNotFoundException("The farm was not found.");

            if (isPreStart && !farm.IsActive)
            {
                throw Validation("farmId", "The selected farm is inactive.");
            }
        }

        if (farmAreaId.HasValue && farmAreaId.Value != Guid.Empty)
        {
            if (!farmId.HasValue || farmId.Value == Guid.Empty)
            {
                throw Validation("farmAreaId", "A farm is required when specifying a farm area.");
            }

            var area = await store.FindFarmAreaAsync(farmAreaId.Value, organizationId, cancellationToken)
                ?? throw new ResourceNotFoundException("The farm area was not found.");

            if (area.FarmId != farmId.Value)
            {
                throw Validation("farmAreaId", "The farm area does not belong to the selected farm.");
            }

            if (isPreStart && !area.IsActive)
            {
                throw Validation("farmAreaId", "The selected farm area is inactive.");
            }
        }

        if (plantationId.HasValue && plantationId.Value != Guid.Empty)
        {
            if (!farmAreaId.HasValue || farmAreaId.Value == Guid.Empty)
            {
                throw Validation("plantationId", "A farm area is required when specifying a plantation.");
            }

            var plantation = await store.FindPlantationAsync(plantationId.Value, organizationId, cancellationToken)
                ?? throw new ResourceNotFoundException("The plantation was not found.");

            if (farmId.HasValue && plantation.FarmId != farmId.Value)
            {
                throw Validation("plantationId", "The plantation does not belong to the selected farm.");
            }

            if (plantation.FarmAreaId != farmAreaId.Value)
            {
                throw Validation("plantationId", "The plantation does not belong to the selected farm area.");
            }

            if (isPreStart && (plantation.Status == PlantationStatus.Terminated || plantation.Status == PlantationStatus.Archived))
            {
                throw Validation("plantationId", "The selected plantation is inactive.");
            }
        }

        if (cropCycleId.HasValue && cropCycleId.Value != Guid.Empty)
        {
            if (!plantationId.HasValue || plantationId.Value == Guid.Empty)
            {
                throw Validation("cropCycleId", "A plantation is required when specifying a crop cycle.");
            }

            var cycle = await store.FindCropCycleAsync(cropCycleId.Value, organizationId, cancellationToken)
                ?? throw new ResourceNotFoundException("The crop cycle was not found.");

            if (cycle.PlantationId != plantationId.Value)
            {
                throw Validation("cropCycleId", "The crop cycle does not belong to the selected plantation.");
            }

            if (isPreStart && (cycle.Status == CropCycleStatus.Cancelled || cycle.Status == CropCycleStatus.Completed))
            {
                throw Validation("cropCycleId", "The selected crop cycle is completed or cancelled.");
            }
        }

        if (cropCycleStageId.HasValue && cropCycleStageId.Value != Guid.Empty)
        {
            if (!cropCycleId.HasValue || cropCycleId.Value == Guid.Empty)
            {
                throw Validation("cropCycleStageId", "A crop cycle is required when specifying a crop cycle stage.");
            }

            var stage = await store.FindCropCycleStageAsync(cropCycleStageId.Value, organizationId, cancellationToken)
                ?? throw new ResourceNotFoundException("The crop cycle stage was not found.");

            if (stage.CropCycleId != cropCycleId.Value)
            {
                throw Validation("cropCycleStageId", "The crop cycle stage does not belong to the selected crop cycle.");
            }
        }
    }

    public async Task ValidateProductEligibilityAsync(
        Guid organizationId,
        Guid inventoryItemId,
        bool isPreStart = true,
        CancellationToken cancellationToken = default)
    {
        if (inventoryItemId == Guid.Empty)
        {
            throw Validation("products", "An inventory item identifier is required.");
        }

        var item = await store.FindInventoryItemAsync(inventoryItemId, organizationId, cancellationToken)
            ?? throw new ResourceNotFoundException("The inventory item was not found.");

        if (isPreStart && !item.IsActive)
        {
            throw Validation("products", $"The inventory item '{item.Name}' is inactive.");
        }

        var isFertilizer = item.CategoryId == FarmInventoryCategories.FertilizersSoilId ||
                           (item.Category != null && string.Equals(item.Category.Code, "FERTILIZERS_SOIL", StringComparison.OrdinalIgnoreCase));

        if (!isFertilizer)
        {
            throw Validation("products", $"The inventory item '{item.Name}' must belong to the Fertilizers & Soil Amendments category.");
        }

        var hasActivePpp = await store.HasActivePlantProtectionProfileAsync(inventoryItemId, organizationId, cancellationToken);
        if (hasActivePpp)
        {
            throw Validation("products", $"The inventory item '{item.Name}' is classified as a plant protection product and cannot be used in fertilizer applications.");
        }

        if (item.StockUnitId == Guid.Empty)
        {
            throw Validation("products", $"The inventory item '{item.Name}' does not have a valid stock unit.");
        }
    }

    public async Task ValidateStorageLocationAsync(
        Guid organizationId,
        Guid farmId,
        Guid storageLocationId,
        bool isPreStart = true,
        CancellationToken cancellationToken = default)
    {
        if (storageLocationId == Guid.Empty)
        {
            throw Validation("storageLocationId", "A storage location identifier is required.");
        }

        var location = await store.FindStorageLocationAsync(storageLocationId, organizationId, cancellationToken)
            ?? throw new ResourceNotFoundException("The storage location was not found.");

        if (farmId != Guid.Empty && location.FarmId != farmId)
        {
            throw Validation("storageLocationId", "The storage location does not belong to the selected farm.");
        }

        if (isPreStart && !location.IsActive)
        {
            throw Validation("storageLocationId", "The selected storage location is inactive.");
        }
    }

    public async Task ValidateApplicationMethodAsync(
        Guid organizationId,
        Guid? applicationMethodId,
        string? otherMethodDescription,
        CancellationToken cancellationToken = default)
    {
        if (applicationMethodId.HasValue && applicationMethodId.Value != Guid.Empty)
        {
            var method = await store.FindApplicationMethodAsync(applicationMethodId.Value, organizationId, cancellationToken)
                ?? throw new ResourceNotFoundException("The application method was not found.");

            if (!method.IsActive)
            {
                throw Validation("applicationMethodId", "The selected application method is inactive.");
            }

            if (!string.Equals(method.Module, "FERTILIZER", StringComparison.OrdinalIgnoreCase))
            {
                throw Validation("applicationMethodId", "The application method is not a fertilizer application method.");
            }

            if (string.Equals(method.Code, "OTHER", StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(otherMethodDescription))
            {
                throw Validation("otherMethodDescription", "A description is required when using the Other application method.");
            }
        }
    }

    public async Task ValidateIrrigationLinkAsync(
        Guid organizationId,
        Guid? applicationMethodId,
        Guid? irrigationEventId,
        Guid? farmId,
        Guid? farmAreaId,
        CancellationToken cancellationToken = default)
    {
        if (irrigationEventId.HasValue && irrigationEventId.Value != Guid.Empty)
        {
            if (!applicationMethodId.HasValue || applicationMethodId.Value == Guid.Empty)
            {
                throw Validation("irrigationEventId", "An irrigation link is only allowed for the Fertigation application method.");
            }

            var method = await store.FindApplicationMethodAsync(applicationMethodId.Value, organizationId, cancellationToken)
                ?? throw new ResourceNotFoundException("The application method was not found.");

            if (!string.Equals(method.Code, "FERTIGATION", StringComparison.OrdinalIgnoreCase))
            {
                throw Validation("irrigationEventId", "An irrigation link is only allowed for the Fertigation application method.");
            }

            var irrigation = await store.FindIrrigationEventAsync(irrigationEventId.Value, organizationId, cancellationToken)
                ?? throw new ResourceNotFoundException("The irrigation event was not found.");

            if (farmId.HasValue && farmId.Value != Guid.Empty && irrigation.FarmId != farmId.Value)
            {
                throw Validation("irrigationEventId", "The linked irrigation event does not belong to the selected farm.");
            }

            if (farmAreaId.HasValue && farmAreaId.Value != Guid.Empty && irrigation.FarmAreaId != farmAreaId.Value)
            {
                throw Validation("irrigationEventId", "The linked irrigation event does not belong to the selected farm area.");
            }
        }
    }

    public async Task ValidateAreaAndUnitsAsync(
        Guid organizationId,
        decimal? plannedArea,
        Guid? plannedAreaUnitId,
        decimal? actualArea,
        Guid? actualAreaUnitId,
        CancellationToken cancellationToken = default)
    {
        if (plannedArea.HasValue && plannedArea.Value <= 0)
        {
            throw Validation("plannedArea", "Planned area must be greater than zero.");
        }

        if (plannedArea.HasValue && (!plannedAreaUnitId.HasValue || plannedAreaUnitId.Value == Guid.Empty))
        {
            throw Validation("plannedAreaUnitId", "An area unit is required when planned area is specified.");
        }

        if (!plannedArea.HasValue && plannedAreaUnitId.HasValue && plannedAreaUnitId.Value != Guid.Empty)
        {
            throw Validation("plannedArea", "A planned area is required when area unit is specified.");
        }

        if (plannedAreaUnitId.HasValue && plannedAreaUnitId.Value != Guid.Empty)
        {
            var unit = await store.FindUnitAsync(plannedAreaUnitId.Value, organizationId, cancellationToken)
                ?? throw new ResourceNotFoundException("The selected area unit was not found.");

            if (!unit.IsActive)
            {
                throw Validation("plannedAreaUnitId", "The selected area unit is inactive.");
            }
        }

        if (actualArea.HasValue && actualArea.Value <= 0)
        {
            throw Validation("actualArea", "Actual area must be greater than zero.");
        }

        if (actualArea.HasValue && (!actualAreaUnitId.HasValue || actualAreaUnitId.Value == Guid.Empty))
        {
            throw Validation("actualAreaUnitId", "An area unit is required when actual area is specified.");
        }

        if (!actualArea.HasValue && actualAreaUnitId.HasValue && actualAreaUnitId.Value != Guid.Empty)
        {
            throw Validation("actualArea", "An actual area is required when area unit is specified.");
        }

        if (actualAreaUnitId.HasValue && actualAreaUnitId.Value != Guid.Empty)
        {
            var unit = await store.FindUnitAsync(actualAreaUnitId.Value, organizationId, cancellationToken)
                ?? throw new ResourceNotFoundException("The selected area unit was not found.");

            if (!unit.IsActive)
            {
                throw Validation("actualAreaUnitId", "The selected area unit is inactive.");
            }
        }
    }

    public void ValidateFreezeRules(
        FertilizerApplication application,
        Guid? newFarmId,
        Guid? newFarmAreaId,
        Guid? newPlantationId,
        Guid? newCropCycleId,
        Guid? newCropCycleStageId,
        Guid? newApplicationMethodId,
        string? newOtherMethodDescription,
        Guid? newIrrigationEventId,
        DateTimeOffset? newPlannedDate,
        DateTimeOffset? newScheduledDate,
        decimal? newPlannedArea,
        Guid? newPlannedAreaUnitId,
        string? newPurposeReason)
    {
        ArgumentNullException.ThrowIfNull(application);

        if (application.Status is FertilizerApplicationStatus.InProgress
            or FertilizerApplicationStatus.Completed
            or FertilizerApplicationStatus.Cancelled)
        {
            var changed = application.FarmId != newFarmId
                || application.FarmAreaId != newFarmAreaId
                || application.PlantationId != newPlantationId
                || application.CropCycleId != newCropCycleId
                || application.CropCycleStageId != newCropCycleStageId
                || application.ApplicationMethodId != newApplicationMethodId
                || !string.Equals(application.OtherMethodDescription, newOtherMethodDescription, StringComparison.Ordinal)
                || application.IrrigationEventId != newIrrigationEventId
                || application.PlannedDate != newPlannedDate
                || application.ScheduledDate != newScheduledDate
                || application.PlannedArea != newPlannedArea
                || application.PlannedAreaUnitId != newPlannedAreaUnitId
                || !string.Equals(application.PurposeReason, newPurposeReason, StringComparison.Ordinal);

            if (changed)
            {
                throw new ValidationException(
                    "Cannot modify frozen fields after application start or completion.",
                    new Dictionary<string, string[]>
                    {
                        ["status"] = ["Operational context, method, irrigation link, planned dates, planned area, and purpose are frozen after start."]
                    });
            }
        }
    }

    private static ValidationException Validation(string property, string message) =>
        new("One or more validation errors occurred.", new Dictionary<string, string[]>
        {
            [property] = [message]
        });
}
