using FarmManagement.Domain.Enums;

namespace FarmManagement.Domain.Entities;

public sealed class Worker
{
    private Worker()
    {
        FirstName = string.Empty;
        DisplayName = string.Empty;
    }

    public Worker(
        Guid organizationId,
        string firstName,
        string? lastName,
        Gender gender,
        EmploymentType employmentType,
        Guid createdBy,
        string? displayName = null,
        string? mobileNumber = null,
        string? alternateMobileNumber = null,
        Guid? laborCategoryId = null,
        Guid? contractorId = null,
        DateOnly? joiningDate = null,
        DateOnly? leavingDate = null,
        string? notes = null)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("An organization is required.", nameof(organizationId));
        }

        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ArgumentException("A first name is required.", nameof(firstName));
        }

        if (createdBy == Guid.Empty)
        {
            throw new ArgumentException("A creating user is required.", nameof(createdBy));
        }

        ValidateGender(gender);
        ValidateEmploymentType(employmentType, contractorId);
        ValidateDates(joiningDate, leavingDate);
        ValidateOptionalIdentifiers(laborCategoryId, contractorId);

        var resolvedDisplayName = string.IsNullOrWhiteSpace(displayName)
            ? BuildDisplayName(firstName, lastName)
            : displayName.Trim();

        if (string.IsNullOrWhiteSpace(resolvedDisplayName))
        {
            throw new ArgumentException("A display name is required.", nameof(displayName));
        }

        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        FirstName = firstName.Trim();
        LastName = NormalizeOptional(lastName);
        DisplayName = resolvedDisplayName;
        Gender = gender;
        EmploymentType = employmentType;
        LaborCategoryId = laborCategoryId;
        ContractorId = contractorId;
        JoiningDate = joiningDate;
        LeavingDate = leavingDate;
        MobileNumber = NormalizeOptional(mobileNumber);
        AlternateMobileNumber = NormalizeOptional(alternateMobileNumber);
        Notes = NormalizeOptional(notes);
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
        CreatedBy = createdBy;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string DisplayName { get; private set; }
    public Gender Gender { get; private set; }
    public string? MobileNumber { get; private set; }
    public string? AlternateMobileNumber { get; private set; }
    public Guid? LaborCategoryId { get; private set; }
    public EmploymentType EmploymentType { get; private set; }
    public Guid? ContractorId { get; private set; }
    public DateOnly? JoiningDate { get; private set; }
    public DateOnly? LeavingDate { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public Organization? Organization { get; private set; }
    public Contractor? Contractor { get; private set; }
    public LaborCategory? LaborCategory { get; private set; }

    public void Update(
        string firstName,
        string? lastName,
        string? displayName,
        Gender gender,
        EmploymentType employmentType,
        Guid? laborCategoryId,
        Guid? contractorId,
        DateOnly? joiningDate,
        DateOnly? leavingDate,
        string? mobileNumber,
        string? alternateMobileNumber,
        string? notes,
        DateTimeOffset now,
        Guid updatedBy)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ArgumentException("A first name is required.", nameof(firstName));
        }

        if (updatedBy == Guid.Empty)
        {
            throw new ArgumentException("A user is required.", nameof(updatedBy));
        }

        ValidateGender(gender);
        ValidateEmploymentType(employmentType, contractorId);
        ValidateDates(joiningDate, leavingDate);
        ValidateOptionalIdentifiers(laborCategoryId, contractorId);

        var resolvedDisplayName = string.IsNullOrWhiteSpace(displayName)
            ? BuildDisplayName(firstName, lastName)
            : displayName.Trim();

        if (string.IsNullOrWhiteSpace(resolvedDisplayName))
        {
            throw new ArgumentException("A display name is required.", nameof(displayName));
        }

        FirstName = firstName.Trim();
        LastName = NormalizeOptional(lastName);
        DisplayName = resolvedDisplayName;
        Gender = gender;
        EmploymentType = employmentType;
        LaborCategoryId = laborCategoryId;
        ContractorId = contractorId;
        JoiningDate = joiningDate;
        LeavingDate = leavingDate;
        MobileNumber = NormalizeOptional(mobileNumber);
        AlternateMobileNumber = NormalizeOptional(alternateMobileNumber);
        Notes = NormalizeOptional(notes);
        UpdatedAt = now;
        UpdatedBy = updatedBy;
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

    public void ValidateOrganizationBoundary(Contractor? contractor, LaborCategory? laborCategory)
    {
        if (contractor is not null && contractor.OrganizationId != OrganizationId)
        {
            throw new InvalidOperationException("The contractor belongs to a different organization.");
        }

        if (laborCategory is not null && !laborCategory.IsSystem && laborCategory.OrganizationId != OrganizationId)
        {
            throw new InvalidOperationException("The labor category belongs to a different organization.");
        }
    }

    private static void ValidateGender(Gender gender)
    {
        if (!Enum.IsDefined(gender))
        {
            throw new ArgumentOutOfRangeException(nameof(gender), "The gender is invalid.");
        }
    }

    private static void ValidateEmploymentType(EmploymentType employmentType, Guid? contractorId)
    {
        if (!Enum.IsDefined(employmentType))
        {
            throw new ArgumentOutOfRangeException(nameof(employmentType), "The employment type is invalid.");
        }

        if (employmentType == EmploymentType.Contract && (contractorId is null || contractorId == Guid.Empty))
        {
            throw new ArgumentException("A contractor is required for contract workers.", nameof(contractorId));
        }
    }

    private static void ValidateDates(DateOnly? joiningDate, DateOnly? leavingDate)
    {
        if (joiningDate.HasValue && leavingDate.HasValue && leavingDate.Value < joiningDate.Value)
        {
            throw new ArgumentException("The leaving date cannot be before the joining date.", nameof(leavingDate));
        }
    }

    private static void ValidateOptionalIdentifiers(Guid? laborCategoryId, Guid? contractorId)
    {
        if (laborCategoryId == Guid.Empty)
        {
            throw new ArgumentException("Labor category identifier must be valid.", nameof(laborCategoryId));
        }

        if (contractorId == Guid.Empty)
        {
            throw new ArgumentException("Contractor identifier must be valid.", nameof(contractorId));
        }
    }

    private static string BuildDisplayName(string firstName, string? lastName)
    {
        var trimmedFirst = firstName.Trim();
        var trimmedLast = NormalizeOptional(lastName);
        return string.IsNullOrWhiteSpace(trimmedLast)
            ? trimmedFirst
            : $"{trimmedFirst} {trimmedLast}";
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
