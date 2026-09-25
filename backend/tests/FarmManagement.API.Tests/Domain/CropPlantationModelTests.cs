using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using FarmManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FarmManagement.API.Tests.Domain;

public sealed class CropPlantationModelTests
{
    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly Guid _farmId = Guid.NewGuid();
    private readonly Guid _farmAreaId = Guid.NewGuid();
    private readonly Guid _cropId = Guid.NewGuid();
    private readonly Guid _varietyId = Guid.NewGuid();
    private readonly Guid _lifecycleTemplateId = Guid.NewGuid();
    private readonly Guid _areaUnitId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public void CropPlantation_Create_WithValidData_SetsPropertiesAndDefaultsActive()
    {
        var plantingDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var expectedEndDate = plantingDate.AddMonths(6);

        var plantation = new CropPlantation(
            organizationId: _organizationId,
            farmId: _farmId,
            farmAreaId: _farmAreaId,
            cropId: _cropId,
            varietyId: _varietyId,
            lifecycleTemplateId: _lifecycleTemplateId,
            plantationName: "Block A Wheat",
            allocatedArea: 12.5m,
            areaUnitId: _areaUnitId,
            plantingDate: plantingDate,
            expectedEndDate: expectedEndDate,
            createdBy: _userId);

        Assert.NotEqual(Guid.Empty, plantation.Id);
        Assert.Equal(_organizationId, plantation.OrganizationId);
        Assert.Equal(_farmId, plantation.FarmId);
        Assert.Equal(_farmAreaId, plantation.FarmAreaId);
        Assert.Equal(_cropId, plantation.CropId);
        Assert.Equal(_varietyId, plantation.VarietyId);
        Assert.Equal(_lifecycleTemplateId, plantation.LifecycleTemplateId);
        Assert.Equal("Block A Wheat", plantation.PlantationName);
        Assert.Equal(12.5m, plantation.AllocatedArea);
        Assert.Equal(_areaUnitId, plantation.AreaUnitId);
        Assert.Equal(plantingDate, plantation.PlantingDate);
        Assert.Equal(expectedEndDate, plantation.ExpectedEndDate);
        Assert.Equal(PlantationStatus.Planned, plantation.Status);
        Assert.True(plantation.IsActive);
        Assert.Equal(_userId, plantation.CreatedBy);
        Assert.True(plantation.CreatedAt <= DateTimeOffset.UtcNow);
        Assert.Null(plantation.UpdatedAt);
        Assert.Null(plantation.UpdatedBy);

        // Verify PlantationCode property does not exist on CropPlantation via reflection
        var codeProp = typeof(CropPlantation).GetProperty("PlantationCode");
        Assert.Null(codeProp);
    }

    [Fact]
    public void CropPlantation_Create_ThrowsWhenNameIsEmpty()
    {
        var plantingDate = DateOnly.FromDateTime(DateTime.UtcNow);

        Assert.Throws<ArgumentException>(() =>
            new CropPlantation(_organizationId, _farmId, _farmAreaId, _cropId, null, null, "", 10m, _areaUnitId, plantingDate, null, _userId));

        Assert.Throws<ArgumentException>(() =>
            new CropPlantation(_organizationId, _farmId, _farmAreaId, _cropId, null, null, "   ", 10m, _areaUnitId, plantingDate, null, _userId));
    }

    [Fact]
    public void CropPlantation_Update_UpdatesPropertiesWithoutCode()
    {
        var plantingDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var plantation = new CropPlantation(_organizationId, _farmId, _farmAreaId, _cropId, null, null, "Initial Plantation", 10m, _areaUnitId, plantingDate, null, _userId);
        var now = DateTimeOffset.UtcNow;
        var updaterId = Guid.NewGuid();

        plantation.Update(
            farmAreaId: _farmAreaId,
            cropId: _cropId,
            varietyId: _varietyId,
            lifecycleTemplateId: _lifecycleTemplateId,
            plantationName: "Updated Plantation",
            allocatedArea: 15m,
            areaUnitId: _areaUnitId,
            plantingDate: plantingDate,
            expectedEndDate: plantingDate.AddMonths(4),
            now: now,
            updatedBy: updaterId);

        Assert.Equal("Updated Plantation", plantation.PlantationName);
        Assert.Equal(15m, plantation.AllocatedArea);
        Assert.Equal(now, plantation.UpdatedAt);
        Assert.Equal(updaterId, plantation.UpdatedBy);
    }

    [Fact]
    public void CropPlantation_EFCoreModel_DoesNotContainCodeColumnOrUniqueCodeIndex()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=farm_test;Username=postgres;Password=postgres")
            .Options;

        using var dbContext = new ApplicationDbContext(options);
        var model = dbContext.Model;

        var entity = model.FindEntityType(typeof(CropPlantation));
        Assert.NotNull(entity);
        Assert.Equal("crop_plantations", entity.GetTableName());

        // Invariant: crop_plantations table must NOT contain PlantationCode or plantation_code property
        Assert.Null(entity.FindProperty("PlantationCode"));
        Assert.Null(entity.FindProperty("plantation_code"));

        // Verify entity properties exist
        Assert.NotNull(entity.FindProperty("Id"));
        Assert.NotNull(entity.FindProperty("OrganizationId"));
        Assert.NotNull(entity.FindProperty("FarmId"));
        Assert.NotNull(entity.FindProperty("FarmAreaId"));
        Assert.NotNull(entity.FindProperty("CropId"));
        Assert.NotNull(entity.FindProperty("PlantationName"));
        Assert.NotNull(entity.FindProperty("AllocatedArea"));
        Assert.NotNull(entity.FindProperty("AreaUnitId"));
        Assert.NotNull(entity.FindProperty("PlantingDate"));
        Assert.NotNull(entity.FindProperty("Status"));
        Assert.NotNull(entity.FindProperty("IsActive"));
        Assert.NotNull(entity.FindProperty("CreatedAt"));
        Assert.NotNull(entity.FindProperty("CreatedBy"));

        // Verify old ux_plantation_organization_code index does NOT exist
        var indexes = entity.GetIndexes().ToList();
        Assert.Null(indexes.FirstOrDefault(ix => ix.GetDatabaseName() == "ux_plantation_organization_code"));

        // Verify ix_crop_plantations_organization_id index exists
        Assert.NotNull(indexes.FirstOrDefault(ix => ix.GetDatabaseName() == "ix_crop_plantations_organization_id"));
    }
}
