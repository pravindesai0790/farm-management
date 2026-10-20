using System.Reflection;
using FarmManagement.API.Controllers;
using FarmManagement.Application.Common.Constants;
using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.Fertilizer;
using FarmManagement.Application.Interfaces.Fertilizer;
using FarmManagement.Application.Services;
using FarmManagement.Application.Services.Fertilizer;
using FarmManagement.Domain.Constants;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using FarmManagement.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace FarmManagement.API.Tests.Authorization;

public class FertilizerAuthorizationSecurityTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _otherOrgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly FakeSecurityFertilizerStore _store = new();
    private readonly FertilizerStockManager _stockManager;
    private readonly FertilizerValidator _validator;
    private readonly TestReferenceGenerator _refGen = new();
    private readonly FertilizerApplicationService _service;

    public FertilizerAuthorizationSecurityTests()
    {
        _stockManager = new FertilizerStockManager(_store);
        _validator = new FertilizerValidator(_store);
        _service = new FertilizerApplicationService(_store, _validator, _refGen, _stockManager);
    }

    private FertilizerActor CreateActor() => new(_userId, _orgId);
    private FertilizerActor CreateOtherActor() => new(_userId, _otherOrgId);

    #region 1. Endpoint-Level Authorization Policy Tests (Reflection)

    [Fact]
    public void FertilizerApplicationsController_HasAuthorizeAttribute_AndActionsHaveCorrectPolicies()
    {
        var controllerType = typeof(FertilizerApplicationsController);
        Assert.NotNull(controllerType.GetCustomAttribute<AuthorizeAttribute>());

        AssertActionPolicy(controllerType, nameof(FertilizerApplicationsController.List), "Permission:Fertilizer.View");
        AssertActionPolicy(controllerType, nameof(FertilizerApplicationsController.Get), "Permission:Fertilizer.View");
        AssertActionPolicy(controllerType, nameof(FertilizerApplicationsController.ListProductsLookup), "Permission:Fertilizer.View");
        AssertActionPolicy(controllerType, nameof(FertilizerApplicationsController.ListStorageLocationsLookup), "Permission:Fertilizer.View");
        AssertActionPolicy(controllerType, nameof(FertilizerApplicationsController.ListApplicationMethodsLookup), "Permission:Fertilizer.View");
        AssertActionPolicy(controllerType, nameof(FertilizerApplicationsController.ListIrrigationsLookup), "Permission:Fertilizer.View");

        AssertActionPolicy(controllerType, nameof(FertilizerApplicationsController.Create), "Permission:Fertilizer.Create");
        AssertActionPolicy(controllerType, nameof(FertilizerApplicationsController.Copy), "Permission:Fertilizer.Create");

        AssertActionPolicy(controllerType, nameof(FertilizerApplicationsController.Update), "Permission:Fertilizer.Update");
        AssertActionPolicy(controllerType, nameof(FertilizerApplicationsController.Plan), "Permission:Fertilizer.Update");
        AssertActionPolicy(controllerType, nameof(FertilizerApplicationsController.Schedule), "Permission:Fertilizer.Update");
        AssertActionPolicy(controllerType, nameof(FertilizerApplicationsController.Reschedule), "Permission:Fertilizer.Update");

        AssertActionPolicy(controllerType, nameof(FertilizerApplicationsController.Delete), "Permission:Fertilizer.DeleteDraft");
        AssertActionPolicy(controllerType, nameof(FertilizerApplicationsController.Start), "Permission:Fertilizer.Start");
        AssertActionPolicy(controllerType, nameof(FertilizerApplicationsController.Complete), "Permission:Fertilizer.Complete");
        AssertActionPolicy(controllerType, nameof(FertilizerApplicationsController.Cancel), "Permission:Fertilizer.Cancel");
        AssertActionPolicy(controllerType, nameof(FertilizerApplicationsController.RecordCompleted), "Permission:Fertilizer.RecordCompleted");
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

    #region 2. Role Permission Matrix Verification Tests (Reflection on IdentityDataSeeder)

    [Fact]
    public void IdentityDataSeeder_SeedPermissions_ContainsAllFertilizerPermissionsWithCorrectModule()
    {
        var field = typeof(IdentityDataSeeder).GetField("SeedPermissions", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(field);
        var seedPermissions = (IEnumerable<object>)field.GetValue(null)!;

        var fertilizerPermissions = new Dictionary<string, (string Description, string Module)>();
        foreach (var p in seedPermissions)
        {
            var pType = p.GetType();
            var name = (string)pType.GetProperty("Name")!.GetValue(p)!;
            var desc = (string)pType.GetProperty("Description")!.GetValue(p)!;
            var module = (string)pType.GetProperty("Module")!.GetValue(p)!;

            if (name.StartsWith("Fertilizer."))
            {
                fertilizerPermissions[name] = (desc, module);
            }
        }

        string[] expectedPermissions =
        [
            "Fertilizer.View",
            "Fertilizer.Create",
            "Fertilizer.Update",
            "Fertilizer.Start",
            "Fertilizer.Complete",
            "Fertilizer.Cancel",
            "Fertilizer.RecordCompleted",
            "Fertilizer.DeleteDraft",
            "Fertilizer.Export"
        ];

        Assert.Equal(9, fertilizerPermissions.Count);
        foreach (var expected in expectedPermissions)
        {
            Assert.True(fertilizerPermissions.ContainsKey(expected), $"Missing permission: {expected}");
            Assert.Equal("Fertilizer", fertilizerPermissions[expected].Module);
        }
    }

    [Fact]
    public void IdentityDataSeeder_OrganizationAdminRole_ContainsAllFertilizerPermissions()
    {
        var field = typeof(IdentityDataSeeder).GetField("OrganizationAdminPermissions", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(field);
        var permissions = (IReadOnlySet<string>)field.GetValue(null)!;

        Assert.Contains("Fertilizer.View", permissions);
        Assert.Contains("Fertilizer.Create", permissions);
        Assert.Contains("Fertilizer.Update", permissions);
        Assert.Contains("Fertilizer.Start", permissions);
        Assert.Contains("Fertilizer.Complete", permissions);
        Assert.Contains("Fertilizer.Cancel", permissions);
        Assert.Contains("Fertilizer.RecordCompleted", permissions);
        Assert.Contains("Fertilizer.DeleteDraft", permissions);
        Assert.Contains("Fertilizer.Export", permissions);
    }

    [Fact]
    public void IdentityDataSeeder_FarmManagerRole_ContainsAllFertilizerPermissions()
    {
        var field = typeof(IdentityDataSeeder).GetField("FarmManagerPermissions", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(field);
        var permissions = (IReadOnlySet<string>)field.GetValue(null)!;

        Assert.Contains("Fertilizer.View", permissions);
        Assert.Contains("Fertilizer.Create", permissions);
        Assert.Contains("Fertilizer.Update", permissions);
        Assert.Contains("Fertilizer.Start", permissions);
        Assert.Contains("Fertilizer.Complete", permissions);
        Assert.Contains("Fertilizer.Cancel", permissions);
        Assert.Contains("Fertilizer.RecordCompleted", permissions);
        Assert.Contains("Fertilizer.DeleteDraft", permissions);
        Assert.Contains("Fertilizer.Export", permissions);
    }

    [Fact]
    public void IdentityDataSeeder_SupervisorRole_ContainsOnlyExecutionFertilizerPermissions()
    {
        var field = typeof(IdentityDataSeeder).GetField("SupervisorPermissions", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(field);
        var permissions = (IReadOnlySet<string>)field.GetValue(null)!;

        // Execution permissions present
        Assert.Contains("Fertilizer.View", permissions);
        Assert.Contains("Fertilizer.Start", permissions);
        Assert.Contains("Fertilizer.Complete", permissions);
        Assert.Contains("Fertilizer.Cancel", permissions);
        Assert.Contains("Fertilizer.RecordCompleted", permissions);

        // Authoring & export permissions excluded
        Assert.DoesNotContain("Fertilizer.Create", permissions);
        Assert.DoesNotContain("Fertilizer.Update", permissions);
        Assert.DoesNotContain("Fertilizer.DeleteDraft", permissions);
        Assert.DoesNotContain("Fertilizer.Export", permissions);
    }

    #endregion

    #region 3. Cross-Tenant Organization Isolation Tests

    [Fact]
    public async Task GetAsync_WhenApplicationBelongsToOtherOrg_ThrowsResourceNotFoundException()
    {
        var farm = CreateFarm(_otherOrgId);
        var area = CreateFarmArea(_otherOrgId, farm.Id);
        var app = new FertilizerApplication(_otherOrgId, "FERT-2026-999999", _userId, farm.Id, area.Id);
        _store.Applications.Add(app);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _service.GetAsync(CreateActor(), app.Id));
    }

    [Fact]
    public async Task ListAsync_WhenApplicationsBelongToMultipleOrgs_ReturnsOnlyCurrentOrgRecords()
    {
        var farm1 = CreateFarm(_orgId);
        var area1 = CreateFarmArea(_orgId, farm1.Id);
        var myApp = new FertilizerApplication(_orgId, "FERT-2026-000001", _userId, farm1.Id, area1.Id);

        var farm2 = CreateFarm(_otherOrgId);
        var area2 = CreateFarmArea(_otherOrgId, farm2.Id);
        var otherApp = new FertilizerApplication(_otherOrgId, "FERT-2026-000002", _userId, farm2.Id, area2.Id);

        _store.Applications.AddRange([myApp, otherApp]);

        var result = await _service.ListAsync(CreateActor(), new FertilizerApplicationListQuery());

        Assert.Single(result.Items);
        Assert.Equal(myApp.Id, result.Items[0].Id);
    }

    [Fact]
    public async Task UpdateDraftAsync_WhenApplicationBelongsToOtherOrg_ThrowsResourceNotFoundException()
    {
        var farm = CreateFarm(_otherOrgId);
        var area = CreateFarmArea(_otherOrgId, farm.Id);
        var app = new FertilizerApplication(_otherOrgId, "FERT-2026-999999", _userId, farm.Id, area.Id);
        _store.Applications.Add(app);

        var request = new UpdateFertilizerDraftRequest(farm.Id, area.Id);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _service.UpdateDraftAsync(CreateActor(), app.Id, request, null));
    }

    [Fact]
    public async Task DeleteDraftAsync_WhenApplicationBelongsToOtherOrg_ThrowsResourceNotFoundException()
    {
        var farm = CreateFarm(_otherOrgId);
        var area = CreateFarmArea(_otherOrgId, farm.Id);
        var app = new FertilizerApplication(_otherOrgId, "FERT-2026-999999", _userId, farm.Id, area.Id);
        _store.Applications.Add(app);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _service.DeleteDraftAsync(CreateActor(), app.Id, null));
    }

    [Fact]
    public async Task CopyAsNewDraftAsync_WhenSourceBelongsToOtherOrg_ThrowsResourceNotFoundException()
    {
        var farm = CreateFarm(_otherOrgId);
        var area = CreateFarmArea(_otherOrgId, farm.Id);
        var app = new FertilizerApplication(_otherOrgId, "FERT-2026-999999", _userId, farm.Id, area.Id);
        _store.Applications.Add(app);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _service.CopyAsNewDraftAsync(CreateActor(), app.Id, null));
    }

    [Fact]
    public async Task PlanAsync_WhenApplicationBelongsToOtherOrg_ThrowsResourceNotFoundException()
    {
        var farm = CreateFarm(_otherOrgId);
        var area = CreateFarmArea(_otherOrgId, farm.Id);
        var app = new FertilizerApplication(_otherOrgId, "FERT-2026-999999", _userId, farm.Id, area.Id);
        _store.Applications.Add(app);

        var request = new PlanFertilizerApplicationRequest();

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _service.PlanAsync(CreateActor(), app.Id, request, null));
    }

    [Fact]
    public async Task ScheduleAsync_WhenApplicationBelongsToOtherOrg_ThrowsResourceNotFoundException()
    {
        var farm = CreateFarm(_otherOrgId);
        var area = CreateFarmArea(_otherOrgId, farm.Id);
        var app = new FertilizerApplication(_otherOrgId, "FERT-2026-999999", _userId, farm.Id, area.Id);
        _store.Applications.Add(app);

        var request = new ScheduleFertilizerApplicationRequest(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)));

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _service.ScheduleAsync(CreateActor(), app.Id, request, null));
    }

    [Fact]
    public async Task RescheduleAsync_WhenApplicationBelongsToOtherOrg_ThrowsResourceNotFoundException()
    {
        var farm = CreateFarm(_otherOrgId);
        var area = CreateFarmArea(_otherOrgId, farm.Id);
        var app = new FertilizerApplication(_otherOrgId, "FERT-2026-999999", _userId, farm.Id, area.Id);
        SetPrivateProperty(app, nameof(FertilizerApplication.Status), FertilizerApplicationStatus.Scheduled);
        _store.Applications.Add(app);

        var request = new RescheduleFertilizerApplicationRequest(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)));

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _service.RescheduleAsync(CreateActor(), app.Id, request, null));
    }

    [Fact]
    public async Task StartAsync_WhenApplicationBelongsToOtherOrg_ThrowsResourceNotFoundException()
    {
        var farm = CreateFarm(_otherOrgId);
        var area = CreateFarmArea(_otherOrgId, farm.Id);
        var app = new FertilizerApplication(_otherOrgId, "FERT-2026-999999", _userId, farm.Id, area.Id);
        SetPrivateProperty(app, nameof(FertilizerApplication.Status), FertilizerApplicationStatus.Scheduled);
        _store.Applications.Add(app);

        var request = new StartFertilizerApplicationRequest();

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _service.StartAsync(CreateActor(), app.Id, request, null));
    }

    [Fact]
    public async Task CompleteAsync_WhenApplicationBelongsToOtherOrg_ThrowsResourceNotFoundException()
    {
        var farm = CreateFarm(_otherOrgId);
        var area = CreateFarmArea(_otherOrgId, farm.Id);
        var app = new FertilizerApplication(_otherOrgId, "FERT-2026-999999", _userId, farm.Id, area.Id);
        SetPrivateProperty(app, nameof(FertilizerApplication.Status), FertilizerApplicationStatus.InProgress);
        _store.Applications.Add(app);

        var request = new CompleteFertilizerApplicationRequest(DateOnly.FromDateTime(DateTime.UtcNow), Products: []);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _service.CompleteAsync(CreateActor(), app.Id, request, null));
    }

    [Fact]
    public async Task CancelAsync_WhenApplicationBelongsToOtherOrg_ThrowsResourceNotFoundException()
    {
        var farm = CreateFarm(_otherOrgId);
        var area = CreateFarmArea(_otherOrgId, farm.Id);
        var app = new FertilizerApplication(_otherOrgId, "FERT-2026-999999", _userId, farm.Id, area.Id);
        _store.Applications.Add(app);

        var request = new CancelFertilizerApplicationRequest("Cancelled");

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _service.CancelAsync(CreateActor(), app.Id, request, null));
    }

    [Fact]
    public async Task CreateDraftAsync_WhenReferencingForeignFarm_ThrowsResourceNotFoundException()
    {
        var otherFarm = CreateFarm(_otherOrgId);
        var otherArea = CreateFarmArea(_otherOrgId, otherFarm.Id);

        var request = new CreateFertilizerDraftRequest(otherFarm.Id, otherArea.Id);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _service.CreateDraftAsync(CreateActor(), request, null));
    }

    [Fact]
    public async Task CreateDraftAsync_WhenReferencingForeignFarmArea_ThrowsResourceNotFoundException()
    {
        var myFarm = CreateFarm(_orgId);
        var otherFarm = CreateFarm(_otherOrgId);
        var otherArea = CreateFarmArea(_otherOrgId, otherFarm.Id);

        var request = new CreateFertilizerDraftRequest(myFarm.Id, otherArea.Id);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _service.CreateDraftAsync(CreateActor(), request, null));
    }

    [Fact]
    public async Task CreateDraftAsync_WhenReferencingForeignInventoryItem_ThrowsResourceNotFoundException()
    {
        var myFarm = CreateFarm(_orgId);
        var myArea = CreateFarmArea(_orgId, myFarm.Id);
        var otherItem = CreateInventoryItem(_otherOrgId, FarmInventoryCategories.FertilizersSoilId, "Other Org Fertilizer");

        var request = new CreateFertilizerDraftRequest(
            myFarm.Id,
            myArea.Id,
            Products: [new(otherItem.Id, 10m)]);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _service.CreateDraftAsync(CreateActor(), request, null));
    }

    [Fact]
    public async Task CreateDraftAsync_WhenReferencingForeignStorageLocation_ThrowsResourceNotFoundException()
    {
        var myFarm = CreateFarm(_orgId);
        var myArea = CreateFarmArea(_orgId, myFarm.Id);
        var myItem = CreateInventoryItem(_orgId, FarmInventoryCategories.FertilizersSoilId, "Urea");
        var otherLoc = CreateStorageLocation(_otherOrgId, Guid.NewGuid(), "Other Shed");

        var request = new CreateFertilizerDraftRequest(
            myFarm.Id,
            myArea.Id,
            Products: [new(myItem.Id, 10m, otherLoc.Id)]);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _service.CreateDraftAsync(CreateActor(), request, null));
    }

    [Fact]
    public async Task Lookups_FilterOutForeignOrganizationData()
    {
        var actor = CreateActor();

        var myItem = CreateInventoryItem(_orgId, FarmInventoryCategories.FertilizersSoilId, "My Org Fertilizer");
        var otherItem = CreateInventoryItem(_otherOrgId, FarmInventoryCategories.FertilizersSoilId, "Other Org Fertilizer");

        var products = await _service.ListProductsLookupAsync(actor);
        Assert.Contains(products, p => p.InventoryItemId == myItem.Id);
        Assert.DoesNotContain(products, p => p.InventoryItemId == otherItem.Id);

        var myFarm = CreateFarm(_orgId);
        var otherFarm = CreateFarm(_otherOrgId);
        var myLoc = CreateStorageLocation(_orgId, myFarm.Id, "My Shed");
        var otherLoc = CreateStorageLocation(_otherOrgId, otherFarm.Id, "Other Shed");

        var locations = await _service.ListStorageLocationsLookupAsync(actor, myFarm.Id, null);
        Assert.Contains(locations, l => l.StorageLocationId == myLoc.Id);
        Assert.DoesNotContain(locations, l => l.StorageLocationId == otherLoc.Id);

        var myMethod = CreateMethod(_orgId, "METHOD-MINE", "My Method");
        var otherMethod = CreateMethod(_otherOrgId, "METHOD-OTHER", "Other Method");

        var methods = await _service.ListApplicationMethodsLookupAsync(actor);
        Assert.Contains(methods, m => m.Id == myMethod.Id);
        Assert.DoesNotContain(methods, m => m.Id == otherMethod.Id);
    }

    #endregion

    #region 4. Terminal & Frozen State Immutability Tests

    [Fact]
    public async Task CompletedApplication_RejectsAllMutationsWithConflictException()
    {
        var farm = CreateFarm(_orgId);
        var area = CreateFarmArea(_orgId, farm.Id);
        var app = new FertilizerApplication(_orgId, "FERT-2026-000010", _userId, farm.Id, area.Id);
        SetPrivateProperty(app, nameof(FertilizerApplication.Status), FertilizerApplicationStatus.Completed);
        _store.Applications.Add(app);

        var actor = CreateActor();

        // 1. UpdateDraftAsync
        await Assert.ThrowsAsync<ConflictException>(() =>
            _service.UpdateDraftAsync(actor, app.Id, new UpdateFertilizerDraftRequest(farm.Id, area.Id), null));

        // 2. DeleteDraftAsync
        await Assert.ThrowsAsync<ConflictException>(() =>
            _service.DeleteDraftAsync(actor, app.Id, null));

        // 3. PlanAsync
        await Assert.ThrowsAsync<ConflictException>(() =>
            _service.PlanAsync(actor, app.Id, new PlanFertilizerApplicationRequest(), null));

        // 4. ScheduleAsync
        await Assert.ThrowsAsync<ConflictException>(() =>
            _service.ScheduleAsync(actor, app.Id, new ScheduleFertilizerApplicationRequest(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))), null));

        // 5. RescheduleAsync
        await Assert.ThrowsAsync<ConflictException>(() =>
            _service.RescheduleAsync(actor, app.Id, new RescheduleFertilizerApplicationRequest(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2))), null));

        // 6. StartAsync
        await Assert.ThrowsAsync<ConflictException>(() =>
            _service.StartAsync(actor, app.Id, new StartFertilizerApplicationRequest(), null));

        // 7. CompleteAsync
        await Assert.ThrowsAsync<ConflictException>(() =>
            _service.CompleteAsync(actor, app.Id, new CompleteFertilizerApplicationRequest(DateOnly.FromDateTime(DateTime.UtcNow), Products: []), null));

        // 8. CancelAsync
        await Assert.ThrowsAsync<ConflictException>(() =>
            _service.CancelAsync(actor, app.Id, new CancelFertilizerApplicationRequest("Cancelled"), null));

        // 9. Direct domain mutation methods throw InvalidOperationException
        Assert.Throws<InvalidOperationException>(() => app.Plan(DateTimeOffset.UtcNow, _userId));
        Assert.Throws<InvalidOperationException>(() => app.Schedule(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), DateTimeOffset.UtcNow, _userId));
        Assert.Throws<InvalidOperationException>(() => app.Reschedule(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)), DateTimeOffset.UtcNow, _userId));
        Assert.Throws<InvalidOperationException>(() => app.Start(DateTimeOffset.UtcNow, _userId));
        Assert.Throws<InvalidOperationException>(() => app.Complete(DateOnly.FromDateTime(DateTime.UtcNow), null, null, DateTimeOffset.UtcNow, _userId));
        Assert.Throws<InvalidOperationException>(() => app.Cancel("Reason", DateTimeOffset.UtcNow, _userId));
        Assert.Throws<InvalidOperationException>(() => app.AddProduct(new FertilizerApplicationProduct(app.Id, Guid.NewGuid(), 10m, _userId)));
        Assert.Throws<InvalidOperationException>(() => app.RemoveProduct(Guid.NewGuid()));
    }

    [Fact]
    public async Task CancelledApplication_RejectsAllMutationsWithConflictException()
    {
        var farm = CreateFarm(_orgId);
        var area = CreateFarmArea(_orgId, farm.Id);
        var app = new FertilizerApplication(_orgId, "FERT-2026-000020", _userId, farm.Id, area.Id);
        SetPrivateProperty(app, nameof(FertilizerApplication.Status), FertilizerApplicationStatus.Cancelled);
        _store.Applications.Add(app);

        var actor = CreateActor();

        // 1. UpdateDraftAsync
        await Assert.ThrowsAsync<ConflictException>(() =>
            _service.UpdateDraftAsync(actor, app.Id, new UpdateFertilizerDraftRequest(farm.Id, area.Id), null));

        // 2. DeleteDraftAsync
        await Assert.ThrowsAsync<ConflictException>(() =>
            _service.DeleteDraftAsync(actor, app.Id, null));

        // 3. PlanAsync
        await Assert.ThrowsAsync<ConflictException>(() =>
            _service.PlanAsync(actor, app.Id, new PlanFertilizerApplicationRequest(), null));

        // 4. ScheduleAsync
        await Assert.ThrowsAsync<ConflictException>(() =>
            _service.ScheduleAsync(actor, app.Id, new ScheduleFertilizerApplicationRequest(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))), null));

        // 5. RescheduleAsync
        await Assert.ThrowsAsync<ConflictException>(() =>
            _service.RescheduleAsync(actor, app.Id, new RescheduleFertilizerApplicationRequest(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2))), null));

        // 6. StartAsync
        await Assert.ThrowsAsync<ConflictException>(() =>
            _service.StartAsync(actor, app.Id, new StartFertilizerApplicationRequest(), null));

        // 7. CompleteAsync
        await Assert.ThrowsAsync<ConflictException>(() =>
            _service.CompleteAsync(actor, app.Id, new CompleteFertilizerApplicationRequest(DateOnly.FromDateTime(DateTime.UtcNow), Products: []), null));

        // 8. CancelAsync
        await Assert.ThrowsAsync<ConflictException>(() =>
            _service.CancelAsync(actor, app.Id, new CancelFertilizerApplicationRequest("Cancelled"), null));

        // 9. Direct domain mutation methods throw InvalidOperationException
        Assert.Throws<InvalidOperationException>(() => app.Plan(DateTimeOffset.UtcNow, _userId));
        Assert.Throws<InvalidOperationException>(() => app.Schedule(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), DateTimeOffset.UtcNow, _userId));
        Assert.Throws<InvalidOperationException>(() => app.Reschedule(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)), DateTimeOffset.UtcNow, _userId));
        Assert.Throws<InvalidOperationException>(() => app.Start(DateTimeOffset.UtcNow, _userId));
        Assert.Throws<InvalidOperationException>(() => app.Complete(DateOnly.FromDateTime(DateTime.UtcNow), null, null, DateTimeOffset.UtcNow, _userId));
        Assert.Throws<InvalidOperationException>(() => app.Cancel("Reason", DateTimeOffset.UtcNow, _userId));
        Assert.Throws<InvalidOperationException>(() => app.AddProduct(new FertilizerApplicationProduct(app.Id, Guid.NewGuid(), 10m, _userId)));
        Assert.Throws<InvalidOperationException>(() => app.RemoveProduct(Guid.NewGuid()));
    }

    [Fact]
    public async Task InProgressApplication_RejectsDraftModificationsAndReStart()
    {
        var farm = CreateFarm(_orgId);
        var area = CreateFarmArea(_orgId, farm.Id);
        var app = new FertilizerApplication(_orgId, "FERT-2026-000030", _userId, farm.Id, area.Id);
        SetPrivateProperty(app, nameof(FertilizerApplication.Status), FertilizerApplicationStatus.InProgress);
        _store.Applications.Add(app);

        var actor = CreateActor();

        await Assert.ThrowsAsync<ConflictException>(() =>
            _service.UpdateDraftAsync(actor, app.Id, new UpdateFertilizerDraftRequest(farm.Id, area.Id), null));

        await Assert.ThrowsAsync<ConflictException>(() =>
            _service.DeleteDraftAsync(actor, app.Id, null));

        await Assert.ThrowsAsync<ConflictException>(() =>
            _service.PlanAsync(actor, app.Id, new PlanFertilizerApplicationRequest(), null));

        await Assert.ThrowsAsync<ConflictException>(() =>
            _service.ScheduleAsync(actor, app.Id, new ScheduleFertilizerApplicationRequest(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))), null));

        await Assert.ThrowsAsync<ConflictException>(() =>
            _service.RescheduleAsync(actor, app.Id, new RescheduleFertilizerApplicationRequest(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2))), null));

        await Assert.ThrowsAsync<ConflictException>(() =>
            _service.StartAsync(actor, app.Id, new StartFertilizerApplicationRequest(), null));
    }

    #endregion

    #region 5. Full Lifecycle Audit Trail Verification Tests

    [Fact]
    public async Task FullLifecycle_EmitsAuditLogsWithExpectedActionsAndPayloads()
    {
        var actor = CreateActor();
        var farm = CreateFarm(_orgId);
        var area = CreateFarmArea(_orgId, farm.Id);
        var method = CreateMethod(_orgId, "BROADCAST", "Broadcasting");
        var item = CreateInventoryItem(_orgId, FarmInventoryCategories.FertilizersSoilId, "Urea 46%");
        var loc = CreateStorageLocation(_orgId, farm.Id, "Main Shed");
        CreateStockBalance(farm.Id, loc.Id, item.Id, 100m);

        // 1. Create Draft
        var plannedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));
        var draft = await _service.CreateDraftAsync(actor, new CreateFertilizerDraftRequest(
            farm.Id, area.Id, ApplicationMethodId: method.Id,
            PlannedDate: plannedDate,
            Products: [new(item.Id, 20m, loc.Id)]), "10.0.0.1");

        var audit1 = _store.AuditLogs.Last();
        Assert.Equal("Fertilizer.DraftCreated", audit1.Action);
        Assert.Equal("FertilizerApplication", audit1.EntityType);
        Assert.Equal(draft.Id, audit1.EntityId);
        Assert.Equal(_orgId, audit1.OrganizationId);
        Assert.Equal(_userId, audit1.UserId);
        Assert.Equal("10.0.0.1", audit1.IpAddress);

        // 2. Update Draft
        await _service.UpdateDraftAsync(actor, draft.Id, new UpdateFertilizerDraftRequest(
            farm.Id, area.Id, ApplicationMethodId: method.Id,
            PlannedDate: plannedDate,
            Products: [new(item.Id, 25m, loc.Id)], Version: draft.Version), "10.0.0.2");

        var audit2 = _store.AuditLogs.Last();
        Assert.Equal("Fertilizer.DraftUpdated", audit2.Action);
        Assert.Equal("10.0.0.2", audit2.IpAddress);

        // 3. Plan
        var planned = await _service.PlanAsync(actor, draft.Id, new PlanFertilizerApplicationRequest(Version: 2), "10.0.0.3");
        var audit3 = _store.AuditLogs.Last();
        Assert.Equal("Fertilizer.Planned", audit3.Action);
        Assert.Equal("10.0.0.3", audit3.IpAddress);

        // 4. Schedule
        var schedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));
        var scheduled = await _service.ScheduleAsync(actor, draft.Id, new ScheduleFertilizerApplicationRequest(schedDate, Version: planned.Version), "10.0.0.4");
        var audit4 = _store.AuditLogs.Last();
        Assert.Equal("Fertilizer.Scheduled", audit4.Action);
        Assert.Equal("10.0.0.4", audit4.IpAddress);

        // 5. Reschedule
        var newSchedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3));
        var rescheduled = await _service.RescheduleAsync(actor, draft.Id, new RescheduleFertilizerApplicationRequest(newSchedDate, Version: scheduled.Version), "10.0.0.5");
        var audit5 = _store.AuditLogs.Last();
        Assert.Equal("Fertilizer.Rescheduled", audit5.Action);
        Assert.Equal("10.0.0.5", audit5.IpAddress);

        // 6. Start
        var started = await _service.StartAsync(actor, draft.Id, new StartFertilizerApplicationRequest(Version: rescheduled.Version), "10.0.0.6");
        var audit6 = _store.AuditLogs.Last();
        Assert.Equal("Fertilizer.Started", audit6.Action);
        Assert.Equal("10.0.0.6", audit6.IpAddress);

        // 7. Complete
        var actualDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var completed = await _service.CompleteAsync(actor, draft.Id, new CompleteFertilizerApplicationRequest(
            actualDate,
            Products: [new(item.Id, 22m)],
            AcknowledgeVariance: true,
            Version: started.Version), "10.0.0.7");

        var audit7 = _store.AuditLogs.Last();
        Assert.Equal("Fertilizer.Completed", audit7.Action);
        Assert.Equal("10.0.0.7", audit7.IpAddress);
        Assert.NotNull(completed.CompletedAt);

        // 8. Copy As New Draft
        var copied = await _service.CopyAsNewDraftAsync(actor, completed.Id, "10.0.0.8");
        var audit8 = _store.AuditLogs.Last();
        Assert.Equal("Fertilizer.DraftCopied", audit8.Action);
        Assert.Equal("10.0.0.8", audit8.IpAddress);

        // 9. Delete Draft (the copied one)
        await _service.DeleteDraftAsync(actor, copied.Id, "10.0.0.9");
        var audit9 = _store.AuditLogs.Last();
        Assert.Equal("Fertilizer.DraftDeleted", audit9.Action);
        Assert.Equal("10.0.0.9", audit9.IpAddress);

        // 10. Direct Completed
        var directCompleted = await _service.RecordCompletedAsync(actor, new RecordCompletedFertilizerApplicationRequest(
            farm.Id, area.Id, method.Id, actualDate,
            Products: [new(item.Id, loc.Id, 10m)]), "10.0.0.10");

        var audit10 = _store.AuditLogs.Last();
        Assert.Equal("Fertilizer.RecordCompleted", audit10.Action);
        Assert.Equal("10.0.0.10", audit10.IpAddress);

        // 11. Cancel (test cancellation audit on a separate draft/in-progress)
        var draftToCancel = await _service.CreateDraftAsync(actor, new CreateFertilizerDraftRequest(
            farm.Id, area.Id, ApplicationMethodId: method.Id), null);

        await _service.CancelAsync(actor, draftToCancel.Id, new CancelFertilizerApplicationRequest("Weather cancellation"), "10.0.0.11");
        var audit11 = _store.AuditLogs.Last();
        Assert.Equal("Fertilizer.Cancelled", audit11.Action);
        Assert.Equal("10.0.0.11", audit11.IpAddress);
    }

    #endregion

    #region Helpers & Test Fakes

    private Farm CreateFarm(Guid orgId)
    {
        var farm = new Farm(orgId, "Security Farm", Guid.NewGuid(), Guid.NewGuid(), 100m, _userId);
        _store.Farms.Add(farm);
        return farm;
    }

    private FarmArea CreateFarmArea(Guid orgId, Guid farmId)
    {
        var area = new FarmArea(orgId, farmId, null, "Security Area", 50m, Guid.NewGuid(), _userId);
        _store.FarmAreas.Add(area);
        return area;
    }

    private InventoryItem CreateInventoryItem(Guid orgId, Guid categoryId, string name = "Urea 46%")
    {
        var item = new InventoryItem(orgId, name, Guid.NewGuid(), _userId, "UREA-SKU", null, categoryId);
        _store.InventoryItems.Add(item);
        return item;
    }

    private StorageLocation CreateStorageLocation(Guid orgId, Guid farmId, string name = "Main Shed")
    {
        var loc = new StorageLocation(orgId, farmId, name, _userId, "Main Storage");
        _store.StorageLocations.Add(loc);
        return loc;
    }

    private StockBalance CreateStockBalance(Guid farmId, Guid storageLocationId, Guid inventoryItemId, decimal balance)
    {
        var sb = new StockBalance(_orgId, farmId, storageLocationId, inventoryItemId, initialQuantity: balance);
        _store.StockBalances.Add(sb);
        return sb;
    }

    private ApplicationMethod CreateMethod(Guid orgId, string code, string name)
    {
        var method = new ApplicationMethod(orgId, code, name, isSystem: false, description: null, displayOrder: 1, createdBy: _userId, module: "FERTILIZER");
        _store.ApplicationMethods.Add(method);
        return method;
    }

    private static void SetPrivateProperty<T>(T target, string propertyName, object? value)
    {
        var prop = typeof(T).GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (prop != null && prop.CanWrite)
        {
            prop.SetValue(target, value);
        }
        else
        {
            var field = typeof(T).GetField($"<{propertyName}>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(target, value);
        }
    }

    private sealed class TestReferenceGenerator : IFertilizerReferenceGenerator
    {
        private int _sequence;
        public Task<string> NextReferenceNumberAsync(Guid organizationId, int year, CancellationToken cancellationToken = default)
        {
            _sequence++;
            return Task.FromResult($"FERT-{year}-{_sequence:D6}");
        }
    }

    private sealed class FakeSecurityFertilizerStore : IFertilizerApplicationStore
    {
        public List<FertilizerApplication> Applications { get; } = [];
        public List<Farm> Farms { get; } = [];
        public List<FarmArea> FarmAreas { get; } = [];
        public List<CropPlantation> Plantations { get; } = [];
        public List<CropCycle> CropCycles { get; } = [];
        public List<CropCycleStage> CropCycleStages { get; } = [];
        public List<ApplicationMethod> ApplicationMethods { get; } = [];
        public List<Unit> Units { get; } = [];
        public List<InventoryItem> InventoryItems { get; } = [];
        public HashSet<Guid> ActivePppItemIds { get; } = [];
        public List<StorageLocation> StorageLocations { get; } = [];
        public List<IrrigationEvent> IrrigationEvents { get; } = [];
        public List<StockBalance> StockBalances { get; } = [];
        public List<AuditLog> AuditLogs { get; } = [];
        public List<StockMovement> StockMovements { get; } = [];
        public List<FertilizerOperationIdempotencyRecord> IdempotencyRecords { get; } = [];

        public Task<StockBalance?> LockBalanceAsync(Guid storageLocationId, Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(StockBalances.FirstOrDefault(sb => sb.StorageLocationId == storageLocationId && sb.InventoryItemId == inventoryItemId && sb.OrganizationId == organizationId));

        public Task AcquireAdvisoryLockAsync(Guid storageLocationId, Guid inventoryItemId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) =>
            await operation(cancellationToken);

        public void AddMovement(StockMovement movement) => StockMovements.Add(movement);

        public Task<FertilizerOperationIdempotencyRecord?> FindIdempotencyRecordAsync(Guid organizationId, string operationType, string idempotencyKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(IdempotencyRecords.FirstOrDefault(r => r.OrganizationId == organizationId && r.OperationType == operationType && r.IdempotencyKey == idempotencyKey));

        public void AddIdempotencyRecord(FertilizerOperationIdempotencyRecord record) => IdempotencyRecords.Add(record);

        public void Add(FertilizerApplication application) => Applications.Add(application);

        public void Remove(FertilizerApplication application) => Applications.Remove(application);

        public void RemoveProduct(FertilizerApplicationProduct product)
        {
            var app = Applications.FirstOrDefault(a => a.Id == product.FertilizerApplicationId);
            if (app is not null)
            {
                app.RemoveProduct(product.InventoryItemId);
            }
        }

        public Task<FertilizerApplication?> FindAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Applications.FirstOrDefault(a => a.Id == id && a.OrganizationId == organizationId));

        public Task<FertilizerApplication?> FindByIdempotencyKeyAsync(Guid organizationId, string idempotencyKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(Applications.FirstOrDefault(a => a.OrganizationId == organizationId && a.IdempotencyKey == idempotencyKey));

        public Task<int> CountAsync(Guid organizationId, FertilizerApplicationListQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult(Applications.Count(a => a.OrganizationId == organizationId));

        public Task<IReadOnlyList<FertilizerApplication>> ListAsync(Guid organizationId, FertilizerApplicationListQuery query, int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FertilizerApplication>>(Applications.Where(a => a.OrganizationId == organizationId).Skip(skip).Take(take).ToArray());

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

        public Task<ApplicationMethod?> FindApplicationMethodAsync(Guid methodId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ApplicationMethods.FirstOrDefault(m => m.Id == methodId && (m.OrganizationId == organizationId || (m.IsSystem && m.OrganizationId == null))));

        public Task<IReadOnlyList<ApplicationMethod>> ListApplicationMethodsAsync(Guid organizationId, bool activeOnly, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ApplicationMethod>>(ApplicationMethods.Where(m => (!activeOnly || m.IsActive) && (m.OrganizationId == organizationId || (m.IsSystem && m.OrganizationId == null))).ToArray());

        public Task<InventoryItem?> FindInventoryItemAsync(Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(InventoryItems.FirstOrDefault(i => i.Id == inventoryItemId && i.OrganizationId == organizationId));

        public Task<bool> HasActivePlantProtectionProfileAsync(Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ActivePppItemIds.Contains(inventoryItemId));

        public Task<StorageLocation?> FindStorageLocationAsync(Guid storageLocationId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(StorageLocations.FirstOrDefault(l => l.Id == storageLocationId && l.OrganizationId == organizationId));

        public Task<IReadOnlyList<StorageLocation>> ListStorageLocationsAsync(Guid organizationId, Guid farmId, bool activeOnly, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StorageLocation>>(StorageLocations.Where(l => l.OrganizationId == organizationId && l.FarmId == farmId && (!activeOnly || l.IsActive)).ToArray());

        public Task<IrrigationEvent?> FindIrrigationEventAsync(Guid irrigationEventId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(IrrigationEvents.FirstOrDefault(e => e.Id == irrigationEventId && e.OrganizationId == organizationId));

        public Task<IReadOnlyList<IrrigationEvent>> ListIrrigationEventsAsync(Guid organizationId, Guid? farmId, Guid? farmAreaId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<IrrigationEvent>>(IrrigationEvents.Where(e => e.OrganizationId == organizationId && (!farmId.HasValue || e.FarmId == farmId.Value) && (!farmAreaId.HasValue || e.FarmAreaId == farmAreaId.Value)).ToArray());

        public Task<IReadOnlyList<FertilizerProductLookupResponse>> ListProductsLookupAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FertilizerProductLookupResponse>>(InventoryItems
                .Where(i => i.OrganizationId == organizationId)
                .Select(i => new FertilizerProductLookupResponse(i.Id, i.Name, i.Sku, Guid.Empty, "Unit", "U", "Symbol", null, null))
                .ToArray());

        public Task<IReadOnlyList<FertilizerStorageLocationLookupResponse>> ListStorageLocationsLookupAsync(Guid organizationId, Guid farmId, Guid? inventoryItemId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FertilizerStorageLocationLookupResponse>>(StorageLocations
                .Where(l => l.OrganizationId == organizationId && l.FarmId == farmId)
                .Select(l => new FertilizerStorageLocationLookupResponse(l.Id, l.Name, 0m, false, null, null, null))
                .ToArray());

        public Task<IReadOnlyList<FertilizerApplicationMethodLookupResponse>> ListApplicationMethodsLookupAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FertilizerApplicationMethodLookupResponse>>(ApplicationMethods
                .Where(m => (m.IsSystem && m.OrganizationId == null) || m.OrganizationId == organizationId)
                .Select(m => new FertilizerApplicationMethodLookupResponse(m.Id, m.Code, m.Name, m.Description, m.DisplayOrder, m.Code == "OTHER"))
                .ToArray());

        public Task<IReadOnlyList<FertilizerIrrigationLookupResponse>> ListIrrigationsLookupAsync(Guid organizationId, Guid? farmId, Guid? farmAreaId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FertilizerIrrigationLookupResponse>>(IrrigationEvents
                .Where(e => e.OrganizationId == organizationId)
                .Select(e => new FertilizerIrrigationLookupResponse(e.Id, e.FarmId, e.FarmAreaId, "Area", null, null, "Completed", null, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "Method", 100m, "L"))
                .ToArray());

        public Task<IReadOnlyDictionary<Guid, string>> GetUserNamesAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(userIds.Distinct().ToDictionary(id => id, id => "Security User"));

        public Task<StockBalance?> FindStockBalanceAsync(Guid storageLocationId, Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(StockBalances.FirstOrDefault(sb => sb.StorageLocationId == storageLocationId && sb.InventoryItemId == inventoryItemId && sb.OrganizationId == organizationId));

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    #endregion
}
