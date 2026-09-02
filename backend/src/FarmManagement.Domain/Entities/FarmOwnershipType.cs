namespace FarmManagement.Domain.Entities;

public sealed class FarmOwnershipType
{
    private FarmOwnershipType()
    {
        Code = string.Empty;
        Name = string.Empty;
    }

    public FarmOwnershipType(string code, string name, bool isSystem = true)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("An ownership type code is required.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("An ownership type name is required.", nameof(name));
        }

        Id = Guid.NewGuid();
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        IsSystem = isSystem;
        IsActive = true;
    }

    public Guid Id { get; private set; }

    public string Code { get; private set; }

    public string Name { get; private set; }

    public bool IsSystem { get; private set; }

    public bool IsActive { get; private set; }
}
