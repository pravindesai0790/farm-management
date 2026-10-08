using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.Sprays;
using FarmManagement.Application.Interfaces.Sprays;
using FarmManagement.Application.Services;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Services;

public class SprayExecutionServiceTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly FakeExecutionSprayStore _store = new();
    private readonly SprayService _sut;

    public SprayExecutionServiceTests()
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

    private StorageLocation CreateStorageLocation(Guid farmId, string name = "Main Barn")
    {
        var location = new StorageLocation(_orgId, farmId, name, _userId);
        _store.StorageLocations.Add(location);
        return location;
    }

    private Unit CreateUnit(string code = "L", string name = "Liter", UnitCategory category = UnitCategory.Volume)
    {
        var unit = new Unit(null, code, name, code, category, isSystem: true);
        _store.Units.Add(unit);
        return unit;
    }

    private InventoryItem CreateInventoryItem(string name = "Copper Oxychloride")
    {
        var unit = CreateUnit();
        var item = new InventoryItem(_orgId, name, unit.Id, _userId);
        _store.InventoryItems.Add(item);
        _store.ActiveProtectionProductItemIds.Add(item.Id);
        return item;
    }

    private Target CreateTarget(string name = "Powdery Mildew")
    {
        var target = new Target(_orgId, "T-01", name, TargetType.Disease);
        _store.Targets.Add(target);
        return target;
    }

    private ApplicationMethod CreateApplicationMethod(string name = "Tractor Sprayer")
    {
        var method = new ApplicationMethod(_orgId, "AM-01", name);
        _store.ApplicationMethods.Add(method);
        return method;
    }

    private static void AssertValidationError(ValidationException ex, string text)
    {
        var allErrors = (ex.Errors?.SelectMany(kv => kv.Value ?? []) ?? []).ToList();
        Assert.Contains(allErrors, err => err.Contains(text, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SaveExecutionAsync_WhenValidRequest_UpdatesExecution_AndLogsAudit()
    {
        // Arrange
        var farm = CreateActiveFarm();
        var storageLocation = CreateStorageLocation(farm.Id);
        var item = CreateInventoryItem();
        var target = CreateTarget();
        var method = CreateApplicationMethod();
        var areaUnit = CreateUnit("HA", "Hectare", UnitCategory.Area);
        var waterUnit = CreateUnit("L", "Liter", UnitCategory.Volume);

        var spray = new Spray(
            _orgId,
            farm.Id,
            _userId,
            status: SprayStatus.InProgress,
            actualApplicationDateTime: DateTimeOffset.UtcNow.AddHours(-2));
        var product = new SprayProduct(spray.Id, item.Id, _userId, storageLocation.Id, plannedQuantity: 10m, actualQuantity: 5m, dosage: "1 L/ha");
        spray.AddProduct(product);
        _store.Sprays.Add(spray);

        var updatedActualTime = DateTimeOffset.UtcNow.AddHours(-1);
        var request = new UpdateSprayExecutionRequest(
            ActualApplicationDateTime: updatedActualTime,
            ActualTreatedArea: 4.5m,
            ActualTreatedAreaUnitId: areaUnit.Id,
            WaterQuantity: 300m,
            WaterUnitId: waterUnit.Id,
            TargetId: target.Id,
            ApplicationMethodId: method.Id,
            PurposeReason: "Routine mildew control update",
            Products: [
                new UpdateSprayExecutionProductItemRequest(
                    InventoryItemId: item.Id,
                    ActualQuantity: 8.5m,
                    Dosage: "1.7 L/ha")
            ]);

        // Act
        var result = await _sut.SaveExecutionAsync(CreateActor(), spray.Id, request, "127.0.0.1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(SprayStatus.InProgress, result.Status);
        Assert.Equal("INPROGRESS", result.StatusName);
        Assert.Equal(updatedActualTime, result.ActualApplicationDateTime);
        Assert.Equal(4.5m, result.ActualTreatedArea);
        Assert.Equal(areaUnit.Id, result.ActualTreatedAreaUnitId);
        Assert.Equal(300m, result.WaterQuantity);
        Assert.Equal(waterUnit.Id, result.WaterUnitId);
        Assert.Equal(target.Id, result.TargetId);
        Assert.Equal(method.Id, result.ApplicationMethodId);
        Assert.Equal("Routine mildew control update", result.PurposeReason);

        var resultProduct = Assert.Single(result.Products);
        Assert.Equal(item.Id, resultProduct.InventoryItemId);
        Assert.Equal(8.5m, resultProduct.ActualQuantity);
        Assert.Equal("1.7 L/ha", resultProduct.Dosage);

        // Verify audit log
        var audit = Assert.Single(_store.AuditLogs);
        Assert.Equal("Spray.ExecutionSaved", audit.Action);
        Assert.Equal(spray.Id, audit.EntityId);
    }

    [Fact]
    public async Task SaveExecutionAsync_WhenSprayNotFound_ThrowsResourceNotFoundException()
    {
        var request = new UpdateSprayExecutionRequest(
            ActualApplicationDateTime: DateTimeOffset.UtcNow);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _sut.SaveExecutionAsync(CreateActor(), Guid.NewGuid(), request, null));
    }

    [Theory]
    [InlineData(SprayStatus.Draft)]
    [InlineData(SprayStatus.Scheduled)]
    [InlineData(SprayStatus.Completed)]
    [InlineData(SprayStatus.Cancelled)]
    public async Task SaveExecutionAsync_WhenSprayNotInProgress_ThrowsConflictException(SprayStatus status)
    {
        // Arrange
        var farm = CreateActiveFarm();
        var spray = new Spray(_orgId, farm.Id, _userId, status: status);
        _store.Sprays.Add(spray);

        var request = new UpdateSprayExecutionRequest(
            ActualApplicationDateTime: DateTimeOffset.UtcNow);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.SaveExecutionAsync(CreateActor(), spray.Id, request, null));
    }

    [Fact]
    public async Task SaveExecutionAsync_WhenActualApplicationDateTimeInFuture_ThrowsValidationException()
    {
        // Arrange
        var farm = CreateActiveFarm();
        var spray = new Spray(_orgId, farm.Id, _userId, status: SprayStatus.InProgress);
        _store.Sprays.Add(spray);

        var futureTime = DateTimeOffset.UtcNow.AddMinutes(30);
        var request = new UpdateSprayExecutionRequest(
            ActualApplicationDateTime: futureTime);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.SaveExecutionAsync(CreateActor(), spray.Id, request, null));
        AssertValidationError(ex, "future");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task SaveExecutionAsync_WhenActualQuantityZeroOrNegative_ThrowsValidationException(decimal quantity)
    {
        // Arrange
        var farm = CreateActiveFarm();
        var storageLocation = CreateStorageLocation(farm.Id);
        var item = CreateInventoryItem();

        var spray = new Spray(_orgId, farm.Id, _userId, status: SprayStatus.InProgress);
        spray.AddProduct(new SprayProduct(spray.Id, item.Id, _userId, storageLocation.Id, actualQuantity: 5m));
        _store.Sprays.Add(spray);

        var request = new UpdateSprayExecutionRequest(
            ActualApplicationDateTime: DateTimeOffset.UtcNow,
            Products: [
                new UpdateSprayExecutionProductItemRequest(item.Id, quantity, null)
            ]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.SaveExecutionAsync(CreateActor(), spray.Id, request, null));
        AssertValidationError(ex, "greater than zero");
    }

    [Fact]
    public async Task SaveExecutionAsync_WhenProductNotPartOfSpray_ThrowsValidationException()
    {
        // Arrange
        var farm = CreateActiveFarm();
        var storageLocation = CreateStorageLocation(farm.Id);
        var item1 = CreateInventoryItem("Item 1");
        var item2 = CreateInventoryItem("Item 2");

        var spray = new Spray(_orgId, farm.Id, _userId, status: SprayStatus.InProgress);
        spray.AddProduct(new SprayProduct(spray.Id, item1.Id, _userId, storageLocation.Id, actualQuantity: 5m));
        _store.Sprays.Add(spray);

        // Attempt to supply item2 which was not part of the spray
        var request = new UpdateSprayExecutionRequest(
            ActualApplicationDateTime: DateTimeOffset.UtcNow,
            Products: [
                new UpdateSprayExecutionProductItemRequest(item2.Id, 10m, null)
            ]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.SaveExecutionAsync(CreateActor(), spray.Id, request, null));
        AssertValidationError(ex, "cannot be added after start");
    }

    [Fact]
    public async Task SaveExecutionAsync_WhenDuplicateProductsInRequest_ThrowsValidationException()
    {
        // Arrange
        var farm = CreateActiveFarm();
        var storageLocation = CreateStorageLocation(farm.Id);
        var item = CreateInventoryItem();

        var spray = new Spray(_orgId, farm.Id, _userId, status: SprayStatus.InProgress);
        spray.AddProduct(new SprayProduct(spray.Id, item.Id, _userId, storageLocation.Id, actualQuantity: 5m));
        _store.Sprays.Add(spray);

        var request = new UpdateSprayExecutionRequest(
            ActualApplicationDateTime: DateTimeOffset.UtcNow,
            Products: [
                new UpdateSprayExecutionProductItemRequest(item.Id, 5m, null),
                new UpdateSprayExecutionProductItemRequest(item.Id, 8m, null)
            ]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.SaveExecutionAsync(CreateActor(), spray.Id, request, null));
        AssertValidationError(ex, "Duplicate inventory items are not allowed");
    }

    [Fact]
    public async Task SaveExecutionAsync_WhenTreatedAreaSuppliedWithoutUnit_ThrowsValidationException()
    {
        // Arrange
        var farm = CreateActiveFarm();
        var spray = new Spray(_orgId, farm.Id, _userId, status: SprayStatus.InProgress);
        _store.Sprays.Add(spray);

        var request = new UpdateSprayExecutionRequest(
            ActualApplicationDateTime: DateTimeOffset.UtcNow,
            ActualTreatedArea: 2.5m,
            ActualTreatedAreaUnitId: null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.SaveExecutionAsync(CreateActor(), spray.Id, request, null));
        AssertValidationError(ex, "area unit is required");
    }

    [Fact]
    public async Task SaveExecutionAsync_WhenTreatedAreaUnitSuppliedWithoutArea_ThrowsValidationException()
    {
        // Arrange
        var farm = CreateActiveFarm();
        var unit = CreateUnit("HA", "Hectare", UnitCategory.Area);
        var spray = new Spray(_orgId, farm.Id, _userId, status: SprayStatus.InProgress);
        _store.Sprays.Add(spray);

        var request = new UpdateSprayExecutionRequest(
            ActualApplicationDateTime: DateTimeOffset.UtcNow,
            ActualTreatedArea: null,
            ActualTreatedAreaUnitId: unit.Id);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.SaveExecutionAsync(CreateActor(), spray.Id, request, null));
        AssertValidationError(ex, "treated area is required");
    }

    [Fact]
    public async Task SaveExecutionAsync_WhenWaterSuppliedWithoutUnit_ThrowsValidationException()
    {
        // Arrange
        var farm = CreateActiveFarm();
        var spray = new Spray(_orgId, farm.Id, _userId, status: SprayStatus.InProgress);
        _store.Sprays.Add(spray);

        var request = new UpdateSprayExecutionRequest(
            ActualApplicationDateTime: DateTimeOffset.UtcNow,
            WaterQuantity: 100m,
            WaterUnitId: null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.SaveExecutionAsync(CreateActor(), spray.Id, request, null));
        AssertValidationError(ex, "water unit is required");
    }

    [Fact]
    public async Task SaveExecutionAsync_WhenWaterUnitSuppliedWithoutQuantity_ThrowsValidationException()
    {
        // Arrange
        var farm = CreateActiveFarm();
        var unit = CreateUnit("L", "Liter", UnitCategory.Volume);
        var spray = new Spray(_orgId, farm.Id, _userId, status: SprayStatus.InProgress);
        _store.Sprays.Add(spray);

        var request = new UpdateSprayExecutionRequest(
            ActualApplicationDateTime: DateTimeOffset.UtcNow,
            WaterQuantity: null,
            WaterUnitId: unit.Id);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.SaveExecutionAsync(CreateActor(), spray.Id, request, null));
        AssertValidationError(ex, "water quantity is required");
    }

    [Fact]
    public async Task SaveExecutionAsync_WhenTargetNotFound_ThrowsResourceNotFoundException()
    {
        // Arrange
        var farm = CreateActiveFarm();
        var spray = new Spray(_orgId, farm.Id, _userId, status: SprayStatus.InProgress);
        _store.Sprays.Add(spray);

        var request = new UpdateSprayExecutionRequest(
            ActualApplicationDateTime: DateTimeOffset.UtcNow,
            TargetId: Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _sut.SaveExecutionAsync(CreateActor(), spray.Id, request, null));
    }

    [Fact]
    public async Task SaveExecutionAsync_WhenApplicationMethodNotFound_ThrowsResourceNotFoundException()
    {
        // Arrange
        var farm = CreateActiveFarm();
        var spray = new Spray(_orgId, farm.Id, _userId, status: SprayStatus.InProgress);
        _store.Sprays.Add(spray);

        var request = new UpdateSprayExecutionRequest(
            ActualApplicationDateTime: DateTimeOffset.UtcNow,
            ApplicationMethodId: Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _sut.SaveExecutionAsync(CreateActor(), spray.Id, request, null));
    }

    private sealed class FakeExecutionSprayStore : ISprayStore
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
        public List<StockBalance> StockBalances { get; } = [];
        public HashSet<Guid> ActiveProtectionProductItemIds { get; } = [];
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
            Task.FromResult(ActiveProtectionProductItemIds.Contains(inventoryItemId));

        public Task<StorageLocation?> FindStorageLocationAsync(Guid storageLocationId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(StorageLocations.FirstOrDefault(l => l.Id == storageLocationId && l.OrganizationId == organizationId));

        public Task<StockBalance?> FindStockBalanceAsync(Guid storageLocationId, Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(StockBalances.FirstOrDefault(sb => sb.StorageLocationId == storageLocationId && sb.InventoryItemId == inventoryItemId && sb.OrganizationId == organizationId));

        public void RemoveSprayProduct(SprayProduct product) { }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
