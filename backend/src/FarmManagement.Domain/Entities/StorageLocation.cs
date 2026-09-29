namespace FarmManagement.Domain.Entities;

public sealed class StorageLocation
{
    private StorageLocation()
    {
        Name = string.Empty;
    }

    public StorageLocation(
        Guid organizationId,
        Guid farmId,
        string name,
        Guid createdBy,
        string? description = null)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("An organization is required.", nameof(organizationId));
        }

        if (farmId == Guid.Empty)
        {
            throw new ArgumentException("A farm is required.", nameof(farmId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A storage location name is required.", nameof(name));
        }

        if (createdBy == Guid.Empty)
        {
            throw new ArgumentException("A creating user is required.", nameof(createdBy));
        }

        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        FarmId = farmId;
        Name = name.Trim();
        Description = NormalizeOptional(description);
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
        CreatedBy = createdBy;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid FarmId { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public Organization? Organization { get; private set; }
    public Farm? Farm { get; private set; }

    public void Update(
        string name,
        string? description,
        DateTimeOffset now,
        Guid updatedBy)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A storage location name is required.", nameof(name));
        }

        if (updatedBy == Guid.Empty)
        {
            throw new ArgumentException("An updating user is required.", nameof(updatedBy));
        }

        Name = name.Trim();
        Description = NormalizeOptional(description);
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    public bool Activate(DateTimeOffset now, Guid updatedBy)
    {
        if (updatedBy == Guid.Empty)
        {
            throw new ArgumentException("A user is required.", nameof(updatedBy));
        }

        if (IsActive)
        {
            return false;
        }

        IsActive = true;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
        return true;
    }

    public bool Deactivate(DateTimeOffset now, Guid updatedBy)
    {
        if (updatedBy == Guid.Empty)
        {
            throw new ArgumentException("A user is required.", nameof(updatedBy));
        }

        if (!IsActive)
        {
            return false;
        }

        IsActive = false;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
        return true;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
