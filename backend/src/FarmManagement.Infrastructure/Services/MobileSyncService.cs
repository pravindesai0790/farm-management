using FarmManagement.Application.DTOs.MobileSync;
using FarmManagement.Application.Interfaces.MobileSync;
using FarmManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FarmManagement.Infrastructure.Services;

/// <summary>
/// Implements the high-performance aggregation and delta extraction service for mobile offline synchronization.
/// Queries PostgreSQL via ApplicationDbContext using AsNoTracking to minimize overhead.
/// Note: To support PostgreSQL string-converted enums, entity queries read domain enums directly
/// via EF Core ValueConverters and perform in-memory integer conversions post-materialization,
/// preventing invalid SQL type casts (e.g., status::int).
/// </summary>
public sealed class MobileSyncService(ApplicationDbContext dbContext) : IMobileSyncService
{
    /// <inheritdoc />
    public async Task<MobileSyncPayloadResponse> GetBootstrapPayloadAsync(
        Guid organizationId,
        Guid? farmId,
        CancellationToken cancellationToken = default)
    {
        var syncedAtUtc = DateTimeOffset.UtcNow;

        // 1. Farms (Tenant wide lookup)
        var farms = await dbContext.Farms
            .AsNoTracking()
            .Where(f => f.OrganizationId == organizationId)
            .Select(f => new CachedFarmDto(
                f.Id,
                f.OrganizationId,
                f.Name,
                f.Description,
                f.OwnershipTypeId,
                f.TotalArea,
                f.AreaUnitId,
                f.AddressLine1,
                f.AddressLine2,
                f.City,
                f.District,
                f.State,
                f.Country,
                f.PostalCode,
                f.Latitude,
                f.Longitude,
                f.IsActive
            ))
            .ToListAsync(cancellationToken);

        // 2. Farm Areas (Scoped to Farm if specified)
        var farmAreasQuery = dbContext.FarmAreas
            .AsNoTracking()
            .Where(fa => fa.OrganizationId == organizationId);
        if (farmId.HasValue)
        {
            farmAreasQuery = farmAreasQuery.Where(fa => fa.FarmId == farmId.Value);
        }
        var farmAreas = await farmAreasQuery
            .Select(fa => new CachedFarmAreaDto(
                fa.Id,
                fa.OrganizationId,
                fa.FarmId,
                fa.ParentFarmAreaId,
                fa.Name,
                fa.Description,
                fa.TotalArea,
                fa.AreaUnitId,
                fa.IsActive
            ))
            .ToListAsync(cancellationToken);

        // 3. Crop Plantations (Scoped to Farm if specified)
        // Fetches raw properties first so EF Core uses the string-to-enum ValueConverter without SQL cast errors
        var plantationsQuery = dbContext.CropPlantations
            .AsNoTracking()
            .Where(p => p.OrganizationId == organizationId);
        if (farmId.HasValue)
        {
            plantationsQuery = plantationsQuery.Where(p => p.FarmId == farmId.Value);
        }
        var rawPlantations = await plantationsQuery
            .Select(p => new
            {
                p.Id,
                p.OrganizationId,
                p.FarmId,
                p.FarmAreaId,
                p.CropId,
                p.VarietyId,
                p.LifecycleTemplateId,
                p.PlantationName,
                p.AllocatedArea,
                p.AreaUnitId,
                p.PlantingDate,
                p.ExpectedEndDate,
                p.ActualEndDate,
                p.Status,
                p.EndReasonId,
                p.EndNotes,
                p.IsActive
            })
            .ToListAsync(cancellationToken);

        var plantations = rawPlantations
            .Select(p => new CachedCropPlantationDto(
                p.Id,
                p.OrganizationId,
                p.FarmId,
                p.FarmAreaId,
                p.CropId,
                p.VarietyId,
                p.LifecycleTemplateId,
                p.PlantationName,
                p.AllocatedArea,
                p.AreaUnitId,
                p.PlantingDate,
                p.ExpectedEndDate,
                p.ActualEndDate,
                (int)p.Status,
                p.EndReasonId,
                p.EndNotes,
                p.IsActive
            ))
            .ToList();

        // 4. Crop Cycles (Scoped via Plantation IDs)
        var targetPlantationIds = rawPlantations.Select(p => p.Id).ToHashSet();
        var cropCyclesQuery = dbContext.CropCycles
            .AsNoTracking()
            .Where(c => c.OrganizationId == organizationId);
        if (farmId.HasValue)
        {
            cropCyclesQuery = cropCyclesQuery.Where(c => targetPlantationIds.Contains(c.PlantationId));
        }
        var rawCropCycles = await cropCyclesQuery
            .Select(c => new
            {
                c.Id,
                c.OrganizationId,
                c.PlantationId,
                c.LifecycleTemplateId,
                c.CycleName,
                c.SeasonYear,
                c.SeasonName,
                c.PlannedStartDate,
                c.ActualStartDate,
                c.ExpectedEndDate,
                c.ActualEndDate,
                c.Status,
                c.CancellationReasonId,
                c.CancellationNotes
            })
            .ToListAsync(cancellationToken);

        var cropCycles = rawCropCycles
            .Select(c => new CachedCropCycleDto(
                c.Id,
                c.OrganizationId,
                c.PlantationId,
                c.LifecycleTemplateId,
                c.CycleName,
                c.SeasonYear,
                c.SeasonName,
                c.PlannedStartDate,
                c.ActualStartDate,
                c.ExpectedEndDate,
                c.ActualEndDate,
                (int)c.Status,
                c.CancellationReasonId,
                c.CancellationNotes
            ))
            .ToList();

        // 5. Crop Cycle Stages (Scoped via CropCycle IDs)
        var targetCycleIds = rawCropCycles.Select(c => c.Id).ToHashSet();
        var cropCycleStagesQuery = dbContext.CropCycleStages
            .AsNoTracking()
            .Where(s => s.CropCycle.OrganizationId == organizationId);
        if (farmId.HasValue)
        {
            cropCycleStagesQuery = cropCycleStagesQuery.Where(s => targetCycleIds.Contains(s.CropCycleId));
        }
        var rawCropCycleStages = await cropCycleStagesQuery
            .Select(s => new
            {
                s.Id,
                s.CropCycleId,
                s.LifecycleTemplateStageId,
                s.StageName,
                s.SequenceNumber,
                s.ExpectedDurationDays,
                s.PlannedStartDate,
                s.PlannedEndDate,
                s.ActualStartDate,
                s.ActualEndDate,
                s.Status,
                s.Notes
            })
            .ToListAsync(cancellationToken);

        var cropCycleStages = rawCropCycleStages
            .Select(s => new CachedCropCycleStageDto(
                s.Id,
                s.CropCycleId,
                s.LifecycleTemplateStageId,
                s.StageName,
                s.SequenceNumber,
                s.ExpectedDurationDays,
                s.PlannedStartDate,
                s.PlannedEndDate,
                s.ActualStartDate,
                s.ActualEndDate,
                (int)s.Status,
                s.Notes
            ))
            .ToList();

        // 6. Workers (Tenant wide)
        // Project Gender and EmploymentType as enums before in-memory integer casting
        var rawWorkers = await dbContext.Workers
            .AsNoTracking()
            .Where(w => w.OrganizationId == organizationId)
            .Select(w => new
            {
                w.Id,
                w.OrganizationId,
                w.FirstName,
                w.LastName,
                w.DisplayName,
                w.Gender,
                w.MobileNumber,
                w.AlternateMobileNumber,
                w.LaborCategoryId,
                w.EmploymentType,
                w.ContractorId,
                w.JoiningDate,
                w.LeavingDate,
                w.Notes,
                w.IsActive
            })
            .ToListAsync(cancellationToken);

        var workers = rawWorkers
            .Select(w => new CachedWorkerDto(
                w.Id,
                w.OrganizationId,
                w.FirstName,
                w.LastName,
                w.DisplayName,
                (int)w.Gender,
                w.MobileNumber,
                w.AlternateMobileNumber,
                w.LaborCategoryId,
                (int)w.EmploymentType,
                w.ContractorId,
                w.JoiningDate,
                w.LeavingDate,
                w.Notes,
                w.IsActive,
                0
            ))
            .ToList();

        // 7. Worker Farm Assignments (Scoped to Farm if specified)
        var assignmentsQuery = dbContext.WorkerFarmAssignments
            .AsNoTracking()
            .Where(a => a.OrganizationId == organizationId);
        if (farmId.HasValue)
        {
            assignmentsQuery = assignmentsQuery.Where(a => a.FarmId == farmId.Value);
        }
        var assignments = await assignmentsQuery
            .Select(a => new CachedWorkerFarmAssignmentDto(
                a.Id,
                a.OrganizationId,
                a.WorkerId,
                a.FarmId,
                a.AssignedFrom,
                a.AssignedTo,
                a.IsActive,
                a.Notes
            ))
            .ToListAsync(cancellationToken);

        // 8. Labor Categories (System or Organization)
        var laborCategories = await dbContext.LaborCategories
            .AsNoTracking()
            .Where(lc => lc.OrganizationId == null || lc.OrganizationId == organizationId)
            .Select(lc => new CachedLaborCategoryDto(
                lc.Id,
                lc.OrganizationId,
                lc.Name,
                lc.Description,
                lc.IsSystem,
                lc.IsActive
            ))
            .ToListAsync(cancellationToken);

        // 9. Contractors (Tenant wide)
        var contractors = await dbContext.Contractors
            .AsNoTracking()
            .Where(c => c.OrganizationId == organizationId)
            .Select(c => new CachedContractorDto(
                c.Id,
                c.OrganizationId,
                c.Name,
                c.ContactPerson,
                c.PhoneNumber,
                c.Email,
                c.Address,
                c.Notes,
                c.IsActive
            ))
            .ToListAsync(cancellationToken);

        // 10. Labor Wage Rates (Tenant wide)
        var rawWageRates = await dbContext.LaborWageRates
            .AsNoTracking()
            .Where(r => r.OrganizationId == organizationId)
            .Select(r => new
            {
                r.Id,
                r.OrganizationId,
                r.Gender,
                r.WageType,
                r.WageRate,
                r.CurrencyId,
                r.EffectiveFrom,
                r.EffectiveTo,
                r.Notes,
                r.IsActive
            })
            .ToListAsync(cancellationToken);

        var wageRates = rawWageRates
            .Select(r => new CachedLaborWageRateDto(
                r.Id,
                r.OrganizationId,
                (int)r.Gender,
                (int)r.WageType,
                r.WageRate,
                r.CurrencyId,
                r.EffectiveFrom,
                r.EffectiveTo,
                r.Notes,
                r.IsActive
            ))
            .ToList();

        // 11. Units (System or Organization)
        var rawUnits = await dbContext.Units
            .AsNoTracking()
            .Where(u => u.OrganizationId == null || u.OrganizationId == organizationId)
            .Select(u => new
            {
                u.Id,
                u.OrganizationId,
                u.Code,
                u.Name,
                u.Symbol,
                u.UnitCategory,
                u.BaseUnitCode,
                u.ConversionFactor,
                u.IsSystem,
                u.IsActive,
                u.DisplayOrder
            })
            .ToListAsync(cancellationToken);

        var units = rawUnits
            .Select(u => new CachedUnitDto(
                u.Id,
                u.OrganizationId,
                u.Code,
                u.Name,
                u.Symbol,
                (int)u.UnitCategory,
                u.BaseUnitCode,
                u.ConversionFactor,
                u.IsSystem,
                u.IsActive,
                u.DisplayOrder
            ))
            .ToList();

        // 12. Storage Locations (Scoped to Farm if specified)
        var storageLocationsQuery = dbContext.StorageLocations
            .AsNoTracking()
            .Where(sl => sl.OrganizationId == organizationId);
        if (farmId.HasValue)
        {
            storageLocationsQuery = storageLocationsQuery.Where(sl => sl.FarmId == farmId.Value);
        }
        var storageLocations = await storageLocationsQuery
            .Select(sl => new CachedStorageLocationDto(
                sl.Id,
                sl.OrganizationId,
                sl.FarmId,
                sl.Name,
                sl.Description,
                sl.IsActive
            ))
            .ToListAsync(cancellationToken);

        // 13. Inventory Items (Tenant wide)
        var inventoryItems = await dbContext.InventoryItems
            .AsNoTracking()
            .Where(ii => ii.OrganizationId == organizationId)
            .Select(ii => new CachedInventoryItemDto(
                ii.Id,
                ii.OrganizationId,
                ii.Name,
                ii.Sku,
                ii.Description,
                ii.CategoryId,
                ii.StockUnitId,
                ii.IsActive
            ))
            .ToListAsync(cancellationToken);

        // 14. Plant Protection Products (Tenant wide)
        var plantProtectionProducts = await dbContext.PlantProtectionProducts
            .AsNoTracking()
            .Where(ppp => ppp.OrganizationId == organizationId)
            .Select(ppp => new CachedPlantProtectionProductDto(
                ppp.Id,
                ppp.OrganizationId,
                ppp.InventoryItemId,
                ppp.ProductTypeId,
                ppp.ActiveIngredient,
                ppp.Manufacturer,
                ppp.Description,
                ppp.IsActive
            ))
            .ToListAsync(cancellationToken);

        // 15. Irrigation Methods (System or Organization)
        var irrigationMethods = await dbContext.IrrigationMethods
            .AsNoTracking()
            .Where(im => im.OrganizationId == null || im.OrganizationId == organizationId)
            .Select(im => new CachedIrrigationMethodDto(
                im.Id,
                im.OrganizationId,
                im.Code,
                im.Name,
                im.Description,
                im.IsSystem,
                im.IsActive,
                im.DisplayOrder
            ))
            .ToListAsync(cancellationToken);

        return new MobileSyncPayloadResponse(
            syncedAtUtc,
            farmId,
            farms,
            farmAreas,
            plantations,
            cropCycles,
            cropCycleStages,
            workers,
            assignments,
            laborCategories,
            contractors,
            wageRates,
            units,
            storageLocations,
            inventoryItems,
            plantProtectionProducts,
            irrigationMethods
        );
    }

    /// <inheritdoc />
    public async Task<MobileSyncPayloadResponse> GetDeltaPayloadAsync(
        Guid organizationId,
        Guid? farmId,
        DateTimeOffset sinceUtc,
        CancellationToken cancellationToken = default)
    {
        var syncedAtUtc = DateTimeOffset.UtcNow;

        // 1. Farms delta
        var farms = await dbContext.Farms
            .AsNoTracking()
            .Where(f => f.OrganizationId == organizationId &&
                        (f.CreatedAt > sinceUtc || (f.UpdatedAt != null && f.UpdatedAt > sinceUtc)))
            .Select(f => new CachedFarmDto(
                f.Id,
                f.OrganizationId,
                f.Name,
                f.Description,
                f.OwnershipTypeId,
                f.TotalArea,
                f.AreaUnitId,
                f.AddressLine1,
                f.AddressLine2,
                f.City,
                f.District,
                f.State,
                f.Country,
                f.PostalCode,
                f.Latitude,
                f.Longitude,
                f.IsActive
            ))
            .ToListAsync(cancellationToken);

        // 2. Farm Areas delta
        var farmAreasQuery = dbContext.FarmAreas
            .AsNoTracking()
            .Where(fa => fa.OrganizationId == organizationId &&
                         (fa.CreatedAt > sinceUtc || (fa.UpdatedAt != null && fa.UpdatedAt > sinceUtc)));
        if (farmId.HasValue)
        {
            farmAreasQuery = farmAreasQuery.Where(fa => fa.FarmId == farmId.Value);
        }
        var farmAreas = await farmAreasQuery
            .Select(fa => new CachedFarmAreaDto(
                fa.Id,
                fa.OrganizationId,
                fa.FarmId,
                fa.ParentFarmAreaId,
                fa.Name,
                fa.Description,
                fa.TotalArea,
                fa.AreaUnitId,
                fa.IsActive
            ))
            .ToListAsync(cancellationToken);

        // 3. Crop Plantations delta
        var plantationsQuery = dbContext.CropPlantations
            .AsNoTracking()
            .Where(p => p.OrganizationId == organizationId &&
                        (p.CreatedAt > sinceUtc || (p.UpdatedAt != null && p.UpdatedAt > sinceUtc)));
        if (farmId.HasValue)
        {
            plantationsQuery = plantationsQuery.Where(p => p.FarmId == farmId.Value);
        }
        var rawPlantations = await plantationsQuery
            .Select(p => new
            {
                p.Id,
                p.OrganizationId,
                p.FarmId,
                p.FarmAreaId,
                p.CropId,
                p.VarietyId,
                p.LifecycleTemplateId,
                p.PlantationName,
                p.AllocatedArea,
                p.AreaUnitId,
                p.PlantingDate,
                p.ExpectedEndDate,
                p.ActualEndDate,
                p.Status,
                p.EndReasonId,
                p.EndNotes,
                p.IsActive
            })
            .ToListAsync(cancellationToken);

        var plantations = rawPlantations
            .Select(p => new CachedCropPlantationDto(
                p.Id,
                p.OrganizationId,
                p.FarmId,
                p.FarmAreaId,
                p.CropId,
                p.VarietyId,
                p.LifecycleTemplateId,
                p.PlantationName,
                p.AllocatedArea,
                p.AreaUnitId,
                p.PlantingDate,
                p.ExpectedEndDate,
                p.ActualEndDate,
                (int)p.Status,
                p.EndReasonId,
                p.EndNotes,
                p.IsActive
            ))
            .ToList();

        // 4. Crop Cycles delta
        var targetPlantationIds = dbContext.CropPlantations
            .AsNoTracking()
            .Where(p => p.OrganizationId == organizationId);
        if (farmId.HasValue)
        {
            targetPlantationIds = targetPlantationIds.Where(p => p.FarmId == farmId.Value);
        }
        var plantationIdSet = targetPlantationIds.Select(p => p.Id);

        var cropCyclesQuery = dbContext.CropCycles
            .AsNoTracking()
            .Where(c => c.OrganizationId == organizationId &&
                        (c.CreatedAt > sinceUtc || (c.UpdatedAt != null && c.UpdatedAt > sinceUtc)));
        if (farmId.HasValue)
        {
            cropCyclesQuery = cropCyclesQuery.Where(c => plantationIdSet.Contains(c.PlantationId));
        }
        var rawCropCycles = await cropCyclesQuery
            .Select(c => new
            {
                c.Id,
                c.OrganizationId,
                c.PlantationId,
                c.LifecycleTemplateId,
                c.CycleName,
                c.SeasonYear,
                c.SeasonName,
                c.PlannedStartDate,
                c.ActualStartDate,
                c.ExpectedEndDate,
                c.ActualEndDate,
                c.Status,
                c.CancellationReasonId,
                c.CancellationNotes
            })
            .ToListAsync(cancellationToken);

        var cropCycles = rawCropCycles
            .Select(c => new CachedCropCycleDto(
                c.Id,
                c.OrganizationId,
                c.PlantationId,
                c.LifecycleTemplateId,
                c.CycleName,
                c.SeasonYear,
                c.SeasonName,
                c.PlannedStartDate,
                c.ActualStartDate,
                c.ExpectedEndDate,
                c.ActualEndDate,
                (int)c.Status,
                c.CancellationReasonId,
                c.CancellationNotes
            ))
            .ToList();

        // 5. Crop Cycle Stages delta
        var targetCycleIdSet = dbContext.CropCycles
            .AsNoTracking()
            .Where(c => c.OrganizationId == organizationId);
        if (farmId.HasValue)
        {
            targetCycleIdSet = targetCycleIdSet.Where(c => plantationIdSet.Contains(c.PlantationId));
        }
        var cycleIdSet = targetCycleIdSet.Select(c => c.Id);

        var cropCycleStagesQuery = dbContext.CropCycleStages
            .AsNoTracking()
            .Where(s => s.CropCycle.OrganizationId == organizationId &&
                        (s.CreatedAt > sinceUtc || (s.UpdatedAt != null && s.UpdatedAt > sinceUtc)));
        if (farmId.HasValue)
        {
            cropCycleStagesQuery = cropCycleStagesQuery.Where(s => cycleIdSet.Contains(s.CropCycleId));
        }
        var rawCropCycleStages = await cropCycleStagesQuery
            .Select(s => new
            {
                s.Id,
                s.CropCycleId,
                s.LifecycleTemplateStageId,
                s.StageName,
                s.SequenceNumber,
                s.ExpectedDurationDays,
                s.PlannedStartDate,
                s.PlannedEndDate,
                s.ActualStartDate,
                s.ActualEndDate,
                s.Status,
                s.Notes
            })
            .ToListAsync(cancellationToken);

        var cropCycleStages = rawCropCycleStages
            .Select(s => new CachedCropCycleStageDto(
                s.Id,
                s.CropCycleId,
                s.LifecycleTemplateStageId,
                s.StageName,
                s.SequenceNumber,
                s.ExpectedDurationDays,
                s.PlannedStartDate,
                s.PlannedEndDate,
                s.ActualStartDate,
                s.ActualEndDate,
                (int)s.Status,
                s.Notes
            ))
            .ToList();

        // 6. Workers delta
        var rawWorkers = await dbContext.Workers
            .AsNoTracking()
            .Where(w => w.OrganizationId == organizationId &&
                        (w.CreatedAt > sinceUtc || (w.UpdatedAt != null && w.UpdatedAt > sinceUtc)))
            .Select(w => new
            {
                w.Id,
                w.OrganizationId,
                w.FirstName,
                w.LastName,
                w.DisplayName,
                w.Gender,
                w.MobileNumber,
                w.AlternateMobileNumber,
                w.LaborCategoryId,
                w.EmploymentType,
                w.ContractorId,
                w.JoiningDate,
                w.LeavingDate,
                w.Notes,
                w.IsActive
            })
            .ToListAsync(cancellationToken);

        var workers = rawWorkers
            .Select(w => new CachedWorkerDto(
                w.Id,
                w.OrganizationId,
                w.FirstName,
                w.LastName,
                w.DisplayName,
                (int)w.Gender,
                w.MobileNumber,
                w.AlternateMobileNumber,
                w.LaborCategoryId,
                (int)w.EmploymentType,
                w.ContractorId,
                w.JoiningDate,
                w.LeavingDate,
                w.Notes,
                w.IsActive,
                0
            ))
            .ToList();

        // 7. Worker Farm Assignments delta
        var assignmentsQuery = dbContext.WorkerFarmAssignments
            .AsNoTracking()
            .Where(a => a.OrganizationId == organizationId &&
                        (a.CreatedAt > sinceUtc || (a.UpdatedAt != null && a.UpdatedAt > sinceUtc)));
        if (farmId.HasValue)
        {
            assignmentsQuery = assignmentsQuery.Where(a => a.FarmId == farmId.Value);
        }
        var assignments = await assignmentsQuery
            .Select(a => new CachedWorkerFarmAssignmentDto(
                a.Id,
                a.OrganizationId,
                a.WorkerId,
                a.FarmId,
                a.AssignedFrom,
                a.AssignedTo,
                a.IsActive,
                a.Notes
            ))
            .ToListAsync(cancellationToken);

        // 8. Labor Categories delta
        var laborCategories = await dbContext.LaborCategories
            .AsNoTracking()
            .Where(lc => (lc.OrganizationId == null || lc.OrganizationId == organizationId) &&
                         (lc.CreatedAt > sinceUtc || (lc.UpdatedAt != null && lc.UpdatedAt > sinceUtc)))
            .Select(lc => new CachedLaborCategoryDto(
                lc.Id,
                lc.OrganizationId,
                lc.Name,
                lc.Description,
                lc.IsSystem,
                lc.IsActive
            ))
            .ToListAsync(cancellationToken);

        // 9. Contractors delta
        var contractors = await dbContext.Contractors
            .AsNoTracking()
            .Where(c => c.OrganizationId == organizationId &&
                        (c.CreatedAt > sinceUtc || (c.UpdatedAt != null && c.UpdatedAt > sinceUtc)))
            .Select(c => new CachedContractorDto(
                c.Id,
                c.OrganizationId,
                c.Name,
                c.ContactPerson,
                c.PhoneNumber,
                c.Email,
                c.Address,
                c.Notes,
                c.IsActive
            ))
            .ToListAsync(cancellationToken);

        // 10. Labor Wage Rates delta
        var rawWageRates = await dbContext.LaborWageRates
            .AsNoTracking()
            .Where(r => r.OrganizationId == organizationId &&
                        (r.CreatedAt > sinceUtc || (r.UpdatedAt != null && r.UpdatedAt > sinceUtc)))
            .Select(r => new
            {
                r.Id,
                r.OrganizationId,
                r.Gender,
                r.WageType,
                r.WageRate,
                r.CurrencyId,
                r.EffectiveFrom,
                r.EffectiveTo,
                r.Notes,
                r.IsActive
            })
            .ToListAsync(cancellationToken);

        var wageRates = rawWageRates
            .Select(r => new CachedLaborWageRateDto(
                r.Id,
                r.OrganizationId,
                (int)r.Gender,
                (int)r.WageType,
                r.WageRate,
                r.CurrencyId,
                r.EffectiveFrom,
                r.EffectiveTo,
                r.Notes,
                r.IsActive
            ))
            .ToList();

        // 11. Units delta
        var rawUnits = await dbContext.Units
            .AsNoTracking()
            .Where(u => (u.OrganizationId == null || u.OrganizationId == organizationId) &&
                        (u.CreatedAt > sinceUtc || (u.UpdatedAt != null && u.UpdatedAt > sinceUtc)))
            .Select(u => new
            {
                u.Id,
                u.OrganizationId,
                u.Code,
                u.Name,
                u.Symbol,
                u.UnitCategory,
                u.BaseUnitCode,
                u.ConversionFactor,
                u.IsSystem,
                u.IsActive,
                u.DisplayOrder
            })
            .ToListAsync(cancellationToken);

        var units = rawUnits
            .Select(u => new CachedUnitDto(
                u.Id,
                u.OrganizationId,
                u.Code,
                u.Name,
                u.Symbol,
                (int)u.UnitCategory,
                u.BaseUnitCode,
                u.ConversionFactor,
                u.IsSystem,
                u.IsActive,
                u.DisplayOrder
            ))
            .ToList();

        // 12. Storage Locations delta
        var storageLocationsQuery = dbContext.StorageLocations
            .AsNoTracking()
            .Where(sl => sl.OrganizationId == organizationId &&
                         (sl.CreatedAt > sinceUtc || (sl.UpdatedAt != null && sl.UpdatedAt > sinceUtc)));
        if (farmId.HasValue)
        {
            storageLocationsQuery = storageLocationsQuery.Where(sl => sl.FarmId == farmId.Value);
        }
        var storageLocations = await storageLocationsQuery
            .Select(sl => new CachedStorageLocationDto(
                sl.Id,
                sl.OrganizationId,
                sl.FarmId,
                sl.Name,
                sl.Description,
                sl.IsActive
            ))
            .ToListAsync(cancellationToken);

        // 13. Inventory Items delta
        var inventoryItems = await dbContext.InventoryItems
            .AsNoTracking()
            .Where(ii => ii.OrganizationId == organizationId &&
                         (ii.CreatedAt > sinceUtc || (ii.UpdatedAt != null && ii.UpdatedAt > sinceUtc)))
            .Select(ii => new CachedInventoryItemDto(
                ii.Id,
                ii.OrganizationId,
                ii.Name,
                ii.Sku,
                ii.Description,
                ii.CategoryId,
                ii.StockUnitId,
                ii.IsActive
            ))
            .ToListAsync(cancellationToken);

        // 14. Plant Protection Products delta
        var plantProtectionProducts = await dbContext.PlantProtectionProducts
            .AsNoTracking()
            .Where(ppp => ppp.OrganizationId == organizationId &&
                          (ppp.CreatedAt > sinceUtc || (ppp.UpdatedAt != null && ppp.UpdatedAt > sinceUtc)))
            .Select(ppp => new CachedPlantProtectionProductDto(
                ppp.Id,
                ppp.OrganizationId,
                ppp.InventoryItemId,
                ppp.ProductTypeId,
                ppp.ActiveIngredient,
                ppp.Manufacturer,
                ppp.Description,
                ppp.IsActive
            ))
            .ToListAsync(cancellationToken);

        // 15. Irrigation Methods delta
        var irrigationMethods = await dbContext.IrrigationMethods
            .AsNoTracking()
            .Where(im => (im.OrganizationId == null || im.OrganizationId == organizationId) &&
                         (im.CreatedAt > sinceUtc || (im.UpdatedAt != null && im.UpdatedAt > sinceUtc)))
            .Select(im => new CachedIrrigationMethodDto(
                im.Id,
                im.OrganizationId,
                im.Code,
                im.Name,
                im.Description,
                im.IsSystem,
                im.IsActive,
                im.DisplayOrder
            ))
            .ToListAsync(cancellationToken);

        return new MobileSyncPayloadResponse(
            syncedAtUtc,
            farmId,
            farms,
            farmAreas,
            plantations,
            cropCycles,
            cropCycleStages,
            workers,
            assignments,
            laborCategories,
            contractors,
            wageRates,
            units,
            storageLocations,
            inventoryItems,
            plantProtectionProducts,
            irrigationMethods
        );
    }
}
