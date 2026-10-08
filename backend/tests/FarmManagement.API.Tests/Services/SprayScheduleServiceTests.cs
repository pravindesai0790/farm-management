using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.Sprays;
using FarmManagement.Application.Interfaces.Sprays;
using FarmManagement.Application.Services;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Services;

public class SprayScheduleServiceTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly FakeScheduleSprayStore _store = new();
    private readonly SprayService _sut;

    public SprayScheduleServiceTests()
    {
        _sut = new SprayService(_store);
    }

    private SprayActor CreateActor() => new(_userId, _orgId);

    private Farm CreateActiveFarm()
    {
        var farm = new Farm(_orgId, "Highland Orchards", Guid.NewGuid(), Guid.NewGuid(), 100m, Guid.NewGuid());
        _store.Farms.Add(farm);
        return farm;
    }

    [Fact]
    public async Task ScheduleAsync_ValidDraft_TransitionsToScheduledAndLogsAudit()
    {
        // Arrange
        var farm = CreateActiveFarm();
        var spray = new Spray(
            _orgId,
            farm.Id,
            _userId,
            status: SprayStatus.Draft,
            plannedDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)));
        _store.Sprays.Add(spray);

        var scheduledTime = DateTimeOffset.UtcNow.AddDays(2).AddHours(8);
        var request = new ScheduleSprayRequest(scheduledTime);

        // Act
        var result = await _sut.ScheduleAsync(CreateActor(), spray.Id, request, "127.0.0.1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(SprayStatus.Scheduled, result.Status);
        Assert.Equal(scheduledTime, result.ScheduledDateTime);
        Assert.Equal(spray.PlannedDate, result.PlannedDate);

        var audit = Assert.Single(_store.AuditLogs);
        Assert.Equal("Spray.Scheduled", audit.Action);
    }

    [Fact]
    public async Task ScheduleAsync_WithExplicitPlannedDate_OverridesDraftPlannedDate()
    {
        // Arrange
        var farm = CreateActiveFarm();
        var spray = new Spray(
            _orgId,
            farm.Id,
            _userId,
            status: SprayStatus.Draft,
            plannedDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)));
        _store.Sprays.Add(spray);

        var newPlannedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(4));
        var scheduledTime = DateTimeOffset.UtcNow.AddDays(4).AddHours(9);
        var request = new ScheduleSprayRequest(scheduledTime, newPlannedDate);

        // Act
        var result = await _sut.ScheduleAsync(CreateActor(), spray.Id, request, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(SprayStatus.Scheduled, result.Status);
        Assert.Equal(newPlannedDate, result.PlannedDate);
        Assert.Equal(scheduledTime, result.ScheduledDateTime);
    }

    [Fact]
    public async Task ScheduleAsync_WhenSprayNotFound_ThrowsResourceNotFoundException()
    {
        var request = new ScheduleSprayRequest(DateTimeOffset.UtcNow.AddDays(1));

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _sut.ScheduleAsync(CreateActor(), Guid.NewGuid(), request, null));
    }

    [Fact]
    public async Task ScheduleAsync_WhenSprayNotDraft_ThrowsConflictException()
    {
        // Arrange
        var farm = CreateActiveFarm();
        var spray = new Spray(
            _orgId,
            farm.Id,
            _userId,
            status: SprayStatus.Scheduled,
            scheduledDateTime: DateTimeOffset.UtcNow.AddDays(1));
        _store.Sprays.Add(spray);

        var request = new ScheduleSprayRequest(DateTimeOffset.UtcNow.AddDays(2));

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.ScheduleAsync(CreateActor(), spray.Id, request, null));
    }

    [Fact]
    public async Task ScheduleAsync_WhenPlannedDateMissingFromBothRequestAndDraft_ThrowsValidationException()
    {
        // Arrange
        var farm = CreateActiveFarm();
        var spray = new Spray(
            _orgId,
            farm.Id,
            _userId,
            status: SprayStatus.Draft,
            plannedDate: null);
        _store.Sprays.Add(spray);

        var request = new ScheduleSprayRequest(DateTimeOffset.UtcNow.AddDays(1), PlannedDate: null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.ScheduleAsync(CreateActor(), spray.Id, request, null));
        Assert.True(ex.Errors?.ContainsKey("plannedDate"));
    }

    [Fact]
    public async Task RescheduleAsync_ValidScheduledSpray_UpdatesScheduledDateTimeAndAudits()
    {
        // Arrange
        var farm = CreateActiveFarm();
        var initialScheduledTime = DateTimeOffset.UtcNow.AddDays(2);
        var spray = new Spray(
            _orgId,
            farm.Id,
            _userId,
            status: SprayStatus.Scheduled,
            scheduledDateTime: initialScheduledTime);
        _store.Sprays.Add(spray);

        var newScheduledTime = DateTimeOffset.UtcNow.AddDays(5);
        var request = new RescheduleSprayRequest(newScheduledTime);

        // Act
        var result = await _sut.RescheduleAsync(CreateActor(), spray.Id, request, "192.168.1.1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(SprayStatus.Scheduled, result.Status);
        Assert.Equal(newScheduledTime, result.ScheduledDateTime);

        var audit = Assert.Single(_store.AuditLogs);
        Assert.Equal("Spray.Rescheduled", audit.Action);
    }

    [Fact]
    public async Task RescheduleAsync_PastScheduledDateTime_AllowedAndProducesOverdueFlag()
    {
        // Arrange
        var farm = CreateActiveFarm();
        var spray = new Spray(
            _orgId,
            farm.Id,
            _userId,
            status: SprayStatus.Scheduled,
            scheduledDateTime: DateTimeOffset.UtcNow.AddDays(1));
        _store.Sprays.Add(spray);

        var pastTime = DateTimeOffset.UtcNow.AddHours(-4);
        var request = new RescheduleSprayRequest(pastTime);

        // Act
        var result = await _sut.RescheduleAsync(CreateActor(), spray.Id, request, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(SprayStatus.Scheduled, result.Status);
        Assert.Equal(pastTime, result.ScheduledDateTime);
        Assert.True(result.IsOverdue);
    }

    [Fact]
    public async Task RescheduleAsync_WhenSprayNotFound_ThrowsResourceNotFoundException()
    {
        var request = new RescheduleSprayRequest(DateTimeOffset.UtcNow.AddDays(2));

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _sut.RescheduleAsync(CreateActor(), Guid.NewGuid(), request, null));
    }

    [Fact]
    public async Task RescheduleAsync_WhenSprayNotScheduled_ThrowsConflictException()
    {
        // Arrange
        var farm = CreateActiveFarm();
        var spray = new Spray(
            _orgId,
            farm.Id,
            _userId,
            status: SprayStatus.Draft);
        _store.Sprays.Add(spray);

        var request = new RescheduleSprayRequest(DateTimeOffset.UtcNow.AddDays(2));

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.RescheduleAsync(CreateActor(), spray.Id, request, null));
    }

    private sealed class FakeScheduleSprayStore : ISprayStore
    {
        public List<Spray> Sprays { get; } = [];
        public List<Farm> Farms { get; } = [];
        public List<FarmArea> FarmAreas { get; } = [];
        public List<CropPlantation> Plantations { get; } = [];
        public List<CropCycle> CropCycles { get; } = [];
        public List<CropCycleStage> CropCycleStages { get; } = [];
        public List<Target> Targets { get; } = [];
        public List<ApplicationMethod> ApplicationMethods { get; } = [];
        public List<Unit> Units { get; } = [];
        public List<InventoryItem> InventoryItems { get; } = [];
        public List<AuditLog> AuditLogs { get; } = [];

        public Task<Spray?> FindAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default)
        {
            var spray = Sprays.FirstOrDefault(s => s.Id == id && s.OrganizationId == organizationId);
            return Task.FromResult(spray);
        }

        public Task<int> CountAsync(Guid organizationId, SprayListQuery query, DateTimeOffset now, CancellationToken cancellationToken = default) =>
            Task.FromResult(Sprays.Count(s => s.OrganizationId == organizationId));

        public Task<IReadOnlyList<Spray>> ListAsync(Guid organizationId, SprayListQuery query, int skip, int take, DateTimeOffset now, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Spray>>(Sprays.Where(s => s.OrganizationId == organizationId).Skip(skip).Take(take).ToList());

        public void Add(Spray spray) => Sprays.Add(spray);

        public void AddAuditLog(AuditLog auditLog) => AuditLogs.Add(auditLog);

        public Task<Farm?> FindFarmAsync(Guid farmId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Farms.FirstOrDefault(f => f.Id == farmId && f.OrganizationId == organizationId));

        public Task<FarmArea?> FindFarmAreaAsync(Guid farmAreaId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(FarmAreas.FirstOrDefault(fa => fa.Id == farmAreaId && fa.OrganizationId == organizationId));

        public Task<CropPlantation?> FindPlantationAsync(Guid plantationId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Plantations.FirstOrDefault(p => p.Id == plantationId && p.OrganizationId == organizationId));

        public Task<CropCycle?> FindCropCycleAsync(Guid cropCycleId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(CropCycles.FirstOrDefault(c => c.Id == cropCycleId && c.OrganizationId == organizationId));

        public Task<CropCycleStage?> FindCropCycleStageAsync(Guid cropCycleStageId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(CropCycleStages.FirstOrDefault(s => s.Id == cropCycleStageId));

        public Task<Target?> FindTargetAsync(Guid targetId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Targets.FirstOrDefault(t => t.Id == targetId && ((t.IsSystem && t.OrganizationId == null) || t.OrganizationId == organizationId)));

        public Task<ApplicationMethod?> FindApplicationMethodAsync(Guid applicationMethodId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ApplicationMethods.FirstOrDefault(am => am.Id == applicationMethodId && ((am.IsSystem && am.OrganizationId == null) || am.OrganizationId == organizationId)));

        public Task<Unit?> FindUnitAsync(Guid unitId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Units.FirstOrDefault(u => u.Id == unitId && ((u.IsSystem && u.OrganizationId == null) || u.OrganizationId == organizationId)));

        public Task<InventoryItem?> FindInventoryItemAsync(Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(InventoryItems.FirstOrDefault(i => i.Id == inventoryItemId && i.OrganizationId == organizationId));

        public Task<bool> HasActivePlantProtectionProfileAsync(Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public void RemoveSprayProduct(SprayProduct product) { }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
