using System.Reflection;
using FarmManagement.API.Controllers;
using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.MasterData;
using FarmManagement.Application.DTOs.PlantProtection;
using FarmManagement.Application.DTOs.Sprays;
using FarmManagement.Application.Interfaces;
using FarmManagement.Application.Interfaces.PlantProtection;
using FarmManagement.Application.Interfaces.Sprays;
using FarmManagement.Application.Services;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using FarmManagement.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace FarmManagement.API.Tests.Authorization;

public class SprayAuthorizationSecurityTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _otherOrgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly FakeSecuritySprayStore _sprayStore = new();
    private readonly FakeSecurityMasterDataStore _masterStore = new();
    private readonly SprayService _sprayService;
    private readonly MasterDataService _masterService;

    public SprayAuthorizationSecurityTests()
    {
        _sprayService = new SprayService(_sprayStore);
        _masterService = new MasterDataService(_masterStore);
    }

    private SprayActor CreateActor() => new(_userId, _orgId);
    private MasterDataActor CreateMasterActor() => new(_userId, _orgId);

    #region 1. Endpoint-Level Authorization Policy Tests (Reflection)

    [Fact]
    public void SpraysController_HasAuthorizeAttribute_AndActionsHaveCorrectPolicies()
    {
        var controllerType = typeof(SpraysController);
        Assert.NotNull(controllerType.GetCustomAttribute<AuthorizeAttribute>());

        AssertActionPolicy(controllerType, nameof(SpraysController.List), "Permission:Spray.View");
        AssertActionPolicy(controllerType, nameof(SpraysController.ListProductsLookup), "Permission:Spray.View");
        AssertActionPolicy(controllerType, nameof(SpraysController.ListStorageLocationsLookup), "Permission:Spray.View");
        AssertActionPolicy(controllerType, nameof(SpraysController.Get), "Permission:Spray.View");
        AssertActionPolicy(controllerType, nameof(SpraysController.Create), "Permission:Spray.Create");
        AssertActionPolicy(controllerType, nameof(SpraysController.Update), "Permission:Spray.Update");
        AssertActionPolicy(controllerType, nameof(SpraysController.Schedule), "Permission:Spray.Schedule");
        AssertActionPolicy(controllerType, nameof(SpraysController.Reschedule), "Permission:Spray.Schedule");
        AssertActionPolicy(controllerType, nameof(SpraysController.Start), "Permission:Spray.Start");
        AssertActionPolicy(controllerType, nameof(SpraysController.SaveExecution), "Permission:Spray.Start");
        AssertActionPolicy(controllerType, nameof(SpraysController.Complete), "Permission:Spray.Complete");
        AssertActionPolicy(controllerType, nameof(SpraysController.Cancel), "Permission:Spray.Cancel");
        AssertActionPolicy(controllerType, nameof(SpraysController.RecordCompleted), "Permission:Spray.Complete");
    }

    [Fact]
    public void PlantProtectionProductsController_HasAuthorizeAttribute_AndActionsHaveCorrectPolicies()
    {
        var controllerType = typeof(PlantProtectionProductsController);
        Assert.NotNull(controllerType.GetCustomAttribute<AuthorizeAttribute>());

        AssertActionPolicy(controllerType, nameof(PlantProtectionProductsController.List), "Permission:PlantProtectionProduct.View");
        AssertActionPolicy(controllerType, nameof(PlantProtectionProductsController.Lookup), "Permission:PlantProtectionProduct.View");
        AssertActionPolicy(controllerType, nameof(PlantProtectionProductsController.Get), "Permission:PlantProtectionProduct.View");
        AssertActionPolicy(controllerType, nameof(PlantProtectionProductsController.Create), "Permission:PlantProtectionProduct.Create");
        AssertActionPolicy(controllerType, nameof(PlantProtectionProductsController.Update), "Permission:PlantProtectionProduct.Update");
        AssertActionPolicy(controllerType, nameof(PlantProtectionProductsController.Activate), "Permission:PlantProtectionProduct.Activate");
        AssertActionPolicy(controllerType, nameof(PlantProtectionProductsController.Deactivate), "Permission:PlantProtectionProduct.Deactivate");
    }

    [Fact]
    public void MasterDataController_SprayAndProtectionEndpoints_HaveCorrectPolicies()
    {
        var controllerType = typeof(MasterDataController);
        Assert.NotNull(controllerType.GetCustomAttribute<AuthorizeAttribute>());

        AssertActionPolicy(controllerType, nameof(MasterDataController.ListProductTypes), "Permission:ProductType.View");
        AssertActionPolicy(controllerType, nameof(MasterDataController.CreateProductType), "Permission:ProductType.Create");
        AssertActionPolicy(controllerType, nameof(MasterDataController.UpdateProductType), "Permission:ProductType.Update");
        AssertActionPolicy(controllerType, nameof(MasterDataController.ActivateProductType), "Permission:ProductType.Activate");
        AssertActionPolicy(controllerType, nameof(MasterDataController.DeactivateProductType), "Permission:ProductType.Deactivate");

        AssertActionPolicy(controllerType, nameof(MasterDataController.ListTargets), "Permission:Target.View");
        AssertActionPolicy(controllerType, nameof(MasterDataController.CreateTarget), "Permission:Target.Create");
        AssertActionPolicy(controllerType, nameof(MasterDataController.UpdateTarget), "Permission:Target.Update");
        AssertActionPolicy(controllerType, nameof(MasterDataController.ActivateTarget), "Permission:Target.Activate");
        AssertActionPolicy(controllerType, nameof(MasterDataController.DeactivateTarget), "Permission:Target.Deactivate");

        AssertActionPolicy(controllerType, nameof(MasterDataController.ListApplicationMethods), "Permission:ApplicationMethod.View");
        AssertActionPolicy(controllerType, nameof(MasterDataController.CreateApplicationMethod), "Permission:ApplicationMethod.Create");
        AssertActionPolicy(controllerType, nameof(MasterDataController.UpdateApplicationMethod), "Permission:ApplicationMethod.Update");
        AssertActionPolicy(controllerType, nameof(MasterDataController.ActivateApplicationMethod), "Permission:ApplicationMethod.Activate");
        AssertActionPolicy(controllerType, nameof(MasterDataController.DeactivateApplicationMethod), "Permission:ApplicationMethod.Deactivate");
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
    public async Task GetAsync_WhenSprayBelongsToDifferentOrganization_ThrowsResourceNotFoundException()
    {
        var farm = CreateActiveFarm(_otherOrgId);
        var spray = new Spray(_otherOrgId, farm.Id, _userId, status: SprayStatus.Draft);
        _sprayStore.Sprays.Add(spray);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _sprayService.GetAsync(CreateActor(), spray.Id));
    }

    [Fact]
    public async Task ListAsync_WhenSpraysBelongToDifferentOrganization_FiltersThemOut()
    {
        var myFarm = CreateActiveFarm(_orgId);
        var otherFarm = CreateActiveFarm(_otherOrgId);

        var mySpray = new Spray(_orgId, myFarm.Id, _userId, status: SprayStatus.Draft);
        var otherSpray = new Spray(_otherOrgId, otherFarm.Id, _userId, status: SprayStatus.Draft);
        _sprayStore.Sprays.AddRange([mySpray, otherSpray]);

        var result = await _sprayService.ListAsync(CreateActor(), new SprayListQuery());

        Assert.Single(result.Items);
        Assert.Equal(mySpray.Id, result.Items[0].Id);
    }

    [Fact]
    public async Task UpdateDraftAsync_WhenSprayBelongsToDifferentOrganization_ThrowsResourceNotFoundException()
    {
        var farm = CreateActiveFarm(_otherOrgId);
        var spray = new Spray(_otherOrgId, farm.Id, _userId, status: SprayStatus.Draft);
        _sprayStore.Sprays.Add(spray);

        var request = new UpdateSprayDraftRequest(
            FarmId: farm.Id,
            PlannedDate: DateOnly.FromDateTime(DateTime.UtcNow));

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _sprayService.UpdateDraftAsync(CreateActor(), spray.Id, request, null));
    }

    [Fact]
    public async Task ScheduleAsync_WhenSprayBelongsToDifferentOrganization_ThrowsResourceNotFoundException()
    {
        var farm = CreateActiveFarm(_otherOrgId);
        var spray = new Spray(_otherOrgId, farm.Id, _userId, status: SprayStatus.Draft);
        _sprayStore.Sprays.Add(spray);

        var request = new ScheduleSprayRequest(DateTimeOffset.UtcNow.AddDays(1));

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _sprayService.ScheduleAsync(CreateActor(), spray.Id, request, null));
    }

    [Fact]
    public async Task RescheduleAsync_WhenSprayBelongsToDifferentOrganization_ThrowsResourceNotFoundException()
    {
        var farm = CreateActiveFarm(_otherOrgId);
        var spray = new Spray(_otherOrgId, farm.Id, _userId, status: SprayStatus.Scheduled, scheduledDateTime: DateTimeOffset.UtcNow.AddDays(1));
        _sprayStore.Sprays.Add(spray);

        var request = new RescheduleSprayRequest(DateTimeOffset.UtcNow.AddDays(2));

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _sprayService.RescheduleAsync(CreateActor(), spray.Id, request, null));
    }

    [Fact]
    public async Task StartAsync_WhenSprayBelongsToDifferentOrganization_ThrowsResourceNotFoundException()
    {
        var farm = CreateActiveFarm(_otherOrgId);
        var spray = new Spray(_otherOrgId, farm.Id, _userId, status: SprayStatus.Scheduled, scheduledDateTime: DateTimeOffset.UtcNow.AddDays(1));
        _sprayStore.Sprays.Add(spray);

        var request = new StartSprayRequest(DateTimeOffset.UtcNow, Products: []);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _sprayService.StartAsync(CreateActor(), spray.Id, request, null));
    }

    [Fact]
    public async Task SaveExecutionAsync_WhenSprayBelongsToDifferentOrganization_ThrowsResourceNotFoundException()
    {
        var farm = CreateActiveFarm(_otherOrgId);
        var spray = new Spray(_otherOrgId, farm.Id, _userId, status: SprayStatus.InProgress, actualApplicationDateTime: DateTimeOffset.UtcNow);
        _sprayStore.Sprays.Add(spray);

        var request = new UpdateSprayExecutionRequest(DateTimeOffset.UtcNow);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _sprayService.SaveExecutionAsync(CreateActor(), spray.Id, request, null));
    }

    [Fact]
    public async Task CompleteAsync_WhenSprayBelongsToDifferentOrganization_ThrowsResourceNotFoundException()
    {
        var farm = CreateActiveFarm(_otherOrgId);
        var spray = new Spray(_otherOrgId, farm.Id, _userId, status: SprayStatus.InProgress, actualApplicationDateTime: DateTimeOffset.UtcNow);
        _sprayStore.Sprays.Add(spray);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _sprayService.CompleteAsync(CreateActor(), spray.Id, null, null));
    }

    [Fact]
    public async Task CancelAsync_WhenSprayBelongsToDifferentOrganization_ThrowsResourceNotFoundException()
    {
        var farm = CreateActiveFarm(_otherOrgId);
        var spray = new Spray(_otherOrgId, farm.Id, _userId, status: SprayStatus.Draft);
        _sprayStore.Sprays.Add(spray);

        var request = new CancelSprayRequest("Weather issues");

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _sprayService.CancelAsync(CreateActor(), spray.Id, request, null));
    }

    #endregion

    #region 3. Master Data Cross-Tenant & System Immutability Tests

    [Fact]
    public async Task CreateDraftAsync_WhenTargetBelongsToDifferentOrganization_ThrowsResourceNotFoundException()
    {
        var farm = CreateActiveFarm(_orgId);
        var otherTarget = new Target(_otherOrgId, "T-OTHER", "Other Org Rust", TargetType.Disease, isSystem: false, createdBy: _userId);
        _sprayStore.Targets.Add(otherTarget);

        var request = new CreateSprayDraftRequest(
            FarmId: farm.Id,
            PlannedDate: DateOnly.FromDateTime(DateTime.UtcNow),
            TargetId: otherTarget.Id);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _sprayService.CreateDraftAsync(CreateActor(), request, null));
    }

    [Fact]
    public async Task CreateDraftAsync_WhenApplicationMethodBelongsToDifferentOrganization_ThrowsResourceNotFoundException()
    {
        var farm = CreateActiveFarm(_orgId);
        var otherMethod = new ApplicationMethod(_otherOrgId, "M-OTHER", "Other Org Sprayer", isSystem: false, createdBy: _userId);
        _sprayStore.ApplicationMethods.Add(otherMethod);

        var request = new CreateSprayDraftRequest(
            FarmId: farm.Id,
            PlannedDate: DateOnly.FromDateTime(DateTime.UtcNow),
            ApplicationMethodId: otherMethod.Id);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _sprayService.CreateDraftAsync(CreateActor(), request, null));
    }

    [Fact]
    public async Task CreateDraftAsync_WhenAreaUnitBelongsToDifferentOrganization_ThrowsResourceNotFoundException()
    {
        var farm = CreateActiveFarm(_orgId);
        var otherUnit = new Unit(_otherOrgId, "U-OTHER", "Other Org Unit", "ou", UnitCategory.Area, isSystem: false);
        _sprayStore.Units.Add(otherUnit);

        var request = new CreateSprayDraftRequest(
            FarmId: farm.Id,
            PlannedDate: DateOnly.FromDateTime(DateTime.UtcNow),
            PlannedArea: 10m,
            PlannedAreaUnitId: otherUnit.Id);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _sprayService.CreateDraftAsync(CreateActor(), request, null));
    }

    [Fact]
    public async Task MasterDataService_WhenUpdatingSystemProductType_ThrowsForbiddenException()
    {
        var systemType = new ProductType(null, "HERB", "Herbicide", isSystem: true, createdBy: _userId);
        _masterStore.ProductTypes.Add(systemType);

        var request = new UpdateProductTypeRequest("Renamed Herbicide", null, 1);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _masterService.UpdateProductTypeAsync(CreateMasterActor(), systemType.Id, request));
    }

    [Fact]
    public async Task MasterDataService_WhenUpdatingSystemTarget_ThrowsForbiddenException()
    {
        var systemTarget = new Target(null, "APHID", "Aphids", TargetType.Insect, isSystem: true, createdBy: _userId);
        _masterStore.Targets.Add(systemTarget);

        var request = new UpdateTargetRequest("Renamed Aphids", "Insect", null, 1);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _masterService.UpdateTargetAsync(CreateMasterActor(), systemTarget.Id, request));
    }

    [Fact]
    public async Task MasterDataService_WhenUpdatingSystemApplicationMethod_ThrowsForbiddenException()
    {
        var systemMethod = new ApplicationMethod(null, "BOOM", "Boom Sprayer", isSystem: true, createdBy: _userId);
        _masterStore.ApplicationMethods.Add(systemMethod);

        var request = new UpdateApplicationMethodRequest("Renamed Boom Sprayer", null, 1);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _masterService.UpdateApplicationMethodAsync(CreateMasterActor(), systemMethod.Id, request));
    }

    [Fact]
    public async Task MasterDataService_WhenDeactivatingSystemProductType_ThrowsForbiddenException()
    {
        var systemType = new ProductType(null, "FUNG", "Fungicide", isSystem: true, createdBy: _userId);
        _masterStore.ProductTypes.Add(systemType);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _masterService.DeactivateProductTypeAsync(CreateMasterActor(), systemType.Id));
    }

    #endregion

    #region 4. Inventory & Cross-Farm Isolation Tests

    [Fact]
    public async Task CreateDraftAsync_WhenInventoryItemBelongsToDifferentOrganization_ThrowsResourceNotFoundException()
    {
        var farm = CreateActiveFarm(_orgId);
        var unit = CreateUnit(_orgId);
        var otherItem = new InventoryItem(_otherOrgId, "Other Org Fungicide", unit.Id, _userId);
        _sprayStore.InventoryItems.Add(otherItem);
        _sprayStore.ActiveProtectionProductItemIds.Add(otherItem.Id);

        var request = new CreateSprayDraftRequest(
            FarmId: farm.Id,
            PlannedDate: DateOnly.FromDateTime(DateTime.UtcNow),
            Products: [new SprayProductItemRequest(otherItem.Id, PlannedQuantity: 5m, Dosage: "2ml/L")]);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _sprayService.CreateDraftAsync(CreateActor(), request, null));
    }

    [Fact]
    public async Task StartAsync_WhenStorageLocationBelongsToDifferentFarm_ThrowsValidationException()
    {
        var farm1 = CreateActiveFarm(_orgId);
        var farm2 = CreateActiveFarm(_orgId);
        var unit = CreateUnit(_orgId);
        var item = new InventoryItem(_orgId, "Valid Fungicide", unit.Id, _userId);
        _sprayStore.InventoryItems.Add(item);
        _sprayStore.ActiveProtectionProductItemIds.Add(item.Id);

        var locationFromFarm2 = new StorageLocation(_orgId, farm2.Id, "Farm 2 Barn", _userId);
        _sprayStore.StorageLocations.Add(locationFromFarm2);

        var spray = new Spray(_orgId, farm1.Id, _userId, status: SprayStatus.Scheduled, scheduledDateTime: DateTimeOffset.UtcNow.AddDays(1));
        var product = new SprayProduct(spray.Id, item.Id, _userId, plannedQuantity: 10m);
        spray.AddProduct(product);
        _sprayStore.Sprays.Add(spray);

        var request = new StartSprayRequest(
            DateTimeOffset.UtcNow,
            Products: [new StartSprayProductItemRequest(item.Id, locationFromFarm2.Id, ActualQuantity: 10m, Dosage: "2ml/L")]);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sprayService.StartAsync(CreateActor(), spray.Id, request, null));

        Assert.Contains("does not belong to the spray farm", ex.Errors!["storageLocationId"][0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RecordCompletedAsync_WhenStorageLocationBelongsToDifferentFarm_ThrowsValidationException()
    {
        var farm1 = CreateActiveFarm(_orgId);
        var farm2 = CreateActiveFarm(_orgId);
        var unit = CreateUnit(_orgId);
        var item = new InventoryItem(_orgId, "Valid Fungicide", unit.Id, _userId);
        _sprayStore.InventoryItems.Add(item);
        _sprayStore.ActiveProtectionProductItemIds.Add(item.Id);

        var locationFromFarm2 = new StorageLocation(_orgId, farm2.Id, "Farm 2 Barn", _userId);
        _sprayStore.StorageLocations.Add(locationFromFarm2);

        var request = new RecordCompletedSprayRequest(
            FarmId: farm1.Id,
            ActualApplicationDateTime: DateTimeOffset.UtcNow.AddHours(-1),
            Products: [new RecordCompletedSprayProductItemRequest(item.Id, locationFromFarm2.Id, ActualQuantity: 10m, Dosage: "2ml/L")]);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sprayService.RecordCompletedAsync(CreateActor(), request, null));

        Assert.Contains("does not belong to the spray farm", ex.Errors!["storageLocationId"][0], StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region 5. Agronomic Hierarchy Integrity Tests

    [Fact]
    public async Task CreateDraftAsync_WhenFarmAreaBelongsToDifferentFarm_ThrowsValidationException()
    {
        var farm1 = CreateActiveFarm(_orgId);
        var farm2 = CreateActiveFarm(_orgId);
        var areaFromFarm2 = new FarmArea(_orgId, farm2.Id, null, "Farm 2 Area", 10m, Guid.NewGuid(), _userId);
        _sprayStore.FarmAreas.Add(areaFromFarm2);

        var request = new CreateSprayDraftRequest(
            FarmId: farm1.Id,
            PlannedDate: DateOnly.FromDateTime(DateTime.UtcNow),
            FarmAreaId: areaFromFarm2.Id);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sprayService.CreateDraftAsync(CreateActor(), request, null));

        Assert.Contains("does not belong to the selected farm", ex.Errors!["farmAreaId"][0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateDraftAsync_WhenPlantationBelongsToDifferentFarmArea_ThrowsValidationException()
    {
        var farm = CreateActiveFarm(_orgId);
        var area1 = new FarmArea(_orgId, farm.Id, null, "Area 1", 10m, Guid.NewGuid(), _userId);
        var area2 = new FarmArea(_orgId, farm.Id, null, "Area 2", 10m, Guid.NewGuid(), _userId);
        _sprayStore.FarmAreas.AddRange([area1, area2]);

        var plantationInArea2 = new CropPlantation(_orgId, farm.Id, area2.Id, Guid.NewGuid(), null, null, "Block 2", 5m, Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), null, _userId);
        _sprayStore.Plantations.Add(plantationInArea2);

        var request = new CreateSprayDraftRequest(
            FarmId: farm.Id,
            PlannedDate: DateOnly.FromDateTime(DateTime.UtcNow),
            FarmAreaId: area1.Id,
            PlantationId: plantationInArea2.Id);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sprayService.CreateDraftAsync(CreateActor(), request, null));

        Assert.Contains("does not belong to the selected farm area", ex.Errors!["plantationId"][0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateDraftAsync_WhenCropCycleBelongsToDifferentPlantation_ThrowsValidationException()
    {
        var farm = CreateActiveFarm(_orgId);
        var area = new FarmArea(_orgId, farm.Id, null, "Area 1", 10m, Guid.NewGuid(), _userId);
        _sprayStore.FarmAreas.Add(area);

        var plantation1 = new CropPlantation(_orgId, farm.Id, area.Id, Guid.NewGuid(), null, null, "Block 1", 5m, Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), null, _userId);
        var plantation2 = new CropPlantation(_orgId, farm.Id, area.Id, Guid.NewGuid(), null, null, "Block 2", 5m, Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), null, _userId);
        _sprayStore.Plantations.AddRange([plantation1, plantation2]);

        var cycleOnPlantation2 = new CropCycle(_orgId, plantation2.Id, "Cycle 2", 2026, "Spring", DateOnly.FromDateTime(DateTime.UtcNow), null, _userId);
        _sprayStore.CropCycles.Add(cycleOnPlantation2);

        var request = new CreateSprayDraftRequest(
            FarmId: farm.Id,
            PlannedDate: DateOnly.FromDateTime(DateTime.UtcNow),
            FarmAreaId: area.Id,
            PlantationId: plantation1.Id,
            CropCycleId: cycleOnPlantation2.Id);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sprayService.CreateDraftAsync(CreateActor(), request, null));

        Assert.Contains("does not belong to the selected plantation", ex.Errors!["cropCycleId"][0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateDraftAsync_WhenCropCycleStageBelongsToDifferentCycle_ThrowsValidationException()
    {
        var farm = CreateActiveFarm(_orgId);
        var area = new FarmArea(_orgId, farm.Id, null, "Area 1", 10m, Guid.NewGuid(), _userId);
        _sprayStore.FarmAreas.Add(area);

        var plantation = new CropPlantation(_orgId, farm.Id, area.Id, Guid.NewGuid(), null, null, "Block 1", 5m, Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), null, _userId);
        _sprayStore.Plantations.Add(plantation);

        var cycle1 = new CropCycle(_orgId, plantation.Id, "Cycle 1", 2026, "Spring", DateOnly.FromDateTime(DateTime.UtcNow), null, _userId);
        var cycle2 = new CropCycle(_orgId, plantation.Id, "Cycle 2", 2026, "Spring", DateOnly.FromDateTime(DateTime.UtcNow), null, _userId);
        _sprayStore.CropCycles.AddRange([cycle1, cycle2]);

        var stageOnCycle2 = new CropCycleStage(cycle2.Id, Guid.NewGuid(), "Stage 2", 1, 10, DateOnly.FromDateTime(DateTime.UtcNow), null, _userId);
        _sprayStore.CropCycleStages.Add(stageOnCycle2);

        var request = new CreateSprayDraftRequest(
            FarmId: farm.Id,
            PlannedDate: DateOnly.FromDateTime(DateTime.UtcNow),
            FarmAreaId: area.Id,
            PlantationId: plantation.Id,
            CropCycleId: cycle1.Id,
            CropCycleStageId: stageOnCycle2.Id);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sprayService.CreateDraftAsync(CreateActor(), request, null));

        Assert.Contains("does not belong to the selected crop cycle", ex.Errors!["cropCycleStageId"][0], StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region 6. Role Permission Matrix Verification Tests

    [Fact]
    public void IdentityDataSeeder_FarmManagerRole_ContainsOperationalPermissions()
    {
        var field = typeof(IdentityDataSeeder).GetField("FarmManagerPermissions", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(field);
        var permissions = (IReadOnlySet<string>)field.GetValue(null)!;

        // Has planning & execution permissions
        Assert.Contains("Spray.View", permissions);
        Assert.Contains("Spray.Create", permissions);
        Assert.Contains("Spray.Update", permissions);
        Assert.Contains("Spray.Schedule", permissions);
        Assert.Contains("Spray.Start", permissions);
        Assert.Contains("Spray.Complete", permissions);
        Assert.Contains("Spray.Cancel", permissions);

        // Has catalog viewing permissions
        Assert.Contains("PlantProtectionProduct.View", permissions);
        Assert.Contains("ProductType.View", permissions);
        Assert.Contains("Target.View", permissions);
        Assert.Contains("ApplicationMethod.View", permissions);
    }

    [Fact]
    public void IdentityDataSeeder_SupervisorRole_ContainsExecutionOnlyPermissions()
    {
        var field = typeof(IdentityDataSeeder).GetField("SupervisorPermissions", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(field);
        var permissions = (IReadOnlySet<string>)field.GetValue(null)!;

        // Has execution permissions
        Assert.Contains("Spray.View", permissions);
        Assert.Contains("Spray.Start", permissions);
        Assert.Contains("Spray.Complete", permissions);

        // Excludes planning, scheduling, cancellation
        Assert.DoesNotContain("Spray.Create", permissions);
        Assert.DoesNotContain("Spray.Update", permissions);
        Assert.DoesNotContain("Spray.Schedule", permissions);
        Assert.DoesNotContain("Spray.Cancel", permissions);

        // Has catalog viewing permissions
        Assert.Contains("PlantProtectionProduct.View", permissions);
        Assert.Contains("ProductType.View", permissions);
        Assert.Contains("Target.View", permissions);
        Assert.Contains("ApplicationMethod.View", permissions);
    }

    #endregion

    #region Helpers & Test Fakes

    private Farm CreateActiveFarm(Guid organizationId)
    {
        var farm = new Farm(organizationId, "Farm " + organizationId.ToString()[..4], Guid.NewGuid(), Guid.NewGuid(), 100m, Guid.NewGuid());
        _sprayStore.Farms.Add(farm);
        return farm;
    }

    private Unit CreateUnit(Guid organizationId)
    {
        var unit = new Unit(organizationId, "L", "Liter", "L", UnitCategory.Volume, isSystem: false);
        _sprayStore.Units.Add(unit);
        return unit;
    }

    private sealed class FakeSecuritySprayStore : ISprayStore
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
        public List<StorageLocation> StorageLocations { get; } = [];
        public HashSet<Guid> ActiveProtectionProductItemIds { get; } = [];
        public List<AuditLog> AuditLogs { get; } = [];

        public Task<Spray?> FindAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Sprays.FirstOrDefault(s => s.Id == id && s.OrganizationId == organizationId));

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
            Task.FromResult(ActiveProtectionProductItemIds.Contains(inventoryItemId));

        public Task<StorageLocation?> FindStorageLocationAsync(Guid storageLocationId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(StorageLocations.FirstOrDefault(sl => sl.Id == storageLocationId && sl.OrganizationId == organizationId));

        public Task<StockBalance?> FindStockBalanceAsync(Guid storageLocationId, Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<StockBalance?>(new StockBalance(organizationId, Guid.NewGuid(), storageLocationId, inventoryItemId, 100m));

        public void RemoveSprayProduct(SprayProduct product) { }
        public void AddMovement(StockMovement movement) { }

        public Task<StockBalance?> LockBalanceAsync(Guid storageLocationId, Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default) =>
            FindStockBalanceAsync(storageLocationId, inventoryItemId, organizationId, cancellationToken);

        public Task AcquireAdvisoryLockAsync(Guid storageLocationId, Guid inventoryItemId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) =>
            operation(cancellationToken);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeSecurityMasterDataStore : IMasterDataStore
    {
        public List<ProductType> ProductTypes { get; } = [];
        public List<Target> Targets { get; } = [];
        public List<ApplicationMethod> ApplicationMethods { get; } = [];

        public Task<IReadOnlyList<Unit>> ListUnitsAsync(Guid organizationId, string? category = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Unit>>([]);

        public Task<IReadOnlyList<FarmOwnershipType>> ListFarmOwnershipTypesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FarmOwnershipType>>([]);

        public Task<IReadOnlyList<PlantationEndReason>> ListPlantationEndReasonsAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PlantationEndReason>>([]);

        public Task<IReadOnlyList<Currency>> ListCurrenciesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Currency>>([]);

        public Task<IReadOnlyList<ProductType>> ListProductTypesAsync(Guid organizationId, bool includeInactive = false, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ProductType>>(ProductTypes.Where(p => (p.IsSystem && p.OrganizationId == null) || p.OrganizationId == organizationId).ToList());

        public Task<ProductType?> FindProductTypeByIdAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ProductTypes.FirstOrDefault(p => p.Id == id && ((p.IsSystem && p.OrganizationId == null) || p.OrganizationId == organizationId)));

        public Task<bool> ProductTypeCodeExistsAsync(string code, Guid organizationId, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public void AddProductType(ProductType productType) => ProductTypes.Add(productType);

        public Task<IReadOnlyList<Target>> ListTargetsAsync(Guid organizationId, TargetType? type = null, bool includeInactive = false, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Target>>(Targets.Where(t => (t.IsSystem && t.OrganizationId == null) || t.OrganizationId == organizationId).ToList());

        public Task<Target?> FindTargetByIdAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Targets.FirstOrDefault(t => t.Id == id && ((t.IsSystem && t.OrganizationId == null) || t.OrganizationId == organizationId)));

        public Task<bool> TargetCodeExistsAsync(string code, Guid organizationId, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public void AddTarget(Target target) => Targets.Add(target);

        public Task<IReadOnlyList<ApplicationMethod>> ListApplicationMethodsAsync(Guid organizationId, bool includeInactive = false, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ApplicationMethod>>(ApplicationMethods.Where(a => (a.IsSystem && a.OrganizationId == null) || a.OrganizationId == organizationId).ToList());

        public Task<ApplicationMethod?> FindApplicationMethodByIdAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ApplicationMethods.FirstOrDefault(a => a.Id == id && ((a.IsSystem && a.OrganizationId == null) || a.OrganizationId == organizationId)));

        public Task<bool> ApplicationMethodCodeExistsAsync(string code, Guid organizationId, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public void AddApplicationMethod(ApplicationMethod method) => ApplicationMethods.Add(method);

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }

    #endregion
}
