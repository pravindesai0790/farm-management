using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.Common.Models.Authentication;

public sealed record RefreshTokenResult(
    Guid TokenId,
    string Token,
    ClientType ClientType,
    DateTimeOffset ExpiresAt,
    int ExpiresInSeconds);
