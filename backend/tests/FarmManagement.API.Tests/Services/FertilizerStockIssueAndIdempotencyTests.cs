using System.Reflection;
using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.Fertilizer;
using FarmManagement.Application.Interfaces.Fertilizer;
using FarmManagement.Application.Services;
using FarmManagement.Application.Services.Fertilizer;
using FarmManagement.Domain.Constants;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Services;

public class FertilizerStockIssueAndIdempotencyTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly FakeFertilizerStore _store = new();
    private readonly FertilizerStockManager _stockManager;
    private readonly FertilizerApplicationService _sut;

    public FertilizerStockIssueAndIdempotencyTests()
    {
        _stockManager = new FertilizerStockManager(_store);
        var validator = new FertilizerValidator(_store);
        _sut = new FertilizerApplicationService(_store, validator, new TestReferenceGenerator(), _stockManager);
    }

    private FertilizerActor CreateActor(Guid? orgId = null) =>
        new(_userId, orgId ?? _orgId);

    // =========================================================================
    // 1. MULTI-LINE STOCK ISSUE & BALANCE DEDUCTION
    // =========================================================================

    [Fact]
    public async Task ConsumeStockAsync_MultiLineIssue_DeductsBalancesAndCreatesMovements()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, plantation, cycle, stage) = CreateValidHierarchyChain();
        var item1 = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId, "Urea 46%");
        var item2 = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId, "Potassium Sulfate");
        var loc1 = CreateStorageLocation(farm.Id, "Silo A");
        var loc2 = CreateStorageLocation(farm.Id, "Silo B");

        var balance1 = new StockBalance(_orgId, farm.Id, loc1.Id, item1.Id, initialQuantity: 100m);
        var balance2 = new StockBalance(_orgId, farm.Id, loc2.Id, item2.Id, initialQuantity: 80m);
        _store.StockBalances.Add(balance1);
        _store.StockBalances.Add(balance2);

        var app = new FertilizerApplication(
            _orgId, "FERT-2026-000001", _userId,
            farmId: farm.Id, farmAreaId: area.Id, plantationId: plantation.Id,
            cropCycleId: cycle.Id, cropCycleStageId: stage.Id);
        app.AddProduct(item1.Id, 50m, _userId, loc1.Id);
        app.AddProduct(item2.Id, 30m, _userId, loc2.Id);
        _store.Applications.Add(app);

        var itemsToIssue = new List<FertilizerStockConsumptionItem>
        {
            new(item1.Id, loc1.Id, 50m),
            new(item2.Id, loc2.Id, 30m)
        };

        var movementDate = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var result = await _stockManager.ConsumeStockAsync(
            actor,
            app,
            itemsToIssue,
            movementDate,
            "Complete",
            "key-multi-001",
            new { ActualDate = movementDate, Items = itemsToIssue });

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsReplay);
        Assert.Equal(2, result.Movements.Count);
        Assert.Equal(2, result.Deductions.Count);

        // Balances deducted
        Assert.Equal(50m, balance1.QuantityOnHand);
        Assert.Equal(50m, balance2.QuantityOnHand);

        // Movements recorded in store
        Assert.Equal(2, _store.StockMovements.Count);
        foreach (var mov in _store.StockMovements)
        {
            Assert.Equal(StockMovementType.Issue, mov.MovementType);
            Assert.Equal(_orgId, mov.OrganizationId);
            Assert.Equal(farm.Id, mov.FarmId);
            Assert.Equal(app.Id, mov.FertilizerApplicationId);
            Assert.Equal(movementDate, mov.MovementDate);
        }

        // Product lines on application updated with actual quantity and movement reference
        var line1 = app.Products.First(p => p.InventoryItemId == item1.Id);
        var line2 = app.Products.First(p => p.InventoryItemId == item2.Id);
        Assert.Equal(50m, line1.ActualQuantity);
        Assert.NotNull(line1.StockMovementId);
        Assert.Equal(30m, line2.ActualQuantity);
        Assert.NotNull(line2.StockMovementId);
    }

    // =========================================================================
    // 2. SHORTAGE AGGREGATION & ROLLBACK
    // =========================================================================

    [Fact]
    public async Task ConsumeStockAsync_OneLineShortage_ThrowsValidationExceptionAndRollsBackWithoutDeductions()
    {
        // Arrange
        var actor = CreateActor();
        var farm = CreateFarm();
        var area = CreateFarmArea(farm.Id);
        var item1 = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId, "Urea 46%");
        var item2 = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId, "Potash");
        var loc = CreateStorageLocation(farm.Id);

        // Line 1 has sufficient (100), Line 2 has insufficient (10 vs 40 required)
        var balance1 = new StockBalance(_orgId, farm.Id, loc.Id, item1.Id, initialQuantity: 100m);
        var balance2 = new StockBalance(_orgId, farm.Id, loc.Id, item2.Id, initialQuantity: 10m);
        _store.StockBalances.Add(balance1);
        _store.StockBalances.Add(balance2);

        var app = new FertilizerApplication(_orgId, "FERT-2026-000001", _userId, farmId: farm.Id, farmAreaId: area.Id);
        app.AddProduct(item1.Id, 50m, _userId, loc.Id);
        app.AddProduct(item2.Id, 40m, _userId, loc.Id);
        _store.Applications.Add(app);

        var itemsToIssue = new List<FertilizerStockConsumptionItem>
        {
            new(item1.Id, loc.Id, 50m),
            new(item2.Id, loc.Id, 40m)
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _stockManager.ConsumeStockAsync(
                actor,
                app,
                itemsToIssue,
                DateOnly.FromDateTime(DateTime.UtcNow),
                "Complete",
                null,
                null));

        // Shortage reported
        Assert.NotNull(ex.Errors);
        Assert.Single(ex.Errors);
        var errorMsg = ex.Errors.Values.First()[0];
        Assert.Contains("Insufficient stock", errorMsg);
        Assert.Contains("Shortage: 30", errorMsg);

        // Zero deductions: balances remain 100 and 10
        Assert.Equal(100m, balance1.QuantityOnHand);
        Assert.Equal(10m, balance2.QuantityOnHand);

        // Zero movements created
        Assert.Empty(_store.StockMovements);
    }

    [Fact]
    public async Task ConsumeStockAsync_MultiLineShortage_GathersAllShortagesInSingleException()
    {
        // Arrange
        var actor = CreateActor();
        var farm = CreateFarm();
        var area = CreateFarmArea(farm.Id);
        var item1 = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId, "Urea");
        var item2 = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId, "DAP");
        var loc = CreateStorageLocation(farm.Id);

        // item1: need 50, have 20 (shortage 30)
        // item2: need 80, have 10 (shortage 70)
        _store.StockBalances.Add(new StockBalance(_orgId, farm.Id, loc.Id, item1.Id, initialQuantity: 20m));
        _store.StockBalances.Add(new StockBalance(_orgId, farm.Id, loc.Id, item2.Id, initialQuantity: 10m));

        var app = new FertilizerApplication(_orgId, "FERT-2026-000001", _userId, farmId: farm.Id, farmAreaId: area.Id);
        app.AddProduct(item1.Id, 50m, _userId, loc.Id);
        app.AddProduct(item2.Id, 80m, _userId, loc.Id);

        var itemsToIssue = new List<FertilizerStockConsumptionItem>
        {
            new(item1.Id, loc.Id, 50m),
            new(item2.Id, loc.Id, 80m)
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _stockManager.ConsumeStockAsync(
                actor,
                app,
                itemsToIssue,
                DateOnly.FromDateTime(DateTime.UtcNow),
                "Complete",
                null,
                null));

        // Both lines appear in errors
        Assert.NotNull(ex.Errors);
        Assert.Equal(2, ex.Errors.Count);
        var allErrors = ex.Errors.Values.SelectMany(v => v).ToList();
        Assert.Contains(allErrors, e => e.Contains("Urea") && e.Contains("Shortage: 30"));
        Assert.Contains(allErrors, e => e.Contains("DAP") && e.Contains("Shortage: 70"));
        Assert.Empty(_store.StockMovements);
    }

    // =========================================================================
    // 3. ZERO AND NEGATIVE QUANTITY RULES
    // =========================================================================

    [Fact]
    public async Task ConsumeStockAsync_ZeroQuantityLine_CreatesNoMovementAndDeductsZero()
    {
        // Arrange
        var actor = CreateActor();
        var farm = CreateFarm();
        var area = CreateFarmArea(farm.Id);
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm.Id);
        var balance = new StockBalance(_orgId, farm.Id, loc.Id, item.Id, initialQuantity: 100m);
        _store.StockBalances.Add(balance);

        var app = new FertilizerApplication(_orgId, "FERT-2026-000001", _userId, farmId: farm.Id, farmAreaId: area.Id);
        app.AddProduct(item.Id, 50m, _userId, loc.Id);

        var itemsToIssue = new List<FertilizerStockConsumptionItem>
        {
            new(item.Id, loc.Id, 0m) // zero quantity
        };

        // Act
        var result = await _stockManager.ConsumeStockAsync(
            actor,
            app,
            itemsToIssue,
            DateOnly.FromDateTime(DateTime.UtcNow),
            "Complete",
            null,
            null);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Movements);
        Assert.Empty(_store.StockMovements);
        Assert.Equal(100m, balance.QuantityOnHand); // untouched
    }

    [Fact]
    public async Task ConsumeStockAsync_NegativeQuantityLine_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var actor = CreateActor();
        var farm = CreateFarm();
        var area = CreateFarmArea(farm.Id);
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm.Id);

        var app = new FertilizerApplication(_orgId, "FERT-2026-000001", _userId, farmId: farm.Id, farmAreaId: area.Id);
        var itemsToIssue = new List<FertilizerStockConsumptionItem>
        {
            new(item.Id, loc.Id, -15m) // negative quantity
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _stockManager.ConsumeStockAsync(
                actor,
                app,
                itemsToIssue,
                DateOnly.FromDateTime(DateTime.UtcNow),
                "Complete",
                null,
                null));
    }

    // =========================================================================
    // 4. DETERMINISTIC LOCK ORDERING
    // =========================================================================

    [Fact]
    public async Task ConsumeStockAsync_DeterministicLocking_LocksByLocationThenItem()
    {
        // Arrange
        var actor = CreateActor();
        var farm = CreateFarm();
        var area = CreateFarmArea(farm.Id);
        var item1 = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId, "Item 1");
        var item2 = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId, "Item 2");
        var loc1 = CreateStorageLocation(farm.Id, "Loc 1");
        var loc2 = CreateStorageLocation(farm.Id, "Loc 2");

        _store.StockBalances.Add(new StockBalance(_orgId, farm.Id, loc1.Id, item1.Id, 100m));
        _store.StockBalances.Add(new StockBalance(_orgId, farm.Id, loc2.Id, item2.Id, 100m));

        var app = new FertilizerApplication(_orgId, "FERT-2026-000001", _userId, farmId: farm.Id, farmAreaId: area.Id);
        app.AddProduct(item1.Id, 10m, _userId, loc1.Id);
        app.AddProduct(item2.Id, 20m, _userId, loc2.Id);

        // Intentionally provide items in reverse order
        var itemsToIssue = new List<FertilizerStockConsumptionItem>
        {
            new(item2.Id, loc2.Id, 20m),
            new(item1.Id, loc1.Id, 10m)
        };

        // Act
        await _stockManager.ConsumeStockAsync(
            actor,
            app,
            itemsToIssue,
            DateOnly.FromDateTime(DateTime.UtcNow),
            "Complete",
            null,
            null);

        // Assert: locks acquired in deterministic order
        Assert.Equal(2, _store.LockOrderLog.Count);
        var expectedFirstLoc = loc1.Id.CompareTo(loc2.Id) < 0 ? loc1.Id : loc2.Id;
        Assert.Equal(expectedFirstLoc, _store.LockOrderLog[0].LocationId);
    }

    // =========================================================================
    // 5. OPERATION-SCOPED IDEMPOTENCY (SAFE RETRY VS KEY MISMATCH)
    // =========================================================================

    [Fact]
    public async Task ConsumeStockAsync_SameKeySamePayload_ReturnsSafeRetryWithoutDoubleDeduction()
    {
        // Arrange
        var actor = CreateActor();
        var farm = CreateFarm();
        var area = CreateFarmArea(farm.Id);
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm.Id);
        var balance = new StockBalance(_orgId, farm.Id, loc.Id, item.Id, initialQuantity: 100m);
        _store.StockBalances.Add(balance);

        var app = new FertilizerApplication(_orgId, "FERT-2026-000001", _userId, farmId: farm.Id, farmAreaId: area.Id);
        app.AddProduct(item.Id, 25m, _userId, loc.Id);

        var itemsToIssue = new List<FertilizerStockConsumptionItem>
        {
            new(item.Id, loc.Id, 25m)
        };
        var payload = new { Items = itemsToIssue, Date = "2026-10-20" };

        // Act 1: Initial call
        var result1 = await _stockManager.ConsumeStockAsync(
            actor,
            app,
            itemsToIssue,
            DateOnly.FromDateTime(DateTime.UtcNow),
            "Complete",
            "idem-key-123",
            payload);

        Assert.False(result1.IsReplay);
        Assert.Equal(75m, balance.QuantityOnHand);
        Assert.Single(_store.StockMovements);

        // Act 2: Retry with exact same key and payload
        var result2 = await _stockManager.ConsumeStockAsync(
            actor,
            app,
            itemsToIssue,
            DateOnly.FromDateTime(DateTime.UtcNow),
            "Complete",
            "idem-key-123",
            payload);

        // Assert: Safe replay
        Assert.True(result2.IsReplay);
        // Stock is NOT deducted twice! Remains 75m
        Assert.Equal(75m, balance.QuantityOnHand);
        // No duplicate movements created! Still exactly 1
        Assert.Single(_store.StockMovements);
    }

    [Fact]
    public async Task ConsumeStockAsync_SameKeyDifferentPayload_ThrowsConflictException()
    {
        // Arrange
        var actor = CreateActor();
        var farm = CreateFarm();
        var area = CreateFarmArea(farm.Id);
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm.Id);
        _store.StockBalances.Add(new StockBalance(_orgId, farm.Id, loc.Id, item.Id, initialQuantity: 100m));

        var app = new FertilizerApplication(_orgId, "FERT-2026-000001", _userId, farmId: farm.Id, farmAreaId: area.Id);
        app.AddProduct(item.Id, 25m, _userId, loc.Id);

        var itemsToIssue = new List<FertilizerStockConsumptionItem> { new(item.Id, loc.Id, 25m) };
        var payloadA = new { Items = itemsToIssue, Note = "First Attempt" };

        // Initial call with payload A
        await _stockManager.ConsumeStockAsync(
            actor,
            app,
            itemsToIssue,
            DateOnly.FromDateTime(DateTime.UtcNow),
            "Complete",
            "idem-key-123",
            payloadA);

        // Retry with same key but DIFFERENT payload B
        var payloadB = new { Items = itemsToIssue, Note = "Different Attempt" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            _stockManager.ConsumeStockAsync(
                actor,
                app,
                itemsToIssue,
                DateOnly.FromDateTime(DateTime.UtcNow),
                "Complete",
                "idem-key-123",
                payloadB));

        Assert.Contains("already exists with different request parameters", ex.Message);
    }

    // =========================================================================
    // 6. LEDGER-LINK CONSISTENCY
    // =========================================================================

    [Fact]
    public async Task ConsumeStockAsync_LedgerLinkConsistency_AllMovementsCorrectlyLinked()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, plantation, cycle, stage) = CreateValidHierarchyChain();
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm.Id);
        _store.StockBalances.Add(new StockBalance(_orgId, farm.Id, loc.Id, item.Id, initialQuantity: 100m));

        var app = new FertilizerApplication(
            _orgId, "FERT-2026-000001", _userId,
            farmId: farm.Id, farmAreaId: area.Id, plantationId: plantation.Id,
            cropCycleId: cycle.Id, cropCycleStageId: stage.Id);
        app.AddProduct(item.Id, 45m, _userId, loc.Id);
        _store.Applications.Add(app);

        var movementDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var itemsToIssue = new List<FertilizerStockConsumptionItem> { new(item.Id, loc.Id, 45m) };

        // Act
        var result = await _stockManager.ConsumeStockAsync(
            actor,
            app,
            itemsToIssue,
            movementDate,
            "Complete",
            null,
            null,
            notes: "Custom field application note");

        // Assert
        Assert.Single(result.Movements);
        var mov = result.Movements[0];
        Assert.Equal(_orgId, mov.OrganizationId);
        Assert.Equal(farm.Id, mov.FarmId);
        Assert.Equal(area.Id, mov.FarmAreaId);
        Assert.Equal(plantation.Id, mov.PlantationId);
        Assert.Equal(cycle.Id, mov.CropCycleId);
        Assert.Equal(stage.Id, mov.CropCycleStageId);
        Assert.Equal(app.Id, mov.FertilizerApplicationId);
        Assert.Equal(loc.Id, mov.StorageLocationId);
        Assert.Equal(item.Id, mov.InventoryItemId);
        Assert.Equal(item.StockUnitId, mov.StockUnitId);
        Assert.Equal(45m, mov.Quantity);
        Assert.Equal(movementDate, mov.MovementDate);
        Assert.Equal(app.ReferenceNumber, mov.ReferenceNumber);
        Assert.Equal("Custom field application note", mov.Notes);
    }

    // =========================================================================
    // 7. START IDEMPOTENCY REPLAY AND CONFLICT
    // =========================================================================

    [Fact]
    public async Task StartAsync_WithIdempotencyKey_SameKeySameRequest_ReturnsSafeRetry()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, plantation, cycle, stage) = CreateValidHierarchyChain();
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm.Id);

        _store.StockBalances.Add(new StockBalance(_orgId, farm.Id, loc.Id, item.Id, initialQuantity: 200m));

        var app = new FertilizerApplication(
            _orgId, "FERT-2026-000001", _userId,
            farmId: farm.Id, farmAreaId: area.Id, plantationId: plantation.Id,
            cropCycleId: cycle.Id, cropCycleStageId: stage.Id,
            applicationMethodId: method.Id,
            plannedDate: DateTimeOffset.UtcNow.AddDays(1));
        app.AddProduct(item.Id, 50m, _userId, loc.Id);
        SetPrivateProperty(app, nameof(FertilizerApplication.Status), FertilizerApplicationStatus.Planned);
        _store.Applications.Add(app);

        var request = new StartFertilizerApplicationRequest(Version: 1, IdempotencyKey: "start-key-001");

        // Act 1: Initial Start
        var result1 = await _sut.StartAsync(actor, app.Id, request, null);
        Assert.Equal(FertilizerApplicationStatus.InProgress, result1.Status);

        // Act 2: Idempotent replay with same key and request
        var result2 = await _sut.StartAsync(actor, app.Id, request, null);
        Assert.Equal(FertilizerApplicationStatus.InProgress, result2.Status);
        Assert.Equal(result1.Id, result2.Id);
    }

    [Fact]
    public async Task StartAsync_WithIdempotencyKey_SameKeyDifferentRequest_ThrowsConflictException()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, plantation, cycle, stage) = CreateValidHierarchyChain();
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm.Id);

        _store.StockBalances.Add(new StockBalance(_orgId, farm.Id, loc.Id, item.Id, initialQuantity: 200m));

        var app = new FertilizerApplication(
            _orgId, "FERT-2026-000001", _userId,
            farmId: farm.Id, farmAreaId: area.Id, plantationId: plantation.Id,
            cropCycleId: cycle.Id, cropCycleStageId: stage.Id,
            applicationMethodId: method.Id,
            plannedDate: DateTimeOffset.UtcNow.AddDays(1));
        app.AddProduct(item.Id, 50m, _userId, loc.Id);
        SetPrivateProperty(app, nameof(FertilizerApplication.Status), FertilizerApplicationStatus.Planned);
        _store.Applications.Add(app);

        var request1 = new StartFertilizerApplicationRequest(Version: 1, IdempotencyKey: "start-key-001");
        await _sut.StartAsync(actor, app.Id, request1, null);

        // Different request version
        var request2 = new StartFertilizerApplicationRequest(Version: 99, IdempotencyKey: "start-key-001");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.StartAsync(actor, app.Id, request2, null));

        Assert.Contains("already exists with different request parameters", ex.Message);
    }

    // =========================================================================
    // HELPER METHODS & TEST FIXTURES
    // =========================================================================

    private Farm CreateFarm()
    {
        var farm = new Farm(_orgId, "Valley Farm", Guid.NewGuid(), Guid.NewGuid(), 100m, Guid.NewGuid());
        _store.Farms.Add(farm);
        return farm;
    }

    private FarmArea CreateFarmArea(Guid farmId)
    {
        var area = new FarmArea(_orgId, farmId, null, "East Plot", 25m, Guid.NewGuid(), _userId);
        _store.FarmAreas.Add(area);
        return area;
    }

    private CropPlantation CreatePlantation(Guid farmId, Guid farmAreaId)
    {
        var plantation = new CropPlantation(
            _orgId, farmId, farmAreaId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Orchard Plot A", 10m, Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), null, _userId);
        _store.Plantations.Add(plantation);
        return plantation;
    }

    private CropCycle CreateCropCycle(Guid plantationId)
    {
        var cycle = new CropCycle(_orgId, plantationId, "Cycle 2026-1", 2026, "Spring", DateOnly.FromDateTime(DateTime.UtcNow), null, _userId);
        _store.CropCycles.Add(cycle);
        return cycle;
    }

    private CropCycleStage CreateCropCycleStage(Guid cropCycleId)
    {
        var stage = new CropCycleStage(cropCycleId, Guid.NewGuid(), "Vegetative", 1, 14, DateOnly.FromDateTime(DateTime.UtcNow), null, _userId);
        _store.CropCycleStages.Add(stage);
        return stage;
    }

    private (Farm, FarmArea, CropPlantation, CropCycle, CropCycleStage) CreateValidHierarchyChain()
    {
        var farm = CreateFarm();
        var area = CreateFarmArea(farm.Id);
        var plantation = CreatePlantation(farm.Id, area.Id);
        var cycle = CreateCropCycle(plantation.Id);
        var stage = CreateCropCycleStage(cycle.Id);
        return (farm, area, plantation, cycle, stage);
    }

    private InventoryItem CreateInventoryItem(Guid categoryId, string name = "Urea 46%")
    {
        var item = new InventoryItem(_orgId, name, Guid.NewGuid(), _userId, "UREA-46", null, categoryId);
        _store.InventoryItems.Add(item);
        return item;
    }

    private StorageLocation CreateStorageLocation(Guid farmId, string name = "North Silo")
    {
        var loc = new StorageLocation(_orgId, farmId, name, _userId, "Main Storage");
        _store.StorageLocations.Add(loc);
        return loc;
    }

    private ApplicationMethod CreateMethod(string code, string name)
    {
        var method = new ApplicationMethod(_orgId, code, name, isSystem: false, description: null, displayOrder: 1, createdBy: _userId, module: "FERTILIZER");
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

    private sealed class FakeFertilizerStore : IFertilizerApplicationStore
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
        public List<(Guid LocationId, Guid ItemId)> LockOrderLog { get; } = [];

        public Task<FertilizerApplication?> FindAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Applications.FirstOrDefault(a => a.Id == id && a.OrganizationId == organizationId));

        public Task<int> CountAsync(Guid organizationId, FertilizerApplicationListQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult(Applications.Count(a => a.OrganizationId == organizationId));

        public Task<IReadOnlyList<FertilizerApplication>> ListAsync(Guid organizationId, FertilizerApplicationListQuery query, int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FertilizerApplication>>(Applications.Where(a => a.OrganizationId == organizationId).Skip(skip).Take(take).ToList());

        public Task<IReadOnlyList<FertilizerApplication>> ListForExportAsync(Guid organizationId, FertilizerApplicationListQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FertilizerApplication>>(Applications.Where(a => a.OrganizationId == organizationId).ToList());

        public Task<IReadOnlyList<FertilizerProductLookupResponse>> ListProductsLookupAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FertilizerProductLookupResponse>>([]);

        public Task<IReadOnlyList<FertilizerStorageLocationLookupResponse>> ListStorageLocationsLookupAsync(Guid organizationId, Guid farmId, Guid? inventoryItemId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FertilizerStorageLocationLookupResponse>>([]);

        public Task<IReadOnlyList<FertilizerApplicationMethodLookupResponse>> ListApplicationMethodsLookupAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FertilizerApplicationMethodLookupResponse>>([]);

        public Task<IReadOnlyList<FertilizerIrrigationLookupResponse>> ListIrrigationsLookupAsync(Guid organizationId, Guid? farmId, Guid? farmAreaId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FertilizerIrrigationLookupResponse>>([]);

        public Task<IReadOnlyDictionary<Guid, string>> GetUserNamesAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());

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

        public Task<ApplicationMethod?> FindApplicationMethodAsync(Guid applicationMethodId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ApplicationMethods.FirstOrDefault(m => m.Id == applicationMethodId && ((m.IsSystem && m.OrganizationId == null) || m.OrganizationId == organizationId)));

        public Task<Unit?> FindUnitAsync(Guid unitId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Units.FirstOrDefault(u => u.Id == unitId && ((u.IsSystem && u.OrganizationId == null) || u.OrganizationId == organizationId)));

        public Task<InventoryItem?> FindInventoryItemAsync(Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(InventoryItems.FirstOrDefault(i => i.Id == inventoryItemId && i.OrganizationId == organizationId));

        public Task<bool> HasActivePlantProtectionProfileAsync(Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ActivePppItemIds.Contains(inventoryItemId));

        public Task<StorageLocation?> FindStorageLocationAsync(Guid storageLocationId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(StorageLocations.FirstOrDefault(l => l.Id == storageLocationId && l.OrganizationId == organizationId));

        public Task<IrrigationEvent?> FindIrrigationEventAsync(Guid irrigationEventId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(IrrigationEvents.FirstOrDefault(e => e.Id == irrigationEventId && e.OrganizationId == organizationId));

        public Task<StockBalance?> FindStockBalanceAsync(Guid storageLocationId, Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(StockBalances.FirstOrDefault(sb => sb.StorageLocationId == storageLocationId && sb.InventoryItemId == inventoryItemId && sb.OrganizationId == organizationId));

        public Task<StockBalance?> LockBalanceAsync(Guid storageLocationId, Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default)
        {
            LockOrderLog.Add((storageLocationId, inventoryItemId));
            return Task.FromResult(StockBalances.FirstOrDefault(sb => sb.StorageLocationId == storageLocationId && sb.InventoryItemId == inventoryItemId && sb.OrganizationId == organizationId));
        }

        public Task AcquireAdvisoryLockAsync(Guid storageLocationId, Guid inventoryItemId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) =>
            await operation(cancellationToken);

        public void Add(FertilizerApplication application) => Applications.Add(application);

        public void Remove(FertilizerApplication application) => Applications.Remove(application);

        public void AddProduct(FertilizerApplicationProduct product)
        {
        }

        public void RemoveProduct(FertilizerApplicationProduct product)
        {
            var app = Applications.FirstOrDefault(a => a.Id == product.FertilizerApplicationId);
            if (app is not null)
            {
                app.RemoveProduct(product.InventoryItemId);
            }
        }

        public void AddMovement(StockMovement movement) => StockMovements.Add(movement);

        public Task<FertilizerOperationIdempotencyRecord?> FindIdempotencyRecordAsync(Guid organizationId, string operationType, string idempotencyKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(IdempotencyRecords.FirstOrDefault(r => r.OrganizationId == organizationId && r.OperationType == operationType && r.IdempotencyKey == idempotencyKey));

        public void AddIdempotencyRecord(FertilizerOperationIdempotencyRecord record) => IdempotencyRecords.Add(record);

        public void AddAuditLog(AuditLog auditLog) => AuditLogs.Add(auditLog);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
