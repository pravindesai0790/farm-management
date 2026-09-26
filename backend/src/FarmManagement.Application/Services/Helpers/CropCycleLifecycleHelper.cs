using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.Services.Helpers;

public static class CropCycleLifecycleHelper
{
    public static IReadOnlyList<CropLifecycleStage> ValidateAndGetActiveTemplateStages(
        CropLifecycleTemplate? template,
        Guid organizationId,
        Guid plantationCropId)
    {
        if (template is null)
        {
            throw Validation("lifecycleTemplateId", "The selected lifecycle template was not found.");
        }

        if (!template.IsSystem && template.OrganizationId != organizationId)
        {
            throw Validation("lifecycleTemplateId", "The selected lifecycle template was not found.");
        }

        if (template.CropId != plantationCropId)
        {
            throw Validation("lifecycleTemplateId", "The lifecycle template does not match the plantation crop.");
        }

        if (!template.IsActive)
        {
            throw Validation("lifecycleTemplateId", "The selected lifecycle template is inactive.");
        }

        var activeStages = template.Stages
            .Where(s => s.IsActive)
            .OrderBy(s => s.SequenceNumber)
            .ToList();

        if (activeStages.Count == 0)
        {
            throw Validation("stages", "The lifecycle template has no active stages.");
        }

        if (activeStages.Any(s => string.IsNullOrWhiteSpace(s.StageName)))
        {
            throw Validation("stages", "One or more lifecycle template stages have an invalid stage name.");
        }

        if (activeStages.Any(s => s.ExpectedDurationDays is <= 0))
        {
            throw Validation("stages", "Expected duration days must be greater than zero.");
        }

        var sequenceSet = new HashSet<int>();
        foreach (var stage in activeStages)
        {
            if (stage.SequenceNumber <= 0)
            {
                throw Validation("sequenceNumber", "Sequence numbers must be greater than zero.");
            }

            if (!sequenceSet.Add(stage.SequenceNumber))
            {
                throw Validation("sequenceNumber", $"Duplicate sequence number {stage.SequenceNumber} in lifecycle template stages.");
            }
        }

        return activeStages;
    }

    public static IReadOnlyList<(DateOnly? PlannedStartDate, DateOnly? PlannedEndDate)> CalculateStagePlannedDates(
        DateOnly basePlannedStartDate,
        IReadOnlyList<CropLifecycleStage> stages)
    {
        var result = new List<(DateOnly? PlannedStartDate, DateOnly? PlannedEndDate)>(stages.Count);
        DateOnly? currentPlannedStart = basePlannedStartDate;

        for (var i = 0; i < stages.Count; i++)
        {
            var stage = stages[i];
            DateOnly? plannedStart = currentPlannedStart;
            DateOnly? plannedEnd = null;

            if (plannedStart is not null && stage.ExpectedDurationDays is > 0)
            {
                plannedEnd = plannedStart.Value.AddDays(stage.ExpectedDurationDays.Value);
            }

            result.Add((plannedStart, plannedEnd));
            currentPlannedStart = plannedEnd;
        }

        return result;
    }

    public static DateOnly? CalculateExpectedEndDate(DateOnly plannedStartDate, CropLifecycleTemplate? template)
    {
        if (template is null || template.Stages.Count == 0) return null;
        var activeStages = template.Stages.Where(s => s.IsActive).ToList();
        if (activeStages.Count == 0) return null;
        if (activeStages.Any(s => s.ExpectedDurationDays is null or <= 0)) return null;

        var totalDays = activeStages.Sum(s => s.ExpectedDurationDays!.Value);
        return totalDays > 0 ? plannedStartDate.AddDays(totalDays) : null;
    }

    public static IReadOnlyList<CropCycleStage> CreateSnapshotStages(
        CropCycle cycle,
        IReadOnlyList<CropLifecycleStage> activeStages,
        IReadOnlyList<(DateOnly? PlannedStartDate, DateOnly? PlannedEndDate)> plannedDates,
        DateOnly actualStartDate,
        Guid userId)
    {
        var result = new List<CropCycleStage>(activeStages.Count);

        for (var i = 0; i < activeStages.Count; i++)
        {
            var templateStage = activeStages[i];
            var (plannedStart, plannedEnd) = plannedDates[i];
            var isFirst = i == 0;

            var cycleStage = new CropCycleStage(
                cycle.Id,
                templateStage.Id,
                templateStage.StageName,
                templateStage.SequenceNumber,
                templateStage.ExpectedDurationDays,
                plannedStart,
                plannedEnd,
                userId,
                isFirst ? CropCycleStageStatus.InProgress : CropCycleStageStatus.NotStarted,
                isFirst ? actualStartDate : null,
                actualEndDate: null,
                notes: null);

            result.Add(cycleStage);
        }

        return result;
    }

    public static ValidationException Validation(string fieldName, string message) =>
        new("Validation failed", new Dictionary<string, string[]> { [fieldName] = [message] });
}
