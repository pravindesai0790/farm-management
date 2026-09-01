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
}
