namespace FarmManagement.Application.DTOs.Authentication;

/// <summary>
/// Data transfer object returned when refreshing an access token.
/// Includes the rotated <see cref="RefreshToken"/> when requested by a mobile client for local storage.
/// </summary>
public sealed record RefreshResponse(
    string AccessToken,
    int ExpiresIn,
    string? RefreshToken = null);
