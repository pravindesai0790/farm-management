using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using FarmManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FarmManagement.API.Tests.Domain;

public sealed class CropLifecycleModelTests
{
    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly Guid _plantationId = Guid.NewGuid();
    private readonly Guid _cropId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public void CropLifecycleStage_Create_WithValidData_SetsPropertiesAndOmitsStageCode()
    {
        var templateId = Guid.NewGuid();
        var stage = new CropLifecycleStage(
            lifecycleTemplateId: templateId,
            stageName: "Bud Break",
            sequenceNumber: 3,
            expectedDurationDays: 14,
            description: "Initial vine bud development");

        Assert.NotEqual(Guid.Empty, stage.Id);
        Assert.Equal(templateId, stage.LifecycleTemplateId);
        Assert.Equal("Bud Break", stage.StageName);
        Assert.Equal(3, stage.SequenceNumber);
        Assert.Equal(14, stage.ExpectedDurationDays);
        Assert.Equal("Initial vine bud development", stage.Description);
        Assert.True(stage.IsActive);

        // Verify stage_code property does not exist via reflection
        var stageCodeProp = typeof(CropLifecycleStage).GetProperty("StageCode");
        Assert.Null(stageCodeProp);
    }

    [Fact]
    public void CropLifecycleStage_Create_WithNullDuration_Succeeds()
    {
        var templateId = Guid.NewGuid();
        var stage = new CropLifecycleStage(templateId, "Dormancy", 1, null);

        Assert.Null(stage.ExpectedDurationDays);
    }

    [Fact]
    public void CropLifecycleStage_Create_InvalidDuration_ThrowsArgumentOutOfRangeException()
    {
        var templateId = Guid.NewGuid();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CropLifecycleStage(templateId, "Dormancy", 1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CropLifecycleStage(templateId, "Dormancy", 1, -5));
    }

    [Fact]
    public void CropLifecycleStage_Update_UpdatesPropertiesCorrectly()
    {
        var templateId = Guid.NewGuid();
        var stage = new CropLifecycleStage(templateId, "Flowering", 4, 10, "Flowering");

        stage.Update("Flowering & Fruit Set", 5, 15, "Updated description");

        Assert.Equal("Flowering & Fruit Set", stage.StageName);
        Assert.Equal(5, stage.SequenceNumber);
        Assert.Equal(15, stage.ExpectedDurationDays);
        Assert.Equal("Updated description", stage.Description);
    }

    [Fact]
    public void CropCycle_Create_WithLifecycleTemplate_SetsLifecycleTemplateId()
    {
        var templateId = Guid.NewGuid();
        var cycle = new CropCycle(
            organizationId: _organizationId,
            plantationId: _plantationId,
            cycleCode: "CC-2026-01",
            cycleName: "2026 Table Grapes",
            seasonYear: 2026,
            seasonName: "Spring 2026",
            plannedStartDate: new DateOnly(2026, 4, 1),
            expectedEndDate: new DateOnly(2026, 10, 1),
            createdBy: _userId,
            lifecycleTemplateId: templateId);

        Assert.Equal(templateId, cycle.LifecycleTemplateId);
        Assert.NotNull(cycle.Stages);
        Assert.Empty(cycle.Stages);
    }

    [Fact]
    public void CropCycle_Create_WithoutLifecycleTemplate_LifecycleTemplateIdIsNull()
    {
        var cycle = new CropCycle(
            organizationId: _organizationId,
            plantationId: _plantationId,
            cycleCode: "CC-2026-02",
            cycleName: "2026 Draft Cycle",
            seasonYear: 2026,
            seasonName: "Kharif",
            plannedStartDate: new DateOnly(2026, 6, 1),
            expectedEndDate: null,
            createdBy: _userId);

        Assert.Null(cycle.LifecycleTemplateId);
    }

    [Fact]
    public void CropCycle_SetLifecycleTemplate_WhilePlanned_UpdatesTemplateId()
    {
        var cycle = new CropCycle(
            organizationId: _organizationId,
            plantationId: _plantationId,
            cycleCode: "CC-2026-03",
            cycleName: "Planned Cycle",
            seasonYear: 2026,
            seasonName: null,
            plannedStartDate: new DateOnly(2026, 5, 1),
            expectedEndDate: null,
            createdBy: _userId);

        var newTemplateId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        cycle.SetLifecycleTemplate(newTemplateId, now, _userId);

        Assert.Equal(newTemplateId, cycle.LifecycleTemplateId);
        Assert.Equal(now, cycle.UpdatedAt);
        Assert.Equal(_userId, cycle.UpdatedBy);
    }

    [Fact]
    public void CropCycle_SetLifecycleTemplate_WhenActive_ThrowsInvalidOperationException()
    {
        var cycle = new CropCycle(
            organizationId: _organizationId,
            plantationId: _plantationId,
            cycleCode: "CC-2026-04",
            cycleName: "Started Cycle",
            seasonYear: 2026,
            seasonName: null,
            plannedStartDate: new DateOnly(2026, 5, 1),
            expectedEndDate: null,
            createdBy: _userId);

        cycle.Start(new DateOnly(2026, 5, 1), DateTimeOffset.UtcNow, _userId);

        Assert.Throws<InvalidOperationException>(() =>
            cycle.SetLifecycleTemplate(Guid.NewGuid(), DateTimeOffset.UtcNow, _userId));
    }

    [Fact]
    public void CropCycleStage_Create_SetsPropertiesCorrectly()
    {
        var cycleId = Guid.NewGuid();
        var templateStageId = Guid.NewGuid();
        var stage = new CropCycleStage(
            cropCycleId: cycleId,
            lifecycleTemplateStageId: templateStageId,
            stageName: "Pruning",
            sequenceNumber: 2,
            expectedDurationDays: 15,
            plannedStartDate: new DateOnly(2026, 3, 1),
            plannedEndDate: new DateOnly(2026, 3, 16),
            createdBy: _userId,
            status: CropCycleStageStatus.NotStarted,
            notes: "Initial stage notes");

        Assert.NotEqual(Guid.Empty, stage.Id);
        Assert.Equal(cycleId, stage.CropCycleId);
        Assert.Equal(templateStageId, stage.LifecycleTemplateStageId);
        Assert.Equal("Pruning", stage.StageName);
        Assert.Equal(2, stage.SequenceNumber);
        Assert.Equal(15, stage.ExpectedDurationDays);
        Assert.Equal(new DateOnly(2026, 3, 1), stage.PlannedStartDate);
        Assert.Equal(new DateOnly(2026, 3, 16), stage.PlannedEndDate);
        Assert.Null(stage.ActualStartDate);
        Assert.Null(stage.ActualEndDate);
        Assert.Equal(CropCycleStageStatus.NotStarted, stage.Status);
        Assert.Equal("Initial stage notes", stage.Notes);
        Assert.Equal(_userId, stage.CreatedBy);

        // Verify stage_code does not exist on CropCycleStage
        var stageCodeProp = typeof(CropCycleStage).GetProperty("StageCode");
        Assert.Null(stageCodeProp);
    }

    [Fact]
    public void CropCycleStage_Start_TransitionsToInProgress()
    {
        var stage = new CropCycleStage(
            Guid.NewGuid(), Guid.NewGuid(), "Berry Development", 7, 30,
            new DateOnly(2026, 6, 1), new DateOnly(2026, 7, 1), _userId);

        var startDate = new DateOnly(2026, 6, 2);
        var now = DateTimeOffset.UtcNow;
        var result = stage.Start(startDate, now, _userId);

        Assert.True(result);
        Assert.Equal(CropCycleStageStatus.InProgress, stage.Status);
        Assert.Equal(startDate, stage.ActualStartDate);
        Assert.Null(stage.ActualEndDate);
        Assert.Equal(now, stage.UpdatedAt);
        Assert.Equal(_userId, stage.UpdatedBy);
    }

    [Fact]
    public void CropCycleStage_Complete_TransitionsToCompleted()
    {
        var stage = new CropCycleStage(
            Guid.NewGuid(), Guid.NewGuid(), "Berry Development", 7, 30,
            new DateOnly(2026, 6, 1), new DateOnly(2026, 7, 1), _userId);

        stage.Start(new DateOnly(2026, 6, 2), DateTimeOffset.UtcNow, _userId);

        var endDate = new DateOnly(2026, 6, 28);
        var now = DateTimeOffset.UtcNow;
        var result = stage.Complete(endDate, now, _userId, "Completed ahead of schedule");

        Assert.True(result);
        Assert.Equal(CropCycleStageStatus.Completed, stage.Status);
        Assert.Equal(endDate, stage.ActualEndDate);
        Assert.Contains("Completed ahead of schedule", stage.Notes);
    }

    [Fact]
    public void CropCycleStage_Skip_TransitionsToSkippedWithReason()
    {
        var stage = new CropCycleStage(
            Guid.NewGuid(), Guid.NewGuid(), "Dormancy", 1, 30,
            new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), _userId);

        var now = DateTimeOffset.UtcNow;
        var result = stage.Skip("Warm climate, dormancy bypassed", now, _userId);

        Assert.True(result);
        Assert.Equal(CropCycleStageStatus.Skipped, stage.Status);
        Assert.Contains("[Skipped] Warm climate, dormancy bypassed", stage.Notes);
    }

    [Fact]
    public void CropCycleStage_Skip_RequiresReason()
    {
        var stage = new CropCycleStage(
            Guid.NewGuid(), Guid.NewGuid(), "Dormancy", 1, 30,
            null, null, _userId);

        Assert.Throws<ArgumentException>(() => stage.Skip("", DateTimeOffset.UtcNow, _userId));
        Assert.Throws<ArgumentException>(() => stage.Skip("   ", DateTimeOffset.UtcNow, _userId));
    }

    [Fact]
    public void CropCycleStage_Reopen_TransitionsToInProgressWithReason()
    {
        var stage = new CropCycleStage(
            Guid.NewGuid(), Guid.NewGuid(), "Ripening", 8, 25,
            new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 26), _userId);

        stage.Start(new DateOnly(2026, 7, 1), DateTimeOffset.UtcNow, _userId);
        stage.Complete(new DateOnly(2026, 7, 25), DateTimeOffset.UtcNow, _userId);

        var now = DateTimeOffset.UtcNow;
        var result = stage.Reopen("Sugar content not yet optimal", now, _userId);

        Assert.True(result);
        Assert.Equal(CropCycleStageStatus.InProgress, stage.Status);
        Assert.Null(stage.ActualEndDate);
        Assert.Contains("[Reopened] Sugar content not yet optimal", stage.Notes);
    }

    [Fact]
    public void CropCycleStage_Override_UpdatesStatusAndDatesWithReason()
    {
        var stage = new CropCycleStage(
            Guid.NewGuid(), Guid.NewGuid(), "Harvest", 9, 15,
            new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 15), _userId);

        var now = DateTimeOffset.UtcNow;
        var result = stage.Override(
            CropCycleStageStatus.Cancelled,
            null,
            null,
            "Severe unseasonal hail destroyed crop before harvest",
            now,
            _userId);

        Assert.True(result);
        Assert.Equal(CropCycleStageStatus.Cancelled, stage.Status);
        Assert.Contains("Severe unseasonal hail destroyed crop", stage.Notes);
    }

    [Fact]
    public void EFCore_Model_CropCycleStage_ConfigurationIsValid()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=farm_test;Username=postgres;Password=postgres")
            .Options;

        using var dbContext = new ApplicationDbContext(options);
        var model = dbContext.Model;

        var entityType = model.FindEntityType(typeof(CropCycleStage));
        Assert.NotNull(entityType);
        Assert.Equal("crop_cycle_stages", entityType.GetTableName());

        // Assert primary key
        var pk = entityType.FindPrimaryKey();
        Assert.NotNull(pk);
        Assert.Equal("id", pk.Properties.Single().GetColumnName());

        // Assert properties
        var stageNameProp = entityType.FindProperty(nameof(CropCycleStage.StageName));
        Assert.NotNull(stageNameProp);
        Assert.Equal("stage_name", stageNameProp.GetColumnName());
        Assert.Equal(150, stageNameProp.GetMaxLength());
        Assert.False(stageNameProp.IsNullable);

        var seqProp = entityType.FindProperty(nameof(CropCycleStage.SequenceNumber));
        Assert.NotNull(seqProp);
        Assert.Equal("sequence_number", seqProp.GetColumnName());

        var durationProp = entityType.FindProperty(nameof(CropCycleStage.ExpectedDurationDays));
        Assert.NotNull(durationProp);
        Assert.Equal("expected_duration_days", durationProp.GetColumnName());
        Assert.True(durationProp.IsNullable);

        // Assert no stage_code property in entity or table
        var stageCodeProp = entityType.FindProperty("StageCode");
        Assert.Null(stageCodeProp);

        // Assert unique constraints
        var uniqueIndexes = entityType.GetIndexes().Where(i => i.IsUnique).ToList();
        var cycleTemplateStageIndex = uniqueIndexes.FirstOrDefault(i =>
            i.Properties.Count == 2 &&
            i.Properties.Any(p => p.Name == nameof(CropCycleStage.CropCycleId)) &&
            i.Properties.Any(p => p.Name == nameof(CropCycleStage.LifecycleTemplateStageId)));
        Assert.NotNull(cycleTemplateStageIndex);
        Assert.Equal("ux_crop_cycle_stages_cycle_template_stage", cycleTemplateStageIndex.GetDatabaseName());

        var cycleSequenceIndex = uniqueIndexes.FirstOrDefault(i =>
            i.Properties.Count == 2 &&
            i.Properties.Any(p => p.Name == nameof(CropCycleStage.CropCycleId)) &&
            i.Properties.Any(p => p.Name == nameof(CropCycleStage.SequenceNumber)));
        Assert.NotNull(cycleSequenceIndex);
        Assert.Equal("ux_crop_cycle_stages_cycle_sequence", cycleSequenceIndex.GetDatabaseName());

        // Assert foreign keys
        var fks = entityType.GetForeignKeys().ToList();
        var cropCycleFk = fks.SingleOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(CropCycle));
        Assert.NotNull(cropCycleFk);
        Assert.Equal(DeleteBehavior.Restrict, cropCycleFk.DeleteBehavior);

        var templateStageFk = fks.SingleOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(CropLifecycleStage));
        Assert.NotNull(templateStageFk);
        Assert.Equal(DeleteBehavior.Restrict, templateStageFk.DeleteBehavior);
    }

    [Fact]
    public void EFCore_Model_CropCycle_HasLifecycleTemplateReference()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=farm_test;Username=postgres;Password=postgres")
            .Options;

        using var dbContext = new ApplicationDbContext(options);
        var model = dbContext.Model;

        var cycleEntityType = model.FindEntityType(typeof(CropCycle));
        Assert.NotNull(cycleEntityType);

        var lifecycleProp = cycleEntityType.FindProperty(nameof(CropCycle.LifecycleTemplateId));
        Assert.NotNull(lifecycleProp);
        Assert.Equal("lifecycle_template_id", lifecycleProp.GetColumnName());
        Assert.True(lifecycleProp.IsNullable);

        var templateFk = cycleEntityType.GetForeignKeys()
            .SingleOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(CropLifecycleTemplate));
        Assert.NotNull(templateFk);
        Assert.Equal(DeleteBehavior.Restrict, templateFk.DeleteBehavior);

        // Stages navigation
        var stagesNav = cycleEntityType.FindNavigation(nameof(CropCycle.Stages));
        Assert.NotNull(stagesNav);
        Assert.True(stagesNav.IsCollection);
    }

    [Fact]
    public void EFCore_Model_CropLifecycleStage_DoesNotHaveStageCodeAndHasDuration()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=farm_test;Username=postgres;Password=postgres")
            .Options;

        using var dbContext = new ApplicationDbContext(options);
        var model = dbContext.Model;

        var stageEntityType = model.FindEntityType(typeof(CropLifecycleStage));
        Assert.NotNull(stageEntityType);

        var stageCodeProp = stageEntityType.FindProperty("StageCode");
        Assert.Null(stageCodeProp);

        var durationProp = stageEntityType.FindProperty(nameof(CropLifecycleStage.ExpectedDurationDays));
        Assert.NotNull(durationProp);
        Assert.Equal("expected_duration_days", durationProp.GetColumnName());
        Assert.True(durationProp.IsNullable);
    }
}
