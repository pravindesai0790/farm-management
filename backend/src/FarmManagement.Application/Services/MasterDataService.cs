using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.MasterData;
using FarmManagement.Application.Interfaces;
using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Services;

public sealed class MasterDataService(IMasterDataStore store) : IMasterDataService
{
    public async Task<IReadOnlyList<UnitResponse>> ListUnitsAsync(MasterDataActor actor, string? category, CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        return (await store.ListUnitsAsync(actor.OrganizationId, category, cancellationToken))
            .Select(unit => new UnitResponse(unit.Id, unit.Code, unit.Name, unit.Symbol, unit.UnitCategory.ToString().ToUpperInvariant(), unit.IsSystem, unit.IsActive))
            .ToArray();
    }

    public async Task<IReadOnlyList<FarmOwnershipTypeResponse>> ListFarmOwnershipTypesAsync(MasterDataActor actor, CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        return (await store.ListFarmOwnershipTypesAsync(cancellationToken))
            .Select(type => new FarmOwnershipTypeResponse(type.Id, type.Code, type.Name, type.IsSystem, type.IsActive))
            .ToArray();
    }

    public async Task<IReadOnlyList<PlantationEndReasonResponse>> ListPlantationEndReasonsAsync(MasterDataActor actor, CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        return (await store.ListPlantationEndReasonsAsync(actor.OrganizationId, cancellationToken))
            .Select(reason => new PlantationEndReasonResponse(reason.Id, reason.Code, reason.Name, reason.Description, reason.IsSystem, reason.IsActive))
            .ToArray();
    }

    public Task<IReadOnlyList<PlantationEndReasonResponse>> ListCycleCancellationReasonsAsync(MasterDataActor actor, CancellationToken cancellationToken = default) =>
        ListPlantationEndReasonsAsync(actor, cancellationToken);

    public async Task<IReadOnlyList<CurrencyResponse>> ListCurrenciesAsync(MasterDataActor actor, CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        return (await store.ListCurrenciesAsync(cancellationToken))
            .Select(c => new CurrencyResponse(c.Id, c.Code, c.Name, c.Symbol, c.IsSystem, c.IsActive, c.DisplayOrder))
            .ToArray();
    }

    public async Task<IReadOnlyList<ProductTypeResponse>> ListProductTypesAsync(MasterDataActor actor, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        return (await store.ListProductTypesAsync(actor.OrganizationId, includeInactive, cancellationToken))
            .Select(pt => new ProductTypeResponse(pt.Id, pt.Code, pt.Name, pt.Description, pt.DisplayOrder, pt.IsSystem, pt.IsActive))
            .ToArray();
    }

    public async Task<ProductTypeResponse> CreateProductTypeAsync(MasterDataActor actor, CreateProductTypeRequest request, CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        if (string.IsNullOrWhiteSpace(request.Code))
            throw Validation("code", "Product type code is required.");
        if (string.IsNullOrWhiteSpace(request.Name))
            throw Validation("name", "Product type name is required.");
        if (request.DisplayOrder < 0)
            throw Validation("displayOrder", "Display order cannot be negative.");

        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        if (await store.ProductTypeCodeExistsAsync(normalizedCode, actor.OrganizationId, cancellationToken: cancellationToken))
            throw new ConflictException("A product type with this code already exists.");

        var productType = new ProductType(
            organizationId: actor.OrganizationId,
            code: normalizedCode,
            name: request.Name.Trim(),
            isSystem: false,
            description: string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            displayOrder: request.DisplayOrder,
            createdBy: actor.UserId);

        store.AddProductType(productType);
        await store.SaveChangesAsync(cancellationToken);
        return new ProductTypeResponse(productType.Id, productType.Code, productType.Name, productType.Description, productType.DisplayOrder, productType.IsSystem, productType.IsActive);
    }

    public async Task<ProductTypeResponse> UpdateProductTypeAsync(MasterDataActor actor, Guid id, UpdateProductTypeRequest request, CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        if (string.IsNullOrWhiteSpace(request.Name))
            throw Validation("name", "Product type name is required.");
        if (request.DisplayOrder < 0)
            throw Validation("displayOrder", "Display order cannot be negative.");

        var productType = await store.FindProductTypeByIdAsync(id, actor.OrganizationId, cancellationToken)
            ?? throw new ResourceNotFoundException("The product type was not found.");

        if (productType.IsSystem)
            throw new ForbiddenException("System product types cannot be modified.");

        productType.Update(request.Name.Trim(), request.Description, request.DisplayOrder, DateTimeOffset.UtcNow, actor.UserId);
        await store.SaveChangesAsync(cancellationToken);
        return new ProductTypeResponse(productType.Id, productType.Code, productType.Name, productType.Description, productType.DisplayOrder, productType.IsSystem, productType.IsActive);
    }

    public async Task ActivateProductTypeAsync(MasterDataActor actor, Guid id, CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var productType = await store.FindProductTypeByIdAsync(id, actor.OrganizationId, cancellationToken)
            ?? throw new ResourceNotFoundException("The product type was not found.");

        if (productType.IsSystem)
            throw new ForbiddenException("System product types cannot be modified.");

        productType.Activate(DateTimeOffset.UtcNow, actor.UserId);
        await store.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateProductTypeAsync(MasterDataActor actor, Guid id, CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var productType = await store.FindProductTypeByIdAsync(id, actor.OrganizationId, cancellationToken)
            ?? throw new ResourceNotFoundException("The product type was not found.");

        if (productType.IsSystem)
            throw new ForbiddenException("System product types cannot be deactivated.");

        productType.Deactivate(DateTimeOffset.UtcNow, actor.UserId);
        await store.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TargetResponse>> ListTargetsAsync(MasterDataActor actor, string? type = null, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);

        Domain.Enums.TargetType? parsedTargetType = null;
        if (!string.IsNullOrWhiteSpace(type))
        {
            if (Enum.TryParse<Domain.Enums.TargetType>(type.Trim(), true, out var targetType))
            {
                parsedTargetType = targetType;
            }
        }

        return (await store.ListTargetsAsync(actor.OrganizationId, parsedTargetType, includeInactive, cancellationToken))
            .Select(t => new TargetResponse(t.Id, t.Code, t.Name, t.TargetType.ToString(), t.Description, t.DisplayOrder, t.IsSystem, t.IsActive))
            .ToArray();
    }

    public async Task<TargetResponse> CreateTargetAsync(MasterDataActor actor, CreateTargetRequest request, CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        if (string.IsNullOrWhiteSpace(request.Code))
            throw Validation("code", "Target code is required.");
        if (string.IsNullOrWhiteSpace(request.Name))
            throw Validation("name", "Target name is required.");
        if (!Enum.TryParse<Domain.Enums.TargetType>(request.TargetType?.Trim(), true, out var targetType))
            throw Validation("targetType", "Invalid target type.");
        if (request.DisplayOrder < 0)
            throw Validation("displayOrder", "Display order cannot be negative.");

        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        if (await store.TargetCodeExistsAsync(normalizedCode, actor.OrganizationId, cancellationToken: cancellationToken))
            throw new ConflictException("A target with this code already exists.");

        var target = new Target(
            organizationId: actor.OrganizationId,
            code: normalizedCode,
            name: request.Name.Trim(),
            targetType: targetType,
            isSystem: false,
            description: string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            displayOrder: request.DisplayOrder,
            createdBy: actor.UserId);

        store.AddTarget(target);
        await store.SaveChangesAsync(cancellationToken);
        return new TargetResponse(target.Id, target.Code, target.Name, target.TargetType.ToString(), target.Description, target.DisplayOrder, target.IsSystem, target.IsActive);
    }

    public async Task<TargetResponse> UpdateTargetAsync(MasterDataActor actor, Guid id, UpdateTargetRequest request, CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        if (string.IsNullOrWhiteSpace(request.Name))
            throw Validation("name", "Target name is required.");
        if (!Enum.TryParse<Domain.Enums.TargetType>(request.TargetType?.Trim(), true, out var targetType))
            throw Validation("targetType", "Invalid target type.");
        if (request.DisplayOrder < 0)
            throw Validation("displayOrder", "Display order cannot be negative.");

        var target = await store.FindTargetByIdAsync(id, actor.OrganizationId, cancellationToken)
            ?? throw new ResourceNotFoundException("The target was not found.");

        if (target.IsSystem)
            throw new ForbiddenException("System targets cannot be modified.");

        target.Update(request.Name.Trim(), targetType, request.Description, request.DisplayOrder, DateTimeOffset.UtcNow, actor.UserId);
        await store.SaveChangesAsync(cancellationToken);
        return new TargetResponse(target.Id, target.Code, target.Name, target.TargetType.ToString(), target.Description, target.DisplayOrder, target.IsSystem, target.IsActive);
    }

    public async Task ActivateTargetAsync(MasterDataActor actor, Guid id, CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var target = await store.FindTargetByIdAsync(id, actor.OrganizationId, cancellationToken)
            ?? throw new ResourceNotFoundException("The target was not found.");

        if (target.IsSystem)
            throw new ForbiddenException("System targets cannot be modified.");

        target.Activate(DateTimeOffset.UtcNow, actor.UserId);
        await store.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateTargetAsync(MasterDataActor actor, Guid id, CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var target = await store.FindTargetByIdAsync(id, actor.OrganizationId, cancellationToken)
            ?? throw new ResourceNotFoundException("The target was not found.");

        if (target.IsSystem)
            throw new ForbiddenException("System targets cannot be deactivated.");

        target.Deactivate(DateTimeOffset.UtcNow, actor.UserId);
        await store.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ApplicationMethodResponse>> ListApplicationMethodsAsync(MasterDataActor actor, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        return (await store.ListApplicationMethodsAsync(actor.OrganizationId, includeInactive, cancellationToken))
            .Select(am => new ApplicationMethodResponse(am.Id, am.Code, am.Name, am.Description, am.DisplayOrder, am.IsSystem, am.IsActive))
            .ToArray();
    }

    public async Task<ApplicationMethodResponse> CreateApplicationMethodAsync(MasterDataActor actor, CreateApplicationMethodRequest request, CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        if (string.IsNullOrWhiteSpace(request.Code))
            throw Validation("code", "Application method code is required.");
        if (string.IsNullOrWhiteSpace(request.Name))
            throw Validation("name", "Application method name is required.");
        if (request.DisplayOrder < 0)
            throw Validation("displayOrder", "Display order cannot be negative.");

        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        if (await store.ApplicationMethodCodeExistsAsync(normalizedCode, actor.OrganizationId, cancellationToken: cancellationToken))
            throw new ConflictException("An application method with this code already exists.");

        var method = new ApplicationMethod(
            organizationId: actor.OrganizationId,
            code: normalizedCode,
            name: request.Name.Trim(),
            isSystem: false,
            description: string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            displayOrder: request.DisplayOrder,
            createdBy: actor.UserId);

        store.AddApplicationMethod(method);
        await store.SaveChangesAsync(cancellationToken);
        return new ApplicationMethodResponse(method.Id, method.Code, method.Name, method.Description, method.DisplayOrder, method.IsSystem, method.IsActive);
    }

    public async Task<ApplicationMethodResponse> UpdateApplicationMethodAsync(MasterDataActor actor, Guid id, UpdateApplicationMethodRequest request, CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        if (string.IsNullOrWhiteSpace(request.Name))
            throw Validation("name", "Application method name is required.");
        if (request.DisplayOrder < 0)
            throw Validation("displayOrder", "Display order cannot be negative.");

        var method = await store.FindApplicationMethodByIdAsync(id, actor.OrganizationId, cancellationToken)
            ?? throw new ResourceNotFoundException("The application method was not found.");

        if (method.IsSystem)
            throw new ForbiddenException("System application methods cannot be modified.");

        method.Update(request.Name.Trim(), request.Description, request.DisplayOrder, DateTimeOffset.UtcNow, actor.UserId);
        await store.SaveChangesAsync(cancellationToken);
        return new ApplicationMethodResponse(method.Id, method.Code, method.Name, method.Description, method.DisplayOrder, method.IsSystem, method.IsActive);
    }

    public async Task ActivateApplicationMethodAsync(MasterDataActor actor, Guid id, CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var method = await store.FindApplicationMethodByIdAsync(id, actor.OrganizationId, cancellationToken)
            ?? throw new ResourceNotFoundException("The application method was not found.");

        if (method.IsSystem)
            throw new ForbiddenException("System application methods cannot be modified.");

        method.Activate(DateTimeOffset.UtcNow, actor.UserId);
        await store.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateApplicationMethodAsync(MasterDataActor actor, Guid id, CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var method = await store.FindApplicationMethodByIdAsync(id, actor.OrganizationId, cancellationToken)
            ?? throw new ResourceNotFoundException("The application method was not found.");

        if (method.IsSystem)
            throw new ForbiddenException("System application methods cannot be deactivated.");

        method.Deactivate(DateTimeOffset.UtcNow, actor.UserId);
        await store.SaveChangesAsync(cancellationToken);
    }

    private static ValidationException Validation(string propertyName, string message) =>
        new("Validation failed.", new Dictionary<string, string[]> { [propertyName] = [message] });

    private static void ValidateActor(MasterDataActor actor)
    {
        if (actor.UserId == Guid.Empty || actor.OrganizationId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("The access token does not contain a valid user scope.");
        }
    }
}
