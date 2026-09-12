using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Labor;
using FarmManagement.Application.Interfaces.Labor;
using FarmManagement.Application.Services;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Services;

public sealed class WorkerPaymentServiceTests
{
    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _workerId = Guid.NewGuid();
    private readonly Guid _currencyId = Guid.NewGuid();

    private PaymentActor CreateActor() => new(_userId, _organizationId);

    private (FakeWorkerPaymentStore paymentStore, FakeEarningsStore earningsStore, Worker worker) SetupScenario()
    {
        var paymentStore = new FakeWorkerPaymentStore();
        var earningsStore = new FakeEarningsStore();

        var currency = new Currency("INR", "Indian Rupee", "₹", true, 1, _currencyId);
        paymentStore.Currencies.Add(currency);

        var worker = new Worker(
            _organizationId,
            "Ramesh",
            "Kumar",
            Gender.Male,
            EmploymentType.Permanent,
            _userId);
        SetEntityId(worker, _workerId);
        paymentStore.Workers.Add(worker);

        return (paymentStore, earningsStore, worker);
    }

    #region Record Payment Tests

    [Fact]
    public async Task RecordPaymentAsync_WithValidAdvance_SucceedsAndCreatesAudit()
    {
        var (paymentStore, earningsStore, _) = SetupScenario();
        var service = new WorkerPaymentService(paymentStore, earningsStore);

        var request = new RecordWorkerPaymentRequest(
            WorkerId: _workerId,
            PaymentDate: new DateOnly(2026, 9, 1),
            PaymentType: "ADVANCE",
            Amount: 1000m,
            CurrencyId: _currencyId,
            PaymentMethod: "CASH",
            ReferenceNumber: "ADV-001",
            Notes: "Early month advance");

        var response = await service.RecordPaymentAsync(CreateActor(), request, "127.0.0.1");

        Assert.NotNull(response);
        Assert.Equal(_organizationId, response.OrganizationId);
        Assert.Equal(_workerId, response.WorkerId);
        Assert.Equal("Ramesh Kumar", response.WorkerDisplayName);
        Assert.Equal("ADVANCE", response.PaymentType);
        Assert.Equal(1000m, response.Amount);
        Assert.Equal("INR", response.CurrencyCode);
        Assert.Equal("₹", response.CurrencySymbol);
        Assert.Equal("CASH", response.PaymentMethod);
        Assert.Equal("COMPLETED", response.Status);
        Assert.Equal("ADV-001", response.ReferenceNumber);

        Assert.Single(paymentStore.Payments);
        Assert.Single(paymentStore.AuditLogs);
        Assert.Equal("WorkerPayment.Created", paymentStore.AuditLogs[0].Action);
    }

    [Fact]
    public async Task RecordPaymentAsync_WithPartialPayout_Succeeds()
    {
        var (paymentStore, earningsStore, _) = SetupScenario();
        var service = new WorkerPaymentService(paymentStore, earningsStore);

        var request = new RecordWorkerPaymentRequest(
            WorkerId: _workerId,
            PaymentDate: new DateOnly(2026, 9, 15),
            PaymentType: "PAYOUT",
            Amount: 700m,
            CurrencyId: _currencyId,
            PaymentMethod: "UPI",
            ReferenceNumber: "UPI-987654",
            PaymentPeriodFrom: new DateOnly(2026, 9, 1),
            PaymentPeriodTo: new DateOnly(2026, 9, 15));

        var response = await service.RecordPaymentAsync(CreateActor(), request);

        Assert.Equal("PAYOUT", response.PaymentType);
        Assert.Equal(700m, response.Amount);
        Assert.Equal("UPI", response.PaymentMethod);
        Assert.Equal(new DateOnly(2026, 9, 1), response.PaymentPeriodFrom);
        Assert.Equal(new DateOnly(2026, 9, 15), response.PaymentPeriodTo);
    }

    [Fact]
    public async Task RecordPaymentAsync_WhenWorkerNotFound_ThrowsResourceNotFoundException()
    {
        var (paymentStore, earningsStore, _) = SetupScenario();
        var service = new WorkerPaymentService(paymentStore, earningsStore);

        var request = new RecordWorkerPaymentRequest(
            WorkerId: Guid.NewGuid(),
            PaymentDate: new DateOnly(2026, 9, 1),
            PaymentType: "ADVANCE",
            Amount: 500m,
            CurrencyId: _currencyId,
            PaymentMethod: "CASH");

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            service.RecordPaymentAsync(CreateActor(), request));
    }

    [Fact]
    public async Task RecordPaymentAsync_WhenAmountIsZeroOrNegative_ThrowsValidationException()
    {
        var (paymentStore, earningsStore, _) = SetupScenario();
        var service = new WorkerPaymentService(paymentStore, earningsStore);

        var request = new RecordWorkerPaymentRequest(
            WorkerId: _workerId,
            PaymentDate: new DateOnly(2026, 9, 1),
            PaymentType: "ADVANCE",
            Amount: 0m,
            CurrencyId: _currencyId,
            PaymentMethod: "CASH");

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.RecordPaymentAsync(CreateActor(), request));
    }

    [Fact]
    public async Task RecordPaymentAsync_WhenPeriodDatesInvalid_ThrowsValidationException()
    {
        var (paymentStore, earningsStore, _) = SetupScenario();
        var service = new WorkerPaymentService(paymentStore, earningsStore);

        var request = new RecordWorkerPaymentRequest(
            WorkerId: _workerId,
            PaymentDate: new DateOnly(2026, 9, 15),
            PaymentType: "PAYOUT",
            Amount: 1000m,
            CurrencyId: _currencyId,
            PaymentMethod: "CASH",
            PaymentPeriodFrom: new DateOnly(2026, 9, 15),
            PaymentPeriodTo: new DateOnly(2026, 9, 10));

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.RecordPaymentAsync(CreateActor(), request));
    }

    #endregion

    #region Cancel Payment Tests

    [Fact]
    public async Task CancelPaymentAsync_WhenCompleted_TransitionsToCancelledWithoutPhysicalDeletion()
    {
        var (paymentStore, earningsStore, _) = SetupScenario();
        var service = new WorkerPaymentService(paymentStore, earningsStore);

        var payment = WorkerPayment.CreateAdvance(
            _organizationId,
            _workerId,
            new DateOnly(2026, 9, 1),
            1000m,
            _currencyId,
            PaymentMethod.Cash,
            _userId);
        paymentStore.Payments.Add(payment);

        var request = new CancelWorkerPaymentRequest("Entered duplicate advance");
        var response = await service.CancelPaymentAsync(CreateActor(), payment.Id, request, "127.0.0.1");

        Assert.Equal("CANCELLED", response.Status);
        Assert.NotNull(response.CancelledAt);
        Assert.Equal(_userId, response.CancelledBy);
        Assert.Equal("Entered duplicate advance", response.CancellationReason);

        // Record must remain in history
        Assert.Single(paymentStore.Payments);
        Assert.Equal(PaymentStatus.Cancelled, paymentStore.Payments[0].Status);

        Assert.Single(paymentStore.AuditLogs);
        Assert.Equal("WorkerPayment.Cancelled", paymentStore.AuditLogs[0].Action);
    }

    [Fact]
    public async Task CancelPaymentAsync_WhenAlreadyCancelled_ThrowsValidationException()
    {
        var (paymentStore, earningsStore, _) = SetupScenario();
        var service = new WorkerPaymentService(paymentStore, earningsStore);

        var payment = WorkerPayment.CreateAdvance(
            _organizationId,
            _workerId,
            new DateOnly(2026, 9, 1),
            1000m,
            _currencyId,
            PaymentMethod.Cash,
            _userId);
        payment.Cancel(DateTimeOffset.UtcNow, _userId, "Already cancelled");
        paymentStore.Payments.Add(payment);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CancelPaymentAsync(CreateActor(), payment.Id, new CancelWorkerPaymentRequest()));
    }

    #endregion

    #region Financial Summary Tests

    [Fact]
    public async Task GetWorkerFinancialSummaryAsync_CalculatesCorrectOutstandingAndSettlement()
    {
        // Matches Spec Section 24:
        // Earnings: ₹2,275
        // Advance: ₹1,000 -> Outstanding = ₹1,275
        // Partial payout: ₹700 -> Outstanding = ₹575
        // Final payout: ₹575 -> Outstanding = ₹0
        var (paymentStore, earningsStore, _) = SetupScenario();
        var service = new WorkerPaymentService(paymentStore, earningsStore);

        // Earnings ledger approved entries totaling ₹2,275
        var e1 = WorkerEarningsLedger.CreateEarning(
            _organizationId, _workerId, new DateOnly(2026, 9, 1),
            WageType.FullDay, 1m, 500m, _currencyId, _userId, approveImmediately: true);
        var e2 = WorkerEarningsLedger.CreateEarning(
            _organizationId, _workerId, new DateOnly(2026, 9, 2),
            WageType.FullDay, 1m, 500m, _currencyId, _userId, approveImmediately: true);
        var e3 = WorkerEarningsLedger.CreateEarning(
            _organizationId, _workerId, new DateOnly(2026, 9, 3),
            WageType.HalfDay, 1m, 275m, _currencyId, _userId, approveImmediately: true);
        var e4 = WorkerEarningsLedger.CreateEarning(
            _organizationId, _workerId, new DateOnly(2026, 9, 4),
            WageType.FullDay, 1m, 500m, _currencyId, _userId, approveImmediately: true);
        var e5 = WorkerEarningsLedger.CreateEarning(
            _organizationId, _workerId, new DateOnly(2026, 9, 5),
            WageType.FullDay, 1m, 500m, _currencyId, _userId, approveImmediately: true);

        earningsStore.Entries.AddRange([e1, e2, e3, e4, e5]);

        // Advance: ₹1,000
        var advance = WorkerPayment.CreateAdvance(
            _organizationId, _workerId, new DateOnly(2026, 9, 1),
            1000m, _currencyId, PaymentMethod.Cash, _userId);

        // Partial payout: ₹700
        var partialPayout = WorkerPayment.CreatePayout(
            _organizationId, _workerId, new DateOnly(2026, 9, 6),
            700m, _currencyId, PaymentMethod.Cash, _userId);

        paymentStore.Payments.AddRange([advance, partialPayout]);

        var summary = await service.GetWorkerFinancialSummaryAsync(CreateActor(), _workerId);

        Assert.Equal(2275m, summary.GrossEarnings);
        Assert.Equal(1000m, summary.TotalAdvances);
        Assert.Equal(700m, summary.TotalPayouts);
        // Net outstanding = 2275 - 1000 - 700 = 575
        Assert.Equal(575m, summary.NetOutstanding);
        Assert.Equal(0m, summary.CarryForwardAdvanceBalance);

        // Now record final payout of ₹575
        var finalPayout = WorkerPayment.CreatePayout(
            _organizationId, _workerId, new DateOnly(2026, 9, 10),
            575m, _currencyId, PaymentMethod.BankTransfer, _userId);
        paymentStore.Payments.Add(finalPayout);

        var settledSummary = await service.GetWorkerFinancialSummaryAsync(CreateActor(), _workerId);
        Assert.Equal(2275m, settledSummary.GrossEarnings);
        Assert.Equal(1000m, settledSummary.TotalAdvances);
        Assert.Equal(1275m, settledSummary.TotalPayouts);
        Assert.Equal(0m, settledSummary.NetOutstanding);
        Assert.Equal(0m, settledSummary.CarryForwardAdvanceBalance);
    }

    [Fact]
    public async Task GetWorkerFinancialSummaryAsync_WhenAdvanceExceedsEarnings_CalculatesNegativeBalanceAndCarryForward()
    {
        // Matches Spec Section 22:
        // Advance received = ₹5,000
        // Current approved earnings = ₹3,000
        // Current balance = -₹2,000
        // Carry forward advance balance = ₹2,000
        var (paymentStore, earningsStore, _) = SetupScenario();
        var service = new WorkerPaymentService(paymentStore, earningsStore);

        var earning = WorkerEarningsLedger.CreateEarning(
            _organizationId, _workerId, new DateOnly(2026, 9, 1),
            WageType.FullDay, 6m, 500m, _currencyId, _userId,
            grossAmount: 3000m, approveImmediately: true);
        earningsStore.Entries.Add(earning);

        var advance = WorkerPayment.CreateAdvance(
            _organizationId, _workerId, new DateOnly(2026, 9, 1),
            5000m, _currencyId, PaymentMethod.Cash, _userId);
        paymentStore.Payments.Add(advance);

        var summary = await service.GetWorkerFinancialSummaryAsync(CreateActor(), _workerId);

        Assert.Equal(3000m, summary.GrossEarnings);
        Assert.Equal(5000m, summary.TotalAdvances);
        Assert.Equal(-2000m, summary.NetOutstanding);
        Assert.Equal(2000m, summary.CarryForwardAdvanceBalance);
    }

    [Fact]
    public async Task GetWorkerFinancialSummaryAsync_WhenNoEarningsExistYet_HandlesAdvanceCleanly()
    {
        var (paymentStore, earningsStore, _) = SetupScenario();
        var service = new WorkerPaymentService(paymentStore, earningsStore);

        // Advance given with zero earnings so far
        var advance = WorkerPayment.CreateAdvance(
            _organizationId, _workerId, new DateOnly(2026, 9, 1),
            1500m, _currencyId, PaymentMethod.Cash, _userId);
        paymentStore.Payments.Add(advance);

        var summary = await service.GetWorkerFinancialSummaryAsync(CreateActor(), _workerId);

        Assert.Equal(0m, summary.GrossEarnings);
        Assert.Equal(1500m, summary.TotalAdvances);
        Assert.Equal(-1500m, summary.NetOutstanding);
        Assert.Equal(1500m, summary.CarryForwardAdvanceBalance);
    }

    #endregion

    private static void SetEntityId<T>(T entity, Guid id)
    {
        var property = typeof(T).GetProperty("Id");
        property?.SetValue(entity, id);
    }

    #region Fakes

    private sealed class FakeWorkerPaymentStore : IWorkerPaymentStore
    {
        public List<WorkerPayment> Payments { get; } = [];
        public List<Worker> Workers { get; } = [];
        public List<Currency> Currencies { get; } = [];
        public List<AuditLog> AuditLogs { get; } = [];

        public Task<WorkerPayment?> FindAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Payments.FirstOrDefault(p => p.Id == id && p.OrganizationId == organizationId));

        public Task<PagedResponse<WorkerPayment>> ListByWorkerAsync(
            Guid organizationId,
            Guid workerId,
            DateOnly? fromDate,
            DateOnly? toDate,
            PaymentType? paymentType,
            PaymentStatus? status,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query = Payments.Where(p => p.OrganizationId == organizationId && p.WorkerId == workerId);
            if (fromDate.HasValue) query = query.Where(p => p.PaymentDate >= fromDate.Value);
            if (toDate.HasValue) query = query.Where(p => p.PaymentDate <= toDate.Value);
            if (paymentType.HasValue) query = query.Where(p => p.PaymentType == paymentType.Value);
            if (status.HasValue) query = query.Where(p => p.Status == status.Value);

            var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return Task.FromResult(new PagedResponse<WorkerPayment>(items, query.Count(), page, pageSize));
        }

        public Task<IReadOnlyList<WorkerPayment>> ListPaymentsForFinancialSummaryAsync(
            Guid organizationId,
            Guid workerId,
            DateOnly? asOfDate,
            CancellationToken cancellationToken = default)
        {
            var query = Payments.Where(p => p.OrganizationId == organizationId && p.WorkerId == workerId);
            if (asOfDate.HasValue) query = query.Where(p => p.PaymentDate <= asOfDate.Value);
            return Task.FromResult<IReadOnlyList<WorkerPayment>>(query.ToList());
        }

        public Task<Worker?> FindWorkerAsync(Guid workerId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Workers.FirstOrDefault(w => w.Id == workerId && w.OrganizationId == organizationId));

        public Task<Currency?> FindCurrencyAsync(Guid currencyId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Currencies.FirstOrDefault(c => c.Id == currencyId));

        public void Add(WorkerPayment payment) => Payments.Add(payment);

        public void AddAuditLog(AuditLog auditLog) => AuditLogs.Add(auditLog);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeEarningsStore : IWorkerEarningsLedgerStore
    {
        public List<WorkerEarningsLedger> Entries { get; } = [];

        public Task<IReadOnlyList<WorkerEarningsLedger>> ListForBalanceCalculationAsync(
            Guid organizationId,
            Guid workerId,
            DateOnly? fromDate,
            DateOnly? toDate,
            CancellationToken cancellationToken = default)
        {
            var query = Entries.Where(e => e.OrganizationId == organizationId && e.WorkerId == workerId);
            if (fromDate.HasValue) query = query.Where(e => e.EarningsDate >= fromDate.Value);
            if (toDate.HasValue) query = query.Where(e => e.EarningsDate <= toDate.Value);
            return Task.FromResult<IReadOnlyList<WorkerEarningsLedger>>(query.ToList());
        }

        public Task<WorkerEarningsLedger?> FindAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Entries.FirstOrDefault(e => e.Id == id && e.OrganizationId == organizationId));

        public Task<WorkerEarningsLedger?> FindByAttendanceIdAsync(Guid organizationId, Guid attendanceId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Entries.FirstOrDefault(e => e.AttendanceId == attendanceId && e.OrganizationId == organizationId && e.EntryType == EarningsEntryType.Earning));

        public Task<PagedResponse<WorkerEarningsLedger>> ListByWorkerAsync(
            Guid organizationId, Guid workerId, DateOnly? fromDate, DateOnly? toDate,
            EarningsLedgerStatus? status, EarningsEntryType? entryType, int page, int pageSize,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> HasActiveReversalAsync(Guid originalLedgerId, Guid organizationId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Worker?> FindWorkerAsync(Guid workerId, Guid organizationId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<LaborWageRate?> FindApplicableWageRateAsync(Guid organizationId, Gender gender, WageType wageType, DateOnly businessDate, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Currency?> FindCurrencyAsync(Guid currencyId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public void Add(WorkerEarningsLedger entry) => Entries.Add(entry);

        public void AddAuditLog(AuditLog auditLog) { }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    #endregion
}
