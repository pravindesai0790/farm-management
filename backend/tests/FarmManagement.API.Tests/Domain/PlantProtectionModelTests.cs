using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Domain;

public sealed class PlantProtectionModelTests
{
    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _itemId = Guid.NewGuid();
    private readonly Guid _productTypeId = Guid.NewGuid();

    [Fact]
    public void ProductType_Create_SystemProductType_DisallowsOrganizationId()
    {
        Assert.Throws<ArgumentException>(() => new ProductType(
            _organizationId,
            "FUNGICIDE",
            "Fungicide",
            isSystem: true));
    }

    [Fact]
    public void ProductType_Create_OrgProductType_RequiresOrganizationId()
    {
        Assert.Throws<ArgumentException>(() => new ProductType(
            null,
            "CUSTOM_TYPE",
            "Custom Type",
            isSystem: false));
    }

    [Fact]
    public void ProductType_Update_SystemProductType_ThrowsInvalidOperationException()
    {
        var type = new ProductType(null, "FUNGICIDE", "Fungicide", isSystem: true);
        var now = DateTimeOffset.UtcNow;

        Assert.Throws<InvalidOperationException>(() =>
            type.Update("Modified", "Desc", 1, now, _userId));
    }

    [Fact]
    public void ProductType_ActivateDeactivate_TogglesState()
    {
        var type = new ProductType(_organizationId, "BIO", "Bio", isSystem: false);
        var now = DateTimeOffset.UtcNow;

        Assert.True(type.IsActive);
        Assert.True(type.Deactivate(now, _userId));
        Assert.False(type.IsActive);
        Assert.False(type.Deactivate(now, _userId)); // idempotent
        Assert.True(type.Activate(now, _userId));
        Assert.True(type.IsActive);
    }

    [Fact]
    public void Target_Create_ValidatesTargetType()
    {
        var target = new Target(
            null,
            "POWDERY_MILDEW",
            "Powdery Mildew",
            TargetType.Disease,
            isSystem: true);

        Assert.Equal(TargetType.Disease, target.TargetType);
        Assert.True(target.IsSystem);
        Assert.Equal("POWDERY_MILDEW", target.Code);
    }

    [Fact]
    public void ApplicationMethod_Create_And_Update_Works()
    {
        var method = new ApplicationMethod(
            _organizationId,
            "CUSTOM_SPRAYER",
            "Custom Sprayer",
            isSystem: false);

        var now = DateTimeOffset.UtcNow;
        method.Update("Custom Sprayer V2", "Updated description", 5, now, _userId);

        Assert.Equal("Custom Sprayer V2", method.Name);
        Assert.Equal("Updated description", method.Description);
        Assert.Equal(5, method.DisplayOrder);
    }

    [Fact]
    public void PlantProtectionProduct_Create_ValidatesRequiredFields()
    {
        var product = new PlantProtectionProduct(
            _organizationId,
            _itemId,
            _productTypeId,
            _userId,
            activeIngredient: "Azoxystrobin 23% SC",
            manufacturer: "Syngenta",
            description: "Broad spectrum systemic fungicide");

        Assert.Equal(_organizationId, product.OrganizationId);
        Assert.Equal(_itemId, product.InventoryItemId);
        Assert.Equal(_productTypeId, product.ProductTypeId);
        Assert.Equal("Azoxystrobin 23% SC", product.ActiveIngredient);
        Assert.Equal("Syngenta", product.Manufacturer);
        Assert.True(product.IsActive);
    }

    [Fact]
    public void PlantProtectionProduct_ActivateDeactivate_TogglesState()
    {
        var product = new PlantProtectionProduct(_organizationId, _itemId, _productTypeId, _userId);
        var now = DateTimeOffset.UtcNow;

        Assert.True(product.IsActive);
        Assert.True(product.Deactivate(now, _userId));
        Assert.False(product.IsActive);
        Assert.True(product.Activate(now, _userId));
        Assert.True(product.IsActive);
    }

    [Fact]
    public void StockMovement_WithSprayId_SetsProperty()
    {
        var sprayId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.UtcNow);

        var movement = new StockMovement(
            _organizationId,
            StockMovementType.Issue,
            _itemId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            15.5m,
            Guid.NewGuid(),
            date,
            _userId,
            sprayId: sprayId);

        Assert.Equal(sprayId, movement.SprayId);
        Assert.Equal(StockMovementType.Issue, movement.MovementType);
        Assert.Equal(15.5m, movement.Quantity);
    }
}
