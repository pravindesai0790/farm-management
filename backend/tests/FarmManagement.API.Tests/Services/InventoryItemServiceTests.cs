using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.Inventory;
using FarmManagement.Application.Interfaces.Inventory;
using FarmManagement.Application.Services;
using FarmManagement.Domain.Constants;
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

        var category = new InventoryItemCategory(null, "Fertilizers & Soil Amendments", "FERTILIZERS_SOIL", isSystem: true, id: FarmInventoryCategories.FertilizersSoilId);
        store.Categories.Add(category);

        var service = new InventoryItemService(store);
        var request = new CreateInventoryItemRequest("Urea 46%", unit.Id, category.Id, "FERT-001", "High-nitrogen fertilizer");

        // Act
        var result = await service.CreateAsync(CreateActor(), request, "127.0.0.1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Urea 46%", result.Name);
        Assert.Equal("FERT-001", result.Sku);
        Assert.Equal(category.Id, result.CategoryId);
        Assert.Equal("Fertilizers & Soil Amendments", result.CategoryName);
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
        var request = new CreateInventoryItemRequest("New Fertilizer", unit.Id, null, "FERT-001", null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(CreateActor(), request, "127.0.0.1"));

        Assert.Contains("FERT-001", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_WhenStockUnitChangedWithNoMovements_Succeeds()
    {
        // Arrange
        var store = new FakeInventoryItemStore();
        var unit1 = new Unit(null, "KG", "Kilogram", "kg", UnitCategory.Weight, null, 1.0m, true, 1);
        var unit2 = new Unit(null, "BAG", "Bag", "bag", UnitCategory.Count, null, 1.0m, true, 2);
        store.Units.Add(unit1);
        store.Units.Add(unit2);

        var item = new InventoryItem(_organizationId, "Seeds", unit1.Id, _userId);
        store.Items.Add(item);

        store.HasMovementsResult = false; // No transactions yet

        var service = new InventoryItemService(store);
        var request = new UpdateInventoryItemRequest("Seeds", unit2.Id, null, null, null);

        // Act
        var result = await service.UpdateAsync(CreateActor(), item.Id, request, "127.0.0.1");

        // Assert
        Assert.Equal(unit2.Id, result.StockUnitId);
        Assert.Equal("BAG", result.StockUnitCode);
    }

    [Fact]
    public async Task UpdateAsync_WhenStockUnitChangedAndMovementsExist_ThrowsValidationException()
    {
        // Arrange
        var store = new FakeInventoryItemStore();
        var unit1 = new Unit(null, "KG", "Kilogram", "kg", UnitCategory.Weight, null, 1.0m, true, 1);
        var unit2 = new Unit(null, "BAG", "Bag", "bag", UnitCategory.Count, null, 1.0m, true, 2);
        store.Units.Add(unit1);
        store.Units.Add(unit2);

        var item = new InventoryItem(_organizationId, "Seeds", unit1.Id, _userId);
        store.Items.Add(item);

        store.HasMovementsResult = true; // Movements already recorded!

        var service = new InventoryItemService(store);
        var request = new UpdateInventoryItemRequest("Seeds", unit2.Id, null, null, null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpdateAsync(CreateActor(), item.Id, request, "127.0.0.1"));

        Assert.Contains("stockUnitId", ex.Errors.Keys);
        Assert.Contains("cannot be changed once stock movements have been recorded", ex.Errors["stockUnitId"][0]);
    }

    [Fact]
    public async Task DeactivateAsync_WhenStockOnHandPositive_ThrowsValidationException()
    {
        // Arrange
        var store = new FakeInventoryItemStore();
        var unit = new Unit(null, "L", "Liter", "L", UnitCategory.Volume, null, 1.0m, true, 1);
        store.Units.Add(unit);

        var item = new InventoryItem(_organizationId, "Diesel", unit.Id, _userId);
        store.Items.Add(item);

        store.TotalQuantityOnHand = 500.0m; // Positive physical stock remains

        var service = new InventoryItemService(store);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.DeactivateAsync(CreateActor(), item.Id, "127.0.0.1"));

        Assert.Contains("isActive", ex.Errors.Keys);
        Assert.Contains("Cannot deactivate inventory item", ex.Errors["isActive"][0]);
        Assert.Contains("500", ex.Errors["isActive"][0]);
        Assert.True(item.IsActive); // Remains active
    }

    [Fact]
    public async Task DeactivateAsync_WhenStockOnHandZero_SuccessfullyDeactivates()
    {
        // Arrange
        var store = new FakeInventoryItemStore();
        var unit = new Unit(null, "L", "Liter", "L", UnitCategory.Volume, null, 1.0m, true, 1);
        store.Units.Add(unit);

        var item = new InventoryItem(_organizationId, "Diesel", unit.Id, _userId);
        store.Items.Add(item);

        store.TotalQuantityOnHand = 0.0m; // Zero stock

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

    [Fact]
    public async Task GetCategoriesAsync_ReturnsDefinedCategoriesInOrder()
    {
        // Arrange
        var store = new FakeInventoryItemStore();
        var service = new InventoryItemService(store);

        // Act
        var categories = await service.GetCategoriesAsync(CreateActor());

        // Assert
        Assert.NotNull(categories);
        Assert.NotEmpty(categories);
        Assert.Equal(13, categories.Count);
        Assert.Contains(categories, c => c.Code == "SEEDS_PLANTING" && c.Name == "Seeds & Planting Materials");
        Assert.Contains(categories, c => c.Code == "FERTILIZERS_SOIL" && c.Name == "Fertilizers & Soil Amendments");
        Assert.Contains(categories, c => c.Code == "SAFETY_PROTECTIVE_GEAR" && c.Name == "Safety & Protective Gear");

        // Verify ordering
        for (int i = 0; i < categories.Count - 1; i++)
        {
            Assert.True(categories[i].DisplayOrder <= categories[i + 1].DisplayOrder);
        }
    }
}

/// <summary>
/// Fake implementation of IInventoryItemStore for isolated unit testing of InventoryItemService.
/// </summary>
public sealed class FakeInventoryItemStore : IInventoryItemStore
{
    public List<InventoryItem> Items { get; } = new();
    public List<Unit> Units { get; } = new();
    public List<InventoryItemCategory> Categories { get; } = new();
    public List<AuditLog> AuditLogs { get; } = new();

    public decimal TotalQuantityOnHand { get; set; } = 0m;
    public bool HasMovementsResult { get; set; } = false;

    public Task<InventoryItem?> FindAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default)
    {
        var item = Items.FirstOrDefault(i => i.Id == id && i.OrganizationId == organizationId);
        if (item != null)
        {
            if (item.StockUnit == null)
            {
                var unit = Units.FirstOrDefault(u => u.Id == item.StockUnitId);
                if (unit != null)
                {
                    typeof(InventoryItem).GetProperty(nameof(InventoryItem.StockUnit))?.SetValue(item, unit);
                }
            }

            if (item.Category == null && item.CategoryId.HasValue)
            {
                var category = Categories.FirstOrDefault(c => c.Id == item.CategoryId.Value);
                if (category != null)
                {
                    typeof(InventoryItem).GetProperty(nameof(InventoryItem.Category))?.SetValue(item, category);
                }
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

    public Task<InventoryItemCategory?> FindCategoryAsync(Guid categoryId, Guid organizationId, CancellationToken cancellationToken = default)
    {
        var category = Categories.FirstOrDefault(c => c.Id == categoryId);
        return Task.FromResult(category);
    }

    public Task<IReadOnlyList<InventoryItemCategory>> ListCategoriesAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<InventoryItemCategory>>(Categories.OrderBy(c => c.DisplayOrder).ToList());
    }

    public Task<int> CountAsync(Guid organizationId, string? search, Guid? categoryId, bool? isActive, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Items.Count(i => i.OrganizationId == organizationId &&
            (!categoryId.HasValue || i.CategoryId == categoryId.Value) &&
            (!isActive.HasValue || i.IsActive == isActive.Value)));
    }

    public Task<IReadOnlyList<InventoryItem>> ListAsync(Guid organizationId, int skip, int take, string? search, Guid? categoryId, bool? isActive, CancellationToken cancellationToken = default)
    {
        var items = Items.Where(i => i.OrganizationId == organizationId &&
            (!categoryId.HasValue || i.CategoryId == categoryId.Value) &&
            (!isActive.HasValue || i.IsActive == isActive.Value))
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
