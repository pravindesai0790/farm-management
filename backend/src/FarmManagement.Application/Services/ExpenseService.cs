using System.Text.Json;
using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Expenses;
using FarmManagement.Application.Interfaces.Expenses;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.Services;

public sealed class ExpenseService(IExpenseStore store) : IExpenseService
{
    private const int DefaultPageSize = 20;
    private const int MaximumPageSize = 100;

    public async Task<PagedResponse<ExpenseResponse>> ListAsync(
        ExpenseActor actor,
        ExpenseFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        if (page < 1)
        {
            throw Validation("page", "Page must be at least 1.");
        }

        pageSize = NormalizePageSize(pageSize);

        var totalCount = await store.CountAsync(actor.OrganizationId, filter, cancellationToken);
        var items = await store.ListAsync(
            actor.OrganizationId,
            filter,
            checked((page - 1) * pageSize),
            pageSize,
            cancellationToken);

        var responses = items.Select(ToResponse).ToArray();
        return new PagedResponse<ExpenseResponse>(responses, page, pageSize, totalCount);
    }

    public async Task<ExpenseResponse> GetAsync(
        ExpenseActor actor,
        Guid expenseId,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var expense = await FindExpenseOrThrowAsync(actor, expenseId, cancellationToken);
        return ToResponse(expense);
    }

    public async Task<ExpenseResponse> CreateDraftAsync(
        ExpenseActor actor,
        CreateExpenseRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ValidateCreateRequest(request);

        await ValidateReferencesAsync(
            actor.OrganizationId,
            request.FarmId,
            request.ExpenseCategoryId,
            request.CurrencyId,
            request.SupplierId,
            request.FarmAreaId,
            request.PlantationId,
            request.CropCycleId,
            request.CropCycleStageId,
            cancellationToken);

        var expense = Expense.CreateDraft(
            actor.OrganizationId,
            request.FarmId,
            request.ExpenseCategoryId,
            request.ExpenseDate,
            request.Description!.Trim(),
            request.Amount,
            request.CurrencyId,
            actor.UserId,
            supplierId: request.SupplierId,
            referenceNumber: request.ReferenceNumber,
            farmAreaId: request.FarmAreaId,
            plantationId: request.PlantationId,
            cropCycleId: request.CropCycleId,
            cropCycleStageId: request.CropCycleStageId,
            attachmentReference: request.AttachmentReference);

        store.Add(expense);
        AddAudit(actor, expense, "Expense.Created", new
        {
            expense.Id,
            expense.FarmId,
            expense.ExpenseCategoryId,
            expense.Amount,
            expense.CurrencyId,
            Status = expense.Status.ToString()
        }, ipAddress);

        await store.SaveChangesAsync(cancellationToken);

        var created = await store.FindAsync(expense.Id, actor.OrganizationId, cancellationToken) ?? expense;
        return ToResponse(created);
    }

    public async Task<ExpenseResponse> UpdateDraftAsync(
        ExpenseActor actor,
        Guid expenseId,
        UpdateExpenseRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ValidateUpdateRequest(request);

        var expense = await FindExpenseOrThrowAsync(actor, expenseId, cancellationToken);

        if (expense.Status != ExpenseStatus.Draft)
        {
            throw Validation("status", $"Only draft expenses can be modified. Current status: {expense.Status}.");
        }

        await ValidateReferencesAsync(
            actor.OrganizationId,
            request.FarmId,
            request.ExpenseCategoryId,
            request.CurrencyId,
            request.SupplierId,
            request.FarmAreaId,
            request.PlantationId,
            request.CropCycleId,
            request.CropCycleStageId,
            cancellationToken);

        var previous = new
        {
            expense.FarmId,
            expense.ExpenseCategoryId,
            expense.ExpenseDate,
            expense.Description,
            expense.Amount,
            expense.CurrencyId,
            expense.SupplierId,
            expense.ReferenceNumber
        };

        expense.UpdateDraft(
            request.FarmId,
            request.ExpenseCategoryId,
            request.ExpenseDate,
            request.Description!.Trim(),
            request.Amount,
            request.CurrencyId,
            actor.UserId,
            supplierId: request.SupplierId,
            referenceNumber: request.ReferenceNumber,
            farmAreaId: request.FarmAreaId,
            plantationId: request.PlantationId,
            cropCycleId: request.CropCycleId,
            cropCycleStageId: request.CropCycleStageId,
            attachmentReference: request.AttachmentReference);

        AddAudit(actor, expense, "Expense.Updated", new
        {
            previous,
            current = new
            {
                expense.FarmId,
                expense.ExpenseCategoryId,
                expense.ExpenseDate,
                expense.Description,
                expense.Amount,
                expense.CurrencyId,
                expense.SupplierId,
                expense.ReferenceNumber
            }
        }, ipAddress);

        await store.SaveChangesAsync(cancellationToken);

        var updated = await store.FindAsync(expense.Id, actor.OrganizationId, cancellationToken) ?? expense;
        return ToResponse(updated);
    }

    public async Task<ExpenseResponse> PostAsync(
        ExpenseActor actor,
        Guid expenseId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var expense = await FindExpenseOrThrowAsync(actor, expenseId, cancellationToken);

        if (expense.Status != ExpenseStatus.Draft)
        {
            throw Validation("status", $"Only draft expenses can be posted. Current status: {expense.Status}.");
        }

        expense.Post(actor.UserId);
        AddAudit(actor, expense, "Expense.Posted", new { expense.Id, expense.Amount, expense.CurrencyId }, ipAddress);

        await store.SaveChangesAsync(cancellationToken);

        var posted = await store.FindAsync(expense.Id, actor.OrganizationId, cancellationToken) ?? expense;
        return ToResponse(posted);
    }

    public async Task<ExpenseResponse> ReverseAsync(
        ExpenseActor actor,
        Guid expenseId,
        ReverseExpenseRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var expense = await FindExpenseOrThrowAsync(actor, expenseId, cancellationToken);

        if (expense.Status != ExpenseStatus.Posted)
        {
            throw Validation("status", $"Only posted expenses can be reversed. Current status: {expense.Status}.");
        }

        if (request is null || string.IsNullOrWhiteSpace(request.Reason))
        {
            throw Validation("reason", "A reversal reason is required.");
        }

        if (request.Reason.Trim().Length > 500)
        {
            throw Validation("reason", "Reversal reason cannot exceed 500 characters.");
        }

        expense.Reverse(request.Reason.Trim(), actor.UserId);
        AddAudit(actor, expense, "Expense.Reversed", new
        {
            expense.Id,
            expense.Amount,
            Reason = expense.ReversalReason
        }, ipAddress);

        await store.SaveChangesAsync(cancellationToken);

        var reversed = await store.FindAsync(expense.Id, actor.OrganizationId, cancellationToken) ?? expense;
        return ToResponse(reversed);
    }

    private async Task ValidateReferencesAsync(
        Guid organizationId,
        Guid farmId,
        Guid categoryId,
        Guid currencyId,
        Guid? supplierId,
        Guid? farmAreaId,
        Guid? plantationId,
        Guid? cropCycleId,
        Guid? cropCycleStageId,
        CancellationToken cancellationToken)
    {
        if (!await store.FarmBelongsToOrganizationAsync(farmId, organizationId, cancellationToken))
        {
            throw Validation("farmId", "The selected farm was not found in your organization.");
        }

        if (!await store.CategoryExistsAndActiveAsync(categoryId, organizationId, cancellationToken))
        {
            throw Validation("expenseCategoryId", "The selected expense category was not found or is inactive.");
        }

        if (!await store.CurrencyExistsAndActiveAsync(currencyId, cancellationToken))
        {
            throw Validation("currencyId", "The selected currency was not found or is inactive.");
        }

        if (supplierId.HasValue && !await store.SupplierBelongsToOrganizationAndActiveAsync(supplierId.Value, organizationId, cancellationToken))
        {
            throw Validation("supplierId", "The selected supplier was not found or is inactive.");
        }

        if (farmAreaId.HasValue && !await store.AreaBelongsToFarmAsync(farmAreaId.Value, farmId, organizationId, cancellationToken))
        {
            throw Validation("farmAreaId", "The selected farm area does not belong to the selected farm.");
        }

        if (plantationId.HasValue && !await store.PlantationBelongsToFarmAsync(plantationId.Value, farmId, organizationId, cancellationToken))
        {
            throw Validation("plantationId", "The selected plantation does not belong to the selected farm.");
        }

        if (cropCycleId.HasValue && !await store.CropCycleBelongsToFarmAsync(cropCycleId.Value, farmId, organizationId, cancellationToken))
        {
            throw Validation("cropCycleId", "The selected crop cycle does not belong to the selected farm.");
        }

        if (cropCycleStageId.HasValue)
        {
            if (!cropCycleId.HasValue)
            {
                throw Validation("cropCycleStageId", "A crop cycle stage requires a crop cycle to be selected.");
            }

            if (!await store.StageBelongsToCropCycleAsync(cropCycleStageId.Value, cropCycleId.Value, organizationId, cancellationToken))
            {
                throw Validation("cropCycleStageId", "The selected crop cycle stage does not belong to the selected crop cycle.");
            }
        }
    }

    private async Task<Expense> FindExpenseOrThrowAsync(ExpenseActor actor, Guid expenseId, CancellationToken cancellationToken)
    {
        if (expenseId == Guid.Empty)
        {
            throw new ResourceNotFoundException("The expense was not found.");
        }

        return await store.FindAsync(expenseId, actor.OrganizationId, cancellationToken)
            ?? throw new ResourceNotFoundException("The expense was not found.");
    }

    private void AddAudit(ExpenseActor actor, Expense expense, string action, object? details, string? ipAddress) =>
        store.AddAuditLog(new AuditLog(
            action,
            expense.OrganizationId,
            actor.UserId,
            entityType: "Expense",
            entityId: expense.Id,
            details: details is null ? null : JsonSerializer.SerializeToDocument(details),
            ipAddress: ipAddress));

    private static ExpenseResponse ToResponse(Expense expense) =>
        new(
            expense.Id,
            expense.OrganizationId,
            expense.FarmId,
            expense.Farm?.Name ?? string.Empty,
            expense.ExpenseCategoryId,
            expense.ExpenseCategory?.Name ?? string.Empty,
            expense.ExpenseDate,
            expense.Description,
            expense.Amount,
            expense.CurrencyId,
            expense.Currency?.Code ?? string.Empty,
            expense.Currency?.Symbol ?? string.Empty,
            expense.SupplierId,
            expense.Supplier?.Name,
            expense.ReferenceNumber,
            expense.FarmAreaId,
            expense.FarmArea?.Name,
            expense.PlantationId,
            expense.Plantation?.PlantationName,
            expense.CropCycleId,
            expense.CropCycle?.CycleName,
            expense.CropCycleStageId,
            expense.CropCycleStage?.StageName,
            expense.AttachmentReference,
            expense.Status,
            expense.PostedAt,
            expense.PostedBy,
            expense.ReversedAt,
            expense.ReversedBy,
            expense.ReversalReason,
            expense.CreatedAt,
            expense.CreatedBy,
            expense.UpdatedAt,
            expense.UpdatedBy);

    private static void ValidateCreateRequest(CreateExpenseRequest? request)
    {
        if (request is null) throw Validation("request", "A request body is required.");
        ValidateFields(request.ExpenseDate, request.Description, request.Amount, request.ReferenceNumber, request.AttachmentReference);
    }

    private static void ValidateUpdateRequest(UpdateExpenseRequest? request)
    {
        if (request is null) throw Validation("request", "A request body is required.");
        ValidateFields(request.ExpenseDate, request.Description, request.Amount, request.ReferenceNumber, request.AttachmentReference);
    }

    private static void ValidateFields(DateOnly expenseDate, string? description, decimal amount, string? referenceNumber, string? attachmentReference)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (expenseDate > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            errors["expenseDate"] = ["Expense date cannot be in the future."];
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            errors["description"] = ["Expense description is required."];
        }
        else if (description.Trim().Length > 500)
        {
            errors["description"] = ["Description cannot exceed 500 characters."];
        }

        if (amount <= 0m)
        {
            errors["amount"] = ["Expense amount must be greater than zero."];
        }

        if (!string.IsNullOrWhiteSpace(referenceNumber) && referenceNumber.Trim().Length > 100)
        {
            errors["referenceNumber"] = ["Reference number cannot exceed 100 characters."];
        }

        if (!string.IsNullOrWhiteSpace(attachmentReference) && attachmentReference.Trim().Length > 500)
        {
            errors["attachmentReference"] = ["Attachment reference cannot exceed 500 characters."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException("Validation failed.", errors);
        }
    }

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
