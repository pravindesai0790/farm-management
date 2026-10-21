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

public class FertilizerRecordCompletedTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly FertilizerApplicationServiceTests.FakeFertilizerStore _store = new();
    private readonly FertilizerStockManager _stockManager;
    private readonly TestReferenceGenerator _referenceGenerator = new();
    private readonly FertilizerApplicationService _sut;

    public FertilizerRecordCompletedTests()
    {
        _stockManager = new FertilizerStockManager(_store);
        var validator = new FertilizerValidator(_store);
        _sut = new FertilizerApplicationService(_store, validator, _referenceGenerator, _stockManager);
    }

    private FertilizerActor CreateActor(Guid? orgId = null) =>
        new(_userId, orgId ?? _orgId);

    // =========================================================================
    // 1. HAPPY PATH CREATION & STATUS
    // =========================================================================

    [Fact]
    public async Task RecordCompletedAsync_WithValidInputs_CreatesCompletedApplicationWithMovements()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId, "Urea 46%");
        var loc = CreateStorageLocation(farm.Id, "Main Shed");
        CreateStockBalance(farm.Id, loc.Id, item.Id, 100m);

        var request = new RecordCompletedFertilizerApplicationRequest(
            FarmId: farm.Id,
            FarmAreaId: area.Id,
            ApplicationMethodId: method.Id,
            ActualApplicationDate: DateTimeOffset.UtcNow,
            Products: [new(item.Id, loc.Id, 30m)],
            PurposeReason: "Post-planting boost");

        // Act
        var result = await _sut.RecordCompletedAsync(actor, request, "192.168.1.1");

        // Assert
        Assert.Equal(FertilizerApplicationStatus.Completed, result.Status);
        Assert.Equal("COMPLETED", result.StatusName);
        Assert.Equal("Post-planting boost", result.PurposeReason);
        Assert.NotNull(result.CompletedAt);
        Assert.Equal(_userId, result.CompletedBy);
        Assert.Equal(_userId, result.CreatedBy);
        Assert.Single(result.Products);

        // Balance updated
        var balance = _store.StockBalances.First(b => b.StorageLocationId == loc.Id && b.InventoryItemId == item.Id);
        Assert.Equal(70m, balance.QuantityOnHand);

        // Stock movement created
        var movement = Assert.Single(_store.StockMovements);
        Assert.Equal(StockMovementType.Issue, movement.MovementType);
        Assert.Equal(30m, movement.Quantity);
        Assert.Equal(result.Id, movement.FertilizerApplicationId);
        Assert.Equal(result.ReferenceNumber, movement.ReferenceNumber);

        // Product line linked
        Assert.Equal(movement.Id, result.Products[0].StockMovementId);
        Assert.Equal(30m, result.Products[0].ActualQuantity);
        Assert.Equal(loc.Id, result.Products[0].StorageLocationId);
    }

    [Fact]
    public async Task RecordCompletedAsync_PlannedFieldsAreNullOnResponse()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm.Id);
        CreateStockBalance(farm.Id, loc.Id, item.Id, 100m);

        var request = new RecordCompletedFertilizerApplicationRequest(
            FarmId: farm.Id,
            FarmAreaId: area.Id,
            ApplicationMethodId: method.Id,
            ActualApplicationDate: DateTimeOffset.UtcNow,
            Products: [new(item.Id, loc.Id, 25m)]);

        // Act
        var result = await _sut.RecordCompletedAsync(actor, request, null);

        // Assert
        Assert.Null(result.PlannedDate);
        Assert.Null(result.ScheduledDate);
        Assert.Null(result.PlannedArea);
        Assert.Null(result.PlannedAreaUnitId);

        var prod = Assert.Single(result.Products);
        Assert.Null(prod.PlannedQuantity);
        Assert.Null(prod.VarianceQuantity);
        Assert.Equal(25m, prod.ActualQuantity);
    }

    // =========================================================================
    // 2. HIERARCHY & CONTEXT VALIDATION
    // =========================================================================

    [Fact]
    public async Task RecordCompletedAsync_MissingFarmOrArea_ThrowsValidationException()
    {
        // Arrange
        var actor = CreateActor();
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);

        var request = new RecordCompletedFertilizerApplicationRequest(
            FarmId: Guid.Empty,
            FarmAreaId: Guid.Empty,
            ApplicationMethodId: method.Id,
            ActualApplicationDate: DateTimeOffset.UtcNow,
            Products: [new(item.Id, Guid.NewGuid(), 10m)]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.RecordCompletedAsync(actor, request, null));

        Assert.True(ex.Errors.ContainsKey("farmId"));
    }

    [Fact]
    public async Task RecordCompletedAsync_InvalidHierarchy_ThrowsValidationException()
    {
        // Arrange
        var actor = CreateActor();
        var farm1 = CreateFarm();
        var farm2 = CreateFarm();
        var areaOfFarm2 = CreateFarmArea(farm2.Id);
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm1.Id);

        var request = new RecordCompletedFertilizerApplicationRequest(
            FarmId: farm1.Id,
            FarmAreaId: areaOfFarm2.Id, // Area belongs to farm2, not farm1
            ApplicationMethodId: method.Id,
            ActualApplicationDate: DateTimeOffset.UtcNow,
            Products: [new(item.Id, loc.Id, 10m)]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.RecordCompletedAsync(actor, request, null));

        Assert.True(ex.Errors.ContainsKey("farmAreaId"));
    }

    // =========================================================================
    // 3. METHOD & IRRIGATION LINK RULES
    // =========================================================================

    [Fact]
    public async Task RecordCompletedAsync_MethodIsOther_WithoutDescription_ThrowsValidationException()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var otherMethod = CreateMethod("OTHER", "Other");
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm.Id);

        var request = new RecordCompletedFertilizerApplicationRequest(
            FarmId: farm.Id,
            FarmAreaId: area.Id,
            ApplicationMethodId: otherMethod.Id,
            OtherMethodDescription: null, // missing required description
            ActualApplicationDate: DateTimeOffset.UtcNow,
            Products: [new(item.Id, loc.Id, 10m)]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.RecordCompletedAsync(actor, request, null));

        Assert.True(ex.Errors.ContainsKey("otherMethodDescription"));
    }

    [Fact]
    public async Task RecordCompletedAsync_MethodNotFertigation_WithIrrigationLink_ThrowsValidationException()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var broadcastMethod = CreateMethod("BROADCASTING", "Broadcasting");
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm.Id);

        var request = new RecordCompletedFertilizerApplicationRequest(
            FarmId: farm.Id,
            FarmAreaId: area.Id,
            ApplicationMethodId: broadcastMethod.Id,
            IrrigationEventId: Guid.NewGuid(), // not allowed for Broadcasting
            ActualApplicationDate: DateTimeOffset.UtcNow,
            Products: [new(item.Id, loc.Id, 10m)]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.RecordCompletedAsync(actor, request, null));

        Assert.True(ex.Errors.ContainsKey("irrigationEventId"));
    }

    [Fact]
    public async Task RecordCompletedAsync_MethodFertigation_WithCompatibleIrrigationLink_Succeeds()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var fertigationMethod = CreateMethod("FERTIGATION", "Fertigation");
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm.Id);
        CreateStockBalance(farm.Id, loc.Id, item.Id, 100m);

        var irrigation = new IrrigationEvent(_orgId, farm.Id, area.Id, _userId);
        _store.IrrigationEvents.Add(irrigation);

        var request = new RecordCompletedFertilizerApplicationRequest(
            FarmId: farm.Id,
            FarmAreaId: area.Id,
            ApplicationMethodId: fertigationMethod.Id,
            IrrigationEventId: irrigation.Id,
            ActualApplicationDate: DateTimeOffset.UtcNow,
            Products: [new(item.Id, loc.Id, 15m)]);

        // Act
        var result = await _sut.RecordCompletedAsync(actor, request, null);

        // Assert
        Assert.Equal(FertilizerApplicationStatus.Completed, result.Status);
        Assert.Equal(irrigation.Id, result.IrrigationEventId);
    }

    // =========================================================================
    // 4. DATE BOUNDARY & RETROSPECTIVE ACKNOWLEDGEMENT
    // =========================================================================

    [Fact]
    public async Task RecordCompletedAsync_ActualDateToday_SucceedsWithoutAcknowledgement()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm.Id);
        CreateStockBalance(farm.Id, loc.Id, item.Id, 100m);

        var today = DateTimeOffset.UtcNow;
        var request = new RecordCompletedFertilizerApplicationRequest(
            FarmId: farm.Id,
            FarmAreaId: area.Id,
            ApplicationMethodId: method.Id,
            ActualApplicationDate: today,
            AcknowledgeRetrospectiveStockDeduction: false, // unacknowledged but today
            Products: [new(item.Id, loc.Id, 20m)]);

        // Act
        var result = await _sut.RecordCompletedAsync(actor, request, null);

        // Assert
        Assert.Equal(FertilizerApplicationStatus.Completed, result.Status);
        Assert.Equal(today, result.ActualApplicationDate);
    }

    [Fact]
    public async Task RecordCompletedAsync_ActualDateFuture_ThrowsValidationException()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm.Id);

        var futureDate = DateTimeOffset.UtcNow.AddDays(2);
        var request = new RecordCompletedFertilizerApplicationRequest(
            FarmId: farm.Id,
            FarmAreaId: area.Id,
            ApplicationMethodId: method.Id,
            ActualApplicationDate: futureDate,
            Products: [new(item.Id, loc.Id, 20m)]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.RecordCompletedAsync(actor, request, null));

        Assert.True(ex.Errors.ContainsKey("actualApplicationDate"));
        Assert.Contains("future", ex.Errors["actualApplicationDate"][0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RecordCompletedAsync_ActualDatePast_WithoutAcknowledgement_ThrowsValidationException()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm.Id);
        CreateStockBalance(farm.Id, loc.Id, item.Id, 100m);

        var pastDate = DateTimeOffset.UtcNow.AddDays(-3);
        var request = new RecordCompletedFertilizerApplicationRequest(
            FarmId: farm.Id,
            FarmAreaId: area.Id,
            ApplicationMethodId: method.Id,
            ActualApplicationDate: pastDate,
            AcknowledgeRetrospectiveStockDeduction: false, // missing acknowledgement
            Products: [new(item.Id, loc.Id, 20m)]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.RecordCompletedAsync(actor, request, null));

        Assert.True(ex.Errors.ContainsKey("actualApplicationDate"));
        Assert.Contains("current on-hand stock", ex.Errors["actualApplicationDate"][0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RecordCompletedAsync_ActualDatePast_WithAcknowledgement_SucceedsAndDeductsCurrentStock()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm.Id);
        CreateStockBalance(farm.Id, loc.Id, item.Id, 100m);

        var pastDate = DateTimeOffset.UtcNow.AddDays(-3);
        var request = new RecordCompletedFertilizerApplicationRequest(
            FarmId: farm.Id,
            FarmAreaId: area.Id,
            ApplicationMethodId: method.Id,
            ActualApplicationDate: pastDate,
            AcknowledgeRetrospectiveStockDeduction: true, // acknowledged
            Products: [new(item.Id, loc.Id, 25m)]);

        // Act
        var result = await _sut.RecordCompletedAsync(actor, request, null);

        // Assert
        Assert.Equal(FertilizerApplicationStatus.Completed, result.Status);
        Assert.Equal(pastDate, result.ActualApplicationDate);
        Assert.Equal(75m, _store.StockBalances.First(b => b.StorageLocationId == loc.Id && b.InventoryItemId == item.Id).QuantityOnHand);
    }

    // =========================================================================
    // 5. AREA & PRODUCT ELIGIBILITY VALIDATIONS
    // =========================================================================

    [Fact]
    public async Task RecordCompletedAsync_ActualAreaProvided_WithoutUnit_ThrowsValidationException()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm.Id);

        var request = new RecordCompletedFertilizerApplicationRequest(
            FarmId: farm.Id,
            FarmAreaId: area.Id,
            ApplicationMethodId: method.Id,
            ActualApplicationDate: DateTimeOffset.UtcNow,
            ActualArea: 10m,
            ActualAreaUnitId: null, // missing unit
            Products: [new(item.Id, loc.Id, 20m)]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.RecordCompletedAsync(actor, request, null));

        Assert.True(ex.Errors.ContainsKey("actualAreaUnitId"));
    }

    [Fact]
    public async Task RecordCompletedAsync_EmptyProducts_ThrowsValidationException()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var method = CreateMethod("BROADCASTING", "Broadcasting");

        var request = new RecordCompletedFertilizerApplicationRequest(
            FarmId: farm.Id,
            FarmAreaId: area.Id,
            ApplicationMethodId: method.Id,
            ActualApplicationDate: DateTimeOffset.UtcNow,
            Products: []);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.RecordCompletedAsync(actor, request, null));

        Assert.True(ex.Errors.ContainsKey("products"));
    }

    [Fact]
    public async Task RecordCompletedAsync_DuplicateProducts_ThrowsValidationException()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm.Id);

        var request = new RecordCompletedFertilizerApplicationRequest(
            FarmId: farm.Id,
            FarmAreaId: area.Id,
            ApplicationMethodId: method.Id,
            ActualApplicationDate: DateTimeOffset.UtcNow,
            Products:
            [
                new(item.Id, loc.Id, 10m),
                new(item.Id, loc.Id, 20m) // duplicate
            ]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.RecordCompletedAsync(actor, request, null));

        Assert.True(ex.Errors.ContainsKey("products"));
        Assert.Contains("duplicate", ex.Errors["products"][0], StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task RecordCompletedAsync_NonPositiveActualQuantity_ThrowsValidationException(decimal invalidQuantity)
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm.Id);

        var request = new RecordCompletedFertilizerApplicationRequest(
            FarmId: farm.Id,
            FarmAreaId: area.Id,
            ApplicationMethodId: method.Id,
            ActualApplicationDate: DateTimeOffset.UtcNow,
            Products: [new(item.Id, loc.Id, invalidQuantity)]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.RecordCompletedAsync(actor, request, null));

        Assert.True(ex.Errors.ContainsKey("products"));
        Assert.Contains("greater than zero", ex.Errors["products"][0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RecordCompletedAsync_StorageLocationFromDifferentFarm_ThrowsValidationException()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var foreignFarm = CreateFarm();
        var foreignLoc = CreateStorageLocation(foreignFarm.Id);
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);

        var request = new RecordCompletedFertilizerApplicationRequest(
            FarmId: farm.Id,
            FarmAreaId: area.Id,
            ApplicationMethodId: method.Id,
            ActualApplicationDate: DateTimeOffset.UtcNow,
            Products: [new(item.Id, foreignLoc.Id, 10m)]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.RecordCompletedAsync(actor, request, null));

        Assert.True(ex.Errors.ContainsKey("storageLocationId"));
    }

    [Fact]
    public async Task RecordCompletedAsync_InactiveItemOrLocation_ThrowsValidationException()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        SetPrivateProperty(item, nameof(InventoryItem.IsActive), false); // Inactive item
        var loc = CreateStorageLocation(farm.Id);

        var request = new RecordCompletedFertilizerApplicationRequest(
            FarmId: farm.Id,
            FarmAreaId: area.Id,
            ApplicationMethodId: method.Id,
            ActualApplicationDate: DateTimeOffset.UtcNow,
            Products: [new(item.Id, loc.Id, 10m)]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.RecordCompletedAsync(actor, request, null));

        Assert.True(ex.Errors.ContainsKey("products"));
        Assert.Contains("inactive", ex.Errors["products"][0], StringComparison.OrdinalIgnoreCase);
    }

    // =========================================================================
    // 6. STOCK SHORTAGE, ATOMIC ROLLBACK & REFERENCE PRESERVATION
    // =========================================================================

    [Fact]
    public async Task RecordCompletedAsync_StockShortage_RollsBack_NoMovements_NoRefAllocated()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId, "SuperPhosphate");
        var loc = CreateStorageLocation(farm.Id);
        CreateStockBalance(farm.Id, loc.Id, item.Id, 15m); // Only 15 in stock

        var request = new RecordCompletedFertilizerApplicationRequest(
            FarmId: farm.Id,
            FarmAreaId: area.Id,
            ApplicationMethodId: method.Id,
            ActualApplicationDate: DateTimeOffset.UtcNow,
            Products: [new(item.Id, loc.Id, 40m)]); // Shortage of 25m

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.RecordCompletedAsync(actor, request, null));

        Assert.True(ex.Errors.ContainsKey("products[0]"));
        Assert.Contains("insufficient stock", ex.Message, StringComparison.OrdinalIgnoreCase);

        // Zero applications saved
        Assert.Empty(_store.Applications);

        // Zero movements created
        Assert.Empty(_store.StockMovements);

        // Balance untouched
        Assert.Equal(15m, _store.StockBalances.First(b => b.StorageLocationId == loc.Id && b.InventoryItemId == item.Id).QuantityOnHand);

        // Reference sequence was not called/burned
        Assert.Equal(0, _referenceGenerator.Sequence);
    }

    // =========================================================================
    // 7. OPERATION-SCOPED IDEMPOTENCY
    // =========================================================================

    [Fact]
    public async Task RecordCompletedAsync_IdempotencyReplay_SamePayload_ReturnsReplayWithoutDuplicateDeduction()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm.Id);
        CreateStockBalance(farm.Id, loc.Id, item.Id, 100m);

        var idempotencyKey = Guid.NewGuid().ToString("N");
        var request = new RecordCompletedFertilizerApplicationRequest(
            FarmId: farm.Id,
            FarmAreaId: area.Id,
            ApplicationMethodId: method.Id,
            ActualApplicationDate: DateTimeOffset.UtcNow,
            Products: [new(item.Id, loc.Id, 20m)],
            IdempotencyKey: idempotencyKey);

        // First call
        var firstResult = await _sut.RecordCompletedAsync(actor, request, null);
        Assert.Equal(FertilizerApplicationStatus.Completed, firstResult.Status);
        Assert.Single(_store.StockMovements);
        Assert.Equal(80m, _store.StockBalances.First(b => b.StorageLocationId == loc.Id && b.InventoryItemId == item.Id).QuantityOnHand);
        Assert.Equal(1, _referenceGenerator.Sequence);

        // Act - Replay with same key & identical payload
        var replayResult = await _sut.RecordCompletedAsync(actor, request, null);

        // Assert
        Assert.Equal(firstResult.Id, replayResult.Id);
        Assert.Equal(firstResult.ReferenceNumber, replayResult.ReferenceNumber);
        Assert.Single(_store.StockMovements); // Still exactly one movement
        Assert.Equal(80m, _store.StockBalances.First(b => b.StorageLocationId == loc.Id && b.InventoryItemId == item.Id).QuantityOnHand);
        Assert.Equal(1, _referenceGenerator.Sequence); // No new reference burned
    }

    [Fact]
    public async Task RecordCompletedAsync_IdempotencyConflict_DifferentPayload_ThrowsConflictException()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm.Id);
        CreateStockBalance(farm.Id, loc.Id, item.Id, 100m);

        var idempotencyKey = Guid.NewGuid().ToString("N");
        var request1 = new RecordCompletedFertilizerApplicationRequest(
            FarmId: farm.Id,
            FarmAreaId: area.Id,
            ApplicationMethodId: method.Id,
            ActualApplicationDate: DateTimeOffset.UtcNow,
            Products: [new(item.Id, loc.Id, 20m)],
            IdempotencyKey: idempotencyKey);

        await _sut.RecordCompletedAsync(actor, request1, null);

        var request2 = new RecordCompletedFertilizerApplicationRequest(
            FarmId: farm.Id,
            FarmAreaId: area.Id,
            ApplicationMethodId: method.Id,
            ActualApplicationDate: DateTimeOffset.UtcNow,
            Products: [new(item.Id, loc.Id, 35m)], // mutated quantity
            IdempotencyKey: idempotencyKey);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.RecordCompletedAsync(actor, request2, null));

        Assert.Contains("different request parameters", ex.Message);
    }

    // =========================================================================
    // 8. TENANT ISOLATION
    // =========================================================================

    [Fact]
    public async Task RecordCompletedAsync_TenantIsolation_CrossOrganizationResource_ThrowsNotFoundOrValidation()
    {
        // Arrange
        var actor = CreateActor();
        var foreignOrgId = Guid.NewGuid();
        var farm = new Farm(foreignOrgId, "Foreign Farm", Guid.NewGuid(), Guid.NewGuid(), 100m, Guid.NewGuid());
        var area = new FarmArea(foreignOrgId, farm.Id, null, "Foreign Plot", 50m, Guid.NewGuid(), _userId);
        _store.Farms.Add(farm);
        _store.FarmAreas.Add(area);

        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm.Id);

        var request = new RecordCompletedFertilizerApplicationRequest(
            FarmId: farm.Id,
            FarmAreaId: area.Id,
            ApplicationMethodId: method.Id,
            ActualApplicationDate: DateTimeOffset.UtcNow,
            Products: [new(item.Id, loc.Id, 10m)]);

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _sut.RecordCompletedAsync(actor, request, null));
    }

    // =========================================================================
    // 9. AUDIT LOGGING
    // =========================================================================

    [Fact]
    public async Task RecordCompletedAsync_AuditLogging_RecordsFertilizerRecordCompleted()
    {
        // Arrange
        var actor = CreateActor();
        var (farm, area, _, _, _) = CreateValidHierarchyChain();
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var item = CreateInventoryItem(FarmInventoryCategories.FertilizersSoilId);
        var loc = CreateStorageLocation(farm.Id);
        CreateStockBalance(farm.Id, loc.Id, item.Id, 100m);

        var request = new RecordCompletedFertilizerApplicationRequest(
            FarmId: farm.Id,
            FarmAreaId: area.Id,
            ApplicationMethodId: method.Id,
            ActualApplicationDate: DateTimeOffset.UtcNow,
            Products: [new(item.Id, loc.Id, 25m)],
            PurposeReason: "Spring dressing");

        // Act
        var result = await _sut.RecordCompletedAsync(actor, request, "10.0.0.1");

        // Assert
        var audit = Assert.Single(_store.AuditLogs, a => a.Action == "Fertilizer.RecordCompleted");
        Assert.Equal("FertilizerApplication", audit.EntityType);
        Assert.Equal(result.Id, audit.EntityId);
        Assert.Equal("10.0.0.1", audit.IpAddress);
        Assert.Contains(result.ReferenceNumber, audit.Details.RootElement.ToString());
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
        public int Sequence { get; private set; }
        public Task<string> NextReferenceNumberAsync(Guid organizationId, int year, CancellationToken cancellationToken = default)
        {
            Sequence++;
            return Task.FromResult($"FERT-{year}-{Sequence:D6}");
        }
    }
}
