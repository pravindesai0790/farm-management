using FarmManagement.Domain.Entities;
using FarmManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FarmManagement.API.Tests.Domain;

public sealed class CropModelTests
{
    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public void Crop_Create_WithValidData_SetsPropertiesAndOmitsCode()
    {
        var crop = new Crop(
            organizationId: _organizationId,
            name: "Wheat",
            cropType: "GRAIN",
            cropDurationType: "ANNUAL",
            isSystem: false,
            scientificName: "Triticum aestivum",
            description: "High yield winter wheat",
            createdBy: _userId);

        Assert.NotEqual(Guid.Empty, crop.Id);
        Assert.Equal(_organizationId, crop.OrganizationId);
        Assert.Equal("Wheat", crop.Name);
        Assert.Equal("Triticum aestivum", crop.ScientificName);
        Assert.Equal("GRAIN", crop.CropType);
        Assert.Equal("ANNUAL", crop.CropDurationType);
        Assert.Equal("High yield winter wheat", crop.Description);
        Assert.False(crop.IsSystem);
        Assert.True(crop.IsActive);
        Assert.Equal(_userId, crop.CreatedBy);
        Assert.True(crop.CreatedAt <= DateTimeOffset.UtcNow);
        Assert.Null(crop.UpdatedAt);
        Assert.Null(crop.UpdatedBy);

        // Verify Code property does not exist on Crop via reflection
        var codeProp = typeof(Crop).GetProperty("Code");
        Assert.Null(codeProp);
    }

    [Fact]
    public void Crop_Create_ThrowsWhenNameIsEmpty()
    {
        Assert.Throws<ArgumentException>(() =>
            new Crop(_organizationId, "", "GRAIN", "ANNUAL", createdBy: _userId));

        Assert.Throws<ArgumentException>(() =>
            new Crop(_organizationId, "   ", "GRAIN", "ANNUAL", createdBy: _userId));
    }

    [Fact]
    public void Crop_Update_UpdatesPropertiesWithoutCode()
    {
        var crop = new Crop(_organizationId, "Initial Crop", "FRUIT", "PERENNIAL", createdBy: _userId);

        var now = DateTimeOffset.UtcNow;
        var updaterId = Guid.NewGuid();

        crop.Update(
            name: "Updated Crop",
            cropType: "FRUIT_TREE",
            cropDurationType: "PERENNIAL",
            scientificName: "Updated scientific",
            description: "Updated description",
            now: now,
            updatedBy: updaterId);

        Assert.Equal("Updated Crop", crop.Name);
        Assert.Equal("FRUIT_TREE", crop.CropType);
        Assert.Equal("PERENNIAL", crop.CropDurationType);
        Assert.Equal("Updated scientific", crop.ScientificName);
        Assert.Equal("Updated description", crop.Description);
        Assert.Equal(now, crop.UpdatedAt);
        Assert.Equal(updaterId, crop.UpdatedBy);
    }

    [Fact]
    public void Crop_EFCoreModel_DoesNotContainCodeColumnOrUniqueCodeIndexes()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=farm_test;Username=postgres;Password=postgres")
            .Options;

        using var dbContext = new ApplicationDbContext(options);
        var model = dbContext.Model;

        var cropEntity = model.FindEntityType(typeof(Crop));
        Assert.NotNull(cropEntity);
        Assert.Equal("crops", cropEntity.GetTableName());

        // Invariant: crops table must NOT contain a code column or property
        Assert.Null(cropEntity.FindProperty("Code"));
        Assert.Null(cropEntity.FindProperty("code"));

        // Verify entity properties exist
        Assert.NotNull(cropEntity.FindProperty("Id"));
        Assert.NotNull(cropEntity.FindProperty("OrganizationId"));
        Assert.NotNull(cropEntity.FindProperty("Name"));
        Assert.NotNull(cropEntity.FindProperty("ScientificName"));
        Assert.NotNull(cropEntity.FindProperty("CropType"));
        Assert.NotNull(cropEntity.FindProperty("CropDurationType"));
        Assert.NotNull(cropEntity.FindProperty("Description"));
        Assert.NotNull(cropEntity.FindProperty("IsSystem"));
        Assert.NotNull(cropEntity.FindProperty("IsActive"));
        Assert.NotNull(cropEntity.FindProperty("CreatedAt"));
        Assert.NotNull(cropEntity.FindProperty("CreatedBy"));
        Assert.NotNull(cropEntity.FindProperty("UpdatedAt"));
        Assert.NotNull(cropEntity.FindProperty("UpdatedBy"));

        // Verify old unique code indexes do NOT exist
        var indexes = cropEntity.GetIndexes().ToList();
        Assert.Null(indexes.FirstOrDefault(ix => ix.GetDatabaseName() == "ux_crops_organization_code"));
        Assert.Null(indexes.FirstOrDefault(ix => ix.GetDatabaseName() == "ux_crops_system_code"));

        // Verify ix_crops_organization_id index exists
        Assert.NotNull(indexes.FirstOrDefault(ix => ix.GetDatabaseName() == "ix_crops_organization_id"));
    }
}
