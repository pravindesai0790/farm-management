using FarmManagement.Domain.Enums;

namespace FarmManagement.Domain.Entities;

public sealed class Unit
{
    private Unit()
    {
        Code = string.Empty;
        Name = string.Empty;
        Symbol = string.Empty;
    }

    public Unit(
        Guid? organizationId,
        string code,
        string name,
        string symbol,
        UnitCategory unitCategory,
        string? baseUnitCode = null,
        decimal? conversionFactor = null,
        bool isSystem = false,
        int displayOrder = 0,
        Guid? createdBy = null)
    {
        if (isSystem && organizationId is not null)
        {
            throw new ArgumentException(
                "A system unit cannot belong to an organization.",
                nameof(organizationId));
        }

        if (!isSystem && organizationId is null)
        {
            throw new ArgumentException(
                "An organization is required for an organization-specific unit.",
                nameof(organizationId));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("A unit code is required.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A unit name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new ArgumentException("A unit symbol is required.", nameof(symbol));
        }

        if (!Enum.IsDefined(unitCategory))
        {
            throw new ArgumentOutOfRangeException(
                nameof(unitCategory),
                "The unit category is invalid.");
        }

        if (conversionFactor is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(conversionFactor),
                "A unit conversion factor must be greater than zero.");
        }

        if (displayOrder < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(displayOrder),
                "A unit display order cannot be negative.");
        }

        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        Code = code.Trim();
        Name = name.Trim();
        Symbol = symbol.Trim();
        UnitCategory = unitCategory;
        BaseUnitCode = string.IsNullOrWhiteSpace(baseUnitCode) ? null : baseUnitCode.Trim();
        ConversionFactor = conversionFactor;
        IsSystem = isSystem;
        IsActive = true;
        DisplayOrder = displayOrder;
        CreatedAt = DateTimeOffset.UtcNow;
        CreatedBy = createdBy;
    }

    public Guid Id { get; private set; }

    public Guid? OrganizationId { get; private set; }

    public string Code { get; private set; }

    public string Name { get; private set; }

    public string Symbol { get; private set; }

    public UnitCategory UnitCategory { get; private set; }

    public string? BaseUnitCode { get; private set; }

    public decimal? ConversionFactor { get; private set; }

    public bool IsSystem { get; private set; }

    public bool IsActive { get; private set; }

    public int DisplayOrder { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public Guid? CreatedBy { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public Guid? UpdatedBy { get; private set; }

    public Organization? Organization { get; private set; }

    public bool Activate(DateTimeOffset now, Guid? updatedBy = null)
    {
        if (IsActive)
        {
            return false;
        }

        IsActive = true;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
        return true;
    }

    public bool Deactivate(DateTimeOffset now, Guid? updatedBy = null)
    {
        if (!IsActive)
        {
            return false;
        }

        IsActive = false;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
        return true;
    }
}
