using System.Security.Cryptography;
using System.Text;
using FarmManagement.Application.Common.Models.Authentication;
using FarmManagement.Application.Interfaces.Authentication;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using FarmManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FarmManagement.Infrastructure.Authentication;

public sealed class RefreshTokenService(
    ApplicationDbContext dbContext,
    IOptions<JwtOptions> options) : IRefreshTokenService
{
    private const int RandomTokenByteLength = 64;

    public async Task<RefreshTokenResult> CreateAsync(
        Guid userId,
        ClientType clientType,
        string? deviceName = null,
        string? createdByIp = null,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(userId, clientType);

        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddDays(GetRefreshTokenLifetimeDays());
        var rawToken = GenerateRawToken();
        var refreshToken = new RefreshToken(
            userId,
            HashToken(rawToken),
            clientType,
            expiresAt,
            deviceName,
            createdByIp);

        dbContext.RefreshTokens.Add(refreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResult(refreshToken, rawToken, now);
    }

    public async Task<RefreshTokenValidationResult?> ValidateAsync(
        string token,
        ClientType clientType,
        CancellationToken cancellationToken = default)
    {
        if (!TryValidateTokenInput(token, clientType, out var normalizedToken))
        {
            return null;
        }

        var refreshToken = await FindTokenAsync(normalizedToken, cancellationToken);
        if (refreshToken is null || !IsUsable(refreshToken, clientType, DateTimeOffset.UtcNow))
        {
            return null;
        }

        return new RefreshTokenValidationResult(
            refreshToken.Id,
            refreshToken.UserId,
            refreshToken.ClientType,
            refreshToken.ExpiresAt);
    }

    public async Task<RefreshTokenRotationResult?> RotateAsync(
        string token,
        ClientType clientType,
        string? revokedByIp = null,
        string? deviceName = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryValidateTokenInput(token, clientType, out var normalizedToken))
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var oldToken = await FindTokenAsync(normalizedToken, cancellationToken);
        if (oldToken is null || !IsUsable(oldToken, clientType, now))
        {
            return null;
        }

        var rawToken = GenerateRawToken();
        var replacementToken = new RefreshToken(
            oldToken.UserId,
            HashToken(rawToken),
            oldToken.ClientType,
            now.AddDays(GetRefreshTokenLifetimeDays()),
            deviceName ?? oldToken.DeviceName,
            revokedByIp);

        if (!oldToken.Rotate(replacementToken.Id, now, revokedByIp))
        {
            return null;
        }

        dbContext.RefreshTokens.Add(replacementToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new RefreshTokenRotationResult(
            oldToken.Id,
            replacementToken.Id,
            replacementToken.UserId,
            rawToken,
            replacementToken.ClientType,
            replacementToken.ExpiresAt,
            checked((int)(replacementToken.ExpiresAt - now).TotalSeconds));
    }

    public async Task<bool> RevokeAsync(
        string token,
        ClientType? clientType = null,
        string? revokedByIp = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var refreshToken = await FindTokenAsync(token.Trim(), cancellationToken);
        if (refreshToken is null || clientType is not null && refreshToken.ClientType != clientType)
        {
            return false;
        }

        var revoked = refreshToken.Revoke(DateTimeOffset.UtcNow, revokedByIp);
        if (revoked)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return revoked;
    }

    public async Task<int> RevokeAllForUserAsync(
        Guid userId,
        string? revokedByIp = null,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("A user is required.", nameof(userId));
        }

        var now = DateTimeOffset.UtcNow;
        var tokens = await dbContext.RefreshTokens
            .Where(refreshToken =>
                refreshToken.UserId == userId &&
                refreshToken.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.Revoke(now, revokedByIp);
        }

        if (tokens.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return tokens.Count;
    }

    public Task<RefreshTokenResult> CreateTokenAsync(
        Guid userId,
        ClientType clientType,
        string? deviceName = null,
        string? createdByIp = null,
        CancellationToken cancellationToken = default) =>
        CreateAsync(userId, clientType, deviceName, createdByIp, cancellationToken);

    public Task<RefreshTokenValidationResult?> ValidateTokenAsync(
        string token,
        ClientType clientType,
        CancellationToken cancellationToken = default) =>
        ValidateAsync(token, clientType, cancellationToken);

    public Task<RefreshTokenRotationResult?> RotateTokenAsync(
        string token,
        ClientType clientType,
        string? revokedByIp = null,
        string? deviceName = null,
        CancellationToken cancellationToken = default) =>
        RotateAsync(token, clientType, revokedByIp, deviceName, cancellationToken);

    public Task<bool> RevokeTokenAsync(
        string token,
        ClientType? clientType = null,
        string? revokedByIp = null,
        CancellationToken cancellationToken = default) =>
        RevokeAsync(token, clientType, revokedByIp, cancellationToken);

    private async Task<RefreshToken?> FindTokenAsync(
        string token,
        CancellationToken cancellationToken)
    {
        var tokenHash = HashToken(token);

        return await dbContext.RefreshTokens
            .Include(refreshToken => refreshToken.User)
            .ThenInclude(user => user.Organization)
            .SingleOrDefaultAsync(
                refreshToken => refreshToken.TokenHash == tokenHash,
                cancellationToken);
    }

    private static bool IsUsable(
        RefreshToken? refreshToken,
        ClientType clientType,
        DateTimeOffset now)
    {
        return refreshToken is not null &&
            refreshToken.ClientType == clientType &&
            refreshToken.IsActive(now) &&
            refreshToken.User.IsActive &&
            refreshToken.User.Organization.IsActive;
    }

    private int GetRefreshTokenLifetimeDays()
    {
        var jwtOptions = options.Value;
        jwtOptions.Validate();
        return jwtOptions.RefreshTokenDays;
    }

    private static RefreshTokenResult ToResult(
        RefreshToken refreshToken,
        string rawToken,
        DateTimeOffset now)
    {
        return new RefreshTokenResult(
            refreshToken.Id,
            rawToken,
            refreshToken.ClientType,
            refreshToken.ExpiresAt,
            checked((int)(refreshToken.ExpiresAt - now).TotalSeconds));
    }

    private static string GenerateRawToken()
    {
        return Base64UrlEncode(RandomNumberGenerator.GetBytes(RandomTokenByteLength));
    }

    private static string HashToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Base64UrlEncode(hash);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static void ValidateRequest(Guid userId, ClientType clientType)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("A user is required.", nameof(userId));
        }

        if (!Enum.IsDefined(clientType))
        {
            throw new ArgumentOutOfRangeException(nameof(clientType), clientType, "Unknown client type.");
        }
    }

    private static bool TryValidateTokenInput(
        string? token,
        ClientType clientType,
        out string normalizedToken)
    {
        normalizedToken = token?.Trim() ?? string.Empty;
        return normalizedToken.Length > 0 && Enum.IsDefined(clientType);
    }
}
