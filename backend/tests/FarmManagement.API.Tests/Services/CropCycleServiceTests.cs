using System.Reflection;
using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.CropCycles;
using FarmManagement.Application.Interfaces.CropCycles;
using FarmManagement.Application.Services;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Services;

public sealed class CropCycleServiceTests
{
    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    private CropCycleActor CreateActor() => new(_userId, _organizationId);

    [Fact]
    public async Task CreateAsync_WithValidLifecycleTemplate_CalculatesExpectedEndDateAndSetsTemplateId()
    {
        var store = new FakeCropCycleStore();
        var crop = CreateCrop(_organizationId, "Grape");
        var plantation = CreatePlantation(store, _organizationId, crop);
        var template = CreateTemplate(store, _organizationId, crop.Id, "Standard Grape Lifecycle", isDefault: true);

        // Add 2 active stages: 30 days + 15 days = 45 days
        AddStage(template, "Dormancy", 1, 30);
        AddStage(template, "Pruning", 2, 15);

        var service = new CropCycleService(store);
        var plannedStart = new DateOnly(2026, 4, 1);

        var request = new CreateCropCycleRequest(
            PlantationId: plantation.Id,
            CycleName: "2026 Grape Season",
            SeasonYear: 2026,
            SeasonName: "Spring",
            PlannedStartDate: plannedStart,
            ExpectedEndDate: null,
            LifecycleTemplateId: template.Id);

        var response = await service.CreateAsync(CreateActor(), request, "127.0.0.1");

        Assert.NotNull(response);
        Assert.Equal("2026 Grape Season", response.CycleName);
        Assert.Equal(template.Id, response.LifecycleTemplateId);
        Assert.Equal("Standard Grape Lifecycle", response.LifecycleTemplateName);
        Assert.Equal(new DateOnly(2026, 5, 16), response.ExpectedEndDate); // plannedStart + 45 days
        Assert.Equal(crop.Id, response.CropId);

        Assert.Single(store.AuditLogs);
        Assert.Equal("CropCycle.Created", store.AuditLogs[0].Action);
    }

    [Fact]
    public async Task CreateAsync_WithoutLifecycleTemplate_SucceedsAsDraft()
    {
        var store = new FakeCropCycleStore();
        var crop = CreateCrop(_organizationId, "Wheat");
        var plantation = CreatePlantation(store, _organizationId, crop);

        var service = new CropCycleService(store);
        var plannedStart = new DateOnly(2026, 6, 1);

        var request = new CreateCropCycleRequest(
            PlantationId: plantation.Id,
            CycleName: "2026 Wheat Draft",
            SeasonYear: 2026,
            SeasonName: "Kharif",
            PlannedStartDate: plannedStart,
            ExpectedEndDate: null,
            LifecycleTemplateId: null);

        var response = await service.CreateAsync(CreateActor(), request, "127.0.0.1");

        Assert.NotNull(response);
        Assert.Null(response.LifecycleTemplateId);
        Assert.Null(response.LifecycleTemplateName);
        Assert.Null(response.ExpectedEndDate);
        Assert.Equal("PLANNED", response.Status);
    }

    [Fact]
    public async Task CreateAsync_WithInactiveTemplate_ThrowsValidationException()
    {
        var store = new FakeCropCycleStore();
        var crop = CreateCrop(_organizationId, "Corn");
        var plantation = CreatePlantation(store, _organizationId, crop);
        var template = CreateTemplate(store, _organizationId, crop.Id, "Inactive Lifecycle", isActive: false);

        var service = new CropCycleService(store);

        var request = new CreateCropCycleRequest(
            PlantationId: plantation.Id,
            CycleName: "2026 Corn",
            SeasonYear: 2026,
            SeasonName: null,
            PlannedStartDate: new DateOnly(2026, 5, 1),
            ExpectedEndDate: null,
            LifecycleTemplateId: template.Id);

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(CreateActor(), request, "127.0.0.1"));
    }

    [Fact]
    public async Task CreateAsync_WithTemplateForDifferentCrop_ThrowsValidationException()
    {
        var store = new FakeCropCycleStore();
        var grapeCrop = CreateCrop(_organizationId, "Grape");
        var wheatCrop = CreateCrop(_organizationId, "Wheat");
        var plantation = CreatePlantation(store, _organizationId, grapeCrop);
        var wheatTemplate = CreateTemplate(store, _organizationId, wheatCrop.Id, "Wheat Standard");

        var service = new CropCycleService(store);

        var request = new CreateCropCycleRequest(
            PlantationId: plantation.Id,
            CycleName: "2026 Mismatched Crop Cycle",
            SeasonYear: 2026,
            SeasonName: null,
            PlannedStartDate: new DateOnly(2026, 5, 1),
            ExpectedEndDate: null,
            LifecycleTemplateId: wheatTemplate.Id);

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(CreateActor(), request, "127.0.0.1"));
    }

    [Fact]
    public async Task UpdateAsync_WhenPlanned_UpdatesLifecycleTemplate()
    {
        var store = new FakeCropCycleStore();
        var crop = CreateCrop(_organizationId, "Grape");
        var plantation = CreatePlantation(store, _organizationId, crop);
        var template1 = CreateTemplate(store, _organizationId, crop.Id, "Template 1");
        var template2 = CreateTemplate(store, _organizationId, crop.Id, "Template 2");
        AddStage(template2, "Stage A", 1, 20);

        var cycle = new CropCycle(
            _organizationId, plantation.Id, "Initial Cycle", 2026, null,
            new DateOnly(2026, 4, 1), null, _userId, template1.Id);
        store.Cycles.Add(cycle);

        var service = new CropCycleService(store);

        var updateRequest = new UpdateCropCycleRequest(
            PlantationId: plantation.Id,
            CycleName: "Updated Cycle",
            SeasonYear: 2026,
            SeasonName: null,
            PlannedStartDate: new DateOnly(2026, 4, 1),
            ExpectedEndDate: null,
            LifecycleTemplateId: template2.Id);

        var response = await service.UpdateAsync(CreateActor(), cycle.Id, updateRequest, "127.0.0.1");

        Assert.Equal(template2.Id, response.LifecycleTemplateId);
        Assert.Equal("Template 2", response.LifecycleTemplateName);
        Assert.Equal(new DateOnly(2026, 4, 21), response.ExpectedEndDate);
    }

    [Fact]
    public async Task UpdateAsync_WhenActive_ThrowsConflictException()
    {
        var store = new FakeCropCycleStore();
        var crop = CreateCrop(_organizationId, "Grape");
        var plantation = CreatePlantation(store, _organizationId, crop);
        var template = CreateTemplate(store, _organizationId, crop.Id, "Template 1");

        var cycle = new CropCycle(
            _organizationId, plantation.Id, "Started Cycle", 2026, null,
            new DateOnly(2026, 4, 1), null, _userId, template.Id);
        cycle.Start(new DateOnly(2026, 4, 1), DateTimeOffset.UtcNow, _userId);
        store.Cycles.Add(cycle);

        var service = new CropCycleService(store);

        var updateRequest = new UpdateCropCycleRequest(
            PlantationId: plantation.Id,
            CycleName: "Attempted Edit",
            SeasonYear: 2026,
            SeasonName: null,
            PlannedStartDate: new DateOnly(2026, 4, 1),
            ExpectedEndDate: null,
            LifecycleTemplateId: template.Id);

        await Assert.ThrowsAsync<ConflictException>(() => service.UpdateAsync(CreateActor(), cycle.Id, updateRequest, "127.0.0.1"));
    }

    [Fact]
    public async Task StartAsync_WithValidLifecycleTemplate_GeneratesSnapshotStagesAndSetsCycleActive()
    {
        var store = new FakeCropCycleStore();
        var crop = CreateCrop(_organizationId, "Grape");
        var plantation = CreatePlantation(store, _organizationId, crop);
        var template = CreateTemplate(store, _organizationId, crop.Id, "Standard Grape");
        AddStage(template, "Dormancy", 1, 30);
        AddStage(template, "Pruning", 2, 15);
        AddStage(template, "Bud Break", 3, 10);

        var plannedStart = new DateOnly(2026, 4, 1);
        var cycle = new CropCycle(_organizationId, plantation.Id, "2026 Grape Cycle", 2026, null, plannedStart, null, _userId, template.Id);
        store.Cycles.Add(cycle);

        var service = new CropCycleService(store);
        var startRequest = new StartCropCycleRequest(plannedStart);

        var success = await service.StartAsync(CreateActor(), cycle.Id, startRequest, "127.0.0.1");

        Assert.True(success);
        Assert.Equal(CropCycleStatus.Active, cycle.Status);
        Assert.Equal(plannedStart, cycle.ActualStartDate);

        Assert.Equal(3, store.Stages.Count);

        var stage1 = store.Stages[0];
        Assert.Equal("Dormancy", stage1.StageName);
        Assert.Equal(1, stage1.SequenceNumber);
        Assert.Equal(30, stage1.ExpectedDurationDays);
        Assert.Equal(CropCycleStageStatus.InProgress, stage1.Status);
        Assert.Equal(plannedStart, stage1.ActualStartDate);
        Assert.Null(stage1.ActualEndDate);
        Assert.Equal(new DateOnly(2026, 4, 1), stage1.PlannedStartDate);
        Assert.Equal(new DateOnly(2026, 5, 1), stage1.PlannedEndDate);

        var stage2 = store.Stages[1];
        Assert.Equal("Pruning", stage2.StageName);
        Assert.Equal(2, stage2.SequenceNumber);
        Assert.Equal(15, stage2.ExpectedDurationDays);
        Assert.Equal(CropCycleStageStatus.NotStarted, stage2.Status);
        Assert.Null(stage2.ActualStartDate);
        Assert.Null(stage2.ActualEndDate);
        Assert.Equal(new DateOnly(2026, 5, 1), stage2.PlannedStartDate);
        Assert.Equal(new DateOnly(2026, 5, 16), stage2.PlannedEndDate);

        var stage3 = store.Stages[2];
        Assert.Equal("Bud Break", stage3.StageName);
        Assert.Equal(3, stage3.SequenceNumber);
        Assert.Equal(10, stage3.ExpectedDurationDays);
        Assert.Equal(CropCycleStageStatus.NotStarted, stage3.Status);
        Assert.Null(stage3.ActualStartDate);
        Assert.Null(stage3.ActualEndDate);
        Assert.Equal(new DateOnly(2026, 5, 16), stage3.PlannedStartDate);
        Assert.Equal(new DateOnly(2026, 5, 26), stage3.PlannedEndDate);

        Assert.Equal(2, store.AuditLogs.Count);
        Assert.Contains(store.AuditLogs, a => a.Action == "CropCycleStage.Started");
        Assert.Contains(store.AuditLogs, a => a.Action == "CropCycle.Started");
    }

    [Fact]
    public async Task StartAsync_WithoutLifecycleTemplate_ThrowsValidationException()
    {
        var store = new FakeCropCycleStore();
        var crop = CreateCrop(_organizationId, "Wheat");
        var plantation = CreatePlantation(store, _organizationId, crop);
        var cycle = new CropCycle(_organizationId, plantation.Id, "Draft Cycle", 2026, null, new DateOnly(2026, 4, 1), null, _userId, lifecycleTemplateId: null);
        store.Cycles.Add(cycle);

        var service = new CropCycleService(store);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.StartAsync(CreateActor(), cycle.Id, new StartCropCycleRequest(new DateOnly(2026, 4, 1)), "127.0.0.1"));
    }

    [Fact]
    public async Task StartAsync_WithInactiveTemplate_ThrowsValidationException()
    {
        var store = new FakeCropCycleStore();
        var crop = CreateCrop(_organizationId, "Grape");
        var plantation = CreatePlantation(store, _organizationId, crop);
        var template = CreateTemplate(store, _organizationId, crop.Id, "Inactive", isActive: false);
        AddStage(template, "Dormancy", 1, 30);

        var cycle = new CropCycle(_organizationId, plantation.Id, "Cycle", 2026, null, new DateOnly(2026, 4, 1), null, _userId, template.Id);
        store.Cycles.Add(cycle);

        var service = new CropCycleService(store);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.StartAsync(CreateActor(), cycle.Id, new StartCropCycleRequest(new DateOnly(2026, 4, 1)), "127.0.0.1"));
    }

    [Fact]
    public async Task StartAsync_WithTemplateForDifferentCrop_ThrowsValidationException()
    {
        var store = new FakeCropCycleStore();
        var grapeCrop = CreateCrop(_organizationId, "Grape");
        var wheatCrop = CreateCrop(_organizationId, "Wheat");
        var plantation = CreatePlantation(store, _organizationId, grapeCrop);
        var wheatTemplate = CreateTemplate(store, _organizationId, wheatCrop.Id, "Wheat Template");
        AddStage(wheatTemplate, "Germination", 1, 10);

        var cycle = new CropCycle(_organizationId, plantation.Id, "Grape Cycle", 2026, null, new DateOnly(2026, 4, 1), null, _userId, wheatTemplate.Id);
        store.Cycles.Add(cycle);

        var service = new CropCycleService(store);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.StartAsync(CreateActor(), cycle.Id, new StartCropCycleRequest(new DateOnly(2026, 4, 1)), "127.0.0.1"));
    }

    [Fact]
    public async Task StartAsync_WhenTemplateHasNoActiveStages_ThrowsValidationException()
    {
        var store = new FakeCropCycleStore();
        var crop = CreateCrop(_organizationId, "Grape");
        var plantation = CreatePlantation(store, _organizationId, crop);
        var template = CreateTemplate(store, _organizationId, crop.Id, "Empty Stages");

        var cycle = new CropCycle(_organizationId, plantation.Id, "Cycle", 2026, null, new DateOnly(2026, 4, 1), null, _userId, template.Id);
        store.Cycles.Add(cycle);

        var service = new CropCycleService(store);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.StartAsync(CreateActor(), cycle.Id, new StartCropCycleRequest(new DateOnly(2026, 4, 1)), "127.0.0.1"));
    }

    [Fact]
    public async Task StartAsync_WhenTemplateHasDuplicateSequenceNumbers_ThrowsValidationException()
    {
        var store = new FakeCropCycleStore();
        var crop = CreateCrop(_organizationId, "Grape");
        var plantation = CreatePlantation(store, _organizationId, crop);
        var template = CreateTemplate(store, _organizationId, crop.Id, "Dup Seq");
        AddStage(template, "Stage A", 1, 10);
        AddStage(template, "Stage B", 1, 15);

        var cycle = new CropCycle(_organizationId, plantation.Id, "Cycle", 2026, null, new DateOnly(2026, 4, 1), null, _userId, template.Id);
        store.Cycles.Add(cycle);

        var service = new CropCycleService(store);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.StartAsync(CreateActor(), cycle.Id, new StartCropCycleRequest(new DateOnly(2026, 4, 1)), "127.0.0.1"));
    }

    [Fact]
    public async Task StartAsync_WhenAlreadyActive_ThrowsConflictException()
    {
        var store = new FakeCropCycleStore();
        var crop = CreateCrop(_organizationId, "Grape");
        var plantation = CreatePlantation(store, _organizationId, crop);
        var template = CreateTemplate(store, _organizationId, crop.Id, "Standard");
        AddStage(template, "Stage A", 1, 10);

        var cycle = new CropCycle(_organizationId, plantation.Id, "Active Cycle", 2026, null, new DateOnly(2026, 4, 1), null, _userId, template.Id);
        cycle.Start(new DateOnly(2026, 4, 1), DateTimeOffset.UtcNow, _userId);
        store.Cycles.Add(cycle);

        var service = new CropCycleService(store);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.StartAsync(CreateActor(), cycle.Id, new StartCropCycleRequest(new DateOnly(2026, 4, 1)), "127.0.0.1"));
    }

    [Fact]
    public async Task StartAsync_WhenAlreadyHasStages_ThrowsConflictException()
    {
        var store = new FakeCropCycleStore();
        var crop = CreateCrop(_organizationId, "Grape");
        var plantation = CreatePlantation(store, _organizationId, crop);
        var template = CreateTemplate(store, _organizationId, crop.Id, "Standard");
        var tStage = new CropLifecycleStage(template.Id, "Stage A", 1, 10);
        template.Stages.Add(tStage);

        var cycle = new CropCycle(_organizationId, plantation.Id, "Cycle", 2026, null, new DateOnly(2026, 4, 1), null, _userId, template.Id);
        store.Cycles.Add(cycle);

        var existingStage = new CropCycleStage(cycle.Id, tStage.Id, "Stage A", 1, 10, null, null, _userId);
        store.Stages.Add(existingStage);

        var service = new CropCycleService(store);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.StartAsync(CreateActor(), cycle.Id, new StartCropCycleRequest(new DateOnly(2026, 4, 1)), "127.0.0.1"));
    }

    [Fact]
    public async Task StartAsync_WhenPlantationHasAnotherActiveCycle_ThrowsConflictException()
    {
        var store = new FakeCropCycleStore();
        var crop = CreateCrop(_organizationId, "Grape");
        var plantation = CreatePlantation(store, _organizationId, crop);
        var template = CreateTemplate(store, _organizationId, crop.Id, "Standard");
        AddStage(template, "Stage A", 1, 10);

        var activeCycle = new CropCycle(_organizationId, plantation.Id, "Cycle 1", 2025, null, new DateOnly(2025, 4, 1), null, _userId, template.Id);
        activeCycle.Start(new DateOnly(2025, 4, 1), DateTimeOffset.UtcNow, _userId);
        store.Cycles.Add(activeCycle);

        var plannedCycle = new CropCycle(_organizationId, plantation.Id, "Cycle 2", 2026, null, new DateOnly(2026, 4, 1), null, _userId, template.Id);
        store.Cycles.Add(plannedCycle);

        var service = new CropCycleService(store);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.StartAsync(CreateActor(), plannedCycle.Id, new StartCropCycleRequest(new DateOnly(2026, 4, 1)), "127.0.0.1"));
    }

    [Fact]
    public async Task StartAsync_WhenStartDateBeforePlannedStart_ThrowsValidationException()
    {
        var store = new FakeCropCycleStore();
        var crop = CreateCrop(_organizationId, "Grape");
        var plantation = CreatePlantation(store, _organizationId, crop);
        var template = CreateTemplate(store, _organizationId, crop.Id, "Standard");
        AddStage(template, "Stage A", 1, 10);

        var cycle = new CropCycle(_organizationId, plantation.Id, "Cycle", 2026, null, new DateOnly(2026, 4, 1), null, _userId, template.Id);
        store.Cycles.Add(cycle);

        var service = new CropCycleService(store);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.StartAsync(CreateActor(), cycle.Id, new StartCropCycleRequest(new DateOnly(2026, 3, 25)), "127.0.0.1"));
    }

    [Fact]
    public async Task StartAsync_WithSystemTemplate_Succeeds()
    {
        var store = new FakeCropCycleStore();
        var crop = CreateCrop(_organizationId, "Grape");
        var plantation = CreatePlantation(store, _organizationId, crop);
        var systemTemplate = new CropLifecycleTemplate(null, crop.Id, "System Grape", isDefault: true, isSystem: true, description: null, createdBy: _userIdStatic);
        AddStage(systemTemplate, "Dormancy", 1, 20);
        store.Templates.Add(systemTemplate);

        var cycle = new CropCycle(_organizationId, plantation.Id, "System Cycle", 2026, null, new DateOnly(2026, 4, 1), null, _userId, systemTemplate.Id);
        store.Cycles.Add(cycle);

        var service = new CropCycleService(store);
        var success = await service.StartAsync(CreateActor(), cycle.Id, new StartCropCycleRequest(new DateOnly(2026, 4, 1)), "127.0.0.1");

        Assert.True(success);
        Assert.Equal(CropCycleStatus.Active, cycle.Status);
        Assert.Single(store.Stages);
    }

    [Fact]
    public async Task StartAsync_WithNullDurationStage_LeavesDependentPlannedDatesNull()
    {
        var store = new FakeCropCycleStore();
        var crop = CreateCrop(_organizationId, "Grape");
        var plantation = CreatePlantation(store, _organizationId, crop);
        var template = CreateTemplate(store, _organizationId, crop.Id, "Template with Null Duration");
        AddStage(template, "Stage 1", 1, 20);
        var nullDurationStage = new CropLifecycleStage(template.Id, "Stage 2", 2, null, null);
        template.Stages.Add(nullDurationStage);
        AddStage(template, "Stage 3", 3, 15);

        var plannedStart = new DateOnly(2026, 4, 1);
        var cycle = new CropCycle(_organizationId, plantation.Id, "Null Duration Cycle", 2026, null, plannedStart, null, _userId, template.Id);
        store.Cycles.Add(cycle);

        var service = new CropCycleService(store);
        var success = await service.StartAsync(CreateActor(), cycle.Id, new StartCropCycleRequest(plannedStart), "127.0.0.1");

        Assert.True(success);
        Assert.Equal(3, store.Stages.Count);

        var stage1 = store.Stages[0];
        Assert.Equal(new DateOnly(2026, 4, 1), stage1.PlannedStartDate);
        Assert.Equal(new DateOnly(2026, 4, 21), stage1.PlannedEndDate);

        var stage2 = store.Stages[1];
        Assert.Equal(new DateOnly(2026, 4, 21), stage2.PlannedStartDate);
        Assert.Null(stage2.PlannedEndDate);

        var stage3 = store.Stages[2];
        Assert.Null(stage3.PlannedStartDate);
        Assert.Null(stage3.PlannedEndDate);
    }

    [Fact]
    public async Task GetLifecycleAsync_WithStartedCycle_ReturnsSnapshottedStagesAndProgress()
    {
        var store = new FakeCropCycleStore();
        var crop = CreateCrop(_organizationId, "Grape");
        var plantation = CreatePlantation(store, _organizationId, crop);
        var template = CreateTemplate(store, _organizationId, crop.Id, "Grape Template");
        AddStage(template, "Dormancy", 1, 30);
        AddStage(template, "Pruning", 2, 15);

        var plannedStart = new DateOnly(2026, 4, 1);
        var cycle = new CropCycle(_organizationId, plantation.Id, "Started Cycle", 2026, null, plannedStart, null, _userId, template.Id);
        store.Cycles.Add(cycle);

        var service = new CropCycleService(store);
        await service.StartAsync(CreateActor(), cycle.Id, new StartCropCycleRequest(plannedStart), "127.0.0.1");

        var lifecycle = await service.GetLifecycleAsync(CreateActor(), cycle.Id);

        Assert.NotNull(lifecycle);
        Assert.Equal(cycle.Id, lifecycle.CropCycleId);
        Assert.Equal("ACTIVE", lifecycle.OverallStatus);
        Assert.True(lifecycle.HasGeneratedStages);
        Assert.Equal("Dormancy", lifecycle.CurrentStageName);
        Assert.Equal(1, lifecycle.CurrentStageSequence);
        Assert.Equal(2, lifecycle.TotalStagesCount);
        Assert.Equal(0, lifecycle.CompletedStagesCount);
        Assert.Equal(0, lifecycle.ProgressPercentage);
        Assert.Equal(2, lifecycle.Stages.Count);
        Assert.Equal("IN_PROGRESS", lifecycle.Stages[0].Status);
        Assert.Equal("NOT_STARTED", lifecycle.Stages[1].Status);
    }

    [Fact]
    public async Task GetLifecycleAsync_WithPlannedCycleAndTemplate_ReturnsProjectedStages()
    {
        var store = new FakeCropCycleStore();
        var crop = CreateCrop(_organizationId, "Grape");
        var plantation = CreatePlantation(store, _organizationId, crop);
        var template = CreateTemplate(store, _organizationId, crop.Id, "Grape Template");
        AddStage(template, "Dormancy", 1, 30);
        AddStage(template, "Pruning", 2, 15);

        var plannedStart = new DateOnly(2026, 4, 1);
        var cycle = new CropCycle(_organizationId, plantation.Id, "Planned Cycle", 2026, null, plannedStart, null, _userId, template.Id);
        store.Cycles.Add(cycle);

        var service = new CropCycleService(store);

        var lifecycle = await service.GetLifecycleAsync(CreateActor(), cycle.Id);

        Assert.NotNull(lifecycle);
        Assert.Equal(cycle.Id, lifecycle.CropCycleId);
        Assert.Equal("PLANNED", lifecycle.OverallStatus);
        Assert.False(lifecycle.HasGeneratedStages);
        Assert.Null(lifecycle.CurrentStageName);
        Assert.Equal(2, lifecycle.TotalStagesCount);
        Assert.Equal(0, lifecycle.CompletedStagesCount);
        Assert.Equal(0, lifecycle.ProgressPercentage);
        Assert.Equal(2, lifecycle.Stages.Count);
        Assert.Equal(plannedStart, lifecycle.Stages[0].PlannedStartDate);
        Assert.Equal(new DateOnly(2026, 5, 1), lifecycle.Stages[0].PlannedEndDate);
        Assert.Equal("NOT_STARTED", lifecycle.Stages[0].Status);
    }

    [Fact]
    public async Task GetLifecycleAsync_WithoutTemplate_ReturnsEmptyLifecycle()
    {
        var store = new FakeCropCycleStore();
        var crop = CreateCrop(_organizationId, "Wheat");
        var plantation = CreatePlantation(store, _organizationId, crop);
        var cycle = new CropCycle(_organizationId, plantation.Id, "Draft Cycle", 2026, null, new DateOnly(2026, 4, 1), null, _userId, lifecycleTemplateId: null);
        store.Cycles.Add(cycle);

        var service = new CropCycleService(store);

        var lifecycle = await service.GetLifecycleAsync(CreateActor(), cycle.Id);

        Assert.NotNull(lifecycle);
        Assert.Equal(cycle.Id, lifecycle.CropCycleId);
        Assert.Equal("PLANNED", lifecycle.OverallStatus);
        Assert.False(lifecycle.HasGeneratedStages);
        Assert.Null(lifecycle.LifecycleTemplateId);
        Assert.Equal(0, lifecycle.TotalStagesCount);
        Assert.Empty(lifecycle.Stages);
    }

    [Fact]
    public async Task CompleteAsync_WhenCropIsPerennial_DoesNotRequireTerminationPrompt()
    {
        var store = new FakeCropCycleStore();
        var crop = new Crop(_organizationId, "Grape", "Fruit", "PERENNIAL");
        var plantation = CreatePlantation(store, _organizationId, crop);
        var cycle = new CropCycle(_organizationId, plantation.Id, "Perennial Cycle", 2026, null, new DateOnly(2026, 4, 1), null, _userId, null);
        cycle.Start(new DateOnly(2026, 4, 1), DateTimeOffset.UtcNow, _userId);
        cycle.Harvest(new DateOnly(2026, 8, 1), DateTimeOffset.UtcNow, _userId);
        store.Cycles.Add(cycle);

        var service = new CropCycleService(store);
        var response = await service.CompleteAsync(CreateActor(), cycle.Id, new CompleteCropCycleRequest(new DateOnly(2026, 8, 5)), "127.0.0.1");

        Assert.NotNull(response);
        Assert.Equal(cycle.Id, response.CycleId);
        Assert.Equal("PERENNIAL", response.CropDurationType);
        Assert.False(response.RequiresPlantationTerminationPrompt);
        Assert.Equal(PlantationStatus.Active, plantation.Status);
    }

    [Fact]
    public async Task CompleteAsync_WhenCropIsNonPerennial_RequiresTerminationPrompt()
    {
        var store = new FakeCropCycleStore();
        var crop = new Crop(_organizationId, "Tomato", "Vegetable", "ANNUAL");
        var plantation = CreatePlantation(store, _organizationId, crop);
        var cycle = new CropCycle(_organizationId, plantation.Id, "Annual Cycle", 2026, null, new DateOnly(2026, 4, 1), null, _userId, null);
        cycle.Start(new DateOnly(2026, 4, 1), DateTimeOffset.UtcNow, _userId);
        cycle.Harvest(new DateOnly(2026, 8, 1), DateTimeOffset.UtcNow, _userId);
        store.Cycles.Add(cycle);

        var service = new CropCycleService(store);
        var response = await service.CompleteAsync(CreateActor(), cycle.Id, new CompleteCropCycleRequest(new DateOnly(2026, 8, 5)), "127.0.0.1");

        Assert.NotNull(response);
        Assert.Equal(cycle.Id, response.CycleId);
        Assert.Equal("ANNUAL", response.CropDurationType);
        Assert.True(response.RequiresPlantationTerminationPrompt);
        Assert.Equal(PlantationStatus.Active, plantation.Status);
    }

    [Fact]
    public async Task CreateAsync_WhenCropIsNonPerennialAndPlantationAlreadyHasCycle_ThrowsConflictException()
    {
        var store = new FakeCropCycleStore();
        var crop = new Crop(_organizationId, "Chili", "Vegetable", "SEASONAL");
        var plantation = CreatePlantation(store, _organizationId, crop);
        var existingCycle = new CropCycle(_organizationId, plantation.Id, "First Cycle", 2025, null, new DateOnly(2025, 4, 1), null, _userId, null);
        store.Cycles.Add(existingCycle);

        var service = new CropCycleService(store);
        var request = new CreateCropCycleRequest(plantation.Id, "Second Cycle", 2026, null, new DateOnly(2026, 4, 1), null, null);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(CreateActor(), request, "127.0.0.1"));
        Assert.Contains("Non-perennial plantations cannot be reused", ex.Message);
    }

    [Fact]
    public async Task LifecycleSnapshot_FullScenario_TemplateModified_ExistingStagesRemainUnchanged_AndNewCycleReceivesNewValues()
    {
        var store = new FakeCropCycleStore();
        var crop = CreateCrop(_organizationId, "Tomato");
        var plantation1 = CreatePlantation(store, _organizationId, crop);
        var templateA = CreateTemplate(store, _organizationId, crop.Id, "Tomato Standard Template");
        AddStage(templateA, "Nursery", 1, 15);
        AddStage(templateA, "Transplanting", 2, 20);

        var service = new CropCycleService(store);

        // 1. Start Crop Cycle 1 using Template A
        var plannedStart1 = new DateOnly(2026, 4, 1);
        var cycle1 = new CropCycle(_organizationId, plantation1.Id, "2026 Tomato Cycle 1", 2026, "Season 1", plannedStart1, null, _userId, templateA.Id);
        store.Cycles.Add(cycle1);

        var startResult1 = await service.StartAsync(CreateActor(), cycle1.Id, new StartCropCycleRequest(plannedStart1), "127.0.0.1");
        Assert.True(startResult1);

        // 2. Confirm crop_cycle_stages were generated
        var lifecycle1Before = await service.GetLifecycleAsync(CreateActor(), cycle1.Id);
        Assert.True(lifecycle1Before.HasGeneratedStages);
        Assert.Equal(2, lifecycle1Before.TotalStagesCount);
        Assert.Equal("Nursery", lifecycle1Before.Stages[0].StageName);
        Assert.Equal(15, lifecycle1Before.Stages[0].ExpectedDurationDays);
        Assert.Equal(plannedStart1, lifecycle1Before.Stages[0].PlannedStartDate);
        Assert.Equal(new DateOnly(2026, 4, 16), lifecycle1Before.Stages[0].PlannedEndDate);
        Assert.Equal("Transplanting", lifecycle1Before.Stages[1].StageName);
        Assert.Equal(20, lifecycle1Before.Stages[1].ExpectedDurationDays);
        Assert.Equal(new DateOnly(2026, 4, 16), lifecycle1Before.Stages[1].PlannedStartDate);
        Assert.Equal(new DateOnly(2026, 5, 6), lifecycle1Before.Stages[1].PlannedEndDate);

        // 3. Modify Template A stage name and duration (and add a new stage)
        var templateStage1 = templateA.Stages.First(s => s.SequenceNumber == 1);
        templateStage1.Update("Early Nursery Phase", 1, 10, "Updated description");
        var templateStage2 = templateA.Stages.First(s => s.SequenceNumber == 2);
        templateStage2.Update("Field Transplanting", 2, 25, "Extended duration");
        AddStage(templateA, "Harvesting", 3, 30);

        // 4. Reload the existing Crop Cycle
        var lifecycle1After = await service.GetLifecycleAsync(CreateActor(), cycle1.Id);

        // 5. Confirm its crop_cycle_stages did NOT change
        Assert.True(lifecycle1After.HasGeneratedStages);
        Assert.Equal(2, lifecycle1After.TotalStagesCount);
        Assert.Equal("Nursery", lifecycle1After.Stages[0].StageName);
        Assert.Equal(15, lifecycle1After.Stages[0].ExpectedDurationDays);
        Assert.Equal(new DateOnly(2026, 4, 16), lifecycle1After.Stages[0].PlannedEndDate);
        Assert.Equal("Transplanting", lifecycle1After.Stages[1].StageName);
        Assert.Equal(20, lifecycle1After.Stages[1].ExpectedDurationDays);
        Assert.Equal(new DateOnly(2026, 5, 6), lifecycle1After.Stages[1].PlannedEndDate);

        // 6. Create/start another Crop Cycle using the modified template
        var plantation2 = CreatePlantation(store, _organizationId, crop);
        var plannedStart2 = new DateOnly(2026, 6, 1);
        var createRequest2 = new CreateCropCycleRequest(
            PlantationId: plantation2.Id,
            CycleName: "2026 Tomato Cycle 2",
            SeasonYear: 2026,
            SeasonName: "Season 2",
            PlannedStartDate: plannedStart2,
            ExpectedEndDate: null,
            LifecycleTemplateId: templateA.Id);

        var cycle2Response = await service.CreateAsync(CreateActor(), createRequest2, "127.0.0.1");
        // Total duration is now 10 + 25 + 30 = 65 days
        Assert.Equal(plannedStart2.AddDays(65), cycle2Response.ExpectedEndDate);

        var startResult2 = await service.StartAsync(CreateActor(), cycle2Response.Id, new StartCropCycleRequest(plannedStart2), "127.0.0.1");
        Assert.True(startResult2);

        // 7. Confirm the new cycle receives the new template values
        var lifecycle2 = await service.GetLifecycleAsync(CreateActor(), cycle2Response.Id);
        Assert.True(lifecycle2.HasGeneratedStages);
        Assert.Equal(3, lifecycle2.TotalStagesCount);
        Assert.Equal("Early Nursery Phase", lifecycle2.Stages[0].StageName);
        Assert.Equal(10, lifecycle2.Stages[0].ExpectedDurationDays);
        Assert.Equal(plannedStart2, lifecycle2.Stages[0].PlannedStartDate);
        Assert.Equal(new DateOnly(2026, 6, 11), lifecycle2.Stages[0].PlannedEndDate);

        Assert.Equal("Field Transplanting", lifecycle2.Stages[1].StageName);
        Assert.Equal(25, lifecycle2.Stages[1].ExpectedDurationDays);
        Assert.Equal(new DateOnly(2026, 6, 11), lifecycle2.Stages[1].PlannedStartDate);
        Assert.Equal(new DateOnly(2026, 7, 6), lifecycle2.Stages[1].PlannedEndDate);

        Assert.Equal("Harvesting", lifecycle2.Stages[2].StageName);
        Assert.Equal(30, lifecycle2.Stages[2].ExpectedDurationDays);
        Assert.Equal(new DateOnly(2026, 7, 6), lifecycle2.Stages[2].PlannedStartDate);
        Assert.Equal(new DateOnly(2026, 8, 5), lifecycle2.Stages[2].PlannedEndDate);
    }

    [Fact]
    public async Task StartedCropCycle_CannotChangeLifecycleTemplateId_ThrowsConflictException()
    {
        var store = new FakeCropCycleStore();
        var crop = CreateCrop(_organizationId, "Grape");
        var plantation = CreatePlantation(store, _organizationId, crop);
        var templateA = CreateTemplate(store, _organizationId, crop.Id, "Template A");
        AddStage(templateA, "Stage 1", 1, 10);
        var templateB = CreateTemplate(store, _organizationId, crop.Id, "Template B");
        AddStage(templateB, "Stage 1B", 1, 20);

        var cycle = new CropCycle(_organizationId, plantation.Id, "Cycle", 2026, null, new DateOnly(2026, 4, 1), null, _userId, templateA.Id);
        store.Cycles.Add(cycle);

        var service = new CropCycleService(store);
        await service.StartAsync(CreateActor(), cycle.Id, new StartCropCycleRequest(new DateOnly(2026, 4, 1)), "127.0.0.1");

        // Cycle is now ACTIVE
        Assert.Equal(CropCycleStatus.Active, cycle.Status);

        var updateRequest = new UpdateCropCycleRequest(
            PlantationId: plantation.Id,
            CycleName: "Attempted Update",
            SeasonYear: 2026,
            SeasonName: null,
            PlannedStartDate: new DateOnly(2026, 4, 1),
            ExpectedEndDate: null,
            LifecycleTemplateId: templateB.Id);

        // Application service check
        var ex = await Assert.ThrowsAsync<ConflictException>(() => service.UpdateAsync(CreateActor(), cycle.Id, updateRequest, "127.0.0.1"));
        Assert.Contains("Only a planned crop cycle can be modified", ex.Message);

        // Domain entity direct checks
        Assert.Throws<InvalidOperationException>(() =>
            cycle.SetLifecycleTemplate(templateB.Id, DateTimeOffset.UtcNow, _userId));

        Assert.Throws<InvalidOperationException>(() =>
            cycle.Update(plantation.Id, "Name", 2026, null, new DateOnly(2026, 4, 1), null, DateTimeOffset.UtcNow, _userId, templateB.Id));
    }

    [Fact]
    public async Task DraftCropCycle_CanChangeLifecycleTemplateId_SuccessfullyUpdatesTemplateAndRecalculatesExpectedEndDate()
    {
        var store = new FakeCropCycleStore();
        var crop = CreateCrop(_organizationId, "Grape");
        var plantation = CreatePlantation(store, _organizationId, crop);
        var templateA = CreateTemplate(store, _organizationId, crop.Id, "Template A");
        AddStage(templateA, "Stage A", 1, 15);
        var templateB = CreateTemplate(store, _organizationId, crop.Id, "Template B");
        AddStage(templateB, "Stage B1", 1, 20);
        AddStage(templateB, "Stage B2", 2, 25); // total 45 days

        var plannedStart = new DateOnly(2026, 4, 1);
        var cycle = new CropCycle(_organizationId, plantation.Id, "Draft Cycle", 2026, null, plannedStart, null, _userId, templateA.Id);
        store.Cycles.Add(cycle);

        var service = new CropCycleService(store);

        // 1. Change from Template A to Template B
        var updateRequest = new UpdateCropCycleRequest(
            PlantationId: plantation.Id,
            CycleName: "Draft Cycle Updated",
            SeasonYear: 2026,
            SeasonName: null,
            PlannedStartDate: plannedStart,
            ExpectedEndDate: null,
            LifecycleTemplateId: templateB.Id);

        var response = await service.UpdateAsync(CreateActor(), cycle.Id, updateRequest, "127.0.0.1");

        Assert.Equal(templateB.Id, response.LifecycleTemplateId);
        Assert.Equal("Template B", response.LifecycleTemplateName);
        Assert.Equal(plannedStart.AddDays(45), response.ExpectedEndDate);
        Assert.Equal(templateB.Id, cycle.LifecycleTemplateId);

        // Also verify projected lifecycle reflects Template B
        var lifecycle = await service.GetLifecycleAsync(CreateActor(), cycle.Id);
        Assert.False(lifecycle.HasGeneratedStages);
        Assert.Equal(templateB.Id, lifecycle.LifecycleTemplateId);
        Assert.Equal(2, lifecycle.TotalStagesCount);
        Assert.Equal("Stage B1", lifecycle.Stages[0].StageName);
        Assert.Equal("Stage B2", lifecycle.Stages[1].StageName);

        // 2. Can also clear template on draft cycle when ClearLifecycleTemplate is true
        var clearTemplateRequest = new UpdateCropCycleRequest(
            PlantationId: plantation.Id,
            CycleName: "Draft Cycle Without Template",
            SeasonYear: 2026,
            SeasonName: null,
            PlannedStartDate: plannedStart,
            ExpectedEndDate: null,
            LifecycleTemplateId: null,
            ClearLifecycleTemplate: true);

        var clearResponse = await service.UpdateAsync(CreateActor(), cycle.Id, clearTemplateRequest, "127.0.0.1");
        Assert.Null(clearResponse.LifecycleTemplateId);
        Assert.Null(cycle.LifecycleTemplateId);

        // Domain entity SetLifecycleTemplate also works directly in Planned status
        cycle.SetLifecycleTemplate(templateA.Id, DateTimeOffset.UtcNow, _userId);
        Assert.Equal(templateA.Id, cycle.LifecycleTemplateId);
    }

    [Fact]
    public async Task UpdateAsync_PartialUpdateWithoutLifecycleTemplate_PreservesExistingLifecycleTemplate()
    {
        var store = new FakeCropCycleStore();
        var crop = CreateCrop(_organizationId, "Grape");
        var plantation = CreatePlantation(store, _organizationId, crop);
        var template = CreateTemplate(store, _organizationId, crop.Id, "Template 1");
        AddStage(template, "Stage A", 1, 20);

        var plannedStart = new DateOnly(2026, 4, 1);
        var cycle = new CropCycle(
            _organizationId, plantation.Id, "Initial Cycle", 2026, null,
            plannedStart, plannedStart.AddDays(20), _userId, template.Id);
        store.Cycles.Add(cycle);

        var service = new CropCycleService(store);

        // Partial update: updating cycleName and seasonName, leaving LifecycleTemplateId omitted/null, ClearLifecycleTemplate: false
        var updateRequest = new UpdateCropCycleRequest(
            PlantationId: plantation.Id,
            CycleName: "Renamed Cycle",
            SeasonYear: 2026,
            SeasonName: "Spring Season",
            PlannedStartDate: plannedStart,
            ExpectedEndDate: null,
            LifecycleTemplateId: null,
            ClearLifecycleTemplate: false);

        var response = await service.UpdateAsync(CreateActor(), cycle.Id, updateRequest, "127.0.0.1");

        // Verifies existing template and duration were PRESERVED, not accidentally detached!
        Assert.Equal(template.Id, response.LifecycleTemplateId);
        Assert.Equal("Template 1", response.LifecycleTemplateName);
        Assert.Equal(plannedStart.AddDays(20), response.ExpectedEndDate);
        Assert.Equal(template.Id, cycle.LifecycleTemplateId);
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
        FakeCropCycleStore store, Guid organizationId, Guid cropId, string name, bool isDefault = false, bool isActive = true)
    {
        var template = new CropLifecycleTemplate(organizationId, cropId, name, isDefault, isSystem: false, description: null, createdBy: _userIdStatic);
        if (!isActive)
        {
            template.Deactivate(DateTimeOffset.UtcNow, _userIdStatic);
        }
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
