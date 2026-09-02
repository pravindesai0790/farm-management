namespace FarmManagement.Domain.Entities;

public sealed class FarmArea
{
    private FarmArea()
    {
        Code = string.Empty;
        Name = string.Empty;
    }

    public FarmArea(
        Guid organizationId,
        Guid farmId,
        Guid? parentFarmAreaId,
        string code,
        string name,
        decimal totalArea,
        Guid areaUnitId,
        Guid createdBy,
        string? description = null)
    {
        ValidateIds(organizationId, farmId, areaUnitId, createdBy);
        ValidateParentId(parentFarmAreaId);
        ValidateCode(code);
        ValidateName(name);
        ValidateTotalArea(totalArea);

        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        FarmId = farmId;
        ParentFarmAreaId = parentFarmAreaId;
        Code = NormalizeCode(code);
        Name = name.Trim();
        Description = NormalizeOptional(description);
        TotalArea = totalArea;
        AreaUnitId = areaUnitId;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
        CreatedBy = createdBy;
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid FarmId { get; private set; }

    public Guid? ParentFarmAreaId { get; private set; }

    public string Code { get; private set; }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public decimal TotalArea { get; private set; }

    public Guid AreaUnitId { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public Guid CreatedBy { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public Guid? UpdatedBy { get; private set; }

    public Organization? Organization { get; private set; }

    public Farm? Farm { get; private set; }

    public FarmArea? ParentFarmArea { get; private set; }

    public Unit? AreaUnit { get; private set; }

    public void Update(
        Guid? parentFarmAreaId,
        string code,
        string name,
        decimal totalArea,
        Guid areaUnitId,
        string? description,
        DateTimeOffset now,
        Guid updatedBy)
    {
        ValidateIds(OrganizationId, FarmId, areaUnitId, updatedBy);
        ValidateParentId(parentFarmAreaId);
        ValidateCode(code);
        ValidateName(name);
        ValidateTotalArea(totalArea);

        ParentFarmAreaId = parentFarmAreaId;
        Code = NormalizeCode(code);
        Name = name.Trim();
        Description = NormalizeOptional(description);
        TotalArea = totalArea;
        AreaUnitId = areaUnitId;
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

    private static void ValidateIds(Guid organizationId, Guid farmId, Guid areaUnitId, Guid userId)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("An organization is required.", nameof(organizationId));
        }

        if (farmId == Guid.Empty)
        {
            throw new ArgumentException("A farm is required.", nameof(farmId));
        }

        if (areaUnitId == Guid.Empty)
        {
            throw new ArgumentException("An area unit is required.", nameof(areaUnitId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("A user is required.", nameof(userId));
        }
    }

    private static void ValidateCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A farm area code is required.", nameof(value));
        }
    }

    private static void ValidateName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A farm area name is required.", nameof(value));
        }
    }

    private static void ValidateTotalArea(decimal value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Total area must be greater than zero.");
        }
    }

    private static void ValidateParentId(Guid? value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("The parent farm area must be valid.", nameof(value));
        }
    }

    private static string NormalizeCode(string value) => value.Trim().ToUpperInvariant();

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
