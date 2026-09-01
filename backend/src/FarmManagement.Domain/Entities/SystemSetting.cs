namespace FarmManagement.Domain.Entities;

public sealed class SystemSetting
{
    private SystemSetting()
    {
        Key = string.Empty;
        Value = string.Empty;
    }

    public SystemSetting(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("A system setting key is required.", nameof(key));
        }

        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        Id = Guid.NewGuid();
        Key = key.Trim();
        Value = value;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }

    public string Key { get; private set; }

    public string Value { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }
}
