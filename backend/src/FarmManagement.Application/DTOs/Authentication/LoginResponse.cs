namespace FarmManagement.Application.DTOs.Authentication;

public sealed record LoginResponse(
    string AccessToken,
    int ExpiresIn,
    AuthenticationUserResponse User);
