using FarmManagement.Domain.Entities;
using FarmManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FarmManagement.API.Tests.Domain;

public sealed class FarmModelTests
{
    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly Guid _ownershipTypeId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public void Farm_Create_WithValidData_SetsPropertiesAndDefaultsActive()
    {
        var farm = new Farm(
            organizationId: _organizationId,
            name: "Sun Valley Orchard",
            ownershipTypeId: _ownershipTypeId,
            createdBy: _userId,
            totalArea: 150.5m,
            areaUnitId: Guid.NewGuid(),
            description: "Main orchard",
            city: "Nashik",
            state: "Maharashtra");

        Assert.NotEqual(Guid.Empty, farm.Id);
        Assert.Equal(_organizationId, farm.OrganizationId);
        Assert.Equal("Sun Valley Orchard", farm.Name);
        Assert.Equal(_ownershipTypeId, farm.OwnershipTypeId);
        Assert.Equal(150.5m, farm.TotalArea);
        Assert.NotNull(farm.AreaUnitId);
        Assert.Equal("Main orchard", farm.Description);
        Assert.Equal("Nashik", farm.City);
        Assert.Equal("Maharashtra", farm.State);
        Assert.True(farm.IsActive);
        Assert.Equal(_userId, farm.CreatedBy);
        Assert.True(farm.CreatedAt <= DateTimeOffset.UtcNow);
        Assert.Null(farm.UpdatedAt);
        Assert.Null(farm.UpdatedBy);
    }

    [Fact]
    public void Farm_Create_ThrowsWhenNameIsEmpty()
    {
        Assert.Throws<ArgumentException>(() =>
            new Farm(_organizationId, "", _ownershipTypeId, _userId));

        Assert.Throws<ArgumentException>(() =>
            new Farm(_organizationId, "   ", _ownershipTypeId, _userId));
    }

    [Fact]
    public void Farm_Update_UpdatesPropertiesWithoutCode()
    {
        var farm = new Farm(_organizationId, "Initial Name", _ownershipTypeId, _userId);
        var now = DateTimeOffset.UtcNow;
        var updaterId = Guid.NewGuid();

        farm.Update(
            name: "Updated Name",
            ownershipTypeId: _ownershipTypeId,
            totalArea: null,
            areaUnitId: null,
            description: "Updated description",
            addressLine1: null,
            addressLine2: null,
            city: null,
            district: null,
            state: null,
            country: null,
            postalCode: null,
            latitude: null,
            longitude: null,
            now: now,
            updatedBy: updaterId);

        Assert.Equal("Updated Name", farm.Name);
        Assert.Equal("Updated description", farm.Description);
        Assert.Equal(now, farm.UpdatedAt);
        Assert.Equal(updaterId, farm.UpdatedBy);
    }

    [Fact]
    public void Farm_EFCoreModel_DoesNotContainCodeColumnOrUniqueCodeIndex()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=farm_test;Username=postgres;Password=postgres")
            .Options;

        using var dbContext = new ApplicationDbContext(options);
        var model = dbContext.Model;

        var farmEntity = model.FindEntityType(typeof(Farm));
        Assert.NotNull(farmEntity);
        Assert.Equal("farms", farmEntity.GetTableName());

        // Invariant: farms table must NOT contain a code column or property
        Assert.Null(farmEntity.FindProperty("Code"));
        Assert.Null(farmEntity.FindProperty("code"));

        // Verify foreign key to Organization with Restrict
        var orgEntity = model.FindEntityType(typeof(Organization));
        Assert.NotNull(orgEntity);
        var orgFk = farmEntity.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType == orgEntity);
        Assert.NotNull(orgFk);
        Assert.Equal(DeleteBehavior.Restrict, orgFk.DeleteBehavior);

        // Verify entity properties exist
        Assert.NotNull(farmEntity.FindProperty("Id"));
        Assert.NotNull(farmEntity.FindProperty("OrganizationId"));
        Assert.NotNull(farmEntity.FindProperty("Name"));
        Assert.NotNull(farmEntity.FindProperty("OwnershipTypeId"));
        Assert.NotNull(farmEntity.FindProperty("TotalArea"));
        Assert.NotNull(farmEntity.FindProperty("AreaUnitId"));
        Assert.NotNull(farmEntity.FindProperty("IsActive"));
        Assert.NotNull(farmEntity.FindProperty("CreatedAt"));
        Assert.NotNull(farmEntity.FindProperty("CreatedBy"));
        Assert.NotNull(farmEntity.FindProperty("UpdatedAt"));
        Assert.NotNull(farmEntity.FindProperty("UpdatedBy"));

        // Verify old ux_farm_organization_code index does NOT exist
        var indexes = farmEntity.GetIndexes().ToList();
        Assert.Null(indexes.FirstOrDefault(ix => ix.GetDatabaseName() == "ux_farm_organization_code"));

        // Verify ix_farms_organization_id index exists
        Assert.NotNull(indexes.FirstOrDefault(ix => ix.GetDatabaseName() == "ix_farms_organization_id"));
    }
}
