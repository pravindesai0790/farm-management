using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.Labor;
using FarmManagement.Application.Interfaces.Labor;
using FarmManagement.Application.Services;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Services;

public sealed class WorkerSettlementServiceTests
{
    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _workerId = Guid.NewGuid();
    private readonly Guid _currencyId = Guid.NewGuid();

    private SettlementActor CreateActor() => new(_userId, _organizationId);

    private (FakeWorkerSettlementStore store, Worker worker) SetupScenario()
    {
        var store = new FakeWorkerSettlementStore();
        var worker = new Worker(
            _organizationId,
            "Ramesh",
            "Kumar",
            Gender.Male,
            EmploymentType.Permanent,
            _userId);

        SetEntityId(worker, _workerId);
        store.Workers.Add(worker);

        return (store, worker);
    }

    #region Spec Section 24 End-to-End Settlement Lifecycle

    [Fact]
    public async Task CalculateSettlementAsync_SpecSection24_FullLifecycleMatchesExpectedBalances()
    {
        var (store, _) = SetupScenario();
        var service = new WorkerSettlementService(store);

        // 1. Five days of attendance earnings:
        // Sep 1: 500, Sep 2: 500, Sep 3: 275, Sep 4: 500, Sep 5: 500 = 2275
        var e1 = CreateApprovedEarning(store, new DateOnly(2026, 9, 1), 500m);
        var e2 = CreateApprovedEarning(store, new DateOnly(2026, 9, 2), 500m);
        var e3 = CreateApprovedEarning(store, new DateOnly(2026, 9, 3), 275m);
        var e4 = CreateApprovedEarning(store, new DateOnly(2026, 9, 4), 500m);
        var e5 = CreateApprovedEarning(store, new DateOnly(2026, 9, 5), 500m);

        // Step 1: Check initial settlement before any payments
        var initial = await service.CalculateSettlementAsync(CreateActor(), new WorkerSettlementQuery(_workerId));
        Assert.Equal(2275m, initial.TotalEarnings);
        Assert.Equal(2275m, initial.GrossPayable);
        Assert.Equal(0m, initial.TotalAdvances);
        Assert.Equal(0m, initial.TotalAdvanceApplied);
        Assert.Equal(2275m, initial.OutstandingAmount);
        Assert.Equal(2275m, initial.AmountAvailableForPayout);
        Assert.Equal(0m, initial.AdvanceCarriedForward);
        Assert.Equal("READY_FOR_PAYOUT", initial.SettlementStatus);
        Assert.Equal(5, initial.UnsettledEarningCount);
        Assert.Equal(0, initial.SettledEarningCount);

        // Step 2: Worker receives advance of 1000, and it is applied against e1 (500) and e2 (500)
        var adv = CreateCompletedPayment(store, PaymentType.Advance, 1000m, new DateOnly(2026, 9, 6));
        store.Allocations.Add(WorkerPaymentAllocation.CreateAdvanceOffset(_organizationId, adv.Id, e1.Id, 500m, new DateOnly(2026, 9, 6), _userId));
        store.Allocations.Add(WorkerPaymentAllocation.CreateAdvanceOffset(_organizationId, adv.Id, e2.Id, 500m, new DateOnly(2026, 9, 6), _userId));

        var afterAdv = await service.CalculateSettlementAsync(CreateActor(), new WorkerSettlementQuery(_workerId));
        Assert.Equal(2275m, afterAdv.GrossPayable);
        Assert.Equal(1000m, afterAdv.TotalAdvances);
        Assert.Equal(1000m, afterAdv.TotalAdvanceApplied);
        Assert.Equal(0m, afterAdv.CurrentAdvanceBalance);
        Assert.Equal(1275m, afterAdv.OutstandingAmount); // 2275 - 1000 = 1275
        Assert.Equal(1275m, afterAdv.AmountAvailableForPayout);
        Assert.Equal("PARTIALLY_PAID", afterAdv.SettlementStatus);
        Assert.Equal(3, afterAdv.UnsettledEarningCount);
        Assert.Equal(2, afterAdv.SettledEarningCount);

        // Step 3: Partial payout of 700 applied against e3 (275) and e4 (425)
        var p1 = CreateCompletedPayment(store, PaymentType.Payout, 700m, new DateOnly(2026, 9, 10));
        store.Allocations.Add(WorkerPaymentAllocation.CreateEarningSettlement(_organizationId, p1.Id, e3.Id, 275m, new DateOnly(2026, 9, 10), _userId));
        store.Allocations.Add(WorkerPaymentAllocation.CreateEarningSettlement(_organizationId, p1.Id, e4.Id, 425m, new DateOnly(2026, 9, 10), _userId));

        var afterP1 = await service.CalculateSettlementAsync(CreateActor(), new WorkerSettlementQuery(_workerId));
        Assert.Equal(575m, afterP1.OutstandingAmount); // 1275 - 700 = 575
        Assert.Equal(575m, afterP1.AmountAvailableForPayout);
        Assert.Equal("PARTIALLY_PAID", afterP1.SettlementStatus);

        // Step 4: Final payout of 575 applied against remaining e4 (75) and e5 (500)
        var p2 = CreateCompletedPayment(store, PaymentType.Payout, 575m, new DateOnly(2026, 9, 15));
        store.Allocations.Add(WorkerPaymentAllocation.CreateEarningSettlement(_organizationId, p2.Id, e4.Id, 75m, new DateOnly(2026, 9, 15), _userId));
        store.Allocations.Add(WorkerPaymentAllocation.CreateEarningSettlement(_organizationId, p2.Id, e5.Id, 500m, new DateOnly(2026, 9, 15), _userId));

        var final = await service.CalculateSettlementAsync(CreateActor(), new WorkerSettlementQuery(_workerId));
        Assert.Equal(2275m, final.GrossPayable);
        Assert.Equal(1000m, final.TotalAdvances);
        Assert.Equal(1000m, final.TotalAdvanceApplied);
        Assert.Equal(1275m, final.TotalPayouts);
        Assert.Equal(1275m, final.TotalPayoutsApplied);
        Assert.Equal(2275m, final.TotalPaid);
        Assert.Equal(0m, final.OutstandingAmount);
        Assert.Equal(0m, final.AmountAvailableForPayout);
        Assert.Equal(0m, final.AdvanceCarriedForward);
        Assert.Equal("PAID", final.SettlementStatus);
        Assert.Equal(0, final.UnsettledEarningCount);
        Assert.Equal(5, final.SettledEarningCount);
    }

    #endregion

    #region Double Counting Prevention

    [Fact]
    public async Task CalculateSettlementAsync_DoesNotDoubleCountAdvancePaymentsAndAllocations()
    {
        var (store, _) = SetupScenario();
        var service = new WorkerSettlementService(store);

        var e1 = CreateApprovedEarning(store, new DateOnly(2026, 9, 1), 5000m);
        var adv = CreateCompletedPayment(store, PaymentType.Advance, 2000m, new DateOnly(2026, 9, 2));
        store.Allocations.Add(WorkerPaymentAllocation.CreateAdvanceOffset(_organizationId, adv.Id, e1.Id, 2000m, new DateOnly(2026, 9, 2), _userId));

        var response = await service.CalculateSettlementAsync(CreateActor(), new WorkerSettlementQuery(_workerId));

        // Must be: Gross 5000 - AdvanceApplied 2000 = 3000 (NOT 1000 by deducting total advances twice!)
        Assert.Equal(5000m, response.GrossPayable);
        Assert.Equal(2000m, response.TotalAdvances);
        Assert.Equal(2000m, response.TotalAdvanceApplied);
        Assert.Equal(0m, response.CurrentAdvanceBalance);
        Assert.Equal(3000m, response.OutstandingAmount);
        Assert.Equal(3000m, response.AmountAvailableForPayout);
    }

    #endregion

    #region Spec Section 22 Carry-Forward Advance Balance

    [Fact]
    public async Task CalculateSettlementAsync_SpecSection22_AdvanceExceedingEarningsCarriesForward()
    {
        var (store, _) = SetupScenario();
        var service = new WorkerSettlementService(store);

        // Advance 5000 received, Earnings 3000
        var e1 = CreateApprovedEarning(store, new DateOnly(2026, 9, 1), 3000m);
        var adv = CreateCompletedPayment(store, PaymentType.Advance, 5000m, new DateOnly(2026, 9, 1));
        store.Allocations.Add(WorkerPaymentAllocation.CreateAdvanceOffset(_organizationId, adv.Id, e1.Id, 3000m, new DateOnly(2026, 9, 1), _userId));

        var response = await service.CalculateSettlementAsync(CreateActor(), new WorkerSettlementQuery(_workerId));

        Assert.Equal(3000m, response.GrossPayable);
        Assert.Equal(5000m, response.TotalAdvances);
        Assert.Equal(3000m, response.TotalAdvanceApplied);
        Assert.Equal(2000m, response.CurrentAdvanceBalance); // 5000 - 3000
        Assert.Equal(0m, response.OutstandingAmount); // 3000 - 3000 = 0
        Assert.Equal(0m, response.AmountAvailableForPayout); // Nothing payable in cash
        Assert.Equal(2000m, response.AdvanceCarriedForward); // 2000 carries forward
        Assert.Equal("PAID", response.SettlementStatus);
    }

    [Fact]
    public async Task CalculateSettlementAsync_WhenNoEarningsInPeriod_ReturnsCarryForwardOnlyStatus()
    {
        var (store, _) = SetupScenario();
        var service = new WorkerSettlementService(store);

        // Worker received advance 2000, no earnings at all yet
        CreateCompletedPayment(store, PaymentType.Advance, 2000m, new DateOnly(2026, 9, 1));

        var response = await service.CalculateSettlementAsync(CreateActor(), new WorkerSettlementQuery(_workerId));

        Assert.Equal(0m, response.GrossPayable);
        Assert.Equal(2000m, response.TotalAdvances);
        Assert.Equal(0m, response.TotalAdvanceApplied);
        Assert.Equal(2000m, response.CurrentAdvanceBalance);
        Assert.Equal(0m, response.OutstandingAmount);
        Assert.Equal(0m, response.AmountAvailableForPayout);
        Assert.Equal(2000m, response.AdvanceCarriedForward);
        Assert.Equal("CARRY_FORWARD_ONLY", response.SettlementStatus);
    }

    #endregion

    #region Unallocated Advance Offsetting Payout Amount

    [Fact]
    public async Task CalculateSettlementAsync_UnallocatedAdvanceReducesAmountAvailableForPayout()
    {
        var (store, _) = SetupScenario();
        var service = new WorkerSettlementService(store);

        // Earnings 3000
        CreateApprovedEarning(store, new DateOnly(2026, 9, 1), 3000m);
        // Advance 1000 received, but not yet allocated
        CreateCompletedPayment(store, PaymentType.Advance, 1000m, new DateOnly(2026, 9, 2));

        var response = await service.CalculateSettlementAsync(CreateActor(), new WorkerSettlementQuery(_workerId));

        Assert.Equal(3000m, response.GrossPayable);
        Assert.Equal(1000m, response.TotalAdvances);
        Assert.Equal(0m, response.TotalAdvanceApplied);
        Assert.Equal(1000m, response.CurrentAdvanceBalance);
        Assert.Equal(3000m, response.OutstandingAmount);
        // Cash available to payout is 3000 - 1000 = 2000!
        Assert.Equal(2000m, response.AmountAvailableForPayout);
        Assert.Equal(0m, response.AdvanceCarriedForward);
    }

    #endregion

    #region Adjustments Support

    [Fact]
    public async Task CalculateSettlementAsync_IncludesApprovedPositiveAndNegativeAdjustments()
    {
        var (store, _) = SetupScenario();
        var service = new WorkerSettlementService(store);

        CreateApprovedEarning(store, new DateOnly(2026, 9, 1), 1000m);

        // Positive adjustment: +200
        var adjPos = WorkerEarningsLedger.CreateAdjustment(
            organizationId: _organizationId,
            workerId: _workerId,
            earningsDate: new DateOnly(2026, 9, 2),
            wageType: WageType.FullDay,
            quantity: 1m,
            wageRate: 200m,
            currencyId: _currencyId,
            grossAmount: 200m,
            referenceLedgerId: Guid.NewGuid(),
            createdBy: _userId,
            approveImmediately: true);
        store.Earnings.Add(adjPos);

        // Negative adjustment: -150
        var adjNeg = WorkerEarningsLedger.CreateAdjustment(
            organizationId: _organizationId,
            workerId: _workerId,
            earningsDate: new DateOnly(2026, 9, 3),
            wageType: WageType.FullDay,
            quantity: 1m,
            wageRate: 150m,
            currencyId: _currencyId,
            grossAmount: -150m,
            referenceLedgerId: Guid.NewGuid(),
            createdBy: _userId,
            approveImmediately: true);
        store.Earnings.Add(adjNeg);

        var response = await service.CalculateSettlementAsync(CreateActor(), new WorkerSettlementQuery(_workerId));

        Assert.Equal(1000m, response.TotalEarnings);
        Assert.Equal(200m, response.TotalApprovedPositiveAdjustments);
        Assert.Equal(150m, response.TotalApprovedNegativeAdjustments);
        Assert.Equal(50m, response.TotalApprovedAdjustments);
        Assert.Equal(1050m, response.GrossPayable);
        Assert.Equal(1050m, response.OutstandingAmount);
    }

    #endregion

    #region Unapproved Earnings

    [Fact]
    public async Task CalculateSettlementAsync_WhenOnlyCalculatedEarningsExist_ReturnsNotReadyStatus()
    {
        var (store, _) = SetupScenario();
        var service = new WorkerSettlementService(store);

        var calculatedEarning = WorkerEarningsLedger.CreateEarning(
            organizationId: _organizationId,
            workerId: _workerId,
            earningsDate: new DateOnly(2026, 9, 1),
            wageType: WageType.FullDay,
            quantity: 1m,
            wageRate: 400m,
            currencyId: _currencyId,
            createdBy: _userId,
            approveImmediately: false); // Status = Calculated
        store.Earnings.Add(calculatedEarning);

        var response = await service.CalculateSettlementAsync(CreateActor(), new WorkerSettlementQuery(_workerId));

        Assert.Equal(0m, response.GrossPayable);
        Assert.Equal(1, response.PendingApprovalEarningCount);
        Assert.Equal("NOT_READY", response.SettlementStatus);
    }

    #endregion

    #region Multi-Tenant Isolation

    [Fact]
    public async Task CalculateSettlementAsync_WhenWorkerInDifferentOrganization_ThrowsResourceNotFoundException()
    {
        var (store, _) = SetupScenario();
        var service = new WorkerSettlementService(store);

        var otherOrgActor = new SettlementActor(_userId, Guid.NewGuid());

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            service.CalculateSettlementAsync(otherOrgActor, new WorkerSettlementQuery(_workerId)));
    }

    #endregion

    #region Batch Calculation

    [Fact]
    public async Task CalculateBatchSettlementAsync_CalculatesSettlementsForMultipleWorkers()
    {
        var (store, worker1) = SetupScenario();
        var worker2 = new Worker(
            _organizationId,
            "Suresh",
            "Patil",
            Gender.Male,
            EmploymentType.Permanent,
            _userId);
        var worker2Id = Guid.NewGuid();
        SetEntityId(worker2, worker2Id);
        store.Workers.Add(worker2);

        CreateApprovedEarning(store, new DateOnly(2026, 9, 1), 1000m, worker1.Id);
        CreateApprovedEarning(store, new DateOnly(2026, 9, 1), 1500m, worker2Id);

        var service = new WorkerSettlementService(store);
        var batch = await service.CalculateBatchSettlementAsync(CreateActor(), new BatchWorkerSettlementQuery());

        Assert.Equal(2, batch.Count);
        var res1 = batch.First(r => r.WorkerId == worker1.Id);
        Assert.Equal(1000m, res1.GrossPayable);
        var res2 = batch.First(r => r.WorkerId == worker2Id);
        Assert.Equal(1500m, res2.GrossPayable);
    }

    #endregion

    #region Helpers

    private WorkerEarningsLedger CreateApprovedEarning(FakeWorkerSettlementStore store, DateOnly date, decimal amount, Guid? workerId = null)
    {
        var earning = WorkerEarningsLedger.CreateEarning(
            organizationId: _organizationId,
            workerId: workerId ?? _workerId,
            earningsDate: date,
            wageType: WageType.FullDay,
            quantity: 1m,
            wageRate: amount,
            currencyId: _currencyId,
            createdBy: _userId,
            approveImmediately: true);
        store.Earnings.Add(earning);
        return earning;
    }

    private WorkerPayment CreateCompletedPayment(FakeWorkerSettlementStore store, PaymentType type, decimal amount, DateOnly date, Guid? workerId = null)
    {
        var payment = type == PaymentType.Advance
            ? WorkerPayment.CreateAdvance(_organizationId, workerId ?? _workerId, date, amount, _currencyId, PaymentMethod.Cash, _userId)
            : WorkerPayment.CreatePayout(_organizationId, workerId ?? _workerId, date, amount, _currencyId, PaymentMethod.BankTransfer, _userId);
        store.Payments.Add(payment);
        return payment;
    }

    private static void SetEntityId<T>(T entity, Guid id) where T : class
    {
        var prop = typeof(T).GetProperty("Id");
        prop?.SetValue(entity, id);
    }

    private sealed class FakeWorkerSettlementStore : IWorkerSettlementStore
    {
        public List<Worker> Workers { get; } = [];
        public List<WorkerEarningsLedger> Earnings { get; } = [];
        public List<WorkerPayment> Payments { get; } = [];
        public List<WorkerPaymentAllocation> Allocations { get; } = [];

        public Task<Worker?> FindWorkerAsync(Guid workerId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Workers.FirstOrDefault(w => w.Id == workerId && w.OrganizationId == organizationId));

        public Task<IReadOnlyList<Worker>> ListWorkersAsync(Guid organizationId, IReadOnlyList<Guid>? workerIds = null, CancellationToken cancellationToken = default)
        {
            var query = Workers.Where(w => w.OrganizationId == organizationId);
            if (workerIds is not null && workerIds.Count > 0)
            {
                query = query.Where(w => workerIds.Contains(w.Id));
            }
            return Task.FromResult<IReadOnlyList<Worker>>(query.ToList());
        }

        public Task<IReadOnlyList<WorkerEarningsLedger>> GetEarningsForSettlementAsync(
            Guid organizationId,
            Guid workerId,
            DateOnly? periodFrom = null,
            DateOnly? periodTo = null,
            DateOnly? asOfDate = null,
            CancellationToken cancellationToken = default)
        {
            var query = Earnings.Where(e => e.OrganizationId == organizationId && e.WorkerId == workerId);
            if (periodFrom.HasValue) query = query.Where(e => e.EarningsDate >= periodFrom.Value);
            if (periodTo.HasValue) query = query.Where(e => e.EarningsDate <= periodTo.Value);
            if (asOfDate.HasValue) query = query.Where(e => e.EarningsDate <= asOfDate.Value);
            return Task.FromResult<IReadOnlyList<WorkerEarningsLedger>>(query.OrderBy(e => e.EarningsDate).ToList());
        }

        public Task<IReadOnlyList<WorkerPayment>> GetCompletedPaymentsAsync(
            Guid organizationId,
            Guid workerId,
            DateOnly? asOfDate = null,
            CancellationToken cancellationToken = default)
        {
            var query = Payments.Where(p => p.OrganizationId == organizationId && p.WorkerId == workerId && p.Status == PaymentStatus.Completed);
            if (asOfDate.HasValue) query = query.Where(p => p.PaymentDate <= asOfDate.Value);
            return Task.FromResult<IReadOnlyList<WorkerPayment>>(query.OrderBy(p => p.PaymentDate).ToList());
        }

        public Task<IReadOnlyList<WorkerPaymentAllocation>> GetAllocationsForWorkerAsync(
            Guid organizationId,
            Guid workerId,
            DateOnly? asOfDate = null,
            CancellationToken cancellationToken = default)
        {
            var query = Allocations.Where(a => a.OrganizationId == organizationId &&
                                               Payments.Any(p => p.Id == a.WorkerPaymentId && p.WorkerId == workerId && p.Status != PaymentStatus.Cancelled));
            if (asOfDate.HasValue) query = query.Where(a => a.AllocationDate <= asOfDate.Value);
            return Task.FromResult<IReadOnlyList<WorkerPaymentAllocation>>(query.OrderBy(a => a.AllocationDate).ToList());
        }
    }

    #endregion
}
