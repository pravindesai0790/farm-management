using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.Expenses;
using FarmManagement.Application.Interfaces.Expenses;
using FarmManagement.Application.Services;
using FarmManagement.Domain.Entities;
using Xunit;

namespace FarmManagement.API.Tests.Services;

public class SupplierServiceTests
{
    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    private ExpenseActor CreateActor(Guid? organizationId = null) =>
        new(_userId, organizationId ?? _organizationId);

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesSupplierAndAuditLog()
    {
        // Arrange
        var store = new FakeSupplierStore();
        var service = new SupplierService(store);
        var request = new CreateSupplierRequest(
            "Agri Supplies Ltd",
            "John Doe",
            "+1234567890",
            "john@agrisupplies.com",
            "123 Farm Road",
            "REG-999",
            "Primary seed and fertilizer supplier");

        // Act
        var result = await service.CreateAsync(CreateActor(), request, "127.0.0.1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Agri Supplies Ltd", result.Name);
        Assert.Equal("John Doe", result.ContactPerson);
        Assert.Equal("+1234567890", result.Phone);
        Assert.Equal("john@agrisupplies.com", result.Email);
        Assert.Equal("123 Farm Road", result.Address);
        Assert.Equal("REG-999", result.RegistrationIdentifier);
        Assert.True(result.IsActive);
        Assert.Single(store.Suppliers);
        Assert.Single(store.AuditLogs);
        Assert.Equal("Supplier.Created", store.AuditLogs[0].Action);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_BlankName_ThrowsValidationException(string? name)
    {
        // Arrange
        var store = new FakeSupplierStore();
        var service = new SupplierService(store);
        var request = new CreateSupplierRequest(name, null, null, null, null, null, null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(CreateActor(), request, "127.0.0.1"));

        Assert.NotNull(ex.Errors);
        Assert.True(ex.Errors.ContainsKey("name"));
    }

    [Fact]
    public async Task CreateAsync_InvalidEmail_ThrowsValidationException()
    {
        // Arrange
        var store = new FakeSupplierStore();
        var service = new SupplierService(store);
        var request = new CreateSupplierRequest("Valid Supplier", null, null, "invalid-email-format", null, null, null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(CreateActor(), request, "127.0.0.1"));

        Assert.NotNull(ex.Errors);
        Assert.True(ex.Errors.ContainsKey("email"));
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ThrowsConflictException()
    {
        // Arrange
        var store = new FakeSupplierStore();
        var existing = Supplier.Create(_organizationId, "Existing Supplier", _userId);
        store.Suppliers.Add(existing);

        var service = new SupplierService(store);
        var request = new CreateSupplierRequest("  existing supplier  ", null, null, null, null, null, null);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(CreateActor(), request, "127.0.0.1"));
    }

    [Fact]
    public async Task UpdateAsync_ValidRequest_UpdatesSupplierAndAuditLog()
    {
        // Arrange
        var store = new FakeSupplierStore();
        var supplier = Supplier.Create(_organizationId, "Old Name", _userId);
        store.Suppliers.Add(supplier);

        var service = new SupplierService(store);
        var request = new UpdateSupplierRequest("New Name", "Jane Doe", "0987654321", "jane@test.com", "New Address", "REG-001", "Updated notes");

        // Act
        var result = await service.UpdateAsync(CreateActor(), supplier.Id, request, "127.0.0.1");

        // Assert
        Assert.Equal("New Name", result.Name);
        Assert.Equal("Jane Doe", result.ContactPerson);
        Assert.Single(store.AuditLogs);
        Assert.Equal("Supplier.Updated", store.AuditLogs[0].Action);
    }

    [Fact]
    public async Task UpdateAsync_DuplicateNameOnOtherSupplier_ThrowsConflictException()
    {
        // Arrange
        var store = new FakeSupplierStore();
        var supplier1 = Supplier.Create(_organizationId, "Supplier One", _userId);
        var supplier2 = Supplier.Create(_organizationId, "Supplier Two", _userId);
        store.Suppliers.Add(supplier1);
        store.Suppliers.Add(supplier2);

        var service = new SupplierService(store);
        var request = new UpdateSupplierRequest("Supplier One", null, null, null, null, null, null);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() =>
            service.UpdateAsync(CreateActor(), supplier2.Id, request, "127.0.0.1"));
    }

    [Fact]
    public async Task UpdateAsync_SameNameOnSameSupplier_Succeeds()
    {
        // Arrange
        var store = new FakeSupplierStore();
        var supplier = Supplier.Create(_organizationId, "Supplier One", _userId);
        store.Suppliers.Add(supplier);

        var service = new SupplierService(store);
        var request = new UpdateSupplierRequest("Supplier One", "New Contact", null, null, null, null, null);

        // Act
        var result = await service.UpdateAsync(CreateActor(), supplier.Id, request, "127.0.0.1");

        // Assert
        Assert.Equal("Supplier One", result.Name);
        Assert.Equal("New Contact", result.ContactPerson);
    }

    [Fact]
    public async Task ActivateAndDeactivate_TogglesStatusAndAddsAudit()
    {
        // Arrange
        var store = new FakeSupplierStore();
        var supplier = Supplier.Create(_organizationId, "Test Supplier", _userId);
        store.Suppliers.Add(supplier);
        var service = new SupplierService(store);

        // Act - Deactivate
        var deactivated = await service.DeactivateAsync(CreateActor(), supplier.Id, "127.0.0.1");
        Assert.True(deactivated);
        Assert.False(supplier.IsActive);

        // Act - Deactivate again
        var deactivatedAgain = await service.DeactivateAsync(CreateActor(), supplier.Id, "127.0.0.1");
        Assert.False(deactivatedAgain);

        // Act - Activate
        var activated = await service.ActivateAsync(CreateActor(), supplier.Id, "127.0.0.1");
        Assert.True(activated);
        Assert.True(supplier.IsActive);

        // Act - Activate again
        var activatedAgain = await service.ActivateAsync(CreateActor(), supplier.Id, "127.0.0.1");
        Assert.False(activatedAgain);
    }

    [Fact]
    public async Task TenantIsolation_CrossTenantAccess_ThrowsResourceNotFoundException()
    {
        // Arrange
        var store = new FakeSupplierStore();
        var otherOrgId = Guid.NewGuid();
        var supplier = Supplier.Create(otherOrgId, "Other Org Supplier", _userId);
        store.Suppliers.Add(supplier);

        var service = new SupplierService(store);

        // Act & Assert
        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            service.GetAsync(CreateActor(_organizationId), supplier.Id));
    }

    private sealed class FakeSupplierStore : ISupplierStore
    {
        public List<Supplier> Suppliers { get; } = [];
        public List<AuditLog> AuditLogs { get; } = [];

        public Task<int> CountAsync(Guid organizationId, string? search, bool? isActive, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Filter(organizationId, search, isActive).Count());
        }

        public Task<IReadOnlyList<Supplier>> ListAsync(Guid organizationId, int skip, int take, string? search, bool? isActive, CancellationToken cancellationToken = default)
        {
            var result = Filter(organizationId, search, isActive).Skip(skip).Take(take).ToList();
            return Task.FromResult<IReadOnlyList<Supplier>>(result);
        }

        public Task<Supplier?> FindAsync(Guid supplierId, Guid organizationId, CancellationToken cancellationToken = default)
        {
            var supplier = Suppliers.FirstOrDefault(s => s.Id == supplierId && s.OrganizationId == organizationId);
            return Task.FromResult(supplier);
        }

        public Task<bool> NameExistsAsync(Guid organizationId, string name, Guid? excludingSupplierId = null, CancellationToken cancellationToken = default)
        {
            var normalized = name.Trim().ToLowerInvariant();
            var exists = Suppliers.Any(s =>
                s.OrganizationId == organizationId &&
                s.Name.ToLowerInvariant() == normalized &&
                (!excludingSupplierId.HasValue || s.Id != excludingSupplierId.Value));
            return Task.FromResult(exists);
        }

        public Task<bool> HasHistoricalReferencesAsync(Guid supplierId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public void Add(Supplier supplier) => Suppliers.Add(supplier);

        public void AddAuditLog(AuditLog auditLog) => AuditLogs.Add(auditLog);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        private IEnumerable<Supplier> Filter(Guid organizationId, string? search, bool? isActive)
        {
            var query = Suppliers.Where(s => s.OrganizationId == organizationId);
            if (isActive.HasValue) query = query.Where(s => s.IsActive == isActive.Value);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLowerInvariant();
                query = query.Where(x => x.Name.ToLowerInvariant().Contains(s));
            }
            return query;
        }
    }
}
