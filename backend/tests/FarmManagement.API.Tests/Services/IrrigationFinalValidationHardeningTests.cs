using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.Irrigation;
using FarmManagement.Application.Interfaces.Irrigation;
using FarmManagement.Application.Services;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Services;

public class IrrigationFinalValidationHardeningTests
{
    private readonly Guid _orgA = Guid.NewGuid();
    private readonly Guid _orgB = Guid.NewGuid();
    private readonly Guid _userA = Guid.NewGuid();
    private readonly Guid _userB = Guid.NewGuid();

    private readonly HardeningFakeStore _store = new();
    private readonly IrrigationService _sut;

    public IrrigationFinalValidationHardeningTests()
    {
        _sut = new IrrigationService(_store);
    }

    private IrrigationActor CreateActorA() => new(_userA, _orgA);
    private IrrigationActor CreateActorB() => new(_userB, _orgB);

    #region Fixture Helpers

    private Farm CreateFarm(Guid orgId, string name = "Test Farm")
    {
        var farm = new Farm(orgId, name, Guid.NewGuid(), Guid.NewGuid(), 100m, Guid.NewGuid());
        _store.Farms.Add(farm);
        return farm;
    }

    private FarmArea CreateFarmArea(Guid orgId, Guid farmId, string name = "Test Area")
    {
        var area = new FarmArea(orgId, farmId, null, name, 25m, Guid.NewGuid(), _userA);
        _store.FarmAreas.Add(area);
        return area;
    }

    private CropPlantation CreatePlantation(Guid orgId, Guid farmId, Guid farmAreaId, string name = "Block 1")
    {
        var plantation = new CropPlantation(
            orgId,
            farmId,
            farmAreaId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            name,
            10m,
            Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.UtcNow),
            null,
            _userA);
        _store.Plantations.Add(plantation);
        return plantation;
    }

    private CropCycle CreateCropCycle(Guid orgId, Guid plantationId, string name = "Cycle 2026")
    {
        var cycle = new CropCycle(
            orgId,
            plantationId,
            name,
            2026,
            "Spring",
            DateOnly.FromDateTime(DateTime.UtcNow),
            null,
            _userA);
        _store.CropCycles.Add(cycle);
        return cycle;
    }

    private CropCycleStage CreateCropCycleStage(Guid cropCycleId, string name = "Stage 1")
    {
        var stage = new CropCycleStage(
            cropCycleId,
            Guid.NewGuid(),
            name,
            1,
            14,
            DateOnly.FromDateTime(DateTime.UtcNow),
            null,
            _userA);
        _store.CropCycleStages.Add(stage);
        return stage;
    }

    private IrrigationMethod CreateMethod(Guid? orgId, string code, string name, bool isSystem = false)
    {
        var method = new IrrigationMethod(orgId, code, name, isSystem: isSystem);
        _store.Methods.Add(method);
        return method;
    }

    private Unit CreateUnit(Guid? orgId, string code, UnitCategory category, bool isSystem = true)
    {
        var unit = new Unit(orgId, code, code, code.ToLower(), category, isSystem: isSystem);
        _store.Units.Add(unit);
        return unit;
    }

    #endregion

    #region 1 & 2: Multi-Tenant Isolation

    [Fact]
    public async Task MultiTenant_CannotAccessOrModifyIrrigationAcrossTenants()
    {
        var farmA = CreateFarm(_orgA);
        var areaA = CreateFarmArea(_orgA, farmA.Id);

        var actorA = CreateActorA();
        var actorB = CreateActorB();

        var created = await _sut.CreateDraftAsync(actorA, new CreateIrrigationDraftRequest(farmA.Id, areaA.Id), null);

        // Org B cannot read Org A's irrigation
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => _sut.GetAsync(actorB, created.Id));

        // Org B cannot schedule Org A's irrigation
        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _sut.ScheduleAsync(actorB, created.Id, new ScheduleIrrigationRequest(DateTimeOffset.UtcNow), null));

        // Org B cannot cancel Org A's irrigation
        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _sut.CancelAsync(actorB, created.Id, new CancelIrrigationRequest("Cross tenant cancel"), null));

        // List query for Org B returns 0 records
        var listB = await _sut.ListAsync(actorB, new IrrigationListQuery());
        Assert.Empty(listB.Items);
    }

    [Fact]
    public async Task MultiTenant_CannotReferenceOtherTenantFarmOrArea()
    {
        var farmB = CreateFarm(_orgB);
        var areaB = CreateFarmArea(_orgB, farmB.Id);

        var actorA = CreateActorA();

        // Org A tries to use Org B's Farm -> Throws ResourceNotFoundException (tenant-isolated lookup)
        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _sut.CreateDraftAsync(actorA, new CreateIrrigationDraftRequest(farmB.Id, areaB.Id), null));
    }

    #endregion

    #region 3 & 4: Cascading Hierarchy Validation

    [Fact]
    public async Task CascadingHierarchy_AreaMismatchWithFarm_ThrowsValidationException()
    {
        var farm1 = CreateFarm(_orgA, "Farm 1");
        var farm2 = CreateFarm(_orgA, "Farm 2");
        var areaOfFarm2 = CreateFarmArea(_orgA, farm2.Id);

        var actor = CreateActorA();

        // Area belongs to farm2, but request specifies farm1
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CreateDraftAsync(actor, new CreateIrrigationDraftRequest(farm1.Id, areaOfFarm2.Id), null));

        Assert.Contains("farmAreaId", ex.Errors.Keys);
    }

    [Fact]
    public async Task CascadingHierarchy_PlantationMismatchWithArea_ThrowsValidationException()
    {
        var farm = CreateFarm(_orgA);
        var area1 = CreateFarmArea(_orgA, farm.Id, "Area 1");
        var area2 = CreateFarmArea(_orgA, farm.Id, "Area 2");
        var plantationInArea2 = CreatePlantation(_orgA, farm.Id, area2.Id);

        var actor = CreateActorA();

        // Plantation belongs to area2, but request specifies area1
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CreateDraftAsync(actor, new CreateIrrigationDraftRequest(farm.Id, area1.Id, PlantationId: plantationInArea2.Id), null));

        Assert.Contains("plantationId", ex.Errors.Keys);
    }

    [Fact]
    public async Task CascadingHierarchy_CycleRequiresPlantation_ThrowsValidationException()
    {
        var farm = CreateFarm(_orgA);
        var area = CreateFarmArea(_orgA, farm.Id);
        var plantation = CreatePlantation(_orgA, farm.Id, area.Id);
        var cycle = CreateCropCycle(_orgA, plantation.Id);

        var actor = CreateActorA();

        // CropCycle specified without PlantationId
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CreateDraftAsync(actor, new CreateIrrigationDraftRequest(farm.Id, area.Id, PlantationId: null, CropCycleId: cycle.Id), null));

        Assert.Contains("cropCycleId", ex.Errors.Keys);
    }

    [Fact]
    public async Task CascadingHierarchy_StageRequiresCycleAndMembership_ThrowsValidationException()
    {
        var farm = CreateFarm(_orgA);
        var area = CreateFarmArea(_orgA, farm.Id);
        var plantation = CreatePlantation(_orgA, farm.Id, area.Id);
        var cycle1 = CreateCropCycle(_orgA, plantation.Id, "Cycle 1");
        var cycle2 = CreateCropCycle(_orgA, plantation.Id, "Cycle 2");
        var stageOfCycle2 = CreateCropCycleStage(cycle2.Id, "Stage Cycle 2");

        var actor = CreateActorA();

        // Stage specified without CropCycleId
        var ex1 = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CreateDraftAsync(actor, new CreateIrrigationDraftRequest(
                farm.Id,
                area.Id,
                PlantationId: plantation.Id,
                CropCycleId: null,
                CropCycleStageId: stageOfCycle2.Id), null));
        Assert.Contains("cropCycleStageId", ex1.Errors.Keys);

        // Stage does not belong to specified cycle1
        var ex2 = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CreateDraftAsync(actor, new CreateIrrigationDraftRequest(
                farm.Id,
                area.Id,
                PlantationId: plantation.Id,
                CropCycleId: cycle1.Id,
                CropCycleStageId: stageOfCycle2.Id), null));
        Assert.Contains("cropCycleStageId", ex2.Errors.Keys);
    }

    #endregion

    #region Master Data & Unit Category Validation

    [Fact]
    public async Task MasterData_NonVolumeUnit_ThrowsValidationException()
    {
        var farm = CreateFarm(_orgA);
        var area = CreateFarmArea(_orgA, farm.Id);
        var massUnit = CreateUnit(null, "HA", UnitCategory.Area);

        var actor = CreateActorA();

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CreateDraftAsync(actor, new CreateIrrigationDraftRequest(
                farm.Id,
                area.Id,
                PlannedWaterQuantity: 500m,
                PlannedWaterUnitId: massUnit.Id), null));

        Assert.Contains("plannedWaterUnitId", ex.Errors.Keys);
    }

    [Fact]
    public async Task MasterData_InactiveMethod_ThrowsValidationException()
    {
        var farm = CreateFarm(_orgA);
        var area = CreateFarmArea(_orgA, farm.Id);
        var method = CreateMethod(_orgA, "CUSTOM_DRIP", "Custom Drip");
        method.Deactivate(DateTimeOffset.UtcNow, _userA);

        var actor = CreateActorA();

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CreateDraftAsync(actor, new CreateIrrigationDraftRequest(
                farm.Id,
                area.Id,
                IrrigationMethodId: method.Id), null));

        Assert.Contains("irrigationMethodId", ex.Errors.Keys);
    }

    [Fact]
    public async Task WaterValidation_ZeroOrNegativeQuantity_ThrowsValidationException()
    {
        var farm = CreateFarm(_orgA);
        var area = CreateFarmArea(_orgA, farm.Id);
        var volumeUnit = CreateUnit(null, "LITER", UnitCategory.Volume);

        var actor = CreateActorA();

        var exZero = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CreateDraftAsync(actor, new CreateIrrigationDraftRequest(
                farm.Id,
                area.Id,
                PlannedWaterQuantity: 0m,
                PlannedWaterUnitId: volumeUnit.Id), null));
        Assert.Contains("plannedWaterQuantity", exZero.Errors.Keys);

        var exNeg = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CreateDraftAsync(actor, new CreateIrrigationDraftRequest(
                farm.Id,
                area.Id,
                PlannedWaterQuantity: -10m,
                PlannedWaterUnitId: volumeUnit.Id), null));
        Assert.Contains("plannedWaterQuantity", exNeg.Errors.Keys);
    }

    #endregion

    #region Lifecycle State Machine & Terminality

    [Fact]
    public async Task Lifecycle_SequentialProgression_DraftToScheduledToInProgressToCompleted()
    {
        var farm = CreateFarm(_orgA);
        var area = CreateFarmArea(_orgA, farm.Id);
        var method = CreateMethod(null, "DRIP", "Drip System", isSystem: true);
        var volumeUnit = CreateUnit(null, "CUBIC_METER", UnitCategory.Volume);

        var actor = CreateActorA();

        // 1. Create Draft
        var draft = await _sut.CreateDraftAsync(actor, new CreateIrrigationDraftRequest(farm.Id, area.Id), null);
        Assert.Equal(IrrigationStatus.Draft, draft.Status);

        // 2. Schedule
        var scheduledTime = DateTimeOffset.UtcNow.AddDays(1);
        var scheduled = await _sut.ScheduleAsync(actor, draft.Id, new ScheduleIrrigationRequest(scheduledTime), null);
        Assert.Equal(IrrigationStatus.Scheduled, scheduled.Status);
        Assert.Equal(scheduledTime, scheduled.ScheduledAt);

        // 3. Start
        var startedTime = DateTimeOffset.UtcNow;
        var inProgress = await _sut.StartAsync(actor, draft.Id, new StartIrrigationRequest(startedTime), null);
        Assert.Equal(IrrigationStatus.InProgress, inProgress.Status);
        Assert.Equal(startedTime, inProgress.ActualStartedAt);

        // 4. Complete
        var endedTime = startedTime.AddHours(2);
        var completed = await _sut.CompleteAsync(actor, draft.Id, new CompleteIrrigationRequest(
            method.Id,
            ActualStartedAt: startedTime,
            ActualEndedAt: endedTime,
            ActualDurationMinutes: 120,
            ActualWaterQuantity: 250m,
            ActualWaterUnitId: volumeUnit.Id), null);

        Assert.Equal(IrrigationStatus.Completed, completed.Status);
        Assert.Equal(120, completed.ActualDurationMinutes);
        Assert.Equal(250m, completed.ActualWaterQuantity);
        Assert.NotNull(completed.CompletedAt);
    }

    [Fact]
    public async Task Lifecycle_DirectCompletionFromDraftOrScheduled_ThrowsConflictException()
    {
        var farm = CreateFarm(_orgA);
        var area = CreateFarmArea(_orgA, farm.Id);
        var method = CreateMethod(null, "DRIP", "Drip System", isSystem: true);

        var actor = CreateActorA();

        // Try direct complete from Draft
        var draft = await _sut.CreateDraftAsync(actor, new CreateIrrigationDraftRequest(farm.Id, area.Id), null);
        await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.CompleteAsync(actor, draft.Id, new CompleteIrrigationRequest(method.Id), null));

        // Try direct complete from Scheduled
        await _sut.ScheduleAsync(actor, draft.Id, new ScheduleIrrigationRequest(DateTimeOffset.UtcNow.AddHours(2)), null);
        await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.CompleteAsync(actor, draft.Id, new CompleteIrrigationRequest(method.Id), null));
    }

    [Fact]
    public async Task Lifecycle_RecordCompleted_DirectRetrospective1StepWorks()
    {
        var farm = CreateFarm(_orgA);
        var area = CreateFarmArea(_orgA, farm.Id);
        var method = CreateMethod(null, "SPRINKLER", "Sprinkler", isSystem: true);
        var volumeUnit = CreateUnit(null, "LITER", UnitCategory.Volume);

        var actor = CreateActorA();
        var startTime = DateTimeOffset.UtcNow.AddDays(-1);
        var endTime = startTime.AddHours(3);

        var completed = await _sut.RecordCompletedAsync(actor, new RecordCompletedIrrigationRequest(
            FarmId: farm.Id,
            FarmAreaId: area.Id,
            IrrigationMethodId: method.Id,
            ActualStartedAt: startTime,
            ActualEndedAt: endTime,
            ActualDurationMinutes: 180,
            ActualWaterQuantity: 1500m,
            ActualWaterUnitId: volumeUnit.Id,
            Notes: "Retrospective logging"), null);

        Assert.Equal(IrrigationStatus.Completed, completed.Status);
        Assert.Equal(180, completed.ActualDurationMinutes);
        Assert.Equal(1500m, completed.ActualWaterQuantity);
        Assert.NotNull(completed.CompletedAt);
    }

    [Fact]
    public async Task Lifecycle_Terminality_CompletedAndCancelledCannotTransition()
    {
        var farm = CreateFarm(_orgA);
        var area = CreateFarmArea(_orgA, farm.Id);
        var method = CreateMethod(null, "DRIP", "Drip", isSystem: true);

        var actor = CreateActorA();

        // 1. Completed cannot be cancelled or modified
        var completed = await _sut.RecordCompletedAsync(actor, new RecordCompletedIrrigationRequest(farm.Id, area.Id, method.Id), null);
        await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.CancelAsync(actor, completed.Id, new CancelIrrigationRequest("Late cancel"), null));
        await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.ScheduleAsync(actor, completed.Id, new ScheduleIrrigationRequest(DateTimeOffset.UtcNow), null));

        // 2. Cancelled cannot be started or completed
        var draft = await _sut.CreateDraftAsync(actor, new CreateIrrigationDraftRequest(farm.Id, area.Id), null);
        var cancelled = await _sut.CancelAsync(actor, draft.Id, new CancelIrrigationRequest("Aborted"), null);
        Assert.Equal(IrrigationStatus.Cancelled, cancelled.Status);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.StartAsync(actor, draft.Id, new StartIrrigationRequest(), null));
        await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.CompleteAsync(actor, draft.Id, new CompleteIrrigationRequest(method.Id), null));
    }

    [Fact]
    public async Task Cancellation_EmptyReason_ThrowsValidationException()
    {
        var farm = CreateFarm(_orgA);
        var area = CreateFarmArea(_orgA, farm.Id);

        var actor = CreateActorA();
        var draft = await _sut.CreateDraftAsync(actor, new CreateIrrigationDraftRequest(farm.Id, area.Id), null);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CancelAsync(actor, draft.Id, new CancelIrrigationRequest("   "), null));

        Assert.Contains("cancellationReason", ex.Errors.Keys);
    }

    #endregion

    #region Operational Invariants: Past Schedules, Derived Overdue & Manual Duration

    [Fact]
    public async Task Operational_PastSchedulesAllowed()
    {
        var farm = CreateFarm(_orgA);
        var area = CreateFarmArea(_orgA, farm.Id);

        var actor = CreateActorA();
        var draft = await _sut.CreateDraftAsync(actor, new CreateIrrigationDraftRequest(farm.Id, area.Id), null);

        var pastTime = DateTimeOffset.UtcNow.AddDays(-3);
        var scheduled = await _sut.ScheduleAsync(actor, draft.Id, new ScheduleIrrigationRequest(pastTime), null);

        Assert.Equal(IrrigationStatus.Scheduled, scheduled.Status);
        Assert.Equal(pastTime, scheduled.ScheduledAt);
    }

    [Fact]
    public async Task Operational_DerivedOverdue_TrueOnlyWhenScheduledAndInThePast()
    {
        var farm = CreateFarm(_orgA);
        var area = CreateFarmArea(_orgA, farm.Id);

        var actor = CreateActorA();
        var draft = await _sut.CreateDraftAsync(actor, new CreateIrrigationDraftRequest(farm.Id, area.Id), null);

        // Draft is never overdue
        Assert.False(draft.IsOverdue);

        // Scheduled in past is overdue
        var scheduledPast = await _sut.ScheduleAsync(actor, draft.Id, new ScheduleIrrigationRequest(DateTimeOffset.UtcNow.AddHours(-2)), null);
        Assert.True(scheduledPast.IsOverdue);

        // Rescheduled in future is not overdue
        var rescheduledFuture = await _sut.RescheduleAsync(actor, draft.Id, new RescheduleIrrigationRequest(DateTimeOffset.UtcNow.AddDays(2)), null);
        Assert.False(rescheduledFuture.IsOverdue);
    }

    [Fact]
    public async Task Operational_ManualDurationPreserved_NotOverwritten()
    {
        var farm = CreateFarm(_orgA);
        var area = CreateFarmArea(_orgA, farm.Id);
        var method = CreateMethod(null, "DRIP", "Drip", isSystem: true);

        var actor = CreateActorA();
        var start = DateTimeOffset.UtcNow.AddHours(-10);
        var end = DateTimeOffset.UtcNow; // 10 hours diff

        // Explicit manual duration of 45 minutes specified
        var completed = await _sut.RecordCompletedAsync(actor, new RecordCompletedIrrigationRequest(
            farm.Id,
            area.Id,
            method.Id,
            ActualStartedAt: start,
            ActualEndedAt: end,
            ActualDurationMinutes: 45), null);

        Assert.Equal(45, completed.ActualDurationMinutes);
    }

    #endregion

    #region Idempotency & Audit Logs

    [Fact]
    public async Task Idempotency_CreateDraftWithSameKey_ReturnsExistingRecordWithoutDuplicates()
    {
        var farm = CreateFarm(_orgA);
        var area = CreateFarmArea(_orgA, farm.Id);
        var actor = CreateActorA();

        var key = "idemp-" + Guid.NewGuid();

        var first = await _sut.CreateDraftAsync(actor, new CreateIrrigationDraftRequest(farm.Id, area.Id, IdempotencyKey: key), null);
        var second = await _sut.CreateDraftAsync(actor, new CreateIrrigationDraftRequest(farm.Id, area.Id, IdempotencyKey: key), null);

        Assert.Equal(first.Id, second.Id);
        Assert.Single(_store.Irrigations);
        // Only one audit record for DraftCreated
        Assert.Single(_store.AuditLogs, a => a.Action == "Irrigation.DraftCreated");
    }

    [Fact]
    public async Task Idempotency_RecordCompletedWithSameKey_ReturnsExistingRecordWithoutDuplicates()
    {
        var farm = CreateFarm(_orgA);
        var area = CreateFarmArea(_orgA, farm.Id);
        var method = CreateMethod(null, "DRIP", "Drip", isSystem: true);
        var actor = CreateActorA();

        var key = "idemp-" + Guid.NewGuid();

        var first = await _sut.RecordCompletedAsync(actor, new RecordCompletedIrrigationRequest(
            farm.Id,
            area.Id,
            method.Id,
            IdempotencyKey: key), null);

        var second = await _sut.RecordCompletedAsync(actor, new RecordCompletedIrrigationRequest(
            farm.Id,
            area.Id,
            method.Id,
            IdempotencyKey: key), null);

        Assert.Equal(first.Id, second.Id);
        Assert.Single(_store.Irrigations);
        Assert.Single(_store.AuditLogs, a => a.Action == "Irrigation.Completed");
    }

    [Fact]
    public async Task AuditLogs_AllLifecycleTransitions_RecordedAtomically()
    {
        var farm = CreateFarm(_orgA);
        var area = CreateFarmArea(_orgA, farm.Id);
        var method = CreateMethod(null, "DRIP", "Drip", isSystem: true);

        var actor = CreateActorA();

        var draft = await _sut.CreateDraftAsync(actor, new CreateIrrigationDraftRequest(farm.Id, area.Id), "127.0.0.1");
        await _sut.ScheduleAsync(actor, draft.Id, new ScheduleIrrigationRequest(DateTimeOffset.UtcNow.AddHours(1)), "127.0.0.1");
        await _sut.StartAsync(actor, draft.Id, new StartIrrigationRequest(), "127.0.0.1");
        await _sut.CompleteAsync(actor, draft.Id, new CompleteIrrigationRequest(method.Id), "127.0.0.1");

        var actions = _store.AuditLogs.Select(a => a.Action).ToList();

        Assert.Contains("Irrigation.DraftCreated", actions);
        Assert.Contains("Irrigation.Scheduled", actions);
        Assert.Contains("Irrigation.Started", actions);
        Assert.Contains("Irrigation.Completed", actions);
    }

    #endregion

    #region Fake Store Implementation

    private sealed class HardeningFakeStore : IIrrigationStore
    {
        public List<IrrigationEvent> Irrigations { get; } = [];
        public List<Farm> Farms { get; } = [];
        public List<FarmArea> FarmAreas { get; } = [];
        public List<CropPlantation> Plantations { get; } = [];
        public List<CropCycle> CropCycles { get; } = [];
        public List<CropCycleStage> CropCycleStages { get; } = [];
        public List<Unit> Units { get; } = [];
        public List<IrrigationMethod> Methods { get; } = [];
        public List<AuditLog> AuditLogs { get; } = [];

        public Task<IrrigationEvent?> FindAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Irrigations.FirstOrDefault(e => e.Id == id && e.OrganizationId == organizationId));

        public Task<IrrigationEvent?> FindByIdempotencyKeyAsync(Guid organizationId, string idempotencyKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(Irrigations.FirstOrDefault(e => e.OrganizationId == organizationId && e.IdempotencyKey == idempotencyKey));

        public Task<int> CountAsync(Guid organizationId, IrrigationListQuery query, DateTimeOffset now, CancellationToken cancellationToken = default) =>
            Task.FromResult(Irrigations.Count(e => e.OrganizationId == organizationId));

        public Task<IReadOnlyList<IrrigationEvent>> ListAsync(Guid organizationId, IrrigationListQuery query, int skip, int take, DateTimeOffset now, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<IrrigationEvent>>(Irrigations.Where(e => e.OrganizationId == organizationId).Skip(skip).Take(take).ToArray());

        public void Add(IrrigationEvent irrigation) => Irrigations.Add(irrigation);

        public void AddAuditLog(AuditLog auditLog) => AuditLogs.Add(auditLog);

        public Task<Farm?> FindFarmAsync(Guid farmId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Farms.FirstOrDefault(f => f.Id == farmId && f.OrganizationId == organizationId));

        public Task<FarmArea?> FindFarmAreaAsync(Guid farmAreaId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(FarmAreas.FirstOrDefault(a => a.Id == farmAreaId && a.OrganizationId == organizationId));

        public Task<CropPlantation?> FindPlantationAsync(Guid plantationId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Plantations.FirstOrDefault(p => p.Id == plantationId && p.OrganizationId == organizationId));

        public Task<CropCycle?> FindCropCycleAsync(Guid cropCycleId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(CropCycles.FirstOrDefault(c => c.Id == cropCycleId && c.OrganizationId == organizationId));

        public Task<CropCycleStage?> FindCropCycleStageAsync(Guid cropCycleStageId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(CropCycleStages.FirstOrDefault(s => s.Id == cropCycleStageId));

        public Task<Unit?> FindUnitAsync(Guid unitId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Units.FirstOrDefault(u => u.Id == unitId && (u.OrganizationId == organizationId || (u.IsSystem && u.OrganizationId == null))));

        public Task<IrrigationMethod?> FindIrrigationMethodAsync(Guid methodId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Methods.FirstOrDefault(m => m.Id == methodId && (m.OrganizationId == organizationId || (m.IsSystem && m.OrganizationId == null))));

        public Task<IReadOnlyList<IrrigationMethod>> ListMethodsAsync(Guid organizationId, bool activeOnly, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<IrrigationMethod>>(Methods.Where(m => (!activeOnly || m.IsActive) && (m.OrganizationId == organizationId || (m.IsSystem && m.OrganizationId == null))).ToArray());

        public Task<Dictionary<Guid, string>> GetUserNamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken = default) =>
            Task.FromResult(userIds.Distinct().ToDictionary(id => id, id => "Hardening User"));

        public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) =>
            await operation(cancellationToken);

        public Task<IrrigationSummaryCountsResponse> GetSummaryCountsAsync(Guid organizationId, Guid? farmId, DateTimeOffset now, CancellationToken cancellationToken = default) =>
            Task.FromResult(new IrrigationSummaryCountsResponse(0, 0, 0, 0, 0, 0, 0));

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    #endregion
}
