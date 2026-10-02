using FarmManagement.Domain.Entities;
using Xunit;

namespace FarmManagement.API.Tests.Domain;

public class SupplierModelTests
{
    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_SetsPropertiesAndIsActive()
    {
        var supplier = Supplier.Create(
            OrgId,
            "Agri-Supplies Ltd",
            UserId,
            contactPerson: "Jane Doe",
            phone: "+123456789",
            email: "contact@agrisupplies.com",
            address: "123 Farm Lane",
            registrationIdentifier: "REG-999888",
            notes: "Preferred seed vendor");

        Assert.NotEqual(Guid.Empty, supplier.Id);
        Assert.Equal(OrgId, supplier.OrganizationId);
        Assert.Equal("Agri-Supplies Ltd", supplier.Name);
        Assert.Equal("Jane Doe", supplier.ContactPerson);
        Assert.Equal("+123456789", supplier.Phone);
        Assert.Equal("contact@agrisupplies.com", supplier.Email);
        Assert.Equal("123 Farm Lane", supplier.Address);
        Assert.Equal("REG-999888", supplier.RegistrationIdentifier);
        Assert.Equal("Preferred seed vendor", supplier.Notes);
        Assert.True(supplier.IsActive);
        Assert.Equal(UserId, supplier.CreatedBy);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithInvalidName_ThrowsArgumentException(string? invalidName)
    {
        Assert.Throws<ArgumentException>(() =>
            Supplier.Create(OrgId, invalidName!, UserId));
    }

    [Fact]
    public void Create_WithEmptyOrganizationId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            Supplier.Create(Guid.Empty, "Vendor Name", UserId));
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            Supplier.Create(OrgId, "Vendor Name", Guid.Empty));
    }

    [Fact]
    public void Update_WithValidData_UpdatesPropertiesAndAudit()
    {
        var supplier = Supplier.Create(OrgId, "Old Name", UserId);
        var updaterId = Guid.NewGuid();
        var updateTime = DateTimeOffset.UtcNow;

        supplier.Update(
            "New Name",
            contactPerson: "New Contact",
            phone: "987654321",
            email: "new@vendor.com",
            address: "New Address",
            registrationIdentifier: "REG-111",
            notes: "Updated notes",
            updatedBy: updaterId,
            now: updateTime);

        Assert.Equal("New Name", supplier.Name);
        Assert.Equal("New Contact", supplier.ContactPerson);
        Assert.Equal(updaterId, supplier.UpdatedBy);
        Assert.Equal(updateTime, supplier.UpdatedAt);
    }

    [Fact]
    public void Deactivate_WhenActive_SetsIsActiveFalse()
    {
        var supplier = Supplier.Create(OrgId, "Vendor", UserId);
        var result = supplier.Deactivate(UserId);

        Assert.True(result);
        Assert.False(supplier.IsActive);
        Assert.Equal(UserId, supplier.UpdatedBy);
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_ReturnsFalse()
    {
        var supplier = Supplier.Create(OrgId, "Vendor", UserId);
        supplier.Deactivate(UserId);

        var secondResult = supplier.Deactivate(UserId);
        Assert.False(secondResult);
    }

    [Fact]
    public void Activate_WhenInactive_SetsIsActiveTrue()
    {
        var supplier = Supplier.Create(OrgId, "Vendor", UserId);
        supplier.Deactivate(UserId);

        var result = supplier.Activate(UserId);
        Assert.True(result);
        Assert.True(supplier.IsActive);
    }
}
