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

public class FertilizerCompleteTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly FakeFertilizerStore _store = new();
    private readonly FertilizerStockManager _stockManager;
    private readonly FertilizerApplicationService _sut;

    public FertilizerCompleteTests()
    {
        _stockManager = new FertilizerStockManager(_store);
        var validator = new FertilizerValidator(_store);
        _sut = new FertilizerApplicationService(_store, validator, new TestReferenceGenerator(), _stockManager);
    }

    private FertilizerActor CreateActor(Guid? orgId = null) =>
        new(_userId, orgId ?? _orgId);

    // =========================================================================
    // 1. STATUS & CONCURRENCY INVARIANTS
    // =========================================================================

    [Theory]
    [InlineData(FertilizerApplicationStatus.Draft)]
    [InlineData(FertilizerApplicationStatus.Planned)]
    [InlineData(FertilizerApplicationStatus.Scheduled)]
    [InlineData(FertilizerApplicationStatus.Completed)]
    [InlineData(FertilizerApplicationStatus.Cancelled)]
    public async Task CompleteAsync_WhenStatusNotPending_ThrowsConflictException(FertilizerApplicationStatus invalidStatus)
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var app = new FertilizerApplication(_orgId, "FERT-2026-000001", _userId, farmId: farm.Id, farmAreaId: area.Id);
        SetPrivateProperty(app, nameof(FertilizerApplication.Status), invalidStatus);
        _store.Applications.Add(app);

        var request = new CompleteFertilizerApplicationRequest(
            ActualApplicationDate: DateTimeOffset.UtcNow);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.CompleteAsync(actor, app.Id, request, null));

        Assert.Contains("Only in-progress fertilizer applications can be completed", ex.Message);
    }

    [Fact]
    public async Task CompleteAsync_WhenVersionMismatch_ThrowsConflictException()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm.Id);

        var app = CreateInProgressApplication(farm.Id, area.Id, item.Id, loc.Id, 50m);
        SetPrivateProperty(app, nameof(FertilizerApplication.Version), (uint)3);
        _store.Applications.Add(app);

        var request = new CompleteFertilizerApplicationRequest(
            ActualApplicationDate: DateTimeOffset.UtcNow,
            Version: 2, // stale version
            Products: [new(item.Id, 50m)]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.CompleteAsync(actor, app.Id, request, null));

        Assert.Contains("modified by another user", ex.Message);
    }

    // =========================================================================
    // 2. DATE BOUNDARY VALIDATION
    // =========================================================================

    [Fact]
    public async Task CompleteAsync_WhenDateInFuture_ThrowsValidationException()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm.Id);

        var app = CreateInProgressApplication(farm.Id, area.Id, item.Id, loc.Id, 50m);
        _store.Applications.Add(app);

        var futureDate = DateTimeOffset.UtcNow.AddDays(1);
        var request = new CompleteFertilizerApplicationRequest(
            ActualApplicationDate: futureDate,
            Products: [new(item.Id, 50m)]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CompleteAsync(actor, app.Id, request, null));

        Assert.True(ex.Errors.ContainsKey("actualApplicationDate"));
        Assert.Contains("cannot be in the future", ex.Errors["actualApplicationDate"][0]);
    }

    [Theory]
    [InlineData(0)] // Today
    [InlineData(-5)] // Past (5 days ago)
    public async Task CompleteAsync_WhenTodayOrPastDate_Succeeds(int dayOffset)
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm.Id);

        _store.StockBalances.Add(new StockBalance(_orgId, farm.Id, loc.Id, item.Id, initialQuantity: 100m));

        var app = CreateInProgressApplication(farm.Id, area.Id, item.Id, loc.Id, 50m);
        _store.Applications.Add(app);

        var targetDate = DateTimeOffset.UtcNow.AddDays(dayOffset);
        var request = new CompleteFertilizerApplicationRequest(
            ActualApplicationDate: targetDate,
            Products: [new(item.Id, 50m)]);

        // Act
        var result = await _sut.CompleteAsync(actor, app.Id, request, null);

        // Assert
        Assert.Equal(FertilizerApplicationStatus.Completed, result.Status);
        Assert.Equal(targetDate, result.ActualApplicationDate);
    }

    // =========================================================================
    // 3. PRODUCT LINE & QUANTITY INTEGRITY
    // =========================================================================

    [Fact]
    public async Task CompleteAsync_WhenProductsNullOrEmpty_ThrowsValidationException()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm.Id);

        var app = CreateInProgressApplication(farm.Id, area.Id, item.Id, loc.Id, 50m);
        _store.Applications.Add(app);

        var request = new CompleteFertilizerApplicationRequest(
            ActualApplicationDate: DateTimeOffset.UtcNow,
            Products: null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CompleteAsync(actor, app.Id, request, null));

        Assert.True(ex.Errors.ContainsKey("products"));
        Assert.Contains("actual quantities are required", ex.Errors["products"][0]);
    }

    [Fact]
    public async Task CompleteAsync_WhenProductCountMismatch_ThrowsValidationException()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var item1 = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId, "Urea");
        var item2 = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId, "DAP");
        var loc = CreateStorageLocation(farm.Id);

        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var app = new FertilizerApplication(
            _orgId, "FERT-2026-000001", _userId,
            farmId: farm.Id, farmAreaId: area.Id,
            applicationMethodId: method.Id,
            plannedDate: DateTimeOffset.UtcNow);
        app.AddProduct(item1.Id, 50m, _userId, loc.Id);
        app.AddProduct(item2.Id, 30m, _userId, loc.Id);
        SetPrivateProperty(app, nameof(FertilizerApplication.Status), FertilizerApplicationStatus.InProgress);
        SetPrivateProperty(app, nameof(FertilizerApplication.StartedAt), DateTimeOffset.UtcNow);
        SetPrivateProperty(app, nameof(FertilizerApplication.StartedBy), _userId);
        _store.Applications.Add(app);

        // Request only has item1
        var request = new CompleteFertilizerApplicationRequest(
            ActualApplicationDate: DateTimeOffset.UtcNow,
            Products: [new(item1.Id, 50m)]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CompleteAsync(actor, app.Id, request, null));

        Assert.True(ex.Errors.ContainsKey("products"));
        Assert.Contains("Actual quantities must be provided for every product line", ex.Errors["products"][0]);
    }

    [Fact]
    public async Task CompleteAsync_WhenProductItemNotInApplication_ThrowsValidationException()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var item1 = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId, "Urea");
        var foreignItem = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId, "Potash");
        var loc = CreateStorageLocation(farm.Id);

        var app = CreateInProgressApplication(farm.Id, area.Id, item1.Id, loc.Id, 50m);
        _store.Applications.Add(app);

        var request = new CompleteFertilizerApplicationRequest(
            ActualApplicationDate: DateTimeOffset.UtcNow,
            Products: [new(foreignItem.Id, 50m)]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CompleteAsync(actor, app.Id, request, null));

        Assert.True(ex.Errors.ContainsKey("products"));
        Assert.Contains("Missing actual quantity for product", ex.Errors["products"][0]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task CompleteAsync_WhenActualQuantityZeroOrNegative_ThrowsValidationException(decimal invalidQuantity)
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm.Id);

        var app = CreateInProgressApplication(farm.Id, area.Id, item.Id, loc.Id, 50m);
        _store.Applications.Add(app);

        var request = new CompleteFertilizerApplicationRequest(
            ActualApplicationDate: DateTimeOffset.UtcNow,
            Products: [new(item.Id, invalidQuantity)]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CompleteAsync(actor, app.Id, request, null));

        Assert.True(ex.Errors.ContainsKey("products"));
        Assert.Contains("must be greater than zero", ex.Errors["products"][0]);
    }

    // =========================================================================
    // 4. VARIANCE CALCULATION & EXPLICIT ACKNOWLEDGEMENT
    // =========================================================================

    [Fact]
    public async Task CompleteAsync_WhenVarianceExistsAndNotAcknowledged_ThrowsValidationException_WithDetailedErrors()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId, "DAP Special");
        var loc = CreateStorageLocation(farm.Id);

        _store.StockBalances.Add(new StockBalance(_orgId, farm.Id, loc.Id, item.Id, initialQuantity: 200m));

        var app = CreateInProgressApplication(farm.Id, area.Id, item.Id, loc.Id, 50m);
        _store.Applications.Add(app);

        // Planned is 50, Actual is 65 (variance +15) but AcknowledgeVariance is false
        var request = new CompleteFertilizerApplicationRequest(
            ActualApplicationDate: DateTimeOffset.UtcNow,
            AcknowledgeVariance: false,
            Products: [new(item.Id, 65m)]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CompleteAsync(actor, app.Id, request, null));

        Assert.Contains("variance compared to planned", ex.Message);
        Assert.Single(ex.Errors);
        var errorMsg = ex.Errors.Values.First()[0];
        Assert.Contains("DAP Special", errorMsg);
        Assert.Contains("Planned: 50", errorMsg);
        Assert.Contains("Actual: 65", errorMsg);
        Assert.Contains("+15", errorMsg);

        // Status remains InProgress
        Assert.Equal(FertilizerApplicationStatus.InProgress, app.Status);
        Assert.Empty(_store.StockMovements);
    }

    [Fact]
    public async Task CompleteAsync_WhenVarianceExistsAndAcknowledged_Succeeds_PreservesPlanned_ExposesVariance()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId, "Urea");
        var loc = CreateStorageLocation(farm.Id);

        var balance = new StockBalance(_orgId, farm.Id, loc.Id, item.Id, initialQuantity: 100m);
        _store.StockBalances.Add(balance);

        var app = CreateInProgressApplication(farm.Id, area.Id, item.Id, loc.Id, 50m);
        _store.Applications.Add(app);

        // Actual is 42 (planned 50, variance -8), acknowledged
        var request = new CompleteFertilizerApplicationRequest(
            ActualApplicationDate: DateTimeOffset.UtcNow,
            AcknowledgeVariance: true,
            Products: [new(item.Id, 42m)]);

        // Act
        var result = await _sut.CompleteAsync(actor, app.Id, request, null);

        // Assert
        Assert.Equal(FertilizerApplicationStatus.Completed, result.Status);
        Assert.NotNull(result.CompletedAt);
        Assert.Equal(actor.UserId, result.CompletedBy);

        // Product line details
        var line = Assert.Single(result.Products);
        Assert.Equal(50m, line.PlannedQuantity); // Planned preserved
        Assert.Equal(42m, line.ActualQuantity); // Actual recorded
        Assert.Equal(-8m, line.VarianceQuantity); // Variance exposed: 42 - 50 = -8

        // Balance deducted by actual quantity
        Assert.Equal(58m, balance.QuantityOnHand); // 100 - 42 = 58

        // Stock movement recorded
        var movement = Assert.Single(_store.StockMovements);
        Assert.Equal(42m, movement.Quantity);
        Assert.Equal(StockMovementType.Issue, movement.MovementType);
        Assert.Equal(app.Id, movement.FertilizerApplicationId);
    }

    [Fact]
    public async Task CompleteAsync_WhenNoVariance_SucceedsWithoutAcknowledgement()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm.Id);

        var balance = new StockBalance(_orgId, farm.Id, loc.Id, item.Id, initialQuantity: 100m);
        _store.StockBalances.Add(balance);

        var app = CreateInProgressApplication(farm.Id, area.Id, item.Id, loc.Id, 50m);
        _store.Applications.Add(app);

        // Actual matches planned exactly (variance 0), AcknowledgeVariance is false
        var request = new CompleteFertilizerApplicationRequest(
            ActualApplicationDate: DateTimeOffset.UtcNow,
            AcknowledgeVariance: false,
            Products: [new(item.Id, 50m)]);

        // Act
        var result = await _sut.CompleteAsync(actor, app.Id, request, null);

        // Assert
        Assert.Equal(FertilizerApplicationStatus.Completed, result.Status);
        var line = Assert.Single(result.Products);
        Assert.Equal(50m, line.PlannedQuantity);
        Assert.Equal(50m, line.ActualQuantity);
        Assert.Equal(0m, line.VarianceQuantity);
        Assert.Equal(50m, balance.QuantityOnHand);
    }

    // =========================================================================
    // 5. INVENTORY SHORTAGE & TRANSACTION ROLLBACK
    // =========================================================================

    [Fact]
    public async Task CompleteAsync_WhenStockShortage_ThrowsValidationException_RollsBack_LeavesInProgress_CreatesNoMovements()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId, "Urea");
        var loc = CreateStorageLocation(farm.Id);

        // Balance has only 30m, but 50m required
        var balance = new StockBalance(_orgId, farm.Id, loc.Id, item.Id, initialQuantity: 30m);
        _store.StockBalances.Add(balance);

        var app = CreateInProgressApplication(farm.Id, area.Id, item.Id, loc.Id, 50m);
        _store.Applications.Add(app);

        var request = new CompleteFertilizerApplicationRequest(
            ActualApplicationDate: DateTimeOffset.UtcNow,
            Products: [new(item.Id, 50m)]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CompleteAsync(actor, app.Id, request, null));

        Assert.Contains("insufficient stock", ex.Message, StringComparison.OrdinalIgnoreCase);

        // Application status remains InProgress
        Assert.Equal(FertilizerApplicationStatus.InProgress, app.Status);

        // Balances untouched
        Assert.Equal(30m, balance.QuantityOnHand);

        // Zero movements
        Assert.Empty(_store.StockMovements);

        // Product actuals untouched
        var prod = app.Products.First();
        Assert.Null(prod.ActualQuantity);
        Assert.Null(prod.StockMovementId);
    }

    // =========================================================================
    // 6. OPERATION-SCOPED IDEMPOTENCY
    // =========================================================================

    [Fact]
    public async Task CompleteAsync_WithIdempotencyKey_SameKeySameRequest_ReturnsSafeReplayWithoutDoubleIssue()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm.Id);

        var balance = new StockBalance(_orgId, farm.Id, loc.Id, item.Id, initialQuantity: 200m);
        _store.StockBalances.Add(balance);

        var app = CreateInProgressApplication(farm.Id, area.Id, item.Id, loc.Id, 50m);
        _store.Applications.Add(app);

        var request = new CompleteFertilizerApplicationRequest(
            ActualApplicationDate: DateTimeOffset.UtcNow,
            IdempotencyKey: "complete-key-999",
            Products: [new(item.Id, 50m)]);

        // Act 1: Initial complete
        var result1 = await _sut.CompleteAsync(actor, app.Id, request, null);
        Assert.Equal(FertilizerApplicationStatus.Completed, result1.Status);
        Assert.Equal(150m, balance.QuantityOnHand);
        Assert.Single(_store.StockMovements);

        // Act 2: Retry with identical key and payload
        var result2 = await _sut.CompleteAsync(actor, app.Id, request, null);
        Assert.Equal(FertilizerApplicationStatus.Completed, result2.Status);
        Assert.Equal(result1.Id, result2.Id);

        // Verifications: exactly-once stock consumption!
        Assert.Equal(150m, balance.QuantityOnHand); // not 100m!
        Assert.Single(_store.StockMovements); // not 2!
    }

    [Fact]
    public async Task CompleteAsync_WithIdempotencyKey_SameKeyDifferentRequest_ThrowsConflictException()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm.Id);

        _store.StockBalances.Add(new StockBalance(_orgId, farm.Id, loc.Id, item.Id, initialQuantity: 200m));

        var app = CreateInProgressApplication(farm.Id, area.Id, item.Id, loc.Id, 50m);
        _store.Applications.Add(app);

        var request1 = new CompleteFertilizerApplicationRequest(
            ActualApplicationDate: DateTimeOffset.UtcNow,
            IdempotencyKey: "complete-key-999",
            Products: [new(item.Id, 50m)]);

        await _sut.CompleteAsync(actor, app.Id, request1, null);

        // Different request: different actual date
        var request2 = new CompleteFertilizerApplicationRequest(
            ActualApplicationDate: DateTimeOffset.UtcNow.AddDays(-2),
            IdempotencyKey: "complete-key-999",
            Products: [new(item.Id, 50m)]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.CompleteAsync(actor, app.Id, request2, null));

        Assert.Contains("already exists with different request parameters", ex.Message);
    }

    // =========================================================================
    // 7. ACTUAL AREA & UNIT VALIDATION
    // =========================================================================

    [Fact]
    public async Task CompleteAsync_WithValidActualAreaAndUnit_SavesArea()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm.Id);

        _store.StockBalances.Add(new StockBalance(_orgId, farm.Id, loc.Id, item.Id, initialQuantity: 100m));

        var app = CreateInProgressApplication(farm.Id, area.Id, item.Id, loc.Id, 50m);
        _store.Applications.Add(app);

        var areaUnit = new Unit(null, "HA", "Hectare", "ha", UnitCategory.Area, isSystem: true);
        _store.Units.Add(areaUnit);

        var request = new CompleteFertilizerApplicationRequest(
            ActualApplicationDate: DateTimeOffset.UtcNow,
            ActualArea: 12.5m,
            ActualAreaUnitId: areaUnit.Id,
            Products: [new(item.Id, 50m)]);

        // Act
        var result = await _sut.CompleteAsync(actor, app.Id, request, null);

        // Assert
        Assert.Equal(FertilizerApplicationStatus.Completed, result.Status);
        Assert.Equal(12.5m, result.ActualArea);
        Assert.Equal(areaUnit.Id, result.ActualAreaUnitId);
    }

    [Fact]
    public async Task CompleteAsync_WithActualAreaWithoutUnit_ThrowsValidationException()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm.Id);

        var app = CreateInProgressApplication(farm.Id, area.Id, item.Id, loc.Id, 50m);
        _store.Applications.Add(app);

        var request = new CompleteFertilizerApplicationRequest(
            ActualApplicationDate: DateTimeOffset.UtcNow,
            ActualArea: 12.5m,
            ActualAreaUnitId: null, // missing unit
            Products: [new(item.Id, 50m)]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CompleteAsync(actor, app.Id, request, null));

        Assert.True(ex.Errors.ContainsKey("actualAreaUnitId"));
        Assert.Contains("required when actual area is specified", ex.Errors["actualAreaUnitId"][0]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task CompleteAsync_WithActualAreaZeroOrNegative_ThrowsValidationException(decimal invalidArea)
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm.Id);

        var app = CreateInProgressApplication(farm.Id, area.Id, item.Id, loc.Id, 50m);
        _store.Applications.Add(app);

        var request = new CompleteFertilizerApplicationRequest(
            ActualApplicationDate: DateTimeOffset.UtcNow,
            ActualArea: invalidArea,
            ActualAreaUnitId: Guid.NewGuid(),
            Products: [new(item.Id, 50m)]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CompleteAsync(actor, app.Id, request, null));

        Assert.True(ex.Errors.ContainsKey("actualArea"));
        Assert.Contains("must be greater than zero", ex.Errors["actualArea"][0]);
    }

    // =========================================================================
    // 8. AUDIT LOGGING VERIFICATION
    // =========================================================================

    [Fact]
    public async Task CompleteAsync_WhenSuccessful_SetsCompletedStatus_SetsAudit()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm.Id);

        _store.StockBalances.Add(new StockBalance(_orgId, farm.Id, loc.Id, item.Id, initialQuantity: 100m));

        var app = CreateInProgressApplication(farm.Id, area.Id, item.Id, loc.Id, 50m);
        _store.Applications.Add(app);

        var request = new CompleteFertilizerApplicationRequest(
            ActualApplicationDate: DateTimeOffset.UtcNow,
            Products: [new(item.Id, 50m)]);

        // Act
        var result = await _sut.CompleteAsync(actor, app.Id, request, "127.0.0.1");

        // Assert
        Assert.Equal(FertilizerApplicationStatus.Completed, result.Status);

        var audit = Assert.Single(_store.AuditLogs, a => a.Action == "Fertilizer.Completed");
        Assert.Equal("127.0.0.1", audit.IpAddress);
        Assert.Contains(app.ReferenceNumber, audit.Details.RootElement.ToString());
    }

    // =========================================================================
    // HELPER METHODS
    // =========================================================================

    private FertilizerApplication CreateInProgressApplication(
        Guid farmId, Guid farmAreaId, Guid inventoryItemId, Guid storageLocationId, decimal plannedQuantity)
    {
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var app = new FertilizerApplication(
            _orgId, "FERT-2026-000001", _userId,
            farmId: farmId, farmAreaId: farmAreaId,
            applicationMethodId: method.Id,
            plannedDate: DateTimeOffset.UtcNow);
        app.AddProduct(inventoryItemId, plannedQuantity, _userId, storageLocationId);
        SetPrivateProperty(app, nameof(FertilizerApplication.Status), FertilizerApplicationStatus.InProgress);
        SetPrivateProperty(app, nameof(FertilizerApplication.StartedAt), DateTimeOffset.UtcNow);
        SetPrivateProperty(app, nameof(FertilizerApplication.StartedBy), _userId);
        return app;
    }

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

    private (Farm, FarmArea, CropPlantation, CropCycle, CropCycleStage) CreateValidHierarchyChain()
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
        public List<InventoryItem> InventoryItems { get; } = [];
        public HashSet<Guid> ActivePppItemIds { get; } = [];
        public List<StorageLocation> StorageLocations { get; } = [];
        public List<IrrigationEvent> IrrigationEvents { get; } = [];
        public List<StockBalance> StockBalances { get; } = [];
        public List<AuditLog> AuditLogs { get; } = [];
        public List<StockMovement> StockMovements { get; } = [];
        public List<FertilizerOperationIdempotencyRecord> IdempotencyRecords { get; } = [];
        public List<Unit> Units { get; } = [];

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

        public void AddAuditLog(AuditLog auditLog) => AuditLogs.Add(auditLog);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<FertilizerApplication?> FindAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Applications.FirstOrDefault(a => a.Id == id && a.OrganizationId == organizationId));

        public Task<int> CountAsync(Guid organizationId, FertilizerApplicationListQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult(Applications.Count(a => a.OrganizationId == organizationId));

        public Task<IReadOnlyList<FertilizerApplication>> ListAsync(
            Guid organizationId,
            FertilizerApplicationListQuery query,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FertilizerApplication>>(Applications.Where(a => a.OrganizationId == organizationId).Skip(skip).Take(take).ToList());

        public Task<IReadOnlyList<FertilizerApplication>> ListForExportAsync(
            Guid organizationId,
            FertilizerApplicationListQuery query,
            CancellationToken cancellationToken = default) =>
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
            Task.FromResult(FarmAreas.FirstOrDefault(a => a.Id == farmAreaId && a.OrganizationId == organizationId));

        public Task<CropPlantation?> FindPlantationAsync(Guid plantationId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Plantations.FirstOrDefault(p => p.Id == plantationId && p.OrganizationId == organizationId));

        public Task<CropCycle?> FindCropCycleAsync(Guid cropCycleId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(CropCycles.FirstOrDefault(c => c.Id == cropCycleId && c.OrganizationId == organizationId));

        public Task<CropCycleStage?> FindCropCycleStageAsync(Guid stageId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(CropCycleStages.FirstOrDefault(s => s.Id == stageId && s.CropCycle != null && s.CropCycle.Plantation != null && s.CropCycle.Plantation.OrganizationId == organizationId));

        public Task<ApplicationMethod?> FindApplicationMethodAsync(Guid methodId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ApplicationMethods.FirstOrDefault(m => m.Id == methodId && (m.OrganizationId == organizationId || m.IsSystem)));

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

        public Task<Unit?> FindUnitAsync(Guid unitId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Units.FirstOrDefault(u => u.Id == unitId));
    }
}
