using System.Text.Json;
using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Labor;
using FarmManagement.Application.Interfaces.Labor;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.Services;

public sealed class WorkerService(IWorkerStore store) : IWorkerService
{
    private const int DefaultPageSize = 20;
    private const int MaximumPageSize = 100;

    public async Task<PagedResponse<WorkerResponse>> ListAsync(
        WorkerActor actor,
        int page,
        int pageSize,
        string? search,
        bool? isActive,
        Guid? contractorId,
        Guid? laborCategoryId,
        string? gender,
        string? employmentType,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        if (page < 1)
        {
            throw Validation("page", "Page must be at least 1.");
        }

        pageSize = NormalizePageSize(pageSize);

        var normalizedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        var parsedGender = ParseOptionalGender(gender);
        var parsedEmploymentType = ParseOptionalEmploymentType(employmentType);

        if (contractorId == Guid.Empty)
        {
            throw Validation("contractorId", "Contractor identifier must be valid.");
        }

        if (laborCategoryId == Guid.Empty)
        {
            throw Validation("laborCategoryId", "Labor category identifier must be valid.");
        }

        var totalCount = await store.CountAsync(
            actor.OrganizationId,
            normalizedSearch,
            isActive,
            contractorId,
            laborCategoryId,
            parsedGender,
            parsedEmploymentType,
            cancellationToken);

        var workers = await store.ListAsync(
            actor.OrganizationId,
            checked((page - 1) * pageSize),
            pageSize,
            normalizedSearch,
            isActive,
            contractorId,
            laborCategoryId,
            parsedGender,
            parsedEmploymentType,
            cancellationToken);

        return new PagedResponse<WorkerResponse>(
            workers.Select(ToResponse).ToArray(),
            page,
            pageSize,
            totalCount);
    }

    public async Task<WorkerDetailResponse> GetAsync(
        WorkerActor actor,
        Guid workerId,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var worker = await FindWorkerOrThrowAsync(actor, workerId, cancellationToken);
        return ToDetailResponse(worker);
    }

    public async Task<WorkerDetailResponse> CreateAsync(
        WorkerActor actor,
        CreateWorkerRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ValidateCreateRequest(request);

        var gender = ParseRequiredGender(request.Gender);
        var employmentType = ParseRequiredEmploymentType(request.EmploymentType);

        if (employmentType == EmploymentType.Contract && (!request.ContractorId.HasValue || request.ContractorId.Value == Guid.Empty))
        {
            throw Validation("contractorId", "A contractor is required for contract workers.");
        }

        Contractor? contractor = null;
        if (request.ContractorId.HasValue && request.ContractorId.Value != Guid.Empty)
        {
            contractor = await store.FindContractorAsync(request.ContractorId.Value, actor.OrganizationId, cancellationToken);
            if (contractor is null)
            {
                throw Validation("contractorId", "The contractor was not found.");
            }

            if (!contractor.IsActive)
            {
                throw Validation("contractorId", "Cannot assign an inactive contractor.");
            }
        }

        LaborCategory? laborCategory = null;
        if (request.LaborCategoryId.HasValue && request.LaborCategoryId.Value != Guid.Empty)
        {
            laborCategory = await store.FindLaborCategoryAsync(request.LaborCategoryId.Value, actor.OrganizationId, cancellationToken);
            if (laborCategory is null)
            {
                throw Validation("laborCategoryId", "The labor category was not found.");
            }

            if (!laborCategory.IsActive)
            {
                throw Validation("laborCategoryId", "Cannot assign an inactive labor category.");
            }
        }

        ValidateDates(request.JoiningDate, request.LeavingDate);

        var worker = new Worker(
            actor.OrganizationId,
            request.FirstName!.Trim(),
            NormalizeOptional(request.LastName),
            gender,
            employmentType,
            actor.UserId,
            NormalizeOptional(request.DisplayName),
            NormalizeOptional(request.MobileNumber),
            NormalizeOptional(request.AlternateMobileNumber),
            request.LaborCategoryId,
            request.ContractorId,
            request.JoiningDate,
            request.LeavingDate,
            NormalizeOptional(request.Notes));

        store.Add(worker);
        AddAudit(
            actor,
            worker,
            "Worker.Created",
            new
            {
                worker.FirstName,
                worker.LastName,
                worker.DisplayName,
                Gender = worker.Gender.ToString().ToUpperInvariant(),
                EmploymentType = worker.EmploymentType.ToString().ToUpperInvariant(),
                worker.ContractorId,
                worker.LaborCategoryId
            },
            ipAddress);

        await store.SaveChangesAsync(cancellationToken);

        // Fetch populated entity for response
        var created = await store.FindAsync(worker.Id, actor.OrganizationId, cancellationToken) ?? worker;
        return ToDetailResponse(created);
    }

    public async Task<WorkerDetailResponse> UpdateAsync(
        WorkerActor actor,
        Guid workerId,
        UpdateWorkerRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ValidateUpdateRequest(request);

        var worker = await FindWorkerOrThrowAsync(actor, workerId, cancellationToken);

        var gender = ParseRequiredGender(request.Gender);
        var employmentType = ParseRequiredEmploymentType(request.EmploymentType);

        if (employmentType == EmploymentType.Contract && (!request.ContractorId.HasValue || request.ContractorId.Value == Guid.Empty))
        {
            throw Validation("contractorId", "A contractor is required for contract workers.");
        }

        Contractor? contractor = null;
        if (request.ContractorId.HasValue && request.ContractorId.Value != Guid.Empty)
        {
            contractor = await store.FindContractorAsync(request.ContractorId.Value, actor.OrganizationId, cancellationToken);
            if (contractor is null)
            {
                throw Validation("contractorId", "The contractor was not found.");
            }

            // If changing to a new contractor or assigning one, contractor must be active
            if (worker.ContractorId != request.ContractorId.Value && !contractor.IsActive)
            {
                throw Validation("contractorId", "Cannot assign an inactive contractor.");
            }
        }

        LaborCategory? laborCategory = null;
        if (request.LaborCategoryId.HasValue && request.LaborCategoryId.Value != Guid.Empty)
        {
            laborCategory = await store.FindLaborCategoryAsync(request.LaborCategoryId.Value, actor.OrganizationId, cancellationToken);
            if (laborCategory is null)
            {
                throw Validation("laborCategoryId", "The labor category was not found.");
            }

            if (worker.LaborCategoryId != request.LaborCategoryId.Value && !laborCategory.IsActive)
            {
                throw Validation("laborCategoryId", "Cannot assign an inactive labor category.");
            }
        }

        ValidateDates(request.JoiningDate, request.LeavingDate);

        var previous = new
        {
            worker.FirstName,
            worker.LastName,
            worker.DisplayName,
            Gender = worker.Gender.ToString().ToUpperInvariant(),
            EmploymentType = worker.EmploymentType.ToString().ToUpperInvariant(),
            worker.ContractorId,
            worker.LaborCategoryId
        };

        worker.Update(
            request.FirstName!.Trim(),
            NormalizeOptional(request.LastName),
            NormalizeOptional(request.DisplayName),
            gender,
            employmentType,
            request.LaborCategoryId,
            request.ContractorId,
            request.JoiningDate,
            request.LeavingDate,
            NormalizeOptional(request.MobileNumber),
            NormalizeOptional(request.AlternateMobileNumber),
            NormalizeOptional(request.Notes),
            DateTimeOffset.UtcNow,
            actor.UserId);

        AddAudit(
            actor,
            worker,
            "Worker.Updated",
            new
            {
                previous,
                current = new
                {
                    worker.FirstName,
                    worker.LastName,
                    worker.DisplayName,
                    Gender = worker.Gender.ToString().ToUpperInvariant(),
                    EmploymentType = worker.EmploymentType.ToString().ToUpperInvariant(),
                    worker.ContractorId,
                    worker.LaborCategoryId
                }
            },
            ipAddress);

        await store.SaveChangesAsync(cancellationToken);

        var updated = await store.FindAsync(worker.Id, actor.OrganizationId, cancellationToken) ?? worker;
        return ToDetailResponse(updated);
    }

    public Task<bool> ActivateAsync(
        WorkerActor actor,
        Guid workerId,
        string? ipAddress,
        CancellationToken cancellationToken = default) =>
        SetActiveAsync(actor, workerId, true, ipAddress, cancellationToken);

    public Task<bool> DeactivateAsync(
        WorkerActor actor,
        Guid workerId,
        string? ipAddress,
        CancellationToken cancellationToken = default) =>
        SetActiveAsync(actor, workerId, false, ipAddress, cancellationToken);

    private async Task<bool> SetActiveAsync(
        WorkerActor actor,
        Guid workerId,
        bool active,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        ValidateActor(actor);
        var worker = await FindWorkerOrThrowAsync(actor, workerId, cancellationToken);
        var changed = active
            ? worker.Activate(DateTimeOffset.UtcNow, actor.UserId)
            : worker.Deactivate(DateTimeOffset.UtcNow, actor.UserId);

        if (!changed)
        {
            return false;
        }

        AddAudit(actor, worker, active ? "Worker.Activated" : "Worker.Deactivated", null, ipAddress);
        await store.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<Worker> FindWorkerOrThrowAsync(WorkerActor actor, Guid workerId, CancellationToken cancellationToken)
    {
        if (workerId == Guid.Empty)
        {
            throw new ResourceNotFoundException("The worker was not found.");
        }

        return await store.FindAsync(workerId, actor.OrganizationId, cancellationToken)
            ?? throw new ResourceNotFoundException("The worker was not found.");
    }

    private void AddAudit(WorkerActor actor, Worker worker, string action, object? details, string? ipAddress) =>
        store.AddAuditLog(new AuditLog(
            action,
            worker.OrganizationId,
            actor.UserId,
            entityType: "Worker",
            entityId: worker.Id,
            details: details is null ? null : JsonSerializer.SerializeToDocument(details),
            ipAddress: ipAddress));

    private static WorkerResponse ToResponse(Worker worker) =>
        new(
            worker.Id,
            worker.OrganizationId,
            worker.FirstName,
            worker.LastName,
            worker.DisplayName,
            worker.Gender.ToString().ToUpperInvariant(),
            ConvertEmploymentTypeToString(worker.EmploymentType),
            worker.MobileNumber,
            worker.AlternateMobileNumber,
            worker.LaborCategoryId,
            worker.LaborCategory?.Name,
            worker.ContractorId,
            worker.Contractor?.Name,
            worker.JoiningDate,
            worker.LeavingDate,
            worker.IsActive,
            worker.CreatedAt,
            worker.UpdatedAt);

    private static WorkerDetailResponse ToDetailResponse(Worker worker) =>
        new(
            worker.Id,
            worker.OrganizationId,
            worker.FirstName,
            worker.LastName,
            worker.DisplayName,
            worker.Gender.ToString().ToUpperInvariant(),
            ConvertEmploymentTypeToString(worker.EmploymentType),
            worker.MobileNumber,
            worker.AlternateMobileNumber,
            worker.LaborCategoryId,
            worker.LaborCategory?.Name,
            worker.ContractorId,
            worker.Contractor?.Name,
            worker.JoiningDate,
            worker.LeavingDate,
            worker.Notes,
            worker.IsActive,
            worker.CreatedAt,
            worker.CreatedBy,
            worker.UpdatedAt,
            worker.UpdatedBy);

    private static string ConvertEmploymentTypeToString(EmploymentType type) => type switch
    {
        EmploymentType.Permanent => "PERMANENT",
        EmploymentType.Seasonal => "SEASONAL",
        EmploymentType.DailyWage => "DAILY_WAGE",
        EmploymentType.Contract => "CONTRACT",
        _ => type.ToString().ToUpperInvariant()
    };

    private static Gender ParseRequiredGender(string? gender)
    {
        if (string.IsNullOrWhiteSpace(gender))
        {
            throw Validation("gender", "Gender is required.");
        }

        return ParseGenderCore(gender);
    }

    private static Gender? ParseOptionalGender(string? gender)
    {
        if (string.IsNullOrWhiteSpace(gender))
        {
            return null;
        }

        return ParseGenderCore(gender);
    }

    private static Gender ParseGenderCore(string gender) =>
        gender.Trim().ToUpperInvariant() switch
        {
            "MALE" => Gender.Male,
            "FEMALE" => Gender.Female,
            "OTHER" => Gender.Other,
            _ => throw Validation("gender", "Gender is invalid. Valid values are MALE, FEMALE, OTHER.")
        };

    private static EmploymentType ParseRequiredEmploymentType(string? employmentType)
    {
        if (string.IsNullOrWhiteSpace(employmentType))
        {
            throw Validation("employmentType", "Employment type is required.");
        }

        return ParseEmploymentTypeCore(employmentType);
    }

    private static EmploymentType? ParseOptionalEmploymentType(string? employmentType)
    {
        if (string.IsNullOrWhiteSpace(employmentType))
        {
            return null;
        }

        return ParseEmploymentTypeCore(employmentType);
    }

    private static EmploymentType ParseEmploymentTypeCore(string employmentType) =>
        employmentType.Trim().ToUpperInvariant() switch
        {
            "PERMANENT" => EmploymentType.Permanent,
            "SEASONAL" => EmploymentType.Seasonal,
            "DAILY_WAGE" or "DAILYWAGE" => EmploymentType.DailyWage,
            "CONTRACT" => EmploymentType.Contract,
            _ => throw Validation("employmentType", "Employment type is invalid. Valid values are PERMANENT, SEASONAL, DAILY_WAGE, CONTRACT.")
        };

    private static void ValidateCreateRequest(CreateWorkerRequest? request)
    {
        if (request is null)
        {
            throw Validation("request", "A request body is required.");
        }

        ValidateFields(request.FirstName, request.LastName, request.DisplayName, request.MobileNumber, request.AlternateMobileNumber);
    }

    private static void ValidateUpdateRequest(UpdateWorkerRequest? request)
    {
        if (request is null)
        {
            throw Validation("request", "A request body is required.");
        }

        ValidateFields(request.FirstName, request.LastName, request.DisplayName, request.MobileNumber, request.AlternateMobileNumber);
    }

    private static void ValidateFields(string? firstName, string? lastName, string? displayName, string? mobileNumber, string? alternateMobileNumber)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw Validation("firstName", "First name is required.");
        }

        if (firstName.Trim().Length > 100)
        {
            throw Validation("firstName", "First name cannot exceed 100 characters.");
        }

        if (lastName is not null && lastName.Trim().Length > 100)
        {
            throw Validation("lastName", "Last name cannot exceed 100 characters.");
        }

        if (displayName is not null && displayName.Trim().Length > 200)
        {
            throw Validation("displayName", "Display name cannot exceed 200 characters.");
        }

        if (mobileNumber is not null && mobileNumber.Trim().Length > 30)
        {
            throw Validation("mobileNumber", "Mobile number cannot exceed 30 characters.");
        }

        if (alternateMobileNumber is not null && alternateMobileNumber.Trim().Length > 30)
        {
            throw Validation("alternateMobileNumber", "Alternate mobile number cannot exceed 30 characters.");
        }
    }

    private static void ValidateDates(DateOnly? joiningDate, DateOnly? leavingDate)
    {
        if (joiningDate.HasValue && leavingDate.HasValue && leavingDate.Value < joiningDate.Value)
        {
            throw Validation("leavingDate", "The leaving date cannot be before the joining date.");
        }
    }

    private static int NormalizePageSize(int pageSize) =>
        pageSize == 0 ? DefaultPageSize : pageSize is < 1 or > MaximumPageSize
            ? throw Validation("pageSize", $"Page size must be between 1 and {MaximumPageSize}.")
            : pageSize;

    private static void ValidateActor(WorkerActor actor)
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
