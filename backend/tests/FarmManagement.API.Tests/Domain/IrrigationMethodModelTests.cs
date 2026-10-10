using System;
using FarmManagement.Domain.Entities;
using Xunit;

namespace FarmManagement.API.Tests.Domain;

public class IrrigationMethodModelTests
{
    [Fact]
    public void Create_ValidParameters_InstantiatesActiveSystemMethod()
    {
        var method = new IrrigationMethod(
            organizationId: null,
            code: "DRIP",
            name: "Drip Irrigation",
            isSystem: true,
            description: "Direct emitter watering",
            displayOrder: 10);

        Assert.Null(method.OrganizationId);
        Assert.Equal("DRIP", method.Code);
        Assert.Equal("Drip Irrigation", method.Name);
        Assert.True(method.IsSystem);
        Assert.True(method.IsActive);
        Assert.Equal(10, method.DisplayOrder);
    }

    [Fact]
    public void Deactivate_WhenActive_SetsIsActiveFalse()
    {
        var orgId = Guid.NewGuid();
        var method = new IrrigationMethod(
            organizationId: orgId,
            code: "SPRINKLER_CUSTOM",
            name: "Sprinkler Custom",
            isSystem: false,
            description: null,
            displayOrder: 20);

        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        method.Deactivate(now, userId);

        Assert.False(method.IsActive);
        Assert.Equal(userId, method.UpdatedBy);
        Assert.Equal(now, method.UpdatedAt);
    }

    [Fact]
    public void Activate_WhenInactive_SetsIsActiveTrue()
    {
        var orgId = Guid.NewGuid();
        var method = new IrrigationMethod(
            organizationId: orgId,
            code: "MANUAL_CUSTOM",
            name: "Manual Custom",
            isSystem: false,
            description: null,
            displayOrder: 30);

        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        method.Deactivate(now, userId);
        Assert.False(method.IsActive);

        method.Activate(now.AddMinutes(5), userId);
        Assert.True(method.IsActive);
    }

    [Fact]
    public void Update_WhenSystemMethod_ThrowsInvalidOperationException()
    {
        var method = new IrrigationMethod(
            organizationId: null,
            code: "DRIP",
            name: "Drip Irrigation",
            isSystem: true,
            description: null,
            displayOrder: 10);

        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var ex = Assert.Throws<InvalidOperationException>(() =>
            method.Update("Custom Name", "Custom Desc", 1, now, userId));

        Assert.Contains("System irrigation methods cannot be modified", ex.Message);
    }

    [Fact]
    public void Update_WhenTenantMethod_UpdatesSuccessfully()
    {
        var orgId = Guid.NewGuid();
        var method = new IrrigationMethod(
            organizationId: orgId,
            code: "MICRO_JET",
            name: "Micro Jet",
            isSystem: false,
            description: "Original",
            displayOrder: 5);

        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        method.Update("Micro Jet Updated", "Updated desc", 8, now, userId);

        Assert.Equal("Micro Jet Updated", method.Name);
        Assert.Equal("Updated desc", method.Description);
        Assert.Equal(8, method.DisplayOrder);
        Assert.Equal(userId, method.UpdatedBy);
        Assert.Equal(now, method.UpdatedAt);
    }
}
