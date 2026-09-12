using FarmManagement.Domain.Enums;

namespace FarmManagement.Domain.Entities;

public sealed class WorkerEarningsLedger
{
    private WorkerEarningsLedger()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid WorkerId { get; private set; }
    public Guid? AttendanceId { get; private set; }
    public DateOnly EarningsDate { get; private set; }
    public WageType WageType { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal WageRate { get; private set; }
    public Guid CurrencyId { get; private set; }
    public decimal GrossAmount { get; private set; }
    public EarningsEntryType EntryType { get; private set; }
    public EarningsLedgerStatus Status { get; private set; }
    public Guid? ReferenceLedgerId { get; private set; }
    public string? Description { get; private set; }
    public DateTimeOffset? FinalizedAt { get; private set; }
    public Guid? FinalizedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public Organization? Organization { get; private set; }
    public Worker? Worker { get; private set; }
    public Currency? Currency { get; private set; }
    public WorkerEarningsLedger? ReferenceLedger { get; private set; }

    public static WorkerEarningsLedger CreateEarning(
        Guid organizationId,
        Guid workerId,
        DateOnly earningsDate,
        WageType wageType,
        decimal quantity,
        decimal wageRate,
        Guid currencyId,
        Guid createdBy,
        Guid? attendanceId = null,
        decimal? grossAmount = null,
        bool approveImmediately = false,
        Guid? referenceLedgerId = null,
        string? description = null)
    {
        ValidateCommonIdentifiers(organizationId, workerId, currencyId, createdBy);

        if (!Enum.IsDefined(wageType))
        {
            throw new ArgumentOutOfRangeException(nameof(wageType), "The wage type is invalid.");
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "The quantity must be greater than zero.");
        }

        if (wageRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(wageRate), "The wage rate must be greater than zero.");
        }

        var calculatedGross = grossAmount ?? Math.Round(quantity * wageRate, 2);
        if (calculatedGross <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(grossAmount), "The gross amount for an earning entry must be greater than zero.");
        }

        if (attendanceId == Guid.Empty)
        {
            throw new ArgumentException("The attendance ID cannot be an empty GUID.", nameof(attendanceId));
        }

        if (referenceLedgerId == Guid.Empty)
        {
            throw new ArgumentException("The reference ledger ID cannot be an empty GUID.", nameof(referenceLedgerId));
        }

        var now = DateTimeOffset.UtcNow;
        var entry = new WorkerEarningsLedger
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            WorkerId = workerId,
            AttendanceId = attendanceId,
            EarningsDate = earningsDate,
            WageType = wageType,
            Quantity = quantity,
            WageRate = wageRate,
            CurrencyId = currencyId,
            GrossAmount = calculatedGross,
            EntryType = EarningsEntryType.Earning,
            Status = approveImmediately ? EarningsLedgerStatus.Approved : EarningsLedgerStatus.Calculated,
            ReferenceLedgerId = referenceLedgerId,
            Description = NormalizeOptional(description),
            FinalizedAt = approveImmediately ? now : null,
            FinalizedBy = approveImmediately ? createdBy : null,
            CreatedAt = now,
            CreatedBy = createdBy
        };

        return entry;
    }

    public static WorkerEarningsLedger CreateReversal(
        WorkerEarningsLedger original,
        Guid createdBy,
        DateTimeOffset now,
        string? description = null,
        DateOnly? reversalDate = null)
    {
        ArgumentNullException.ThrowIfNull(original);

        if (createdBy == Guid.Empty)
        {
            throw new ArgumentException("A creating user is required.", nameof(createdBy));
        }

        if (original.Status == EarningsLedgerStatus.Reversed)
        {
            throw new InvalidOperationException("The ledger entry has already been reversed.");
        }

        if (original.EntryType == EarningsEntryType.Reversal)
        {
            throw new InvalidOperationException("A reversal entry cannot be reversed.");
        }

        var entry = new WorkerEarningsLedger
        {
            Id = Guid.NewGuid(),
            OrganizationId = original.OrganizationId,
            WorkerId = original.WorkerId,
            AttendanceId = original.AttendanceId,
            EarningsDate = reversalDate ?? original.EarningsDate,
            WageType = original.WageType,
            Quantity = original.Quantity,
            WageRate = original.WageRate,
            CurrencyId = original.CurrencyId,
            GrossAmount = -Math.Abs(original.GrossAmount),
            EntryType = EarningsEntryType.Reversal,
            Status = EarningsLedgerStatus.Reversed,
            ReferenceLedgerId = original.Id,
            Description = NormalizeOptional(description) ?? $"Reversal of ledger entry {original.Id}",
            FinalizedAt = now,
            FinalizedBy = createdBy,
            CreatedAt = now,
            CreatedBy = createdBy
        };

        return entry;
    }

    public static WorkerEarningsLedger CreateAdjustment(
        Guid organizationId,
        Guid workerId,
        DateOnly earningsDate,
        WageType wageType,
        decimal quantity,
        decimal wageRate,
        decimal grossAmount,
        Guid currencyId,
        Guid createdBy,
        bool approveImmediately = false,
        Guid? referenceLedgerId = null,
        string? description = null)
    {
        ValidateCommonIdentifiers(organizationId, workerId, currencyId, createdBy);

        if (!Enum.IsDefined(wageType))
        {
            throw new ArgumentOutOfRangeException(nameof(wageType), "The wage type is invalid.");
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "The quantity must be greater than zero.");
        }

        if (wageRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(wageRate), "The wage rate must be greater than zero.");
        }

        if (grossAmount == 0)
        {
            throw new ArgumentException("An adjustment gross amount cannot be zero.", nameof(grossAmount));
        }

        if (referenceLedgerId == Guid.Empty)
        {
            throw new ArgumentException("The reference ledger ID cannot be an empty GUID.", nameof(referenceLedgerId));
        }

        var now = DateTimeOffset.UtcNow;
        var entry = new WorkerEarningsLedger
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            WorkerId = workerId,
            AttendanceId = null,
            EarningsDate = earningsDate,
            WageType = wageType,
            Quantity = quantity,
            WageRate = wageRate,
            CurrencyId = currencyId,
            GrossAmount = grossAmount,
            EntryType = EarningsEntryType.Adjustment,
            Status = approveImmediately ? EarningsLedgerStatus.Approved : EarningsLedgerStatus.Calculated,
            ReferenceLedgerId = referenceLedgerId,
            Description = NormalizeOptional(description),
            FinalizedAt = approveImmediately ? now : null,
            FinalizedBy = approveImmediately ? createdBy : null,
            CreatedAt = now,
            CreatedBy = createdBy
        };

        return entry;
    }

    public bool Approve(DateTimeOffset now, Guid approvedBy)
    {
        if (approvedBy == Guid.Empty)
        {
            throw new ArgumentException("A user is required.", nameof(approvedBy));
        }

        if (Status == EarningsLedgerStatus.Approved)
        {
            return false;
        }

        if (Status == EarningsLedgerStatus.Reversed)
        {
            throw new InvalidOperationException("A reversed ledger entry cannot be approved.");
        }

        Status = EarningsLedgerStatus.Approved;
        FinalizedAt = now;
        FinalizedBy = approvedBy;
        UpdatedAt = now;
        UpdatedBy = approvedBy;
        return true;
    }

    public bool MarkReversed(DateTimeOffset now, Guid reversedBy)
    {
        if (reversedBy == Guid.Empty)
        {
            throw new ArgumentException("A user is required.", nameof(reversedBy));
        }

        if (Status == EarningsLedgerStatus.Reversed)
        {
            return false;
        }

        Status = EarningsLedgerStatus.Reversed;
        UpdatedAt = now;
        UpdatedBy = reversedBy;
        return true;
    }

    public void ValidateOrganizationBoundary(Worker? worker, Currency? currency, WorkerEarningsLedger? referenceLedger)
    {
        if (worker is not null && worker.OrganizationId != OrganizationId)
        {
            throw new InvalidOperationException("The worker belongs to a different organization.");
        }

        if (referenceLedger is not null)
        {
            if (referenceLedger.OrganizationId != OrganizationId)
            {
                throw new InvalidOperationException("The referenced ledger entry belongs to a different organization.");
            }

            if (referenceLedger.WorkerId != WorkerId)
            {
                throw new InvalidOperationException("The referenced ledger entry belongs to a different worker.");
            }
        }
    }

    private static void ValidateCommonIdentifiers(Guid organizationId, Guid workerId, Guid currencyId, Guid createdBy)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("An organization is required.", nameof(organizationId));
        }

        if (workerId == Guid.Empty)
        {
            throw new ArgumentException("A worker is required.", nameof(workerId));
        }

        if (currencyId == Guid.Empty)
        {
            throw new ArgumentException("A currency is required.", nameof(currencyId));
        }

        if (createdBy == Guid.Empty)
        {
            throw new ArgumentException("A creating user is required.", nameof(createdBy));
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
