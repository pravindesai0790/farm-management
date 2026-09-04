using System.Text.Json;
using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.CropCycles;
using FarmManagement.Application.Interfaces.CropCycles;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.Services;

public sealed class CropCycleService(ICropCycleStore store) : ICropCycleService
{
    public async Task<CropCycleListResponse> ListAsync(
        CropCycleActor actor,
        Guid? farmId,
        Guid? farmAreaId,
        Guid? plantationId,
        string? status,
        int? seasonYear,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        if (farmId == Guid.Empty) throw Validation("farmId", "Farm must be valid.");
        if (farmAreaId == Guid.Empty) throw Validation("farmAreaId", "Farm area must be valid.");
        if (plantationId == Guid.Empty) throw Validation("plantationId", "Plantation must be valid.");
        if (seasonYear is <= 0) throw Validation("seasonYear", "Season year must be greater than zero.");

        var parsedStatus = ParseStatus(status);
        var cycles = await store.ListAsync(actor.OrganizationId, farmId, farmAreaId, plantationId, parsedStatus, seasonYear, cancellationToken);
        return new CropCycleListResponse(cycles.Select(ToResponse).ToArray(), cycles.Count);
    }

    public async Task<CropCycleResponse> GetAsync(
        CropCycleActor actor,
        Guid cycleId,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        return ToResponse(await FindCycleOrThrowAsync(actor, cycleId, cancellationToken));
    }

    public async Task<CropCycleResponse> CreateAsync(
        CropCycleActor actor,
        CreateCropCycleRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var values = ReadValues(request);
        if (values.PlantationId == Guid.Empty) throw Validation("plantationId", "Plantation is required.");

        return await store.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            var plantation = await store.LockPlantationAsync(values.PlantationId, actor.OrganizationId, transactionCancellationToken)
                ?? throw new ResourceNotFoundException("The plantation was not found.");
            EnsureCanCreateForPlantation(plantation);
            ValidateDates(values.PlannedStartDate, values.ExpectedEndDate, plantation);
            await EnsureCodeIsAvailableAsync(actor, values.CycleCode, null, transactionCancellationToken);

            var cycle = new CropCycle(
                actor.OrganizationId,
                plantation.Id,
                values.CycleCode,
                values.CycleName,
                values.SeasonYear,
                values.SeasonName,
                values.PlannedStartDate,
                values.ExpectedEndDate,
                actor.UserId);

            store.Add(cycle);
            AddAudit(actor, cycle, "CropCycle.Created", new
            {
                cycle.CycleCode,
                cycle.CycleName,
                cycle.SeasonYear,
                cycle.Status
            }, ipAddress);
            await store.SaveChangesAsync(transactionCancellationToken);
            return ToResponse(cycle, plantation);
        }, cancellationToken);
    }

    public async Task<CropCycleResponse> UpdateAsync(
        CropCycleActor actor,
        Guid cycleId,
        UpdateCropCycleRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var values = ReadValues(request);

        return await store.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            var cycle = await store.LockAsync(cycleId, actor.OrganizationId, transactionCancellationToken)
                ?? throw new ResourceNotFoundException("The crop cycle was not found.");
            if (cycle.Status != CropCycleStatus.Planned)
            {
                throw new ConflictException("Only a planned crop cycle can be modified.");
            }

            var plantation = await store.LockPlantationAsync(cycle.PlantationId, actor.OrganizationId, transactionCancellationToken)
                ?? throw new ResourceNotFoundException("The plantation was not found.");
            EnsureCanCreateForPlantation(plantation);
            ValidateDates(values.PlannedStartDate, values.ExpectedEndDate, plantation);
            await EnsureCodeIsAvailableAsync(actor, values.CycleCode, cycle.Id, transactionCancellationToken);

            var previous = new
            {
                cycle.CycleCode,
                cycle.CycleName,
                cycle.SeasonYear,
                cycle.SeasonName,
                cycle.PlannedStartDate,
                cycle.ExpectedEndDate
            };
            cycle.Update(
                values.CycleCode,
                values.CycleName,
                values.SeasonYear,
                values.SeasonName,
                values.PlannedStartDate,
                values.ExpectedEndDate,
                DateTimeOffset.UtcNow,
                actor.UserId);
            AddAudit(actor, cycle, "CropCycle.Updated", new
            {
                previous,
                current = new
                {
                    cycle.CycleCode,
                    cycle.CycleName,
                    cycle.SeasonYear,
                    cycle.SeasonName,
                    cycle.PlannedStartDate,
                    cycle.ExpectedEndDate
                }
            }, ipAddress);
            await store.SaveChangesAsync(transactionCancellationToken);
            return ToResponse(cycle, plantation);
        }, cancellationToken);
    }

    public Task<bool> StartAsync(
        CropCycleActor actor,
        Guid cycleId,
        StartCropCycleRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(actor, cycleId, request, ipAddress, "CropCycle.Started", async (cycle, plantation, now) =>
        {
            if (request is null || request.StartDate is null) throw Validation("startDate", "Start date is required.");
            EnsurePlantationIsActive(plantation);
            if (await store.HasActiveCycleAsync(cycle.PlantationId, cycle.Id, cancellationToken))
            {
                throw new ConflictException("The plantation already has an active crop cycle.");
            }
            EnsureDateIsWithinPlantation(request.StartDate.Value, plantation, "startDate");
            if (cycle.ExpectedEndDate is not null && request.StartDate.Value > cycle.ExpectedEndDate)
            {
                throw Validation("startDate", "The actual start date cannot be after the expected end date.");
            }
            EnsureTransition(cycle, CropCycleStatus.Planned, "Only a planned crop cycle can be started.");
            if (!cycle.Start(request.StartDate.Value, now, actor.UserId)) return false;
            return true;
        }, cancellationToken);

    public Task<bool> HarvestAsync(
        CropCycleActor actor,
        Guid cycleId,
        HarvestCropCycleRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(actor, cycleId, request, ipAddress, "CropCycle.Harvested", (cycle, plantation, now) =>
        {
            if (request is null || request.HarvestDate is null) throw Validation("harvestDate", "Harvest date is required.");
            EnsurePlantationIsActive(plantation);
            EnsureDateIsWithinPlantation(request.HarvestDate.Value, plantation, "harvestDate");
            EnsureTransition(cycle, CropCycleStatus.Active, "Only an active crop cycle can be harvested.");
            return Task.FromResult(cycle.Harvest(request.HarvestDate.Value, now, actor.UserId));
        }, cancellationToken);

    public Task<bool> CompleteAsync(
        CropCycleActor actor,
        Guid cycleId,
        CompleteCropCycleRequest? request,
        string? ipAddress,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(actor, cycleId, request, ipAddress, "CropCycle.Completed", (cycle, plantation, now) =>
        {
            EnsureTransition(cycle, CropCycleStatus.Harvested, "Only a harvested crop cycle can be completed.");
            if (request?.CompletionDate is not null && request.CompletionDate < (cycle.ActualEndDate ?? cycle.PlannedStartDate))
            {
                throw Validation("completionDate", "Completion date cannot be before the harvest date.");
            }
            return Task.FromResult(cycle.Complete(request?.CompletionDate, now, actor.UserId));
        }, cancellationToken);

    public Task<bool> CancelAsync(
        CropCycleActor actor,
        Guid cycleId,
        CancelCropCycleRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(actor, cycleId, request, ipAddress, "CropCycle.Cancelled", async (cycle, plantation, now) =>
        {
            if (request is null || request.CancellationDate is null) throw Validation("cancellationDate", "Cancellation date is required.");
            if (request.CancellationReasonId is null || request.CancellationReasonId == Guid.Empty)
            {
                throw Validation("cancellationReasonId", "Cancellation reason is required.");
            }
            var reason = await store.FindCancellationReasonAsync(request.CancellationReasonId.Value, actor.OrganizationId, cancellationToken)
                ?? throw Validation("cancellationReasonId", "The cancellation reason was not found or is inactive.");
            EnsureTransition(cycle, CropCycleStatus.Active, "Only an active crop cycle can be cancelled.");
            EnsureDateIsWithinPlantation(request.CancellationDate.Value, plantation, "cancellationDate");
            if (!cycle.Cancel(request.CancellationDate.Value, reason.Id, request.Notes, now, actor.UserId)) return false;
            return true;
        }, cancellationToken);

    private async Task<bool> TransitionAsync<TRequest>(
        CropCycleActor actor,
        Guid cycleId,
        TRequest request,
        string? ipAddress,
        string auditAction,
        Func<CropCycle, CropPlantation, DateTimeOffset, Task<bool>> transition,
        CancellationToken cancellationToken)
    {
        ValidateActor(actor);
        return await store.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            var cycle = await store.LockAsync(cycleId, actor.OrganizationId, transactionCancellationToken)
                ?? throw new ResourceNotFoundException("The crop cycle was not found.");
            var plantation = await store.LockPlantationAsync(cycle.PlantationId, actor.OrganizationId, transactionCancellationToken)
                ?? throw new ResourceNotFoundException("The plantation was not found.");
            var previousStatus = cycle.Status;
            var changed = await transition(cycle, plantation, DateTimeOffset.UtcNow);
            if (!changed) return false;

            AddAudit(actor, cycle, auditAction, new
            {
                PlantationId = plantation.Id,
                PreviousStatus = previousStatus.ToString().ToUpperInvariant(),
                NewStatus = cycle.Status.ToString().ToUpperInvariant(),
                cycle.ActualStartDate,
                cycle.ActualEndDate,
                cycle.CancellationReasonId,
                cycle.CancellationNotes
            }, ipAddress);
            await store.SaveChangesAsync(transactionCancellationToken);
            return true;
        }, cancellationToken);
    }

    private async Task<CropCycle> FindCycleOrThrowAsync(
        CropCycleActor actor,
        Guid cycleId,
        CancellationToken cancellationToken) =>
        cycleId == Guid.Empty
            ? throw new ResourceNotFoundException("The crop cycle was not found.")
            : await store.FindAsync(cycleId, actor.OrganizationId, cancellationToken)
                ?? throw new ResourceNotFoundException("The crop cycle was not found.");

    private async Task EnsureCodeIsAvailableAsync(
        CropCycleActor actor,
        string code,
        Guid? excludingId,
        CancellationToken cancellationToken)
    {
        if (await store.CodeExistsAsync(actor.OrganizationId, code, excludingId, cancellationToken))
        {
            throw new ConflictException("A crop cycle with this code already exists in the organization.");
        }
    }

    private static void EnsureCanCreateForPlantation(CropPlantation plantation)
    {
        if (plantation.Status is PlantationStatus.Terminated or PlantationStatus.Archived)
        {
            throw new ConflictException("A crop cycle cannot be created for a terminated or archived plantation.");
        }
    }

    private static void EnsurePlantationIsActive(CropPlantation plantation)
    {
        if (plantation.Status != PlantationStatus.Active)
        {
            throw new ConflictException("A crop cycle can only be started while its plantation is active.");
        }
    }

    private static void EnsureDateIsWithinPlantation(DateOnly date, CropPlantation plantation, string fieldName)
    {
        if (date < plantation.PlantingDate)
        {
            throw Validation(fieldName, "The date cannot be before the plantation's planting date.");
        }
        if (plantation.ActualEndDate is not null && date > plantation.ActualEndDate)
        {
            throw Validation(fieldName, "The date cannot be after the plantation termination date.");
        }
    }

    private static void ValidateDates(DateOnly plannedStartDate, DateOnly? expectedEndDate, CropPlantation plantation)
    {
        if (plannedStartDate < plantation.PlantingDate)
        {
            throw Validation("plannedStartDate", "The planned start date cannot be before the plantation's planting date.");
        }
        if (plantation.ActualEndDate is not null && plannedStartDate > plantation.ActualEndDate)
        {
            throw Validation("plannedStartDate", "The planned start date cannot be after the plantation termination date.");
        }
        if (expectedEndDate is not null && plantation.ActualEndDate is not null && expectedEndDate > plantation.ActualEndDate)
        {
            throw Validation("expectedEndDate", "The expected end date cannot be after the plantation termination date.");
        }
    }

    private static void EnsureTransition(CropCycle cycle, CropCycleStatus expectedStatus, string message)
    {
        if (cycle.Status != expectedStatus) throw new ConflictException(message);
    }

    private static CropCycleStatus? ParseStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return null;
        if (!Enum.TryParse<CropCycleStatus>(status.Trim(), true, out var parsed))
        {
            throw Validation("status", "Status must be PLANNED, ACTIVE, HARVESTED, COMPLETED, or CANCELLED.");
        }
        return parsed;
    }

    private void AddAudit(CropCycleActor actor, CropCycle cycle, string action, object details, string? ipAddress) =>
        store.AddAuditLog(new AuditLog(
            action,
            cycle.OrganizationId,
            actor.UserId,
            "CropCycle",
            cycle.Id,
            JsonSerializer.SerializeToDocument(details),
            ipAddress));

    private static CropCycleResponse ToResponse(CropCycle cycle) =>
        ToResponse(cycle, cycle.Plantation ?? throw new InvalidOperationException("A crop cycle references a missing plantation."));

    private static CropCycleResponse ToResponse(CropCycle cycle, CropPlantation plantation)
    {
        var crop = plantation.Crop ?? throw new InvalidOperationException("A crop cycle references a plantation with a missing crop.");
        var farm = plantation.Farm;
        var farmArea = plantation.FarmArea;
        return new CropCycleResponse(
            cycle.Id,
            cycle.PlantationId,
            plantation.PlantationCode,
            plantation.PlantationName,
            plantation.FarmId,
            farm?.Code ?? string.Empty,
            farm?.Name ?? string.Empty,
            plantation.FarmAreaId,
            farmArea?.Code ?? string.Empty,
            farmArea?.Name ?? string.Empty,
            plantation.CropId,
            crop.Code,
            crop.Name,
            crop.CropDurationType,
            cycle.CycleCode,
            cycle.CycleName,
            cycle.SeasonYear,
            cycle.SeasonName,
            cycle.PlannedStartDate,
            cycle.ActualStartDate,
            cycle.ExpectedEndDate,
            cycle.ActualEndDate,
            cycle.Status.ToString().ToUpperInvariant(),
            cycle.CancellationReasonId,
            cycle.CancellationReason?.Code,
            cycle.CancellationReason?.Name,
            cycle.CancellationNotes,
            cycle.CreatedAt,
            cycle.CreatedBy,
            cycle.UpdatedAt,
            cycle.UpdatedBy);
    }

    private static CreateValues ReadValues(CreateCropCycleRequest? request)
    {
        if (request is null) throw Validation("request", "A request body is required.");
        return ReadValues(request.PlantationId, request.CycleCode, request.CycleName, request.SeasonYear,
            request.SeasonName, request.PlannedStartDate, request.ExpectedEndDate);
    }

    private static CreateValues ReadValues(UpdateCropCycleRequest? request)
    {
        if (request is null) throw Validation("request", "A request body is required.");
        return ReadValues(null, request.CycleCode, request.CycleName, request.SeasonYear,
            request.SeasonName, request.PlannedStartDate, request.ExpectedEndDate);
    }

    private static CreateValues ReadValues(
        Guid? plantationId,
        string? cycleCode,
        string? cycleName,
        int? seasonYear,
        string? seasonName,
        DateOnly? plannedStartDate,
        DateOnly? expectedEndDate)
    {
        if (string.IsNullOrWhiteSpace(cycleCode)) throw Validation("cycleCode", "Cycle code is required.");
        if (cycleCode.Trim().Length > 50) throw Validation("cycleCode", "Cycle code cannot exceed 50 characters.");
        if (string.IsNullOrWhiteSpace(cycleName)) throw Validation("cycleName", "Cycle name is required.");
        if (cycleName.Trim().Length > 200) throw Validation("cycleName", "Cycle name cannot exceed 200 characters.");
        if (seasonYear is null or <= 0) throw Validation("seasonYear", "Season year must be greater than zero.");
        if (seasonName?.Trim().Length > 100) throw Validation("seasonName", "Season name cannot exceed 100 characters.");
        if (plannedStartDate is null) throw Validation("plannedStartDate", "Planned start date is required.");
        if (expectedEndDate is not null && expectedEndDate < plannedStartDate)
        {
            throw Validation("expectedEndDate", "Expected end date cannot be before the planned start date.");
        }

        return new CreateValues(
            plantationId ?? Guid.Empty,
            cycleCode.Trim().ToUpperInvariant(),
            cycleName.Trim(),
            seasonYear.Value,
            string.IsNullOrWhiteSpace(seasonName) ? null : seasonName.Trim(),
            plannedStartDate.Value,
            expectedEndDate);
    }

    private static void ValidateActor(CropCycleActor actor)
    {
        if (actor.UserId == Guid.Empty || actor.OrganizationId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("The access token does not contain a valid user scope.");
        }
    }

    private static ValidationException Validation(string fieldName, string message) =>
        new("Validation failed", new Dictionary<string, string[]> { [fieldName] = [message] });

    private sealed record CreateValues(
        Guid PlantationId,
        string CycleCode,
        string CycleName,
        int SeasonYear,
        string? SeasonName,
        DateOnly PlannedStartDate,
        DateOnly? ExpectedEndDate);
}
