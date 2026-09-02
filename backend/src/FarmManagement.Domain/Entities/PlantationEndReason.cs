namespace FarmManagement.Domain.Entities;

public sealed class PlantationEndReason
{
    private PlantationEndReason()
    {
        Code = string.Empty;
        Name = string.Empty;
    }

    public PlantationEndReason(
        Guid? organizationId,
        string code,
        string name,
        bool isSystem = false,
        string? description = null,
        Guid? createdBy = null)
    {
        if (isSystem != (organizationId is null))
        {
            throw new ArgumentException("System end reasons cannot belong to an organization and organization end reasons require an organization.", nameof(organizationId));
        }

        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("An end reason code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("An end reason name is required.", nameof(name));

        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        IsSystem = isSystem;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
        CreatedBy = createdBy;
    }

    public Guid Id { get; private set; }
    public Guid? OrganizationId { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public bool IsSystem { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public Organization? Organization { get; private set; }
}
