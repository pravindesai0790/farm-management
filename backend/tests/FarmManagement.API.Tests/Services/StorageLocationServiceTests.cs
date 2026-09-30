using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.Inventory;
using FarmManagement.Application.Interfaces.Inventory;
using FarmManagement.Application.Services;
using FarmManagement.Domain.Entities;
using Xunit;

namespace FarmManagement.API.Tests.Services;

/// <summary>
/// Unit tests for StorageLocationService verifying location CRUD, name uniqueness per farm,
/// and active stock deactivation safety invariants.
/// </summary>
public class StorageLocationServiceTests
{
    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    private InventoryActor CreateActor() => new(_userId, _organizationId);

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesStorageLocationAndAuditLog()
    {
        // Arrange
        var store = new FakeStorageLocationStore();
        var farm = new Farm(_organizationId, "North Valley Farm", Guid.NewGuid(), _userId);
        store.Farms.Add(farm);

        var service = new StorageLocationService(store);
        var request = new CreateStorageLocationRequest(farm.Id, "Shed 01", "Chemical & Fertilizer Depot");

        // Act
        var result = await service.CreateAsync(CreateActor(), request, "127.0.0.1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Shed 01", result.Name);
        Assert.Equal(farm.Id, result.FarmId);
        Assert.Equal("North Valley Farm", result.FarmName);
        Assert.Single(store.Locations);
        Assert.Single(store.AuditLogs);
    }

    [Fact]
    public async Task CreateAsync_DuplicateNameInSameFarm_ThrowsConflictException()
    {
        // Arrange
        var store = new FakeStorageLocationStore();
        var farm = new Farm(_organizationId, "North Valley Farm", Guid.NewGuid(), _userId);
        store.Farms.Add(farm);

        var existingLocation = new StorageLocation(_organizationId, farm.Id, "Shed 01", _userId);
        store.Locations.Add(existingLocation);

        var service = new StorageLocationService(store);
        var request = new CreateStorageLocationRequest(farm.Id, "shed 01 ", "Duplicate name test");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(CreateActor(), request, "127.0.0.1"));

        Assert.Contains("shed 01", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateAsync_DuplicateNameInSameFarm_ThrowsConflictException()
    {
        // Arrange
        var store = new FakeStorageLocationStore();
        var farm = new Farm(_organizationId, "North Valley Farm", Guid.NewGuid(), _userId);
        store.Farms.Add(farm);

        var locA = new StorageLocation(_organizationId, farm.Id, "Main Warehouse", _userId);
        var locB = new StorageLocation(_organizationId, farm.Id, "Secondary Shed", _userId);
        store.Locations.Add(locA);
        store.Locations.Add(locB);

        var service = new StorageLocationService(store);
        var updateRequest = new UpdateStorageLocationRequest("Main Warehouse", "Attempting rename collision");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            service.UpdateAsync(CreateActor(), locB.Id, updateRequest, "127.0.0.1"));

        Assert.Contains("Main Warehouse", ex.Message);
    }

    [Fact]
    public async Task DeactivateAsync_WhenActiveStockExists_ThrowsValidationException()
    {
        // Arrange
        var store = new FakeStorageLocationStore();
        var farm = new Farm(_organizationId, "North Valley Farm", Guid.NewGuid(), _userId);
        store.Farms.Add(farm);

        var loc = new StorageLocation(_organizationId, farm.Id, "Chemical Shed", _userId);
        store.Locations.Add(loc);
        store.ItemsWithStockCount = 3; // 3 distinct items currently stored in shed

        var service = new StorageLocationService(store);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.DeactivateAsync(CreateActor(), loc.Id, "127.0.0.1"));

        Assert.Contains("Cannot deactivate storage location", ex.Message);
        Assert.Contains("holds active stock for 3 item(s)", ex.Message);
        Assert.True(loc.IsActive); // Location remains active
    }

    [Fact]
    public async Task DeactivateAsync_WhenStockIsZero_SuccessfullyDeactivates()
    {
        // Arrange
        var store = new FakeStorageLocationStore();
        var farm = new Farm(_organizationId, "North Valley Farm", Guid.NewGuid(), _userId);
        store.Farms.Add(farm);

        var loc = new StorageLocation(_organizationId, farm.Id, "Chemical Shed", _userId);
        store.Locations.Add(loc);
        store.ItemsWithStockCount = 0; // Empty location

        var service = new StorageLocationService(store);

        // Act
        var result = await service.DeactivateAsync(CreateActor(), loc.Id, "127.0.0.1");

        // Assert
        Assert.True(result);
        Assert.False(loc.IsActive);
    }

    [Fact]
    public async Task ActivateAsync_WhenInactive_SuccessfullyActivates()
    {
        // Arrange
        var store = new FakeStorageLocationStore();
        var farm = new Farm(_organizationId, "North Valley Farm", Guid.NewGuid(), _userId);
        store.Farms.Add(farm);

        var loc = new StorageLocation(_organizationId, farm.Id, "Old Barn", _userId);
        loc.Deactivate(DateTimeOffset.UtcNow, _userId);
        store.Locations.Add(loc);

        var service = new StorageLocationService(store);

        // Act
        var result = await service.ActivateAsync(CreateActor(), loc.Id, "127.0.0.1");

        // Assert
        Assert.True(result);
        Assert.True(loc.IsActive);
    }
}

/// <summary>
/// Fake implementation of IStorageLocationStore for isolated unit testing of StorageLocationService.
/// </summary>
public sealed class FakeStorageLocationStore : IStorageLocationStore
{
    public List<StorageLocation> Locations { get; } = new();
    public List<Farm> Farms { get; } = new();
    public List<AuditLog> AuditLogs { get; } = new();

    public int ItemsWithStockCount { get; set; } = 0;

    public Task<StorageLocation?> FindAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default)
    {
        var loc = Locations.FirstOrDefault(l => l.Id == id && l.OrganizationId == organizationId);
        return Task.FromResult(loc);
    }

    public Task<Farm?> FindFarmAsync(Guid farmId, Guid organizationId, CancellationToken cancellationToken = default)
    {
        var farm = Farms.FirstOrDefault(f => f.Id == farmId && f.OrganizationId == organizationId);
        return Task.FromResult(farm);
    }

    public Task<int> CountAsync(Guid organizationId, Guid? farmId, bool? isActive, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Locations.Count(l => l.OrganizationId == organizationId &&
            (!farmId.HasValue || l.FarmId == farmId.Value) &&
            (!isActive.HasValue || l.IsActive == isActive.Value)));
    }

    public Task<IReadOnlyList<StorageLocation>> ListAsync(Guid organizationId, Guid? farmId, int skip, int take, bool? isActive, CancellationToken cancellationToken = default)
    {
        var locations = Locations.Where(l => l.OrganizationId == organizationId &&
            (!farmId.HasValue || l.FarmId == farmId.Value) &&
            (!isActive.HasValue || l.IsActive == isActive.Value))
            .Skip(skip).Take(take).ToList();
        return Task.FromResult<IReadOnlyList<StorageLocation>>(locations);
    }

    public Task<bool> ExistsNameInFarmAsync(Guid farmId, string name, Guid? excludeLocationId = null, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim();
        var exists = Locations.Any(l => l.FarmId == farmId &&
            (!excludeLocationId.HasValue || l.Id != excludeLocationId.Value) &&
            l.Name.Equals(normalizedName, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(exists);
    }

    public Task<int> CountItemsWithStockAsync(Guid locationId, Guid organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(ItemsWithStockCount);

    public void Add(StorageLocation location) => Locations.Add(location);
    public void AddAuditLog(AuditLog auditLog) => AuditLogs.Add(auditLog);
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
