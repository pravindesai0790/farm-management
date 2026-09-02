namespace FarmManagement.Domain.Entities;

public sealed class Organization
{
    private Organization()
    {
        Name = string.Empty;
        Code = string.Empty;
        Users = [];
    }

    public Organization(string name, string code)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("An organization name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("An organization code is required.", nameof(code));
        }

        Id = Guid.NewGuid();
        Name = name.Trim();
        Code = code.Trim();
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
        Users = [];
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public string Code { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public ICollection<User> Users { get; private set; }

    public bool UpdateProfile(
        string name,
        string code,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("An organization name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("An organization code is required.", nameof(code));
        }

        var normalizedName = name.Trim();
        var normalizedCode = code.Trim();
        var changed = !string.Equals(Name, normalizedName, StringComparison.Ordinal) ||
            !string.Equals(Code, normalizedCode, StringComparison.Ordinal);

        if (changed)
        {
            Name = normalizedName;
            Code = normalizedCode;
            UpdatedAt = now;
        }

        return changed;
    }

    public bool Activate(DateTimeOffset now)
    {
        if (IsActive)
        {
            return false;
        }

        IsActive = true;
        UpdatedAt = now;
        return true;
    }

    public bool Deactivate(DateTimeOffset now)
    {
        if (!IsActive)
        {
            return false;
        }

        IsActive = false;
        UpdatedAt = now;
        return true;
    }
}
