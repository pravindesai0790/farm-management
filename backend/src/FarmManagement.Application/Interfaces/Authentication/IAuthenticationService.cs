using FarmManagement.Application.DTOs.Authentication;

namespace FarmManagement.Application.Interfaces.Authentication;

public interface IAuthenticationService
{
    Task<AuthenticationResult> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<AuthenticationResult> RefreshAsync(
        string refreshToken,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task LogoutAsync(
        string? refreshToken,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<AuthenticationUserResponse?> GetCurrentUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);
}
