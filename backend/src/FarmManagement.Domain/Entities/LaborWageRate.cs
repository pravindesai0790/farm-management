using FarmManagement.Domain.Enums;

namespace FarmManagement.Domain.Entities;

public sealed class LaborWageRate
{
    private LaborWageRate()
    {
    }

    public LaborWageRate(
        Guid organizationId,
        Gender gender,
        WageType wageType,
        decimal wageRate,
        Guid currencyId,
        DateOnly effectiveFrom,
        Guid createdBy,
        DateOnly? effectiveTo = null,
        string? notes = null)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("An organization is required.", nameof(organizationId));
        }

        if (!Enum.IsDefined(gender))
        {
            throw new ArgumentOutOfRangeException(nameof(gender), "The gender is invalid.");
        }

        if (!Enum.IsDefined(wageType))
        {
            throw new ArgumentOutOfRangeException(nameof(wageType), "The wage type is invalid.");
        }

        if (wageRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(wageRate), "The wage rate must be greater than zero.");
        }

        if (currencyId == Guid.Empty)
        {
            throw new ArgumentException("A currency is required.", nameof(currencyId));
        }

        if (createdBy == Guid.Empty)
        {
            throw new ArgumentException("A creating user is required.", nameof(createdBy));
        }

        ValidateDates(effectiveFrom, effectiveTo);

        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        Gender = gender;
        WageType = wageType;
        WageRate = wageRate;
        CurrencyId = currencyId;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        Notes = NormalizeOptional(notes);
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
        CreatedBy = createdBy;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Gender Gender { get; private set; }
    public WageType WageType { get; private set; }
    public decimal WageRate { get; private set; }
    public Guid CurrencyId { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public Organization? Organization { get; private set; }
    public Currency? Currency { get; private set; }

    public void Update(
        decimal wageRate,
        Guid currencyId,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo,
        string? notes,
        DateTimeOffset now,
        Guid updatedBy)
    {
        if (updatedBy == Guid.Empty)
        {
            throw new ArgumentException("A user is required.", nameof(updatedBy));
        }

        if (currencyId == Guid.Empty)
        {
            throw new ArgumentException("A currency is required.", nameof(currencyId));
        }

        if (wageRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(wageRate), "The wage rate must be greater than zero.");
        }

        ValidateDates(effectiveFrom, effectiveTo);

        WageRate = wageRate;
        CurrencyId = currencyId;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        Notes = NormalizeOptional(notes);
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    public bool CloseEffectivePeriod(DateOnly effectiveTo, DateTimeOffset now, Guid updatedBy)
    {
        if (updatedBy == Guid.Empty)
        {
            throw new ArgumentException("A user is required.", nameof(updatedBy));
        }

        if (effectiveTo < EffectiveFrom)
        {
            throw new ArgumentException("The effective to date cannot be earlier than effective from date.", nameof(effectiveTo));
        }

        EffectiveTo = effectiveTo;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
        return true;
    }

    public bool Deactivate(DateTimeOffset now, Guid updatedBy)
    {
        if (updatedBy == Guid.Empty)
        {
            throw new ArgumentException("A user is required.", nameof(updatedBy));
        }

        if (!IsActive)
        {
            return false;
        }

        IsActive = false;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
        return true;
    }

    public bool Activate(DateTimeOffset now, Guid updatedBy)
    {
        if (updatedBy == Guid.Empty)
        {
            throw new ArgumentException("A user is required.", nameof(updatedBy));
        }

        if (IsActive)
        {
            return false;
        }

        IsActive = true;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
        return true;
    }

    public bool IsApplicableOn(DateOnly businessDate) =>
        IsActive &&
        businessDate >= EffectiveFrom &&
        (!EffectiveTo.HasValue || businessDate <= EffectiveTo.Value);

    private static void ValidateDates(DateOnly effectiveFrom, DateOnly? effectiveTo)
    {
        if (effectiveTo.HasValue && effectiveTo.Value < effectiveFrom)
        {
            throw new ArgumentException("The effective to date cannot be earlier than effective from date.", nameof(effectiveTo));
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
