namespace FarmManagement.Domain.Entities;

public sealed class CropLifecycleTemplate
{
    private CropLifecycleTemplate()
    {
        Name = string.Empty;
    }

    public CropLifecycleTemplate(
        Guid? organizationId,
        Guid cropId,
        string name,
        bool isDefault = false,
        bool isSystem = false,
        string? description = null,
        Guid? createdBy = null)
    {
        if (isSystem != (organizationId is null))
        {
            throw new ArgumentException(
                isSystem
                    ? "A system lifecycle template cannot belong to an organization."
                    : "An organization is required for an organization-specific lifecycle template.",
                nameof(organizationId));
        }

        if (cropId == Guid.Empty) throw new ArgumentException("A crop is required.", nameof(cropId));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A lifecycle template name is required.", nameof(name));

        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        CropId = cropId;
        Name = name.Trim();
        Description = NormalizeOptional(description);
        IsDefault = isDefault;
        IsSystem = isSystem;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
        CreatedBy = createdBy;
    }

    public Guid Id { get; private set; }
    public Guid? OrganizationId { get; private set; }
    public Guid CropId { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsSystem { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public Organization? Organization { get; private set; }
    public Crop Crop { get; private set; } = null!;
    public ICollection<CropLifecycleStage> Stages { get; private set; } = new List<CropLifecycleStage>();

    public void Update(
        Guid cropId,
        string name,
        bool isDefault,
        string? description,
        DateTimeOffset now,
        Guid updatedBy)
    {
        if (cropId == Guid.Empty) throw new ArgumentException("A crop is required.", nameof(cropId));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A lifecycle template name is required.", nameof(name));

        CropId = cropId;
        Name = name.Trim();
        Description = NormalizeOptional(description);
        IsDefault = isDefault;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    public void SetDefault(bool isDefault, DateTimeOffset now, Guid updatedBy)
    {
        IsDefault = isDefault;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    public bool Activate(DateTimeOffset now, Guid updatedBy)
    {
        if (IsActive) return false;
        IsActive = true;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
        return true;
    }

    public bool Deactivate(DateTimeOffset now, Guid updatedBy)
    {
        if (!IsActive) return false;
        IsActive = false;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
        return true;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
