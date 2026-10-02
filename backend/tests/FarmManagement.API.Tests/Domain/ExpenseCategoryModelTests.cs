using FarmManagement.Domain.Entities;
using Xunit;

namespace FarmManagement.API.Tests.Domain;

public class ExpenseCategoryModelTests
{
    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void CreateSystemDefault_WithValidData_CreatesSystemCategory()
    {
        var category = new ExpenseCategory(
            organizationId: null,
            name: "Fertilizer",
            code: "FERT",
            isSystemDefault: true,
            description: "Soil enrichment inputs");

        Assert.NotEqual(Guid.Empty, category.Id);
        Assert.Null(category.OrganizationId);
        Assert.Equal("Fertilizer", category.Name);
        Assert.Equal("FERT", category.Code);
        Assert.True(category.IsSystemDefault);
        Assert.True(category.IsActive);
        Assert.Equal("Soil enrichment inputs", category.Description);
    }

    [Fact]
    public void CreateSystemDefault_WithOrganizationId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new ExpenseCategory(
                organizationId: OrgId,
                name: "Fertilizer",
                code: "FERT",
                isSystemDefault: true));
    }

    [Fact]
    public void CreateCustom_WithoutOrganizationId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new ExpenseCategory(
                organizationId: null,
                name: "Custom Category",
                code: "CUST",
                isSystemDefault: false,
                createdBy: UserId));
    }

    [Fact]
    public void CreateCustom_WithoutCreatedBy_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new ExpenseCategory(
                organizationId: OrgId,
                name: "Custom Category",
                code: "CUST",
                isSystemDefault: false,
                createdBy: null));
    }

    [Fact]
    public void Update_SystemDefault_ThrowsInvalidOperationException()
    {
        var category = new ExpenseCategory(
            organizationId: null,
            name: "Fertilizer",
            code: "FERT",
            isSystemDefault: true);

        Assert.Throws<InvalidOperationException>(() =>
            category.Update("Renamed", "New Description", UserId));
    }

    [Fact]
    public void Update_CustomCategory_UpdatesProperties()
    {
        var category = new ExpenseCategory(
            organizationId: OrgId,
            name: "Initial Name",
            code: "INIT",
            isSystemDefault: false,
            description: "Initial description",
            createdBy: UserId);

        var updateTime = DateTimeOffset.UtcNow;
        var updaterId = Guid.NewGuid();

        category.Update("Updated Name", "Updated description", updaterId, updateTime);

        Assert.Equal("Updated Name", category.Name);
        Assert.Equal("Updated description", category.Description);
        Assert.Equal(updaterId, category.UpdatedBy);
        Assert.Equal(updateTime, category.UpdatedAt);
    }

    [Fact]
    public void Deactivate_And_Activate_TogglesIsActive()
    {
        var category = new ExpenseCategory(
            organizationId: OrgId,
            name: "Tools",
            isSystemDefault: false,
            createdBy: UserId);

        Assert.True(category.IsActive);

        var deactivated = category.Deactivate(UserId);
        Assert.True(deactivated);
        Assert.False(category.IsActive);

        var reactivated = category.Activate(UserId);
        Assert.True(reactivated);
        Assert.True(category.IsActive);
    }
}
