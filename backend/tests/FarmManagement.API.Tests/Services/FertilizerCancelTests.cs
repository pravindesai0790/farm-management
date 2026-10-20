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

public class FertilizerCancelTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly FertilizerApplicationServiceTests.FakeFertilizerStore _store = new();
    private readonly FertilizerStockManager _stockManager;
    private readonly FertilizerApplicationService _sut;

    public FertilizerCancelTests()
    {
        _stockManager = new FertilizerStockManager(_store);
        var validator = new FertilizerValidator(_store);
        _sut = new FertilizerApplicationService(_store, validator, new TestReferenceGenerator(), _stockManager);
    }

    private FertilizerActor CreateActor(Guid? orgId = null) =>
        new(_userId, orgId ?? _orgId);

    // =========================================================================
    // 1. ZERO-USAGE CANCELLATION FROM ALLOWED SOURCE STATUSES
    // =========================================================================

    [Theory]
    [InlineData(FertilizerApplicationStatus.Draft)]
    [InlineData(FertilizerApplicationStatus.Planned)]
    [InlineData(FertilizerApplicationStatus.Scheduled)]
    public async Task CancelAsync_FromPreStartStatus_WithZeroUsage_CancelsWithoutStockDeduction(FertilizerApplicationStatus sourceStatus)
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm.Id);

        var app = new FertilizerApplication(_orgId, "FERT-2026-000001", _userId, farmId: farm.Id, farmAreaId: area.Id);
        app.AddProduct(item.Id, plannedQuantity: 100m, createdBy: _userId);
        SetPrivateProperty(app, nameof(FertilizerApplication.Status), sourceStatus);
        _store.Applications.Add(app);

        var request = new CancelFertilizerApplicationRequest(
            CancellationReason: "Cancelled due to unexpected heavy rain",
            Products: [new(item.Id, ActualQuantity: 0m)]);

        // Act
        var result = await _sut.CancelAsync(actor, app.Id, request, null);

        // Assert
        Assert.Equal(FertilizerApplicationStatus.Cancelled, result.Status);
        Assert.Equal("CANCELLED", result.StatusName);
        Assert.Equal("Cancelled due to unexpected heavy rain", result.CancellationReason);
        Assert.NotNull(result.CancelledAt);
        Assert.Equal(_userId, result.CancelledBy);
        Assert.Empty(_store.StockMovements);
    }

    [Fact]
    public async Task CancelAsync_FromInProgress_WithZeroUsage_CancelsWithoutStockDeduction()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm.Id);
        CreateStockBalance(farm.Id, loc.Id, item.Id, 200m);

        var app = CreateInProgressApplication(farm.Id, area.Id, item.Id, loc.Id, 50m);
        _store.Applications.Add(app);

        var request = new CancelFertilizerApplicationRequest(
            CancellationReason: "Equipment broke down before any fertilizer was applied",
            Products: [new(item.Id, ActualQuantity: 0m)]);

        // Act
        var result = await _sut.CancelAsync(actor, app.Id, request, null);

        // Assert
        Assert.Equal(FertilizerApplicationStatus.Cancelled, result.Status);
        Assert.Equal("Equipment broke down before any fertilizer was applied", result.CancellationReason);
        Assert.Empty(_store.StockMovements);
    }

    // =========================================================================
    // 2. INVALID SOURCE STATUS & CONCURRENCY
    // =========================================================================

    [Theory]
    [InlineData(FertilizerApplicationStatus.Completed)]
    [InlineData(FertilizerApplicationStatus.Cancelled)]
    public async Task CancelAsync_WhenAlreadyCompletedOrCancelled_ThrowsConflictException(FertilizerApplicationStatus invalidStatus)
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var app = new FertilizerApplication(_orgId, "FERT-2026-000001", _userId, farmId: farm.Id, farmAreaId: area.Id);
        SetPrivateProperty(app, nameof(FertilizerApplication.Status), invalidStatus);
        _store.Applications.Add(app);

        var request = new CancelFertilizerApplicationRequest(CancellationReason: "Try to cancel terminal state");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.CancelAsync(actor, app.Id, request, null));

        Assert.Contains("cannot be cancelled", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CancelAsync_WhenVersionMismatch_ThrowsConflictException()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var app = new FertilizerApplication(_orgId, "FERT-2026-000001", _userId, farmId: farm.Id, farmAreaId: area.Id);
        SetPrivateProperty(app, nameof(FertilizerApplication.Version), (uint)5);
        _store.Applications.Add(app);

        var request = new CancelFertilizerApplicationRequest(
            CancellationReason: "Valid reason",
            Version: 2); // stale version

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.CancelAsync(actor, app.Id, request, null));

        Assert.Contains("modified by another user", ex.Message);
    }

    // =========================================================================
    // 3. VALIDATION RULES (REASON & ACTUAL QUANTITY)
    // =========================================================================

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CancelAsync_WhenCancellationReasonMissingOrWhitespace_ThrowsValidationException(string? invalidReason)
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var app = new FertilizerApplication(_orgId, "FERT-2026-000001", _userId, farmId: farm.Id, farmAreaId: area.Id);
        _store.Applications.Add(app);

        var request = new CancelFertilizerApplicationRequest(CancellationReason: invalidReason!);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CancelAsync(actor, app.Id, request, null));

        Assert.True(ex.Errors.ContainsKey("cancellationReason"));
    }

    [Fact]
    public async Task CancelAsync_WhenNegativeActualQuantity_ThrowsValidationException()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var app = new FertilizerApplication(_orgId, "FERT-2026-000001", _userId, farmId: farm.Id, farmAreaId: area.Id);
        app.AddProduct(item.Id, plannedQuantity: 50m, createdBy: _userId);
        _store.Applications.Add(app);

        var request = new CancelFertilizerApplicationRequest(
            CancellationReason: "Valid reason",
            Products: [new(item.Id, ActualQuantity: -10m)]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CancelAsync(actor, app.Id, request, null));

        Assert.True(ex.Errors.ContainsKey("products"));
    }

    // =========================================================================
    // 4. POSITIVE USAGE DATE & STORAGE LOCATION VALIDATION
    // =========================================================================

    [Fact]
    public async Task CancelAsync_PreStart_PositiveUsageWithoutActualDate_ThrowsValidationException()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm.Id);

        var app = new FertilizerApplication(_orgId, "FERT-2026-000001", _userId, farmId: farm.Id, farmAreaId: area.Id);
        app.AddProduct(item.Id, plannedQuantity: 50m, createdBy: _userId);
        _store.Applications.Add(app);

        var request = new CancelFertilizerApplicationRequest(
            CancellationReason: "Partial usage before aborting",
            ActualApplicationDate: null,
            Products: [new(item.Id, ActualQuantity: 20m, StorageLocationId: loc.Id)]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CancelAsync(actor, app.Id, request, null));

        Assert.True(ex.Errors.ContainsKey("actualApplicationDate"));
    }

    [Fact]
    public async Task CancelAsync_PreStart_PositiveUsageWithFutureActualDate_ThrowsValidationException()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm.Id);

        var app = new FertilizerApplication(_orgId, "FERT-2026-000001", _userId, farmId: farm.Id, farmAreaId: area.Id);
        app.AddProduct(item.Id, plannedQuantity: 50m, createdBy: _userId);
        _store.Applications.Add(app);

        var request = new CancelFertilizerApplicationRequest(
            CancellationReason: "Partial usage",
            ActualApplicationDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
            Products: [new(item.Id, ActualQuantity: 20m, StorageLocationId: loc.Id)]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CancelAsync(actor, app.Id, request, null));

        Assert.True(ex.Errors.ContainsKey("actualApplicationDate"));
    }

    [Fact]
    public async Task CancelAsync_PreStart_PositiveUsageWithoutStorageLocation_ThrowsValidationException()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);

        var app = new FertilizerApplication(_orgId, "FERT-2026-000001", _userId, farmId: farm.Id, farmAreaId: area.Id);
        app.AddProduct(item.Id, plannedQuantity: 50m, createdBy: _userId);
        _store.Applications.Add(app);

        var request = new CancelFertilizerApplicationRequest(
            CancellationReason: "Partial usage",
            ActualApplicationDate: DateOnly.FromDateTime(DateTime.UtcNow),
            Products: [new(item.Id, ActualQuantity: 20m, StorageLocationId: null)]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CancelAsync(actor, app.Id, request, null));

        Assert.True(ex.Errors.ContainsKey("products"));
    }

    [Fact]
    public async Task CancelAsync_PreStart_PositiveUsageWithForeignStorageLocation_ThrowsValidationException()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var foreignFarmId = Guid.NewGuid();
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var foreignLoc = CreateStorageLocation(foreignFarmId);

        var app = new FertilizerApplication(_orgId, "FERT-2026-000001", _userId, farmId: farm.Id, farmAreaId: area.Id);
        app.AddProduct(item.Id, plannedQuantity: 50m, createdBy: _userId);
        _store.Applications.Add(app);

        var request = new CancelFertilizerApplicationRequest(
            CancellationReason: "Partial usage",
            ActualApplicationDate: DateOnly.FromDateTime(DateTime.UtcNow),
            Products: [new(item.Id, ActualQuantity: 20m, StorageLocationId: foreignLoc.Id)]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CancelAsync(actor, app.Id, request, null));

        Assert.True(ex.Errors.ContainsKey("storageLocationId"));
    }

    // =========================================================================
    // 5. POSITIVE USAGE EXECUTION & STOCK DEDUCTIONS
    // =========================================================================

    [Fact]
    public async Task CancelAsync_PreStart_PositiveUsageSuccess_DeductsStockAndCreatesMovements()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm.Id);
        CreateStockBalance(farm.Id, loc.Id, item.Id, 100m);

        var app = new FertilizerApplication(_orgId, "FERT-2026-000001", _userId, farmId: farm.Id, farmAreaId: area.Id);
        app.AddProduct(item.Id, plannedQuantity: 50m, createdBy: _userId);
        _store.Applications.Add(app);

        var applyDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var request = new CancelFertilizerApplicationRequest(
            CancellationReason: "Tank leaked after spreading 25kg",
            ActualApplicationDate: applyDate,
            Products: [new(item.Id, ActualQuantity: 25m, StorageLocationId: loc.Id)]);

        // Act
        var result = await _sut.CancelAsync(actor, app.Id, request, null);

        // Assert
        Assert.Equal(FertilizerApplicationStatus.Cancelled, result.Status);
        Assert.Equal("Tank leaked after spreading 25kg", result.CancellationReason);
        Assert.Equal(applyDate, result.ActualApplicationDate);

        // Balance updated
        var bal = _store.StockBalances.First(b => b.StorageLocationId == loc.Id && b.InventoryItemId == item.Id);
        Assert.Equal(75m, bal.QuantityOnHand);

        // Movement created
        Assert.Single(_store.StockMovements);
        var mov = _store.StockMovements[0];
        Assert.Equal(StockMovementType.Issue, mov.MovementType);
        Assert.Equal(25m, mov.Quantity);
        Assert.Equal(app.Id, mov.FertilizerApplicationId);
    }

    [Fact]
    public async Task CancelAsync_InProgress_PositiveUsageSuccess_UsesSavedLocationsAndDeductsStock()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm.Id);
        CreateStockBalance(farm.Id, loc.Id, item.Id, 100m);

        var app = CreateInProgressApplication(farm.Id, area.Id, item.Id, loc.Id, 60m);
        _store.Applications.Add(app);

        var applyDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var request = new CancelFertilizerApplicationRequest(
            CancellationReason: "Tractor broke down after applying 15kg",
            ActualApplicationDate: applyDate,
            Products: [new(item.Id, ActualQuantity: 15m)]); // StorageLocationId omitted; uses frozen location

        // Act
        var result = await _sut.CancelAsync(actor, app.Id, request, null);

        // Assert
        Assert.Equal(FertilizerApplicationStatus.Cancelled, result.Status);
        Assert.Equal(15m, result.Products[0].ActualQuantity);
        Assert.Equal(loc.Id, result.Products[0].StorageLocationId);

        var bal = _store.StockBalances.First(b => b.StorageLocationId == loc.Id && b.InventoryItemId == item.Id);
        Assert.Equal(85m, bal.QuantityOnHand);

        Assert.Single(_store.StockMovements);
        Assert.Equal(15m, _store.StockMovements[0].Quantity);
    }

    [Fact]
    public async Task CancelAsync_InProgress_PositiveUsage_StockShortage_RollsBackAndThrowsValidationException()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm.Id);
        CreateStockBalance(farm.Id, loc.Id, item.Id, 10m); // Only 10 in stock

        var app = CreateInProgressApplication(farm.Id, area.Id, item.Id, loc.Id, 50m);
        _store.Applications.Add(app);

        var request = new CancelFertilizerApplicationRequest(
            CancellationReason: "Abort",
            ActualApplicationDate: DateOnly.FromDateTime(DateTime.UtcNow),
            Products: [new(item.Id, ActualQuantity: 25m)]); // Shortage of 15m

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CancelAsync(actor, app.Id, request, null));

        Assert.Contains("insufficient stock", ex.Message, StringComparison.OrdinalIgnoreCase);

        // Status unchanged
        Assert.Equal(FertilizerApplicationStatus.InProgress, app.Status);

        // Zero movements created
        Assert.Empty(_store.StockMovements);

        // Balance untouched
        var bal = _store.StockBalances.First(b => b.StorageLocationId == loc.Id && b.InventoryItemId == item.Id);
        Assert.Equal(10m, bal.QuantityOnHand);
    }

    // =========================================================================
    // 6. IDEMPOTENCY REPLAY & PAYLOAD CONFLICT
    // =========================================================================

    [Fact]
    public async Task CancelAsync_IdempotencyReplay_SamePayload_ReturnsReplayWithoutDuplicateDeduction()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm.Id);
        CreateStockBalance(farm.Id, loc.Id, item.Id, 100m);

        var app = CreateInProgressApplication(farm.Id, area.Id, item.Id, loc.Id, 50m);
        _store.Applications.Add(app);

        var idempotencyKey = Guid.NewGuid().ToString("N");
        var applyDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var request = new CancelFertilizerApplicationRequest(
            CancellationReason: "Idempotent cancellation",
            ActualApplicationDate: applyDate,
            Products: [new(item.Id, ActualQuantity: 20m)],
            IdempotencyKey: idempotencyKey);

        // First call
        var firstResult = await _sut.CancelAsync(actor, app.Id, request, null);
        Assert.Equal(FertilizerApplicationStatus.Cancelled, firstResult.Status);
        Assert.Single(_store.StockMovements);
        Assert.Equal(80m, _store.StockBalances.First(b => b.StorageLocationId == loc.Id && b.InventoryItemId == item.Id).QuantityOnHand);

        // Act - Replay with same key & payload
        var replayResult = await _sut.CancelAsync(actor, app.Id, request, null);

        // Assert
        Assert.Equal(firstResult.Id, replayResult.Id);
        Assert.Equal(FertilizerApplicationStatus.Cancelled, replayResult.Status);
        Assert.Single(_store.StockMovements); // Still exactly one movement
        Assert.Equal(80m, _store.StockBalances.First(b => b.StorageLocationId == loc.Id && b.InventoryItemId == item.Id).QuantityOnHand);
    }

    [Fact]
    public async Task CancelAsync_IdempotencyConflict_DifferentPayload_ThrowsConflictException()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm.Id);
        CreateStockBalance(farm.Id, loc.Id, item.Id, 100m);

        var app = CreateInProgressApplication(farm.Id, area.Id, item.Id, loc.Id, 50m);
        _store.Applications.Add(app);

        var idempotencyKey = Guid.NewGuid().ToString("N");
        var applyDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var request1 = new CancelFertilizerApplicationRequest(
            CancellationReason: "Initial payload",
            ActualApplicationDate: applyDate,
            Products: [new(item.Id, ActualQuantity: 20m)],
            IdempotencyKey: idempotencyKey);

        await _sut.CancelAsync(actor, app.Id, request1, null);

        var request2 = new CancelFertilizerApplicationRequest(
            CancellationReason: "Mutated payload with different reason",
            ActualApplicationDate: applyDate,
            Products: [new(item.Id, ActualQuantity: 20m)],
            IdempotencyKey: idempotencyKey);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.CancelAsync(actor, app.Id, request2, null));

        Assert.Contains("different request parameters", ex.Message);
    }

    // =========================================================================
    // 7. AUDIT LOGGING
    // =========================================================================

    [Fact]
    public async Task CancelAsync_AuditLogging_RecordsFertilizerCancelledWithReasonAndUsage()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm.Id);
        CreateStockBalance(farm.Id, loc.Id, item.Id, 100m);

        var app = CreateInProgressApplication(farm.Id, area.Id, item.Id, loc.Id, 40m);
        _store.Applications.Add(app);

        var request = new CancelFertilizerApplicationRequest(
            CancellationReason: "Wind speed exceeded safe threshold after spreading 10kg",
            ActualApplicationDate: DateOnly.FromDateTime(DateTime.UtcNow),
            Products: [new(item.Id, ActualQuantity: 10m)]);

        // Act
        await _sut.CancelAsync(actor, app.Id, request, null);

        // Assert
        var audit = Assert.Single(_store.AuditLogs, a => a.Action == "Fertilizer.Cancelled");
        Assert.Equal("FertilizerApplication", audit.EntityType);
        Assert.Equal(app.Id, audit.EntityId);
        Assert.Contains("Wind speed exceeded safe threshold", audit.Details.RootElement.ToString());
    }

    // =========================================================================
    // HELPER METHODS
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

    private (Farm Farm, FarmArea Area, CropPlantation Plantation, CropCycle Cycle, CropCycleStage Stage) CreateValidHierarchyChain()
    {
        var farm = CreateFarm();
        var area = CreateFarmArea(farm.Id);
        var plantation = new CropPlantation(
            _orgId, farm.Id, area.Id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Orchard Plot A", 10m, Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), null, _userId);
        _store.Plantations.Add(plantation);
        var cycle = new CropCycle(_orgId, plantation.Id, "Cycle 2026-1", 2026, "Spring", DateOnly.FromDateTime(DateTime.UtcNow), null, _userId);
        _store.CropCycles.Add(cycle);
        var stage = new CropCycleStage(cycle.Id, Guid.NewGuid(), "Vegetative", 1, 14, DateOnly.FromDateTime(DateTime.UtcNow), null, _userId);
        _store.CropCycleStages.Add(stage);

        return (farm, area, plantation, cycle, stage);
    }

    private InventoryItem CreateInventoryItem(Guid categoryId, string name = "Urea 46%")
    {
        var item = new InventoryItem(_orgId, name, Guid.NewGuid(), _userId, "UREA-46", null, categoryId);
        _store.InventoryItems.Add(item);
        return item;
    }

    private StorageLocation CreateStorageLocation(Guid farmId, string name = "Shed A")
    {
        var loc = new StorageLocation(_orgId, farmId, name, _userId, "Main Storage");
        _store.StorageLocations.Add(loc);
        return loc;
    }

    private StockBalance CreateStockBalance(Guid farmId, Guid storageLocationId, Guid inventoryItemId, decimal balance)
    {
        var sb = new StockBalance(_orgId, farmId, storageLocationId, inventoryItemId, initialQuantity: balance);
        _store.StockBalances.Add(sb);
        return sb;
    }

    private ApplicationMethod CreateMethod(string code, string name)
    {
        var method = new ApplicationMethod(_orgId, code, name, isSystem: false, description: null, displayOrder: 1, createdBy: _userId, module: "FERTILIZER");
        _store.ApplicationMethods.Add(method);
        return method;
    }

    private FertilizerApplication CreateInProgressApplication(Guid farmId, Guid farmAreaId, Guid inventoryItemId, Guid storageLocationId, decimal plannedQty)
    {
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var app = new FertilizerApplication(
            _orgId, "FERT-2026-000001", _userId,
            farmId: farmId, farmAreaId: farmAreaId,
            applicationMethodId: method.Id,
            plannedDate: DateOnly.FromDateTime(DateTime.UtcNow));
        app.AddProduct(inventoryItemId, plannedQty, _userId, storageLocationId);

        SetPrivateProperty(app, nameof(FertilizerApplication.Status), FertilizerApplicationStatus.InProgress);
        SetPrivateProperty(app, nameof(FertilizerApplication.StartedAt), DateTimeOffset.UtcNow.AddHours(-2));
        SetPrivateProperty(app, nameof(FertilizerApplication.StartedBy), _userId);

        return app;
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
}
