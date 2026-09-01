using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.Common.Models.Authentication;

public sealed record RefreshTokenRotationResult(
    Guid ReplacedTokenId,
    Guid TokenId,
    Guid UserId,
    string Token,
    ClientType ClientType,
    DateTimeOffset ExpiresAt,
    int ExpiresInSeconds);
