using System.Text.Json;
using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.Labor;
using FarmManagement.Application.Interfaces.Labor;
using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Services;

public sealed class WorkerFarmAssignmentService(IWorkerFarmAssignmentStore store) : IWorkerFarmAssignmentService
{
    public async Task<IReadOnlyList<WorkerFarmAssignmentResponse>> ListByWorkerAsync(
        WorkerActor actor,
        Guid workerId,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        await EnsureWorkerExistsAsync(actor, workerId, cancellationToken);

        var assignments = await store.ListByWorkerAsync(actor.OrganizationId, workerId, isActive, cancellationToken);
        return assignments.Select(ToResponse).ToArray();
    }

    public async Task<IReadOnlyList<WorkerFarmAssignmentResponse>> ListByFarmAsync(
        WorkerActor actor,
        Guid farmId,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        await EnsureFarmExistsAsync(actor, farmId, cancellationToken);

        var assignments = await store.ListByFarmAsync(actor.OrganizationId, farmId, isActive, cancellationToken);
        return assignments.Select(ToResponse).ToArray();
    }

    public async Task<WorkerFarmAssignmentResponse> GetAsync(
        WorkerActor actor,
        Guid workerId,
        Guid assignmentId,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        await EnsureWorkerExistsAsync(actor, workerId, cancellationToken);

        var assignment = await FindAssignmentOrThrowAsync(actor, workerId, assignmentId, cancellationToken);
        return ToResponse(assignment);
    }

    public async Task<WorkerFarmAssignmentResponse> CreateAsync(
        WorkerActor actor,
        Guid workerId,
        CreateWorkerFarmAssignmentRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ValidateCreateRequest(request);

        var worker = await store.FindWorkerAsync(workerId, actor.OrganizationId, cancellationToken)
            ?? throw new ResourceNotFoundException("The worker was not found.");

        if (!worker.IsActive)
        {
            throw Validation("workerId", "Cannot assign an inactive worker.");
        }

        var farm = await store.FindFarmAsync(request.FarmId!.Value, actor.OrganizationId, cancellationToken);
        if (farm is null)
        {
            throw Validation("farmId", "The farm was not found.");
        }

        if (!farm.IsActive)
        {
            throw Validation("farmId", "Cannot assign a worker to an inactive farm.");
        }

        ValidateDates(request.AssignedFrom!.Value, request.AssignedTo);

        var assignment = new WorkerFarmAssignment(
            actor.OrganizationId,
            worker.Id,
            farm.Id,
            request.AssignedFrom.Value,
            actor.UserId,
            request.AssignedTo,
            NormalizeOptional(request.Notes));

        store.Add(assignment);
        AddAudit(
            actor,
            assignment,
            "WorkerFarmAssignment.Created",
            new
            {
                assignment.WorkerId,
                assignment.FarmId,
                assignment.AssignedFrom,
                assignment.AssignedTo
            },
            ipAddress);

        await store.SaveChangesAsync(cancellationToken);

        var created = await store.FindAsync(assignment.Id, actor.OrganizationId, cancellationToken) ?? assignment;
        return ToResponse(created);
    }

    public async Task<WorkerFarmAssignmentResponse> UpdateAsync(
        WorkerActor actor,
        Guid workerId,
        Guid assignmentId,
        UpdateWorkerFarmAssignmentRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ValidateUpdateRequest(request);

        var assignment = await FindAssignmentOrThrowAsync(actor, workerId, assignmentId, cancellationToken);
        ValidateDates(request.AssignedFrom!.Value, request.AssignedTo);

        var previous = new
        {
            assignment.AssignedFrom,
            assignment.AssignedTo,
            assignment.Notes
        };

        assignment.Update(
            request.AssignedFrom.Value,
            request.AssignedTo,
            NormalizeOptional(request.Notes),
            DateTimeOffset.UtcNow,
            actor.UserId);

        AddAudit(
            actor,
            assignment,
            "WorkerFarmAssignment.Updated",
            new
            {
                previous,
                current = new
                {
                    assignment.AssignedFrom,
                    assignment.AssignedTo,
                    assignment.Notes
                }
            },
            ipAddress);

        await store.SaveChangesAsync(cancellationToken);

        var updated = await store.FindAsync(assignment.Id, actor.OrganizationId, cancellationToken) ?? assignment;
        return ToResponse(updated);
    }

    public async Task<WorkerFarmAssignmentResponse> EndAssignmentAsync(
        WorkerActor actor,
        Guid workerId,
        Guid assignmentId,
        EndWorkerFarmAssignmentRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var assignment = await FindAssignmentOrThrowAsync(actor, workerId, assignmentId, cancellationToken);

        var endDate = request?.EndDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        if (endDate < assignment.AssignedFrom)
        {
            throw Validation("endDate", "The end date cannot be earlier than assigned from date.");
        }

        assignment.EndAssignment(endDate, DateTimeOffset.UtcNow, actor.UserId);

        AddAudit(
            actor,
            assignment,
            "WorkerFarmAssignment.Ended",
            new { assignment.AssignedFrom, EndDate = endDate },
            ipAddress);

        await store.SaveChangesAsync(cancellationToken);

        var updated = await store.FindAsync(assignment.Id, actor.OrganizationId, cancellationToken) ?? assignment;
        return ToResponse(updated);
    }

    public Task<bool> DeactivateAsync(
        WorkerActor actor,
        Guid workerId,
        Guid assignmentId,
        string? ipAddress,
        CancellationToken cancellationToken = default) =>
        SetActiveAsync(actor, workerId, assignmentId, false, ipAddress, cancellationToken);

    public Task<bool> ActivateAsync(
        WorkerActor actor,
        Guid workerId,
        Guid assignmentId,
        string? ipAddress,
        CancellationToken cancellationToken = default) =>
        SetActiveAsync(actor, workerId, assignmentId, true, ipAddress, cancellationToken);

    private async Task<bool> SetActiveAsync(
        WorkerActor actor,
        Guid workerId,
        Guid assignmentId,
        bool active,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        ValidateActor(actor);
        var assignment = await FindAssignmentOrThrowAsync(actor, workerId, assignmentId, cancellationToken);

        var changed = active
            ? assignment.Activate(DateTimeOffset.UtcNow, actor.UserId)
            : assignment.Deactivate(DateTimeOffset.UtcNow, actor.UserId);

        if (!changed)
        {
            return false;
        }

        AddAudit(
            actor,
            assignment,
            active ? "WorkerFarmAssignment.Activated" : "WorkerFarmAssignment.Deactivated",
            null,
            ipAddress);

        await store.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task EnsureWorkerExistsAsync(WorkerActor actor, Guid workerId, CancellationToken cancellationToken)
    {
        if (workerId == Guid.Empty)
        {
            throw new ResourceNotFoundException("The worker was not found.");
        }

        _ = await store.FindWorkerAsync(workerId, actor.OrganizationId, cancellationToken)
            ?? throw new ResourceNotFoundException("The worker was not found.");
    }

    private async Task EnsureFarmExistsAsync(WorkerActor actor, Guid farmId, CancellationToken cancellationToken)
    {
        if (farmId == Guid.Empty)
        {
            throw new ResourceNotFoundException("The farm was not found.");
        }

        _ = await store.FindFarmAsync(farmId, actor.OrganizationId, cancellationToken)
            ?? throw new ResourceNotFoundException("The farm was not found.");
    }

    private async Task<WorkerFarmAssignment> FindAssignmentOrThrowAsync(
        WorkerActor actor,
        Guid workerId,
        Guid assignmentId,
        CancellationToken cancellationToken)
    {
        if (assignmentId == Guid.Empty)
        {
            throw new ResourceNotFoundException("The farm assignment was not found.");
        }

        var assignment = await store.FindAsync(assignmentId, actor.OrganizationId, cancellationToken)
            ?? throw new ResourceNotFoundException("The farm assignment was not found.");

        if (assignment.WorkerId != workerId)
        {
            throw new ResourceNotFoundException("The farm assignment was not found.");
        }

        return assignment;
    }

    private void AddAudit(WorkerActor actor, WorkerFarmAssignment assignment, string action, object? details, string? ipAddress) =>
        store.AddAuditLog(new AuditLog(
            action,
            assignment.OrganizationId,
            actor.UserId,
            entityType: "WorkerFarmAssignment",
            entityId: assignment.Id,
            details: details is null ? null : JsonSerializer.SerializeToDocument(details),
            ipAddress: ipAddress));

    private static WorkerFarmAssignmentResponse ToResponse(WorkerFarmAssignment assignment) =>
        new(
            assignment.Id,
            assignment.OrganizationId,
            assignment.WorkerId,
            assignment.Worker?.DisplayName ?? string.Empty,
            assignment.FarmId,
            assignment.Farm?.Code ?? string.Empty,
            assignment.Farm?.Name ?? string.Empty,
            assignment.AssignedFrom,
            assignment.AssignedTo,
            assignment.IsActive,
            assignment.Notes,
            assignment.CreatedAt,
            assignment.CreatedBy,
            assignment.UpdatedAt,
            assignment.UpdatedBy);

    private static void ValidateCreateRequest(CreateWorkerFarmAssignmentRequest? request)
    {
        if (request is null)
        {
            throw Validation("request", "A request body is required.");
        }

        if (request.FarmId is null || request.FarmId == Guid.Empty)
        {
            throw Validation("farmId", "Farm is required.");
        }

        if (request.AssignedFrom is null)
        {
            throw Validation("assignedFrom", "Assigned from date is required.");
        }
    }

    private static void ValidateUpdateRequest(UpdateWorkerFarmAssignmentRequest? request)
    {
        if (request is null)
        {
            throw Validation("request", "A request body is required.");
        }

        if (request.AssignedFrom is null)
        {
            throw Validation("assignedFrom", "Assigned from date is required.");
        }
    }

    private static void ValidateDates(DateOnly assignedFrom, DateOnly? assignedTo)
    {
        if (assignedTo.HasValue && assignedTo.Value < assignedFrom)
        {
            throw Validation("assignedTo", "The assigned to date cannot be before the assigned from date.");
        }
    }

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
