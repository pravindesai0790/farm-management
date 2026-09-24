namespace FarmManagement.Domain.Entities;

public sealed class CropLifecycleStage
{
    private CropLifecycleStage()
    {
        StageName = string.Empty;
    }

    public CropLifecycleStage(
        Guid lifecycleTemplateId,
        string stageName,
        int sequenceNumber,
        int? expectedDurationDays = null,
        string? description = null)
    {
        if (lifecycleTemplateId == Guid.Empty) throw new ArgumentException("A lifecycle template is required.", nameof(lifecycleTemplateId));
        ValidateStageName(stageName);
        ValidateSequenceNumber(sequenceNumber);
        ValidateExpectedDurationDays(expectedDurationDays);

        Id = Guid.NewGuid();
        LifecycleTemplateId = lifecycleTemplateId;
        StageName = stageName.Trim();
        SequenceNumber = sequenceNumber;
        ExpectedDurationDays = expectedDurationDays;
        Description = NormalizeOptional(description);
        IsActive = true;
    }

    public Guid Id { get; private set; }
    public Guid LifecycleTemplateId { get; private set; }
    public string StageName { get; private set; }
    public int SequenceNumber { get; private set; }
    public int? ExpectedDurationDays { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    public CropLifecycleTemplate LifecycleTemplate { get; private set; } = null!;

    public void Update(
        string stageName,
        int sequenceNumber,
        int? expectedDurationDays,
        string? description)
    {
        ValidateStageName(stageName);
        ValidateSequenceNumber(sequenceNumber);
        ValidateExpectedDurationDays(expectedDurationDays);

        StageName = stageName.Trim();
        SequenceNumber = sequenceNumber;
        ExpectedDurationDays = expectedDurationDays;
        Description = NormalizeOptional(description);
    }

    public bool Activate()
    {
        if (IsActive) return false;
        IsActive = true;
        return true;
    }

    public bool Deactivate()
    {
        if (!IsActive) return false;
        IsActive = false;
        return true;
    }

    private static void ValidateStageName(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A lifecycle stage name is required.", nameof(value));
    }

    private static void ValidateSequenceNumber(int value)
    {
        if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), "A lifecycle stage sequence number must be greater than zero.");
    }

    private static void ValidateExpectedDurationDays(int? value)
    {
        if (value is not null && value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Expected duration days must be greater than zero.");
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
