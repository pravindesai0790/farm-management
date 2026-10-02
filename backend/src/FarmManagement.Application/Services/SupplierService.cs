using System.Net.Mail;
using System.Text.Json;
using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Expenses;
using FarmManagement.Application.Interfaces.Expenses;
using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Services;

public sealed class SupplierService(ISupplierStore store) : ISupplierService
{
    private const int DefaultPageSize = 20;
    private const int MaximumPageSize = 100;

    public async Task<PagedResponse<SupplierResponse>> ListAsync(
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
        return new PagedResponse<SupplierResponse>(responses, page, pageSize, totalCount);
    }

    public async Task<SupplierResponse> GetAsync(
        ExpenseActor actor,
        Guid supplierId,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var supplier = await FindSupplierOrThrowAsync(actor, supplierId, cancellationToken);
        return ToResponse(supplier);
    }

    public async Task<SupplierResponse> CreateAsync(
        ExpenseActor actor,
        CreateSupplierRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ValidateCreateRequest(request);

        var trimmedName = request.Name!.Trim();

        if (await store.NameExistsAsync(actor.OrganizationId, trimmedName, cancellationToken: cancellationToken))
        {
            throw new ConflictException("A supplier with this name already exists.");
        }

        var supplier = Supplier.Create(
            actor.OrganizationId,
            trimmedName,
            actor.UserId,
            contactPerson: request.ContactPerson,
            phone: request.Phone,
            email: request.Email,
            address: request.Address,
            registrationIdentifier: request.RegistrationIdentifier,
            notes: request.Notes);

        store.Add(supplier);
        AddAudit(actor, supplier, "Supplier.Created", new { supplier.Name, supplier.Email, supplier.Phone }, ipAddress);

        await store.SaveChangesAsync(cancellationToken);
        return ToResponse(supplier);
    }

    public async Task<SupplierResponse> UpdateAsync(
        ExpenseActor actor,
        Guid supplierId,
        UpdateSupplierRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ValidateUpdateRequest(request);

        var supplier = await FindSupplierOrThrowAsync(actor, supplierId, cancellationToken);
        var trimmedName = request.Name!.Trim();

        if (await store.NameExistsAsync(actor.OrganizationId, trimmedName, supplierId, cancellationToken))
        {
            throw new ConflictException("A supplier with this name already exists.");
        }

        var previous = new
        {
            supplier.Name,
            supplier.ContactPerson,
            supplier.Phone,
            supplier.Email,
            supplier.Address,
            supplier.RegistrationIdentifier,
            supplier.Notes
        };

        supplier.Update(
            trimmedName,
            request.ContactPerson,
            request.Phone,
            request.Email,
            request.Address,
            request.RegistrationIdentifier,
            request.Notes,
            actor.UserId);

        AddAudit(
            actor,
            supplier,
            "Supplier.Updated",
            new
            {
                previous,
                current = new { supplier.Name, supplier.ContactPerson, supplier.Phone, supplier.Email }
            },
            ipAddress);

        await store.SaveChangesAsync(cancellationToken);
        return ToResponse(supplier);
    }

    public Task<bool> ActivateAsync(
        ExpenseActor actor,
        Guid supplierId,
        string? ipAddress,
        CancellationToken cancellationToken = default) =>
        SetActiveAsync(actor, supplierId, true, ipAddress, cancellationToken);

    public Task<bool> DeactivateAsync(
        ExpenseActor actor,
        Guid supplierId,
        string? ipAddress,
        CancellationToken cancellationToken = default) =>
        SetActiveAsync(actor, supplierId, false, ipAddress, cancellationToken);

    private async Task<bool> SetActiveAsync(
        ExpenseActor actor,
        Guid supplierId,
        bool active,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        ValidateActor(actor);
        var supplier = await FindSupplierOrThrowAsync(actor, supplierId, cancellationToken);

        var changed = active
            ? supplier.Activate(actor.UserId)
            : supplier.Deactivate(actor.UserId);

        if (!changed)
        {
            return false;
        }

        AddAudit(actor, supplier, active ? "Supplier.Activated" : "Supplier.Deactivated", null, ipAddress);
        await store.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<Supplier> FindSupplierOrThrowAsync(ExpenseActor actor, Guid supplierId, CancellationToken cancellationToken)
    {
        if (supplierId == Guid.Empty)
        {
            throw new ResourceNotFoundException("The supplier was not found.");
        }

        return await store.FindAsync(supplierId, actor.OrganizationId, cancellationToken)
            ?? throw new ResourceNotFoundException("The supplier was not found.");
    }

    private void AddAudit(ExpenseActor actor, Supplier supplier, string action, object? details, string? ipAddress) =>
        store.AddAuditLog(new AuditLog(
            action,
            supplier.OrganizationId,
            actor.UserId,
            entityType: "Supplier",
            entityId: supplier.Id,
            details: details is null ? null : JsonSerializer.SerializeToDocument(details),
            ipAddress: ipAddress));

    private static SupplierResponse ToResponse(Supplier supplier) =>
        new(
            supplier.Id,
            supplier.OrganizationId,
            supplier.Name,
            supplier.ContactPerson,
            supplier.Phone,
            supplier.Email,
            supplier.Address,
            supplier.RegistrationIdentifier,
            supplier.Notes,
            supplier.IsActive,
            supplier.CreatedAt,
            supplier.CreatedBy,
            supplier.UpdatedAt,
            supplier.UpdatedBy);

    private static void ValidateCreateRequest(CreateSupplierRequest? request)
    {
        if (request is null)
        {
            throw Validation("request", "A request body is required.");
        }

        ValidateFields(
            request.Name,
            request.ContactPerson,
            request.Phone,
            request.Email,
            request.Address,
            request.RegistrationIdentifier,
            request.Notes);
    }

    private static void ValidateUpdateRequest(UpdateSupplierRequest? request)
    {
        if (request is null)
        {
            throw Validation("request", "A request body is required.");
        }

        ValidateFields(
            request.Name,
            request.ContactPerson,
            request.Phone,
            request.Email,
            request.Address,
            request.RegistrationIdentifier,
            request.Notes);
    }

    private static void ValidateFields(
        string? name,
        string? contactPerson,
        string? phone,
        string? email,
        string? address,
        string? registrationIdentifier,
        string? notes)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(name))
        {
            errors["name"] = ["Supplier name is required."];
        }
        else if (name.Trim().Length > 200)
        {
            errors["name"] = ["Supplier name cannot exceed 200 characters."];
        }

        if (!string.IsNullOrWhiteSpace(contactPerson) && contactPerson.Trim().Length > 150)
        {
            errors["contactPerson"] = ["Contact person cannot exceed 150 characters."];
        }

        if (!string.IsNullOrWhiteSpace(phone) && phone.Trim().Length > 50)
        {
            errors["phone"] = ["Phone number cannot exceed 50 characters."];
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            if (email.Trim().Length > 150)
            {
                errors["email"] = ["Email cannot exceed 150 characters."];
            }
            else
            {
                try
                {
                    _ = new MailAddress(email.Trim());
                }
                catch
                {
                    errors["email"] = ["Email format is invalid."];
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(address) && address.Trim().Length > 500)
        {
            errors["address"] = ["Address cannot exceed 500 characters."];
        }

        if (!string.IsNullOrWhiteSpace(registrationIdentifier) && registrationIdentifier.Trim().Length > 100)
        {
            errors["registrationIdentifier"] = ["Registration identifier cannot exceed 100 characters."];
        }

        if (!string.IsNullOrWhiteSpace(notes) && notes.Trim().Length > 1000)
        {
            errors["notes"] = ["Notes cannot exceed 1000 characters."];
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
