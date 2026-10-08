using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.PlantProtection;
using FarmManagement.Application.Interfaces.PlantProtection;
using FarmManagement.Application.Services;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Services;

public class PlantProtectionProductServiceTests
{
    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    private PlantProtectionActor CreateActor() => new(_userId, _organizationId);

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesPlantProtectionProductAndAuditLog()
    {
        // Arrange
        var store = new FakePlantProtectionProductStore();
        var unit = new Unit(null, "L", "Liter", "L", UnitCategory.Volume, null, 1.0m, true, 1);
        var item = new InventoryItem(_organizationId, "Azoxystrobin 23% SC", unit.Id, _userId, "CHEM-AZOX-01");
        store.InventoryItems.Add(item);

        var productType = new ProductType(null, "FUNGICIDE", "Fungicide", isSystem: true);
        store.ProductTypes.Add(productType);

        var service = new PlantProtectionProductService(store);
        var request = new CreatePlantProtectionProductRequest(
            InventoryItemId: item.Id,
            ProductTypeId: productType.Id,
            ActiveIngredient: "Azoxystrobin 23%",
            Manufacturer: "Syngenta",
            Description: "Broad-spectrum systemic fungicide");

        // Act
        var result = await service.CreateAsync(CreateActor(), request, "127.0.0.1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(item.Id, result.InventoryItemId);
        Assert.Equal("Azoxystrobin 23% SC", result.InventoryItemName);
        Assert.Equal("CHEM-AZOX-01", result.InventoryItemSku);
        Assert.Equal(productType.Id, result.ProductTypeId);
        Assert.Equal("Fungicide", result.ProductTypeName);
        Assert.Equal("FUNGICIDE", result.ProductTypeCode);
        Assert.Equal("Azoxystrobin 23%", result.ActiveIngredient);
        Assert.Equal("Syngenta", result.Manufacturer);
        Assert.Equal("Broad-spectrum systemic fungicide", result.Description);
        Assert.True(result.IsActive);
        Assert.False(result.HasCompletedSprayUsage);
        Assert.Single(store.Products);
        Assert.Single(store.AuditLogs);
        Assert.Equal("PlantProtectionProduct.Created", store.AuditLogs[0].Action);
    }

    [Fact]
    public async Task CreateAsync_InventoryItemNotFound_ThrowsResourceNotFoundException()
    {
        // Arrange
        var store = new FakePlantProtectionProductStore();
        var productType = new ProductType(null, "FUNGICIDE", "Fungicide", isSystem: true);
        store.ProductTypes.Add(productType);

        var service = new PlantProtectionProductService(store);
        var request = new CreatePlantProtectionProductRequest(Guid.NewGuid(), productType.Id);

        // Act & Assert
        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            service.CreateAsync(CreateActor(), request, "127.0.0.1"));
    }

    [Fact]
    public async Task CreateAsync_InventoryItemDifferentOrganization_ThrowsResourceNotFoundException()
    {
        // Arrange
        var otherOrgId = Guid.NewGuid();
        var store = new FakePlantProtectionProductStore();
        var unit = new Unit(null, "L", "Liter", "L", UnitCategory.Volume, null, 1.0m, true, 1);
        var item = new InventoryItem(otherOrgId, "Other Org Item", unit.Id, _userId);
        store.InventoryItems.Add(item);

        var productType = new ProductType(null, "FUNGICIDE", "Fungicide", isSystem: true);
        store.ProductTypes.Add(productType);

        var service = new PlantProtectionProductService(store);
        var request = new CreatePlantProtectionProductRequest(item.Id, productType.Id);

        // Act & Assert
        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            service.CreateAsync(CreateActor(), request, "127.0.0.1"));
    }

    [Fact]
    public async Task CreateAsync_InactiveInventoryItem_ThrowsValidationException()
    {
        // Arrange
        var store = new FakePlantProtectionProductStore();
        var unit = new Unit(null, "L", "Liter", "L", UnitCategory.Volume, null, 1.0m, true, 1);
        var item = new InventoryItem(_organizationId, "Inactive Item", unit.Id, _userId);
        item.Deactivate(DateTimeOffset.UtcNow, _userId);
        store.InventoryItems.Add(item);

        var productType = new ProductType(null, "FUNGICIDE", "Fungicide", isSystem: true);
        store.ProductTypes.Add(productType);

        var service = new PlantProtectionProductService(store);
        var request = new CreatePlantProtectionProductRequest(item.Id, productType.Id);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(CreateActor(), request, "127.0.0.1"));

        Assert.Contains("inactive", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_DuplicateProfileForSameItem_ThrowsConflictException()
    {
        // Arrange
        var store = new FakePlantProtectionProductStore();
        var unit = new Unit(null, "L", "Liter", "L", UnitCategory.Volume, null, 1.0m, true, 1);
        var item = new InventoryItem(_organizationId, "Azoxystrobin 23% SC", unit.Id, _userId);
        store.InventoryItems.Add(item);

        var productType = new ProductType(null, "FUNGICIDE", "Fungicide", isSystem: true);
        store.ProductTypes.Add(productType);

        var existingProfile = new PlantProtectionProduct(_organizationId, item.Id, productType.Id, _userId);
        store.Products.Add(existingProfile);

        var service = new PlantProtectionProductService(store);
        var request = new CreatePlantProtectionProductRequest(item.Id, productType.Id);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(CreateActor(), request, "127.0.0.1"));

        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_InactiveProductType_ThrowsValidationException()
    {
        // Arrange
        var store = new FakePlantProtectionProductStore();
        var unit = new Unit(null, "L", "Liter", "L", UnitCategory.Volume, null, 1.0m, true, 1);
        var item = new InventoryItem(_organizationId, "Azoxystrobin 23% SC", unit.Id, _userId);
        store.InventoryItems.Add(item);

        var productType = new ProductType(_organizationId, "CUSTOM_CHEM", "Custom Chem", isSystem: false);
        productType.Deactivate(DateTimeOffset.UtcNow, _userId);
        store.ProductTypes.Add(productType);

        var service = new PlantProtectionProductService(store);
        var request = new CreatePlantProtectionProductRequest(item.Id, productType.Id);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(CreateActor(), request, "127.0.0.1"));
    }

    [Fact]
    public async Task GetAsync_ValidId_ReturnsMappedResponse()
    {
        // Arrange
        var store = new FakePlantProtectionProductStore();
        var unit = new Unit(null, "L", "Liter", "L", UnitCategory.Volume, null, 1.0m, true, 1);
        var item = new InventoryItem(_organizationId, "Azoxystrobin 23% SC", unit.Id, _userId);
        store.InventoryItems.Add(item);

        var productType = new ProductType(null, "FUNGICIDE", "Fungicide", isSystem: true);
        store.ProductTypes.Add(productType);

        var product = new PlantProtectionProduct(_organizationId, item.Id, productType.Id, _userId, "Azoxystrobin", "Syngenta");
        store.Products.Add(product);

        var service = new PlantProtectionProductService(store);

        // Act
        var result = await service.GetAsync(CreateActor(), product.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(product.Id, result.Id);
        Assert.Equal(item.Id, result.InventoryItemId);
        Assert.Equal("Azoxystrobin 23% SC", result.InventoryItemName);
        Assert.Equal("Fungicide", result.ProductTypeName);
        Assert.Equal("Syngenta", result.Manufacturer);
    }

    [Fact]
    public async Task GetAsync_NotFoundOrCrossTenant_ThrowsResourceNotFoundException()
    {
        // Arrange
        var store = new FakePlantProtectionProductStore();
        var service = new PlantProtectionProductService(store);

        // Act & Assert
        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            service.GetAsync(CreateActor(), Guid.NewGuid()));
    }

    [Fact]
    public async Task ListAsync_PagingAndFiltering_ReturnsPagedResponse()
    {
        // Arrange
        var store = new FakePlantProtectionProductStore();
        var unit = new Unit(null, "L", "Liter", "L", UnitCategory.Volume, null, 1.0m, true, 1);

        var pt1 = new ProductType(null, "FUNGICIDE", "Fungicide", isSystem: true);
        var pt2 = new ProductType(null, "INSECTICIDE", "Insecticide", isSystem: true);
        store.ProductTypes.Add(pt1);
        store.ProductTypes.Add(pt2);

        var item1 = new InventoryItem(_organizationId, "Azoxystrobin", unit.Id, _userId);
        var item2 = new InventoryItem(_organizationId, "Imidacloprid", unit.Id, _userId);
        store.InventoryItems.Add(item1);
        store.InventoryItems.Add(item2);

        var p1 = new PlantProtectionProduct(_organizationId, item1.Id, pt1.Id, _userId, "Azoxystrobin");
        var p2 = new PlantProtectionProduct(_organizationId, item2.Id, pt2.Id, _userId, "Imidacloprid");
        store.Products.Add(p1);
        store.Products.Add(p2);

        var service = new PlantProtectionProductService(store);

        // Act
        var result = await service.ListAsync(CreateActor(), 1, 20, "Azox", null, true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal(p1.Id, result.Items[0].Id);
    }

    [Fact]
    public async Task UpdateAsync_WhenNoCompletedSpray_AllowsChangingProductTypeAndMetadata()
    {
        // Arrange
        var store = new FakePlantProtectionProductStore();
        var unit = new Unit(null, "L", "Liter", "L", UnitCategory.Volume, null, 1.0m, true, 1);
        var item = new InventoryItem(_organizationId, "Dual Action Product", unit.Id, _userId);
        store.InventoryItems.Add(item);

        var oldType = new ProductType(null, "FUNGICIDE", "Fungicide", isSystem: true);
        var newType = new ProductType(null, "INSECTICIDE", "Insecticide", isSystem: true);
        store.ProductTypes.Add(oldType);
        store.ProductTypes.Add(newType);

        var product = new PlantProtectionProduct(_organizationId, item.Id, oldType.Id, _userId, "Old Ing", "Old Mfr", "Old Desc");
        store.Products.Add(product);

        var service = new PlantProtectionProductService(store);
        var request = new UpdatePlantProtectionProductRequest(newType.Id, "New Ing", "New Mfr", "New Desc");

        // Act
        var result = await service.UpdateAsync(CreateActor(), product.Id, request, "127.0.0.1");

        // Assert
        Assert.Equal(newType.Id, result.ProductTypeId);
        Assert.Equal("New Ing", result.ActiveIngredient);
        Assert.Equal("New Mfr", result.Manufacturer);
        Assert.Equal("New Desc", result.Description);
        Assert.Single(store.AuditLogs);
        Assert.Equal("PlantProtectionProduct.Updated", store.AuditLogs[0].Action);
    }

    [Fact]
    public async Task UpdateAsync_WhenSprayCompleted_ProductTypeUnchanged_AllowsProfileUpdate()
    {
        // Arrange
        var store = new FakePlantProtectionProductStore();
        var unit = new Unit(null, "L", "Liter", "L", UnitCategory.Volume, null, 1.0m, true, 1);
        var item = new InventoryItem(_organizationId, "Used Product", unit.Id, _userId);
        store.InventoryItems.Add(item);

        var productType = new ProductType(null, "FUNGICIDE", "Fungicide", isSystem: true);
        store.ProductTypes.Add(productType);

        var product = new PlantProtectionProduct(_organizationId, item.Id, productType.Id, _userId, "Original Ing", "Mfr A");
        store.Products.Add(product);
        store.CompletedSprayUsageItems.Add(item.Id); // Mark as used in completed spray

        var service = new PlantProtectionProductService(store);
        var request = new UpdatePlantProtectionProductRequest(productType.Id, "Updated Ing", "Mfr B", "Updated note");

        // Act
        var result = await service.UpdateAsync(CreateActor(), product.Id, request, "127.0.0.1");

        // Assert
        Assert.Equal(productType.Id, result.ProductTypeId);
        Assert.Equal("Updated Ing", result.ActiveIngredient);
        Assert.Equal("Mfr B", result.Manufacturer);
        Assert.True(result.HasCompletedSprayUsage);
    }

    [Fact]
    public async Task UpdateAsync_WhenSprayCompleted_ProductTypeChanged_ThrowsValidationException()
    {
        // Arrange
        var store = new FakePlantProtectionProductStore();
        var unit = new Unit(null, "L", "Liter", "L", UnitCategory.Volume, null, 1.0m, true, 1);
        var item = new InventoryItem(_organizationId, "Used Product", unit.Id, _userId);
        store.InventoryItems.Add(item);

        var oldType = new ProductType(null, "FUNGICIDE", "Fungicide", isSystem: true);
        var newType = new ProductType(null, "INSECTICIDE", "Insecticide", isSystem: true);
        store.ProductTypes.Add(oldType);
        store.ProductTypes.Add(newType);

        var product = new PlantProtectionProduct(_organizationId, item.Id, oldType.Id, _userId);
        store.Products.Add(product);
        store.CompletedSprayUsageItems.Add(item.Id); // Mark as used in completed spray

        var service = new PlantProtectionProductService(store);
        var request = new UpdatePlantProtectionProductRequest(newType.Id, "Updated Ing");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpdateAsync(CreateActor(), product.Id, request, "127.0.0.1"));

        Assert.Contains("completed spray application", ex.Message);
    }

    [Fact]
    public async Task ActivateAsync_WhenInactive_ActivatesAndLogsAudit()
    {
        // Arrange
        var store = new FakePlantProtectionProductStore();
        var unit = new Unit(null, "L", "Liter", "L", UnitCategory.Volume, null, 1.0m, true, 1);
        var item = new InventoryItem(_organizationId, "Product", unit.Id, _userId);
        store.InventoryItems.Add(item);

        var productType = new ProductType(null, "FUNGICIDE", "Fungicide", isSystem: true);
        store.ProductTypes.Add(productType);

        var product = new PlantProtectionProduct(_organizationId, item.Id, productType.Id, _userId);
        product.Deactivate(DateTimeOffset.UtcNow, _userId);
        store.Products.Add(product);

        var service = new PlantProtectionProductService(store);

        // Act
        var result = await service.ActivateAsync(CreateActor(), product.Id, "127.0.0.1");

        // Assert
        Assert.True(result);
        Assert.True(product.IsActive);
        Assert.Single(store.AuditLogs);
        Assert.Equal("PlantProtectionProduct.Activated", store.AuditLogs[0].Action);
    }

    [Fact]
    public async Task ActivateAsync_WhenAlreadyActive_ReturnsFalse()
    {
        // Arrange
        var store = new FakePlantProtectionProductStore();
        var unit = new Unit(null, "L", "Liter", "L", UnitCategory.Volume, null, 1.0m, true, 1);
        var item = new InventoryItem(_organizationId, "Product", unit.Id, _userId);
        store.InventoryItems.Add(item);

        var productType = new ProductType(null, "FUNGICIDE", "Fungicide", isSystem: true);
        store.ProductTypes.Add(productType);

        var product = new PlantProtectionProduct(_organizationId, item.Id, productType.Id, _userId);
        store.Products.Add(product);

        var service = new PlantProtectionProductService(store);

        // Act
        var result = await service.ActivateAsync(CreateActor(), product.Id, "127.0.0.1");

        // Assert
        Assert.False(result);
        Assert.Empty(store.AuditLogs);
    }

    [Fact]
    public async Task DeactivateAsync_WhenActive_DeactivatesAndLogsAudit()
    {
        // Arrange
        var store = new FakePlantProtectionProductStore();
        var unit = new Unit(null, "L", "Liter", "L", UnitCategory.Volume, null, 1.0m, true, 1);
        var item = new InventoryItem(_organizationId, "Product", unit.Id, _userId);
        store.InventoryItems.Add(item);

        var productType = new ProductType(null, "FUNGICIDE", "Fungicide", isSystem: true);
        store.ProductTypes.Add(productType);

        var product = new PlantProtectionProduct(_organizationId, item.Id, productType.Id, _userId);
        store.Products.Add(product);

        var service = new PlantProtectionProductService(store);

        // Act
        var result = await service.DeactivateAsync(CreateActor(), product.Id, "127.0.0.1");

        // Assert
        Assert.True(result);
        Assert.False(product.IsActive);
        Assert.Single(store.AuditLogs);
        Assert.Equal("PlantProtectionProduct.Deactivated", store.AuditLogs[0].Action);
    }

    [Fact]
    public async Task DeactivateAsync_WhenAlreadyInactive_ReturnsFalse()
    {
        // Arrange
        var store = new FakePlantProtectionProductStore();
        var unit = new Unit(null, "L", "Liter", "L", UnitCategory.Volume, null, 1.0m, true, 1);
        var item = new InventoryItem(_organizationId, "Product", unit.Id, _userId);
        store.InventoryItems.Add(item);

        var productType = new ProductType(null, "FUNGICIDE", "Fungicide", isSystem: true);
        store.ProductTypes.Add(productType);

        var product = new PlantProtectionProduct(_organizationId, item.Id, productType.Id, _userId);
        product.Deactivate(DateTimeOffset.UtcNow, _userId);
        store.Products.Add(product);

        var service = new PlantProtectionProductService(store);

        // Act
        var result = await service.DeactivateAsync(CreateActor(), product.Id, "127.0.0.1");

        // Assert
        Assert.False(result);
        Assert.Empty(store.AuditLogs);
    }

    [Fact]
    public async Task ValidateActor_EmptyUserIdOrOrganizationId_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var store = new FakePlantProtectionProductStore();
        var service = new PlantProtectionProductService(store);
        var invalidActor = new PlantProtectionActor(Guid.Empty, _organizationId);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.GetAsync(invalidActor, Guid.NewGuid()));
    }

    private sealed class FakePlantProtectionProductStore : IPlantProtectionProductStore
    {
        public List<PlantProtectionProduct> Products { get; } = [];
        public List<InventoryItem> InventoryItems { get; } = [];
        public List<ProductType> ProductTypes { get; } = [];
        public HashSet<Guid> CompletedSprayUsageItems { get; } = [];
        public List<AuditLog> AuditLogs { get; } = [];

        public Task<PlantProtectionProduct?> FindAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default)
        {
            var product = Products.FirstOrDefault(p => p.Id == id && p.OrganizationId == organizationId);
            if (product != null)
            {
                AttachNavigations(product);
            }

            return Task.FromResult(product);
        }

        public Task<PlantProtectionProduct?> FindByInventoryItemIdAsync(Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default)
        {
            var product = Products.FirstOrDefault(p => p.InventoryItemId == inventoryItemId && p.OrganizationId == organizationId);
            if (product != null)
            {
                AttachNavigations(product);
            }

            return Task.FromResult(product);
        }

        public Task<InventoryItem?> FindInventoryItemAsync(Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(InventoryItems.FirstOrDefault(i => i.Id == inventoryItemId && i.OrganizationId == organizationId));

        public Task<ProductType?> FindProductTypeAsync(Guid productTypeId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ProductTypes.FirstOrDefault(pt => pt.Id == productTypeId && pt.IsActive &&
                ((pt.IsSystem && pt.OrganizationId == null) || pt.OrganizationId == organizationId)));

        public Task<int> CountAsync(Guid organizationId, string? search, Guid? productTypeId, bool? isActive, CancellationToken cancellationToken = default) =>
            Task.FromResult(QueryProducts(organizationId, search, productTypeId, isActive).Count());

        public Task<IReadOnlyList<PlantProtectionProduct>> ListAsync(Guid organizationId, int skip, int take, string? search, Guid? productTypeId, bool? isActive, CancellationToken cancellationToken = default)
        {
            var list = QueryProducts(organizationId, search, productTypeId, isActive)
                .Skip(skip)
                .Take(take)
                .ToList();

            foreach (var p in list)
            {
                AttachNavigations(p);
            }

            return Task.FromResult<IReadOnlyList<PlantProtectionProduct>>(list);
        }

        public Task<bool> HasCompletedSprayUsageAsync(Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(CompletedSprayUsageItems.Contains(inventoryItemId));

        public void Add(PlantProtectionProduct product)
        {
            Products.Add(product);
            AttachNavigations(product);
        }

        public void AddAuditLog(AuditLog auditLog) => AuditLogs.Add(auditLog);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        private IEnumerable<PlantProtectionProduct> QueryProducts(Guid organizationId, string? search, Guid? productTypeId, bool? isActive)
        {
            var q = Products.Where(p => p.OrganizationId == organizationId);
            if (isActive.HasValue)
            {
                q = q.Where(p => p.IsActive == isActive.Value);
            }

            if (productTypeId.HasValue && productTypeId.Value != Guid.Empty)
            {
                q = q.Where(p => p.ProductTypeId == productTypeId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLowerInvariant();
                q = q.Where(p =>
                {
                    var item = InventoryItems.FirstOrDefault(i => i.Id == p.InventoryItemId);
                    return (item != null && item.Name.ToLowerInvariant().Contains(s)) ||
                           (item != null && item.Sku != null && item.Sku.ToLowerInvariant().Contains(s)) ||
                           (p.ActiveIngredient != null && p.ActiveIngredient.ToLowerInvariant().Contains(s)) ||
                           (p.Manufacturer != null && p.Manufacturer.ToLowerInvariant().Contains(s));
                });
            }

            return q;
        }

        private void AttachNavigations(PlantProtectionProduct product)
        {
            var item = InventoryItems.FirstOrDefault(i => i.Id == product.InventoryItemId);
            var pt = ProductTypes.FirstOrDefault(t => t.Id == product.ProductTypeId);

            typeof(PlantProtectionProduct).GetProperty(nameof(PlantProtectionProduct.InventoryItem))?.SetValue(product, item);
            typeof(PlantProtectionProduct).GetProperty(nameof(PlantProtectionProduct.ProductType))?.SetValue(product, pt);
        }
    }
}
