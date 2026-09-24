using FarmManagement.Domain.Entities;
using FarmManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FarmManagement.API.Tests.Domain;

public sealed class FarmAreaModelTests
{
    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly Guid _farmId = Guid.NewGuid();
    private readonly Guid _areaUnitId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public void FarmArea_Create_WithValidData_SetsPropertiesAndDefaultsActive()
    {
        var area = new FarmArea(
            organizationId: _organizationId,
            farmId: _farmId,
            parentFarmAreaId: null,
            name: "North Field",
            totalArea: 25.5m,
            areaUnitId: _areaUnitId,
            createdBy: _userId,
            description: "Main north section");

        Assert.NotEqual(Guid.Empty, area.Id);
        Assert.Equal(_organizationId, area.OrganizationId);
        Assert.Equal(_farmId, area.FarmId);
        Assert.Null(area.ParentFarmAreaId);
        Assert.Equal("North Field", area.Name);
        Assert.Equal(25.5m, area.TotalArea);
        Assert.Equal(_areaUnitId, area.AreaUnitId);
        Assert.Equal("Main north section", area.Description);
        Assert.True(area.IsActive);
        Assert.Equal(_userId, area.CreatedBy);
        Assert.True(area.CreatedAt <= DateTimeOffset.UtcNow);
        Assert.Null(area.UpdatedAt);
        Assert.Null(area.UpdatedBy);

        // Verify Code property does not exist on FarmArea via reflection
        var codeProp = typeof(FarmArea).GetProperty("Code");
        Assert.Null(codeProp);
    }

    [Fact]
    public void FarmArea_Create_ThrowsWhenNameIsEmpty()
    {
        Assert.Throws<ArgumentException>(() =>
            new FarmArea(_organizationId, _farmId, null, "", 10m, _areaUnitId, _userId));

        Assert.Throws<ArgumentException>(() =>
            new FarmArea(_organizationId, _farmId, null, "   ", 10m, _areaUnitId, _userId));
    }

    [Fact]
    public void FarmArea_Update_UpdatesPropertiesWithoutCode()
    {
        var area = new FarmArea(_organizationId, _farmId, null, "Initial Name", 10m, _areaUnitId, _userId);
        var now = DateTimeOffset.UtcNow;
        var updaterId = Guid.NewGuid();

        area.Update(
            parentFarmAreaId: null,
            name: "Updated Name",
            totalArea: 15m,
            areaUnitId: _areaUnitId,
            description: "Updated description",
            now: now,
            updatedBy: updaterId);

        Assert.Equal("Updated Name", area.Name);
        Assert.Equal(15m, area.TotalArea);
        Assert.Equal("Updated description", area.Description);
        Assert.Equal(now, area.UpdatedAt);
        Assert.Equal(updaterId, area.UpdatedBy);
    }

    [Fact]
    public void FarmArea_EFCoreModel_DoesNotContainCodeColumnOrUniqueCodeIndex()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=farm_test;Username=postgres;Password=postgres")
            .Options;

        using var dbContext = new ApplicationDbContext(options);
        var model = dbContext.Model;

        var farmAreaEntity = model.FindEntityType(typeof(FarmArea));
        Assert.NotNull(farmAreaEntity);
        Assert.Equal("farm_areas", farmAreaEntity.GetTableName());

        // Invariant: farm_areas table must NOT contain a code column or property
        Assert.Null(farmAreaEntity.FindProperty("Code"));
        Assert.Null(farmAreaEntity.FindProperty("code"));

        // Verify entity properties exist
        Assert.NotNull(farmAreaEntity.FindProperty("Id"));
        Assert.NotNull(farmAreaEntity.FindProperty("OrganizationId"));
        Assert.NotNull(farmAreaEntity.FindProperty("FarmId"));
        Assert.NotNull(farmAreaEntity.FindProperty("ParentFarmAreaId"));
        Assert.NotNull(farmAreaEntity.FindProperty("Name"));
        Assert.NotNull(farmAreaEntity.FindProperty("Description"));
        Assert.NotNull(farmAreaEntity.FindProperty("TotalArea"));
        Assert.NotNull(farmAreaEntity.FindProperty("AreaUnitId"));
        Assert.NotNull(farmAreaEntity.FindProperty("IsActive"));
        Assert.NotNull(farmAreaEntity.FindProperty("CreatedAt"));
        Assert.NotNull(farmAreaEntity.FindProperty("CreatedBy"));
        Assert.NotNull(farmAreaEntity.FindProperty("UpdatedAt"));
        Assert.NotNull(farmAreaEntity.FindProperty("UpdatedBy"));

        // Verify old ux_farm_area_farm_code index does NOT exist
        var indexes = farmAreaEntity.GetIndexes().ToList();
        Assert.Null(indexes.FirstOrDefault(ix => ix.GetDatabaseName() == "ux_farm_area_farm_code"));

        // Verify ix_farm_areas_farm_id index exists
        Assert.NotNull(indexes.FirstOrDefault(ix => ix.GetDatabaseName() == "ix_farm_areas_farm_id"));
    }
}
