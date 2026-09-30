using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.Inventory;
using FarmManagement.Application.Interfaces.Inventory;
using FarmManagement.Application.Services;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Services;

/// <summary>
/// Unit tests for InventoryItemService verifying item CRUD, SKU uniqueness,
/// stock unit immutability when movements exist, and deactivation safety invariants.
/// </summary>
public class InventoryItemServiceTests
{
    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    private InventoryActor CreateActor() => new(_userId, _organizationId);

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesItemAndAuditLog()
    {
        // Arrange
        var store = new FakeInventoryItemStore();
        var unit = new Unit(null, "KG", "Kilogram", "kg", UnitCategory.Weight, null, 1.0m, true, 1);
        store.Units.Add(unit);

        var service = new InventoryItemService(store);
        var request = new CreateInventoryItemRequest("Urea 46%", unit.Id, "FERT-001", "High-nitrogen fertilizer", "Fertilizer");

        // Act
        var result = await service.CreateAsync(CreateActor(), request, "127.0.0.1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Urea 46%", result.Name);
        Assert.Equal("FERT-001", result.Sku);
        Assert.Equal("Fertilizer", result.Category);
        Assert.Single(store.Items);
        Assert.Single(store.AuditLogs);
    }

    [Fact]
    public async Task CreateAsync_DuplicateSku_ThrowsConflictException()
    {
        // Arrange
        var store = new FakeInventoryItemStore();
        var unit = new Unit(null, "KG", "Kilogram", "kg", UnitCategory.Weight, null, 1.0m, true, 1);
        store.Units.Add(unit);

        var existingItem = new InventoryItem(_organizationId, "Existing Fertilizer", unit.Id, _userId, "FERT-001");
        store.Items.Add(existingItem);

        var service = new InventoryItemService(store);
        var request = new CreateInventoryItemRequest("New Fertilizer", unit.Id, "FERT-001", null, null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(CreateActor(), request, "127.0.0.1"));

        Assert.Contains("FERT-001", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_WhenStockMovementsExist_CannotChangeStockUnit()
    {
        // Arrange
        var store = new FakeInventoryItemStore();
        var kgUnit = new Unit(null, "KG", "Kilogram", "kg", UnitCategory.Weight, null, 1.0m, true, 1);
        var literUnit = new Unit(null, "LITER", "Liter", "L", UnitCategory.Volume, null, 1.0m, true, 2);
        store.Units.Add(kgUnit);
        store.Units.Add(literUnit);

        var item = new InventoryItem(_organizationId, "Urea Fertilizer", kgUnit.Id, _userId, "FERT-001");
        store.Items.Add(item);
        store.HasMovementsResult = true; // Simulate historical stock transactions exist

        var service = new InventoryItemService(store);
        var updateRequest = new UpdateInventoryItemRequest("Urea Fertilizer", literUnit.Id, "FERT-001", null, null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpdateAsync(CreateActor(), item.Id, updateRequest, "127.0.0.1"));

        Assert.Contains("stock unit of measurement cannot be changed", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_WhenNoStockMovementsExist_CanChangeStockUnit()
    {
        // Arrange
        var store = new FakeInventoryItemStore();
        var kgUnit = new Unit(null, "KG", "Kilogram", "kg", UnitCategory.Weight, null, 1.0m, true, 1);
        var literUnit = new Unit(null, "LITER", "Liter", "L", UnitCategory.Volume, null, 1.0m, true, 2);
        store.Units.Add(kgUnit);
        store.Units.Add(literUnit);

        var item = new InventoryItem(_organizationId, "Urea Fertilizer", kgUnit.Id, _userId, "FERT-001");
        store.Items.Add(item);
        store.HasMovementsResult = false; // No transactions recorded yet

        var service = new InventoryItemService(store);
        var updateRequest = new UpdateInventoryItemRequest("Urea Fertilizer", literUnit.Id, "FERT-001", null, null);

        // Act
        var result = await service.UpdateAsync(CreateActor(), item.Id, updateRequest, "127.0.0.1");

        // Assert
        Assert.Equal(literUnit.Id, result.StockUnitId);
        Assert.Equal("LITER", result.StockUnitCode);
    }

    [Fact]
    public async Task DeactivateAsync_WhenPositiveStockExists_ThrowsValidationException()
    {
        // Arrange
        var store = new FakeInventoryItemStore();
        var unit = new Unit(null, "KG", "Kilogram", "kg", UnitCategory.Weight, null, 1.0m, true, 1);
        store.Units.Add(unit);

        var item = new InventoryItem(_organizationId, "Pesticide Alpha", unit.Id, _userId);
        store.Items.Add(item);
        store.TotalQuantityOnHand = 150.5m; // Active stock on hand

        var service = new InventoryItemService(store);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.DeactivateAsync(CreateActor(), item.Id, "127.0.0.1"));

        Assert.Contains("Cannot deactivate inventory item", ex.Message);
        Assert.Contains("150.5 kg on hand", ex.Message);
        Assert.True(item.IsActive); // Item remains active
    }

    [Fact]
    public async Task DeactivateAsync_WhenStockIsZero_SuccessfullyDeactivates()
    {
        // Arrange
        var store = new FakeInventoryItemStore();
        var unit = new Unit(null, "KG", "Kilogram", "kg", UnitCategory.Weight, null, 1.0m, true, 1);
        store.Units.Add(unit);

        var item = new InventoryItem(_organizationId, "Pesticide Alpha", unit.Id, _userId);
        store.Items.Add(item);
        store.TotalQuantityOnHand = 0m; // Stock fully depleted

        var service = new InventoryItemService(store);

        // Act
        var result = await service.DeactivateAsync(CreateActor(), item.Id, "127.0.0.1");

        // Assert
        Assert.True(result);
        Assert.False(item.IsActive);
    }

    [Fact]
    public async Task ActivateAsync_WhenInactive_SuccessfullyActivates()
    {
        // Arrange
        var store = new FakeInventoryItemStore();
        var unit = new Unit(null, "KG", "Kilogram", "kg", UnitCategory.Weight, null, 1.0m, true, 1);
        store.Units.Add(unit);

        var item = new InventoryItem(_organizationId, "Legacy Seed Pack", unit.Id, _userId);
        item.Deactivate(DateTimeOffset.UtcNow, _userId);
        store.Items.Add(item);

        var service = new InventoryItemService(store);

        // Act
        var result = await service.ActivateAsync(CreateActor(), item.Id, "127.0.0.1");

        // Assert
        Assert.True(result);
        Assert.True(item.IsActive);
    }
}

/// <summary>
/// Fake implementation of IInventoryItemStore for isolated unit testing of InventoryItemService.
/// </summary>
public sealed class FakeInventoryItemStore : IInventoryItemStore
{
    public List<InventoryItem> Items { get; } = new();
    public List<Unit> Units { get; } = new();
    public List<AuditLog> AuditLogs { get; } = new();

    public decimal TotalQuantityOnHand { get; set; } = 0m;
    public bool HasMovementsResult { get; set; } = false;

    public Task<InventoryItem?> FindAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default)
    {
        var item = Items.FirstOrDefault(i => i.Id == id && i.OrganizationId == organizationId);
        if (item != null && item.StockUnit == null)
        {
            var unit = Units.FirstOrDefault(u => u.Id == item.StockUnitId);
            if (unit != null)
            {
                // Assign StockUnit navigation property as done by EF Core Include
                typeof(InventoryItem).GetProperty(nameof(InventoryItem.StockUnit))?.SetValue(item, unit);
            }
        }
        return Task.FromResult(item);
    }

    public Task<InventoryItem?> FindBySkuAsync(string sku, Guid organizationId, CancellationToken cancellationToken = default)
    {
        var item = Items.FirstOrDefault(i => i.OrganizationId == organizationId && i.Sku != null && i.Sku.Equals(sku.Trim(), StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(item);
    }

    public Task<Unit?> FindUnitAsync(Guid unitId, Guid organizationId, CancellationToken cancellationToken = default)
    {
        var unit = Units.FirstOrDefault(u => u.Id == unitId);
        return Task.FromResult(unit);
    }

    public Task<int> CountAsync(Guid organizationId, string? search, string? category, bool? isActive, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Items.Count(i => i.OrganizationId == organizationId && (!isActive.HasValue || i.IsActive == isActive.Value)));
    }

    public Task<IReadOnlyList<InventoryItem>> ListAsync(Guid organizationId, int skip, int take, string? search, string? category, bool? isActive, CancellationToken cancellationToken = default)
    {
        var items = Items.Where(i => i.OrganizationId == organizationId && (!isActive.HasValue || i.IsActive == isActive.Value))
            .Skip(skip).Take(take).ToList();
        return Task.FromResult<IReadOnlyList<InventoryItem>>(items);
    }

    public Task<decimal> GetTotalQuantityOnHandAsync(Guid itemId, Guid organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(TotalQuantityOnHand);

    public Task<bool> HasMovementsAsync(Guid itemId, Guid organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(HasMovementsResult);

    public void Add(InventoryItem item) => Items.Add(item);
    public void AddAuditLog(AuditLog auditLog) => AuditLogs.Add(auditLog);
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
