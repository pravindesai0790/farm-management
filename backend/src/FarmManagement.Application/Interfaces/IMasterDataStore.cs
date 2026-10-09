using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Interfaces;

public interface IMasterDataStore
{
    Task<IReadOnlyList<Unit>> ListUnitsAsync(Guid organizationId, string? category, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FarmOwnershipType>> ListFarmOwnershipTypesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PlantationEndReason>> ListPlantationEndReasonsAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Currency>> ListCurrenciesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductType>> ListProductTypesAsync(Guid organizationId, bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<ProductType?> FindProductTypeByIdAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default);
    Task<bool> ProductTypeCodeExistsAsync(string code, Guid organizationId, Guid? excludeId = null, CancellationToken cancellationToken = default);
    void AddProductType(ProductType productType);

    Task<IReadOnlyList<Target>> ListTargetsAsync(Guid organizationId, Domain.Enums.TargetType? targetType = null, bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<Target?> FindTargetByIdAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default);
    Task<bool> TargetCodeExistsAsync(string code, Guid organizationId, Guid? excludeId = null, CancellationToken cancellationToken = default);
    void AddTarget(Target target);

    Task<IReadOnlyList<ApplicationMethod>> ListApplicationMethodsAsync(Guid organizationId, bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<ApplicationMethod?> FindApplicationMethodByIdAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default);
    Task<bool> ApplicationMethodCodeExistsAsync(string code, Guid organizationId, Guid? excludeId = null, CancellationToken cancellationToken = default);
    void AddApplicationMethod(ApplicationMethod applicationMethod);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

