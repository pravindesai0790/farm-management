using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using FarmManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FarmManagement.API.Tests.Domain;

public sealed class CropCycleModelTests
{
    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly Guid _plantationId = Guid.NewGuid();
    private readonly Guid _lifecycleTemplateId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public void CropCycle_Create_WithValidData_SetsPropertiesAndOmitsCycleCode()
    {
        var plannedStart = DateOnly.FromDateTime(DateTime.UtcNow);
        var expectedEnd = plannedStart.AddMonths(6);

        var cycle = new CropCycle(
            organizationId: _organizationId,
            plantationId: _plantationId,
            cycleName: "2026 Wheat Production",
            seasonYear: 2026,
            seasonName: "Kharif",
            plannedStartDate: plannedStart,
            expectedEndDate: expectedEnd,
            createdBy: _userId,
            lifecycleTemplateId: _lifecycleTemplateId);

        Assert.NotEqual(Guid.Empty, cycle.Id);
        Assert.Equal(_organizationId, cycle.OrganizationId);
        Assert.Equal(_plantationId, cycle.PlantationId);
        Assert.Equal("2026 Wheat Production", cycle.CycleName);
        Assert.Equal(2026, cycle.SeasonYear);
        Assert.Equal("Kharif", cycle.SeasonName);
        Assert.Equal(plannedStart, cycle.PlannedStartDate);
        Assert.Equal(expectedEnd, cycle.ExpectedEndDate);
        Assert.Equal(CropCycleStatus.Planned, cycle.Status);
        Assert.Equal(_lifecycleTemplateId, cycle.LifecycleTemplateId);
        Assert.Equal(_userId, cycle.CreatedBy);
        Assert.True(cycle.CreatedAt <= DateTimeOffset.UtcNow);
        Assert.Null(cycle.UpdatedAt);
        Assert.Null(cycle.UpdatedBy);

        // Verify CycleCode property does not exist on CropCycle via reflection
        var codeProp = typeof(CropCycle).GetProperty("CycleCode");
        Assert.Null(codeProp);
    }

    [Fact]
    public void CropCycle_Create_ThrowsWhenNameIsEmpty()
    {
        var plannedStart = DateOnly.FromDateTime(DateTime.UtcNow);

        Assert.Throws<ArgumentException>(() =>
            new CropCycle(_organizationId, _plantationId, "", 2026, null, plannedStart, null, _userId));

        Assert.Throws<ArgumentException>(() =>
            new CropCycle(_organizationId, _plantationId, "   ", 2026, null, plannedStart, null, _userId));
    }

    [Fact]
    public void CropCycle_Update_UpdatesPropertiesWithoutCycleCode()
    {
        var plannedStart = DateOnly.FromDateTime(DateTime.UtcNow);
        var cycle = new CropCycle(_organizationId, _plantationId, "Initial Cycle", 2026, null, plannedStart, null, _userId);

        var now = DateTimeOffset.UtcNow;
        var updaterId = Guid.NewGuid();

        cycle.Update(
            plantationId: _plantationId,
            cycleName: "Updated Cycle",
            seasonYear: 2026,
            seasonName: "Spring",
            plannedStartDate: plannedStart,
            expectedEndDate: plannedStart.AddMonths(4),
            now: now,
            updatedBy: updaterId);

        Assert.Equal("Updated Cycle", cycle.CycleName);
        Assert.Equal("Spring", cycle.SeasonName);
        Assert.Equal(now, cycle.UpdatedAt);
        Assert.Equal(updaterId, cycle.UpdatedBy);
    }

    [Fact]
    public void CropCycle_EFCoreModel_DoesNotContainCycleCodeColumnOrUniqueCodeIndex()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=farm_test;Username=postgres;Password=postgres")
            .Options;

        using var dbContext = new ApplicationDbContext(options);
        var model = dbContext.Model;

        var entity = model.FindEntityType(typeof(CropCycle));
        Assert.NotNull(entity);
        Assert.Equal("crop_cycles", entity.GetTableName());

        // Invariant: crop_cycles table must NOT contain CycleCode or cycle_code property
        Assert.Null(entity.FindProperty("CycleCode"));
        Assert.Null(entity.FindProperty("cycle_code"));

        // Verify entity properties exist
        Assert.NotNull(entity.FindProperty("Id"));
        Assert.NotNull(entity.FindProperty("OrganizationId"));
        Assert.NotNull(entity.FindProperty("PlantationId"));
        Assert.NotNull(entity.FindProperty("CycleName"));
        Assert.NotNull(entity.FindProperty("SeasonYear"));
        Assert.NotNull(entity.FindProperty("PlannedStartDate"));
        Assert.NotNull(entity.FindProperty("Status"));
        Assert.NotNull(entity.FindProperty("CreatedAt"));
        Assert.NotNull(entity.FindProperty("CreatedBy"));

        // Verify old ux_crop_cycle_organization_code index does NOT exist
        var indexes = entity.GetIndexes().ToList();
        Assert.Null(indexes.FirstOrDefault(ix => ix.GetDatabaseName() == "ux_crop_cycle_organization_code"));

        // Verify ix_crop_cycles_organization_id index exists
        Assert.NotNull(indexes.FirstOrDefault(ix => ix.GetDatabaseName() == "ix_crop_cycles_organization_id"));
    }
}
