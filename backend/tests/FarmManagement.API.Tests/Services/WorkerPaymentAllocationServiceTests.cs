using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Labor;
using FarmManagement.Application.Interfaces.Labor;
using FarmManagement.Application.Services;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Services;

public sealed class WorkerPaymentAllocationServiceTests
{
    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _workerId = Guid.NewGuid();
    private readonly Guid _currencyId = Guid.NewGuid();

    private PaymentActor CreateActor() => new(_userId, _organizationId);

    private (FakeWorkerPaymentAllocationStore store, WorkerPayment payment, WorkerEarningsLedger earning1, WorkerEarningsLedger earning2) SetupScenario(
        decimal paymentAmount = 1000m,
        decimal earning1Amount = 400m,
        decimal earning2Amount = 500m)
    {
        var store = new FakeWorkerPaymentAllocationStore();

        var payment = WorkerPayment.CreatePayout(
            organizationId: _organizationId,
            workerId: _workerId,
            paymentDate: new DateOnly(2026, 9, 15),
            amount: paymentAmount,
            currencyId: _currencyId,
            paymentMethod: PaymentMethod.BankTransfer,
            createdBy: _userId);
        store.Payments.Add(payment);

        var earning1 = WorkerEarningsLedger.CreateEarning(
            organizationId: _organizationId,
            workerId: _workerId,
            earningsDate: new DateOnly(2026, 9, 1),
            wageType: WageType.FullDay,
            quantity: 1m,
            wageRate: earning1Amount,
            currencyId: _currencyId,
            createdBy: _userId,
            approveImmediately: true);
        store.Earnings.Add(earning1);

        var earning2 = WorkerEarningsLedger.CreateEarning(
            organizationId: _organizationId,
            workerId: _workerId,
            earningsDate: new DateOnly(2026, 9, 2),
            wageType: WageType.FullDay,
            quantity: 1m,
            wageRate: earning2Amount,
            currencyId: _currencyId,
            createdBy: _userId,
            approveImmediately: true);
        store.Earnings.Add(earning2);

        return (store, payment, earning1, earning2);
    }

    #region Manual Allocation Tests

    [Fact]
    public async Task CreateAllocationAsync_WithValidManualSettlement_SucceedsAndCreatesAudit()
    {
        var (store, payment, earning1, _) = SetupScenario();
        var service = new WorkerPaymentAllocationService(store);

        var request = new CreateWorkerPaymentAllocationRequest(
            WorkerPaymentId: payment.Id,
            WorkerEarningsLedgerId: earning1.Id,
            AllocatedAmount: 300m,
            AllocationType: "EARNING_SETTLEMENT",
            AllocationDate: new DateOnly(2026, 9, 15),
            Notes: "Partial payment");

        var response = await service.CreateAllocationAsync(CreateActor(), request, "127.0.0.1");

        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal(payment.Id, response.WorkerPaymentId);
        Assert.Equal(earning1.Id, response.WorkerEarningsLedgerId);
        Assert.Equal(300m, response.AllocatedAmount);
        Assert.Equal("EARNING_SETTLEMENT", response.AllocationType);
        Assert.Single(store.Allocations);
        Assert.Single(store.AuditLogs);
        Assert.Equal("WorkerPaymentAllocation.Created", store.AuditLogs[0].Action);
    }

    [Fact]
    public async Task CreateAllocationAsync_WhenPaymentCancelled_ThrowsValidationException()
    {
        var (store, payment, earning1, _) = SetupScenario();
        payment.Cancel(DateTimeOffset.UtcNow, _userId, "Mistake");
        var service = new WorkerPaymentAllocationService(store);

        var request = new CreateWorkerPaymentAllocationRequest(
            WorkerPaymentId: payment.Id,
            WorkerEarningsLedgerId: earning1.Id,
            AllocatedAmount: 300m,
            AllocationType: "EARNING_SETTLEMENT",
            AllocationDate: new DateOnly(2026, 9, 15));

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAllocationAsync(CreateActor(), request));

        Assert.Contains("cancelled", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAllocationAsync_WhenAmountExceedsRemainingPayment_ThrowsValidationException()
    {
        var (store, payment, earning1, _) = SetupScenario(paymentAmount: 500m);
        var service = new WorkerPaymentAllocationService(store);

        // Pre-allocate 400m
        store.Allocations.Add(WorkerPaymentAllocation.Create(
            _organizationId, payment.Id, earning1.Id, 400m, PaymentAllocationType.EarningSettlement, new DateOnly(2026, 9, 15), _userId));

        // Try allocating 200m when only 100m remains
        var request = new CreateWorkerPaymentAllocationRequest(
            WorkerPaymentId: payment.Id,
            WorkerEarningsLedgerId: earning1.Id,
            AllocatedAmount: 200m,
            AllocationType: "EARNING_SETTLEMENT",
            AllocationDate: new DateOnly(2026, 9, 15));

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAllocationAsync(CreateActor(), request));

        Assert.Contains("exceeds the remaining unallocated payment amount", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAllocationAsync_WhenAmountExceedsRemainingEarning_ThrowsValidationException()
    {
        var (store, payment, earning1, _) = SetupScenario(earning1Amount: 300m);
        var service = new WorkerPaymentAllocationService(store);

        var request = new CreateWorkerPaymentAllocationRequest(
            WorkerPaymentId: payment.Id,
            WorkerEarningsLedgerId: earning1.Id,
            AllocatedAmount: 350m, // Earning gross is only 300m
            AllocationType: "EARNING_SETTLEMENT",
            AllocationDate: new DateOnly(2026, 9, 15));

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAllocationAsync(CreateActor(), request));

        Assert.Contains("exceeds the remaining unpaid earning amount", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAllocationAsync_WhenWorkerMismatch_ThrowsValidationException()
    {
        var (store, payment, _, _) = SetupScenario();
        var differentWorkerEarning = WorkerEarningsLedger.CreateEarning(
            organizationId: _organizationId,
            workerId: Guid.NewGuid(),
            earningsDate: new DateOnly(2026, 9, 1),
            wageType: WageType.FullDay,
            quantity: 1m,
            wageRate: 400m,
            currencyId: _currencyId,
            createdBy: _userId,
            approveImmediately: true);
        store.Earnings.Add(differentWorkerEarning);

        var service = new WorkerPaymentAllocationService(store);

        var request = new CreateWorkerPaymentAllocationRequest(
            WorkerPaymentId: payment.Id,
            WorkerEarningsLedgerId: differentWorkerEarning.Id,
            AllocatedAmount: 200m,
            AllocationType: "EARNING_SETTLEMENT",
            AllocationDate: new DateOnly(2026, 9, 15));

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAllocationAsync(CreateActor(), request));

        Assert.Contains("different workers", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAllocationAsync_WhenEarningReversed_ThrowsValidationException()
    {
        var (store, payment, earning1, _) = SetupScenario();
        earning1.MarkReversed(DateTimeOffset.UtcNow, _userId);
        var service = new WorkerPaymentAllocationService(store);

        var request = new CreateWorkerPaymentAllocationRequest(
            WorkerPaymentId: payment.Id,
            WorkerEarningsLedgerId: earning1.Id,
            AllocatedAmount: 200m,
            AllocationType: "EARNING_SETTLEMENT",
            AllocationDate: new DateOnly(2026, 9, 15));

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAllocationAsync(CreateActor(), request));

        Assert.Contains("reversed", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Auto-Allocation FIFO Tests

    [Fact]
    public async Task AutoAllocatePaymentAsync_WithUnallocatedEarnings_AllocatesFIFO()
    {
        // payment = 650m, earning1 = 400m (Sept 1), earning2 = 500m (Sept 2)
        var (store, payment, earning1, earning2) = SetupScenario(paymentAmount: 650m, earning1Amount: 400m, earning2Amount: 500m);
        var service = new WorkerPaymentAllocationService(store);

        var request = new AutoAllocatePaymentRequest(WorkerPaymentId: payment.Id);

        var allocations = await service.AutoAllocatePaymentAsync(CreateActor(), request, "127.0.0.1");

        // Should fully cover earning1 (400m) and partially cover earning2 (250m)
        Assert.Equal(2, allocations.Count);
        Assert.Equal(earning1.Id, allocations[0].WorkerEarningsLedgerId);
        Assert.Equal(400m, allocations[0].AllocatedAmount);
        Assert.Equal(earning2.Id, allocations[1].WorkerEarningsLedgerId);
        Assert.Equal(250m, allocations[1].AllocatedAmount);

        // Check total allocated is 650m
        Assert.Equal(650m, allocations.Sum(a => a.AllocatedAmount));
        Assert.Equal(2, store.Allocations.Count);
    }

    [Fact]
    public async Task AutoAllocatePaymentAsync_WhenPaymentAlreadyFullyAllocated_ThrowsValidationException()
    {
        var (store, payment, earning1, _) = SetupScenario(paymentAmount: 300m);
        store.Allocations.Add(WorkerPaymentAllocation.Create(
            _organizationId, payment.Id, earning1.Id, 300m, PaymentAllocationType.EarningSettlement, new DateOnly(2026, 9, 15), _userId));

        var service = new WorkerPaymentAllocationService(store);
        var request = new AutoAllocatePaymentRequest(WorkerPaymentId: payment.Id);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.AutoAllocatePaymentAsync(CreateActor(), request));

        Assert.Contains("already fully allocated", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Deallocation Tests

    [Fact]
    public async Task DeleteAllocationAsync_RemovesAllocationAndAudits()
    {
        var (store, payment, earning1, _) = SetupScenario();
        var alloc = WorkerPaymentAllocation.Create(
            _organizationId, payment.Id, earning1.Id, 300m, PaymentAllocationType.EarningSettlement, new DateOnly(2026, 9, 15), _userId);
        store.Allocations.Add(alloc);

        var service = new WorkerPaymentAllocationService(store);
        await service.DeleteAllocationAsync(CreateActor(), alloc.Id, "127.0.0.1");

        Assert.Empty(store.Allocations);
        Assert.Single(store.AuditLogs);
        Assert.Equal("WorkerPaymentAllocation.Deleted", store.AuditLogs[0].Action);
    }

    #endregion

    #region Unsettled Balances & Queries

    [Fact]
    public async Task GetUnsettledEarningsForWorkerAsync_ReturnsRemainingUnpaidAmounts()
    {
        var (store, payment, earning1, earning2) = SetupScenario(earning1Amount: 400m, earning2Amount: 500m);
        // Partially settle earning1 by 150m
        store.Allocations.Add(WorkerPaymentAllocation.Create(
            _organizationId, payment.Id, earning1.Id, 150m, PaymentAllocationType.EarningSettlement, new DateOnly(2026, 9, 15), _userId));

        var service = new WorkerPaymentAllocationService(store);
        var unsettled = await service.GetUnsettledEarningsForWorkerAsync(CreateActor(), _workerId);

        Assert.Equal(2, unsettled.Count);
        var u1 = unsettled.First(u => u.WorkerEarningsLedgerId == earning1.Id);
        Assert.Equal(400m, u1.GrossAmount);
        Assert.Equal(150m, u1.AllocatedAmount);
        Assert.Equal(250m, u1.RemainingUnpaidAmount);

        var u2 = unsettled.First(u => u.WorkerEarningsLedgerId == earning2.Id);
        Assert.Equal(500m, u2.GrossAmount);
        Assert.Equal(0m, u2.AllocatedAmount);
        Assert.Equal(500m, u2.RemainingUnpaidAmount);
    }

    [Fact]
    public async Task GetPaymentUnallocatedBalanceAsync_ReturnsCorrectRemainingAmount()
    {
        var (store, payment, earning1, _) = SetupScenario(paymentAmount: 1000m);
        store.Allocations.Add(WorkerPaymentAllocation.Create(
            _organizationId, payment.Id, earning1.Id, 350m, PaymentAllocationType.EarningSettlement, new DateOnly(2026, 9, 15), _userId));

        var service = new WorkerPaymentAllocationService(store);
        var balance = await service.GetPaymentUnallocatedBalanceAsync(CreateActor(), payment.Id);

        Assert.Equal(payment.Id, balance.WorkerPaymentId);
        Assert.Equal(1000m, balance.Amount);
        Assert.Equal(350m, balance.AllocatedAmount);
        Assert.Equal(650m, balance.RemainingUnallocatedAmount);
    }

    #endregion

    #region Rollback on Payment Cancellation

    [Fact]
    public async Task CancelPaymentAsync_InWorkerPaymentService_RollsBackAllocations()
    {
        var (store, payment, earning1, _) = SetupScenario(paymentAmount: 1000m);
        var alloc = WorkerPaymentAllocation.Create(
            _organizationId, payment.Id, earning1.Id, 400m, PaymentAllocationType.EarningSettlement, new DateOnly(2026, 9, 15), _userId);
        store.Allocations.Add(alloc);

        var paymentStore = new FakeWorkerPaymentStoreBridge(store);
        var earningsStore = new FakeEarningsStoreBridge(store);
        var paymentService = new WorkerPaymentService(paymentStore, earningsStore, store);

        await paymentService.CancelPaymentAsync(CreateActor(), payment.Id, new CancelWorkerPaymentRequest("Mistake in payment"));

        // Payment status is cancelled
        Assert.Equal(PaymentStatus.Cancelled, payment.Status);
        // Allocations must have been rolled back
        Assert.Empty(store.Allocations);
    }

    #endregion

    #region Fakes

    private sealed class FakeWorkerPaymentAllocationStore : IWorkerPaymentAllocationStore
    {
        public List<WorkerPaymentAllocation> Allocations { get; } = [];
        public List<WorkerPayment> Payments { get; } = [];
        public List<WorkerEarningsLedger> Earnings { get; } = [];
        public List<AuditLog> AuditLogs { get; } = [];

        public Task<WorkerPaymentAllocation?> FindAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Allocations.FirstOrDefault(a => a.Id == id && a.OrganizationId == organizationId));

        public Task<WorkerPayment?> FindPaymentAsync(Guid paymentId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Payments.FirstOrDefault(p => p.Id == paymentId && p.OrganizationId == organizationId));

        public Task<WorkerEarningsLedger?> FindEarningAsync(Guid earningId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Earnings.FirstOrDefault(e => e.Id == earningId && e.OrganizationId == organizationId));

        public Task<IReadOnlyList<WorkerPaymentAllocation>> ListByPaymentAsync(Guid paymentId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WorkerPaymentAllocation>>(
                Allocations.Where(a => a.WorkerPaymentId == paymentId && a.OrganizationId == organizationId).ToList());

        public Task<IReadOnlyList<WorkerPaymentAllocation>> ListByEarningAsync(Guid earningId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WorkerPaymentAllocation>>(
                Allocations.Where(a => a.WorkerEarningsLedgerId == earningId && a.OrganizationId == organizationId).ToList());

        public Task<IReadOnlyList<WorkerPaymentAllocation>> ListByWorkerAsync(Guid workerId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WorkerPaymentAllocation>>(
                Allocations.Where(a => a.OrganizationId == organizationId &&
                                       Payments.Any(p => p.Id == a.WorkerPaymentId && p.WorkerId == workerId)).ToList());

        public Task<decimal> GetTotalAllocatedForPaymentAsync(Guid paymentId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Allocations.Where(a => a.WorkerPaymentId == paymentId && a.OrganizationId == organizationId)
                .Sum(a => a.AllocatedAmount));

        public Task<decimal> GetTotalSettledForEarningAsync(Guid earningId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Allocations.Where(a => a.WorkerEarningsLedgerId == earningId &&
                                                  a.OrganizationId == organizationId &&
                                                  Payments.Any(p => p.Id == a.WorkerPaymentId && p.Status != PaymentStatus.Cancelled))
                .Sum(a => a.AllocatedAmount));

        public Task<IReadOnlyDictionary<Guid, decimal>> GetSettledTotalsForEarningsBatchAsync(
            IEnumerable<Guid> earningIds,
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            var ids = earningIds.ToHashSet();
            var dict = Allocations.Where(a => a.OrganizationId == organizationId &&
                                              a.WorkerEarningsLedgerId.HasValue &&
                                              ids.Contains(a.WorkerEarningsLedgerId.Value) &&
                                              Payments.Any(p => p.Id == a.WorkerPaymentId && p.Status != PaymentStatus.Cancelled))
                .GroupBy(a => a.WorkerEarningsLedgerId!.Value)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.AllocatedAmount));

            return Task.FromResult<IReadOnlyDictionary<Guid, decimal>>(dict);
        }

        public Task<IReadOnlyList<WorkerEarningsLedger>> ListUnsettledEarningsForWorkerAsync(
            Guid organizationId,
            Guid workerId,
            DateOnly? upToDate = null,
            CancellationToken cancellationToken = default)
        {
            var query = Earnings.Where(e => e.OrganizationId == organizationId &&
                                            e.WorkerId == workerId &&
                                            e.Status == EarningsLedgerStatus.Approved &&
                                            e.EntryType != EarningsEntryType.Reversal);
            if (upToDate.HasValue)
            {
                query = query.Where(e => e.EarningsDate <= upToDate.Value);
            }

            return Task.FromResult<IReadOnlyList<WorkerEarningsLedger>>(query.OrderBy(e => e.EarningsDate).ThenBy(e => e.CreatedAt).ToList());
        }

        public void Add(WorkerPaymentAllocation allocation) => Allocations.Add(allocation);

        public void AddRange(IEnumerable<WorkerPaymentAllocation> allocations) => Allocations.AddRange(allocations);

        public void Remove(WorkerPaymentAllocation allocation) => Allocations.Remove(allocation);

        public void RemoveRange(IEnumerable<WorkerPaymentAllocation> allocations)
        {
            foreach (var a in allocations.ToList())
            {
                Allocations.Remove(a);
            }
        }

        public void AddAuditLog(AuditLog auditLog) => AuditLogs.Add(auditLog);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeWorkerPaymentStoreBridge(FakeWorkerPaymentAllocationStore allocStore) : IWorkerPaymentStore
    {
        public Task<WorkerPayment?> FindAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default) =>
            allocStore.FindPaymentAsync(id, organizationId, cancellationToken);

        public Task<PagedResponse<WorkerPayment>> ListByWorkerAsync(
            Guid organizationId, Guid workerId, DateOnly? fromDate, DateOnly? toDate,
            PaymentType? paymentType, PaymentStatus? status, int page, int pageSize,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<IReadOnlyList<WorkerPayment>> ListPaymentsForFinancialSummaryAsync(
            Guid organizationId, Guid workerId, DateOnly? asOfDate, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WorkerPayment>>(allocStore.Payments.Where(p => p.OrganizationId == organizationId && p.WorkerId == workerId).ToList());

        public Task<Worker?> FindWorkerAsync(Guid workerId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Worker?>(null);

        public Task<Currency?> FindCurrencyAsync(Guid currencyId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Currency?>(null);

        public void Add(WorkerPayment payment) => allocStore.Payments.Add(payment);

        public void AddAuditLog(AuditLog auditLog) => allocStore.AuditLogs.Add(auditLog);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeEarningsStoreBridge(FakeWorkerPaymentAllocationStore allocStore) : IWorkerEarningsLedgerStore
    {
        public Task<IReadOnlyList<WorkerEarningsLedger>> ListForBalanceCalculationAsync(
            Guid organizationId, Guid workerId, DateOnly? fromDate, DateOnly? toDate,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WorkerEarningsLedger>>(allocStore.Earnings.Where(e => e.OrganizationId == organizationId && e.WorkerId == workerId).ToList());

        public Task<WorkerEarningsLedger?> FindAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default) =>
            allocStore.FindEarningAsync(id, organizationId, cancellationToken);

        public Task<WorkerEarningsLedger?> FindByAttendanceIdAsync(Guid organizationId, Guid attendanceId, CancellationToken cancellationToken = default) =>
            Task.FromResult(allocStore.Earnings.FirstOrDefault(e => e.AttendanceId == attendanceId && e.OrganizationId == organizationId && e.EntryType == EarningsEntryType.Earning));

        public Task<PagedResponse<WorkerEarningsLedger>> ListByWorkerAsync(Guid organizationId, Guid workerId, DateOnly? fromDate, DateOnly? toDate, EarningsLedgerStatus? status, EarningsEntryType? entryType, int page, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasActiveReversalAsync(Guid originalLedgerId, Guid organizationId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Worker?> FindWorkerAsync(Guid workerId, Guid organizationId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<LaborWageRate?> FindApplicableWageRateAsync(Guid organizationId, Gender gender, WageType wageType, DateOnly businessDate, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Currency?> FindCurrencyAsync(Guid currencyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public void Add(WorkerEarningsLedger entry) => allocStore.Earnings.Add(entry);
        public void AddAuditLog(AuditLog auditLog) { }
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    #endregion
}
