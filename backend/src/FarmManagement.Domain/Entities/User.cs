namespace FarmManagement.Domain.Entities;

public sealed class User
{
    private User()
    {
        FirstName = string.Empty;
        LastName = string.Empty;
        Email = string.Empty;
        PasswordHash = string.Empty;
        UserRoles = [];
        RefreshTokens = [];
        AuditLogs = [];
    }

    public User(
        Guid organizationId,
        string firstName,
        string lastName,
        string email,
        string passwordHash)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("An organization is required.", nameof(organizationId));
        }

        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ArgumentException("A first name is required.", nameof(firstName));
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new ArgumentException("A last name is required.", nameof(lastName));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("An email address is required.", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("A password hash is required.", nameof(passwordHash));
        }

        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Email = email.Trim();
        PasswordHash = passwordHash;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
        UserRoles = [];
        RefreshTokens = [];
        AuditLogs = [];
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public string FirstName { get; private set; }

    public string LastName { get; private set; }

    public string Email { get; private set; }

    public string PasswordHash { get; private set; }

    public bool IsActive { get; private set; }

    public int FailedLoginCount { get; private set; }

    public DateTimeOffset? LockoutEnd { get; private set; }

    public DateTimeOffset? LastLoginAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public Organization Organization { get; private set; } = null!;

    public ICollection<UserRole> UserRoles { get; private set; }

    public ICollection<RefreshToken> RefreshTokens { get; private set; }

    public ICollection<AuditLog> AuditLogs { get; private set; }

    public bool IsLockedOut(DateTimeOffset now)
    {
        return LockoutEnd is not null && LockoutEnd > now;
    }

    public void RecordFailedLogin(
        DateTimeOffset now,
        int maximumAttempts,
        TimeSpan lockoutDuration)
    {
        if (maximumAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumAttempts));
        }

        if (lockoutDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(lockoutDuration));
        }

        if (LockoutEnd is not null && LockoutEnd <= now)
        {
            FailedLoginCount = 0;
            LockoutEnd = null;
        }

        FailedLoginCount++;
        if (FailedLoginCount >= maximumAttempts)
        {
            LockoutEnd = now.Add(lockoutDuration);
        }

        UpdatedAt = now;
    }

    public void RecordSuccessfulLogin(DateTimeOffset now)
    {
        FailedLoginCount = 0;
        LockoutEnd = null;
        LastLoginAt = now;
        UpdatedAt = now;
    }

    public void ChangePassword(string passwordHash, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("A password hash is required.", nameof(passwordHash));
        }

        PasswordHash = passwordHash;
        UpdatedAt = now;
    }

    public bool UpdateProfile(
        string firstName,
        string lastName,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ArgumentException("A first name is required.", nameof(firstName));
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new ArgumentException("A last name is required.", nameof(lastName));
        }

        var normalizedFirstName = firstName.Trim();
        var normalizedLastName = lastName.Trim();
        var changed = !string.Equals(FirstName, normalizedFirstName, StringComparison.Ordinal) ||
            !string.Equals(LastName, normalizedLastName, StringComparison.Ordinal);

        if (changed)
        {
            FirstName = normalizedFirstName;
            LastName = normalizedLastName;
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

    public bool Unlock(DateTimeOffset now)
    {
        if (FailedLoginCount == 0 && LockoutEnd is null)
        {
            return false;
        }

        FailedLoginCount = 0;
        LockoutEnd = null;
        UpdatedAt = now;
        return true;
    }
}
