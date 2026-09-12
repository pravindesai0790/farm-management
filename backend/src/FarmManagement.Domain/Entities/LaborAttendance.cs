using FarmManagement.Domain.Enums;

namespace FarmManagement.Domain.Entities;

public sealed class LaborAttendance
{
    private LaborAttendance()
    {
    }

    public LaborAttendance(
        Guid organizationId,
        Guid farmId,
        Guid workerId,
        DateOnly attendanceDate,
        AttendanceType attendanceType,
        Guid createdBy,
        decimal? workingHours = null,
        decimal? calculatedRate = null,
        decimal? calculatedAmount = null,
        Guid? currencyId = null,
        AttendanceStatus status = AttendanceStatus.Draft,
        string? notes = null,
        DateTimeOffset? finalizedAt = null,
        Guid? finalizedBy = null)
    {
        ValidateRequiredIdentifiers(organizationId, farmId, workerId, createdBy);
        ValidateAttendanceType(attendanceType);
        ValidateStatus(status);
        ValidateWorkingHours(attendanceType, ref workingHours);
        ValidateEarnings(attendanceType, ref calculatedRate, ref calculatedAmount, currencyId);
        ValidateFinalization(status, ref finalizedAt, ref finalizedBy);

        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        FarmId = farmId;
        WorkerId = workerId;
        AttendanceDate = attendanceDate;
        AttendanceType = attendanceType;
        WorkingHours = workingHours;
        CalculatedRate = calculatedRate;
        CalculatedAmount = calculatedAmount;
        CurrencyId = currencyId;
        Status = status;
        Notes = NormalizeOptional(notes);
        FinalizedAt = finalizedAt;
        FinalizedBy = finalizedBy;
        CreatedAt = DateTimeOffset.UtcNow;
        CreatedBy = createdBy;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid WorkerId { get; private set; }
    public DateOnly AttendanceDate { get; private set; }
    public AttendanceType AttendanceType { get; private set; }
    public decimal? WorkingHours { get; private set; }
    public decimal? CalculatedRate { get; private set; }
    public decimal? CalculatedAmount { get; private set; }
    public Guid? CurrencyId { get; private set; }
    public AttendanceStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset? FinalizedAt { get; private set; }
    public Guid? FinalizedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public Organization? Organization { get; private set; }
    public Farm? Farm { get; private set; }
    public Worker? Worker { get; private set; }
    public Currency? Currency { get; private set; }

    public bool IsFinalized => Status == AttendanceStatus.Finalized;
    public bool IsDraft => Status == AttendanceStatus.Draft;
    public bool GeneratesPayrollEarnings =>
        AttendanceType != AttendanceType.NotWorked &&
        CalculatedAmount.HasValue &&
        CalculatedAmount.Value > 0m;

    public void Update(
        AttendanceType attendanceType,
        decimal? workingHours,
        string? notes,
        DateTimeOffset now,
        Guid updatedBy)
    {
        if (Status == AttendanceStatus.Finalized)
        {
            throw new InvalidOperationException("Finalized attendance records cannot be modified.");
        }

        if (updatedBy == Guid.Empty)
        {
            throw new ArgumentException("A user is required.", nameof(updatedBy));
        }

        ValidateAttendanceType(attendanceType);
        ValidateWorkingHours(attendanceType, ref workingHours);

        AttendanceType = attendanceType;
        WorkingHours = workingHours;
        Notes = NormalizeOptional(notes);

        if (attendanceType == AttendanceType.NotWorked)
        {
            CalculatedRate = null;
            CalculatedAmount = 0m;
        }

        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    public void SetCalculatedEarnings(
        decimal? calculatedRate,
        decimal? calculatedAmount,
        Guid? currencyId,
        DateTimeOffset now,
        Guid updatedBy)
    {
        if (Status == AttendanceStatus.Finalized)
        {
            throw new InvalidOperationException("Cannot update calculated earnings on a finalized attendance record.");
        }

        if (updatedBy == Guid.Empty)
        {
            throw new ArgumentException("A user is required.", nameof(updatedBy));
        }

        ValidateEarnings(AttendanceType, ref calculatedRate, ref calculatedAmount, currencyId);

        CalculatedRate = calculatedRate;
        CalculatedAmount = calculatedAmount;
        CurrencyId = currencyId;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    public void Finalize(
        DateTimeOffset now,
        Guid finalizedBy,
        decimal? finalRate = null,
        decimal? finalAmount = null,
        Guid? currencyId = null)
    {
        if (Status == AttendanceStatus.Finalized)
        {
            throw new InvalidOperationException("The attendance record is already finalized.");
        }

        if (finalizedBy == Guid.Empty)
        {
            throw new ArgumentException("A finalizing user is required.", nameof(finalizedBy));
        }

        if (AttendanceType == AttendanceType.NotWorked)
        {
            CalculatedRate = null;
            CalculatedAmount = 0m;
        }
        else if (finalRate.HasValue || finalAmount.HasValue || currencyId.HasValue)
        {
            ValidateEarnings(AttendanceType, ref finalRate, ref finalAmount, currencyId);
            CalculatedRate = finalRate ?? CalculatedRate;
            CalculatedAmount = finalAmount ?? CalculatedAmount;
            CurrencyId = currencyId ?? CurrencyId;
        }

        Status = AttendanceStatus.Finalized;
        FinalizedAt = now;
        FinalizedBy = finalizedBy;
        UpdatedAt = now;
        UpdatedBy = finalizedBy;
    }

    public bool ConflictsWith(LaborAttendance other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return OrganizationId == other.OrganizationId &&
               WorkerId == other.WorkerId &&
               AttendanceDate == other.AttendanceDate &&
               Id != other.Id;
    }

    public static LaborAttendance CreateDraft(
        Guid organizationId,
        Guid farmId,
        Guid workerId,
        DateOnly attendanceDate,
        AttendanceType attendanceType,
        Guid createdBy,
        decimal? workingHours = null,
        decimal? calculatedRate = null,
        decimal? calculatedAmount = null,
        Guid? currencyId = null,
        string? notes = null) =>
        new(
            organizationId,
            farmId,
            workerId,
            attendanceDate,
            attendanceType,
            createdBy,
            workingHours,
            calculatedRate,
            calculatedAmount,
            currencyId,
            status: AttendanceStatus.Draft,
            notes: notes);

    public static LaborAttendance CreateFinalized(
        Guid organizationId,
        Guid farmId,
        Guid workerId,
        DateOnly attendanceDate,
        AttendanceType attendanceType,
        Guid finalizedBy,
        decimal? workingHours = null,
        decimal? calculatedRate = null,
        decimal? calculatedAmount = null,
        Guid? currencyId = null,
        string? notes = null) =>
        new(
            organizationId,
            farmId,
            workerId,
            attendanceDate,
            attendanceType,
            createdBy: finalizedBy,
            workingHours: workingHours,
            calculatedRate: calculatedRate,
            calculatedAmount: calculatedAmount,
            currencyId: currencyId,
            status: AttendanceStatus.Finalized,
            notes: notes,
            finalizedAt: DateTimeOffset.UtcNow,
            finalizedBy: finalizedBy);

    private static void ValidateRequiredIdentifiers(
        Guid organizationId,
        Guid farmId,
        Guid workerId,
        Guid createdBy)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("An organization is required.", nameof(organizationId));
        }

        if (farmId == Guid.Empty)
        {
            throw new ArgumentException("A farm is required.", nameof(farmId));
        }

        if (workerId == Guid.Empty)
        {
            throw new ArgumentException("A worker is required.", nameof(workerId));
        }

        if (createdBy == Guid.Empty)
        {
            throw new ArgumentException("A creating user is required.", nameof(createdBy));
        }
    }

    private static void ValidateAttendanceType(AttendanceType attendanceType)
    {
        if (!Enum.IsDefined(attendanceType))
        {
            throw new ArgumentOutOfRangeException(nameof(attendanceType), "The attendance type is invalid.");
        }
    }

    private static void ValidateStatus(AttendanceStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), "The attendance status is invalid.");
        }
    }

    private static void ValidateWorkingHours(AttendanceType attendanceType, ref decimal? workingHours)
    {
        if (attendanceType == AttendanceType.Hourly)
        {
            if (!workingHours.HasValue || workingHours.Value <= 0)
            {
                throw new ArgumentException("Working hours are required and must be greater than zero for hourly attendance.", nameof(workingHours));
            }
        }
        else
        {
            if (workingHours.HasValue && workingHours.Value != 0)
            {
                throw new ArgumentException($"Working hours cannot be specified for {attendanceType} attendance.", nameof(workingHours));
            }

            workingHours = null;
        }
    }

    private static void ValidateEarnings(
        AttendanceType attendanceType,
        ref decimal? calculatedRate,
        ref decimal? calculatedAmount,
        Guid? currencyId)
    {
        if (currencyId.HasValue && currencyId.Value == Guid.Empty)
        {
            throw new ArgumentException("Currency identifier must be valid.", nameof(currencyId));
        }

        if (attendanceType == AttendanceType.NotWorked)
        {
            if (calculatedAmount.HasValue && calculatedAmount.Value > 0)
            {
                throw new ArgumentException("Not-worked attendance must have zero earnings.", nameof(calculatedAmount));
            }

            calculatedRate = null;
            calculatedAmount = 0m;
            return;
        }

        if (calculatedRate.HasValue && calculatedRate.Value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(calculatedRate), "The calculated rate cannot be negative.");
        }

        if (calculatedAmount.HasValue && calculatedAmount.Value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(calculatedAmount), "The calculated amount cannot be negative.");
        }
    }

    private static void ValidateFinalization(
        AttendanceStatus status,
        ref DateTimeOffset? finalizedAt,
        ref Guid? finalizedBy)
    {
        if (status == AttendanceStatus.Finalized)
        {
            if (!finalizedBy.HasValue || finalizedBy.Value == Guid.Empty)
            {
                throw new ArgumentException("A finalizing user is required when status is finalized.", nameof(finalizedBy));
            }

            finalizedAt ??= DateTimeOffset.UtcNow;
        }
        else
        {
            finalizedAt = null;
            finalizedBy = null;
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
