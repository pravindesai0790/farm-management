using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Domain;

public sealed class WorkerEarningsLedgerModelTests
{
    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly Guid _workerId = Guid.NewGuid();
    private readonly Guid _currencyId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public void CreateEarning_WithValidParameters_CreatesCalculatedEntry()
    {
        var attendanceId = Guid.NewGuid();
        var date = new DateOnly(2026, 9, 1);

        var entry = WorkerEarningsLedger.CreateEarning(
            organizationId: _organizationId,
            workerId: _workerId,
            earningsDate: date,
            wageType: WageType.FullDay,
            quantity: 1m,
            wageRate: 500m,
            currencyId: _currencyId,
            createdBy: _userId,
            attendanceId: attendanceId,
            description: "Daily attendance wage");

        Assert.NotEqual(Guid.Empty, entry.Id);
        Assert.Equal(_organizationId, entry.OrganizationId);
        Assert.Equal(_workerId, entry.WorkerId);
        Assert.Equal(attendanceId, entry.AttendanceId);
        Assert.Equal(date, entry.EarningsDate);
        Assert.Equal(WageType.FullDay, entry.WageType);
        Assert.Equal(1m, entry.Quantity);
        Assert.Equal(500m, entry.WageRate);
        Assert.Equal(500m, entry.GrossAmount);
        Assert.Equal(_currencyId, entry.CurrencyId);
        Assert.Equal(EarningsEntryType.Earning, entry.EntryType);
        Assert.Equal(EarningsLedgerStatus.Calculated, entry.Status);
        Assert.Null(entry.ReferenceLedgerId);
        Assert.Equal("Daily attendance wage", entry.Description);
        Assert.Null(entry.FinalizedAt);
        Assert.Null(entry.FinalizedBy);
        Assert.Equal(_userId, entry.CreatedBy);
    }

    [Fact]
    public void CreateEarning_WithApproveImmediately_CreatesApprovedEntry()
    {
        var date = new DateOnly(2026, 9, 2);

        var entry = WorkerEarningsLedger.CreateEarning(
            organizationId: _organizationId,
            workerId: _workerId,
            earningsDate: date,
            wageType: WageType.HalfDay,
            quantity: 1m,
            wageRate: 250m,
            currencyId: _currencyId,
            createdBy: _userId,
            approveImmediately: true);

        Assert.Equal(EarningsLedgerStatus.Approved, entry.Status);
        Assert.NotNull(entry.FinalizedAt);
        Assert.Equal(_userId, entry.FinalizedBy);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void CreateEarning_WhenQuantityIsZeroOrNegative_ThrowsArgumentOutOfRangeException(decimal quantity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            WorkerEarningsLedger.CreateEarning(
                _organizationId,
                _workerId,
                new DateOnly(2026, 9, 1),
                WageType.FullDay,
                quantity,
                500m,
                _currencyId,
                _userId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public void CreateEarning_WhenWageRateIsZeroOrNegative_ThrowsArgumentOutOfRangeException(decimal wageRate)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            WorkerEarningsLedger.CreateEarning(
                _organizationId,
                _workerId,
                new DateOnly(2026, 9, 1),
                WageType.FullDay,
                1m,
                wageRate,
                _currencyId,
                _userId));
    }

    [Fact]
    public void CreateEarning_WhenGrossAmountIsZeroOrNegative_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            WorkerEarningsLedger.CreateEarning(
                _organizationId,
                _workerId,
                new DateOnly(2026, 9, 1),
                WageType.FullDay,
                1m,
                500m,
                _currencyId,
                _userId,
                grossAmount: 0m));
    }

    [Fact]
    public void CreateEarning_WhenOrganizationIdIsEmpty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            WorkerEarningsLedger.CreateEarning(
                Guid.Empty,
                _workerId,
                new DateOnly(2026, 9, 1),
                WageType.FullDay,
                1m,
                500m,
                _currencyId,
                _userId));
    }

    [Fact]
    public void CreateReversal_WithValidOriginalEntry_CreatesNegativeAmountReversal()
    {
        var original = WorkerEarningsLedger.CreateEarning(
            _organizationId,
            _workerId,
            new DateOnly(2026, 9, 1),
            WageType.FullDay,
            1m,
            500m,
            _currencyId,
            _userId,
            approveImmediately: true);

        var now = DateTimeOffset.UtcNow;
        var reversal = WorkerEarningsLedger.CreateReversal(original, _userId, now, "Incorrect attendance recorded");

        Assert.NotEqual(original.Id, reversal.Id);
        Assert.Equal(original.OrganizationId, reversal.OrganizationId);
        Assert.Equal(original.WorkerId, reversal.WorkerId);
        Assert.Equal(original.Id, reversal.ReferenceLedgerId);
        Assert.Equal(-500m, reversal.GrossAmount);
        Assert.Equal(500m, reversal.WageRate);
        Assert.Equal(1m, reversal.Quantity);
        Assert.Equal(WageType.FullDay, reversal.WageType);
        Assert.Equal(_currencyId, reversal.CurrencyId);
        Assert.Equal(EarningsEntryType.Reversal, reversal.EntryType);
        Assert.Equal(EarningsLedgerStatus.Reversed, reversal.Status);
        Assert.Equal("Incorrect attendance recorded", reversal.Description);
        Assert.Equal(now, reversal.FinalizedAt);
        Assert.Equal(_userId, reversal.FinalizedBy);
    }

    [Fact]
    public void CreateReversal_WhenOriginalAlreadyReversed_ThrowsInvalidOperationException()
    {
        var original = WorkerEarningsLedger.CreateEarning(
            _organizationId,
            _workerId,
            new DateOnly(2026, 9, 1),
            WageType.FullDay,
            1m,
            500m,
            _currencyId,
            _userId);

        original.MarkReversed(DateTimeOffset.UtcNow, _userId);

        Assert.Throws<InvalidOperationException>(() =>
            WorkerEarningsLedger.CreateReversal(original, _userId, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void CreateReversal_WhenOriginalIsReversalEntry_ThrowsInvalidOperationException()
    {
        var original = WorkerEarningsLedger.CreateEarning(
            _organizationId,
            _workerId,
            new DateOnly(2026, 9, 1),
            WageType.FullDay,
            1m,
            500m,
            _currencyId,
            _userId);

        var reversal = WorkerEarningsLedger.CreateReversal(original, _userId, DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            WorkerEarningsLedger.CreateReversal(reversal, _userId, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void CreateAdjustment_WithValidValues_Succeeds()
    {
        var adjustment = WorkerEarningsLedger.CreateAdjustment(
            organizationId: _organizationId,
            workerId: _workerId,
            earningsDate: new DateOnly(2026, 9, 5),
            wageType: WageType.Hourly,
            quantity: 2m,
            wageRate: 75m,
            grossAmount: 150m,
            currencyId: _currencyId,
            createdBy: _userId,
            approveImmediately: true,
            description: "Overtime compensation adjustment");

        Assert.Equal(EarningsEntryType.Adjustment, adjustment.EntryType);
        Assert.Equal(150m, adjustment.GrossAmount);
        Assert.Equal(EarningsLedgerStatus.Approved, adjustment.Status);
        Assert.Equal("Overtime compensation adjustment", adjustment.Description);
    }

    [Fact]
    public void CreateAdjustment_WithZeroGrossAmount_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            WorkerEarningsLedger.CreateAdjustment(
                _organizationId,
                _workerId,
                new DateOnly(2026, 9, 5),
                WageType.Hourly,
                1m,
                50m,
                grossAmount: 0m,
                currencyId: _currencyId,
                createdBy: _userId));
    }

    [Fact]
    public void Approve_WhenCalculated_TransitionsToApproved()
    {
        var entry = WorkerEarningsLedger.CreateEarning(
            _organizationId,
            _workerId,
            new DateOnly(2026, 9, 1),
            WageType.FullDay,
            1m,
            500m,
            _currencyId,
            _userId);

        Assert.Equal(EarningsLedgerStatus.Calculated, entry.Status);

        var now = DateTimeOffset.UtcNow;
        var approved = entry.Approve(now, _userId);

        Assert.True(approved);
        Assert.Equal(EarningsLedgerStatus.Approved, entry.Status);
        Assert.Equal(now, entry.FinalizedAt);
        Assert.Equal(_userId, entry.FinalizedBy);

        // Calling approve again is idempotent and returns false
        Assert.False(entry.Approve(now, _userId));
    }

    [Fact]
    public void Approve_WhenReversed_ThrowsInvalidOperationException()
    {
        var entry = WorkerEarningsLedger.CreateEarning(
            _organizationId,
            _workerId,
            new DateOnly(2026, 9, 1),
            WageType.FullDay,
            1m,
            500m,
            _currencyId,
            _userId);

        entry.MarkReversed(DateTimeOffset.UtcNow, _userId);

        Assert.Throws<InvalidOperationException>(() =>
            entry.Approve(DateTimeOffset.UtcNow, _userId));
    }

    [Fact]
    public void MarkReversed_WhenApproved_TransitionsToReversed()
    {
        var entry = WorkerEarningsLedger.CreateEarning(
            _organizationId,
            _workerId,
            new DateOnly(2026, 9, 1),
            WageType.FullDay,
            1m,
            500m,
            _currencyId,
            _userId,
            approveImmediately: true);

        var now = DateTimeOffset.UtcNow;
        var reversed = entry.MarkReversed(now, _userId);

        Assert.True(reversed);
        Assert.Equal(EarningsLedgerStatus.Reversed, entry.Status);
        Assert.Equal(now, entry.UpdatedAt);
        Assert.Equal(_userId, entry.UpdatedBy);

        // Idempotent call
        Assert.False(entry.MarkReversed(now, _userId));
    }

    [Fact]
    public void ValidateOrganizationBoundary_WhenEntitiesCrossBoundary_ThrowsInvalidOperationException()
    {
        var otherOrgId = Guid.NewGuid();

        var entry = WorkerEarningsLedger.CreateEarning(
            _organizationId,
            _workerId,
            new DateOnly(2026, 9, 1),
            WageType.FullDay,
            1m,
            500m,
            _currencyId,
            _userId);

        var foreignWorker = new Worker(
            otherOrgId,
            "Foreign",
            "Worker",
            Gender.Male,
            EmploymentType.Permanent,
            _userId);

        Assert.Throws<InvalidOperationException>(() =>
            entry.ValidateOrganizationBoundary(foreignWorker, null, null));

        var foreignRefLedger = WorkerEarningsLedger.CreateEarning(
            otherOrgId,
            _workerId,
            new DateOnly(2026, 9, 1),
            WageType.FullDay,
            1m,
            500m,
            _currencyId,
            _userId);

        Assert.Throws<InvalidOperationException>(() =>
            entry.ValidateOrganizationBoundary(null, null, foreignRefLedger));

        var otherWorkerRefLedger = WorkerEarningsLedger.CreateEarning(
            _organizationId,
            Guid.NewGuid(),
            new DateOnly(2026, 9, 1),
            WageType.FullDay,
            1m,
            500m,
            _currencyId,
            _userId);

        Assert.Throws<InvalidOperationException>(() =>
            entry.ValidateOrganizationBoundary(null, null, otherWorkerRefLedger));
    }
}
