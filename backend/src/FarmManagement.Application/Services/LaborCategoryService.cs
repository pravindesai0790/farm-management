using System.Text.Json;
using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Labor;
using FarmManagement.Application.Interfaces.Labor;
using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Services;

public sealed class LaborCategoryService(ILaborCategoryStore store) : ILaborCategoryService
{
    private const int DefaultPageSize = 20;
    private const int MaximumPageSize = 100;

    public async Task<PagedResponse<LaborCategoryResponse>> ListAsync(
        LaborCategoryActor actor,
        int page,
        int pageSize,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        if (page < 1)
        {
            throw Validation("page", "Page must be at least 1.");
        }

        pageSize = NormalizePageSize(pageSize);
        var normalizedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        var totalCount = await store.CountAsync(actor.OrganizationId, normalizedSearch, isActive, cancellationToken);
        var items = await store.ListAsync(
            actor.OrganizationId,
            checked((page - 1) * pageSize),
            pageSize,
            normalizedSearch,
            isActive,
            cancellationToken);

        var responses = items.Select(x => ToResponse(x.Category, x.WorkerCount)).ToArray();
        return new PagedResponse<LaborCategoryResponse>(responses, page, pageSize, totalCount);
    }

    public async Task<LaborCategoryResponse> GetAsync(
        LaborCategoryActor actor,
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var category = await FindCategoryOrThrowAsync(actor, categoryId, cancellationToken);
        var workerCount = await store.GetWorkerCountAsync(category.Id, actor.OrganizationId, cancellationToken);
        return ToResponse(category, workerCount);
    }

    public async Task<LaborCategoryResponse> CreateAsync(
        LaborCategoryActor actor,
        CreateLaborCategoryRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ValidateCreateRequest(request);

        var trimmedName = request.Name!.Trim();

        if (await store.NameExistsAsync(actor.OrganizationId, trimmedName, cancellationToken: cancellationToken))
        {
            throw new ConflictException("A labor category with this name already exists.");
        }

        var category = new LaborCategory(
            actor.OrganizationId,
            trimmedName,
            isSystem: false,
            description: NormalizeOptional(request.Description),
            createdBy: actor.UserId);

        store.Add(category);
        AddAudit(actor, category, "LaborCategory.Created", new { category.Name, category.Description }, ipAddress);

        await store.SaveChangesAsync(cancellationToken);
        return ToResponse(category, 0);
    }

    public async Task<LaborCategoryResponse> UpdateAsync(
        LaborCategoryActor actor,
        Guid categoryId,
        UpdateLaborCategoryRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ValidateUpdateRequest(request);

        var category = await FindCategoryOrThrowAsync(actor, categoryId, cancellationToken);

        if (category.IsSystem)
        {
            throw new ForbiddenException("System labor categories cannot be modified.");
        }

        var trimmedName = request.Name!.Trim();

        if (await store.NameExistsAsync(actor.OrganizationId, trimmedName, category.Id, cancellationToken))
        {
            throw new ConflictException("A labor category with this name already exists.");
        }

        var previous = new { category.Name, category.Description };

        category.Update(
            trimmedName,
            NormalizeOptional(request.Description),
            DateTimeOffset.UtcNow,
            actor.UserId);

        AddAudit(
            actor,
            category,
            "LaborCategory.Updated",
            new { previous, current = new { category.Name, category.Description } },
            ipAddress);

        await store.SaveChangesAsync(cancellationToken);
        var workerCount = await store.GetWorkerCountAsync(category.Id, actor.OrganizationId, cancellationToken);
        return ToResponse(category, workerCount);
    }

    public Task<bool> ActivateAsync(
        LaborCategoryActor actor,
        Guid categoryId,
        string? ipAddress,
        CancellationToken cancellationToken = default) =>
        SetActiveAsync(actor, categoryId, true, ipAddress, cancellationToken);

    public Task<bool> DeactivateAsync(
        LaborCategoryActor actor,
        Guid categoryId,
        string? ipAddress,
        CancellationToken cancellationToken = default) =>
        SetActiveAsync(actor, categoryId, false, ipAddress, cancellationToken);

    private async Task<bool> SetActiveAsync(
        LaborCategoryActor actor,
        Guid categoryId,
        bool active,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        ValidateActor(actor);
        var category = await FindCategoryOrThrowAsync(actor, categoryId, cancellationToken);

        if (category.IsSystem)
        {
            throw new ForbiddenException("System labor categories cannot be modified.");
        }

        var changed = active
            ? category.Activate(DateTimeOffset.UtcNow, actor.UserId)
            : category.Deactivate(DateTimeOffset.UtcNow, actor.UserId);

        if (!changed)
        {
            return false;
        }

        AddAudit(actor, category, active ? "LaborCategory.Activated" : "LaborCategory.Deactivated", null, ipAddress);
        await store.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<LaborCategory> FindCategoryOrThrowAsync(LaborCategoryActor actor, Guid categoryId, CancellationToken cancellationToken)
    {
        if (categoryId == Guid.Empty)
        {
            throw new ResourceNotFoundException("The labor category was not found.");
        }

        return await store.FindAsync(categoryId, actor.OrganizationId, cancellationToken)
            ?? throw new ResourceNotFoundException("The labor category was not found.");
    }

    private void AddAudit(LaborCategoryActor actor, LaborCategory category, string action, object? details, string? ipAddress) =>
        store.AddAuditLog(new AuditLog(
            action,
            category.OrganizationId,
            actor.UserId,
            entityType: "LaborCategory",
            entityId: category.Id,
            details: details is null ? null : JsonSerializer.SerializeToDocument(details),
            ipAddress: ipAddress));

    private static LaborCategoryResponse ToResponse(LaborCategory category, int workerCount) =>
        new(
            category.Id,
            category.OrganizationId,
            category.Name,
            category.Description,
            category.IsSystem,
            category.IsActive,
            workerCount,
            category.CreatedAt,
            category.CreatedBy,
            category.UpdatedAt,
            category.UpdatedBy);

    private static void ValidateCreateRequest(CreateLaborCategoryRequest? request)
    {
        if (request is null)
        {
            throw Validation("request", "A request body is required.");
        }

        ValidateFields(request.Name);
    }

    private static void ValidateUpdateRequest(UpdateLaborCategoryRequest? request)
    {
        if (request is null)
        {
            throw Validation("request", "A request body is required.");
        }

        ValidateFields(request.Name);
    }

    private static void ValidateFields(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw Validation("name", "Labor category name is required.");
        }

        if (name.Trim().Length > 150)
        {
            throw Validation("name", "Labor category name cannot exceed 150 characters.");
        }
    }

    private static int NormalizePageSize(int pageSize) =>
        pageSize == 0 ? DefaultPageSize : pageSize is < 1 or > MaximumPageSize
            ? throw Validation("pageSize", $"Page size must be between 1 and {MaximumPageSize}.")
            : pageSize;

    private static void ValidateActor(LaborCategoryActor actor)
    {
        if (actor.UserId == Guid.Empty || actor.OrganizationId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("The access token does not contain a valid user scope.");
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ValidationException Validation(string propertyName, string message) =>
        new("Validation failed.", new Dictionary<string, string[]> { [propertyName] = [message] });
}
