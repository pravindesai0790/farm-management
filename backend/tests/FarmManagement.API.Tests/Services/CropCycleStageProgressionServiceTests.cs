using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.CropCycles;
using FarmManagement.Application.Interfaces.CropCycles;
using FarmManagement.Application.Services;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Services;

public sealed class CropCycleStageProgressionServiceTests
{
    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    private CropCycleActor CreateActor(Guid? organizationId = null) => new(_userId, organizationId ?? _organizationId);

    [Fact]
    public async Task CompleteStageAsync_WhenCurrentStageInProgress_CompletesStageAndStartsNextStage()
    {
        var store = new FakeCropCycleStore();
        var crop = CreateCrop(_organizationId, "Grape");
        var plantation = CreatePlantation(store, _organizationId, crop);
        var template = CreateTemplate(store, _organizationId, crop.Id, "Grape Standard");
        AddStage(template, "Dormancy", 1, 30);
        AddStage(template, "Pruning", 2, 15);

        var cycle = new CropCycle(_organizationId, plantation.Id, "2026 Cycle", 2026, null, new DateOnly(2026, 4, 1), null, _userId, template.Id);
        store.Cycles.Add(cycle);

        var service = new CropCycleService(store);
        await service.StartAsync(CreateActor(), cycle.Id, new StartCropCycleRequest(new DateOnly(2026, 4, 1)), "127.0.0.1");

        var stages = await store.GetStagesAsync(cycle.Id);
        var stage1 = stages[0];
        var stage2 = stages[1];

        Assert.Equal(CropCycleStageStatus.InProgress, stage1.Status);
        Assert.Equal(CropCycleStageStatus.NotStarted, stage2.Status);

        var endDate = new DateOnly(2026, 4, 30);
        var response = await service.CompleteStageAsync(CreateActor(), stage1.Id, new CompleteCropCycleStageRequest(endDate, "Completed dormancy"), "127.0.0.1");

        Assert.Equal("COMPLETED", response.Status);
        Assert.Equal(endDate, response.ActualEndDate);

        var updatedStages = await store.GetStagesAsync(cycle.Id);
        Assert.Equal(CropCycleStageStatus.Completed, updatedStages[0].Status);
        Assert.Equal(CropCycleStageStatus.InProgress, updatedStages[1].Status);
        Assert.Equal(endDate, updatedStages[1].ActualStartDate);

        Assert.Contains(store.AuditLogs, a => a.Action == "CropCycleStage.Completed");
        Assert.Contains(store.AuditLogs, a => a.Action == "CropCycleStage.Started");
    }

    [Fact]
    public async Task CompleteStageAsync_WhenLastStageCompleted_LeavesAllStagesCompleted()
    {
        var store = new FakeCropCycleStore();
        var crop = CreateCrop(_organizationId, "Grape");
        var plantation = CreatePlantation(store, _organizationId, crop);
        var template = CreateTemplate(store, _organizationId, crop.Id, "Single Stage Template");
        AddStage(template, "Harvest", 1, 10);

        var cycle = new CropCycle(_organizationId, plantation.Id, "Harvest Cycle", 2026, null, new DateOnly(2026, 4, 1), null, _userId, template.Id);
        store.Cycles.Add(cycle);

        var service = new CropCycleService(store);
        await service.StartAsync(CreateActor(), cycle.Id, new StartCropCycleRequest(new DateOnly(2026, 4, 1)), "127.0.0.1");

        var stage = (await store.GetStagesAsync(cycle.Id))[0];
        var response = await service.CompleteStageAsync(CreateActor(), stage.Id, new CompleteCropCycleStageRequest(new DateOnly(2026, 4, 10), "Harvest done"), "127.0.0.1");

        Assert.Equal("COMPLETED", response.Status);

        var stages = await store.GetStagesAsync(cycle.Id);
        Assert.All(stages, s => Assert.Equal(CropCycleStageStatus.Completed, s.Status));
    }

    [Fact]
    public async Task CompleteStageAsync_WhenStageNotStarted_ThrowsConflictException()
    {
        var store = new FakeCropCycleStore();
        var crop = CreateCrop(_organizationId, "Grape");
        var plantation = CreatePlantation(store, _organizationId, crop);
        var template = CreateTemplate(store, _organizationId, crop.Id, "Grape Standard");
        AddStage(template, "Dormancy", 1, 30);
        AddStage(template, "Pruning", 2, 15);

        var cycle = new CropCycle(_organizationId, plantation.Id, "2026 Cycle", 2026, null, new DateOnly(2026, 4, 1), null, _userId, template.Id);
        store.Cycles.Add(cycle);

        var service = new CropCycleService(store);
        await service.StartAsync(CreateActor(), cycle.Id, new StartCropCycleRequest(new DateOnly(2026, 4, 1)), "127.0.0.1");

        var stage2 = (await store.GetStagesAsync(cycle.Id))[1];

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CompleteStageAsync(CreateActor(), stage2.Id, new CompleteCropCycleStageRequest(new DateOnly(2026, 5, 1)), "127.0.0.1"));
    }

    [Fact]
    public async Task SkipStageAsync_WithValidReason_SkipsStageAndAdvancesNextStage()
    {
        var store = new FakeCropCycleStore();
        var crop = CreateCrop(_organizationId, "Grape");
        var plantation = CreatePlantation(store, _organizationId, crop);
        var template = CreateTemplate(store, _organizationId, crop.Id, "Grape Standard");
        AddStage(template, "Dormancy", 1, 30);
        AddStage(template, "Pruning", 2, 15);

        var cycle = new CropCycle(_organizationId, plantation.Id, "2026 Cycle", 2026, null, new DateOnly(2026, 4, 1), null, _userId, template.Id);
        store.Cycles.Add(cycle);

        var service = new CropCycleService(store);
        await service.StartAsync(CreateActor(), cycle.Id, new StartCropCycleRequest(new DateOnly(2026, 4, 1)), "127.0.0.1");

        var stage1 = (await store.GetStagesAsync(cycle.Id))[0];
        var skipDate = new DateOnly(2026, 4, 15);

        var response = await service.SkipStageAsync(CreateActor(), stage1.Id, new SkipCropCycleStageRequest("Skipped due to warm winter", skipDate), "127.0.0.1");

        Assert.Equal("SKIPPED", response.Status);

        var stages = await store.GetStagesAsync(cycle.Id);
        Assert.Equal(CropCycleStageStatus.Skipped, stages[0].Status);
        Assert.Equal(CropCycleStageStatus.InProgress, stages[1].Status);
        Assert.Equal(skipDate, stages[1].ActualStartDate);

        Assert.Contains(store.AuditLogs, a => a.Action == "CropCycleStage.Skipped");
    }

    [Fact]
    public async Task SkipStageAsync_WithoutReason_ThrowsValidationException()
    {
        var store = new FakeCropCycleStore();
        var crop = CreateCrop(_organizationId, "Grape");
        var plantation = CreatePlantation(store, _organizationId, crop);
        var template = CreateTemplate(store, _organizationId, crop.Id, "Grape Standard");
        AddStage(template, "Dormancy", 1, 30);

        var cycle = new CropCycle(_organizationId, plantation.Id, "2026 Cycle", 2026, null, new DateOnly(2026, 4, 1), null, _userId, template.Id);
        store.Cycles.Add(cycle);

        var service = new CropCycleService(store);
        await service.StartAsync(CreateActor(), cycle.Id, new StartCropCycleRequest(new DateOnly(2026, 4, 1)), "127.0.0.1");

        var stage1 = (await store.GetStagesAsync(cycle.Id))[0];

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.SkipStageAsync(CreateActor(), stage1.Id, new SkipCropCycleStageRequest("   "), "127.0.0.1"));
    }

    [Fact]
    public async Task SkipStageAsync_WhenStageNotStarted_ThrowsConflictException()
    {
        var store = new FakeCropCycleStore();
        var crop = CreateCrop(_organizationId, "Grape");
        var plantation = CreatePlantation(store, _organizationId, crop);
        var template = CreateTemplate(store, _organizationId, crop.Id, "Grape Standard");
        AddStage(template, "Dormancy", 1, 30);
        AddStage(template, "Pruning", 2, 15);

        var cycle = new CropCycle(_organizationId, plantation.Id, "2026 Cycle", 2026, null, new DateOnly(2026, 4, 1), null, _userId, template.Id);
        store.Cycles.Add(cycle);

        var service = new CropCycleService(store);
        await service.StartAsync(CreateActor(), cycle.Id, new StartCropCycleRequest(new DateOnly(2026, 4, 1)), "127.0.0.1");

        var stage2 = (await store.GetStagesAsync(cycle.Id))[1]; // Stage 2 is NOT_STARTED

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.SkipStageAsync(CreateActor(), stage2.Id, new SkipCropCycleStageRequest("Cannot skip future stage"), "127.0.0.1"));
    }

    [Fact]
    public async Task ReopenStageAsync_WhenPriorStageIncomplete_ThrowsConflictException()
    {
        var store = new FakeCropCycleStore();
        var crop = CreateCrop(_organizationId, "Grape");
        var plantation = CreatePlantation(store, _organizationId, crop);
        var template = CreateTemplate(store, _organizationId, crop.Id, "Grape Standard");
        AddStage(template, "Stage 1", 1, 10);
        AddStage(template, "Stage 2", 2, 10);
        AddStage(template, "Stage 3", 3, 10);

        var cycle = new CropCycle(_organizationId, plantation.Id, "2026 Cycle", 2026, null, new DateOnly(2026, 4, 1), null, _userId, template.Id);
        store.Cycles.Add(cycle);

        var service = new CropCycleService(store);
        await service.StartAsync(CreateActor(), cycle.Id, new StartCropCycleRequest(new DateOnly(2026, 4, 1)), "127.0.0.1");

        var stages = await store.GetStagesAsync(cycle.Id);
        // Manually force Stage 3 to SKIPPED to test reopening while Stage 1 is IN_PROGRESS
        stages[2].Skip("Forced skip test", DateTimeOffset.UtcNow, _userId, null);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.ReopenStageAsync(CreateActor(), stages[2].Id, new ReopenCropCycleStageRequest("Try reopen Stage 3"), "127.0.0.1"));
    }

    [Fact]
    public async Task ReopenStageAsync_ReopensCompletedStageAndReconcilesLaterStages()
    {
        var store = new FakeCropCycleStore();
        var crop = CreateCrop(_organizationId, "Grape");
        var plantation = CreatePlantation(store, _organizationId, crop);
        var template = CreateTemplate(store, _organizationId, crop.Id, "Grape Standard");
        AddStage(template, "Stage 1", 1, 10);
        AddStage(template, "Stage 2", 2, 10);
        AddStage(template, "Stage 3", 3, 10);

        var cycle = new CropCycle(_organizationId, plantation.Id, "2026 Cycle", 2026, null, new DateOnly(2026, 4, 1), null, _userId, template.Id);
        store.Cycles.Add(cycle);

        var service = new CropCycleService(store);
        await service.StartAsync(CreateActor(), cycle.Id, new StartCropCycleRequest(new DateOnly(2026, 4, 1)), "127.0.0.1");

        var stages = await store.GetStagesAsync(cycle.Id);
        await service.CompleteStageAsync(CreateActor(), stages[0].Id, new CompleteCropCycleStageRequest(new DateOnly(2026, 4, 10)), "127.0.0.1");

        // Now Stage 1 is COMPLETED, Stage 2 is IN_PROGRESS, Stage 3 is NOT_STARTED
        stages = await store.GetStagesAsync(cycle.Id);
        Assert.Equal(CropCycleStageStatus.Completed, stages[0].Status);
        Assert.Equal(CropCycleStageStatus.InProgress, stages[1].Status);

        // Reopen Stage 1
        var response = await service.ReopenStageAsync(CreateActor(), stages[0].Id, new ReopenCropCycleStageRequest("Need to adjust Stage 1 records"), "127.0.0.1");

        Assert.Equal("IN_PROGRESS", response.Status);

        var updatedStages = await store.GetStagesAsync(cycle.Id);
        Assert.Equal(CropCycleStageStatus.InProgress, updatedStages[0].Status);
        Assert.Equal(CropCycleStageStatus.NotStarted, updatedStages[1].Status); // Reconciled!
        Assert.Equal(CropCycleStageStatus.NotStarted, updatedStages[2].Status);

        // Verify invariant: ONLY 1 IN_PROGRESS stage exists
        Assert.Single(updatedStages, s => s.Status == CropCycleStageStatus.InProgress);

        Assert.Contains(store.AuditLogs, a => a.Action == "CropCycleStage.Reopened");
    }

    [Fact]
    public async Task OverrideStageAsync_SetsTargetStatusAndReconcilesOtherInProgressStages()
    {
        var store = new FakeCropCycleStore();
        var crop = CreateCrop(_organizationId, "Grape");
        var plantation = CreatePlantation(store, _organizationId, crop);
        var template = CreateTemplate(store, _organizationId, crop.Id, "Grape Standard");
        AddStage(template, "Stage 1", 1, 10);
        AddStage(template, "Stage 2", 2, 10);

        var cycle = new CropCycle(_organizationId, plantation.Id, "2026 Cycle", 2026, null, new DateOnly(2026, 4, 1), null, _userId, template.Id);
        store.Cycles.Add(cycle);

        var service = new CropCycleService(store);
        await service.StartAsync(CreateActor(), cycle.Id, new StartCropCycleRequest(new DateOnly(2026, 4, 1)), "127.0.0.1");

        var stages = await store.GetStagesAsync(cycle.Id);
        // Stage 1 is IN_PROGRESS. Override Stage 2 directly to IN_PROGRESS
        var overrideRequest = new OverrideCropCycleStageRequest(
            TargetStatus: "IN_PROGRESS",
            ActualStartDate: new DateOnly(2026, 4, 15),
            ActualEndDate: null,
            Reason: "Special override to force Stage 2 active");

        var response = await service.OverrideStageAsync(CreateActor(), stages[1].Id, overrideRequest, "127.0.0.1");

        Assert.Equal("IN_PROGRESS", response.Status);

        var updatedStages = await store.GetStagesAsync(cycle.Id);
        Assert.Equal(CropCycleStageStatus.NotStarted, updatedStages[0].Status); // Reconciled!
        Assert.Equal(CropCycleStageStatus.InProgress, updatedStages[1].Status);

        Assert.Single(updatedStages, s => s.Status == CropCycleStageStatus.InProgress);
        Assert.Contains(store.AuditLogs, a => a.Action == "CropCycleStage.Overridden");
    }

    [Fact]
    public async Task UpdateStagePlannedDatesAsync_UpdatesPlannedDatesWithoutChangingActualStatus()
    {
        var store = new FakeCropCycleStore();
        var crop = CreateCrop(_organizationId, "Grape");
        var plantation = CreatePlantation(store, _organizationId, crop);
        var template = CreateTemplate(store, _organizationId, crop.Id, "Grape Standard");
        AddStage(template, "Stage 1", 1, 10);

        var cycle = new CropCycle(_organizationId, plantation.Id, "2026 Cycle", 2026, null, new DateOnly(2026, 4, 1), null, _userId, template.Id);
        store.Cycles.Add(cycle);

        var service = new CropCycleService(store);
        await service.StartAsync(CreateActor(), cycle.Id, new StartCropCycleRequest(new DateOnly(2026, 4, 1)), "127.0.0.1");

        var stage = (await store.GetStagesAsync(cycle.Id))[0];
        var newPlannedStart = new DateOnly(2026, 4, 5);
        var newPlannedEnd = new DateOnly(2026, 4, 20);

        var response = await service.UpdateStagePlannedDatesAsync(CreateActor(), stage.Id, new UpdateCropCycleStagePlannedDatesRequest(newPlannedStart, newPlannedEnd), "127.0.0.1");

        Assert.Equal(newPlannedStart, response.PlannedStartDate);
        Assert.Equal(newPlannedEnd, response.PlannedEndDate);
        Assert.Equal("IN_PROGRESS", response.Status); // Status unchanged!
        Assert.Contains(store.AuditLogs, a => a.Action == "CropCycleStage.PlannedDatesUpdated");
    }

    [Fact]
    public async Task UpdateStagePlannedDatesAsync_WhenCycleNotActive_ThrowsConflictException()
    {
        var store = new FakeCropCycleStore();
        var crop = CreateCrop(_organizationId, "Grape");
        var plantation = CreatePlantation(store, _organizationId, crop);
        var template = CreateTemplate(store, _organizationId, crop.Id, "Grape Standard");
        AddStage(template, "Stage 1", 1, 10);

        var cycle = new CropCycle(_organizationId, plantation.Id, "2026 Cycle", 2026, null, new DateOnly(2026, 4, 1), null, _userId, template.Id);
        store.Cycles.Add(cycle);
        var stage = new CropCycleStage(cycle.Id, template.Stages.First().Id, "Stage 1", 1, 10, new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 10), _userId);
        cycle.Stages.Add(stage);
        store.Stages.Add(stage);

        var service = new CropCycleService(store);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.UpdateStagePlannedDatesAsync(CreateActor(), stage.Id, new UpdateCropCycleStagePlannedDatesRequest(new DateOnly(2026, 4, 5), new DateOnly(2026, 4, 15)), "127.0.0.1"));
    }

    [Fact]
    public async Task UpdateStagePlannedDatesAsync_WhenPlannedEndDateBeforePlannedStartDate_ThrowsValidationException()
    {
        var store = new FakeCropCycleStore();
        var crop = CreateCrop(_organizationId, "Grape");
        var plantation = CreatePlantation(store, _organizationId, crop);
        var template = CreateTemplate(store, _organizationId, crop.Id, "Grape Standard");
        AddStage(template, "Stage 1", 1, 10);

        var cycle = new CropCycle(_organizationId, plantation.Id, "2026 Cycle", 2026, null, new DateOnly(2026, 4, 1), null, _userId, template.Id);
        store.Cycles.Add(cycle);

        var service = new CropCycleService(store);
        await service.StartAsync(CreateActor(), cycle.Id, new StartCropCycleRequest(new DateOnly(2026, 4, 1)), "127.0.0.1");
        var stage = (await store.GetStagesAsync(cycle.Id))[0];

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpdateStagePlannedDatesAsync(CreateActor(), stage.Id, new UpdateCropCycleStagePlannedDatesRequest(new DateOnly(2026, 4, 15), new DateOnly(2026, 4, 10)), "127.0.0.1"));
    }

    [Fact]
    public async Task UpdateStagePlannedDatesAsync_WhenPlannedStartDateBeforePlantationDate_ThrowsValidationException()
    {
        var store = new FakeCropCycleStore();
        var crop = CreateCrop(_organizationId, "Grape");
        var plantation = CreatePlantation(store, _organizationId, crop); // PlantingDate = 2025-01-01
        var template = CreateTemplate(store, _organizationId, crop.Id, "Grape Standard");
        AddStage(template, "Stage 1", 1, 10);

        var cycle = new CropCycle(_organizationId, plantation.Id, "2026 Cycle", 2026, null, new DateOnly(2026, 4, 1), null, _userId, template.Id);
        store.Cycles.Add(cycle);

        var service = new CropCycleService(store);
        await service.StartAsync(CreateActor(), cycle.Id, new StartCropCycleRequest(new DateOnly(2026, 4, 1)), "127.0.0.1");
        var stage = (await store.GetStagesAsync(cycle.Id))[0];

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpdateStagePlannedDatesAsync(CreateActor(), stage.Id, new UpdateCropCycleStagePlannedDatesRequest(new DateOnly(2024, 12, 1), new DateOnly(2026, 4, 10)), "127.0.0.1"));
    }

    [Fact]
    public async Task UpdateStagePlannedDatesAsync_WhenPlannedStartDateBeforePreviousStageStartDate_ThrowsValidationException()
    {
        var store = new FakeCropCycleStore();
        var crop = CreateCrop(_organizationId, "Grape");
        var plantation = CreatePlantation(store, _organizationId, crop);
        var template = CreateTemplate(store, _organizationId, crop.Id, "Grape Standard");
        AddStage(template, "Dormancy", 1, 30);
        AddStage(template, "Pruning", 2, 15);

        var cycle = new CropCycle(_organizationId, plantation.Id, "2026 Cycle", 2026, null, new DateOnly(2026, 4, 1), null, _userId, template.Id);
        store.Cycles.Add(cycle);

        var service = new CropCycleService(store);
        await service.StartAsync(CreateActor(), cycle.Id, new StartCropCycleRequest(new DateOnly(2026, 4, 1)), "127.0.0.1");
        var stages = await store.GetStagesAsync(cycle.Id);
        var stage1 = stages[0]; // PlannedStart = 2026-04-01
        var stage2 = stages[1]; // PlannedStart = 2026-05-01

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpdateStagePlannedDatesAsync(CreateActor(), stage2.Id, new UpdateCropCycleStagePlannedDatesRequest(new DateOnly(2026, 3, 15), new DateOnly(2026, 5, 10)), "127.0.0.1"));
    }

    [Fact]
    public async Task UpdateStagePlannedDatesAsync_WhenStageCompleted_DoesNotAlterHistoricalActualDates()
    {
        var store = new FakeCropCycleStore();
        var crop = CreateCrop(_organizationId, "Grape");
        var plantation = CreatePlantation(store, _organizationId, crop);
        var template = CreateTemplate(store, _organizationId, crop.Id, "Grape Standard");
        AddStage(template, "Dormancy", 1, 30);
        AddStage(template, "Pruning", 2, 15);

        var cycle = new CropCycle(_organizationId, plantation.Id, "2026 Cycle", 2026, null, new DateOnly(2026, 4, 1), null, _userId, template.Id);
        store.Cycles.Add(cycle);

        var service = new CropCycleService(store);
        await service.StartAsync(CreateActor(), cycle.Id, new StartCropCycleRequest(new DateOnly(2026, 4, 1)), "127.0.0.1");
        var stages = await store.GetStagesAsync(cycle.Id);
        var stage1 = stages[0];

        var actualEnd = new DateOnly(2026, 4, 25);
        await service.CompleteStageAsync(CreateActor(), stage1.Id, new CompleteCropCycleStageRequest(actualEnd, "Finished"), "127.0.0.1");

        // Now edit planned dates on completed stage 1
        var newPlannedStart = new DateOnly(2026, 4, 2);
        var newPlannedEnd = new DateOnly(2026, 4, 28);
        var response = await service.UpdateStagePlannedDatesAsync(CreateActor(), stage1.Id, new UpdateCropCycleStagePlannedDatesRequest(newPlannedStart, newPlannedEnd), "127.0.0.1");

        Assert.Equal("COMPLETED", response.Status);
        Assert.Equal(new DateOnly(2026, 4, 1), response.ActualStartDate);
        Assert.Equal(actualEnd, response.ActualEndDate);
        Assert.Equal(newPlannedStart, response.PlannedStartDate);
        Assert.Equal(newPlannedEnd, response.PlannedEndDate);
    }

    [Fact]
    public async Task GetStageAsync_CrossOrganization_ThrowsResourceNotFoundException()
    {
        var store = new FakeCropCycleStore();
        var crop = CreateCrop(_organizationId, "Grape");
        var plantation = CreatePlantation(store, _organizationId, crop);
        var template = CreateTemplate(store, _organizationId, crop.Id, "Grape Standard");
        AddStage(template, "Stage 1", 1, 10);

        var cycle = new CropCycle(_organizationId, plantation.Id, "2026 Cycle", 2026, null, new DateOnly(2026, 4, 1), null, _userId, template.Id);
        store.Cycles.Add(cycle);

        var service = new CropCycleService(store);
        await service.StartAsync(CreateActor(), cycle.Id, new StartCropCycleRequest(new DateOnly(2026, 4, 1)), "127.0.0.1");

        var stage = (await store.GetStagesAsync(cycle.Id))[0];
        var otherOrgActor = CreateActor(Guid.NewGuid());

        await Assert.ThrowsAsync<ResourceNotFoundException>(() => service.GetStageAsync(otherOrgActor, stage.Id));
    }

    private static Crop CreateCrop(Guid organizationId, string name) =>
        new(organizationId, name, "CropType", "PERENNIAL");

    private static CropPlantation CreatePlantation(FakeCropCycleStore store, Guid organizationId, Crop crop)
    {
        var ownershipTypeId = Guid.NewGuid();
        var unitId = Guid.NewGuid();

        var farm = new Farm(organizationId, "Main Farm", ownershipTypeId, _userIdStatic, 100, unitId);
        var area = new FarmArea(organizationId, farm.Id, null, "North Area", 100, unitId, _userIdStatic);
        var plantation = new CropPlantation(
            organizationId, farm.Id, area.Id, crop.Id, null, null,
            "Plantation 1", 50, unitId, new DateOnly(2025, 1, 1), null, _userIdStatic);
        plantation.Activate(DateTimeOffset.UtcNow, _userIdStatic);

        typeof(CropPlantation).GetProperty("Crop")?.SetValue(plantation, crop);
        typeof(CropPlantation).GetProperty("Farm")?.SetValue(plantation, farm);
        typeof(CropPlantation).GetProperty("FarmArea")?.SetValue(plantation, area);

        store.Plantations.Add(plantation);
        return plantation;
    }

    private static readonly Guid _userIdStatic = Guid.NewGuid();

    private static CropLifecycleTemplate CreateTemplate(
        FakeCropCycleStore store, Guid organizationId, Guid cropId, string name)
    {
        var template = new CropLifecycleTemplate(organizationId, cropId, name, isDefault: true, isSystem: false, description: null, createdBy: _userIdStatic);
        store.Templates.Add(template);
        return template;
    }

    private static void AddStage(CropLifecycleTemplate template, string name, int seq, int duration)
    {
        var stage = new CropLifecycleStage(template.Id, name, seq, duration, null);
        template.Stages.Add(stage);
    }

    private sealed class FakeCropCycleStore : ICropCycleStore
    {
        public List<CropCycle> Cycles { get; } = [];
        public List<CropPlantation> Plantations { get; } = [];
        public List<CropLifecycleTemplate> Templates { get; } = [];
        public List<CropCycleStage> Stages { get; } = [];
        public List<AuditLog> AuditLogs { get; } = [];

        public Task<IReadOnlyList<CropCycle>> ListAsync(
            Guid organizationId, Guid? farmId, Guid? farmAreaId, Guid? plantationId, CropCycleStatus? status, int? seasonYear, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CropCycle>>(Cycles.Where(c => c.OrganizationId == organizationId).ToList());

        public Task<(IReadOnlyList<CropCycle> Items, int TotalCount)> ListPagedAsync(
            Guid organizationId, Guid? farmId, Guid? farmAreaId, Guid? plantationId, CropCycleStatus? status, int? seasonYear, int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<CropCycle>, int)>((Cycles.Where(c => c.OrganizationId == organizationId).ToList(), Cycles.Count));

        public Task<CropCycle?> FindAsync(Guid cycleId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Cycles.FirstOrDefault(c => c.Id == cycleId && c.OrganizationId == organizationId));

        public Task<CropCycle?> LockAsync(Guid cycleId, Guid organizationId, CancellationToken cancellationToken = default) =>
            FindAsync(cycleId, organizationId, cancellationToken);

        public Task<CropPlantation?> LockPlantationAsync(Guid plantationId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Plantations.FirstOrDefault(p => p.Id == plantationId && p.OrganizationId == organizationId));

        public Task<PlantationEndReason?> FindCancellationReasonAsync(Guid reasonId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<PlantationEndReason?>(null);

        public Task<CropLifecycleTemplate?> FindLifecycleTemplateAsync(Guid templateId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Templates.FirstOrDefault(t => t.Id == templateId && (t.OrganizationId == organizationId || (t.IsSystem && t.OrganizationId == null))));

        public Task<bool> HasActiveCycleAsync(Guid plantationId, Guid? excludingCycleId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(Cycles.Any(c => c.PlantationId == plantationId && c.Status == CropCycleStatus.Active && c.Id != excludingCycleId));

        public Task<bool> HasCycleForSeasonAsync(Guid plantationId, int seasonYear, Guid? excludingCycleId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(Cycles.Any(c => c.PlantationId == plantationId && c.SeasonYear == seasonYear && c.Status != CropCycleStatus.Cancelled && c.Id != excludingCycleId));

        public Task<bool> HasAnyCycleAsync(Guid plantationId, Guid? excludingCycleId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(Cycles.Any(c => c.PlantationId == plantationId && c.Status != CropCycleStatus.Cancelled && c.Id != excludingCycleId));

        public void Add(CropCycle cycle) => Cycles.Add(cycle);
        public void AddStage(CropCycleStage stage) => Stages.Add(stage);
        public Task<CropCycleStage?> FindStageAsync(Guid stageId, Guid organizationId, CancellationToken cancellationToken = default)
        {
            var stage = Stages.FirstOrDefault(s => s.Id == stageId);
            if (stage is null) return Task.FromResult<CropCycleStage?>(null);
            var cycle = Cycles.FirstOrDefault(c => c.Id == stage.CropCycleId);
            if (cycle is null || cycle.OrganizationId != organizationId) return Task.FromResult<CropCycleStage?>(null);
            return Task.FromResult<CropCycleStage?>(stage);
        }
        public Task<bool> HasStagesAsync(Guid cycleId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Stages.Any(s => s.CropCycleId == cycleId));
        public Task<IReadOnlyList<CropCycleStage>> GetStagesAsync(Guid cycleId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CropCycleStage>>(Stages.Where(s => s.CropCycleId == cycleId).OrderBy(s => s.SequenceNumber).ToList());

        public void AddAuditLog(AuditLog auditLog) => AuditLogs.Add(auditLog);
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) => operation(cancellationToken);
    }
}
