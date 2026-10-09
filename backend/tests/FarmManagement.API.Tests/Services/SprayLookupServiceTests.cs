using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.Sprays;
using FarmManagement.Application.Interfaces.Sprays;
using FarmManagement.Application.Services;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Services;

public class SprayLookupServiceTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly FakeSprayLookupStore _store = new();
    private readonly SprayService _sut;

    public SprayLookupServiceTests()
    {
        _sut = new SprayService(_store);
    }

    private SprayActor CreateActor(Guid? orgId = null) =>
        new(_userId, orgId ?? _orgId);

    [Fact]
    public async Task ListProductsLookup_ValidActor_CallsStoreAndReturnsProducts()
    {
        // Arrange
        var actor = CreateActor();
        var item1 = new SprayProductLookupResponse(
            Guid.NewGuid(), "Alpha Product", "SKU-001", Guid.NewGuid(), "Liters", "L", "L",
            Guid.NewGuid(), "HERB", "Herbicide", "Glyphosate", "Bayer", Guid.NewGuid());
        var item2 = new SprayProductLookupResponse(
            Guid.NewGuid(), "Beta Product", "SKU-002", Guid.NewGuid(), "Kilograms", "KG", "kg",
            Guid.NewGuid(), "FUNG", "Fungicide", "Copper", "Syngenta", Guid.NewGuid());
        _store.Products.AddRange([item1, item2]);

        // Act
        var result = await _sut.ListProductsLookupAsync(actor);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Alpha Product", result[0].Name);
        Assert.Equal("Beta Product", result[1].Name);
    }

    [Fact]
    public async Task ListProductsLookup_EmptyActor_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var actor = new SprayActor(Guid.Empty, Guid.Empty);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.ListProductsLookupAsync(actor));
    }

    [Fact]
    public async Task ListStorageLocationsLookup_ValidInputs_ReturnsLocations()
    {
        // Arrange
        var actor = CreateActor();
        var farmId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        _store.TargetFarmId = farmId;
        _store.TargetItemId = itemId;

        var loc1 = new SprayStorageLocationLookupResponse(Guid.NewGuid(), "Shed A", 50m, true, Guid.NewGuid(), "Liters");
        var loc2 = new SprayStorageLocationLookupResponse(Guid.NewGuid(), "Shed B", 0m, false, Guid.NewGuid(), "Liters");
        _store.StorageLocations.AddRange([loc1, loc2]);

        // Act
        var result = await _sut.ListStorageLocationsLookupAsync(actor, farmId, itemId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.True(result[0].HasStock);
        Assert.False(result[1].HasStock);
    }

    [Fact]
    public async Task ListStorageLocationsLookup_EmptyFarmId_ThrowsValidationException()
    {
        // Arrange
        var actor = CreateActor();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.ListStorageLocationsLookupAsync(actor, Guid.Empty, Guid.NewGuid()));
        Assert.True(ex.Errors?.ContainsKey("farmId"));
    }

    [Fact]
    public async Task ListStorageLocationsLookup_EmptyInventoryItemId_ThrowsValidationException()
    {
        // Arrange
        var actor = CreateActor();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.ListStorageLocationsLookupAsync(actor, Guid.NewGuid(), Guid.Empty));
        Assert.True(ex.Errors?.ContainsKey("inventoryItemId"));
    }

    [Fact]
    public async Task ListStorageLocationsLookup_FarmNotFound_ThrowsResourceNotFoundException()
    {
        // Arrange
        var actor = CreateActor();
        _store.TargetFarmId = null;

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _sut.ListStorageLocationsLookupAsync(actor, Guid.NewGuid(), Guid.NewGuid()));
        Assert.Contains("farm", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ListStorageLocationsLookup_ItemNotFound_ThrowsResourceNotFoundException()
    {
        // Arrange
        var actor = CreateActor();
        var farmId = Guid.NewGuid();
        _store.TargetFarmId = farmId;
        _store.TargetItemId = null; // Item not found

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _sut.ListStorageLocationsLookupAsync(actor, farmId, Guid.NewGuid()));
        Assert.Contains("inventory item", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FakeSprayLookupStore : ISprayStore
    {
        public List<Farm> Farms { get; } = [];
        public List<SprayProductLookupResponse> Products { get; } = [];
        public List<SprayStorageLocationLookupResponse> StorageLocations { get; } = [];

        public Guid? TargetFarmId { get; set; }
        public Guid? TargetItemId { get; set; }

        public Task<Spray?> FindAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Spray?>(null);

        public Task<int> CountAsync(Guid organizationId, SprayListQuery query, DateTimeOffset now, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<IReadOnlyList<Spray>> ListAsync(Guid organizationId, SprayListQuery query, int skip, int take, DateTimeOffset now, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Spray>>([]);

        public void Add(Spray spray) { }
        public void AddAuditLog(AuditLog auditLog) { }

        public Task<Farm?> FindFarmAsync(Guid farmId, Guid organizationId, CancellationToken cancellationToken = default)
        {
            if (TargetFarmId.HasValue && TargetFarmId.Value == farmId)
            {
                return Task.FromResult<Farm?>(new Farm(organizationId, "Farm", Guid.NewGuid(), Guid.NewGuid(), 100m, Guid.NewGuid()));
            }
            return Task.FromResult<Farm?>(null);
        }

        public Task<FarmArea?> FindFarmAreaAsync(Guid farmAreaId, Guid organizationId, CancellationToken cancellationToken = default) => Task.FromResult<FarmArea?>(null);
        public Task<CropPlantation?> FindPlantationAsync(Guid plantationId, Guid organizationId, CancellationToken cancellationToken = default) => Task.FromResult<CropPlantation?>(null);
        public Task<CropCycle?> FindCropCycleAsync(Guid cropCycleId, Guid organizationId, CancellationToken cancellationToken = default) => Task.FromResult<CropCycle?>(null);
        public Task<CropCycleStage?> FindCropCycleStageAsync(Guid cropCycleStageId, Guid organizationId, CancellationToken cancellationToken = default) => Task.FromResult<CropCycleStage?>(null);
        public Task<Target?> FindTargetAsync(Guid targetId, Guid organizationId, CancellationToken cancellationToken = default) => Task.FromResult<Target?>(null);
        public Task<ApplicationMethod?> FindApplicationMethodAsync(Guid applicationMethodId, Guid organizationId, CancellationToken cancellationToken = default) => Task.FromResult<ApplicationMethod?>(null);
        public Task<Unit?> FindUnitAsync(Guid unitId, Guid organizationId, CancellationToken cancellationToken = default) => Task.FromResult<Unit?>(null);

        public Task<InventoryItem?> FindInventoryItemAsync(Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default)
        {
            if (TargetItemId.HasValue && TargetItemId.Value == inventoryItemId)
            {
                return Task.FromResult<InventoryItem?>(new InventoryItem(organizationId, "Item", Guid.NewGuid(), Guid.NewGuid(), sku: "SKU"));
            }
            return Task.FromResult<InventoryItem?>(null);
        }

        public Task<bool> HasActivePlantProtectionProfileAsync(Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<StorageLocation?> FindStorageLocationAsync(Guid storageLocationId, Guid organizationId, CancellationToken cancellationToken = default) => Task.FromResult<StorageLocation?>(null);
        public Task<StockBalance?> FindStockBalanceAsync(Guid storageLocationId, Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default) => Task.FromResult<StockBalance?>(null);
        public void RemoveSprayProduct(SprayProduct product) { }
        public void AddMovement(StockMovement movement) { }
        public Task<StockBalance?> LockBalanceAsync(Guid storageLocationId, Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default) => Task.FromResult<StockBalance?>(null);
        public Task AcquireAdvisoryLockAsync(Guid storageLocationId, Guid inventoryItemId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) => operation(cancellationToken);
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyList<SprayProductLookupResponse>> ListProductsLookupAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SprayProductLookupResponse>>(Products);

        public Task<IReadOnlyList<SprayStorageLocationLookupResponse>> ListStorageLocationsLookupAsync(Guid organizationId, Guid farmId, Guid inventoryItemId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SprayStorageLocationLookupResponse>>(StorageLocations);
    }
}
