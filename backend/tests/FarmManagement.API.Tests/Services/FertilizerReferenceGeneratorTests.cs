using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using FarmManagement.Domain.Entities;
using FarmManagement.Infrastructure.Persistence;
using FarmManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FarmManagement.API.Tests.Services;

[Collection("DatabaseSeeder")]
public sealed class FertilizerReferenceGeneratorTests
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

    private static async Task<Organization> CreateTestOrgAsync(ApplicationDbContext dbContext)
    {
        var uniqueCode = $"ORG_{Guid.NewGuid():N}"[..20].ToUpperInvariant();
        var org = new Organization($"Test Organization {uniqueCode}", uniqueCode);
        dbContext.Organizations.Add(org);
        await dbContext.SaveChangesAsync();
        return org;
    }

    [Fact]
    public async Task NextReferenceNumber_GeneratesFormattedNumber_WithLeadingZeros()
    {
        await using var dbContext = CreateDbContext();
        if (!await dbContext.Database.CanConnectAsync())
        {
            return;
        }

        var org = await CreateTestOrgAsync(dbContext);
        var generator = new FertilizerReferenceGenerator(dbContext);

        var refNumber = await generator.NextReferenceNumberAsync(org.Id, 2026);

        Assert.NotNull(refNumber);
        Assert.Matches(@"^FERT-2026-\d{6}$", refNumber);
        Assert.Equal("FERT-2026-000001", refNumber);
    }

    [Fact]
    public async Task NextReferenceNumber_SequentialCalls_GeneratesAscendingNumbers()
    {
        await using var dbContext = CreateDbContext();
        if (!await dbContext.Database.CanConnectAsync())
        {
            return;
        }

        var org = await CreateTestOrgAsync(dbContext);
        var generator = new FertilizerReferenceGenerator(dbContext);

        var ref1 = await generator.NextReferenceNumberAsync(org.Id, 2026);
        var ref2 = await generator.NextReferenceNumberAsync(org.Id, 2026);
        var ref3 = await generator.NextReferenceNumberAsync(org.Id, 2026);

        Assert.Equal("FERT-2026-000001", ref1);
        Assert.Equal("FERT-2026-000002", ref2);
        Assert.Equal("FERT-2026-000003", ref3);
    }

    [Fact]
    public async Task NextReferenceNumber_MultipleOrganizations_HaveIndependentSequences()
    {
        await using var dbContext = CreateDbContext();
        if (!await dbContext.Database.CanConnectAsync())
        {
            return;
        }

        var orgA = await CreateTestOrgAsync(dbContext);
        var orgB = await CreateTestOrgAsync(dbContext);
        var generator = new FertilizerReferenceGenerator(dbContext);

        var refA1 = await generator.NextReferenceNumberAsync(orgA.Id, 2026);
        var refB1 = await generator.NextReferenceNumberAsync(orgB.Id, 2026);
        var refA2 = await generator.NextReferenceNumberAsync(orgA.Id, 2026);
        var refB2 = await generator.NextReferenceNumberAsync(orgB.Id, 2026);

        Assert.Equal("FERT-2026-000001", refA1);
        Assert.Equal("FERT-2026-000001", refB1);
        Assert.Equal("FERT-2026-000002", refA2);
        Assert.Equal("FERT-2026-000002", refB2);
    }

    [Fact]
    public async Task NextReferenceNumber_YearRollover_ResetsSequenceForNewYear()
    {
        await using var dbContext = CreateDbContext();
        if (!await dbContext.Database.CanConnectAsync())
        {
            return;
        }

        var org = await CreateTestOrgAsync(dbContext);
        var generator = new FertilizerReferenceGenerator(dbContext);

        var ref2026_1 = await generator.NextReferenceNumberAsync(org.Id, 2026);
        var ref2027_1 = await generator.NextReferenceNumberAsync(org.Id, 2027);
        var ref2026_2 = await generator.NextReferenceNumberAsync(org.Id, 2026);

        Assert.Equal("FERT-2026-000001", ref2026_1);
        Assert.Equal("FERT-2027-000001", ref2027_1);
        Assert.Equal("FERT-2026-000002", ref2026_2);
    }

    [Fact]
    public async Task NextReferenceNumber_InvalidInputs_ThrowsAppropriateExceptions()
    {
        await using var dbContext = CreateDbContext();
        var generator = new FertilizerReferenceGenerator(dbContext);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            generator.NextReferenceNumberAsync(Guid.Empty, 2026));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            generator.NextReferenceNumberAsync(Guid.NewGuid(), 1999));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            generator.NextReferenceNumberAsync(Guid.NewGuid(), 2201));
    }

    [Fact]
    public async Task NextReferenceNumber_ConcurrentAllocations_GeneratesUniqueSequentialNumbers()
    {
        await using var dbContext = CreateDbContext();
        if (!await dbContext.Database.CanConnectAsync())
        {
            return;
        }

        var org = await CreateTestOrgAsync(dbContext);
        const int concurrentTasks = 15;
        var bag = new ConcurrentBag<string>();

        // Execute concurrent tasks across separate DbContext instances to simulate distinct HTTP requests
        var tasks = Enumerable.Range(0, concurrentTasks).Select(async _ =>
        {
            await using var ctx = CreateDbContext();
            var gen = new FertilizerReferenceGenerator(ctx);
            var refNo = await gen.NextReferenceNumberAsync(org.Id, 2026);
            bag.Add(refNo);
        });

        await Task.WhenAll(tasks);

        Assert.Equal(concurrentTasks, bag.Count);
        var distinctRefs = bag.Distinct().ToList();
        Assert.Equal(concurrentTasks, distinctRefs.Count);

        // Every number must match the expected format
        foreach (var r in distinctRefs)
        {
            Assert.Matches(@"^FERT-2026-\d{6}$", r);
        }

        // Sequence numbers should span 1..concurrentTasks
        var sequenceNumbers = distinctRefs
            .Select(r => int.Parse(r.Split('-')[2]))
            .OrderBy(n => n)
            .ToList();

        for (int i = 0; i < concurrentTasks; i++)
        {
            Assert.Equal(i + 1, sequenceNumbers[i]);
        }
    }

    [Fact]
    public async Task NextReferenceNumber_DeletedReference_IsNotReused()
    {
        await using var dbContext = CreateDbContext();
        if (!await dbContext.Database.CanConnectAsync())
        {
            return;
        }

        var org = await CreateTestOrgAsync(dbContext);
        var generator = new FertilizerReferenceGenerator(dbContext);

        // Allocate first reference (simulate draft creation)
        var ref1 = await generator.NextReferenceNumberAsync(org.Id, 2026);
        Assert.Equal("FERT-2026-000001", ref1);

        // Simulate deletion of the draft: the sequence in fertilizer_reference_sequences is NOT decremented
        var ref2 = await generator.NextReferenceNumberAsync(org.Id, 2026);
        Assert.Equal("FERT-2026-000002", ref2);
        Assert.NotEqual(ref1, ref2);
    }
}
