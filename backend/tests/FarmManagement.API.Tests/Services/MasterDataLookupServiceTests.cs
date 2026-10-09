using FarmManagement.Application.Interfaces;
using FarmManagement.Application.Services;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Services;

public class MasterDataLookupServiceTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly FakeMasterDataStore _store = new();
    private readonly MasterDataService _sut;

    public MasterDataLookupServiceTests()
    {
        _sut = new MasterDataService(_store);
    }

    private MasterDataActor CreateActor(Guid? orgId = null) =>
        new(_userId, orgId ?? _orgId, false);

    [Fact]
    public async Task ListProductTypes_ValidActor_ReturnsMappedResponses()
    {
        // Arrange
        var actor = CreateActor();
        _store.ProductTypes.AddRange([
            new ProductType(null, "HERB", "Herbicide", isSystem: true, displayOrder: 1),
            new ProductType(null, "FUNG", "Fungicide", isSystem: true, displayOrder: 2)
        ]);

        // Act
        var result = await _sut.ListProductTypesAsync(actor);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("HERB", result[0].Code);
        Assert.Equal("Herbicide", result[0].Name);
        Assert.True(result[0].IsSystem);
    }

    [Fact]
    public async Task ListTargets_WithTargetTypeFilter_FiltersCorrectly()
    {
        // Arrange
        var actor = CreateActor();
        _store.Targets.AddRange([
            new Target(null, "INSECT_1", "Aphids", TargetType.Insect, isSystem: true),
            new Target(null, "WEED_1", "Crabgrass", TargetType.Weed, isSystem: true)
        ]);

        // Act
        var result = await _sut.ListTargetsAsync(actor, "Insect");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Aphids", result[0].Name);
        Assert.Equal("Insect", result[0].TargetType);
    }

    [Fact]
    public async Task ListApplicationMethods_ValidActor_ReturnsMappedResponses()
    {
        // Arrange
        var actor = CreateActor();
        _store.ApplicationMethods.AddRange([
            new ApplicationMethod(null, "FOLIAR", "Foliar Spray", isSystem: true, displayOrder: 1),
            new ApplicationMethod(null, "DRIP", "Drip Chemigation", isSystem: true, displayOrder: 2)
        ]);

        // Act
        var result = await _sut.ListApplicationMethodsAsync(actor);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("FOLIAR", result[0].Code);
        Assert.Equal("Foliar Spray", result[0].Name);
    }

    [Fact]
    public async Task MasterData_InvalidActor_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var actor = new MasterDataActor(Guid.Empty, Guid.Empty);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.ListProductTypesAsync(actor));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.ListTargetsAsync(actor));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.ListApplicationMethodsAsync(actor));
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
