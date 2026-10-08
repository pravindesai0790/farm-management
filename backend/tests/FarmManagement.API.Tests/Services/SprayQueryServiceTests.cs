using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.Sprays;
using FarmManagement.Application.Interfaces.Sprays;
using FarmManagement.Application.Services;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Services;

public class SprayQueryServiceTests
{
    private readonly FakeSprayStore _store = new();
    private readonly SprayService _sut;

    public SprayQueryServiceTests()
    {
        _sut = new SprayService(_store);
    }

    [Fact]
    public async Task ListAsync_MapsItemsAndCalculatesOverdueCorrectly()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var farmId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var actor = new SprayActor(userId, orgId);

        var sprayPast = new Spray(
            orgId,
            farmId,
            userId,
            status: SprayStatus.Scheduled,
            scheduledDateTime: DateTimeOffset.UtcNow.AddHours(-3),
            purposeReason: "Pest control");

        var sprayFuture = new Spray(
            orgId,
            farmId,
            userId,
            status: SprayStatus.Scheduled,
            scheduledDateTime: DateTimeOffset.UtcNow.AddHours(5),
            purposeReason: "Preventative");

        var sprayCompleted = new Spray(
            orgId,
            farmId,
            userId,
            status: SprayStatus.Completed,
            scheduledDateTime: DateTimeOffset.UtcNow.AddDays(-2),
            actualApplicationDateTime: DateTimeOffset.UtcNow.AddDays(-2));

        _store.Sprays.AddRange([sprayPast, sprayFuture, sprayCompleted]);

        var query = new SprayListQuery(FarmId: farmId);

        // Act
        var result = await _sut.ListAsync(actor, query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Items.Count);

        var first = result.Items.First(x => x.Id == sprayPast.Id);
        Assert.True(first.IsOverdue);
        Assert.Equal(SprayStatus.Scheduled, first.Status);

        var second = result.Items.First(x => x.Id == sprayFuture.Id);
        Assert.False(second.IsOverdue);

        var third = result.Items.First(x => x.Id == sprayCompleted.Id);
        Assert.False(third.IsOverdue);
    }

    [Fact]
    public async Task GetAsync_WhenNotFound_ThrowsResourceNotFoundException()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var sprayId = Guid.NewGuid();
        var actor = new SprayActor(Guid.NewGuid(), orgId);

        // Act & Assert
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => _sut.GetAsync(actor, sprayId));
    }

    [Fact]
    public async Task GetAsync_WhenFound_ReturnsMappedDetails()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var farmId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var actor = new SprayActor(userId, orgId);

        var spray = new Spray(
            orgId,
            farmId,
            userId,
            status: SprayStatus.Scheduled,
            scheduledDateTime: DateTimeOffset.UtcNow.AddHours(-1),
            purposeReason: "Test spray reason");

        var item = new SprayProduct(spray.Id, Guid.NewGuid(), userId, plannedQuantity: 15.5m, dosage: "2ml/L");
        spray.AddProduct(item);

        _store.Sprays.Add(spray);

        // Act
        var result = await _sut.GetAsync(actor, spray.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(spray.Id, result.Id);
        Assert.True(result.IsOverdue);
        Assert.Equal("Test spray reason", result.PurposeReason);
        Assert.Single(result.Products);
        Assert.Equal(15.5m, result.Products[0].PlannedQuantity);
        Assert.Equal("2ml/L", result.Products[0].Dosage);
    }

    private sealed class FakeSprayStore : ISprayStore
    {
        public List<Spray> Sprays { get; } = [];
        public List<AuditLog> AuditLogs { get; } = [];

        public Task<Spray?> FindAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default)
        {
            var spray = Sprays.FirstOrDefault(s => s.OrganizationId == organizationId && s.Id == id);
            return Task.FromResult(spray);
        }

        public Task<int> CountAsync(Guid organizationId, SprayListQuery query, DateTimeOffset now, CancellationToken cancellationToken = default)
        {
            var filtered = Sprays.Where(s => s.OrganizationId == organizationId);
            if (query.FarmId.HasValue)
                filtered = filtered.Where(s => s.FarmId == query.FarmId.Value);

            return Task.FromResult(filtered.Count());
        }

        public Task<IReadOnlyList<Spray>> ListAsync(Guid organizationId, SprayListQuery query, int skip, int take, DateTimeOffset now, CancellationToken cancellationToken = default)
        {
            var filtered = Sprays.Where(s => s.OrganizationId == organizationId);
            if (query.FarmId.HasValue)
                filtered = filtered.Where(s => s.FarmId == query.FarmId.Value);

            var items = filtered.Skip(skip).Take(take).ToList();
            return Task.FromResult<IReadOnlyList<Spray>>(items);
        }

        public void Add(Spray spray) => Sprays.Add(spray);

        public void AddAuditLog(AuditLog auditLog) => AuditLogs.Add(auditLog);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
