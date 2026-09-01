namespace FarmManagement.Application.DTOs.Authentication;

public sealed record RefreshResponse(string AccessToken, int ExpiresIn);
