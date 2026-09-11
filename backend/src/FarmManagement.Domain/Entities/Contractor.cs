namespace FarmManagement.Domain.Entities;

public sealed class Contractor
{
    private Contractor()
    {
        Name = string.Empty;
        Workers = [];
    }

    public Contractor(
        Guid organizationId,
        string name,
        Guid createdBy,
        string? contactPerson = null,
        string? phoneNumber = null,
        string? email = null,
        string? address = null,
        string? notes = null)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("An organization is required.", nameof(organizationId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A contractor name is required.", nameof(name));
        }

        if (createdBy == Guid.Empty)
        {
            throw new ArgumentException("A creating user is required.", nameof(createdBy));
        }

        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        Name = name.Trim();
        ContactPerson = NormalizeOptional(contactPerson);
        PhoneNumber = NormalizeOptional(phoneNumber);
        Email = NormalizeOptional(email);
        Address = NormalizeOptional(address);
        Notes = NormalizeOptional(notes);
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
        CreatedBy = createdBy;
        Workers = [];
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Name { get; private set; }
    public string? ContactPerson { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? Email { get; private set; }
    public string? Address { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public Organization? Organization { get; private set; }
    public ICollection<Worker> Workers { get; private set; }

    public void Update(
        string name,
        string? contactPerson,
        string? phoneNumber,
        string? email,
        string? address,
        string? notes,
        DateTimeOffset now,
        Guid updatedBy)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A contractor name is required.", nameof(name));
        }

        if (updatedBy == Guid.Empty)
        {
            throw new ArgumentException("A user is required.", nameof(updatedBy));
        }

        Name = name.Trim();
        ContactPerson = NormalizeOptional(contactPerson);
        PhoneNumber = NormalizeOptional(phoneNumber);
        Email = NormalizeOptional(email);
        Address = NormalizeOptional(address);
        Notes = NormalizeOptional(notes);
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
