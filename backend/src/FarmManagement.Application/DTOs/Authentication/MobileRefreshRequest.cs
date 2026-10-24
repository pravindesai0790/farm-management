namespace FarmManagement.Application.DTOs.Authentication;

/// <summary>
/// Request payload used by mobile applications to deliver the refresh token
/// in the request body rather than relying on browser-managed HttpOnly cookies.
/// </summary>
/// <param name="RefreshToken">The active refresh token stored in the mobile device's secure store.</param>
public sealed record MobileRefreshRequest(string? RefreshToken);
