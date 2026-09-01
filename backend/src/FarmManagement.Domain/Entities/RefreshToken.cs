using FarmManagement.Domain.Enums;

namespace FarmManagement.Domain.Entities;

public sealed class RefreshToken
{
    private RefreshToken()
    {
        TokenHash = string.Empty;
    }

    public RefreshToken(
        Guid userId,
        string tokenHash,
        ClientType clientType,
        DateTimeOffset expiresAt,
        string? deviceName = null,
        string? createdByIp = null)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("A user is required.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new ArgumentException("A token hash is required.", nameof(tokenHash));
        }

        Id = Guid.NewGuid();
        UserId = userId;
        TokenHash = tokenHash;
        ClientType = clientType;
        DeviceName = deviceName?.Trim();
        ExpiresAt = expiresAt;
        CreatedAt = DateTimeOffset.UtcNow;
        CreatedByIp = createdByIp?.Trim();
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; }

    public ClientType ClientType { get; private set; }

    public string? DeviceName { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public Guid? ReplacedByTokenId { get; private set; }

    public string? CreatedByIp { get; private set; }

    public string? RevokedByIp { get; private set; }

    public User User { get; private set; } = null!;

    public bool IsActive(DateTimeOffset now)
    {
        return RevokedAt is null && ExpiresAt > now;
    }

    public bool Revoke(DateTimeOffset revokedAt, string? revokedByIp = null)
    {
        if (RevokedAt is not null)
        {
            return false;
        }

        RevokedAt = revokedAt;
        RevokedByIp = revokedByIp?.Trim();
        return true;
    }

    public bool Rotate(
        Guid replacementTokenId,
        DateTimeOffset revokedAt,
        string? revokedByIp = null)
    {
        if (replacementTokenId == Guid.Empty)
        {
            throw new ArgumentException(
                "A replacement refresh token is required.",
                nameof(replacementTokenId));
        }

        if (!Revoke(revokedAt, revokedByIp))
        {
            return false;
        }

        ReplacedByTokenId = replacementTokenId;
        return true;
    }
}
