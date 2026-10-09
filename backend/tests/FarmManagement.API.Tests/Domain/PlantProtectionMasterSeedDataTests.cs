using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using FarmManagement.Infrastructure.Persistence;
using FarmManagement.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FarmManagement.API.Tests.Domain;

[Collection("DatabaseSeeder")]
public sealed class PlantProtectionMasterSeedDataTests
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
    public async Task SeedAsync_SeedsAllNineProductTypes()
    {
        await using var dbContext = CreateDbContext();
        if (!await dbContext.Database.CanConnectAsync())
        {
            return;
        }

        var config = CreateConfiguration();
        var seeder = new IdentityDataSeeder(dbContext, config, NullLogger<IdentityDataSeeder>.Instance);

        await seeder.SeedAsync();

        var productTypes = await dbContext.ProductTypes
            .Where(pt => pt.IsSystem && pt.OrganizationId == null)
            .OrderBy(pt => pt.DisplayOrder)
            .ToListAsync();

        Assert.Equal(9, productTypes.Count);

        var expectedTypes = new (string Code, string Name)[]
        {
            ("FUNGICIDE", "Fungicide"),
            ("INSECTICIDE", "Insecticide"),
            ("MITICIDE", "Miticide"),
            ("BACTERICIDE", "Bactericide"),
            ("HERBICIDE", "Herbicide"),
            ("NEMATICIDE", "Nematicide"),
            ("BIOLOGICAL", "Biological"),
            ("ADJUVANT", "Adjuvant"),
            ("OTHER", "Other")
        };

        for (int i = 0; i < expectedTypes.Length; i++)
        {
            Assert.Equal(expectedTypes[i].Code, productTypes[i].Code);
            Assert.Equal(expectedTypes[i].Name, productTypes[i].Name);
            Assert.True(productTypes[i].IsSystem);
            Assert.True(productTypes[i].IsActive);
            Assert.Null(productTypes[i].OrganizationId);
            Assert.False(string.IsNullOrWhiteSpace(productTypes[i].Description));
        }
    }

    [Fact]
    public async Task SeedAsync_SeedsAllSixteenTargetsWithCorrectTargetTypes()
    {
        await using var dbContext = CreateDbContext();
        if (!await dbContext.Database.CanConnectAsync())
        {
            return;
        }

        var config = CreateConfiguration();
        var seeder = new IdentityDataSeeder(dbContext, config, NullLogger<IdentityDataSeeder>.Instance);

        await seeder.SeedAsync();

        var targets = await dbContext.Targets
            .Where(t => t.IsSystem && t.OrganizationId == null)
            .OrderBy(t => t.DisplayOrder)
            .ToListAsync();

        Assert.Equal(16, targets.Count);

        var expectedTargets = new (string Code, string Name, TargetType Type)[]
        {
            ("POWDERY_MILDEW", "Powdery Mildew", TargetType.Disease),
            ("DOWNY_MILDEW", "Downy Mildew", TargetType.Disease),
            ("ANTHRACNOSE", "Anthracnose", TargetType.Disease),
            ("BACTERIAL_DISEASE", "Bacterial Disease", TargetType.Disease),
            ("OTHER_DISEASE", "Other Disease", TargetType.Disease),
            ("THRIPS", "Thrips", TargetType.Insect),
            ("MEALYBUG", "Mealybug", TargetType.Insect),
            ("APHIDS", "Aphids", TargetType.Insect),
            ("FRUIT_FLY", "Fruit Fly", TargetType.Insect),
            ("OTHER_INSECT", "Other Insect", TargetType.Insect),
            ("RED_SPIDER_MITE", "Red Spider Mite", TargetType.Mite),
            ("OTHER_MITE", "Other Mite", TargetType.Mite),
            ("GENERAL_WEED", "General Weed", TargetType.Weed),
            ("OTHER_WEED", "Other Weed", TargetType.Weed),
            ("GENERAL_PREVENTIVE", "General Preventive", TargetType.Other),
            ("OTHER", "Other", TargetType.Other)
        };

        for (int i = 0; i < expectedTargets.Length; i++)
        {
            Assert.Equal(expectedTargets[i].Code, targets[i].Code);
            Assert.Equal(expectedTargets[i].Name, targets[i].Name);
            Assert.Equal(expectedTargets[i].Type, targets[i].TargetType);
            Assert.True(targets[i].IsSystem);
            Assert.True(targets[i].IsActive);
            Assert.Null(targets[i].OrganizationId);
            Assert.False(string.IsNullOrWhiteSpace(targets[i].Description));
        }
    }

    [Fact]
    public async Task SeedAsync_SeedsAllSixApplicationMethods()
    {
        await using var dbContext = CreateDbContext();
        if (!await dbContext.Database.CanConnectAsync())
        {
            return;
        }

        var config = CreateConfiguration();
        var seeder = new IdentityDataSeeder(dbContext, config, NullLogger<IdentityDataSeeder>.Instance);

        await seeder.SeedAsync();

        var methods = await dbContext.ApplicationMethods
            .Where(m => m.IsSystem && m.OrganizationId == null)
            .OrderBy(m => m.DisplayOrder)
            .ToListAsync();

        Assert.Equal(6, methods.Count);

        var expectedMethods = new (string Code, string Name)[]
        {
            ("KNAPSACK_SPRAYER", "Knapsack Sprayer"),
            ("POWER_SPRAYER", "Power Sprayer"),
            ("TRACTOR_SPRAYER", "Tractor Sprayer"),
            ("DRONE_SPRAYER", "Drone Sprayer"),
            ("MANUAL", "Manual"),
            ("OTHER", "Other")
        };

        for (int i = 0; i < expectedMethods.Length; i++)
        {
            Assert.Equal(expectedMethods[i].Code, methods[i].Code);
            Assert.Equal(expectedMethods[i].Name, methods[i].Name);
            Assert.True(methods[i].IsSystem);
            Assert.True(methods[i].IsActive);
            Assert.Null(methods[i].OrganizationId);
            Assert.False(string.IsNullOrWhiteSpace(methods[i].Description));
        }
    }

    [Fact]
    public async Task SeedAsync_MasterSeedData_IsIdempotent()
    {
        await using var dbContext = CreateDbContext();
        if (!await dbContext.Database.CanConnectAsync())
        {
            return;
        }

        var config = CreateConfiguration();
        var seeder = new IdentityDataSeeder(dbContext, config, NullLogger<IdentityDataSeeder>.Instance);

        // Run 1
        await seeder.SeedAsync();

        var ptCount1 = await dbContext.ProductTypes.CountAsync(pt => pt.IsSystem);
        var targetCount1 = await dbContext.Targets.CountAsync(t => t.IsSystem);
        var methodCount1 = await dbContext.ApplicationMethods.CountAsync(m => m.IsSystem);

        Assert.Equal(9, ptCount1);
        Assert.Equal(16, targetCount1);
        Assert.Equal(6, methodCount1);

        // Run 2 (idempotency check)
        await seeder.SeedAsync();

        var ptCount2 = await dbContext.ProductTypes.CountAsync(pt => pt.IsSystem);
        var targetCount2 = await dbContext.Targets.CountAsync(t => t.IsSystem);
        var methodCount2 = await dbContext.ApplicationMethods.CountAsync(m => m.IsSystem);

        Assert.Equal(9, ptCount2);
        Assert.Equal(16, targetCount2);
        Assert.Equal(6, methodCount2);
    }
}
