using FarmManagement.Domain.Entities;
using FarmManagement.Infrastructure.Persistence;
using FarmManagement.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FarmManagement.API.Tests.Domain;

public sealed class CropLifecycleSeedDataTests
{
    private const string TestConnectionString =
        "Host=localhost;Port=5432;Database=farm_management;Username=farm_user;Password=change_this_password";

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(TestConnectionString)
            .Options;

        return new ApplicationDbContext(options);
    }

    private static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["InitialAdmin:Email"] = "admin@example.com",
                ["InitialAdmin:Password"] = "TestPassword123!"
            })
            .Build();
    }

    [Fact]
    public async Task SeedAsync_SeedsGrapeStandardLifecycleWithNineStages()
    {
        await using var dbContext = CreateDbContext();
        var config = CreateConfiguration();
        var seeder = new IdentityDataSeeder(dbContext, config, NullLogger<IdentityDataSeeder>.Instance);

        await seeder.SeedAsync();

        var grapeCrop = await dbContext.Crops
            .SingleOrDefaultAsync(c => c.IsSystem && c.OrganizationId == null && (c.Name == "Grapes" || c.Name == "Grape"));
        Assert.NotNull(grapeCrop);

        var template = await dbContext.CropLifecycleTemplates
            .Include(t => t.Stages)
            .SingleOrDefaultAsync(t => t.IsSystem && t.OrganizationId == null && t.CropId == grapeCrop.Id && t.Name == "Grape Standard Lifecycle");

        Assert.NotNull(template);
        Assert.True(template.IsSystem);
        Assert.True(template.IsActive);
        Assert.True(template.IsDefault);
        Assert.Null(template.OrganizationId);

        var stages = template.Stages.OrderBy(s => s.SequenceNumber).ToList();
        Assert.Equal(9, stages.Count);

        var expectedStages = new (int Sequence, string Name, int Duration)[]
        {
            (1, "Dormancy", 30),
            (2, "Pruning", 15),
            (3, "Bud Break", 10),
            (4, "Shoot Development", 20),
            (5, "Flowering", 7),
            (6, "Fruit Set", 10),
            (7, "Berry Development", 30),
            (8, "Ripening", 25),
            (9, "Harvest", 15)
        };

        for (int i = 0; i < expectedStages.Length; i++)
        {
            var expected = expectedStages[i];
            var actual = stages[i];

            Assert.Equal(expected.Sequence, actual.SequenceNumber);
            Assert.Equal(expected.Name, actual.StageName);
            Assert.Equal(expected.Duration, actual.ExpectedDurationDays);
            Assert.True(actual.IsActive);
        }

        var totalExpectedDays = stages.Sum(s => s.ExpectedDurationDays ?? 0);
        Assert.Equal(162, totalExpectedDays);

        // Verify stage_code property does not exist on CropLifecycleStage
        var stageCodeProp = typeof(CropLifecycleStage).GetProperty("StageCode");
        Assert.Null(stageCodeProp);
    }

    [Fact]
    public async Task SeedAsync_IsIdempotent_OnMultipleExecutions()
    {
        await using var dbContext = CreateDbContext();
        var config = CreateConfiguration();
        var seeder = new IdentityDataSeeder(dbContext, config, NullLogger<IdentityDataSeeder>.Instance);

        // First seeding pass
        await seeder.SeedAsync();

        var grapeCrop = await dbContext.Crops.SingleAsync(c => c.IsSystem && c.OrganizationId == null && (c.Name == "Grapes" || c.Name == "Grape"));
        var initialTemplateCount = await dbContext.CropLifecycleTemplates.CountAsync(t => t.CropId == grapeCrop.Id && t.Name == "Grape Standard Lifecycle");
        var initialStageCount = await dbContext.CropLifecycleStages.CountAsync(s => s.LifecycleTemplate.Name == "Grape Standard Lifecycle");

        Assert.Equal(1, initialTemplateCount);
        Assert.Equal(9, initialStageCount);

        // Second seeding pass (idempotency check)
        await seeder.SeedAsync();

        var secondTemplateCount = await dbContext.CropLifecycleTemplates.CountAsync(t => t.CropId == grapeCrop.Id && t.Name == "Grape Standard Lifecycle");
        var secondStageCount = await dbContext.CropLifecycleStages.CountAsync(s => s.LifecycleTemplate.Name == "Grape Standard Lifecycle");

        Assert.Equal(1, secondTemplateCount);
        Assert.Equal(9, secondStageCount);
    }

    [Fact]
    public async Task EFCore_Model_CropLifecycleStage_DoesNotContainStageCode()
    {
        await using var dbContext = CreateDbContext();
        var model = dbContext.Model;

        var entityType = model.FindEntityType(typeof(CropLifecycleStage));
        Assert.NotNull(entityType);

        var stageCodeProp = entityType.FindProperty("StageCode");
        Assert.Null(stageCodeProp);

        var durationProp = entityType.FindProperty(nameof(CropLifecycleStage.ExpectedDurationDays));
        Assert.NotNull(durationProp);
        Assert.Equal("expected_duration_days", durationProp.GetColumnName());
    }
}
