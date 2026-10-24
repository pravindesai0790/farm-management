namespace FarmManagement.Application.DTOs.Authentication;

/// <summary>
/// Data transfer object returned upon successful user authentication.
/// The optional <see cref="RefreshToken"/> is populated specifically for mobile clients (via X-Client-Platform header)
/// to allow hardware-backed secure storage, while web clients continue using HttpOnly cookies.
/// </summary>
public sealed record LoginResponse(
    string AccessToken,
    int ExpiresIn,
    AuthenticationUserResponse User,
    string? RefreshToken = null);
