using System.Reflection;
using FarmManagement.API.Controllers;
using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.Irrigation;
using FarmManagement.Application.Interfaces.Irrigation;
using FarmManagement.Application.Services;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace FarmManagement.API.Tests.Authorization;

public class IrrigationAuthorizationSecurityTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _otherOrgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly FakeSecurityIrrigationStore _irrigationStore = new();
    private readonly IrrigationService _irrigationService;

    public IrrigationAuthorizationSecurityTests()
    {
        _irrigationService = new IrrigationService(_irrigationStore);
    }

    private IrrigationActor CreateActor() => new(_userId, _orgId);

    #region 1. Endpoint-Level Authorization Policy Tests (Reflection)

    [Fact]
    public void IrrigationsController_HasAuthorizeAttribute_AndActionsHaveCorrectPolicies()
    {
        var controllerType = typeof(IrrigationsController);
        Assert.NotNull(controllerType.GetCustomAttribute<AuthorizeAttribute>());

        AssertActionPolicy(controllerType, nameof(IrrigationsController.List), "Permission:Irrigation.View");
        AssertActionPolicy(controllerType, nameof(IrrigationsController.Summary), "Permission:Irrigation.View");
        AssertActionPolicy(controllerType, nameof(IrrigationsController.ListMethods), "Permission:IrrigationMethod.View");
        AssertActionPolicy(controllerType, nameof(IrrigationsController.Get), "Permission:Irrigation.View");
        AssertActionPolicy(controllerType, nameof(IrrigationsController.Create), "Permission:Irrigation.Create");
        AssertActionPolicy(controllerType, nameof(IrrigationsController.Update), "Permission:Irrigation.Update");
        AssertActionPolicy(controllerType, nameof(IrrigationsController.Schedule), "Permission:Irrigation.Schedule");
        AssertActionPolicy(controllerType, nameof(IrrigationsController.Reschedule), "Permission:Irrigation.Schedule");
        AssertActionPolicy(controllerType, nameof(IrrigationsController.Start), "Permission:Irrigation.Start");
        AssertActionPolicy(controllerType, nameof(IrrigationsController.Complete), "Permission:Irrigation.Complete");
        AssertActionPolicy(controllerType, nameof(IrrigationsController.RecordCompleted), "Permission:Irrigation.Complete");
        AssertActionPolicy(controllerType, nameof(IrrigationsController.Cancel), "Permission:Irrigation.Cancel");
    }

    private static void AssertActionPolicy(Type controllerType, string methodName, string expectedPolicy)
    {
        var method = controllerType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(method);

        var authorizeAttr = method.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorizeAttr);
        Assert.Equal(expectedPolicy, authorizeAttr.Policy);
    }

    #endregion

    #region 2. Cross-Tenant Organization Isolation Tests

    [Fact]
    public async Task GetAsync_WhenIrrigationBelongsToDifferentOrganization_ThrowsResourceNotFoundException()
    {
        var farm = CreateActiveFarm(_otherOrgId);
        var area = CreateActiveFarmArea(farm.Id, _otherOrgId);
        var irrigation = new IrrigationEvent(_otherOrgId, farm.Id, area.Id, _userId);
        _irrigationStore.Irrigations.Add(irrigation);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _irrigationService.GetAsync(CreateActor(), irrigation.Id));
    }

    [Fact]
    public async Task ListAsync_WhenIrrigationsBelongToDifferentOrganization_FiltersThemOut()
    {
        var myFarm = CreateActiveFarm(_orgId);
        var myArea = CreateActiveFarmArea(myFarm.Id, _orgId);
        var otherFarm = CreateActiveFarm(_otherOrgId);
        var otherArea = CreateActiveFarmArea(otherFarm.Id, _otherOrgId);

        var myIrrigation = new IrrigationEvent(_orgId, myFarm.Id, myArea.Id, _userId);
        var otherIrrigation = new IrrigationEvent(_otherOrgId, otherFarm.Id, otherArea.Id, _userId);
        _irrigationStore.Irrigations.AddRange([myIrrigation, otherIrrigation]);

        var result = await _irrigationService.ListAsync(CreateActor(), new IrrigationListQuery());

        Assert.Single(result.Items);
        Assert.Equal(myIrrigation.Id, result.Items[0].Id);
    }

    [Fact]
    public async Task UpdateDraftAsync_WhenIrrigationBelongsToDifferentOrganization_ThrowsResourceNotFoundException()
    {
        var farm = CreateActiveFarm(_otherOrgId);
        var area = CreateActiveFarmArea(farm.Id, _otherOrgId);
        var irrigation = new IrrigationEvent(_otherOrgId, farm.Id, area.Id, _userId);
        _irrigationStore.Irrigations.Add(irrigation);

        var request = new UpdateIrrigationDraftRequest(FarmId: farm.Id, FarmAreaId: area.Id);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _irrigationService.UpdateDraftAsync(CreateActor(), irrigation.Id, request, null));
    }

    [Fact]
    public async Task ScheduleAsync_WhenIrrigationBelongsToDifferentOrganization_ThrowsResourceNotFoundException()
    {
        var farm = CreateActiveFarm(_otherOrgId);
        var area = CreateActiveFarmArea(farm.Id, _otherOrgId);
        var irrigation = new IrrigationEvent(_otherOrgId, farm.Id, area.Id, _userId);
        _irrigationStore.Irrigations.Add(irrigation);

        var request = new ScheduleIrrigationRequest(DateTimeOffset.UtcNow.AddHours(2));

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _irrigationService.ScheduleAsync(CreateActor(), irrigation.Id, request, null));
    }

    [Fact]
    public async Task CompleteAsync_WhenIrrigationBelongsToDifferentOrganization_ThrowsResourceNotFoundException()
    {
        var farm = CreateActiveFarm(_otherOrgId);
        var area = CreateActiveFarmArea(farm.Id, _otherOrgId);
        var irrigation = new IrrigationEvent(_otherOrgId, farm.Id, area.Id, _userId);
        _irrigationStore.Irrigations.Add(irrigation);

        var method = CreateIrrigationMethod(_otherOrgId);

        var request = new CompleteIrrigationRequest(method.Id);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _irrigationService.CompleteAsync(CreateActor(), irrigation.Id, request, null));
    }

    [Fact]
    public async Task CancelAsync_WhenIrrigationBelongsToDifferentOrganization_ThrowsResourceNotFoundException()
    {
        var farm = CreateActiveFarm(_otherOrgId);
        var area = CreateActiveFarmArea(farm.Id, _otherOrgId);
        var irrigation = new IrrigationEvent(_otherOrgId, farm.Id, area.Id, _userId);
        _irrigationStore.Irrigations.Add(irrigation);

        var request = new CancelIrrigationRequest("Cancelled by other");

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _irrigationService.CancelAsync(CreateActor(), irrigation.Id, request, null));
    }

    [Fact]
    public async Task CreateDraftAsync_WhenReferencingFarmFromAnotherOrganization_ThrowsResourceNotFoundException()
    {
        var otherFarm = CreateActiveFarm(_otherOrgId);
        var otherArea = CreateActiveFarmArea(otherFarm.Id, _otherOrgId);

        var request = new CreateIrrigationDraftRequest(FarmId: otherFarm.Id, FarmAreaId: otherArea.Id);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _irrigationService.CreateDraftAsync(CreateActor(), request, null));
    }

    #endregion

    #region Helper Factories & Fake Store

    private Farm CreateActiveFarm(Guid orgId)
    {
        var farm = new Farm(orgId, "Security Test Farm", Guid.NewGuid(), Guid.NewGuid(), 100m, _userId);
        _irrigationStore.Farms.Add(farm);
        return farm;
    }

    private FarmArea CreateActiveFarmArea(Guid farmId, Guid orgId)
    {
        var area = new FarmArea(orgId, farmId, null, "Security Test Area", 50m, Guid.NewGuid(), _userId);
        _irrigationStore.FarmAreas.Add(area);
        return area;
    }

    private IrrigationMethod CreateIrrigationMethod(Guid orgId)
    {
        var method = new IrrigationMethod(orgId, "METHOD", "Method Name", isSystem: false);
        _irrigationStore.Methods.Add(method);
        return method;
    }

    private sealed class FakeSecurityIrrigationStore : IIrrigationStore
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
            Task.FromResult<IReadOnlyList<IrrigationMethod>>(Methods.Where(m => (!activeOnly || m.IsActive) && (m.OrganizationId == organizationId || (m.IsSystem && m.OrganizationId == null))).ToArray());

        public Task<IrrigationSummaryCountsResponse> GetSummaryCountsAsync(Guid organizationId, Guid? farmId, DateTimeOffset now, CancellationToken cancellationToken = default) =>
            Task.FromResult(new IrrigationSummaryCountsResponse(0, 0, 0, 0, 0, 0, 0));

        public Task<Dictionary<Guid, string>> GetUserNamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken = default) =>
            Task.FromResult(userIds.Distinct().ToDictionary(id => id, id => "Security User"));

        public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) =>
            await operation(cancellationToken);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    #endregion
}
