using System.Text.Json;
using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Expenses;
using FarmManagement.Application.Interfaces.Expenses;
using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Services;

public sealed class ExpenseCategoryService(IExpenseCategoryStore store) : IExpenseCategoryService
{
    private const int DefaultPageSize = 20;
    private const int MaximumPageSize = 100;

    public async Task<PagedResponse<ExpenseCategoryResponse>> ListAsync(
        ExpenseActor actor,
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

        var responses = items.Select(ToResponse).ToArray();
        return new PagedResponse<ExpenseCategoryResponse>(responses, page, pageSize, totalCount);
    }

    public async Task<ExpenseCategoryResponse> GetAsync(
        ExpenseActor actor,
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var category = await FindCategoryOrThrowAsync(actor, categoryId, cancellationToken);
        return ToResponse(category);
    }

    public async Task<ExpenseCategoryResponse> CreateAsync(
        ExpenseActor actor,
        CreateExpenseCategoryRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ValidateCreateRequest(request);

        var trimmedName = request.Name!.Trim();

        if (await store.NameExistsAsync(actor.OrganizationId, trimmedName, cancellationToken: cancellationToken))
        {
            throw new ConflictException("An expense category with this name already exists.");
        }

        var category = new ExpenseCategory(
            organizationId: actor.OrganizationId,
            name: trimmedName,
            code: null,
            isSystemDefault: false,
            description: NormalizeOptional(request.Description),
            createdBy: actor.UserId);

        store.Add(category);
        AddAudit(actor, category, "ExpenseCategory.Created", new { category.Name, category.Description }, ipAddress);

        await store.SaveChangesAsync(cancellationToken);
        return ToResponse(category);
    }

    public async Task<ExpenseCategoryResponse> UpdateAsync(
        ExpenseActor actor,
        Guid categoryId,
        UpdateExpenseCategoryRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ValidateUpdateRequest(request);

        var category = await FindCategoryOrThrowAsync(actor, categoryId, cancellationToken);

        if (category.IsSystemDefault)
        {
            throw new ForbiddenException("System default expense categories cannot be modified.");
        }

        var trimmedName = request.Name!.Trim();

        if (await store.NameExistsAsync(actor.OrganizationId, trimmedName, categoryId, cancellationToken))
        {
            throw new ConflictException("An expense category with this name already exists.");
        }

        var previous = new { category.Name, category.Description };

        category.Update(
            trimmedName,
            NormalizeOptional(request.Description),
            actor.UserId);

        AddAudit(
            actor,
            category,
            "ExpenseCategory.Updated",
            new { previous, current = new { category.Name, category.Description } },
            ipAddress);

        await store.SaveChangesAsync(cancellationToken);
        return ToResponse(category);
    }

    public Task<bool> ActivateAsync(
        ExpenseActor actor,
        Guid categoryId,
        string? ipAddress,
        CancellationToken cancellationToken = default) =>
        SetActiveAsync(actor, categoryId, true, ipAddress, cancellationToken);

    public Task<bool> DeactivateAsync(
        ExpenseActor actor,
        Guid categoryId,
        string? ipAddress,
        CancellationToken cancellationToken = default) =>
        SetActiveAsync(actor, categoryId, false, ipAddress, cancellationToken);

    private async Task<bool> SetActiveAsync(
        ExpenseActor actor,
        Guid categoryId,
        bool active,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        ValidateActor(actor);
        var category = await FindCategoryOrThrowAsync(actor, categoryId, cancellationToken);

        if (category.IsSystemDefault)
        {
            throw new ForbiddenException("System default expense categories cannot be deactivated.");
        }

        var changed = active
            ? category.Activate(actor.UserId)
            : category.Deactivate(actor.UserId);

        if (!changed)
        {
            return false;
        }

        AddAudit(actor, category, active ? "ExpenseCategory.Activated" : "ExpenseCategory.Deactivated", null, ipAddress);
        await store.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<ExpenseCategory> FindCategoryOrThrowAsync(
        ExpenseActor actor,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        if (categoryId == Guid.Empty)
        {
            throw new ResourceNotFoundException("The expense category was not found.");
        }

        return await store.FindAsync(categoryId, actor.OrganizationId, cancellationToken)
            ?? throw new ResourceNotFoundException("The expense category was not found.");
    }

    private void AddAudit(ExpenseActor actor, ExpenseCategory category, string action, object? details, string? ipAddress) =>
        store.AddAuditLog(new AuditLog(
            action,
            category.OrganizationId ?? actor.OrganizationId,
            actor.UserId,
            entityType: "ExpenseCategory",
            entityId: category.Id,
            details: details is null ? null : JsonSerializer.SerializeToDocument(details),
            ipAddress: ipAddress));

    private static ExpenseCategoryResponse ToResponse(ExpenseCategory category) =>
        new(
            category.Id,
            category.OrganizationId,
            category.Name,
            category.Code,
            category.Description,
            category.IsSystemDefault,
            category.IsActive,
            category.CreatedAt,
            category.CreatedBy,
            category.UpdatedAt,
            category.UpdatedBy);

    private static void ValidateCreateRequest(CreateExpenseCategoryRequest? request)
    {
        if (request is null)
        {
            throw Validation("request", "A request body is required.");
        }

        ValidateFields(request.Name, request.Description);
    }

    private static void ValidateUpdateRequest(UpdateExpenseCategoryRequest? request)
    {
        if (request is null)
        {
            throw Validation("request", "A request body is required.");
        }

        ValidateFields(request.Name, request.Description);
    }

    private static void ValidateFields(string? name, string? description)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(name))
        {
            errors["name"] = ["Category name is required."];
        }
        else if (name.Trim().Length > 100)
        {
            errors["name"] = ["Category name cannot exceed 100 characters."];
        }

        if (!string.IsNullOrWhiteSpace(description) && description.Trim().Length > 500)
        {
            errors["description"] = ["Description cannot exceed 500 characters."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException("Validation failed.", errors);
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void ValidateActor(ExpenseActor actor)
    {
        if (actor is null || actor.UserId == Guid.Empty || actor.OrganizationId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("The authenticated user context is invalid.");
        }
    }

    private static int NormalizePageSize(int pageSize) => pageSize switch
    {
        < 1 => DefaultPageSize,
        > MaximumPageSize => MaximumPageSize,
        _ => pageSize
    };

    private static ValidationException Validation(string propertyName, string message) =>
        new(message, new Dictionary<string, string[]>
        {
            [propertyName] = [message]
        });
}
