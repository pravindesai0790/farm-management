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

public class IrrigationServiceTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly FakeIrrigationStore _store = new();
    private readonly IrrigationService _sut;

    public IrrigationServiceTests()
    {
        _sut = new IrrigationService(_store);
    }

    private IrrigationActor CreateActor() => new(_userId, _orgId);

    private Farm CreateActiveFarm()
    {
        var farm = new Farm(_orgId, "Valley Farm", Guid.NewGuid(), Guid.NewGuid(), 100m, Guid.NewGuid());
        _store.Farms.Add(farm);
        return farm;
    }

    private FarmArea CreateActiveFarmArea(Guid farmId)
    {
        var area = new FarmArea(_orgId, farmId, null, "Area East", 20m, Guid.NewGuid(), _userId);
        _store.FarmAreas.Add(area);
        return area;
    }

    private CropPlantation CreateActivePlantation(Guid farmId, Guid farmAreaId)
    {
        var plantation = new CropPlantation(_orgId, farmId, farmAreaId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Block Alpha", 10m, Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), null, _userId);
        _store.Plantations.Add(plantation);
        return plantation;
    }

    private CropCycle CreateActiveCropCycle(Guid plantationId)
    {
        var cycle = new CropCycle(_orgId, plantationId, "Cycle 2026-1", 2026, "Spring", DateOnly.FromDateTime(DateTime.UtcNow), null, _userId);
        _store.CropCycles.Add(cycle);
        return cycle;
    }

    private CropCycleStage CreateActiveCropCycleStage(Guid cropCycleId)
    {
        var stage = new CropCycleStage(cropCycleId, Guid.NewGuid(), "Vegetative", 1, 14, DateOnly.FromDateTime(DateTime.UtcNow), null, _userId);
        _store.CropCycleStages.Add(stage);
        return stage;
    }

    private Unit CreateActiveVolumeUnit(string code = "LITER")
    {
        var unit = new Unit(null, code, "Liter", "L", UnitCategory.Volume, isSystem: true);
        _store.Units.Add(unit);
        return unit;
    }

    private Unit CreateActiveAreaUnit()
    {
        var unit = new Unit(null, "HECTARE", "Hectare", "ha", UnitCategory.Area, isSystem: true);
        _store.Units.Add(unit);
        return unit;
    }

    private IrrigationMethod CreateActiveMethod(string code = "DRIP")
    {
        var method = new IrrigationMethod(null, code, "Drip Irrigation", isSystem: true);
        _store.Methods.Add(method);
        return method;
    }

    [Fact]
    public async Task CreateDraft_ValidHierarchyAndWaterVolume_CreatesSuccessfully()
    {
        var farm = CreateActiveFarm();
        var area = CreateActiveFarmArea(farm.Id);
        var plantation = CreateActivePlantation(farm.Id, area.Id);
        var cycle = CreateActiveCropCycle(plantation.Id);
        var stage = CreateActiveCropCycleStage(cycle.Id);
        var unit = CreateActiveVolumeUnit();
        var method = CreateActiveMethod();

        var request = new CreateIrrigationDraftRequest(
            FarmId: farm.Id,
            FarmAreaId: area.Id,
            PlantationId: plantation.Id,
            CropCycleId: cycle.Id,
            CropCycleStageId: stage.Id,
            IrrigationMethodId: method.Id,
            PlannedAt: DateTimeOffset.UtcNow.AddDays(1),
            PlannedWaterQuantity: 500m,
            PlannedWaterUnitId: unit.Id,
            Notes: "Early morning drip");

        var response = await _sut.CreateDraftAsync(CreateActor(), request, "127.0.0.1");

        Assert.NotNull(response);
        Assert.Equal(farm.Id, response.FarmId);
        Assert.Equal(area.Id, response.FarmAreaId);
        Assert.Equal(plantation.Id, response.PlantationId);
        Assert.Equal(cycle.Id, response.CropCycleId);
        Assert.Equal(stage.Id, response.CropCycleStageId);
        Assert.Equal(IrrigationStatus.Draft, response.Status);
        Assert.Equal(500m, response.PlannedWaterQuantity);
        Assert.Equal("Early morning drip", response.Notes);
        Assert.Single(_store.AuditLogs);
        Assert.Equal("Irrigation.DraftCreated", _store.AuditLogs[0].Action);
    }

    [Fact]
    public async Task CreateDraft_FarmAreaFromDifferentFarm_ThrowsValidationException()
    {
        var farm = CreateActiveFarm();
        var otherFarm = CreateActiveFarm();
        var areaFromOtherFarm = CreateActiveFarmArea(otherFarm.Id);

        var request = new CreateIrrigationDraftRequest(
            FarmId: farm.Id,
            FarmAreaId: areaFromOtherFarm.Id);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CreateDraftAsync(CreateActor(), request, "127.0.0.1"));

        Assert.True(ex.Errors?.ContainsKey("farmAreaId"));
    }

    [Fact]
    public async Task CreateDraft_PlantationMismatchArea_ThrowsValidationException()
    {
        var farm = CreateActiveFarm();
        var area1 = CreateActiveFarmArea(farm.Id);
        var area2 = CreateActiveFarmArea(farm.Id);
        var plantationInArea2 = CreateActivePlantation(farm.Id, area2.Id);

        var request = new CreateIrrigationDraftRequest(
            FarmId: farm.Id,
            FarmAreaId: area1.Id,
            PlantationId: plantationInArea2.Id);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CreateDraftAsync(CreateActor(), request, "127.0.0.1"));

        Assert.True(ex.Errors?.ContainsKey("plantationId"));
    }

    [Fact]
    public async Task CreateDraft_CycleMismatchPlantation_ThrowsValidationException()
    {
        var farm = CreateActiveFarm();
        var area = CreateActiveFarmArea(farm.Id);
        var plantation1 = CreateActivePlantation(farm.Id, area.Id);
        var plantation2 = CreateActivePlantation(farm.Id, area.Id);
        var cycleForPlantation2 = CreateActiveCropCycle(plantation2.Id);

        var request = new CreateIrrigationDraftRequest(
            FarmId: farm.Id,
            FarmAreaId: area.Id,
            PlantationId: plantation1.Id,
            CropCycleId: cycleForPlantation2.Id);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CreateDraftAsync(CreateActor(), request, "127.0.0.1"));

        Assert.True(ex.Errors?.ContainsKey("cropCycleId"));
    }

    [Fact]
    public async Task CreateDraft_StageWithoutCycle_ThrowsValidationException()
    {
        var farm = CreateActiveFarm();
        var area = CreateActiveFarmArea(farm.Id);
        var plantation = CreateActivePlantation(farm.Id, area.Id);

        var request = new CreateIrrigationDraftRequest(
            FarmId: farm.Id,
            FarmAreaId: area.Id,
            PlantationId: plantation.Id,
            CropCycleId: null,
            CropCycleStageId: Guid.NewGuid());

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CreateDraftAsync(CreateActor(), request, "127.0.0.1"));

        Assert.True(ex.Errors?.ContainsKey("cropCycleStageId"));
    }

    [Fact]
    public async Task CreateDraft_NonVolumeUnit_ThrowsValidationException()
    {
        var farm = CreateActiveFarm();
        var area = CreateActiveFarmArea(farm.Id);
        var areaUnit = CreateActiveAreaUnit();

        var request = new CreateIrrigationDraftRequest(
            FarmId: farm.Id,
            FarmAreaId: area.Id,
            PlannedWaterQuantity: 100m,
            PlannedWaterUnitId: areaUnit.Id);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CreateDraftAsync(CreateActor(), request, "127.0.0.1"));

        Assert.True(ex.Errors?.ContainsKey("plannedWaterUnitId"));
        Assert.Contains("must be a volume unit", ex.Errors!["plannedWaterUnitId"][0]);
    }

    [Fact]
    public async Task Schedule_FromDraft_SetsScheduledStatus()
    {
        var farm = CreateActiveFarm();
        var area = CreateActiveFarmArea(farm.Id);
        var scheduledAt = DateTimeOffset.UtcNow.AddDays(2);

        var draft = new IrrigationEvent(_orgId, farm.Id, area.Id, _userId);
        _store.Irrigations.Add(draft);

        var response = await _sut.ScheduleAsync(CreateActor(), draft.Id, new ScheduleIrrigationRequest(scheduledAt), "127.0.0.1");

        Assert.Equal(IrrigationStatus.Scheduled, response.Status);
        Assert.Equal(scheduledAt, response.ScheduledAt);
    }

    [Fact]
    public async Task Schedule_PastTimestamp_Accepted()
    {
        var farm = CreateActiveFarm();
        var area = CreateActiveFarmArea(farm.Id);
        var pastScheduledAt = DateTimeOffset.UtcNow.AddDays(-2);

        var draft = new IrrigationEvent(_orgId, farm.Id, area.Id, _userId);
        _store.Irrigations.Add(draft);

        var response = await _sut.ScheduleAsync(CreateActor(), draft.Id, new ScheduleIrrigationRequest(pastScheduledAt), "127.0.0.1");

        Assert.Equal(IrrigationStatus.Scheduled, response.Status);
        Assert.Equal(pastScheduledAt, response.ScheduledAt);
        Assert.True(response.IsOverdue);
    }

    [Fact]
    public async Task Reschedule_WhenScheduled_UpdatesTimestamp()
    {
        var farm = CreateActiveFarm();
        var area = CreateActiveFarmArea(farm.Id);

        var irrigation = new IrrigationEvent(_orgId, farm.Id, area.Id, _userId);
        irrigation.Schedule(DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow, _userId);
        _store.Irrigations.Add(irrigation);

        var newTime = DateTimeOffset.UtcNow.AddDays(3);
        var response = await _sut.RescheduleAsync(CreateActor(), irrigation.Id, new RescheduleIrrigationRequest(newTime), "127.0.0.1");

        Assert.Equal(newTime, response.ScheduledAt);
        Assert.Equal(IrrigationStatus.Scheduled, response.Status);
    }

    [Fact]
    public async Task Reschedule_WhenDraft_ThrowsConflictException()
    {
        var farm = CreateActiveFarm();
        var area = CreateActiveFarmArea(farm.Id);
        var draft = new IrrigationEvent(_orgId, farm.Id, area.Id, _userId);
        _store.Irrigations.Add(draft);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.RescheduleAsync(CreateActor(), draft.Id, new RescheduleIrrigationRequest(DateTimeOffset.UtcNow), "127.0.0.1"));
    }

    [Fact]
    public async Task Start_FromDraftOrScheduled_SetsInProgress()
    {
        var farm = CreateActiveFarm();
        var area = CreateActiveFarmArea(farm.Id);
        var draft = new IrrigationEvent(_orgId, farm.Id, area.Id, _userId);
        _store.Irrigations.Add(draft);

        var startTime = DateTimeOffset.UtcNow;
        var response = await _sut.StartAsync(CreateActor(), draft.Id, new StartIrrigationRequest(startTime), "127.0.0.1");

        Assert.Equal(IrrigationStatus.InProgress, response.Status);
        Assert.Equal(startTime, response.ActualStartedAt);
    }

    [Fact]
    public async Task Start_WhenAlreadyInProgress_ThrowsConflictException()
    {
        var farm = CreateActiveFarm();
        var area = CreateActiveFarmArea(farm.Id);
        var irrigation = new IrrigationEvent(_orgId, farm.Id, area.Id, _userId);
        irrigation.Start(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, _userId);
        _store.Irrigations.Add(irrigation);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.StartAsync(CreateActor(), irrigation.Id, new StartIrrigationRequest(), "127.0.0.1"));
    }

    [Fact]
    public async Task Complete_FromInProgress_SetsStatusCompletedAndAudit()
    {
        var farm = CreateActiveFarm();
        var area = CreateActiveFarmArea(farm.Id);
        var method = CreateActiveMethod();
        var unit = CreateActiveVolumeUnit();

        var irrigation = new IrrigationEvent(_orgId, farm.Id, area.Id, _userId);
        irrigation.Start(DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow, _userId);
        _store.Irrigations.Add(irrigation);

        var request = new CompleteIrrigationRequest(
            IrrigationMethodId: method.Id,
            ActualStartedAt: DateTimeOffset.UtcNow.AddHours(-1),
            ActualEndedAt: DateTimeOffset.UtcNow,
            ActualDurationMinutes: 60,
            ActualWaterQuantity: 1200m,
            ActualWaterUnitId: unit.Id,
            Notes: "Finished without issues");

        var response = await _sut.CompleteAsync(CreateActor(), irrigation.Id, request, "127.0.0.1");

        Assert.Equal(IrrigationStatus.Completed, response.Status);
        Assert.Equal(60, response.ActualDurationMinutes);
        Assert.Equal(1200m, response.ActualWaterQuantity);
        Assert.NotNull(response.CompletedAt);
        Assert.Contains(_store.AuditLogs, a => a.Action == "Irrigation.Completed");
    }

    [Fact]
    public async Task Complete_DirectFromDraft_SetsStatusCompleted()
    {
        var farm = CreateActiveFarm();
        var area = CreateActiveFarmArea(farm.Id);
        var method = CreateActiveMethod();

        var draft = new IrrigationEvent(_orgId, farm.Id, area.Id, _userId);
        _store.Irrigations.Add(draft);

        var request = new CompleteIrrigationRequest(
            IrrigationMethodId: method.Id,
            ActualDurationMinutes: 30);

        var response = await _sut.CompleteAsync(CreateActor(), draft.Id, request, "127.0.0.1");

        Assert.Equal(IrrigationStatus.Completed, response.Status);
        Assert.Equal(30, response.ActualDurationMinutes);
    }

    [Fact]
    public async Task Complete_DirectFromScheduled_SetsStatusCompleted()
    {
        var farm = CreateActiveFarm();
        var area = CreateActiveFarmArea(farm.Id);
        var method = CreateActiveMethod();

        var scheduled = new IrrigationEvent(_orgId, farm.Id, area.Id, _userId);
        scheduled.Schedule(DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow, _userId);
        _store.Irrigations.Add(scheduled);

        var request = new CompleteIrrigationRequest(
            IrrigationMethodId: method.Id,
            ActualDurationMinutes: 45);

        var response = await _sut.CompleteAsync(CreateActor(), scheduled.Id, request, "127.0.0.1");

        Assert.Equal(IrrigationStatus.Completed, response.Status);
        Assert.Equal(45, response.ActualDurationMinutes);
    }

    [Fact]
    public async Task Complete_EndBeforeStart_ThrowsValidationException()
    {
        var farm = CreateActiveFarm();
        var area = CreateActiveFarmArea(farm.Id);
        var method = CreateActiveMethod();

        var draft = new IrrigationEvent(_orgId, farm.Id, area.Id, _userId);
        _store.Irrigations.Add(draft);

        var start = DateTimeOffset.UtcNow;
        var end = start.AddHours(-1);

        var request = new CompleteIrrigationRequest(
            IrrigationMethodId: method.Id,
            ActualStartedAt: start,
            ActualEndedAt: end);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CompleteAsync(CreateActor(), draft.Id, request, "127.0.0.1"));

        Assert.True(ex.Errors?.ContainsKey("actualEndedAt"));
    }

    [Fact]
    public async Task Complete_WhenAlreadyCompleted_ThrowsConflictException()
    {
        var farm = CreateActiveFarm();
        var area = CreateActiveFarmArea(farm.Id);
        var method = CreateActiveMethod();

        var completed = new IrrigationEvent(_orgId, farm.Id, area.Id, _userId);
        completed.Complete(method.Id, null, null, null, null, null, null, DateTimeOffset.UtcNow, _userId);
        _store.Irrigations.Add(completed);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.CompleteAsync(CreateActor(), completed.Id, new CompleteIrrigationRequest(method.Id), "127.0.0.1"));
    }

    [Fact]
    public async Task RecordCompleted_CreatesAtomicCompletedEvent()
    {
        var farm = CreateActiveFarm();
        var area = CreateActiveFarmArea(farm.Id);
        var method = CreateActiveMethod();
        var unit = CreateActiveVolumeUnit();

        var request = new RecordCompletedIrrigationRequest(
            FarmId: farm.Id,
            FarmAreaId: area.Id,
            IrrigationMethodId: method.Id,
            ActualDurationMinutes: 90,
            ActualWaterQuantity: 2500m,
            ActualWaterUnitId: unit.Id,
            Notes: "Retrospective logging");

        var response = await _sut.RecordCompletedAsync(CreateActor(), request, "127.0.0.1");

        Assert.NotNull(response);
        Assert.Equal(IrrigationStatus.Completed, response.Status);
        Assert.Equal(90, response.ActualDurationMinutes);
        Assert.Equal(2500m, response.ActualWaterQuantity);
        Assert.Contains(_store.AuditLogs, a => a.Action == "Irrigation.Completed");
    }

    [Fact]
    public async Task Cancel_WithReason_CancelsDraftScheduledOrInProgress()
    {
        var farm = CreateActiveFarm();
        var area = CreateActiveFarmArea(farm.Id);
        var scheduled = new IrrigationEvent(_orgId, farm.Id, area.Id, _userId);
        scheduled.Schedule(DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow, _userId);
        _store.Irrigations.Add(scheduled);

        var response = await _sut.CancelAsync(CreateActor(), scheduled.Id, new CancelIrrigationRequest("Thunderstorm alert"), "127.0.0.1");

        Assert.Equal(IrrigationStatus.Cancelled, response.Status);
        Assert.Equal("Thunderstorm alert", response.CancellationReason);
    }

    [Fact]
    public async Task Cancel_BlankReason_ThrowsValidationException()
    {
        var farm = CreateActiveFarm();
        var area = CreateActiveFarmArea(farm.Id);
        var draft = new IrrigationEvent(_orgId, farm.Id, area.Id, _userId);
        _store.Irrigations.Add(draft);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CancelAsync(CreateActor(), draft.Id, new CancelIrrigationRequest("   "), "127.0.0.1"));

        Assert.True(ex.Errors?.ContainsKey("cancellationReason"));
    }

    [Fact]
    public async Task Cancel_WhenCompleted_ThrowsConflictException()
    {
        var farm = CreateActiveFarm();
        var area = CreateActiveFarmArea(farm.Id);
        var method = CreateActiveMethod();

        var completed = new IrrigationEvent(_orgId, farm.Id, area.Id, _userId);
        completed.Complete(method.Id, null, null, null, null, null, null, DateTimeOffset.UtcNow, _userId);
        _store.Irrigations.Add(completed);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.CancelAsync(CreateActor(), completed.Id, new CancelIrrigationRequest("Cannot cancel"), "127.0.0.1"));
    }

    [Fact]
    public async Task GetAsync_ComputesIsOverdue_TrueWhenScheduledAndPast()
    {
        var farm = CreateActiveFarm();
        var area = CreateActiveFarmArea(farm.Id);
        var pastTime = DateTimeOffset.UtcNow.AddDays(-1);

        var scheduled = new IrrigationEvent(_orgId, farm.Id, area.Id, _userId);
        scheduled.Schedule(pastTime, DateTimeOffset.UtcNow, _userId);
        _store.Irrigations.Add(scheduled);

        var response = await _sut.GetAsync(CreateActor(), scheduled.Id);

        Assert.True(response.IsOverdue);
    }

    [Fact]
    public async Task GetAsync_ComputesIsOverdue_FalseWhenDraftOrFuture()
    {
        var farm = CreateActiveFarm();
        var area = CreateActiveFarmArea(farm.Id);
        var draft = new IrrigationEvent(_orgId, farm.Id, area.Id, _userId, plannedAt: DateTimeOffset.UtcNow.AddDays(-5));
        _store.Irrigations.Add(draft);

        var response = await _sut.GetAsync(CreateActor(), draft.Id);

        Assert.False(response.IsOverdue);
    }

    [Fact]
    public async Task GetSummaryCountsAsync_ComputesBucketsCorrectly()
    {
        var farm1 = CreateActiveFarm();
        var farm2 = CreateActiveFarm();
        var area1 = CreateActiveFarmArea(farm1.Id);
        var area2 = CreateActiveFarmArea(farm2.Id);

        var draft = new IrrigationEvent(_orgId, farm1.Id, area1.Id, _userId);
        var scheduledOverdue = new IrrigationEvent(_orgId, farm1.Id, area1.Id, _userId);
        scheduledOverdue.Schedule(DateTimeOffset.UtcNow.AddHours(-2), DateTimeOffset.UtcNow, _userId);
        var scheduledFuture = new IrrigationEvent(_orgId, farm2.Id, area2.Id, _userId);
        scheduledFuture.Schedule(DateTimeOffset.UtcNow.AddHours(2), DateTimeOffset.UtcNow, _userId);

        _store.Irrigations.AddRange([draft, scheduledOverdue, scheduledFuture]);

        var actor = CreateActor();
        var summaryAll = await _sut.GetSummaryCountsAsync(actor, null);
        Assert.Equal(3, summaryAll.TotalCount);
        Assert.Equal(1, summaryAll.DraftCount);
        Assert.Equal(2, summaryAll.ScheduledCount);
        Assert.Equal(1, summaryAll.OverdueCount);

        var summaryFarm1 = await _sut.GetSummaryCountsAsync(actor, farm1.Id);
        Assert.Equal(2, summaryFarm1.TotalCount);
        Assert.Equal(1, summaryFarm1.DraftCount);
        Assert.Equal(1, summaryFarm1.ScheduledCount);
        Assert.Equal(1, summaryFarm1.OverdueCount);
    }

    [Fact]
    public async Task Mutation_WithMismatchedConcurrencyToken_ThrowsConflictException()
    {
        var farm = CreateActiveFarm();
        var area = CreateActiveFarmArea(farm.Id);
        var draft = new IrrigationEvent(_orgId, farm.Id, area.Id, _userId);
        _store.Irrigations.Add(draft);

        var actor = CreateActor();
        var staleToken = "2020-01-01T00:00:00.0000000Z";

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.ScheduleAsync(actor, draft.Id, new ScheduleIrrigationRequest(DateTimeOffset.UtcNow, staleToken), null));

        Assert.Contains("modified by another operation", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FakeIrrigationStore : IIrrigationStore
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
            Task.FromResult<IReadOnlyList<IrrigationMethod>>(Methods.Where(m => !activeOnly || m.IsActive).ToArray());

        public Task<Dictionary<Guid, string>> GetUserNamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken = default) =>
            Task.FromResult(userIds.Distinct().ToDictionary(id => id, id => "Test User"));

        public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) =>
            await operation(cancellationToken);

        public Task<IrrigationSummaryCountsResponse> GetSummaryCountsAsync(Guid organizationId, Guid? farmId, DateTimeOffset now, CancellationToken cancellationToken = default)
        {
            var items = Irrigations.Where(e => e.OrganizationId == organizationId);
            if (farmId.HasValue && farmId.Value != Guid.Empty)
            {
                items = items.Where(e => e.FarmId == farmId.Value);
            }

            var list = items.ToList();
            var total = list.Count;
            var draft = list.Count(e => e.Status == IrrigationStatus.Draft);
            var scheduled = list.Count(e => e.Status == IrrigationStatus.Scheduled);
            var inProgress = list.Count(e => e.Status == IrrigationStatus.InProgress);
            var completed = list.Count(e => e.Status == IrrigationStatus.Completed);
            var cancelled = list.Count(e => e.Status == IrrigationStatus.Cancelled);
            var overdue = list.Count(e => e.Status == IrrigationStatus.Scheduled && e.ScheduledAt.HasValue && e.ScheduledAt.Value < now);

            return Task.FromResult(new IrrigationSummaryCountsResponse(total, draft, scheduled, inProgress, completed, cancelled, overdue));
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
