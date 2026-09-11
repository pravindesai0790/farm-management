namespace FarmManagement.Domain.Entities;

public sealed class WorkerFarmAssignment
{
    private WorkerFarmAssignment()
    {
    }

    public WorkerFarmAssignment(
        Guid organizationId,
        Guid workerId,
        Guid farmId,
        DateOnly assignedFrom,
        Guid createdBy,
        DateOnly? assignedTo = null,
        string? notes = null)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("An organization is required.", nameof(organizationId));
        }

        if (workerId == Guid.Empty)
        {
            throw new ArgumentException("A worker is required.", nameof(workerId));
        }

        if (farmId == Guid.Empty)
        {
            throw new ArgumentException("A farm is required.", nameof(farmId));
        }

        if (createdBy == Guid.Empty)
        {
            throw new ArgumentException("A creating user is required.", nameof(createdBy));
        }

        ValidateDates(assignedFrom, assignedTo);

        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        WorkerId = workerId;
        FarmId = farmId;
        AssignedFrom = assignedFrom;
        AssignedTo = assignedTo;
        Notes = NormalizeOptional(notes);
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
        CreatedBy = createdBy;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid WorkerId { get; private set; }
    public Guid FarmId { get; private set; }
    public DateOnly AssignedFrom { get; private set; }
    public DateOnly? AssignedTo { get; private set; }
    public bool IsActive { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public Organization? Organization { get; private set; }
    public Worker? Worker { get; private set; }
    public Farm? Farm { get; private set; }

    public void Update(
        DateOnly assignedFrom,
        DateOnly? assignedTo,
        string? notes,
        DateTimeOffset now,
        Guid updatedBy)
    {
        if (updatedBy == Guid.Empty)
        {
            throw new ArgumentException("A user is required.", nameof(updatedBy));
        }

        ValidateDates(assignedFrom, assignedTo);

        AssignedFrom = assignedFrom;
        AssignedTo = assignedTo;
        Notes = NormalizeOptional(notes);
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    public bool EndAssignment(DateOnly endDate, DateTimeOffset now, Guid updatedBy)
    {
        if (updatedBy == Guid.Empty)
        {
            throw new ArgumentException("A user is required.", nameof(updatedBy));
        }

        if (endDate < AssignedFrom)
        {
            throw new ArgumentException("The end date cannot be earlier than assigned from date.", nameof(endDate));
        }

        AssignedTo = endDate;
        IsActive = false;
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

    private static void ValidateDates(DateOnly assignedFrom, DateOnly? assignedTo)
    {
        if (assignedTo.HasValue && assignedTo.Value < assignedFrom)
        {
            throw new ArgumentException("The assigned to date cannot be before assigned from date.", nameof(assignedTo));
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
