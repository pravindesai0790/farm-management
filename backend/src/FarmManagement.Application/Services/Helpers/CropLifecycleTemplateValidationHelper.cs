using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.Crops;

namespace FarmManagement.Application.Services.Helpers;

public static class CropLifecycleTemplateValidationHelper
{
    private const int DefaultPageSize = 20;
    private const int MaximumPageSize = 100;

    public static TemplateValues ReadTemplateValues(CreateCropLifecycleTemplateRequest? request) =>
        request is null
            ? throw Validation("request", "A request body is required.")
            : ReadTemplateValues(request.CropId, request.Name, request.Description, request.IsDefault);

    public static TemplateValues ReadTemplateValues(UpdateCropLifecycleTemplateRequest? request) =>
        request is null
            ? throw Validation("request", "A request body is required.")
            : ReadTemplateValues(request.CropId, request.Name, request.Description, request.IsDefault);

    public static TemplateValues ReadTemplateValues(Guid? cropId, string? name, string? description, bool isDefault)
    {
        if (cropId is null || cropId == Guid.Empty) throw Validation("cropId", "Crop is required.");
        if (string.IsNullOrWhiteSpace(name)) throw Validation("name", "Name is required.");
        if (name.Trim().Length > 150) throw Validation("name", "Name cannot exceed 150 characters.");
        if (description?.Trim().Length > 2000) throw Validation("description", "Description cannot exceed 2000 characters.");
        return new TemplateValues(cropId.Value, name.Trim(), NormalizeOptional(description), isDefault);
    }

    public static StageValues ReadStageValues(CreateCropLifecycleStageRequest? request) =>
        request is null
            ? throw Validation("request", "A request body is required.")
            : ReadStageValues(request.StageName, request.SequenceNumber, request.ExpectedDurationDays, request.Description);

    public static StageValues ReadStageValues(UpdateCropLifecycleStageRequest? request) =>
        request is null
            ? throw Validation("request", "A request body is required.")
            : ReadStageValues(request.StageName, request.SequenceNumber, request.ExpectedDurationDays, request.Description);

    public static StageValues ReadStageValues(string? stageName, int sequenceNumber, int? expectedDurationDays, string? description)
    {
        if (string.IsNullOrWhiteSpace(stageName)) throw Validation("stageName", "Stage name is required.");
        if (stageName.Trim().Length > 150) throw Validation("stageName", "Stage name cannot exceed 150 characters.");
        if (sequenceNumber <= 0) throw Validation("sequenceNumber", "Sequence number must be greater than zero.");
        if (expectedDurationDays is <= 0) throw Validation("expectedDurationDays", "Expected duration days must be greater than zero.");
        if (description?.Trim().Length > 2000) throw Validation("description", "Description cannot exceed 2000 characters.");
        return new StageValues(stageName.Trim(), sequenceNumber, expectedDurationDays, NormalizeOptional(description));
    }

    public static IReadOnlyList<StageValues> ReadBatchStageValues(IReadOnlyList<CreateCropLifecycleStageRequest>? stages)
    {
        if (stages is null || stages.Count == 0) return Array.Empty<StageValues>();

        var result = new List<StageValues>(stages.Count);
        var sequences = new HashSet<int>();

        for (var i = 0; i < stages.Count; i++)
        {
            var stage = stages[i];
            var parsed = ReadStageValues(stage);

            if (!sequences.Add(parsed.SequenceNumber))
            {
                throw Validation($"stages[{i}].sequenceNumber", $"Duplicate sequence number {parsed.SequenceNumber} in stage list.");
            }

            result.Add(parsed);
        }

        return result;
    }

    public static int NormalizePageSize(int value) => value == 0
        ? DefaultPageSize
        : value is < 1 or > MaximumPageSize
            ? throw Validation("pageSize", $"Page size must be between 1 and {MaximumPageSize}.")
            : value;

    public static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static ValidationException Validation(string fieldName, string message) =>
        new("Validation failed", new Dictionary<string, string[]> { [fieldName] = [message] });

    public sealed record TemplateValues(Guid CropId, string Name, string? Description, bool IsDefault);
    public sealed record StageValues(string StageName, int SequenceNumber, int? ExpectedDurationDays, string? Description);
}
