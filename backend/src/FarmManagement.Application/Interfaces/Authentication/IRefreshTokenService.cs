using FarmManagement.Application.Common.Models.Authentication;
using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.Interfaces.Authentication;

public interface IRefreshTokenService
{
    Task<RefreshTokenResult> CreateAsync(
        Guid userId,
        ClientType clientType,
        string? deviceName = null,
        string? createdByIp = null,
        CancellationToken cancellationToken = default);

    Task<RefreshTokenValidationResult?> ValidateAsync(
        string token,
        ClientType clientType,
        CancellationToken cancellationToken = default);

    Task<RefreshTokenRotationResult?> RotateAsync(
        string token,
        ClientType clientType,
        string? revokedByIp = null,
        string? deviceName = null,
        CancellationToken cancellationToken = default);

    Task<bool> RevokeAsync(
        string token,
        ClientType? clientType = null,
        string? revokedByIp = null,
        CancellationToken cancellationToken = default);

    Task<int> RevokeAllForUserAsync(
        Guid userId,
        string? revokedByIp = null,
        CancellationToken cancellationToken = default);
}
