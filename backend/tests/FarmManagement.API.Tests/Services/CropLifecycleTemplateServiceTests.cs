using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.Crops;
using FarmManagement.Application.Interfaces.Crops;
using FarmManagement.Application.Services;
using FarmManagement.Domain.Entities;
using Xunit;

namespace FarmManagement.API.Tests.Services;

public sealed class CropLifecycleTemplateServiceTests
{
    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    private CropLifecycleTemplateActor CreateActor(bool isGlobalAdmin = false) =>
        new(_userId, _organizationId, isGlobalAdmin);

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesTemplateAndLogsAudit()
    {
        var store = new FakeCropLifecycleTemplateStore();
        var crop = CreateCrop(store, _organizationId, "Grape", isSystem: true);
        var service = new CropLifecycleTemplateService(store);

        var request = new CreateCropLifecycleTemplateRequest(
            CropId: crop.Id,
            Name: "Standard Table Grape",
            Description: "Standard 9-stage lifecycle",
            IsDefault: true);

        var response = await service.CreateAsync(CreateActor(), request, "127.0.0.1");

        Assert.NotNull(response);
        Assert.Equal("Standard Table Grape", response.Name);
        Assert.Equal(crop.Id, response.CropId);
        Assert.True(response.IsDefault);
        Assert.True(response.IsActive);
        Assert.False(response.IsSystem);
        Assert.Empty(response.Stages);

        Assert.Single(store.AuditLogs);
        Assert.Equal("CropLifecycleTemplate.Created", store.AuditLogs[0].Action);
    }

    [Fact]
    public async Task CreateAsync_WithInitialStages_CreatesTemplateAndStagesOrdered()
    {
        var store = new FakeCropLifecycleTemplateStore();
        var crop = CreateCrop(store, _organizationId, "Tomato", isSystem: false);
        var service = new CropLifecycleTemplateService(store);

        var stages = new List<CreateCropLifecycleStageRequest>
        {
            new("Nursery", 1, 15, "Initial seedling stage"),
            new("Vegetative", 2, 25, "Foliage growth"),
            new("Flowering", 3, 10, "Bloom stage")
        };

        var request = new CreateCropLifecycleTemplateRequest(
            CropId: crop.Id,
            Name: "Tomato Commercial Cycle",
            Description: "3-stage lifecycle",
            IsDefault: false,
            Stages: stages);

        var response = await service.CreateAsync(CreateActor(), request, "127.0.0.1");

        Assert.NotNull(response);
        Assert.Equal(3, response.Stages.Count);
        Assert.Equal("Nursery", response.Stages[0].StageName);
        Assert.Equal(1, response.Stages[0].SequenceNumber);
        Assert.Equal("Vegetative", response.Stages[1].StageName);
        Assert.Equal(2, response.Stages[1].SequenceNumber);
        Assert.Equal("Flowering", response.Stages[2].StageName);
        Assert.Equal(3, response.Stages[2].SequenceNumber);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateSequencesInStageList_ThrowsValidationException()
    {
        var store = new FakeCropLifecycleTemplateStore();
        var crop = CreateCrop(store, _organizationId, "Grape", isSystem: true);
        var service = new CropLifecycleTemplateService(store);

        var stages = new List<CreateCropLifecycleStageRequest>
        {
            new("Pruning", 1, 15, "Pruning"),
            new("Bud Break", 1, 10, "Bud Break duplicate seq")
        };

        var request = new CreateCropLifecycleTemplateRequest(
            CropId: crop.Id,
            Name: "Invalid Template",
            Description: null,
            IsDefault: false,
            Stages: stages);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(CreateActor(), request, "127.0.0.1"));

        Assert.NotNull(ex.Errors);
        Assert.Contains(ex.Errors.Values.SelectMany(v => v), msg => msg.Contains("Duplicate sequence number"));
    }

    [Fact]
    public async Task CreateAsync_WithIsDefault_ClearsOtherDefaultsForCrop()
    {
        var store = new FakeCropLifecycleTemplateStore();
        var crop = CreateCrop(store, _organizationId, "Corn", isSystem: false);
        var service = new CropLifecycleTemplateService(store);

        var firstRequest = new CreateCropLifecycleTemplateRequest(
            CropId: crop.Id, Name: "Template 1", Description: null, IsDefault: true);
        var firstResponse = await service.CreateAsync(CreateActor(), firstRequest, "127.0.0.1");
        Assert.True(firstResponse.IsDefault);

        var secondRequest = new CreateCropLifecycleTemplateRequest(
            CropId: crop.Id, Name: "Template 2", Description: null, IsDefault: true);
        var secondResponse = await service.CreateAsync(CreateActor(), secondRequest, "127.0.0.1");
        Assert.True(secondResponse.IsDefault);

        var updatedFirst = await service.GetAsync(CreateActor(), firstResponse.Id);
        Assert.False(updatedFirst.IsDefault);
    }

    [Fact]
    public async Task UpdateAsync_WhenInactiveAndSettingDefault_ThrowsValidationException()
    {
        var store = new FakeCropLifecycleTemplateStore();
        var crop = CreateCrop(store, _organizationId, "Apple", isSystem: false);
        var service = new CropLifecycleTemplateService(store);

        var createReq = new CreateCropLifecycleTemplateRequest(crop.Id, "Apple Life", null, IsDefault: false);
        var templateResp = await service.CreateAsync(CreateActor(), createReq, "127.0.0.1");

        await service.DeactivateAsync(CreateActor(), templateResp.Id, "127.0.0.1");

        var updateReq = new UpdateCropLifecycleTemplateRequest(crop.Id, "Apple Life", null, IsDefault: true);
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpdateAsync(CreateActor(), templateResp.Id, updateReq, "127.0.0.1"));

        Assert.True(ex.Errors?.ContainsKey("isDefault"));
    }

    [Fact]
    public async Task UpdateAsync_OnSystemTemplateByNonGlobalAdmin_ThrowsForbiddenException()
    {
        var store = new FakeCropLifecycleTemplateStore();
        var crop = CreateCrop(store, null, "Grape", isSystem: true);
        var systemTemplate = new CropLifecycleTemplate(null, crop.Id, "System Grape Standard", isDefault: true, isSystem: true);
        store.Templates.Add(systemTemplate);

        var service = new CropLifecycleTemplateService(store);

        var updateReq = new UpdateCropLifecycleTemplateRequest(crop.Id, "Hacked System Grape", null, IsDefault: false);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.UpdateAsync(CreateActor(isGlobalAdmin: false), systemTemplate.Id, updateReq, "127.0.0.1"));
    }

    [Fact]
    public async Task UpdateAsync_OnSystemTemplateByGlobalAdmin_Succeeds()
    {
        var store = new FakeCropLifecycleTemplateStore();
        var crop = CreateCrop(store, null, "Grape", isSystem: true);
        var systemTemplate = new CropLifecycleTemplate(null, crop.Id, "System Grape Standard", isDefault: true, isSystem: true);
        store.Templates.Add(systemTemplate);

        var service = new CropLifecycleTemplateService(store);

        var updateReq = new UpdateCropLifecycleTemplateRequest(crop.Id, "Updated System Grape", "New desc", IsDefault: true);
        var response = await service.UpdateAsync(CreateActor(isGlobalAdmin: true), systemTemplate.Id, updateReq, "127.0.0.1");

        Assert.Equal("Updated System Grape", response.Name);
    }

    [Fact]
    public async Task DeactivateAsync_OnDefaultTemplate_ThrowsValidationException()
    {
        var store = new FakeCropLifecycleTemplateStore();
        var crop = CreateCrop(store, _organizationId, "Grape", isSystem: true);
        var service = new CropLifecycleTemplateService(store);

        var createReq = new CreateCropLifecycleTemplateRequest(crop.Id, "Default Template", null, IsDefault: true);
        var templateResp = await service.CreateAsync(CreateActor(), createReq, "127.0.0.1");

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.DeactivateAsync(CreateActor(), templateResp.Id, "127.0.0.1"));

        Assert.True(ex.Errors?.ContainsKey("isActive"));
    }

    [Fact]
    public async Task CreateStageAsync_WithValidData_AddsStageToTemplate()
    {
        var store = new FakeCropLifecycleTemplateStore();
        var crop = CreateCrop(store, _organizationId, "Grape", isSystem: true);
        var service = new CropLifecycleTemplateService(store);

        var templateResp = await service.CreateAsync(
            CreateActor(), new CreateCropLifecycleTemplateRequest(crop.Id, "Grape Life", null, false), "127.0.0.1");

        var stageReq = new CreateCropLifecycleStageRequest("Dormancy", 1, 30, "Winter dormancy");
        var stageResp = await service.CreateStageAsync(CreateActor(), templateResp.Id, stageReq, "127.0.0.1");

        Assert.NotNull(stageResp);
        Assert.Equal("Dormancy", stageResp.StageName);
        Assert.Equal(1, stageResp.SequenceNumber);
        Assert.Equal(30, stageResp.ExpectedDurationDays);
        Assert.True(stageResp.IsActive);
    }

    [Fact]
    public async Task CreateStageAsync_WithDuplicateSequence_ThrowsConflictException()
    {
        var store = new FakeCropLifecycleTemplateStore();
        var crop = CreateCrop(store, _organizationId, "Grape", isSystem: true);
        var service = new CropLifecycleTemplateService(store);

        var templateResp = await service.CreateAsync(
            CreateActor(), new CreateCropLifecycleTemplateRequest(crop.Id, "Grape Life", null, false), "127.0.0.1");

        await service.CreateStageAsync(CreateActor(), templateResp.Id, new CreateCropLifecycleStageRequest("Dormancy", 1, 30, null), "127.0.0.1");

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateStageAsync(CreateActor(), templateResp.Id, new CreateCropLifecycleStageRequest("Pruning", 1, 15, null), "127.0.0.1"));
    }

    [Fact]
    public async Task CreateStageAsync_OnInactiveTemplate_ThrowsValidationException()
    {
        var store = new FakeCropLifecycleTemplateStore();
        var crop = CreateCrop(store, _organizationId, "Grape", isSystem: true);
        var service = new CropLifecycleTemplateService(store);

        var templateResp = await service.CreateAsync(
            CreateActor(), new CreateCropLifecycleTemplateRequest(crop.Id, "Grape Life", null, false), "127.0.0.1");

        await service.DeactivateAsync(CreateActor(), templateResp.Id, "127.0.0.1");

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateStageAsync(CreateActor(), templateResp.Id, new CreateCropLifecycleStageRequest("Dormancy", 1, 30, null), "127.0.0.1"));

        Assert.True(ex.Errors?.ContainsKey("templateId"));
    }

    [Fact]
    public async Task ListAsync_UsesQueryObjectAndEnforcesTenantIsolation()
    {
        var store = new FakeCropLifecycleTemplateStore();
        var otherOrgId = Guid.NewGuid();
        var crop = CreateCrop(store, _organizationId, "Wheat", isSystem: false);
        var otherCrop = CreateCrop(store, otherOrgId, "Rice", isSystem: false);

        var service = new CropLifecycleTemplateService(store);

        await service.CreateAsync(CreateActor(), new CreateCropLifecycleTemplateRequest(crop.Id, "My Wheat Life", null, false), "127.0.0.1");

        var otherActor = new CropLifecycleTemplateActor(Guid.NewGuid(), otherOrgId);
        await service.CreateAsync(otherActor, new CreateCropLifecycleTemplateRequest(otherCrop.Id, "Other Rice Life", null, false), "127.0.0.1");

        var query = new CropLifecycleTemplateQuery(Page: 1, PageSize: 20);
        var pagedResult = await service.ListAsync(CreateActor(), query);

        Assert.Equal(1, pagedResult.TotalCount);
        Assert.Equal("My Wheat Life", pagedResult.Items[0].Name);
    }

    [Fact]
    public async Task ReorderStagesAsync_WithValidRequest_ReordersStagesAndLogsAudit()
    {
        var store = new FakeCropLifecycleTemplateStore();
        var crop = CreateCrop(store, _organizationId, "Grape", isSystem: true);
        var service = new CropLifecycleTemplateService(store);

        var templateResp = await service.CreateAsync(
            CreateActor(),
            new CreateCropLifecycleTemplateRequest(crop.Id, "Reorder Test Life", null, false, new List<CreateCropLifecycleStageRequest>
            {
                new("Stage A", 1, 10, null),
                new("Stage B", 2, 20, null)
            }),
            "127.0.0.1");

        var stageA = templateResp.Stages.First(s => s.StageName == "Stage A");
        var stageB = templateResp.Stages.First(s => s.StageName == "Stage B");

        var reorderReq = new ReorderCropLifecycleStagesRequest(new List<ReorderCropLifecycleStageItem>
        {
            new(stageA.Id, 2),
            new(stageB.Id, 1)
        });

        var reordered = await service.ReorderStagesAsync(CreateActor(), templateResp.Id, reorderReq, "127.0.0.1");

        Assert.NotNull(reordered);
        Assert.Equal(2, reordered.Count);
        Assert.Equal("Stage B", reordered[0].StageName);
        Assert.Equal(1, reordered[0].SequenceNumber);
        Assert.Equal("Stage A", reordered[1].StageName);
        Assert.Equal(2, reordered[1].SequenceNumber);
    }


    private static Crop CreateCrop(FakeCropLifecycleTemplateStore store, Guid? organizationId, string name, bool isSystem)
    {
        var cropOrgId = isSystem ? null : organizationId;
        var crop = new Crop(cropOrgId, name, "GRAIN", "ANNUAL", isSystem: isSystem);
        store.Crops.Add(crop);
        return crop;
    }

    private sealed class FakeCropLifecycleTemplateStore : ICropLifecycleTemplateStore
    {
        public List<CropLifecycleTemplate> Templates { get; } = [];
        public List<CropLifecycleStage> Stages { get; } = [];
        public List<Crop> Crops { get; } = [];
        public List<AuditLog> AuditLogs { get; } = [];

        public Task<int> CountAsync(Guid organizationId, Guid? cropId, bool? isActive, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Filter(organizationId, cropId, isActive).Count());
        }

        public Task<IReadOnlyList<CropLifecycleTemplate>> ListAsync(
            Guid organizationId, Guid? cropId, int skip, int take, bool? isActive, CancellationToken cancellationToken = default)
        {
            var result = Filter(organizationId, cropId, isActive)
                .Skip(skip)
                .Take(take)
                .ToList();
            return Task.FromResult<IReadOnlyList<CropLifecycleTemplate>>(result);
        }

        public Task<CropLifecycleTemplate?> FindAsync(Guid templateId, Guid organizationId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Templates.FirstOrDefault(t =>
                t.Id == templateId && ((t.IsSystem && t.OrganizationId == null) || t.OrganizationId == organizationId)));
        }

        public Task<Crop?> FindCropAsync(Guid cropId, Guid organizationId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Crops.FirstOrDefault(c =>
                c.Id == cropId && ((c.IsSystem && c.OrganizationId == null) || c.OrganizationId == organizationId)));
        }

        public Task<bool> SequenceExistsAsync(Guid templateId, int sequenceNumber, Guid? excludingStageId = null, CancellationToken cancellationToken = default)
        {
            var query = Stages.Where(s => s.LifecycleTemplateId == templateId && s.SequenceNumber == sequenceNumber);
            if (excludingStageId.HasValue) query = query.Where(s => s.Id != excludingStageId.Value);
            return Task.FromResult(query.Any());
        }

        public Task ClearDefaultAsync(Guid? organizationId, Guid cropId, Guid excludingTemplateId, DateTimeOffset now, Guid updatedBy, CancellationToken cancellationToken = default)
        {
            var defaults = Templates.Where(t =>
                t.OrganizationId == organizationId &&
                t.CropId == cropId &&
                t.Id != excludingTemplateId &&
                t.IsDefault).ToList();

            foreach (var t in defaults)
            {
                t.SetDefault(false, now, updatedBy);
            }

            return Task.CompletedTask;
        }

        public void Add(CropLifecycleTemplate template) => Templates.Add(template);

        public void Add(CropLifecycleStage stage) => Stages.Add(stage);

        public void AddAuditLog(AuditLog auditLog) => AuditLogs.Add(auditLog);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        private IEnumerable<CropLifecycleTemplate> Filter(Guid organizationId, Guid? cropId, bool? isActive)
        {
            var query = Templates.Where(t =>
                (t.IsSystem && t.OrganizationId == null) || t.OrganizationId == organizationId);

            if (cropId.HasValue) query = query.Where(t => t.CropId == cropId.Value);
            if (isActive.HasValue) query = query.Where(t => t.IsActive == isActive.Value);

            return query;
        }
    }
}
