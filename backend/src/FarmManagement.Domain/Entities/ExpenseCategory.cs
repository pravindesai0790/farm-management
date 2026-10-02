namespace FarmManagement.Domain.Entities;

public sealed class ExpenseCategory
{
    private ExpenseCategory()
    {
        Name = string.Empty;
    }

    public ExpenseCategory(
        Guid? organizationId,
        string name,
        string? code = null,
        bool isSystemDefault = false,
        string? description = null,
        Guid? createdBy = null)
    {
        if (isSystemDefault != (organizationId is null))
        {
            throw new ArgumentException(
                "System default expense categories cannot belong to an organization, and organization expense categories require an organization.",
                nameof(organizationId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("An expense category name is required.", nameof(name));
        }

        if (name.Trim().Length > 100)
        {
            throw new ArgumentException("Expense category name cannot exceed 100 characters.", nameof(name));
        }

        if (!isSystemDefault && (createdBy is null || createdBy == Guid.Empty))
        {
            throw new ArgumentException("A creating user is required for custom expense categories.", nameof(createdBy));
        }

        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        Name = name.Trim();
        Code = NormalizeOptional(code, 50)?.ToUpperInvariant();
        Description = NormalizeOptional(description, 500);
        IsSystemDefault = isSystemDefault;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
        CreatedBy = createdBy;
    }

    public Guid Id { get; private set; }
    public Guid? OrganizationId { get; private set; }
    public string Name { get; private set; }
    public string? Code { get; private set; }
    public string? Description { get; private set; }
    public bool IsSystemDefault { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public Organization? Organization { get; private set; }

    public void Update(string name, string? description, Guid updatedBy, DateTimeOffset? now = null)
    {
        if (IsSystemDefault)
        {
            throw new InvalidOperationException("System default expense categories cannot be edited.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("An expense category name is required.", nameof(name));
        }

        if (name.Trim().Length > 100)
        {
            throw new ArgumentException("Expense category name cannot exceed 100 characters.", nameof(name));
        }

        if (updatedBy == Guid.Empty)
        {
            throw new ArgumentException("An updating user is required.", nameof(updatedBy));
        }

        Name = name.Trim();
        Description = NormalizeOptional(description, 500);
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
