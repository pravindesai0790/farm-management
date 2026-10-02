using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.Expenses;
using FarmManagement.Application.Interfaces.Expenses;
using FarmManagement.Application.Services;
using FarmManagement.Domain.Entities;
using Xunit;

namespace FarmManagement.API.Tests.Services;

public class ExpenseCategoryServiceTests
{
    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    private ExpenseActor CreateActor(Guid? organizationId = null) =>
        new(_userId, organizationId ?? _organizationId);

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesCustomCategoryAndAuditLog()
    {
        // Arrange
        var store = new FakeExpenseCategoryStore();
        var service = new ExpenseCategoryService(store);
        var request = new CreateExpenseCategoryRequest("Organic Pesticide", "Non-chemical insect control products");

        // Act
        var result = await service.CreateAsync(CreateActor(), request, "127.0.0.1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Organic Pesticide", result.Name);
        Assert.Equal("Non-chemical insect control products", result.Description);
        Assert.False(result.IsSystemDefault);
        Assert.Equal(_organizationId, result.OrganizationId);
        Assert.True(result.IsActive);
        Assert.Single(store.Categories);
        Assert.Single(store.AuditLogs);
        Assert.Equal("ExpenseCategory.Created", store.AuditLogs[0].Action);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_BlankName_ThrowsValidationException(string? name)
    {
        // Arrange
        var store = new FakeExpenseCategoryStore();
        var service = new ExpenseCategoryService(store);
        var request = new CreateExpenseCategoryRequest(name, null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(CreateActor(), request, "127.0.0.1"));

        Assert.NotNull(ex.Errors);
        Assert.True(ex.Errors.ContainsKey("name"));
    }

    [Fact]
    public async Task CreateAsync_DuplicateNameWithCustomCategory_ThrowsConflictException()
    {
        // Arrange
        var store = new FakeExpenseCategoryStore();
        var existing = new ExpenseCategory(_organizationId, "Seedlings", createdBy: _userId);
        store.Categories.Add(existing);

        var service = new ExpenseCategoryService(store);
        var request = new CreateExpenseCategoryRequest(" seedlings ", null);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(CreateActor(), request, "127.0.0.1"));
    }

    [Fact]
    public async Task CreateAsync_DuplicateNameWithSystemDefaultCategory_ThrowsConflictException()
    {
        // Arrange
        var store = new FakeExpenseCategoryStore();
        var systemDefault = new ExpenseCategory(null, "Fertilizer", isSystemDefault: true);
        store.Categories.Add(systemDefault);

        var service = new ExpenseCategoryService(store);
        var request = new CreateExpenseCategoryRequest("fertilizer", null);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(CreateActor(), request, "127.0.0.1"));
    }

    [Fact]
    public async Task UpdateAsync_SystemDefaultCategory_ThrowsForbiddenException()
    {
        // Arrange
        var store = new FakeExpenseCategoryStore();
        var systemDefault = new ExpenseCategory(null, "Fertilizer", isSystemDefault: true);
        store.Categories.Add(systemDefault);

        var service = new ExpenseCategoryService(store);
        var request = new UpdateExpenseCategoryRequest("Fertilizer Edited", "Updated desc");

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.UpdateAsync(CreateActor(), systemDefault.Id, request, "127.0.0.1"));
    }

    [Fact]
    public async Task UpdateAsync_CustomCategory_UpdatesAndLogsAudit()
    {
        // Arrange
        var store = new FakeExpenseCategoryStore();
        var custom = new ExpenseCategory(_organizationId, "Equipment Maintenance", createdBy: _userId);
        store.Categories.Add(custom);

        var service = new ExpenseCategoryService(store);
        var request = new UpdateExpenseCategoryRequest("Tractor Maintenance", "Tractor repairs and fluids");

        // Act
        var result = await service.UpdateAsync(CreateActor(), custom.Id, request, "127.0.0.1");

        // Assert
        Assert.Equal("Tractor Maintenance", result.Name);
        Assert.Equal("Tractor repairs and fluids", result.Description);
        Assert.Single(store.AuditLogs);
        Assert.Equal("ExpenseCategory.Updated", store.AuditLogs[0].Action);
    }

    [Fact]
    public async Task DeactivateAsync_SystemDefaultCategory_ThrowsForbiddenException()
    {
        // Arrange
        var store = new FakeExpenseCategoryStore();
        var systemDefault = new ExpenseCategory(null, "Seeds", isSystemDefault: true);
        store.Categories.Add(systemDefault);

        var service = new ExpenseCategoryService(store);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.DeactivateAsync(CreateActor(), systemDefault.Id, "127.0.0.1"));
    }

    [Fact]
    public async Task DeactivateAndActivate_CustomCategory_TogglesStatus()
    {
        // Arrange
        var store = new FakeExpenseCategoryStore();
        var custom = new ExpenseCategory(_organizationId, "Special Tools", createdBy: _userId);
        store.Categories.Add(custom);

        var service = new ExpenseCategoryService(store);

        // Act - Deactivate
        var deactivated = await service.DeactivateAsync(CreateActor(), custom.Id, "127.0.0.1");
        Assert.True(deactivated);
        Assert.False(custom.IsActive);

        // Act - Activate
        var activated = await service.ActivateAsync(CreateActor(), custom.Id, "127.0.0.1");
        Assert.True(activated);
        Assert.True(custom.IsActive);
    }

    [Fact]
    public async Task ListAsync_ReturnsBothSystemAndCustomCategories()
    {
        // Arrange
        var store = new FakeExpenseCategoryStore();
        var systemDefault = new ExpenseCategory(null, "Fertilizer", isSystemDefault: true);
        var custom = new ExpenseCategory(_organizationId, "Greenhouse Plastic", createdBy: _userId);
        var otherOrgCategory = new ExpenseCategory(Guid.NewGuid(), "Other Org Only", createdBy: _userId);

        store.Categories.Add(systemDefault);
        store.Categories.Add(custom);
        store.Categories.Add(otherOrgCategory);

        var service = new ExpenseCategoryService(store);

        // Act
        var result = await service.ListAsync(CreateActor(), 1, 20, null, null);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.Contains(result.Items, c => c.Name == "Fertilizer" && c.IsSystemDefault);
        Assert.Contains(result.Items, c => c.Name == "Greenhouse Plastic" && !c.IsSystemDefault);
        Assert.DoesNotContain(result.Items, c => c.Name == "Other Org Only");
    }

    private sealed class FakeExpenseCategoryStore : IExpenseCategoryStore
    {
        public List<ExpenseCategory> Categories { get; } = [];
        public List<AuditLog> AuditLogs { get; } = [];

        public Task<int> CountAsync(Guid organizationId, string? search, bool? isActive, CancellationToken cancellationToken = default) =>
            Task.FromResult(Filter(organizationId, search, isActive).Count());

        public Task<IReadOnlyList<ExpenseCategory>> ListAsync(Guid organizationId, int skip, int take, string? search, bool? isActive, CancellationToken cancellationToken = default)
        {
            var result = Filter(organizationId, search, isActive).Skip(skip).Take(take).ToList();
            return Task.FromResult<IReadOnlyList<ExpenseCategory>>(result);
        }

        public Task<ExpenseCategory?> FindAsync(Guid categoryId, Guid organizationId, CancellationToken cancellationToken = default)
        {
            var category = Categories.FirstOrDefault(c =>
                c.Id == categoryId && (c.IsSystemDefault || c.OrganizationId == organizationId));
            return Task.FromResult(category);
        }

        public Task<bool> NameExistsAsync(Guid organizationId, string name, Guid? excludingCategoryId = null, CancellationToken cancellationToken = default)
        {
            var normalized = name.Trim().ToLowerInvariant();
            var exists = Categories.Any(c =>
                (c.IsSystemDefault || c.OrganizationId == organizationId) &&
                c.Name.ToLowerInvariant() == normalized &&
                (!excludingCategoryId.HasValue || c.Id != excludingCategoryId.Value));
            return Task.FromResult(exists);
        }

        public Task<bool> HasHistoricalReferencesAsync(Guid categoryId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public void Add(ExpenseCategory category) => Categories.Add(category);

        public void AddAuditLog(AuditLog auditLog) => AuditLogs.Add(auditLog);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        private IEnumerable<ExpenseCategory> Filter(Guid organizationId, string? search, bool? isActive)
        {
            var query = Categories.Where(c => c.IsSystemDefault || c.OrganizationId == organizationId);
            if (isActive.HasValue) query = query.Where(c => c.IsActive == isActive.Value);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLowerInvariant();
                query = query.Where(x => x.Name.ToLowerInvariant().Contains(s));
            }
            return query;
        }
    }
}
