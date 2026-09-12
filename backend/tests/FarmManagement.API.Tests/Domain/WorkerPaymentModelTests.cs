using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Domain;

public sealed class WorkerPaymentModelTests
{
    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly Guid _workerId = Guid.NewGuid();
    private readonly Guid _currencyId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public void CreateAdvance_WithValidParameters_CreatesCompletedAdvance()
    {
        var paymentDate = new DateOnly(2026, 9, 1);

        var payment = WorkerPayment.CreateAdvance(
            organizationId: _organizationId,
            workerId: _workerId,
            paymentDate: paymentDate,
            amount: 1000m,
            currencyId: _currencyId,
            paymentMethod: PaymentMethod.Cash,
            createdBy: _userId,
            referenceNumber: "ADV-001",
            notes: "Festival advance");

        Assert.NotEqual(Guid.Empty, payment.Id);
        Assert.Equal(_organizationId, payment.OrganizationId);
        Assert.Equal(_workerId, payment.WorkerId);
        Assert.Equal(paymentDate, payment.PaymentDate);
        Assert.Equal(PaymentType.Advance, payment.PaymentType);
        Assert.Equal(1000m, payment.Amount);
        Assert.Equal(_currencyId, payment.CurrencyId);
        Assert.Equal(PaymentMethod.Cash, payment.PaymentMethod);
        Assert.Equal("ADV-001", payment.ReferenceNumber);
        Assert.Equal(PaymentStatus.Completed, payment.Status);
        Assert.Equal("Festival advance", payment.Notes);
        Assert.Null(payment.CancelledAt);
        Assert.Null(payment.CancelledBy);
        Assert.Null(payment.CancellationReason);
        Assert.Equal(_userId, payment.CreatedBy);
    }

    [Fact]
    public void CreatePayout_WithValidPeriod_CreatesCompletedPayout()
    {
        var paymentDate = new DateOnly(2026, 9, 15);
        var periodFrom = new DateOnly(2026, 9, 1);
        var periodTo = new DateOnly(2026, 9, 15);

        var payment = WorkerPayment.CreatePayout(
            organizationId: _organizationId,
            workerId: _workerId,
            paymentDate: paymentDate,
            amount: 3500m,
            currencyId: _currencyId,
            paymentMethod: PaymentMethod.BankTransfer,
            createdBy: _userId,
            paymentPeriodFrom: periodFrom,
            paymentPeriodTo: periodTo,
            referenceNumber: "TXN123456",
            notes: "Mid-month payout");

        Assert.Equal(PaymentType.Payout, payment.PaymentType);
        Assert.Equal(3500m, payment.Amount);
        Assert.Equal(PaymentMethod.BankTransfer, payment.PaymentMethod);
        Assert.Equal(periodFrom, payment.PaymentPeriodFrom);
        Assert.Equal(periodTo, payment.PaymentPeriodTo);
        Assert.Equal("TXN123456", payment.ReferenceNumber);
        Assert.Equal(PaymentStatus.Completed, payment.Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-500)]
    public void Create_WhenAmountIsZeroOrNegative_ThrowsArgumentOutOfRangeException(decimal amount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            WorkerPayment.Create(
                _organizationId,
                _workerId,
                new DateOnly(2026, 9, 1),
                PaymentType.Advance,
                amount,
                _currencyId,
                PaymentMethod.Cash,
                _userId));
    }

    [Fact]
    public void Create_WhenPeriodToIsBeforePeriodFrom_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            WorkerPayment.Create(
                _organizationId,
                _workerId,
                new DateOnly(2026, 9, 1),
                PaymentType.Payout,
                1000m,
                _currencyId,
                PaymentMethod.Cash,
                _userId,
                paymentPeriodFrom: new DateOnly(2026, 9, 15),
                paymentPeriodTo: new DateOnly(2026, 9, 10)));
    }

    [Fact]
    public void Create_WhenOrganizationIdIsEmpty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            WorkerPayment.Create(
                Guid.Empty,
                _workerId,
                new DateOnly(2026, 9, 1),
                PaymentType.Advance,
                1000m,
                _currencyId,
                PaymentMethod.Cash,
                _userId));
    }

    [Fact]
    public void Create_WithCancelledStatus_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            WorkerPayment.Create(
                _organizationId,
                _workerId,
                new DateOnly(2026, 9, 1),
                PaymentType.Advance,
                1000m,
                _currencyId,
                PaymentMethod.Cash,
                _userId,
                status: PaymentStatus.Cancelled));
    }

    [Fact]
    public void Cancel_WhenCompleted_TransitionsToCancelledWithAudit()
    {
        var payment = WorkerPayment.CreateAdvance(
            _organizationId,
            _workerId,
            new DateOnly(2026, 9, 1),
            1000m,
            _currencyId,
            PaymentMethod.Cash,
            _userId);

        Assert.Equal(PaymentStatus.Completed, payment.Status);

        var now = DateTimeOffset.UtcNow;
        var cancelled = payment.Cancel(now, _userId, "Paid by mistake");

        Assert.True(cancelled);
        Assert.Equal(PaymentStatus.Cancelled, payment.Status);
        Assert.Equal(now, payment.CancelledAt);
        Assert.Equal(_userId, payment.CancelledBy);
        Assert.Equal("Paid by mistake", payment.CancellationReason);
        Assert.Equal(now, payment.UpdatedAt);
        Assert.Equal(_userId, payment.UpdatedBy);

        // Idempotent cancel call returns false
        Assert.False(payment.Cancel(now, _userId, "Duplicate"));
    }

    [Fact]
    public void Complete_WhenPending_TransitionsToCompleted()
    {
        var payment = WorkerPayment.Create(
            _organizationId,
            _workerId,
            new DateOnly(2026, 9, 1),
            PaymentType.Payout,
            2000m,
            _currencyId,
            PaymentMethod.BankTransfer,
            _userId,
            status: PaymentStatus.Pending);

        Assert.Equal(PaymentStatus.Pending, payment.Status);

        var now = DateTimeOffset.UtcNow;
        var completed = payment.Complete(now, _userId);

        Assert.True(completed);
        Assert.Equal(PaymentStatus.Completed, payment.Status);

        // Idempotent call returns false
        Assert.False(payment.Complete(now, _userId));
    }

    [Fact]
    public void Complete_WhenCancelled_ThrowsInvalidOperationException()
    {
        var payment = WorkerPayment.Create(
            _organizationId,
            _workerId,
            new DateOnly(2026, 9, 1),
            PaymentType.Payout,
            2000m,
            _currencyId,
            PaymentMethod.BankTransfer,
            _userId,
            status: PaymentStatus.Pending);

        payment.Cancel(DateTimeOffset.UtcNow, _userId, "Invalid");

        Assert.Throws<InvalidOperationException>(() =>
            payment.Complete(DateTimeOffset.UtcNow, _userId));
    }

    [Fact]
    public void ValidateOrganizationBoundary_WhenWorkerBelongsToDifferentOrg_ThrowsInvalidOperationException()
    {
        var payment = WorkerPayment.CreateAdvance(
            _organizationId,
            _workerId,
            new DateOnly(2026, 9, 1),
            1000m,
            _currencyId,
            PaymentMethod.Cash,
            _userId);

        var foreignWorker = new Worker(
            Guid.NewGuid(),
            "Foreign",
            "Worker",
            Gender.Male,
            EmploymentType.Permanent,
            _userId);

        Assert.Throws<InvalidOperationException>(() =>
            payment.ValidateOrganizationBoundary(foreignWorker, null));
    }
}
