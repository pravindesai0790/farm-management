using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.LaborActivities;
using FarmManagement.Application.Interfaces.LaborActivities;
using FarmManagement.Application.Services;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Services;

public sealed class LaborActivityServiceTests
{
    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    private LaborActivityActor CreateActor() => new(_userId, _organizationId);

    [Fact]
    public async Task CreateAsync_WithMinimalValidFields_Succeeds()
    {
        var store = new FakeLaborActivityStore();
        var farm = CreateFarm(store, _organizationId, "Green Valley Farm");
        var activityType = CreateLaborActivityType(store, _organizationId, "Weeding");

        var service = new LaborActivityService(store);
        var date = DateOnly.FromDateTime(DateTime.UtcNow);

        var request = new CreateLaborActivityRequest(
            ActivityDate: date,
            FarmId: farm.Id,
            FarmAreaId: null,
            PlantationId: null,
            CropCycleId: null,
            CropCycleStageId: null,
            LaborActivityTypeId: activityType.Id,
            Description: "General farm weeding",
            Status: "COMPLETED");

        var response = await service.CreateAsync(CreateActor(), request, "127.0.0.1");

        Assert.NotNull(response);
        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal(date, response.ActivityDate);
        Assert.Equal(farm.Id, response.Farm.Id);
        Assert.Equal("Green Valley Farm", response.Farm.Name);
        Assert.Null(response.FarmArea);
        Assert.Null(response.Plantation);
        Assert.Null(response.CropCycle);
        Assert.Null(response.CropCycleStage);
        Assert.Equal(activityType.Id, response.ActivityType.Id);
        Assert.Equal("Weeding", response.ActivityType.Name);
        Assert.Equal("COMPLETED", response.Status);
        Assert.Equal("General farm weeding", response.Description);

        Assert.Single(store.AuditLogs);
        Assert.Equal("LaborActivity.Created", store.AuditLogs[0].Action);
    }

    [Fact]
    public async Task CreateAsync_WithFullHierarchy_ExposesStageReferenceResponse()
    {
        var store = new FakeLaborActivityStore();
        var farm = CreateFarm(store, _organizationId, "North Field");
        var area = CreateFarmArea(store, _organizationId, farm.Id, "Block A");
        var plantation = CreatePlantation(store, _organizationId, farm.Id, area.Id, "Vineyard 1");
        var cycle = CreateCropCycle(store, _organizationId, plantation.Id, "2026 Cycle");
        var stage = CreateCropCycleStage(store, cycle.Id, "Pruning", sequenceNumber: 2);
        var activityType = CreateLaborActivityType(store, _organizationId, "Pruning");

        var service = new LaborActivityService(store);
        var date = DateOnly.FromDateTime(DateTime.UtcNow);

        var request = new CreateLaborActivityRequest(
            ActivityDate: date,
            FarmId: farm.Id,
            FarmAreaId: area.Id,
            PlantationId: plantation.Id,
            CropCycleId: cycle.Id,
            CropCycleStageId: stage.Id,
            LaborActivityTypeId: activityType.Id,
            Description: "Winter pruning stage 2",
            Status: "COMPLETED");

        var response = await service.CreateAsync(CreateActor(), request, "127.0.0.1");

        Assert.NotNull(response);
        Assert.Equal(area.Id, response.FarmArea?.Id);
        Assert.Equal(plantation.Id, response.Plantation?.Id);
        Assert.Equal(cycle.Id, response.CropCycle?.Id);
        Assert.NotNull(response.CropCycleStage);
        Assert.Equal(stage.Id, response.CropCycleStage.Id);
        Assert.Equal("Pruning", response.CropCycleStage.Name);
        Assert.Equal(2, response.CropCycleStage.SequenceNumber);
    }

    [Fact]
    public async Task CreateAsync_InfersFarmAreaFromPlantation_WhenFarmAreaOmitted()
    {
        var store = new FakeLaborActivityStore();
        var farm = CreateFarm(store, _organizationId, "Main Farm");
        var area = CreateFarmArea(store, _organizationId, farm.Id, "East Parcel");
        var plantation = CreatePlantation(store, _organizationId, farm.Id, area.Id, "Wheat Plot");
        var activityType = CreateLaborActivityType(store, _organizationId, "Fertilization");

        var service = new LaborActivityService(store);
        var date = DateOnly.FromDateTime(DateTime.UtcNow);

        var request = new CreateLaborActivityRequest(
            ActivityDate: date,
            FarmId: farm.Id,
            FarmAreaId: null,
            PlantationId: plantation.Id,
            CropCycleId: null,
            CropCycleStageId: null,
            LaborActivityTypeId: activityType.Id,
            Description: null,
            Status: "COMPLETED");

        var response = await service.CreateAsync(CreateActor(), request, "127.0.0.1");

        Assert.NotNull(response.FarmArea);
        Assert.Equal(area.Id, response.FarmArea.Id);
        Assert.Equal("East Parcel", response.FarmArea.Name);
    }

    [Fact]
    public async Task CreateAsync_RejectsStageWithoutCycle()
    {
        var store = new FakeLaborActivityStore();
        var farm = CreateFarm(store, _organizationId, "South Farm");
        var activityType = CreateLaborActivityType(store, _organizationId, "Harvesting");
        var stageId = Guid.NewGuid();

        var service = new LaborActivityService(store);
        var request = new CreateLaborActivityRequest(
            ActivityDate: DateOnly.FromDateTime(DateTime.UtcNow),
            FarmId: farm.Id,
            FarmAreaId: null,
            PlantationId: null,
            CropCycleId: null,
            CropCycleStageId: stageId,
            LaborActivityTypeId: activityType.Id,
            Description: null);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(CreateActor(), request, "127.0.0.1"));

        Assert.True(ex.Errors.ContainsKey("cropCycleStageId"));
    }

    [Fact]
    public async Task CreateAsync_RejectsStageBelongingToDifferentCycle()
    {
        var store = new FakeLaborActivityStore();
        var farm = CreateFarm(store, _organizationId, "West Farm");
        var area = CreateFarmArea(store, _organizationId, farm.Id, "Zone B");
        var plantation = CreatePlantation(store, _organizationId, farm.Id, area.Id, "Plum Orchard");
        var cycle1 = CreateCropCycle(store, _organizationId, plantation.Id, "Cycle 1");
        var cycle2 = CreateCropCycle(store, _organizationId, plantation.Id, "Cycle 2");
        var stageOfCycle2 = CreateCropCycleStage(store, cycle2.Id, "Flowering", sequenceNumber: 1);
        var activityType = CreateLaborActivityType(store, _organizationId, "Inspection");

        var service = new LaborActivityService(store);
        var request = new CreateLaborActivityRequest(
            ActivityDate: DateOnly.FromDateTime(DateTime.UtcNow),
            FarmId: farm.Id,
            FarmAreaId: area.Id,
            PlantationId: plantation.Id,
            CropCycleId: cycle1.Id, // Specifies Cycle 1
            CropCycleStageId: stageOfCycle2.Id, // But passes Stage belonging to Cycle 2
            LaborActivityTypeId: activityType.Id,
            Description: null);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(CreateActor(), request, "127.0.0.1"));

        Assert.True(ex.Errors.ContainsKey("cropCycleStageId"));
    }

    [Fact]
    public async Task CreateAsync_RejectsStageFromOtherOrganization()
    {
        var store = new FakeLaborActivityStore();
        var otherOrgId = Guid.NewGuid();
        var farm = CreateFarm(store, _organizationId, "Farm A");
        var plantation = CreatePlantation(store, _organizationId, farm.Id, null, "Plantation A");
        var cycle = CreateCropCycle(store, _organizationId, plantation.Id, "Cycle A");

        var otherCycle = CreateCropCycle(store, otherOrgId, Guid.NewGuid(), "Other Cycle");
        var foreignStage = CreateCropCycleStage(store, otherCycle.Id, "Foreign Stage", sequenceNumber: 1);
        var activityType = CreateLaborActivityType(store, _organizationId, "Scouting");

        var service = new LaborActivityService(store);
        var request = new CreateLaborActivityRequest(
            ActivityDate: DateOnly.FromDateTime(DateTime.UtcNow),
            FarmId: farm.Id,
            FarmAreaId: null,
            PlantationId: plantation.Id,
            CropCycleId: cycle.Id,
            CropCycleStageId: foreignStage.Id,
            LaborActivityTypeId: activityType.Id,
            Description: null);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            service.CreateAsync(CreateActor(), request, "127.0.0.1"));
    }

    [Fact]
    public async Task CancelAsync_SetsCancelledStatusAndWritesAudit()
    {
        var store = new FakeLaborActivityStore();
        var farm = CreateFarm(store, _organizationId, "Farm B");
        var activityType = CreateLaborActivityType(store, _organizationId, "Tilling");
        var service = new LaborActivityService(store);

        var created = await service.CreateAsync(CreateActor(), new CreateLaborActivityRequest(
            DateOnly.FromDateTime(DateTime.UtcNow),
            farm.Id,
            null, null, null, null,
            activityType.Id,
            "To be cancelled"), "127.0.0.1");

        await service.CancelAsync(CreateActor(), created.Id, new CancelLaborActivityRequest("Weather event"), "127.0.0.1");

        var fetched = await service.GetAsync(CreateActor(), created.Id);
        Assert.Equal("CANCELLED", fetched.Status);
        Assert.Equal("Weather event", fetched.CancellationReason);
    }

    [Fact]
    public async Task ListAsync_FiltersByCropCycleStageId()
    {
        var store = new FakeLaborActivityStore();
        var farm = CreateFarm(store, _organizationId, "Farm C");
        var plantation = CreatePlantation(store, _organizationId, farm.Id, null, "Field C");
        var cycle = CreateCropCycle(store, _organizationId, plantation.Id, "Cycle C");
        var stage1 = CreateCropCycleStage(store, cycle.Id, "Stage 1", 1);
        var stage2 = CreateCropCycleStage(store, cycle.Id, "Stage 2", 2);
        var activityType = CreateLaborActivityType(store, _organizationId, "General");

        var service = new LaborActivityService(store);
        var date = DateOnly.FromDateTime(DateTime.UtcNow);

        await service.CreateAsync(CreateActor(), new CreateLaborActivityRequest(date, farm.Id, null, plantation.Id, cycle.Id, stage1.Id, activityType.Id, "Stage 1 Activity"), "127.0.0.1");
        await service.CreateAsync(CreateActor(), new CreateLaborActivityRequest(date, farm.Id, null, plantation.Id, cycle.Id, stage2.Id, activityType.Id, "Stage 2 Activity"), "127.0.0.1");

        var paged1 = await service.ListAsync(CreateActor(), 1, 20, farm.Id, null, plantation.Id, cycle.Id, stage1.Id, activityType.Id, null, null, null);
        Assert.Single(paged1.Items);
        Assert.NotNull(paged1.Items[0].CropCycleStage);
        Assert.Equal(stage1.Id, paged1.Items[0].CropCycleStage!.Id);

        var paged2 = await service.ListAsync(CreateActor(), 1, 20, farm.Id, null, plantation.Id, cycle.Id, stage2.Id, activityType.Id, null, null, null);
        Assert.Single(paged2.Items);
        Assert.NotNull(paged2.Items[0].CropCycleStage);
        Assert.Equal(stage2.Id, paged2.Items[0].CropCycleStage!.Id);
    }

    private static Farm CreateFarm(FakeLaborActivityStore store, Guid orgId, string name)
    {
        var farm = new Farm(orgId, name, Guid.NewGuid(), Guid.NewGuid(), 100m, Guid.NewGuid());
        store.Farms[farm.Id] = farm;
        return farm;
    }

    private static FarmArea CreateFarmArea(FakeLaborActivityStore store, Guid orgId, Guid farmId, string name)
    {
        var area = new FarmArea(orgId, farmId, null, name, 50m, Guid.NewGuid(), Guid.NewGuid());
        store.FarmAreas[area.Id] = area;
        return area;
    }

    private static CropPlantation CreatePlantation(FakeLaborActivityStore store, Guid orgId, Guid farmId, Guid? areaId, string name)
    {
        var effectiveAreaId = areaId ?? Guid.NewGuid();
        var plantation = new CropPlantation(orgId, farmId, effectiveAreaId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), name, 10m, Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), null, Guid.NewGuid());
        store.Plantations[plantation.Id] = plantation;
        return plantation;
    }

    private static CropCycle CreateCropCycle(FakeLaborActivityStore store, Guid orgId, Guid plantationId, string name)
    {
        var cycle = new CropCycle(orgId, plantationId, name, 2026, "Spring", DateOnly.FromDateTime(DateTime.UtcNow), null, Guid.NewGuid());
        store.CropCycles[cycle.Id] = cycle;
        return cycle;
    }

    private static CropCycleStage CreateCropCycleStage(FakeLaborActivityStore store, Guid cycleId, string name, int sequenceNumber)
    {
        var stage = new CropCycleStage(cycleId, Guid.NewGuid(), name, sequenceNumber, 15, DateOnly.FromDateTime(DateTime.UtcNow), null, Guid.NewGuid());
        store.CropCycleStages[stage.Id] = stage;
        return stage;
    }

    private static LaborActivityType CreateLaborActivityType(FakeLaborActivityStore store, Guid orgId, string name)
    {
        var type = new LaborActivityType(orgId, name.ToUpperInvariant(), name, isSystem: false, createdBy: Guid.NewGuid());
        store.ActivityTypes[type.Id] = type;
        return type;
    }
}

public sealed class FakeLaborActivityStore : ILaborActivityStore
{
    public Dictionary<Guid, LaborActivity> Activities { get; } = new();
    public Dictionary<Guid, Farm> Farms { get; } = new();
    public Dictionary<Guid, FarmArea> FarmAreas { get; } = new();
    public Dictionary<Guid, CropPlantation> Plantations { get; } = new();
    public Dictionary<Guid, CropCycle> CropCycles { get; } = new();
    public Dictionary<Guid, CropCycleStage> CropCycleStages { get; } = new();
    public Dictionary<Guid, LaborActivityType> ActivityTypes { get; } = new();
    public List<AuditLog> AuditLogs { get; } = new();

    public Task<(IReadOnlyList<LaborActivity> Items, int TotalCount)> ListPagedAsync(
        Guid organizationId,
        Guid? farmId,
        Guid? farmAreaId,
        Guid? plantationId,
        Guid? cropCycleId,
        Guid? cropCycleStageId,
        Guid? activityTypeId,
        DateOnly? fromDate,
        DateOnly? toDate,
        LaborActivityStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = Activities.Values.Where(a => a.OrganizationId == organizationId);
        if (farmId.HasValue) query = query.Where(a => a.FarmId == farmId.Value);
        if (farmAreaId.HasValue) query = query.Where(a => a.FarmAreaId == farmAreaId.Value);
        if (plantationId.HasValue) query = query.Where(a => a.PlantationId == plantationId.Value);
        if (cropCycleId.HasValue) query = query.Where(a => a.CropCycleId == cropCycleId.Value);
        if (cropCycleStageId.HasValue) query = query.Where(a => a.CropCycleStageId == cropCycleStageId.Value);
        if (activityTypeId.HasValue) query = query.Where(a => a.LaborActivityTypeId == activityTypeId.Value);
        if (fromDate.HasValue) query = query.Where(a => a.ActivityDate >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(a => a.ActivityDate <= toDate.Value);
        if (status.HasValue) query = query.Where(a => a.Status == status.Value);

        var list = query.ToList();
        var paged = list.Skip(skip).Take(take).ToList();
        return Task.FromResult<(IReadOnlyList<LaborActivity> Items, int TotalCount)>((paged, list.Count));
    }

    public Task<LaborActivity?> FindAsync(Guid activityId, Guid organizationId, CancellationToken cancellationToken = default)
    {
        Activities.TryGetValue(activityId, out var item);
        return Task.FromResult(item?.OrganizationId == organizationId ? Hydrate(item) : null);
    }

    public Task<LaborActivity?> LockAsync(Guid activityId, Guid organizationId, CancellationToken cancellationToken = default) =>
        FindAsync(activityId, organizationId, cancellationToken);

    public Task<Farm?> FindFarmAsync(Guid farmId, Guid organizationId, CancellationToken cancellationToken = default)
    {
        Farms.TryGetValue(farmId, out var farm);
        return Task.FromResult(farm?.OrganizationId == organizationId ? farm : null);
    }

    public Task<FarmArea?> FindFarmAreaAsync(Guid farmAreaId, Guid organizationId, CancellationToken cancellationToken = default)
    {
        FarmAreas.TryGetValue(farmAreaId, out var area);
        return Task.FromResult(area?.OrganizationId == organizationId ? area : null);
    }

    public Task<CropPlantation?> FindPlantationAsync(Guid plantationId, Guid organizationId, CancellationToken cancellationToken = default)
    {
        Plantations.TryGetValue(plantationId, out var p);
        return Task.FromResult(p?.OrganizationId == organizationId ? p : null);
    }

    public Task<CropCycle?> FindCropCycleAsync(Guid cropCycleId, Guid organizationId, CancellationToken cancellationToken = default)
    {
        CropCycles.TryGetValue(cropCycleId, out var c);
        return Task.FromResult(c?.OrganizationId == organizationId ? c : null);
    }

    public Task<CropCycleStage?> FindCropCycleStageAsync(Guid stageId, Guid organizationId, CancellationToken cancellationToken = default)
    {
        if (!CropCycleStages.TryGetValue(stageId, out var stage)) return Task.FromResult<CropCycleStage?>(null);
        if (!CropCycles.TryGetValue(stage.CropCycleId, out var cycle)) return Task.FromResult<CropCycleStage?>(null);
        return Task.FromResult(cycle.OrganizationId == organizationId ? stage : null);
    }

    public Task<LaborActivityType?> FindLaborActivityTypeAsync(Guid activityTypeId, Guid organizationId, CancellationToken cancellationToken = default)
    {
        ActivityTypes.TryGetValue(activityTypeId, out var type);
        return Task.FromResult(type);
    }

    public Task<IReadOnlyList<LaborActivityType>> ListLaborActivityTypesAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<LaborActivityType>>(ActivityTypes.Values.ToList());

    public Task<string?> GetCancellationReasonAsync(Guid activityId, Guid organizationId, CancellationToken cancellationToken = default)
    {
        var audit = AuditLogs.FirstOrDefault(a => a.EntityId == activityId && a.Action == "LaborActivity.Cancelled");
        if (audit?.Details is null) return Task.FromResult<string?>(null);
        if (audit.Details.RootElement.TryGetProperty("CancellationReason", out var prop))
        {
            return Task.FromResult(prop.GetString());
        }
        return Task.FromResult<string?>(null);
    }

    public void Add(LaborActivity activity) => Activities[activity.Id] = activity;

    public void AddAuditLog(AuditLog auditLog) => AuditLogs.Add(auditLog);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) =>
        operation(cancellationToken);

    private LaborActivity Hydrate(LaborActivity activity)
    {
        // Populate navigation properties for ToResponse testing
        if (Farms.TryGetValue(activity.FarmId, out var farm))
        {
            typeof(LaborActivity).GetProperty(nameof(LaborActivity.Farm))?.SetValue(activity, farm);
        }
        if (activity.FarmAreaId.HasValue && FarmAreas.TryGetValue(activity.FarmAreaId.Value, out var area))
        {
            typeof(LaborActivity).GetProperty(nameof(LaborActivity.FarmArea))?.SetValue(activity, area);
        }
        if (activity.PlantationId.HasValue && Plantations.TryGetValue(activity.PlantationId.Value, out var plantation))
        {
            typeof(LaborActivity).GetProperty(nameof(LaborActivity.Plantation))?.SetValue(activity, plantation);
        }
        if (activity.CropCycleId.HasValue && CropCycles.TryGetValue(activity.CropCycleId.Value, out var cycle))
        {
            typeof(LaborActivity).GetProperty(nameof(LaborActivity.CropCycle))?.SetValue(activity, cycle);
        }
        if (activity.CropCycleStageId.HasValue && CropCycleStages.TryGetValue(activity.CropCycleStageId.Value, out var stage))
        {
            typeof(LaborActivity).GetProperty(nameof(LaborActivity.CropCycleStage))?.SetValue(activity, stage);
        }
        if (ActivityTypes.TryGetValue(activity.LaborActivityTypeId, out var type))
        {
            typeof(LaborActivity).GetProperty(nameof(LaborActivity.LaborActivityType))?.SetValue(activity, type);
        }
        return activity;
    }
}
