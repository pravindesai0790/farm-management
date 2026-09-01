using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.Common.Models.Authentication;

public sealed record RefreshTokenValidationResult(
    Guid TokenId,
    Guid UserId,
    ClientType ClientType,
    DateTimeOffset ExpiresAt);
