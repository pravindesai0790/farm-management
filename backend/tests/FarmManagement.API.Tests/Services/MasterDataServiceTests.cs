using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.MasterData;
using FarmManagement.Application.Interfaces;
using FarmManagement.Application.Services;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Services;

public sealed class MasterDataServiceTests
{
    private readonly FakeMasterDataStore _store = new();
    private readonly MasterDataService _sut;
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public MasterDataServiceTests()
    {
        _sut = new MasterDataService(_store);
    }

    private MasterDataActor CreateActor() => new(_userId, _orgId);

    [Fact]
    public async Task ListProductTypes_WhenIncludeInactiveFalse_ReturnsOnlyActive()
    {
        // Arrange
        var actor = CreateActor();
        var activeSystem = new ProductType(null, "FUNG", "Fungicide", isSystem: true);
        var inactiveCustom = new ProductType(_orgId, "CUST_INACT", "Inactive Custom", isSystem: false);
        inactiveCustom.Deactivate(DateTimeOffset.UtcNow, _userId);

        _store.ProductTypes.AddRange([activeSystem, inactiveCustom]);

        // Act
        var result = await _sut.ListProductTypesAsync(actor, includeInactive: false);

        // Assert
        Assert.Single(result);
        Assert.Equal("FUNG", result[0].Code);
    }

    [Fact]
    public async Task ListProductTypes_WhenIncludeInactiveTrue_ReturnsAll()
    {
        // Arrange
        var actor = CreateActor();
        var activeSystem = new ProductType(null, "FUNG", "Fungicide", isSystem: true);
        var inactiveCustom = new ProductType(_orgId, "CUST_INACT", "Inactive Custom", isSystem: false);
        inactiveCustom.Deactivate(DateTimeOffset.UtcNow, _userId);

        _store.ProductTypes.AddRange([activeSystem, inactiveCustom]);

        // Act
        var result = await _sut.ListProductTypesAsync(actor, includeInactive: true);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task CreateProductType_WithValidData_CreatesOrganizationProductType()
    {
        // Arrange
        var actor = CreateActor();
        var request = new CreateProductTypeRequest("bio_fung", "Bio Fungicide", "Organic solution", 5);

        // Act
        var result = await _sut.CreateProductTypeAsync(actor, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("BIO_FUNG", result.Code);
        Assert.Equal("Bio Fungicide", result.Name);
        Assert.False(result.IsSystem);
        Assert.True(result.IsActive);
        Assert.Equal(5, result.DisplayOrder);
    }

    [Fact]
    public async Task CreateProductType_WhenCodeExists_ThrowsConflictException()
    {
        // Arrange
        var actor = CreateActor();
        _store.ProductTypes.Add(new ProductType(null, "EXISTING", "Existing", isSystem: true));

        var request = new CreateProductTypeRequest("existing", "Duplicate Code", null, 0);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => _sut.CreateProductTypeAsync(actor, request));
    }

    [Fact]
    public async Task UpdateProductType_WhenIsSystem_ThrowsForbiddenException()
    {
        // Arrange
        var actor = CreateActor();
        var systemItem = new ProductType(null, "SYSTEM_TYPE", "System Type", isSystem: true);
        _store.ProductTypes.Add(systemItem);

        var request = new UpdateProductTypeRequest("Updated Name", null, 0);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(() => _sut.UpdateProductTypeAsync(actor, systemItem.Id, request));
    }

    [Fact]
    public async Task UpdateProductType_WhenCustom_UpdatesSuccessfully()
    {
        // Arrange
        var actor = CreateActor();
        var custom = new ProductType(_orgId, "CUSTOM_TYPE", "Original Name", isSystem: false);
        _store.ProductTypes.Add(custom);

        var request = new UpdateProductTypeRequest("Updated Name", "New Description", 10);

        // Act
        var result = await _sut.UpdateProductTypeAsync(actor, custom.Id, request);

        // Assert
        Assert.Equal("Updated Name", result.Name);
        Assert.Equal("New Description", result.Description);
        Assert.Equal(10, result.DisplayOrder);
    }

    [Fact]
    public async Task DeactivateProductType_WhenIsSystem_ThrowsForbiddenException()
    {
        // Arrange
        var actor = CreateActor();
        var systemItem = new ProductType(null, "SYSTEM_TYPE", "System Type", isSystem: true);
        _store.ProductTypes.Add(systemItem);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(() => _sut.DeactivateProductTypeAsync(actor, systemItem.Id));
    }

    [Fact]
    public async Task DeactivateAndActivateProductType_WhenCustom_TogglesStatus()
    {
        // Arrange
        var actor = CreateActor();
        var custom = new ProductType(_orgId, "CUSTOM_TYPE", "Custom Type", isSystem: false);
        _store.ProductTypes.Add(custom);

        // Act - Deactivate
        await _sut.DeactivateProductTypeAsync(actor, custom.Id);
        Assert.False(custom.IsActive);

        // Act - Activate
        await _sut.ActivateProductTypeAsync(actor, custom.Id);
        Assert.True(custom.IsActive);
    }

    [Fact]
    public async Task CreateTarget_WithValidTargetType_CreatesTarget()
    {
        // Arrange
        var actor = CreateActor();
        var request = new CreateTargetRequest("T_APHID", "Special Aphid", "INSECT", "Aphid variant", 2);

        // Act
        var result = await _sut.CreateTargetAsync(actor, request);

        // Assert
        Assert.Equal("T_APHID", result.Code);
        Assert.Equal("Special Aphid", result.Name);
        Assert.Equal("Insect", result.TargetType);
        Assert.False(result.IsSystem);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task CreateTarget_WithInvalidTargetType_ThrowsValidationException()
    {
        // Arrange
        var actor = CreateActor();
        var request = new CreateTargetRequest("T_INVALID", "Invalid Type", "NON_EXISTENT", null, 0);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateTargetAsync(actor, request));
    }

    [Fact]
    public async Task UpdateTarget_WhenIsSystem_ThrowsForbiddenException()
    {
        // Arrange
        var actor = CreateActor();
        var systemTarget = new Target(null, "SYS_TARGET", "System Target", TargetType.Disease, isSystem: true);
        _store.Targets.Add(systemTarget);

        var request = new UpdateTargetRequest("New Name", "DISEASE", null, 0);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(() => _sut.UpdateTargetAsync(actor, systemTarget.Id, request));
    }

    [Fact]
    public async Task CreateApplicationMethod_WithValidData_CreatesApplicationMethod()
    {
        // Arrange
        var actor = CreateActor();
        var request = new CreateApplicationMethodRequest("DRONE_SPRAY", "Precision Drone", "Autonomous aerial spray", 1);

        // Act
        var result = await _sut.CreateApplicationMethodAsync(actor, request);

        // Assert
        Assert.Equal("DRONE_SPRAY", result.Code);
        Assert.Equal("Precision Drone", result.Name);
        Assert.False(result.IsSystem);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task UpdateApplicationMethod_WhenIsSystem_ThrowsForbiddenException()
    {
        // Arrange
        var actor = CreateActor();
        var systemMethod = new ApplicationMethod(null, "KNAPSACK", "Knapsack", isSystem: true);
        _store.ApplicationMethods.Add(systemMethod);

        var request = new UpdateApplicationMethodRequest("Updated", null, 0);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(() => _sut.UpdateApplicationMethodAsync(actor, systemMethod.Id, request));
    }

    private sealed class FakeMasterDataStore : IMasterDataStore
    {
        public List<Unit> Units { get; } = [];
        public List<FarmOwnershipType> FarmOwnershipTypes { get; } = [];
        public List<PlantationEndReason> PlantationEndReasons { get; } = [];
        public List<Currency> Currencies { get; } = [];
        public List<ProductType> ProductTypes { get; } = [];
        public List<Target> Targets { get; } = [];
        public List<ApplicationMethod> ApplicationMethods { get; } = [];

        public Task<IReadOnlyList<Unit>> ListUnitsAsync(Guid organizationId, string? category, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Unit>>(Units);

        public Task<IReadOnlyList<FarmOwnershipType>> ListFarmOwnershipTypesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FarmOwnershipType>>(FarmOwnershipTypes);

        public Task<IReadOnlyList<PlantationEndReason>> ListPlantationEndReasonsAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PlantationEndReason>>(PlantationEndReasons);

        public Task<IReadOnlyList<Currency>> ListCurrenciesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Currency>>(Currencies);

        public Task<IReadOnlyList<ProductType>> ListProductTypesAsync(Guid organizationId, bool includeInactive = false, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ProductType>>(includeInactive ? ProductTypes : ProductTypes.Where(x => x.IsActive).ToList());

        public Task<ProductType?> FindProductTypeByIdAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ProductTypes.FirstOrDefault(x => x.Id == id));

        public Task<bool> ProductTypeCodeExistsAsync(string code, Guid organizationId, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(ProductTypes.Any(x => x.Code == code.Trim().ToUpperInvariant() && (!excludeId.HasValue || x.Id != excludeId.Value)));

        public void AddProductType(ProductType productType) => ProductTypes.Add(productType);

        public Task<IReadOnlyList<Target>> ListTargetsAsync(Guid organizationId, TargetType? targetType = null, bool includeInactive = false, CancellationToken cancellationToken = default)
        {
            var query = includeInactive ? Targets.AsEnumerable() : Targets.Where(x => x.IsActive);
            if (targetType.HasValue)
            {
                query = query.Where(t => t.TargetType == targetType.Value);
            }
            return Task.FromResult<IReadOnlyList<Target>>(query.ToList());
        }

        public Task<Target?> FindTargetByIdAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Targets.FirstOrDefault(x => x.Id == id));

        public Task<bool> TargetCodeExistsAsync(string code, Guid organizationId, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(Targets.Any(x => x.Code == code.Trim().ToUpperInvariant() && (!excludeId.HasValue || x.Id != excludeId.Value)));

        public void AddTarget(Target target) => Targets.Add(target);

        public Task<IReadOnlyList<ApplicationMethod>> ListApplicationMethodsAsync(Guid organizationId, bool includeInactive = false, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ApplicationMethod>>(includeInactive ? ApplicationMethods : ApplicationMethods.Where(x => x.IsActive).ToList());

        public Task<ApplicationMethod?> FindApplicationMethodByIdAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ApplicationMethods.FirstOrDefault(x => x.Id == id));

        public Task<bool> ApplicationMethodCodeExistsAsync(string code, Guid organizationId, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(ApplicationMethods.Any(x => x.Code == code.Trim().ToUpperInvariant() && (!excludeId.HasValue || x.Id != excludeId.Value)));

        public void AddApplicationMethod(ApplicationMethod applicationMethod) => ApplicationMethods.Add(applicationMethod);

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }
}
