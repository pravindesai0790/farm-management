namespace FarmManagement.Application.DTOs.Dashboard;

public sealed record DashboardActor(Guid UserId, Guid OrganizationId);

public sealed record DashboardSummaryResponse(
    KpiSummaryDto Kpi,
    IReadOnlyList<CropAllocationSummaryDto> CropAllocations,
    IReadOnlyList<ActiveCycleSummaryDto> ActiveCycles,
    IReadOnlyList<FarmUtilizationSummaryDto> FarmUtilizations,
    string? CurrentSeason
);

public sealed record KpiSummaryDto(
    int TotalFarms,
    int TotalAreas,
    decimal TotalArea,
    decimal AllocatedArea,
    decimal AvailableArea,
    decimal UtilizationPercentage,
    string AreaUnitSymbol,
    int ActivePlantationsCount,
    int PlannedPlantationsCount,
    int ActiveCyclesCount
);

public sealed record CropAllocationSummaryDto(
    Guid CropId,
    string CropName,
    string CropCode,
    decimal TotalAllocatedArea,
    string AreaUnitSymbol,
    decimal PercentageOfAllocated,
    IReadOnlyList<VarietyAllocationSummaryDto> Varieties
);

public sealed record VarietyAllocationSummaryDto(
    Guid? VarietyId,
    string VarietyName,
    decimal AllocatedArea,
    string AreaUnitSymbol,
    decimal PercentageOfCrop
);

public sealed record ActiveCycleSummaryDto(
    Guid CycleId,
    string CycleCode,
    string CycleName,
    int SeasonYear,
    string? SeasonName,
    Guid PlantationId,
    string PlantationName,
    string FarmName,
    string FarmAreaName,
    string CropName,
    string? VarietyName,
    decimal AllocatedArea,
    string AreaUnitSymbol,
    DateOnly StartDate,
    DateOnly? ExpectedEndDate,
    int? ProgressPercentage,
    string Status
);

public sealed record FarmUtilizationSummaryDto(
    Guid FarmId,
    string FarmCode,
    string FarmName,
    decimal TotalArea,
    decimal AllocatedArea,
    decimal AvailableArea,
    decimal UtilizationPercentage,
    string AreaUnitSymbol,
    int ActivePlantationsCount
);
