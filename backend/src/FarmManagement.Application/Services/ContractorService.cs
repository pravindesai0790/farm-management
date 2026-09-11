using System.Text.Json;
using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Labor;
using FarmManagement.Application.Interfaces.Labor;
using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Services;

public sealed class ContractorService(IContractorStore store) : IContractorService
{
    private const int DefaultPageSize = 20;
    private const int MaximumPageSize = 100;

    public async Task<PagedResponse<ContractorResponse>> ListAsync(
        ContractorActor actor,
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

        var responses = items.Select(x => ToResponse(x.Contractor, x.WorkerCount)).ToArray();
        return new PagedResponse<ContractorResponse>(responses, page, pageSize, totalCount);
    }

    public async Task<ContractorResponse> GetAsync(
        ContractorActor actor,
        Guid contractorId,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var contractor = await FindContractorOrThrowAsync(actor, contractorId, cancellationToken);
        var workerCount = await store.GetWorkerCountAsync(contractor.Id, actor.OrganizationId, cancellationToken);
        return ToResponse(contractor, workerCount);
    }

    public async Task<ContractorResponse> CreateAsync(
        ContractorActor actor,
        CreateContractorRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ValidateCreateRequest(request);

        var trimmedName = request.Name!.Trim();

        if (await store.NameExistsAsync(actor.OrganizationId, trimmedName, cancellationToken: cancellationToken))
        {
            throw new ConflictException("A contractor with this name already exists in the organization.");
        }

        var contractor = new Contractor(
            actor.OrganizationId,
            trimmedName,
            actor.UserId,
            NormalizeOptional(request.ContactPerson),
            NormalizeOptional(request.PhoneNumber),
            NormalizeOptional(request.Email),
            NormalizeOptional(request.Address),
            NormalizeOptional(request.Notes));

        store.Add(contractor);
        AddAudit(actor, contractor, "Contractor.Created", new { contractor.Name, contractor.ContactPerson }, ipAddress);

        await store.SaveChangesAsync(cancellationToken);
        return ToResponse(contractor, 0);
    }

    public async Task<ContractorResponse> UpdateAsync(
        ContractorActor actor,
        Guid contractorId,
        UpdateContractorRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ValidateUpdateRequest(request);

        var contractor = await FindContractorOrThrowAsync(actor, contractorId, cancellationToken);
        var trimmedName = request.Name!.Trim();

        if (await store.NameExistsAsync(actor.OrganizationId, trimmedName, contractor.Id, cancellationToken))
        {
            throw new ConflictException("A contractor with this name already exists in the organization.");
        }

        var previous = new { contractor.Name, contractor.ContactPerson, contractor.PhoneNumber, contractor.Email };

        contractor.Update(
            trimmedName,
            NormalizeOptional(request.ContactPerson),
            NormalizeOptional(request.PhoneNumber),
            NormalizeOptional(request.Email),
            NormalizeOptional(request.Address),
            NormalizeOptional(request.Notes),
            DateTimeOffset.UtcNow,
            actor.UserId);

        AddAudit(
            actor,
            contractor,
            "Contractor.Updated",
            new { previous, current = new { contractor.Name, contractor.ContactPerson, contractor.PhoneNumber, contractor.Email } },
            ipAddress);

        await store.SaveChangesAsync(cancellationToken);
        var workerCount = await store.GetWorkerCountAsync(contractor.Id, actor.OrganizationId, cancellationToken);
        return ToResponse(contractor, workerCount);
    }

    public Task<bool> ActivateAsync(
        ContractorActor actor,
        Guid contractorId,
        string? ipAddress,
        CancellationToken cancellationToken = default) =>
        SetActiveAsync(actor, contractorId, true, ipAddress, cancellationToken);

    public Task<bool> DeactivateAsync(
        ContractorActor actor,
        Guid contractorId,
        string? ipAddress,
        CancellationToken cancellationToken = default) =>
        SetActiveAsync(actor, contractorId, false, ipAddress, cancellationToken);

    private async Task<bool> SetActiveAsync(
        ContractorActor actor,
        Guid contractorId,
        bool active,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        ValidateActor(actor);
        var contractor = await FindContractorOrThrowAsync(actor, contractorId, cancellationToken);

        var changed = active
            ? contractor.Activate(DateTimeOffset.UtcNow, actor.UserId)
            : contractor.Deactivate(DateTimeOffset.UtcNow, actor.UserId);

        if (!changed)
        {
            return false;
        }

        AddAudit(actor, contractor, active ? "Contractor.Activated" : "Contractor.Deactivated", null, ipAddress);
        await store.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<Contractor> FindContractorOrThrowAsync(ContractorActor actor, Guid contractorId, CancellationToken cancellationToken)
    {
        if (contractorId == Guid.Empty)
        {
            throw new ResourceNotFoundException("The contractor was not found.");
        }

        return await store.FindAsync(contractorId, actor.OrganizationId, cancellationToken)
            ?? throw new ResourceNotFoundException("The contractor was not found.");
    }

    private void AddAudit(ContractorActor actor, Contractor contractor, string action, object? details, string? ipAddress) =>
        store.AddAuditLog(new AuditLog(
            action,
            contractor.OrganizationId,
            actor.UserId,
            entityType: "Contractor",
            entityId: contractor.Id,
            details: details is null ? null : JsonSerializer.SerializeToDocument(details),
            ipAddress: ipAddress));

    private static ContractorResponse ToResponse(Contractor contractor, int workerCount) =>
        new(
            contractor.Id,
            contractor.OrganizationId,
            contractor.Name,
            contractor.ContactPerson,
            contractor.PhoneNumber,
            contractor.Email,
            contractor.Address,
            contractor.Notes,
            contractor.IsActive,
            workerCount,
            contractor.CreatedAt,
            contractor.CreatedBy,
            contractor.UpdatedAt,
            contractor.UpdatedBy);

    private static void ValidateCreateRequest(CreateContractorRequest? request)
    {
        if (request is null)
        {
            throw Validation("request", "A request body is required.");
        }

        ValidateFields(request.Name, request.ContactPerson, request.PhoneNumber, request.Email);
    }

    private static void ValidateUpdateRequest(UpdateContractorRequest? request)
    {
        if (request is null)
        {
            throw Validation("request", "A request body is required.");
        }

        ValidateFields(request.Name, request.ContactPerson, request.PhoneNumber, request.Email);
    }

    private static void ValidateFields(string? name, string? contactPerson, string? phoneNumber, string? email)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw Validation("name", "Contractor name is required.");
        }

        if (name.Trim().Length > 200)
        {
            throw Validation("name", "Contractor name cannot exceed 200 characters.");
        }

        if (contactPerson is not null && contactPerson.Trim().Length > 150)
        {
            throw Validation("contactPerson", "Contact person cannot exceed 150 characters.");
        }

        if (phoneNumber is not null && phoneNumber.Trim().Length > 50)
        {
            throw Validation("phoneNumber", "Phone number cannot exceed 50 characters.");
        }

        if (email is not null && email.Trim().Length > 255)
        {
            throw Validation("email", "Email cannot exceed 255 characters.");
        }
    }

    private static int NormalizePageSize(int pageSize) =>
        pageSize == 0 ? DefaultPageSize : pageSize is < 1 or > MaximumPageSize
            ? throw Validation("pageSize", $"Page size must be between 1 and {MaximumPageSize}.")
            : pageSize;

    private static void ValidateActor(ContractorActor actor)
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
