namespace FarmManagement.Domain.Entities;

public sealed class Currency
{
    private Currency()
    {
        Code = string.Empty;
        Name = string.Empty;
        Symbol = string.Empty;
    }

    public Currency(
        string code,
        string name,
        string symbol,
        bool isSystem = true,
        int displayOrder = 0,
        Guid? id = null,
        Guid? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("A currency code is required.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A currency name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new ArgumentException("A currency symbol is required.", nameof(symbol));
        }

        if (displayOrder < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(displayOrder), "Display order cannot be negative.");
        }

        Id = id ?? Guid.NewGuid();
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Symbol = symbol.Trim();
        IsSystem = isSystem;
        IsActive = true;
        DisplayOrder = displayOrder;
        CreatedAt = DateTimeOffset.UtcNow;
        CreatedBy = createdBy;
    }

    public Guid Id { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string Symbol { get; private set; }
    public bool IsSystem { get; private set; }
    public bool IsActive { get; private set; }
    public int DisplayOrder { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public void Update(
        string name,
        string symbol,
        int displayOrder,
        DateTimeOffset now,
        Guid? updatedBy = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A currency name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new ArgumentException("A currency symbol is required.", nameof(symbol));
        }

        if (displayOrder < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(displayOrder), "Display order cannot be negative.");
        }

        Name = name.Trim();
        Symbol = symbol.Trim();
        DisplayOrder = displayOrder;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    public bool Activate(DateTimeOffset now, Guid? updatedBy = null)
    {
        if (IsActive)
        {
            return false;
        }

        IsActive = true;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
        return true;
    }

    public bool Deactivate(DateTimeOffset now, Guid? updatedBy = null)
    {
        if (!IsActive)
        {
            return false;
        }

        IsActive = false;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
        return true;
    }
}
