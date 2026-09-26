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

        public void Add(CropCycle cycle) => Cycles.Add(cycle);
        public void AddAuditLog(AuditLog auditLog) => AuditLogs.Add(auditLog);
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) => operation(cancellationToken);
    }
}
