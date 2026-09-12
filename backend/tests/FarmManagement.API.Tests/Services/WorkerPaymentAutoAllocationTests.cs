using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.Labor;
using FarmManagement.Application.Interfaces.Labor;
using FarmManagement.Application.Services;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Services;

public sealed class WorkerPaymentAutoAllocationTests
{
    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _workerId = Guid.NewGuid();
    private readonly Guid _currencyId = Guid.NewGuid();

    private PaymentActor CreateActor() => new(_userId, _organizationId);

    private (FakeWorkerPaymentStore paymentStore, FakeEarningsStore earningsStore, FakeAllocationService allocationService) SetupScenario()
    {
        var paymentStore = new FakeWorkerPaymentStore();
        var earningsStore = new FakeEarningsStore();
        var allocationService = new FakeAllocationService();

        var currency = new Currency("INR", "Indian Rupee", "₹", true, 1, _currencyId);
        paymentStore.Currencies.Add(currency);

        var worker = new Worker(
            _organizationId,
            "Ramesh",
            "Kumar",
            Gender.Male,
            EmploymentType.Permanent,
            _userId);
        typeof(Worker).GetProperty("Id")!.SetValue(worker, _workerId);
        paymentStore.Workers.Add(worker);

        return (paymentStore, earningsStore, allocationService);
    }

    [Fact]
    public async Task RecordPaymentAsync_CompletedPayout_CallsAutoAllocate()
    {
        var (paymentStore, earningsStore, allocationService) = SetupScenario();
        var service = new WorkerPaymentService(paymentStore, earningsStore, allocationStore: null, allocationService: allocationService);

        var request = new RecordWorkerPaymentRequest(
            WorkerId: _workerId,
            PaymentDate: new DateOnly(2026, 9, 12),
            PaymentType: "PAYOUT",
            Amount: 350m,
            CurrencyId: _currencyId,
            PaymentMethod: "CASH",
            AutoAllocate: true);

        var response = await service.RecordPaymentAsync(CreateActor(), request);

        Assert.NotNull(response);
        Assert.Equal(350m, response.Amount);
        Assert.Single(allocationService.AutoAllocatedRequests);
        Assert.Equal(response.Id, allocationService.AutoAllocatedRequests[0].WorkerPaymentId);
    }

    [Fact]
    public async Task RecordPaymentAsync_WhenAutoAllocateIsFalse_DoesNotCallAutoAllocate()
    {
        var (paymentStore, earningsStore, allocationService) = SetupScenario();
        var service = new WorkerPaymentService(paymentStore, earningsStore, allocationStore: null, allocationService: allocationService);

        var request = new RecordWorkerPaymentRequest(
            WorkerId: _workerId,
            PaymentDate: new DateOnly(2026, 9, 12),
            PaymentType: "PAYOUT",
            Amount: 350m,
            CurrencyId: _currencyId,
            PaymentMethod: "CASH",
            AutoAllocate: false);

        var response = await service.RecordPaymentAsync(CreateActor(), request);

        Assert.NotNull(response);
        Assert.Empty(allocationService.AutoAllocatedRequests);
    }

    [Fact]
    public async Task RecordPaymentAsync_WhenAutoAllocateThrowsValidation_PaymentStillSucceeds()
    {
        var (paymentStore, earningsStore, allocationService) = SetupScenario();
        allocationService.ThrowOnAutoAllocate = new ValidationException("No eligible unsettled earnings found for allocation.");
        var service = new WorkerPaymentService(paymentStore, earningsStore, allocationStore: null, allocationService: allocationService);

        var request = new RecordWorkerPaymentRequest(
            WorkerId: _workerId,
            PaymentDate: new DateOnly(2026, 9, 12),
            PaymentType: "PAYOUT",
            Amount: 350m,
            CurrencyId: _currencyId,
            PaymentMethod: "CASH",
            AutoAllocate: true);

        var response = await service.RecordPaymentAsync(CreateActor(), request);

        Assert.NotNull(response);
        Assert.Equal(350m, response.Amount);
        Assert.Single(paymentStore.Payments);
    }

    private sealed class FakeAllocationService : IWorkerPaymentAllocationService
    {
        public List<AutoAllocatePaymentRequest> AutoAllocatedRequests { get; } = [];
        public Exception? ThrowOnAutoAllocate { get; set; }

        public Task<IReadOnlyList<WorkerPaymentAllocationResponse>> AutoAllocatePaymentAsync(
            PaymentActor actor,
            AutoAllocatePaymentRequest request,
            string? ipAddress = null,
            CancellationToken cancellationToken = default)
        {
            if (ThrowOnAutoAllocate != null)
            {
                throw ThrowOnAutoAllocate;
            }

            AutoAllocatedRequests.Add(request);
            return Task.FromResult<IReadOnlyList<WorkerPaymentAllocationResponse>>([]);
        }

        public Task<WorkerPaymentAllocationResponse> CreateAllocationAsync(PaymentActor actor, CreateWorkerPaymentAllocationRequest request, string? ipAddress = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteAllocationAsync(PaymentActor actor, Guid id, string? ipAddress = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<WorkerPaymentAllocationResponse> GetByIdAsync(PaymentActor actor, Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PaymentUnallocatedBalanceResponse> GetPaymentUnallocatedBalanceAsync(PaymentActor actor, Guid paymentId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<EarningUnpaidBalanceResponse>> GetUnsettledEarningsForWorkerAsync(PaymentActor actor, Guid workerId, DateOnly? upToDate = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<WorkerPaymentAllocationResponse>> ListByEarningAsync(PaymentActor actor, Guid earningId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<WorkerPaymentAllocationResponse>> ListByPaymentAsync(PaymentActor actor, Guid paymentId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<WorkerPaymentAllocationResponse>> ListByWorkerAsync(PaymentActor actor, Guid workerId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class FakeWorkerPaymentStore : IWorkerPaymentStore
    {
        public List<WorkerPayment> Payments { get; } = [];
        public List<Worker> Workers { get; } = [];
        public List<Currency> Currencies { get; } = [];
        public List<AuditLog> AuditLogs { get; } = [];

        public void Add(WorkerPayment payment) => Payments.Add(payment);
        public void AddAuditLog(AuditLog auditLog) => AuditLogs.Add(auditLog);
        public Task<WorkerPayment?> FindAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Payments.FirstOrDefault(p => p.Id == id && p.OrganizationId == organizationId));
        public Task<Worker?> FindWorkerAsync(Guid workerId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Workers.FirstOrDefault(w => w.Id == workerId && w.OrganizationId == organizationId));
        public Task<Currency?> FindCurrencyAsync(Guid currencyId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Currencies.FirstOrDefault(c => c.Id == currencyId));
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<FarmManagement.Application.Common.Models.PagedResponse<WorkerPayment>> ListByWorkerAsync(Guid organizationId, Guid workerId, DateOnly? fromDate, DateOnly? toDate, PaymentType? paymentType, PaymentStatus? status, int page, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<WorkerPayment>> ListPaymentsForFinancialSummaryAsync(Guid organizationId, Guid workerId, DateOnly? asOfDate, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class FakeEarningsStore : IWorkerEarningsLedgerStore
    {
        public Task<IReadOnlyList<WorkerEarningsLedger>> ListForBalanceCalculationAsync(Guid organizationId, Guid workerId, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WorkerEarningsLedger>>([]);
        public Task<WorkerEarningsLedger?> FindAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<WorkerEarningsLedger?> FindByAttendanceIdAsync(Guid organizationId, Guid attendanceId, CancellationToken cancellationToken = default) =>
            Task.FromResult<WorkerEarningsLedger?>(null);
        public Task<FarmManagement.Application.Common.Models.PagedResponse<WorkerEarningsLedger>> ListByWorkerAsync(Guid organizationId, Guid workerId, DateOnly? fromDate, DateOnly? toDate, EarningsLedgerStatus? status, EarningsEntryType? entryType, int page, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasActiveReversalAsync(Guid originalLedgerId, Guid organizationId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<Worker?> FindWorkerAsync(Guid workerId, Guid organizationId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<LaborWageRate?> FindApplicableWageRateAsync(Guid organizationId, Gender gender, WageType wageType, DateOnly businessDate, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Currency?> FindCurrencyAsync(Guid currencyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<LaborActivity?> FindActivityAsync(Guid activityId, Guid organizationId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public void Add(WorkerEarningsLedger entry) => throw new NotImplementedException();
        public void AddRange(IEnumerable<WorkerEarningsLedger> entries) => throw new NotImplementedException();
        public void AddAuditLog(AuditLog auditLog) { }
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
