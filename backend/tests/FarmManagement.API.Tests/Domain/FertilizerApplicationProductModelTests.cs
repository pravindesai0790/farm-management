using FarmManagement.Domain.Entities;
using Xunit;

namespace FarmManagement.API.Tests.Domain;

public sealed class FertilizerApplicationProductModelTests
{
    private readonly Guid _applicationId = Guid.NewGuid();
    private readonly Guid _itemId = Guid.NewGuid();
    private readonly Guid _storageLocationId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithValidInputs_InitializesCorrectly()
    {
        var product = new FertilizerApplicationProduct(
            _applicationId,
            _itemId,
            25.5m,
            _userId,
            _storageLocationId);

        Assert.NotEqual(Guid.Empty, product.Id);
        Assert.Equal(_applicationId, product.FertilizerApplicationId);
        Assert.Equal(_itemId, product.InventoryItemId);
        Assert.Equal(25.5m, product.PlannedQuantity);
        Assert.Equal(_storageLocationId, product.StorageLocationId);
        Assert.Equal(_userId, product.CreatedBy);
        Assert.Null(product.ActualQuantity);
        Assert.Null(product.StockMovementId);
    }

    [Fact]
    public void Constructor_WithEmptyApplicationId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new FertilizerApplicationProduct(
            Guid.Empty,
            _itemId,
            10m,
            _userId));
    }

    [Fact]
    public void Constructor_WithEmptyItemId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new FertilizerApplicationProduct(
            _applicationId,
            Guid.Empty,
            10m,
            _userId));
    }

    [Fact]
    public void Constructor_WithNegativePlannedQuantity_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FertilizerApplicationProduct(
            _applicationId,
            _itemId,
            -1.0m,
            _userId));
    }

    [Fact]
    public void UpdatePlanned_ValidValues_UpdatesPlannedQuantityAndLocation()
    {
        var product = new FertilizerApplicationProduct(_applicationId, _itemId, 10m, _userId);
        var now = DateTimeOffset.UtcNow;
        var newLocationId = Guid.NewGuid();

        product.UpdatePlanned(15m, newLocationId, now, _userId);

        Assert.Equal(15m, product.PlannedQuantity);
        Assert.Equal(newLocationId, product.StorageLocationId);
        Assert.Equal(now, product.UpdatedAt);
        Assert.Equal(_userId, product.UpdatedBy);
    }

    [Fact]
    public void SetStorageLocation_ValidLocation_UpdatesLocation()
    {
        var product = new FertilizerApplicationProduct(_applicationId, _itemId, 10m, _userId);
        var now = DateTimeOffset.UtcNow;
        var newLocationId = Guid.NewGuid();

        product.SetStorageLocation(newLocationId, now, _userId);

        Assert.Equal(newLocationId, product.StorageLocationId);
        Assert.Equal(now, product.UpdatedAt);
        Assert.Equal(_userId, product.UpdatedBy);
    }

    [Fact]
    public void RecordActual_ValidQuantityAndMovement_UpdatesActuals()
    {
        var product = new FertilizerApplicationProduct(_applicationId, _itemId, 10m, _userId);
        var now = DateTimeOffset.UtcNow;
        var movementId = Guid.NewGuid();

        product.RecordActual(12.5m, movementId, now, _userId);

        Assert.Equal(12.5m, product.ActualQuantity);
        Assert.Equal(movementId, product.StockMovementId);
        Assert.Equal(now, product.UpdatedAt);
        Assert.Equal(_userId, product.UpdatedBy);
    }
}
