namespace FarmManagement.Domain.Entities;

public sealed class Supplier
{
    private Supplier()
    {
        Name = string.Empty;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Name { get; private set; }
    public string? ContactPerson { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Address { get; private set; }
    public string? RegistrationIdentifier { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public Organization? Organization { get; private set; }

    public static Supplier Create(
        Guid organizationId,
        string name,
        Guid createdBy,
        string? contactPerson = null,
        string? phone = null,
        string? email = null,
        string? address = null,
        string? registrationIdentifier = null,
        string? notes = null)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("An organization is required.", nameof(organizationId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A supplier name is required.", nameof(name));
        }

        if (name.Trim().Length > 200)
        {
            throw new ArgumentException("Supplier name cannot exceed 200 characters.", nameof(name));
        }

        if (createdBy == Guid.Empty)
        {
            throw new ArgumentException("A creating user is required.", nameof(createdBy));
        }

        var now = DateTimeOffset.UtcNow;
        return new Supplier
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = name.Trim(),
            ContactPerson = NormalizeOptional(contactPerson, 150),
            Phone = NormalizeOptional(phone, 50),
            Email = NormalizeOptional(email, 150),
            Address = NormalizeOptional(address, 500),
            RegistrationIdentifier = NormalizeOptional(registrationIdentifier, 100),
            Notes = NormalizeOptional(notes, 1000),
            IsActive = true,
            CreatedAt = now,
            CreatedBy = createdBy
        };
    }

    public void Update(
        string name,
        string? contactPerson,
        string? phone,
        string? email,
        string? address,
        string? registrationIdentifier,
        string? notes,
        Guid updatedBy,
        DateTimeOffset? now = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A supplier name is required.", nameof(name));
        }

        if (name.Trim().Length > 200)
        {
            throw new ArgumentException("Supplier name cannot exceed 200 characters.", nameof(name));
        }

        if (updatedBy == Guid.Empty)
        {
            throw new ArgumentException("An updating user is required.", nameof(updatedBy));
        }

        Name = name.Trim();
        ContactPerson = NormalizeOptional(contactPerson, 150);
        Phone = NormalizeOptional(phone, 50);
        Email = NormalizeOptional(email, 150);
        Address = NormalizeOptional(address, 500);
        RegistrationIdentifier = NormalizeOptional(registrationIdentifier, 100);
        Notes = NormalizeOptional(notes, 1000);
        UpdatedAt = now ?? DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }

    public bool Deactivate(Guid updatedBy, DateTimeOffset? now = null)
    {
        if (!IsActive) return false;
        if (updatedBy == Guid.Empty) throw new ArgumentException("An updating user is required.", nameof(updatedBy));

        IsActive = false;
        UpdatedAt = now ?? DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
        return true;
    }

    public bool Activate(Guid updatedBy, DateTimeOffset? now = null)
    {
        if (IsActive) return false;
        if (updatedBy == Guid.Empty) throw new ArgumentException("An updating user is required.", nameof(updatedBy));

        IsActive = true;
        UpdatedAt = now ?? DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
        return true;
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }
}
