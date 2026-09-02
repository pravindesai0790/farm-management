namespace FarmManagement.Domain.Entities;

public sealed class CropLifecycleStage
{
    private CropLifecycleStage()
    {
        StageCode = string.Empty;
        StageName = string.Empty;
    }

    public CropLifecycleStage(
        Guid lifecycleTemplateId,
        string stageCode,
        string stageName,
        int sequenceNumber,
        string? description = null)
    {
        if (lifecycleTemplateId == Guid.Empty) throw new ArgumentException("A lifecycle template is required.", nameof(lifecycleTemplateId));
        ValidateStageCode(stageCode);
        ValidateStageName(stageName);
        ValidateSequenceNumber(sequenceNumber);

        Id = Guid.NewGuid();
        LifecycleTemplateId = lifecycleTemplateId;
        StageCode = NormalizeCode(stageCode);
        StageName = stageName.Trim();
        SequenceNumber = sequenceNumber;
        Description = NormalizeOptional(description);
        IsActive = true;
    }

    public Guid Id { get; private set; }
    public Guid LifecycleTemplateId { get; private set; }
    public string StageCode { get; private set; }
    public string StageName { get; private set; }
    public int SequenceNumber { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    public CropLifecycleTemplate LifecycleTemplate { get; private set; } = null!;

    public void Update(
        string stageCode,
        string stageName,
        int sequenceNumber,
        string? description)
    {
        ValidateStageCode(stageCode);
        ValidateStageName(stageName);
        ValidateSequenceNumber(sequenceNumber);

        StageCode = NormalizeCode(stageCode);
        StageName = stageName.Trim();
        SequenceNumber = sequenceNumber;
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

    private static void ValidateStageCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A lifecycle stage code is required.", nameof(value));
    }

    private static void ValidateStageName(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A lifecycle stage name is required.", nameof(value));
    }

    private static void ValidateSequenceNumber(int value)
    {
        if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), "A lifecycle stage sequence number must be greater than zero.");
    }

    private static string NormalizeCode(string value) => value.Trim().ToUpperInvariant();

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
