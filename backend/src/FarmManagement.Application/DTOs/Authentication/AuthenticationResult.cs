namespace FarmManagement.Application.DTOs.Authentication;

public sealed record AuthenticationResult(
    string AccessToken,
    int ExpiresIn,
    AuthenticationUserResponse? User,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);
