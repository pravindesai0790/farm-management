using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.DTOs.MobileSync;

/// <summary>
/// Top-level mobile sync payload envelope returned by bootstrap and delta sync endpoints.
/// Bundles all 15 master cache tables and synchronization metadata into a single atomic payload.
/// </summary>
public sealed record MobileSyncPayloadResponse(
    DateTimeOffset SyncedAtUtc,
    Guid? FarmId,
    IReadOnlyList<CachedFarmDto> Farms,
    IReadOnlyList<CachedFarmAreaDto> FarmAreas,
    IReadOnlyList<CachedCropPlantationDto> CropPlantations,
    IReadOnlyList<CachedCropCycleDto> CropCycles,
    IReadOnlyList<CachedCropCycleStageDto> CropCycleStages,
    IReadOnlyList<CachedWorkerDto> Workers,
    IReadOnlyList<CachedWorkerFarmAssignmentDto> WorkerFarmAssignments,
    IReadOnlyList<CachedLaborCategoryDto> LaborCategories,
    IReadOnlyList<CachedContractorDto> Contractors,
    IReadOnlyList<CachedLaborWageRateDto> LaborWageRates,
    IReadOnlyList<CachedUnitDto> Units,
    IReadOnlyList<CachedStorageLocationDto> StorageLocations,
    IReadOnlyList<CachedInventoryItemDto> InventoryItems,
    IReadOnlyList<CachedPlantProtectionProductDto> PlantProtectionProducts,
    IReadOnlyList<CachedIrrigationMethodDto> IrrigationMethods
);

/// <summary>
/// Cached farm record mirroring Farm.cs for offline master data caching.
/// </summary>
public sealed record CachedFarmDto(
    Guid Id,
    Guid OrganizationId,
    string Name,
    string? Description,
    Guid OwnershipTypeId,
    decimal? TotalArea,
    Guid? AreaUnitId,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? District,
    string? State,
    string? Country,
    string? PostalCode,
    decimal? Latitude,
    decimal? Longitude,
    bool IsActive
);

/// <summary>
/// Cached farm area / parcelation zone mirroring FarmArea.cs.
/// </summary>
public sealed record CachedFarmAreaDto(
    Guid Id,
    Guid OrganizationId,
    Guid FarmId,
    Guid? ParentFarmAreaId,
    string Name,
    string? Description,
    decimal TotalArea,
    Guid AreaUnitId,
    bool IsActive
);

/// <summary>
/// Cached plantation record mirroring CropPlantation.cs.
/// </summary>
public sealed record CachedCropPlantationDto(
    Guid Id,
    Guid OrganizationId,
    Guid FarmId,
    Guid FarmAreaId,
    Guid CropId,
    Guid? VarietyId,
    Guid? LifecycleTemplateId,
    string PlantationName,
    decimal AllocatedArea,
    Guid AreaUnitId,
    DateOnly PlantingDate,
    DateOnly? ExpectedEndDate,
    DateOnly? ActualEndDate,
    int Status,
    Guid? EndReasonId,
    string? EndNotes,
    bool IsActive
);

/// <summary>
/// Cached crop cycle record mirroring CropCycle.cs.
/// </summary>
public sealed record CachedCropCycleDto(
    Guid Id,
    Guid OrganizationId,
    Guid PlantationId,
    Guid? LifecycleTemplateId,
    string CycleName,
    int SeasonYear,
    string? SeasonName,
    DateOnly PlannedStartDate,
    DateOnly? ActualStartDate,
    DateOnly? ExpectedEndDate,
    DateOnly? ActualEndDate,
    int Status,
    Guid? CancellationReasonId,
    string? CancellationNotes
);

/// <summary>
/// Cached crop cycle stage record mirroring CropCycleStage.cs.
/// </summary>
public sealed record CachedCropCycleStageDto(
    Guid Id,
    Guid CropCycleId,
    Guid LifecycleTemplateStageId,
    string StageName,
    int SequenceNumber,
    int? ExpectedDurationDays,
    DateOnly? PlannedStartDate,
    DateOnly? PlannedEndDate,
    DateOnly? ActualStartDate,
    DateOnly? ActualEndDate,
    int Status,
    string? Notes
);

/// <summary>
/// Cached worker profile mirroring Worker.cs.
/// </summary>
public sealed record CachedWorkerDto(
    Guid Id,
    Guid OrganizationId,
    string FirstName,
    string? LastName,
    string DisplayName,
    int Gender,
    string? MobileNumber,
    string? AlternateMobileNumber,
    Guid? LaborCategoryId,
    int EmploymentType,
    Guid? ContractorId,
    DateOnly? JoiningDate,
    DateOnly? LeavingDate,
    string? Notes,
    bool IsActive,
    int IsLocallyCreated = 0
);

/// <summary>
/// Cached worker farm assignment mirroring WorkerFarmAssignment.cs.
/// </summary>
public sealed record CachedWorkerFarmAssignmentDto(
    Guid Id,
    Guid OrganizationId,
    Guid WorkerId,
    Guid FarmId,
    DateOnly AssignedFrom,
    DateOnly? AssignedTo,
    bool IsActive,
    string? Notes
);

/// <summary>
/// Cached labor skill category mirroring LaborCategory.cs.
/// </summary>
public sealed record CachedLaborCategoryDto(
    Guid Id,
    Guid? OrganizationId,
    string Name,
    string? Description,
    bool IsSystem,
    bool IsActive
);

/// <summary>
/// Cached labor contractor profile mirroring Contractor.cs.
/// </summary>
public sealed record CachedContractorDto(
    Guid Id,
    Guid OrganizationId,
    string Name,
    string? ContactPerson,
    string? PhoneNumber,
    string? Email,
    string? Address,
    string? Notes,
    bool IsActive
);

/// <summary>
/// Cached labor wage rate structure mirroring LaborWageRate.cs.
/// </summary>
public sealed record CachedLaborWageRateDto(
    Guid Id,
    Guid OrganizationId,
    int Gender,
    int WageType,
    decimal WageRate,
    Guid CurrencyId,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string? Notes,
    bool IsActive
);

/// <summary>
/// Cached measurement unit mirroring Unit.cs.
/// </summary>
public sealed record CachedUnitDto(
    Guid Id,
    Guid? OrganizationId,
    string Code,
    string Name,
    string Symbol,
    int UnitCategory,
    string? BaseUnitCode,
    decimal? ConversionFactor,
    bool IsSystem,
    bool IsActive,
    int DisplayOrder
);

/// <summary>
/// Cached farm warehouse / storage location mirroring StorageLocation.cs.
/// </summary>
public sealed record CachedStorageLocationDto(
    Guid Id,
    Guid OrganizationId,
    Guid FarmId,
    string Name,
    string? Description,
    bool IsActive
);

/// <summary>
/// Cached inventory item catalog entry mirroring InventoryItem.cs.
/// </summary>
public sealed record CachedInventoryItemDto(
    Guid Id,
    Guid OrganizationId,
    string Name,
    string? Sku,
    string? Description,
    Guid? CategoryId,
    Guid StockUnitId,
    bool IsActive
);

/// <summary>
/// Cached plant protection product profile mirroring PlantProtectionProduct.cs.
/// </summary>
public sealed record CachedPlantProtectionProductDto(
    Guid Id,
    Guid OrganizationId,
    Guid InventoryItemId,
    Guid ProductTypeId,
    string? ActiveIngredient,
    string? Manufacturer,
    string? Description,
    bool IsActive
);

/// <summary>
/// Cached irrigation method lookup mirroring IrrigationMethod.cs.
/// </summary>
public sealed record CachedIrrigationMethodDto(
    Guid Id,
    Guid? OrganizationId,
    string Code,
    string Name,
    string? Description,
    bool IsSystem,
    bool IsActive,
    int DisplayOrder
);
