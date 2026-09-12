using System.Text.Json;
using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Labor;
using FarmManagement.Application.Interfaces.Labor;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.Services;

public sealed class LaborWageRateService(ILaborWageRateStore store) : ILaborWageRateService
{
    public async Task<PagedResponse<LaborWageRateResponse>> ListAsync(
        LaborWageRateActor actor,
        int page,
        int pageSize,
        string? gender,
        string? wageType,
        bool? isActive,
        DateOnly? businessDate,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);

        Gender? parsedGender = null;
        if (!string.IsNullOrWhiteSpace(gender))
        {
            parsedGender = ParseGender(gender);
        }

        WageType? parsedWageType = null;
        if (!string.IsNullOrWhiteSpace(wageType))
        {
            parsedWageType = ParseWageType(wageType);
        }

        var paged = await store.ListAsync(
            actor.OrganizationId,
            page,
            pageSize,
            parsedGender,
            parsedWageType,
            isActive,
            businessDate,
            cancellationToken);

        var items = paged.Items.Select(MapToResponse).ToList();
        return new PagedResponse<LaborWageRateResponse>(items, paged.TotalCount, paged.Page, paged.PageSize);
    }

    public async Task<IReadOnlyList<LaborWageRateResponse>> ListAllAsync(
        LaborWageRateActor actor,
        string? gender,
        string? wageType,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);

        Gender? parsedGender = null;
        if (!string.IsNullOrWhiteSpace(gender))
        {
            parsedGender = ParseGender(gender);
        }

        WageType? parsedWageType = null;
        if (!string.IsNullOrWhiteSpace(wageType))
        {
            parsedWageType = ParseWageType(wageType);
        }

        var rates = await store.ListAllAsync(
            actor.OrganizationId,
            parsedGender,
            parsedWageType,
            isActive,
            cancellationToken);

        return rates.Select(MapToResponse).ToList();
    }

    public async Task<LaborWageRateResponse> GetAsync(
        LaborWageRateActor actor,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var wageRate = await store.FindAsync(id, actor.OrganizationId, cancellationToken);
        if (wageRate is null)
        {
            throw new ResourceNotFoundException("The labor wage rate was not found.");
        }

        return MapToResponse(wageRate);
    }

    public async Task<LaborWageRateResponse> CreateAsync(
        LaborWageRateActor actor,
        CreateLaborWageRateRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);

        var gender = ParseGender(request.Gender);
        var wageType = ParseWageType(request.WageType);

        if (request.WageRate <= 0)
        {
            throw new ValidationException("The wage rate must be greater than zero.");
        }

        if (request.EffectiveTo.HasValue && request.EffectiveTo.Value < request.EffectiveFrom)
        {
            throw new ValidationException("The effective to date cannot be earlier than effective from date.");
        }

        var currency = await store.FindCurrencyAsync(request.CurrencyId, cancellationToken);
        if (currency is null || !currency.IsActive)
        {
            throw new ValidationException("The selected currency is invalid or inactive.");
        }

        var now = DateTimeOffset.UtcNow;
        var activeRates = await store.GetActiveRatesForOrganizationAsync(
            actor.OrganizationId,
            gender,
            wageType,
            excludeId: null,
            cancellationToken);

        var ratesToCheckForOverlap = new List<LaborWageRate>();

        // Deterministic auto-close rule:
        // If a preceding active open-ended rate exists for the same organization + gender + wage_type:
        // When request.EffectiveFrom > existing.EffectiveFrom, automatically close the previous rate at effectiveFrom - 1 day.
        // If request.EffectiveFrom <= existing.EffectiveFrom, reject because it cannot close backwards.
        var openEndedRate = activeRates.FirstOrDefault(r => r.EffectiveTo == null);
        if (openEndedRate is not null)
        {
            if (request.EffectiveFrom > openEndedRate.EffectiveFrom)
            {
                var closeDate = request.EffectiveFrom.AddDays(-1);
                openEndedRate.CloseEffectivePeriod(closeDate, now, actor.UserId);
                AddAudit(actor, openEndedRate, "LaborWageRate.Closed", new
                {
                    ClosedToDate = closeDate,
                    ClosedByNewEffectiveFrom = request.EffectiveFrom
                }, ipAddress);
            }
            else
            {
                throw new ValidationException(
                    $"An active open-ended wage rate already exists with effective start date {openEndedRate.EffectiveFrom:yyyy-MM-dd}. " +
                    $"The new rate's effective date must be after {openEndedRate.EffectiveFrom:yyyy-MM-dd} to close the previous rate.");
            }
        }

        foreach (var rate in activeRates)
        {
            if (openEndedRate is not null && rate.Id == openEndedRate.Id)
            {
                // Verify the newly closed rate doesn't conflict
                ratesToCheckForOverlap.Add(rate);
            }
            else
            {
                ratesToCheckForOverlap.Add(rate);
            }
        }

        // Check overlaps
        var newStart = request.EffectiveFrom;
        var newEnd = request.EffectiveTo ?? DateOnly.MaxValue;

        foreach (var existing in ratesToCheckForOverlap)
        {
            var existingStart = existing.EffectiveFrom;
            var existingEnd = existing.EffectiveTo ?? DateOnly.MaxValue;

            if (newStart <= existingEnd && newEnd >= existingStart)
            {
                throw new ValidationException(
                    $"The effective period {newStart:yyyy-MM-dd} to {(request.EffectiveTo.HasValue ? request.EffectiveTo.Value.ToString("yyyy-MM-dd") : "indefinite")} " +
                    $"overlaps with an existing active wage rate ({existingStart:yyyy-MM-dd} to {(existing.EffectiveTo.HasValue ? existing.EffectiveTo.Value.ToString("yyyy-MM-dd") : "indefinite")}).");
            }
        }

        var wageRate = new LaborWageRate(
            actor.OrganizationId,
            gender,
            wageType,
            request.WageRate,
            request.CurrencyId,
            request.EffectiveFrom,
            actor.UserId,
            request.EffectiveTo,
            request.Notes);

        store.Add(wageRate);
        AddAudit(actor, wageRate, "LaborWageRate.Created", request, ipAddress);

        await store.SaveChangesAsync(cancellationToken);

        // Fetch with navigations for complete response
        var created = await store.FindAsync(wageRate.Id, actor.OrganizationId, cancellationToken);
        return MapToResponse(created ?? wageRate);
    }

    public async Task<LaborWageRateResponse> UpdateAsync(
        LaborWageRateActor actor,
        Guid id,
        UpdateLaborWageRateRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);

        var wageRate = await store.FindAsync(id, actor.OrganizationId, cancellationToken);
        if (wageRate is null)
        {
            throw new ResourceNotFoundException("The labor wage rate was not found.");
        }

        if (request.WageRate <= 0)
        {
            throw new ValidationException("The wage rate must be greater than zero.");
        }

        if (request.EffectiveTo.HasValue && request.EffectiveTo.Value < request.EffectiveFrom)
        {
            throw new ValidationException("The effective to date cannot be earlier than effective from date.");
        }

        var currency = await store.FindCurrencyAsync(request.CurrencyId, cancellationToken);
        if (currency is null || !currency.IsActive)
        {
            throw new ValidationException("The selected currency is invalid or inactive.");
        }

        if (wageRate.IsActive)
        {
            var activeRates = await store.GetActiveRatesForOrganizationAsync(
                actor.OrganizationId,
                wageRate.Gender,
                wageRate.WageType,
                excludeId: id,
                cancellationToken);

            var newStart = request.EffectiveFrom;
            var newEnd = request.EffectiveTo ?? DateOnly.MaxValue;

            foreach (var existing in activeRates)
            {
                var existingStart = existing.EffectiveFrom;
                var existingEnd = existing.EffectiveTo ?? DateOnly.MaxValue;

                if (newStart <= existingEnd && newEnd >= existingStart)
                {
                    throw new ValidationException(
                        $"The effective period {newStart:yyyy-MM-dd} to {(request.EffectiveTo.HasValue ? request.EffectiveTo.Value.ToString("yyyy-MM-dd") : "indefinite")} " +
                        $"overlaps with an existing active wage rate ({existingStart:yyyy-MM-dd} to {(existing.EffectiveTo.HasValue ? existing.EffectiveTo.Value.ToString("yyyy-MM-dd") : "indefinite")}).");
                }
            }
        }

        var now = DateTimeOffset.UtcNow;
        wageRate.Update(
            request.WageRate,
            request.CurrencyId,
            request.EffectiveFrom,
            request.EffectiveTo,
            request.Notes,
            now,
            actor.UserId);

        AddAudit(actor, wageRate, "LaborWageRate.Updated", request, ipAddress);

        await store.SaveChangesAsync(cancellationToken);

        var updated = await store.FindAsync(id, actor.OrganizationId, cancellationToken);
        return MapToResponse(updated ?? wageRate);
    }

    public async Task ActivateAsync(
        LaborWageRateActor actor,
        Guid id,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);

        var wageRate = await store.FindAsync(id, actor.OrganizationId, cancellationToken);
        if (wageRate is null)
        {
            throw new ResourceNotFoundException("The labor wage rate was not found.");
        }

        if (wageRate.IsActive)
        {
            return;
        }

        // Verify that activating this rate does not create an overlap with an active rate
        var activeRates = await store.GetActiveRatesForOrganizationAsync(
            actor.OrganizationId,
            wageRate.Gender,
            wageRate.WageType,
            excludeId: id,
            cancellationToken);

        var newStart = wageRate.EffectiveFrom;
        var newEnd = wageRate.EffectiveTo ?? DateOnly.MaxValue;

        foreach (var existing in activeRates)
        {
            var existingStart = existing.EffectiveFrom;
            var existingEnd = existing.EffectiveTo ?? DateOnly.MaxValue;

            if (newStart <= existingEnd && newEnd >= existingStart)
            {
                throw new ValidationException(
                    $"Cannot activate this wage rate because its effective period {newStart:yyyy-MM-dd} to {(wageRate.EffectiveTo.HasValue ? wageRate.EffectiveTo.Value.ToString("yyyy-MM-dd") : "indefinite")} " +
                    $"overlaps with an existing active wage rate ({existingStart:yyyy-MM-dd} to {(existing.EffectiveTo.HasValue ? existing.EffectiveTo.Value.ToString("yyyy-MM-dd") : "indefinite")}).");
            }
        }

        var now = DateTimeOffset.UtcNow;
        wageRate.Activate(now, actor.UserId);
        AddAudit(actor, wageRate, "LaborWageRate.Activated", null, ipAddress);

        await store.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateAsync(
        LaborWageRateActor actor,
        Guid id,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);

        var wageRate = await store.FindAsync(id, actor.OrganizationId, cancellationToken);
        if (wageRate is null)
        {
            throw new ResourceNotFoundException("The labor wage rate was not found.");
        }

        if (!wageRate.IsActive)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        wageRate.Deactivate(now, actor.UserId);
        AddAudit(actor, wageRate, "LaborWageRate.Deactivated", null, ipAddress);

        await store.SaveChangesAsync(cancellationToken);
    }

    public Task<LaborWageRateResponse?> GetApplicableRateAsync(
        LaborWageRateActor actor,
        Gender gender,
        WageType wageType,
        DateOnly businessDate,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        return LookupApplicableRateAsync(actor.OrganizationId, gender, wageType, businessDate, cancellationToken);
    }

    public async Task<LaborWageRateResponse?> LookupApplicableRateAsync(
        Guid organizationId,
        Gender gender,
        WageType wageType,
        DateOnly businessDate,
        CancellationToken cancellationToken = default)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("An organization is required.", nameof(organizationId));
        }

        var rate = await store.FindApplicableRateAsync(
            organizationId,
            gender,
            wageType,
            businessDate,
            cancellationToken);

        return rate is null ? null : MapToResponse(rate);
    }

    private void AddAudit(LaborWageRateActor actor, LaborWageRate rate, string action, object? details, string? ipAddress) =>
        store.AddAuditLog(new AuditLog(
            action: action,
            organizationId: actor.OrganizationId,
            userId: actor.UserId,
            entityType: nameof(LaborWageRate),
            entityId: rate.Id,
            details: details is null ? null : JsonSerializer.SerializeToDocument(details),
            ipAddress: ipAddress));

    private static LaborWageRateResponse MapToResponse(LaborWageRate rate) =>
        new(
            Id: rate.Id,
            OrganizationId: rate.OrganizationId,
            Gender: rate.Gender.ToString().ToUpperInvariant(),
            WageType: FormatWageType(rate.WageType),
            WageRate: rate.WageRate,
            CurrencyId: rate.CurrencyId,
            CurrencyCode: rate.Currency?.Code ?? "INR",
            CurrencySymbol: rate.Currency?.Symbol ?? "₹",
            EffectiveFrom: rate.EffectiveFrom,
            EffectiveTo: rate.EffectiveTo,
            IsActive: rate.IsActive,
            Notes: rate.Notes,
            CreatedAt: rate.CreatedAt,
            UpdatedAt: rate.UpdatedAt);

    public static WageType ParseWageType(string value) => value.Trim().ToUpperInvariant() switch
    {
        "FULL_DAY" or "FULLDAY" => WageType.FullDay,
        "HALF_DAY" or "HALFDAY" => WageType.HalfDay,
        "HOURLY" => WageType.Hourly,
        "MONTHLY" => WageType.Monthly,
        _ => throw new ValidationException($"The wage type '{value}' is invalid. Supported values are: FULL_DAY, HALF_DAY, HOURLY, MONTHLY.")
    };

    public static string FormatWageType(WageType wageType) => wageType switch
    {
        WageType.FullDay => "FULL_DAY",
        WageType.HalfDay => "HALF_DAY",
        WageType.Hourly => "HOURLY",
        WageType.Monthly => "MONTHLY",
        _ => wageType.ToString().ToUpperInvariant()
    };

    public static Gender ParseGender(string value) => value.Trim().ToUpperInvariant() switch
    {
        "MALE" => Gender.Male,
        "FEMALE" => Gender.Female,
        "OTHER" => Gender.Other,
        _ => throw new ValidationException($"The gender '{value}' is invalid. Supported values are: MALE, FEMALE, OTHER.")
    };

    private static void ValidateActor(LaborWageRateActor actor)
    {
        if (actor.OrganizationId == Guid.Empty)
        {
            throw new ArgumentException("An organization context is required.", nameof(actor));
        }

        if (actor.UserId == Guid.Empty)
        {
            throw new ArgumentException("A user context is required.", nameof(actor));
        }
    }
}
