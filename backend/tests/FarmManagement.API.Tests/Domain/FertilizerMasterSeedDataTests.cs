using FarmManagement.Infrastructure.Persistence;
using FarmManagement.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FarmManagement.API.Tests.Domain;

[Collection("DatabaseSeeder")]
public sealed class FertilizerMasterSeedDataTests
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
    public async Task SeedAsync_SeedsAllSevenFertilizerApplicationMethods()
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
            .Where(m => m.IsSystem && m.OrganizationId == null && m.Module == "FERTILIZER")
            .OrderBy(m => m.DisplayOrder)
            .ToListAsync();

        Assert.Equal(7, methods.Count);

        var expectedMethods = new (string Code, string Name, int DisplayOrder)[]
        {
            ("BROADCASTING", "Broadcasting", 10),
            ("TOP_DRESSING", "Top Dressing", 20),
            ("SIDE_DRESSING_BANDING", "Side Dressing / Banding", 30),
            ("PLACEMENT", "Placement", 40),
            ("FERTIGATION", "Fertigation", 50),
            ("FOLIAR", "Foliar Application", 60),
            ("OTHER", "Other", 70)
        };

        for (int i = 0; i < expectedMethods.Length; i++)
        {
            Assert.Equal(expectedMethods[i].Code, methods[i].Code);
            Assert.Equal(expectedMethods[i].Name, methods[i].Name);
            Assert.Equal(expectedMethods[i].DisplayOrder, methods[i].DisplayOrder);
            Assert.Equal("FERTILIZER", methods[i].Module);
            Assert.True(methods[i].IsSystem);
            Assert.True(methods[i].IsActive);
            Assert.Null(methods[i].OrganizationId);
            Assert.False(string.IsNullOrWhiteSpace(methods[i].Description));
        }
    }

    [Fact]
    public async Task SeedAsync_FertilizerAndSprayOtherMethods_CoexistWithoutCollision()
    {
        await using var dbContext = CreateDbContext();
        if (!await dbContext.Database.CanConnectAsync())
        {
            return;
        }

        var config = CreateConfiguration();
        var seeder = new IdentityDataSeeder(dbContext, config, NullLogger<IdentityDataSeeder>.Instance);

        await seeder.SeedAsync();

        var sprayOther = await dbContext.ApplicationMethods
            .SingleOrDefaultAsync(m => m.IsSystem && m.OrganizationId == null && m.Module == "SPRAY" && m.Code == "OTHER");

        var fertilizerOther = await dbContext.ApplicationMethods
            .SingleOrDefaultAsync(m => m.IsSystem && m.OrganizationId == null && m.Module == "FERTILIZER" && m.Code == "OTHER");

        Assert.NotNull(sprayOther);
        Assert.NotNull(fertilizerOther);
        Assert.NotEqual(sprayOther.Id, fertilizerOther.Id);
        Assert.Equal("SPRAY", sprayOther.Module);
        Assert.Equal("FERTILIZER", fertilizerOther.Module);
    }

    [Fact]
    public async Task SeedAsync_FertilizerSeedData_IsIdempotent()
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
        var count1 = await dbContext.ApplicationMethods.CountAsync(m => m.IsSystem && m.Module == "FERTILIZER");

        // Run 2
        await seeder.SeedAsync();
        var count2 = await dbContext.ApplicationMethods.CountAsync(m => m.IsSystem && m.Module == "FERTILIZER");

        Assert.Equal(7, count1);
        Assert.Equal(7, count2);
    }
}
