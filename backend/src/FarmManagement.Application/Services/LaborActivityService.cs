using System.Text.Json;
using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.LaborActivities;
using FarmManagement.Application.Interfaces.LaborActivities;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.Services;

public sealed class LaborActivityService(ILaborActivityStore store) : ILaborActivityService
{
    private const int DefaultPageSize = 20;
    private const int MaximumPageSize = 100;

    public async Task<PagedResponse<LaborActivityResponse>> ListAsync(
        LaborActivityActor actor,
        int page,
        int pageSize,
        Guid? farmId,
        Guid? farmAreaId,
        Guid? plantationId,
        Guid? cropCycleId,
        Guid? activityTypeId,
        DateOnly? fromDate,
        DateOnly? toDate,
        string? status,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        if (page < 1) throw Validation("page", "Page must be at least 1.");
        pageSize = NormalizePageSize(pageSize);

        if (farmId == Guid.Empty) throw Validation("farmId", "Farm identifier must be valid.");
        if (farmAreaId == Guid.Empty) throw Validation("farmAreaId", "Farm area identifier must be valid.");
        if (plantationId == Guid.Empty) throw Validation("plantationId", "Plantation identifier must be valid.");
        if (cropCycleId == Guid.Empty) throw Validation("cropCycleId", "Crop cycle identifier must be valid.");
        if (activityTypeId == Guid.Empty) throw Validation("activityTypeId", "Labor activity type identifier must be valid.");
        if (fromDate.HasValue && toDate.HasValue && fromDate.Value > toDate.Value)
        {
            throw Validation("toDate", "To date cannot be earlier than from date.");
        }

        var parsedStatus = ParseStatus(status);
        var currency = await store.ResolveOrganizationCurrencyAsync(actor.OrganizationId, cancellationToken);

        var (items, totalCount) = await store.ListPagedAsync(
            actor.OrganizationId,
            farmId,
            farmAreaId,
            plantationId,
            cropCycleId,
            activityTypeId,
            fromDate,
            toDate,
            parsedStatus,
            checked((page - 1) * pageSize),
            pageSize,
            cancellationToken);

        var responses = items.Select(item => ToResponse(item, currency)).ToArray();
        return new PagedResponse<LaborActivityResponse>(responses, page, pageSize, totalCount);
    }

    public async Task<LaborActivityResponse> GetAsync(
        LaborActivityActor actor,
        Guid activityId,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var activity = await store.FindAsync(activityId, actor.OrganizationId, cancellationToken)
            ?? throw new ResourceNotFoundException("The labor activity was not found.");

        var currency = await store.ResolveOrganizationCurrencyAsync(actor.OrganizationId, cancellationToken);
        return ToResponse(activity, currency);
    }

    public async Task<LaborActivityResponse> CreateAsync(
        LaborActivityActor actor,
        CreateLaborActivityRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var values = ReadValues(request);

        return await store.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            var (farm, effectiveAreaId, activityType) = await ValidateHierarchyAsync(
                actor.OrganizationId,
                values.FarmId,
                values.FarmAreaId,
                values.PlantationId,
                values.CropCycleId,
                values.LaborActivityTypeId,
                transactionCancellationToken);

            var currency = await store.ResolveOrganizationCurrencyAsync(actor.OrganizationId, transactionCancellationToken);

            var activity = new LaborActivity(
                actor.OrganizationId,
                values.ActivityDate,
                farm.Id,
                activityType.Id,
                values.WorkerCount,
                actor.UserId,
                effectiveAreaId,
                values.PlantationId,
                values.CropCycleId,
                values.TotalWorkingHours,
                values.CostAmount,
                currencyId: null,
                description: values.Description,
                status: values.Status);

            store.Add(activity);

            AddAudit(actor, activity, "LaborActivity.Created", new
            {
                activity.Id,
                activity.ActivityDate,
                activity.FarmId,
                activity.FarmAreaId,
                activity.PlantationId,
                activity.CropCycleId,
                activity.LaborActivityTypeId,
                activity.WorkerCount,
                activity.TotalWorkingHours,
                activity.CostAmount,
                Status = activity.Status.ToString().ToUpperInvariant(),
                activity.Description
            }, ipAddress);

            await store.SaveChangesAsync(transactionCancellationToken);

            var created = await store.FindAsync(activity.Id, actor.OrganizationId, transactionCancellationToken)
                ?? activity;

            return ToResponse(created, currency);
        }, cancellationToken);
    }

    public async Task<LaborActivityResponse> UpdateAsync(
        LaborActivityActor actor,
        Guid activityId,
        UpdateLaborActivityRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var values = ReadValues(request);

        return await store.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            var activity = await store.LockAsync(activityId, actor.OrganizationId, transactionCancellationToken)
                ?? throw new ResourceNotFoundException("The labor activity was not found.");

            if (activity.Status == LaborActivityStatus.Cancelled)
            {
                throw new ConflictException("Cancelled labor activities cannot be modified.");
            }

            var (farm, effectiveAreaId, activityType) = await ValidateHierarchyAsync(
                actor.OrganizationId,
                values.FarmId,
                values.FarmAreaId,
                values.PlantationId,
                values.CropCycleId,
                values.LaborActivityTypeId,
                transactionCancellationToken);

            var now = DateTimeOffset.UtcNow;
            var previous = new
            {
                activity.ActivityDate,
                activity.FarmId,
                activity.FarmAreaId,
                activity.PlantationId,
                activity.CropCycleId,
                activity.LaborActivityTypeId,
                activity.WorkerCount,
                activity.TotalWorkingHours,
                activity.CostAmount,
                Status = activity.Status.ToString().ToUpperInvariant(),
                activity.Description
            };

            activity.Update(
                values.ActivityDate,
                farm.Id,
                effectiveAreaId,
                values.PlantationId,
                values.CropCycleId,
                activityType.Id,
                values.WorkerCount,
                values.TotalWorkingHours,
                values.CostAmount,
                currencyId: null,
                values.Description,
                now,
                actor.UserId);

            AddAudit(actor, activity, "LaborActivity.Updated", new
            {
                Previous = previous,
                Current = new
                {
                    activity.ActivityDate,
                    activity.FarmId,
                    activity.FarmAreaId,
                    activity.PlantationId,
                    activity.CropCycleId,
                    activity.LaborActivityTypeId,
                    activity.WorkerCount,
                    activity.TotalWorkingHours,
                    activity.CostAmount,
                    Status = activity.Status.ToString().ToUpperInvariant(),
                    activity.Description
                }
            }, ipAddress);

            await store.SaveChangesAsync(transactionCancellationToken);

            var updated = await store.FindAsync(activity.Id, actor.OrganizationId, transactionCancellationToken)
                ?? activity;

            var currency = await store.ResolveOrganizationCurrencyAsync(actor.OrganizationId, transactionCancellationToken);
            return ToResponse(updated, currency);
        }, cancellationToken);
    }

    public async Task CancelAsync(
        LaborActivityActor actor,
        Guid activityId,
        CancelLaborActivityRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        if (request is null || string.IsNullOrWhiteSpace(request.Reason))
        {
            throw Validation("reason", "A cancellation reason is required.");
        }

        await store.ExecuteInTransactionAsync<object?>(async transactionCancellationToken =>
        {
            var activity = await store.LockAsync(activityId, actor.OrganizationId, transactionCancellationToken)
                ?? throw new ResourceNotFoundException("The labor activity was not found.");

            if (activity.Status == LaborActivityStatus.Cancelled)
            {
                throw new ConflictException("The labor activity is already cancelled.");
            }

            var previousStatus = activity.Status;
            var now = DateTimeOffset.UtcNow;
            activity.Cancel(now, actor.UserId);

            AddAudit(actor, activity, "LaborActivity.Cancelled", new
            {
                CancellationReason = request.Reason.Trim(),
                CancelledBy = actor.UserId,
                CancelledAt = now,
                PreviousStatus = previousStatus.ToString().ToUpperInvariant(),
                NewStatus = activity.Status.ToString().ToUpperInvariant()
            }, ipAddress);

            await store.SaveChangesAsync(transactionCancellationToken);
            return null;
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<NamedReferenceResponse>> ListTypesAsync(
        LaborActivityActor actor,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var types = await store.ListLaborActivityTypesAsync(actor.OrganizationId, cancellationToken);
        return types.Select(t => new NamedReferenceResponse(t.Id, t.Name)).ToList();
    }

    private async Task<(Farm Farm, Guid? EffectiveAreaId, LaborActivityType ActivityType)> ValidateHierarchyAsync(
        Guid organizationId,
        Guid farmId,
        Guid? farmAreaId,
        Guid? plantationId,
        Guid? cropCycleId,
        Guid laborActivityTypeId,
        CancellationToken cancellationToken)
    {
        var farm = await store.FindFarmAsync(farmId, organizationId, cancellationToken)
            ?? throw new ResourceNotFoundException("The farm was not found.");

        if (!farm.IsActive)
        {
            throw Validation("farmId", "The selected farm is inactive.");
        }

        Guid? effectiveAreaId = farmAreaId;

        if (farmAreaId.HasValue)
        {
            var area = await store.FindFarmAreaAsync(farmAreaId.Value, organizationId, cancellationToken)
                ?? throw new ResourceNotFoundException("The farm area was not found.");

            if (area.FarmId != farm.Id)
            {
                throw Validation("farmAreaId", "The farm area does not belong to the selected farm.");
            }

            if (!area.IsActive)
            {
                throw Validation("farmAreaId", "The selected farm area is inactive.");
            }
        }

        if (plantationId.HasValue)
        {
            var plantation = await store.FindPlantationAsync(plantationId.Value, organizationId, cancellationToken)
                ?? throw new ResourceNotFoundException("The plantation was not found.");

            if (plantation.FarmId != farm.Id)
            {
                throw Validation("plantationId", "The plantation does not belong to the selected farm.");
            }

            if (farmAreaId.HasValue && plantation.FarmAreaId != farmAreaId.Value)
            {
                throw Validation("plantationId", "The plantation does not belong to the selected farm area.");
            }

            effectiveAreaId ??= plantation.FarmAreaId;
        }

        if (cropCycleId.HasValue)
        {
            if (!plantationId.HasValue)
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

        var activityType = await store.FindLaborActivityTypeAsync(laborActivityTypeId, organizationId, cancellationToken)
            ?? throw new ResourceNotFoundException("The labor activity type was not found.");

        if (!activityType.IsActive)
        {
            throw Validation("laborActivityTypeId", "The selected labor activity type is inactive.");
        }

        return (farm, effectiveAreaId, activityType);
    }

    private static LaborActivityResponse ToResponse(LaborActivity activity, string currency)
    {
        var farm = activity.Farm != null
            ? new NamedReferenceResponse(activity.Farm.Id, activity.Farm.Name)
            : new NamedReferenceResponse(activity.FarmId, string.Empty);

        var farmArea = activity.FarmArea != null
            ? new NamedReferenceResponse(activity.FarmArea.Id, activity.FarmArea.Name)
            : activity.FarmAreaId.HasValue
                ? new NamedReferenceResponse(activity.FarmAreaId.Value, string.Empty)
                : null;

        var plantation = activity.Plantation != null
            ? new NamedReferenceResponse(activity.Plantation.Id, activity.Plantation.PlantationName)
            : activity.PlantationId.HasValue
                ? new NamedReferenceResponse(activity.PlantationId.Value, string.Empty)
                : null;

        var cropCycle = activity.CropCycle != null
            ? new NamedReferenceResponse(activity.CropCycle.Id, activity.CropCycle.CycleName)
            : activity.CropCycleId.HasValue
                ? new NamedReferenceResponse(activity.CropCycleId.Value, string.Empty)
                : null;

        var activityType = activity.LaborActivityType != null
            ? new NamedReferenceResponse(activity.LaborActivityType.Id, activity.LaborActivityType.Name)
            : new NamedReferenceResponse(activity.LaborActivityTypeId, string.Empty);

        return new LaborActivityResponse(
            activity.Id,
            activity.ActivityDate,
            farm,
            farmArea,
            plantation,
            cropCycle,
            activityType,
            activity.WorkerCount,
            activity.TotalWorkingHours,
            activity.CostAmount,
            currency,
            activity.Status.ToString().ToUpperInvariant(),
            activity.Description,
            activity.CreatedAt,
            activity.UpdatedAt);
    }

    private static ActivityValues ReadValues(CreateLaborActivityRequest? request)
    {
        if (request is null) throw Validation("request", "A request body is required.");
        return ReadValues(
            request.ActivityDate,
            request.FarmId,
            request.FarmAreaId,
            request.PlantationId,
            request.CropCycleId,
            request.LaborActivityTypeId,
            request.Description,
            request.WorkerCount,
            request.TotalWorkingHours,
            request.CostAmount,
            request.Status);
    }

    private static ActivityValues ReadValues(UpdateLaborActivityRequest? request)
    {
        if (request is null) throw Validation("request", "A request body is required.");
        return ReadValues(
            request.ActivityDate,
            request.FarmId,
            request.FarmAreaId,
            request.PlantationId,
            request.CropCycleId,
            request.LaborActivityTypeId,
            request.Description,
            request.WorkerCount,
            request.TotalWorkingHours,
            request.CostAmount,
            request.Status);
    }

    private static ActivityValues ReadValues(
        DateOnly? activityDate,
        Guid? farmId,
        Guid? farmAreaId,
        Guid? plantationId,
        Guid? cropCycleId,
        Guid? laborActivityTypeId,
        string? description,
        int? workerCount,
        decimal? totalWorkingHours,
        decimal? costAmount,
        string? status)
    {
        if (activityDate is null) throw Validation("activityDate", "Activity date is required.");
        if (!farmId.HasValue || farmId.Value == Guid.Empty) throw Validation("farmId", "Farm is required.");
        if (!laborActivityTypeId.HasValue || laborActivityTypeId.Value == Guid.Empty) throw Validation("laborActivityTypeId", "Labor activity type is required.");
        if (farmAreaId == Guid.Empty) throw Validation("farmAreaId", "Farm area identifier must be valid.");
        if (plantationId == Guid.Empty) throw Validation("plantationId", "Plantation identifier must be valid.");
        if (cropCycleId == Guid.Empty) throw Validation("cropCycleId", "Crop cycle identifier must be valid.");

        if (!workerCount.HasValue || workerCount.Value <= 0) throw Validation("workerCount", "Worker count is required and must be greater than zero.");
        if (totalWorkingHours is <= 0) throw Validation("totalWorkingHours", "Total working hours must be greater than zero.");
        if (costAmount is < 0) throw Validation("costAmount", "Cost amount cannot be negative.");

        var parsedStatus = LaborActivityStatus.Completed;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<LaborActivityStatus>(status.Trim(), true, out var parsed))
            {
                throw Validation("status", "Status must be DRAFT, COMPLETED, or CANCELLED.");
            }

            parsedStatus = parsed;
        }

        return new ActivityValues(
            activityDate.Value,
            farmId.Value,
            farmAreaId,
            plantationId,
            cropCycleId,
            laborActivityTypeId.Value,
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            workerCount.Value,
            totalWorkingHours,
            costAmount,
            parsedStatus);
    }

    private static void ValidateActor(LaborActivityActor actor)
    {
        if (actor.UserId == Guid.Empty || actor.OrganizationId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("The access token does not contain a valid user scope.");
        }
    }

    private void AddAudit(LaborActivityActor actor, LaborActivity activity, string action, object details, string? ipAddress) =>
        store.AddAuditLog(new AuditLog(
            action,
            activity.OrganizationId,
            actor.UserId,
            "LaborActivity",
            activity.Id,
            JsonSerializer.SerializeToDocument(details),
            ipAddress));

    private static LaborActivityStatus? ParseStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return null;
        if (!Enum.TryParse<LaborActivityStatus>(status.Trim(), true, out var parsed))
        {
            throw Validation("status", "Status must be DRAFT, COMPLETED, or CANCELLED.");
        }
        return parsed;
    }

    private static ValidationException Validation(string fieldName, string message) =>
        new("Validation failed", new Dictionary<string, string[]> { [fieldName] = [message] });

    private static int NormalizePageSize(int pageSize) =>
        pageSize == 0 ? DefaultPageSize : pageSize is < 1 or > MaximumPageSize
            ? throw Validation("pageSize", $"Page size must be between 1 and {MaximumPageSize}.")
            : pageSize;

    private sealed record ActivityValues(
        DateOnly ActivityDate,
        Guid FarmId,
        Guid? FarmAreaId,
        Guid? PlantationId,
        Guid? CropCycleId,
        Guid LaborActivityTypeId,
        string? Description,
        int WorkerCount,
        decimal? TotalWorkingHours,
        decimal? CostAmount,
        LaborActivityStatus Status);
}
