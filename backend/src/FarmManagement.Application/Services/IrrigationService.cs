using System.Text.Json;
using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Irrigation;
using FarmManagement.Application.Interfaces.Irrigation;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.Services;

public sealed class IrrigationService(IIrrigationStore store) : IIrrigationService
{
    private const int DefaultPageSize = 20;
    private const int MaximumPageSize = 100;

    public async Task<PagedResponse<IrrigationListItemResponse>> ListAsync(
        IrrigationActor actor,
        IrrigationListQuery query,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ArgumentNullException.ThrowIfNull(query);

        var page = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = NormalizePageSize(query.PageSize);
        var now = DateTimeOffset.UtcNow;

        var totalCount = await store.CountAsync(actor.OrganizationId, query, now, cancellationToken);
        var items = await store.ListAsync(
            actor.OrganizationId,
            query,
            (page - 1) * pageSize,
            pageSize,
            now,
            cancellationToken);

        var responses = items.Select(item => ToListItemResponse(item, now)).ToArray();
        return new PagedResponse<IrrigationListItemResponse>(responses, page, pageSize, totalCount);
    }

    public async Task<IrrigationSummaryCountsResponse> GetSummaryCountsAsync(
        IrrigationActor actor,
        Guid? farmId,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var now = DateTimeOffset.UtcNow;
        return await store.GetSummaryCountsAsync(actor.OrganizationId, farmId, now, cancellationToken);
    }

    public async Task<IrrigationDetailsResponse> GetAsync(
        IrrigationActor actor,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        if (id == Guid.Empty)
        {
            throw new ResourceNotFoundException("The irrigation event was not found.");
        }

        var irrigation = await store.FindAsync(id, actor.OrganizationId, cancellationToken)
            ?? throw new ResourceNotFoundException("The irrigation event was not found.");

        var userIds = new List<Guid> { irrigation.CreatedBy };
        if (irrigation.UpdatedBy.HasValue)
        {
            userIds.Add(irrigation.UpdatedBy.Value);
        }

        var userNames = await store.GetUserNamesAsync(userIds.Distinct().ToArray(), cancellationToken);
        userNames.TryGetValue(irrigation.CreatedBy, out var createdByName);
        string? updatedByName = null;
        if (irrigation.UpdatedBy.HasValue)
        {
            userNames.TryGetValue(irrigation.UpdatedBy.Value, out updatedByName);
        }

        return ToDetailsResponse(irrigation, DateTimeOffset.UtcNow, createdByName, updatedByName);
    }

    public async Task<IrrigationDetailsResponse> CreateDraftAsync(
        IrrigationActor actor,
        CreateIrrigationDraftRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ArgumentNullException.ThrowIfNull(request);

        if (request.FarmId == Guid.Empty)
        {
            throw Validation("farmId", "A farm is required.");
        }

        if (request.FarmAreaId == Guid.Empty)
        {
            throw Validation("farmAreaId", "A farm area is required.");
        }

        await ValidateHierarchyAsync(
            actor.OrganizationId,
            request.FarmId,
            request.FarmAreaId,
            request.PlantationId,
            request.CropCycleId,
            request.CropCycleStageId,
            cancellationToken);

        await ValidateWaterAndUnitsAsync(actor.OrganizationId, request.PlannedWaterQuantity, request.PlannedWaterUnitId, "plannedWaterQuantity", "plannedWaterUnitId", cancellationToken);

        if (request.IrrigationMethodId.HasValue && request.IrrigationMethodId.Value != Guid.Empty)
        {
            await ValidateIrrigationMethodAsync(actor.OrganizationId, request.IrrigationMethodId.Value, cancellationToken);
        }

        var irrigation = new IrrigationEvent(
            organizationId: actor.OrganizationId,
            farmId: request.FarmId,
            farmAreaId: request.FarmAreaId,
            createdBy: actor.UserId,
            plantationId: request.PlantationId,
            cropCycleId: request.CropCycleId,
            cropCycleStageId: request.CropCycleStageId,
            irrigationMethodId: request.IrrigationMethodId,
            status: IrrigationStatus.Draft,
            plannedAt: request.PlannedAt,
            scheduledAt: request.ScheduledAt,
            plannedWaterQuantity: request.PlannedWaterQuantity,
            plannedWaterUnitId: request.PlannedWaterUnitId,
            notes: request.Notes);

        store.Add(irrigation);

        AddAudit(
            actor,
            irrigation,
            "Irrigation.DraftCreated",
            new
            {
                irrigation.Id,
                irrigation.FarmId,
                irrigation.FarmAreaId,
                irrigation.PlantationId,
                irrigation.CropCycleId,
                irrigation.CropCycleStageId,
                irrigation.PlannedAt,
                irrigation.PlannedWaterQuantity,
                irrigation.PlannedWaterUnitId,
                Status = irrigation.Status.ToString().ToUpperInvariant()
            },
            ipAddress);

        await store.SaveChangesAsync(cancellationToken);

        return await GetAsync(actor, irrigation.Id, cancellationToken);
    }

    public async Task<IrrigationDetailsResponse> UpdateDraftAsync(
        IrrigationActor actor,
        Guid id,
        UpdateIrrigationDraftRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ArgumentNullException.ThrowIfNull(request);

        if (id == Guid.Empty)
        {
            throw new ResourceNotFoundException("The irrigation event was not found.");
        }

        var irrigation = await store.FindAsync(id, actor.OrganizationId, cancellationToken)
            ?? throw new ResourceNotFoundException("The irrigation event was not found.");

        if (irrigation.Status != IrrigationStatus.Draft)
        {
            throw new ConflictException("Only draft irrigation events can be updated.");
        }

        ValidateConcurrencyToken(irrigation, request.ConcurrencyToken);

        if (request.FarmId == Guid.Empty)
        {
            throw Validation("farmId", "A farm is required.");
        }

        if (request.FarmAreaId == Guid.Empty)
        {
            throw Validation("farmAreaId", "A farm area is required.");
        }

        await ValidateHierarchyAsync(
            actor.OrganizationId,
            request.FarmId,
            request.FarmAreaId,
            request.PlantationId,
            request.CropCycleId,
            request.CropCycleStageId,
            cancellationToken);

        await ValidateWaterAndUnitsAsync(actor.OrganizationId, request.PlannedWaterQuantity, request.PlannedWaterUnitId, "plannedWaterQuantity", "plannedWaterUnitId", cancellationToken);

        if (request.IrrigationMethodId.HasValue && request.IrrigationMethodId.Value != Guid.Empty)
        {
            await ValidateIrrigationMethodAsync(actor.OrganizationId, request.IrrigationMethodId.Value, cancellationToken);
        }

        var now = DateTimeOffset.UtcNow;
        irrigation.UpdatePlanning(
            farmId: request.FarmId,
            farmAreaId: request.FarmAreaId,
            plantationId: request.PlantationId,
            cropCycleId: request.CropCycleId,
            cropCycleStageId: request.CropCycleStageId,
            plannedAt: request.PlannedAt,
            plannedWaterQuantity: request.PlannedWaterQuantity,
            plannedWaterUnitId: request.PlannedWaterUnitId,
            notes: request.Notes,
            now: now,
            updatedBy: actor.UserId);

        AddAudit(
            actor,
            irrigation,
            "Irrigation.DraftUpdated",
            new
            {
                irrigation.Id,
                irrigation.FarmId,
                irrigation.FarmAreaId,
                irrigation.PlantationId,
                irrigation.CropCycleId,
                irrigation.CropCycleStageId,
                irrigation.PlannedAt,
                irrigation.PlannedWaterQuantity,
                irrigation.PlannedWaterUnitId,
                Status = irrigation.Status.ToString().ToUpperInvariant()
            },
            ipAddress);

        await store.SaveChangesAsync(cancellationToken);

        return await GetAsync(actor, irrigation.Id, cancellationToken);
    }

    public async Task<IrrigationDetailsResponse> ScheduleAsync(
        IrrigationActor actor,
        Guid id,
        ScheduleIrrigationRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ArgumentNullException.ThrowIfNull(request);

        if (id == Guid.Empty)
        {
            throw new ResourceNotFoundException("The irrigation event was not found.");
        }

        var irrigation = await store.FindAsync(id, actor.OrganizationId, cancellationToken)
            ?? throw new ResourceNotFoundException("The irrigation event was not found.");

        ValidateConcurrencyToken(irrigation, request.ConcurrencyToken);

        if (irrigation.Status != IrrigationStatus.Draft)
        {
            throw new ConflictException($"Only draft irrigation events can be scheduled; current status is '{irrigation.Status}'.");
        }

        var now = DateTimeOffset.UtcNow;
        irrigation.Schedule(request.ScheduledAt, now, actor.UserId);

        AddAudit(
            actor,
            irrigation,
            "Irrigation.Scheduled",
            new
            {
                irrigation.Id,
                irrigation.FarmId,
                irrigation.ScheduledAt,
                Status = irrigation.Status.ToString().ToUpperInvariant()
            },
            ipAddress);

        await store.SaveChangesAsync(cancellationToken);

        return await GetAsync(actor, irrigation.Id, cancellationToken);
    }

    public async Task<IrrigationDetailsResponse> RescheduleAsync(
        IrrigationActor actor,
        Guid id,
        RescheduleIrrigationRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ArgumentNullException.ThrowIfNull(request);

        if (id == Guid.Empty)
        {
            throw new ResourceNotFoundException("The irrigation event was not found.");
        }

        var irrigation = await store.FindAsync(id, actor.OrganizationId, cancellationToken)
            ?? throw new ResourceNotFoundException("The irrigation event was not found.");

        ValidateConcurrencyToken(irrigation, request.ConcurrencyToken);

        if (irrigation.Status != IrrigationStatus.Scheduled)
        {
            throw new ConflictException("Only scheduled irrigation events can be rescheduled.");
        }

        var now = DateTimeOffset.UtcNow;
        irrigation.Reschedule(request.ScheduledAt, now, actor.UserId);

        AddAudit(
            actor,
            irrigation,
            "Irrigation.Rescheduled",
            new
            {
                irrigation.Id,
                irrigation.FarmId,
                irrigation.ScheduledAt,
                Status = irrigation.Status.ToString().ToUpperInvariant()
            },
            ipAddress);

        await store.SaveChangesAsync(cancellationToken);

        return await GetAsync(actor, irrigation.Id, cancellationToken);
    }

    public async Task<IrrigationDetailsResponse> StartAsync(
        IrrigationActor actor,
        Guid id,
        StartIrrigationRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ArgumentNullException.ThrowIfNull(request);

        if (id == Guid.Empty)
        {
            throw new ResourceNotFoundException("The irrigation event was not found.");
        }

        var irrigation = await store.FindAsync(id, actor.OrganizationId, cancellationToken)
            ?? throw new ResourceNotFoundException("The irrigation event was not found.");

        ValidateConcurrencyToken(irrigation, request.ConcurrencyToken);

        if (irrigation.Status != IrrigationStatus.Draft && irrigation.Status != IrrigationStatus.Scheduled)
        {
            throw new ConflictException($"Cannot start an irrigation event in '{irrigation.Status}' status; allowed transitions are from Draft or Scheduled.");
        }

        var now = DateTimeOffset.UtcNow;
        irrigation.Start(request.ActualStartedAt, now, actor.UserId);

        AddAudit(
            actor,
            irrigation,
            "Irrigation.Started",
            new
            {
                irrigation.Id,
                irrigation.FarmId,
                irrigation.ActualStartedAt,
                Status = irrigation.Status.ToString().ToUpperInvariant()
            },
            ipAddress);

        await store.SaveChangesAsync(cancellationToken);

        return await GetAsync(actor, irrigation.Id, cancellationToken);
    }

    public async Task<IrrigationDetailsResponse> CompleteAsync(
        IrrigationActor actor,
        Guid id,
        CompleteIrrigationRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ArgumentNullException.ThrowIfNull(request);

        if (id == Guid.Empty)
        {
            throw new ResourceNotFoundException("The irrigation event was not found.");
        }

        var irrigation = await store.FindAsync(id, actor.OrganizationId, cancellationToken)
            ?? throw new ResourceNotFoundException("The irrigation event was not found.");

        ValidateConcurrencyToken(irrigation, request.ConcurrencyToken);

        if (irrigation.Status == IrrigationStatus.Completed || irrigation.Status == IrrigationStatus.Cancelled)
        {
            throw new ConflictException($"Cannot complete an irrigation event with terminal status '{irrigation.Status}'.");
        }

        if (request.IrrigationMethodId == Guid.Empty)
        {
            throw Validation("irrigationMethodId", "An irrigation method is required to complete an irrigation event.");
        }

        await ValidateIrrigationMethodAsync(actor.OrganizationId, request.IrrigationMethodId, cancellationToken);
        await ValidateWaterAndUnitsAsync(actor.OrganizationId, request.ActualWaterQuantity, request.ActualWaterUnitId, "actualWaterQuantity", "actualWaterUnitId", cancellationToken);

        if (request.ActualDurationMinutes.HasValue && request.ActualDurationMinutes.Value <= 0)
        {
            throw Validation("actualDurationMinutes", "Duration must be greater than zero minutes.");
        }

        var effectiveStartedAt = request.ActualStartedAt ?? irrigation.ActualStartedAt ?? DateTimeOffset.UtcNow;
        if (effectiveStartedAt != default && request.ActualEndedAt.HasValue && request.ActualEndedAt.Value < effectiveStartedAt)
        {
            throw Validation("actualEndedAt", "Actual ended date/time cannot be earlier than actual started date/time.");
        }

        var now = DateTimeOffset.UtcNow;

        await store.ExecuteInTransactionAsync(async ct =>
        {
            irrigation.Complete(
                irrigationMethodId: request.IrrigationMethodId,
                actualStartedAt: effectiveStartedAt,
                actualEndedAt: request.ActualEndedAt,
                actualDurationMinutes: request.ActualDurationMinutes,
                actualWaterQuantity: request.ActualWaterQuantity,
                actualWaterUnitId: request.ActualWaterUnitId,
                notes: request.Notes,
                now: now,
                updatedBy: actor.UserId);

            AddAudit(
                actor,
                irrigation,
                "Irrigation.Completed",
                new
                {
                    irrigation.Id,
                    irrigation.FarmId,
                    irrigation.IrrigationMethodId,
                    irrigation.ActualStartedAt,
                    irrigation.ActualEndedAt,
                    irrigation.ActualDurationMinutes,
                    irrigation.ActualWaterQuantity,
                    irrigation.ActualWaterUnitId,
                    Status = irrigation.Status.ToString().ToUpperInvariant()
                },
                ipAddress);

            await store.SaveChangesAsync(ct);
            return true;
        }, cancellationToken);

        return await GetAsync(actor, irrigation.Id, cancellationToken);
    }

    public async Task<IrrigationDetailsResponse> RecordCompletedAsync(
        IrrigationActor actor,
        RecordCompletedIrrigationRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ArgumentNullException.ThrowIfNull(request);

        if (request.FarmId == Guid.Empty)
        {
            throw Validation("farmId", "A farm is required.");
        }

        if (request.FarmAreaId == Guid.Empty)
        {
            throw Validation("farmAreaId", "A farm area is required.");
        }

        if (request.IrrigationMethodId == Guid.Empty)
        {
            throw Validation("irrigationMethodId", "An irrigation method is required.");
        }

        await ValidateHierarchyAsync(
            actor.OrganizationId,
            request.FarmId,
            request.FarmAreaId,
            request.PlantationId,
            request.CropCycleId,
            request.CropCycleStageId,
            cancellationToken);

        await ValidateIrrigationMethodAsync(actor.OrganizationId, request.IrrigationMethodId, cancellationToken);
        await ValidateWaterAndUnitsAsync(actor.OrganizationId, request.ActualWaterQuantity, request.ActualWaterUnitId, "actualWaterQuantity", "actualWaterUnitId", cancellationToken);

        if (request.ActualDurationMinutes.HasValue && request.ActualDurationMinutes.Value <= 0)
        {
            throw Validation("actualDurationMinutes", "Duration must be greater than zero minutes.");
        }

        var effectiveStartedAt = request.ActualStartedAt ?? DateTimeOffset.UtcNow;
        if (request.ActualEndedAt.HasValue && request.ActualEndedAt.Value < effectiveStartedAt)
        {
            throw Validation("actualEndedAt", "Actual ended date/time cannot be earlier than actual started date/time.");
        }

        var now = DateTimeOffset.UtcNow;

        var irrigation = new IrrigationEvent(
            organizationId: actor.OrganizationId,
            farmId: request.FarmId,
            farmAreaId: request.FarmAreaId,
            createdBy: actor.UserId,
            plantationId: request.PlantationId,
            cropCycleId: request.CropCycleId,
            cropCycleStageId: request.CropCycleStageId,
            irrigationMethodId: request.IrrigationMethodId,
            status: IrrigationStatus.Completed,
            actualStartedAt: effectiveStartedAt,
            actualEndedAt: request.ActualEndedAt,
            actualDurationMinutes: request.ActualDurationMinutes,
            actualWaterQuantity: request.ActualWaterQuantity,
            actualWaterUnitId: request.ActualWaterUnitId,
            notes: request.Notes,
            completedAt: now);

        await store.ExecuteInTransactionAsync(async ct =>
        {
            store.Add(irrigation);

            AddAudit(
                actor,
                irrigation,
                "Irrigation.Completed",
                new
                {
                    irrigation.Id,
                    irrigation.FarmId,
                    irrigation.FarmAreaId,
                    irrigation.IrrigationMethodId,
                    irrigation.ActualStartedAt,
                    irrigation.ActualEndedAt,
                    irrigation.ActualDurationMinutes,
                    irrigation.ActualWaterQuantity,
                    irrigation.ActualWaterUnitId,
                    Status = irrigation.Status.ToString().ToUpperInvariant()
                },
                ipAddress);

            await store.SaveChangesAsync(ct);
            return true;
        }, cancellationToken);

        return await GetAsync(actor, irrigation.Id, cancellationToken);
    }

    public async Task<IrrigationDetailsResponse> CancelAsync(
        IrrigationActor actor,
        Guid id,
        CancelIrrigationRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ArgumentNullException.ThrowIfNull(request);

        if (id == Guid.Empty)
        {
            throw new ResourceNotFoundException("The irrigation event was not found.");
        }

        if (string.IsNullOrWhiteSpace(request.CancellationReason))
        {
            throw Validation("cancellationReason", "A cancellation reason is required.");
        }

        var irrigation = await store.FindAsync(id, actor.OrganizationId, cancellationToken)
            ?? throw new ResourceNotFoundException("The irrigation event was not found.");

        ValidateConcurrencyToken(irrigation, request.ConcurrencyToken);

        if (irrigation.Status == IrrigationStatus.Completed || irrigation.Status == IrrigationStatus.Cancelled)
        {
            throw new ConflictException($"Cannot cancel an irrigation event with terminal status '{irrigation.Status}'.");
        }

        var now = DateTimeOffset.UtcNow;
        irrigation.Cancel(request.CancellationReason, now, actor.UserId);

        AddAudit(
            actor,
            irrigation,
            "Irrigation.Cancelled",
            new
            {
                irrigation.Id,
                irrigation.FarmId,
                irrigation.CancellationReason,
                Status = irrigation.Status.ToString().ToUpperInvariant()
            },
            ipAddress);

        await store.SaveChangesAsync(cancellationToken);

        return await GetAsync(actor, irrigation.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<IrrigationMethodDto>> ListMethodsAsync(
        IrrigationActor actor,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var methods = await store.ListMethodsAsync(actor.OrganizationId, activeOnly, cancellationToken);
        return methods
            .Select(m => new IrrigationMethodDto(
                m.Id,
                m.OrganizationId,
                m.Code,
                m.Name,
                m.Description,
                m.DisplayOrder,
                m.IsSystem,
                m.IsActive))
            .ToArray();
    }

    private static void ValidateActor(IrrigationActor actor)
    {
        ArgumentNullException.ThrowIfNull(actor);
        if (actor.OrganizationId == Guid.Empty)
        {
            throw new ArgumentException("An organization identifier is required.", nameof(actor));
        }

        if (actor.UserId == Guid.Empty)
        {
            throw new ArgumentException("A user identifier is required.", nameof(actor));
        }
    }

    private async Task ValidateHierarchyAsync(
        Guid organizationId,
        Guid farmId,
        Guid farmAreaId,
        Guid? plantationId,
        Guid? cropCycleId,
        Guid? cropCycleStageId,
        CancellationToken cancellationToken)
    {
        var farm = await store.FindFarmAsync(farmId, organizationId, cancellationToken)
            ?? throw new ResourceNotFoundException("The farm was not found.");

        if (!farm.IsActive)
        {
            throw Validation("farmId", "The selected farm is inactive.");
        }

        var area = await store.FindFarmAreaAsync(farmAreaId, organizationId, cancellationToken)
            ?? throw new ResourceNotFoundException("The farm area was not found.");

        if (area.FarmId != farm.Id)
        {
            throw Validation("farmAreaId", "The farm area does not belong to the selected farm.");
        }

        if (!area.IsActive)
        {
            throw Validation("farmAreaId", "The selected farm area is inactive.");
        }

        if (plantationId.HasValue && plantationId.Value != Guid.Empty)
        {
            var plantation = await store.FindPlantationAsync(plantationId.Value, organizationId, cancellationToken)
                ?? throw new ResourceNotFoundException("The plantation was not found.");

            if (plantation.FarmId != farm.Id)
            {
                throw Validation("plantationId", "The plantation does not belong to the selected farm.");
            }

            if (plantation.FarmAreaId != area.Id)
            {
                throw Validation("plantationId", "The plantation does not belong to the selected farm area.");
            }
        }

        if (cropCycleId.HasValue && cropCycleId.Value != Guid.Empty)
        {
            if (!plantationId.HasValue || plantationId.Value == Guid.Empty)
            {
                throw Validation("cropCycleId", "A plantation is required when specifying a crop cycle.");
            }

            var cycle = await store.FindCropCycleAsync(cropCycleId.Value, organizationId, cancellationToken)
                ?? throw new ResourceNotFoundException("The crop cycle was not found.");

            if (cycle.PlantationId != plantationId.Value)
            {
                throw Validation("cropCycleId", "The crop cycle does not belong to the selected plantation.");
            }
        }

        if (cropCycleStageId.HasValue && cropCycleStageId.Value != Guid.Empty)
        {
            if (!cropCycleId.HasValue || cropCycleId.Value == Guid.Empty)
            {
                throw Validation("cropCycleStageId", "A crop cycle is required when specifying a crop cycle stage.");
            }

            var stage = await store.FindCropCycleStageAsync(cropCycleStageId.Value, organizationId, cancellationToken)
                ?? throw new ResourceNotFoundException("The crop cycle stage was not found.");

            if (stage.CropCycleId != cropCycleId.Value)
            {
                throw Validation("cropCycleStageId", "The crop cycle stage does not belong to the selected crop cycle.");
            }
        }
    }

    private async Task ValidateWaterAndUnitsAsync(
        Guid organizationId,
        decimal? quantity,
        Guid? unitId,
        string quantityParam,
        string unitParam,
        CancellationToken cancellationToken)
    {
        if (quantity.HasValue && quantity.Value <= 0)
        {
            throw Validation(quantityParam, "Water quantity must be greater than zero.");
        }

        if (quantity.HasValue && !unitId.HasValue)
        {
            throw Validation(unitParam, "A water unit is required when water quantity is specified.");
        }

        if (!quantity.HasValue && unitId.HasValue)
        {
            throw Validation(quantityParam, "A water quantity is required when water unit is specified.");
        }

        if (unitId.HasValue)
        {
            var unit = await store.FindUnitAsync(unitId.Value, organizationId, cancellationToken)
                ?? throw new ResourceNotFoundException("The selected water unit was not found.");

            if (!unit.IsActive)
            {
                throw Validation(unitParam, "The selected water unit is inactive.");
            }

            if (unit.UnitCategory != UnitCategory.Volume)
            {
                throw Validation(unitParam, "The selected water unit must be a volume unit.");
            }
        }
    }

    private async Task ValidateIrrigationMethodAsync(
        Guid organizationId,
        Guid methodId,
        CancellationToken cancellationToken)
    {
        var method = await store.FindIrrigationMethodAsync(methodId, organizationId, cancellationToken)
            ?? throw new ResourceNotFoundException("The selected irrigation method was not found.");

        if (!method.IsActive)
        {
            throw Validation("irrigationMethodId", "The selected irrigation method is inactive.");
        }
    }

    private void AddAudit(
        IrrigationActor actor,
        IrrigationEvent irrigation,
        string action,
        object details,
        string? ipAddress)
    {
        store.AddAuditLog(new AuditLog(
            action,
            irrigation.OrganizationId,
            actor.UserId,
            entityType: "Irrigation",
            entityId: irrigation.Id,
            details: JsonSerializer.SerializeToDocument(details),
            ipAddress: ipAddress));
    }

    private static bool ComputeIsOverdue(IrrigationEvent irrigation, DateTimeOffset now) =>
        irrigation.Status == IrrigationStatus.Scheduled &&
        irrigation.ScheduledAt.HasValue &&
        irrigation.ScheduledAt.Value < now;

    private static int NormalizePageSize(int pageSize) =>
        pageSize switch
        {
            < 1 => DefaultPageSize,
            > MaximumPageSize => MaximumPageSize,
            _ => pageSize
        };

    private static ValidationException Validation(string property, string message) =>
        new("One or more validation errors occurred.", new Dictionary<string, string[]>
        {
            [property] = [message]
        });

    private static IrrigationListItemResponse ToListItemResponse(IrrigationEvent irrigation, DateTimeOffset now) =>
        new(
            Id: irrigation.Id,
            FarmId: irrigation.FarmId,
            FarmName: irrigation.Farm?.Name ?? string.Empty,
            FarmAreaId: irrigation.FarmAreaId,
            FarmAreaName: irrigation.FarmArea?.Name ?? string.Empty,
            PlantationId: irrigation.PlantationId,
            PlantationName: irrigation.Plantation?.PlantationName,
            CropCycleId: irrigation.CropCycleId,
            CropCycleName: irrigation.CropCycle?.CycleName,
            CropCycleStageId: irrigation.CropCycleStageId,
            CropCycleStageName: irrigation.CropCycleStage?.StageName,
            Status: irrigation.Status,
            StatusName: irrigation.Status.ToString(),
            IsOverdue: ComputeIsOverdue(irrigation, now),
            PlannedAt: irrigation.PlannedAt,
            ScheduledAt: irrigation.ScheduledAt,
            ActualStartedAt: irrigation.ActualStartedAt,
            ActualEndedAt: irrigation.ActualEndedAt,
            ActualDurationMinutes: irrigation.ActualDurationMinutes,
            IrrigationMethodId: irrigation.IrrigationMethodId,
            IrrigationMethodName: irrigation.IrrigationMethod?.Name,
            PlannedWaterQuantity: irrigation.PlannedWaterQuantity,
            PlannedWaterUnitId: irrigation.PlannedWaterUnitId,
            PlannedWaterUnitName: irrigation.PlannedWaterUnit?.Name,
            ActualWaterQuantity: irrigation.ActualWaterQuantity,
            ActualWaterUnitId: irrigation.ActualWaterUnitId,
            ActualWaterUnitName: irrigation.ActualWaterUnit?.Name,
            CompletedAt: irrigation.CompletedAt,
            CreatedAt: irrigation.CreatedAt);

    private static IrrigationDetailsResponse ToDetailsResponse(
        IrrigationEvent irrigation,
        DateTimeOffset now,
        string? createdByName,
        string? updatedByName) =>
        new(
            Id: irrigation.Id,
            OrganizationId: irrigation.OrganizationId,
            FarmId: irrigation.FarmId,
            FarmName: irrigation.Farm?.Name ?? string.Empty,
            FarmAreaId: irrigation.FarmAreaId,
            FarmAreaName: irrigation.FarmArea?.Name ?? string.Empty,
            PlantationId: irrigation.PlantationId,
            PlantationName: irrigation.Plantation?.PlantationName,
            CropCycleId: irrigation.CropCycleId,
            CropCycleName: irrigation.CropCycle?.CycleName,
            CropCycleStageId: irrigation.CropCycleStageId,
            CropCycleStageName: irrigation.CropCycleStage?.StageName,
            Status: irrigation.Status,
            StatusName: irrigation.Status.ToString(),
            IsOverdue: ComputeIsOverdue(irrigation, now),
            PlannedAt: irrigation.PlannedAt,
            ScheduledAt: irrigation.ScheduledAt,
            ActualStartedAt: irrigation.ActualStartedAt,
            ActualEndedAt: irrigation.ActualEndedAt,
            ActualDurationMinutes: irrigation.ActualDurationMinutes,
            IrrigationMethodId: irrigation.IrrigationMethodId,
            IrrigationMethodName: irrigation.IrrigationMethod?.Name,
            PlannedWaterQuantity: irrigation.PlannedWaterQuantity,
            PlannedWaterUnitId: irrigation.PlannedWaterUnitId,
            PlannedWaterUnitName: irrigation.PlannedWaterUnit?.Name,
            PlannedWaterUnitSymbol: irrigation.PlannedWaterUnit?.Symbol,
            ActualWaterQuantity: irrigation.ActualWaterQuantity,
            ActualWaterUnitId: irrigation.ActualWaterUnitId,
            ActualWaterUnitName: irrigation.ActualWaterUnit?.Name,
            ActualWaterUnitSymbol: irrigation.ActualWaterUnit?.Symbol,
            Notes: irrigation.Notes,
            CancellationReason: irrigation.CancellationReason,
            CompletedAt: irrigation.CompletedAt,
            CreatedAt: irrigation.CreatedAt,
            CreatedBy: irrigation.CreatedBy,
            UpdatedAt: irrigation.UpdatedAt,
            UpdatedBy: irrigation.UpdatedBy,
            CreatedByName: createdByName,
            UpdatedByName: updatedByName,
            ConcurrencyToken: ComputeConcurrencyToken(irrigation));

    private static string ComputeConcurrencyToken(IrrigationEvent irrigation) =>
        (irrigation.UpdatedAt ?? irrigation.CreatedAt).ToString("O");

    private static void ValidateConcurrencyToken(IrrigationEvent irrigation, string? suppliedToken)
    {
        if (string.IsNullOrWhiteSpace(suppliedToken)) return;

        var currentToken = ComputeConcurrencyToken(irrigation);
        if (!string.Equals(currentToken, suppliedToken.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException("The irrigation event was modified by another operation. Please refresh the latest state and retry.");
        }
    }
}
