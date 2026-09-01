namespace FarmManagement.Application.Common.Models.Authentication;

public sealed record AccessTokenResult(
    string Token,
    DateTimeOffset ExpiresAt,
    int ExpiresInSeconds);
