using FarmManagement.Application.DTOs.Dashboard;
using FarmManagement.Application.Interfaces.Dashboard;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.Services;

public sealed class DashboardService(IDashboardStore store) : IDashboardService
{
    public async Task<DashboardSummaryResponse> GetSummaryAsync(
        DashboardActor actor,
        Guid? farmId,
        CancellationToken cancellationToken = default)
    {
        if (actor.OrganizationId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("An organization is required.");
        }

        var farms = await store.GetFarmsWithAreasAsync(actor.OrganizationId, farmId, cancellationToken);
        var totalAreasCount = await store.GetActiveFarmAreasCountAsync(actor.OrganizationId, farmId, cancellationToken);
        var plantations = await store.GetPlantationsAsync(actor.OrganizationId, farmId, cancellationToken);
        var cycles = await store.GetCyclesAsync(actor.OrganizationId, farmId, cancellationToken);
        var areaUnits = await store.GetAreaUnitsAsync(cancellationToken);

        var targetUnit = DetermineTargetUnit(farms, plantations, areaUnits, farmId);
        var targetSymbol = targetUnit?.Symbol ?? "ac";

        var activePlantations = plantations.Where(p => p.Status == PlantationStatus.Active).ToList();
        var plannedPlantations = plantations.Where(p => p.Status == PlantationStatus.Planned).ToList();

        var totalArea = Math.Round(
            farms.Where(f => f.TotalArea.HasValue)
                 .Sum(f => ConvertArea(f.TotalArea!.Value, f.AreaUnit, targetUnit)),
            2,
            MidpointRounding.AwayFromZero);

        var allocatedArea = Math.Round(
            activePlantations.Sum(p => ConvertArea(p.AllocatedArea, p.AreaUnit, targetUnit)),
            2,
            MidpointRounding.AwayFromZero);

        var availableArea = Math.Max(0m, Math.Round(totalArea - allocatedArea, 2, MidpointRounding.AwayFromZero));
        var utilizationPercentage = totalArea > 0
            ? Math.Round((allocatedArea / totalArea) * 100m, 1, MidpointRounding.AwayFromZero)
            : 0m;

        var activeCyclesCount = cycles.Count(c => c.Status == CropCycleStatus.Active);

        var kpi = new KpiSummaryDto(
            TotalFarms: farms.Count,
            TotalAreas: totalAreasCount,
            TotalArea: totalArea,
            AllocatedArea: allocatedArea,
            AvailableArea: availableArea,
            UtilizationPercentage: utilizationPercentage,
            AreaUnitSymbol: targetSymbol,
            ActivePlantationsCount: activePlantations.Count,
            PlannedPlantationsCount: plannedPlantations.Count,
            ActiveCyclesCount: activeCyclesCount
        );

        var cropAllocations = BuildCropAllocations(activePlantations, targetUnit, targetSymbol, allocatedArea);
        var activeCycleSummaries = BuildActiveCycles(cycles, targetUnit, targetSymbol);
        var farmUtilizations = BuildFarmUtilizations(farms, activePlantations, targetUnit, targetSymbol);
        var currentSeason = DetermineCurrentSeason(cycles);

        return new DashboardSummaryResponse(
            Kpi: kpi,
            CropAllocations: cropAllocations,
            ActiveCycles: activeCycleSummaries,
            FarmUtilizations: farmUtilizations,
            CurrentSeason: currentSeason
        );
    }

    private static IReadOnlyList<CropAllocationSummaryDto> BuildCropAllocations(
        List<CropPlantation> activePlantations,
        Unit? targetUnit,
        string targetSymbol,
        decimal totalAllocatedArea)
    {
        return activePlantations
            .GroupBy(p => p.CropId)
            .Select(cg =>
            {
                var first = cg.First();
                var cropName = first.Crop?.Name ?? "Unknown Crop";
                var cropCode = first.Crop?.Code ?? "";
                var cropArea = Math.Round(
                    cg.Sum(p => ConvertArea(p.AllocatedArea, p.AreaUnit, targetUnit)),
                    2,
                    MidpointRounding.AwayFromZero);

                var cropPct = totalAllocatedArea > 0
                    ? Math.Round((cropArea / totalAllocatedArea) * 100m, 1, MidpointRounding.AwayFromZero)
                    : 0m;

                var varieties = cg
                    .GroupBy(p => p.VarietyId)
                    .Select(vg =>
                    {
                        var varFirst = vg.First();
                        var varietyName = varFirst.Variety?.Name ?? "Standard / Unspecified";
                        var varArea = Math.Round(
                            vg.Sum(p => ConvertArea(p.AllocatedArea, p.AreaUnit, targetUnit)),
                            2,
                            MidpointRounding.AwayFromZero);
                        var varPct = cropArea > 0
                            ? Math.Round((varArea / cropArea) * 100m, 1, MidpointRounding.AwayFromZero)
                            : 0m;

                        return new VarietyAllocationSummaryDto(
                            VarietyId: vg.Key,
                            VarietyName: varietyName,
                            AllocatedArea: varArea,
                            AreaUnitSymbol: targetSymbol,
                            PercentageOfCrop: varPct
                        );
                    })
                    .OrderByDescending(v => v.AllocatedArea)
                    .ToList();

                return new CropAllocationSummaryDto(
                    CropId: cg.Key,
                    CropName: cropName,
                    CropCode: cropCode,
                    TotalAllocatedArea: cropArea,
                    AreaUnitSymbol: targetSymbol,
                    PercentageOfAllocated: cropPct,
                    Varieties: varieties
                );
            })
            .OrderByDescending(c => c.TotalAllocatedArea)
            .ToList();
    }

    private static IReadOnlyList<ActiveCycleSummaryDto> BuildActiveCycles(
        IReadOnlyList<CropCycle> cycles,
        Unit? targetUnit,
        string targetSymbol)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return cycles
            .Where(c => c.Status == CropCycleStatus.Active || c.Status == CropCycleStatus.Planned)
            .OrderBy(c => c.Status == CropCycleStatus.Active ? 0 : 1)
            .ThenByDescending(c => c.PlannedStartDate)
            .Take(8)
            .Select(c =>
            {
                var plantation = c.Plantation;
                var startDate = c.ActualStartDate ?? c.PlannedStartDate;
                var endDate = c.ActualEndDate ?? c.ExpectedEndDate;

                int? progressPct = null;
                if (endDate.HasValue && endDate.Value > startDate)
                {
                    var totalDays = endDate.Value.DayNumber - startDate.DayNumber;
                    var elapsedDays = today.DayNumber - startDate.DayNumber;
                    if (elapsedDays <= 0)
                    {
                        progressPct = 0;
                    }
                    else if (elapsedDays >= totalDays)
                    {
                        progressPct = 100;
                    }
                    else
                    {
                        progressPct = (int)Math.Round((decimal)elapsedDays / totalDays * 100m);
                    }
                }

                var plantationArea = plantation != null
                    ? Math.Round(ConvertArea(plantation.AllocatedArea, plantation.AreaUnit, targetUnit), 2, MidpointRounding.AwayFromZero)
                    : 0m;

                return new ActiveCycleSummaryDto(
                    CycleId: c.Id,
                    CycleCode: c.CycleCode,
                    CycleName: c.CycleName,
                    SeasonYear: c.SeasonYear,
                    SeasonName: c.SeasonName,
                    PlantationId: c.PlantationId,
                    PlantationName: plantation?.PlantationName ?? "N/A",
                    FarmName: plantation?.Farm?.Name ?? "N/A",
                    FarmAreaName: plantation?.FarmArea?.Name ?? "N/A",
                    CropName: plantation?.Crop?.Name ?? "Unknown Crop",
                    VarietyName: plantation?.Variety?.Name,
                    AllocatedArea: plantationArea,
                    AreaUnitSymbol: targetSymbol,
                    StartDate: startDate,
                    ExpectedEndDate: c.ExpectedEndDate,
                    ProgressPercentage: progressPct,
                    Status: c.Status.ToString().ToUpperInvariant()
                );
            })
            .ToList();
    }

    private static IReadOnlyList<FarmUtilizationSummaryDto> BuildFarmUtilizations(
        IReadOnlyList<Farm> farms,
        List<CropPlantation> activePlantations,
        Unit? targetUnit,
        string targetSymbol)
    {
        return farms
            .Select(f =>
            {
                var farmTotal = f.TotalArea.HasValue
                    ? Math.Round(ConvertArea(f.TotalArea.Value, f.AreaUnit, targetUnit), 2, MidpointRounding.AwayFromZero)
                    : 0m;

                var farmPlantations = activePlantations.Where(p => p.FarmId == f.Id).ToList();
                var farmAllocated = Math.Round(
                    farmPlantations.Sum(p => ConvertArea(p.AllocatedArea, p.AreaUnit, targetUnit)),
                    2,
                    MidpointRounding.AwayFromZero);

                var farmAvailable = Math.Max(0m, Math.Round(farmTotal - farmAllocated, 2, MidpointRounding.AwayFromZero));
                var farmUtilizationPct = farmTotal > 0
                    ? Math.Round((farmAllocated / farmTotal) * 100m, 1, MidpointRounding.AwayFromZero)
                    : 0m;

                return new FarmUtilizationSummaryDto(
                    FarmId: f.Id,
                    FarmCode: f.Code,
                    FarmName: f.Name,
                    TotalArea: farmTotal,
                    AllocatedArea: farmAllocated,
                    AvailableArea: farmAvailable,
                    UtilizationPercentage: farmUtilizationPct,
                    AreaUnitSymbol: targetSymbol,
                    ActivePlantationsCount: farmPlantations.Count
                );
            })
            .OrderByDescending(f => f.TotalArea)
            .ToList();
    }

    private static string? DetermineCurrentSeason(IReadOnlyList<CropCycle> cycles)
    {
        var activeWithSeason = cycles
            .Where(c => c.Status == CropCycleStatus.Active && !string.IsNullOrWhiteSpace(c.SeasonName))
            .Select(c => c.SeasonName!.Trim())
            .FirstOrDefault();

        if (activeWithSeason is not null)
        {
            return activeWithSeason;
        }

        var anyWithSeason = cycles
            .Where(c => !string.IsNullOrWhiteSpace(c.SeasonName))
            .Select(c => c.SeasonName!.Trim())
            .FirstOrDefault();

        if (anyWithSeason is not null)
        {
            return anyWithSeason;
        }

        if (cycles.Any())
        {
            return $"Season {cycles.Max(c => c.SeasonYear)}";
        }

        return $"Season {DateTime.UtcNow.Year}";
    }

    private static Unit? DetermineTargetUnit(
        IReadOnlyList<Farm> farms,
        IReadOnlyList<CropPlantation> plantations,
        IReadOnlyList<Unit> areaUnits,
        Guid? farmId)
    {
        if (farmId.HasValue)
        {
            var selectedFarm = farms.FirstOrDefault(f => f.Id == farmId.Value);
            if (selectedFarm?.AreaUnit != null)
            {
                return selectedFarm.AreaUnit;
            }
        }

        var mostFrequentFarmUnit = farms
            .Where(f => f.AreaUnit != null)
            .GroupBy(f => f.AreaUnit!)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault();

        if (mostFrequentFarmUnit != null)
        {
            return mostFrequentFarmUnit;
        }

        var mostFrequentPlantationUnit = plantations
            .Where(p => p.AreaUnit != null)
            .GroupBy(p => p.AreaUnit!)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault();

        if (mostFrequentPlantationUnit != null)
        {
            return mostFrequentPlantationUnit;
        }

        return areaUnits.FirstOrDefault(u => u.Code == "ACRE")
            ?? areaUnits.FirstOrDefault();
    }

    private static decimal ConvertArea(decimal amount, Unit? sourceUnit, Unit? targetUnit)
    {
        if (sourceUnit is null || targetUnit is null || sourceUnit.Id == targetUnit.Id)
        {
            return amount;
        }

        if (sourceUnit.ConversionFactor is > 0 && targetUnit.ConversionFactor is > 0)
        {
            var baseSqMeters = amount * sourceUnit.ConversionFactor.Value;
            return baseSqMeters / targetUnit.ConversionFactor.Value;
        }

        return amount;
    }
}
