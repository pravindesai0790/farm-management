namespace FarmManagement.Application.DTOs.Labor;

public sealed record LaborWageRateActor(Guid UserId, Guid OrganizationId);

public sealed record CreateLaborWageRateRequest(
    string Gender,
    string WageType,
    decimal WageRate,
    Guid CurrencyId,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo = null,
    string? Notes = null);

public sealed record UpdateLaborWageRateRequest(
    decimal WageRate,
    Guid CurrencyId,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo = null,
    string? Notes = null);

public sealed record LaborWageRateResponse(
    Guid Id,
    Guid OrganizationId,
    string Gender,
    string WageType,
    decimal WageRate,
    Guid CurrencyId,
    string CurrencyCode,
    string CurrencySymbol,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    bool IsActive,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
