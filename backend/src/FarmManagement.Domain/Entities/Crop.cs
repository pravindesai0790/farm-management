namespace FarmManagement.Domain.Entities;

public sealed class Crop
{
    private Crop()
    {
        Code = string.Empty;
        Name = string.Empty;
        CropType = string.Empty;
        CropDurationType = string.Empty;
    }

    public Crop(
        Guid? organizationId,
        string code,
        string name,
        string cropType,
        string cropDurationType,
        bool isSystem = false,
        string? scientificName = null,
        string? description = null,
        Guid? createdBy = null)
    {
        if (isSystem != (organizationId is null))
        {
            throw new ArgumentException(
                isSystem
                    ? "A system crop cannot belong to an organization."
                    : "An organization is required for an organization-specific crop.",
                nameof(organizationId));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("A crop code is required.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A crop name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(cropType))
        {
            throw new ArgumentException("A crop type is required.", nameof(cropType));
        }

        if (string.IsNullOrWhiteSpace(cropDurationType))
        {
            throw new ArgumentException("A crop duration type is required.", nameof(cropDurationType));
        }

        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        Code = NormalizeCode(code);
        Name = name.Trim();
        ScientificName = NormalizeOptional(scientificName);
        CropType = cropType.Trim();
        CropDurationType = cropDurationType.Trim().ToUpperInvariant();
        Description = NormalizeOptional(description);
        IsSystem = isSystem;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
        CreatedBy = createdBy;
    }

    public Guid Id { get; private set; }
    public Guid? OrganizationId { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string? ScientificName { get; private set; }
    public string CropType { get; private set; }
    public string CropDurationType { get; private set; }
    public string? Description { get; private set; }
    public bool IsSystem { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public Organization? Organization { get; private set; }

    public void Update(
        string code,
        string name,
        string cropType,
        string cropDurationType,
        string? scientificName,
        string? description,
        DateTimeOffset now,
        Guid updatedBy)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("A crop code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A crop name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(cropType)) throw new ArgumentException("A crop type is required.", nameof(cropType));
        if (string.IsNullOrWhiteSpace(cropDurationType)) throw new ArgumentException("A crop duration type is required.", nameof(cropDurationType));

        Code = NormalizeCode(code);
        Name = name.Trim();
        ScientificName = NormalizeOptional(scientificName);
        CropType = cropType.Trim();
        CropDurationType = cropDurationType.Trim().ToUpperInvariant();
        Description = NormalizeOptional(description);
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

    private static string NormalizeCode(string value) => value.Trim().ToUpperInvariant();

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
