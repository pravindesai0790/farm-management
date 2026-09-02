namespace FarmManagement.Domain.Entities;

public sealed class Farm
{
    private Farm()
    {
        Code = string.Empty;
        Name = string.Empty;
    }

    public Farm(
        Guid organizationId,
        string code,
        string name,
        Guid ownershipTypeId,
        Guid createdBy,
        decimal? totalArea = null,
        Guid? areaUnitId = null,
        string? description = null,
        string? addressLine1 = null,
        string? addressLine2 = null,
        string? city = null,
        string? district = null,
        string? state = null,
        string? country = null,
        string? postalCode = null,
        decimal? latitude = null,
        decimal? longitude = null)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("An organization is required.", nameof(organizationId));
        }

        ValidateCode(code);
        ValidateName(name);
        ValidateOwnershipType(ownershipTypeId);
        ValidateArea(totalArea, areaUnitId);
        ValidateCoordinates(latitude, longitude);
        if (createdBy == Guid.Empty)
        {
            throw new ArgumentException("A creating user is required.", nameof(createdBy));
        }

        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        Code = NormalizeCode(code);
        Name = name.Trim();
        OwnershipTypeId = ownershipTypeId;
        TotalArea = totalArea;
        AreaUnitId = areaUnitId;
        Description = NormalizeOptional(description);
        AddressLine1 = NormalizeOptional(addressLine1);
        AddressLine2 = NormalizeOptional(addressLine2);
        City = NormalizeOptional(city);
        District = NormalizeOptional(district);
        State = NormalizeOptional(state);
        Country = NormalizeOptional(country);
        PostalCode = NormalizeOptional(postalCode);
        Latitude = latitude;
        Longitude = longitude;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
        CreatedBy = createdBy;
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public string Code { get; private set; }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public Guid OwnershipTypeId { get; private set; }

    public decimal? TotalArea { get; private set; }

    public Guid? AreaUnitId { get; private set; }

    public string? AddressLine1 { get; private set; }

    public string? AddressLine2 { get; private set; }

    public string? City { get; private set; }

    public string? District { get; private set; }

    public string? State { get; private set; }

    public string? Country { get; private set; }

    public string? PostalCode { get; private set; }

    public decimal? Latitude { get; private set; }

    public decimal? Longitude { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public Guid CreatedBy { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public Guid? UpdatedBy { get; private set; }

    public Organization? Organization { get; private set; }

    public FarmOwnershipType? OwnershipType { get; private set; }

    public Unit? AreaUnit { get; private set; }

    public void Update(
        string code,
        string name,
        Guid ownershipTypeId,
        decimal? totalArea,
        Guid? areaUnitId,
        string? description,
        string? addressLine1,
        string? addressLine2,
        string? city,
        string? district,
        string? state,
        string? country,
        string? postalCode,
        decimal? latitude,
        decimal? longitude,
        DateTimeOffset now,
        Guid updatedBy)
    {
        ValidateCode(code);
        ValidateName(name);
        ValidateOwnershipType(ownershipTypeId);
        ValidateArea(totalArea, areaUnitId);
        ValidateCoordinates(latitude, longitude);

        Code = NormalizeCode(code);
        Name = name.Trim();
        OwnershipTypeId = ownershipTypeId;
        TotalArea = totalArea;
        AreaUnitId = areaUnitId;
        Description = NormalizeOptional(description);
        AddressLine1 = NormalizeOptional(addressLine1);
        AddressLine2 = NormalizeOptional(addressLine2);
        City = NormalizeOptional(city);
        District = NormalizeOptional(district);
        State = NormalizeOptional(state);
        Country = NormalizeOptional(country);
        PostalCode = NormalizeOptional(postalCode);
        Latitude = latitude;
        Longitude = longitude;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    public bool Activate(DateTimeOffset now, Guid updatedBy)
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

    public bool Deactivate(DateTimeOffset now, Guid updatedBy)
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

    private static void ValidateCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A farm code is required.", nameof(value));
        }
    }

    private static void ValidateName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A farm name is required.", nameof(value));
        }
    }

    private static void ValidateOwnershipType(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("A farm ownership type is required.", nameof(value));
        }
    }

    private static void ValidateArea(decimal? totalArea, Guid? areaUnitId)
    {
        if (totalArea is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalArea), "Total area must be greater than zero.");
        }

        if ((totalArea is null) != (areaUnitId is null))
        {
            throw new ArgumentException("Total area and area unit must be provided together.");
        }
    }

    private static void ValidateCoordinates(decimal? latitude, decimal? longitude)
    {
        if (latitude is < -90 or > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90.");
        }

        if (longitude is < -180 or > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180.");
        }
    }

    private static string NormalizeCode(string value) => value.Trim().ToUpperInvariant();

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
