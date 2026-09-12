using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Labor;
using FarmManagement.Application.Interfaces.Labor;
using FarmManagement.Application.Services;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Services;

public sealed class WorkerEarningsLedgerServiceTests
{
    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _workerId = Guid.NewGuid();
    private readonly Guid _currencyId = Guid.NewGuid();

    private EarningsActor CreateActor() => new(_userId, _organizationId);

    private (FakeWorkerEarningsLedgerStore store, Worker worker, LaborWageRate wageRate) SetupDefaultScenario()
    {
        var store = new FakeWorkerEarningsLedgerStore();
        var currency = new Currency("INR", "Indian Rupee", "₹", true, 1, _currencyId);
        store.Currencies.Add(currency);

        var worker = new Worker(
            _organizationId,
            "Ramesh",
            "Kumar",
            Gender.Male,
            EmploymentType.Permanent,
            _userId);
        SetEntityId(worker, _workerId);
        store.Workers.Add(worker);

        var wageRate = new LaborWageRate(
            _organizationId,
            Gender.Male,
            WageType.FullDay,
            500m,
            _currencyId,
            new DateOnly(2026, 1, 1),
            _userId);
        store.WageRates.Add(wageRate);

        return (store, worker, wageRate);
    }

    #region Attendance Integration Tests

    [Fact]
    public async Task CreateEarningFromFinalizedAttendanceAsync_WithValidAttendance_CreatesEntryWithWageSnapshot()
    {
        var (store, worker, _) = SetupDefaultScenario();
        var service = new WorkerEarningsLedgerService(store);
        var attendanceId = Guid.NewGuid();
        var attendanceDate = new DateOnly(2026, 9, 1);

        var request = new FinalizedAttendanceRecord(
            AttendanceId: attendanceId,
            OrganizationId: _organizationId,
            WorkerId: _workerId,
            AttendanceDate: attendanceDate,
            WageType: WageType.FullDay,
            Quantity: 1m,
            Description: "Harvesting attendance");

        var response = await service.CreateEarningFromFinalizedAttendanceAsync(CreateActor(), request);

        Assert.NotNull(response);
        Assert.Equal(_organizationId, response.OrganizationId);
        Assert.Equal(_workerId, response.WorkerId);
        Assert.Equal("Ramesh Kumar", response.WorkerDisplayName);
        Assert.Equal(attendanceId, response.AttendanceId);
        Assert.Equal(attendanceDate, response.EarningsDate);
        Assert.Equal("FULL_DAY", response.WageType);
        Assert.Equal(1m, response.Quantity);
        Assert.Equal(500m, response.WageRate);
        Assert.Equal(500m, response.GrossAmount);
        Assert.Equal("INR", response.CurrencyCode);
        Assert.Equal("₹", response.CurrencySymbol);
        Assert.Equal("EARNING", response.EntryType);
        Assert.Equal("CALCULATED", response.Status);
        Assert.Null(response.FinalizedAt);

        Assert.Single(store.LedgerEntries);
        Assert.Single(store.AuditLogs);
        Assert.Equal("WorkerEarnings.CreatedFromAttendance", store.AuditLogs[0].Action);
    }

    [Fact]
    public async Task CreateEarningFromFinalizedAttendanceAsync_WithAutoApprove_CreatesApprovedEntry()
    {
        var (store, _, _) = SetupDefaultScenario();
        var service = new WorkerEarningsLedgerService(store);

        var request = new FinalizedAttendanceRecord(
            AttendanceId: Guid.NewGuid(),
            OrganizationId: _organizationId,
            WorkerId: _workerId,
            AttendanceDate: new DateOnly(2026, 9, 1),
            WageType: WageType.FullDay,
            Quantity: 1m,
            AutoApprove: true);

        var response = await service.CreateEarningFromFinalizedAttendanceAsync(CreateActor(), request);

        Assert.Equal("APPROVED", response.Status);
        Assert.NotNull(response.FinalizedAt);
        Assert.Equal(_userId, response.FinalizedBy);
    }

    [Fact]
    public async Task CreateEarningFromFinalizedAttendanceAsync_WhenWorkerInactive_ThrowsValidationException()
    {
        var (store, worker, _) = SetupDefaultScenario();
        worker.Deactivate(DateTimeOffset.UtcNow, _userId);
        var service = new WorkerEarningsLedgerService(store);

        var request = new FinalizedAttendanceRecord(
            AttendanceId: Guid.NewGuid(),
            OrganizationId: _organizationId,
            WorkerId: _workerId,
            AttendanceDate: new DateOnly(2026, 9, 1),
            WageType: WageType.FullDay,
            Quantity: 1m);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateEarningFromFinalizedAttendanceAsync(CreateActor(), request));
    }

    [Fact]
    public async Task CreateEarningFromFinalizedAttendanceAsync_WhenAttendanceBelongsToDifferentOrg_ThrowsValidationException()
    {
        var (store, _, _) = SetupDefaultScenario();
        var service = new WorkerEarningsLedgerService(store);

        var request = new FinalizedAttendanceRecord(
            AttendanceId: Guid.NewGuid(),
            OrganizationId: Guid.NewGuid(), // Foreign org
            WorkerId: _workerId,
            AttendanceDate: new DateOnly(2026, 9, 1),
            WageType: WageType.FullDay,
            Quantity: 1m);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateEarningFromFinalizedAttendanceAsync(CreateActor(), request));
    }

    [Fact]
    public async Task CreateEarningFromFinalizedAttendanceAsync_WhenNoApplicableWageRate_ThrowsValidationException()
    {
        var (store, _, _) = SetupDefaultScenario();
        var service = new WorkerEarningsLedgerService(store);

        // Hourly rate is not configured in default scenario (only FullDay is configured)
        var request = new FinalizedAttendanceRecord(
            AttendanceId: Guid.NewGuid(),
            OrganizationId: _organizationId,
            WorkerId: _workerId,
            AttendanceDate: new DateOnly(2026, 9, 1),
            WageType: WageType.Hourly,
            Quantity: 4m);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateEarningFromFinalizedAttendanceAsync(CreateActor(), request));
    }

    #endregion

    #region Reversal Tests

    [Fact]
    public async Task ReverseEarningAsync_WithApprovedEntry_CreatesReversalAndMarksOriginalReversed()
    {
        var (store, _, _) = SetupDefaultScenario();
        var service = new WorkerEarningsLedgerService(store);

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
        store.LedgerEntries.Add(original);

        var request = new ReverseEarningsRequest("Attendance recorded in error");
        var response = await service.ReverseEarningAsync(CreateActor(), original.Id, request, "127.0.0.1");

        Assert.NotNull(response);
        Assert.Equal("REVERSAL", response.EntryType);
        Assert.Equal("REVERSED", response.Status);
        Assert.Equal(-500m, response.GrossAmount);
        Assert.Equal(original.Id, response.ReferenceLedgerId);
        Assert.Equal(EarningsLedgerStatus.Reversed, original.Status);

        Assert.Equal(2, store.LedgerEntries.Count);
        Assert.Single(store.AuditLogs);
        Assert.Equal("WorkerEarnings.Reversed", store.AuditLogs[0].Action);
    }

    [Fact]
    public async Task ReverseEarningAsync_WhenAlreadyReversed_ThrowsValidationException()
    {
        var (store, _, _) = SetupDefaultScenario();
        var service = new WorkerEarningsLedgerService(store);

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
        store.LedgerEntries.Add(original);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.ReverseEarningAsync(CreateActor(), original.Id, new ReverseEarningsRequest()));
    }

    [Fact]
    public async Task ReverseEarningAsync_WhenDuplicateReversalExists_ThrowsConflictException()
    {
        var (store, _, _) = SetupDefaultScenario();
        var service = new WorkerEarningsLedgerService(store);

        var original = WorkerEarningsLedger.CreateEarning(
            _organizationId,
            _workerId,
            new DateOnly(2026, 9, 1),
            WageType.FullDay,
            1m,
            500m,
            _currencyId,
            _userId);
        store.LedgerEntries.Add(original);

        var existingReversal = WorkerEarningsLedger.CreateReversal(original, _userId, DateTimeOffset.UtcNow);
        store.LedgerEntries.Add(existingReversal);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.ReverseEarningAsync(CreateActor(), original.Id, new ReverseEarningsRequest()));
    }

    #endregion

    #region Adjustment Tests

    [Fact]
    public async Task CreateAdjustmentAsync_WithValidPositiveAmount_CreatesAdjustment()
    {
        var (store, _, _) = SetupDefaultScenario();
        var service = new WorkerEarningsLedgerService(store);

        var request = new CreateEarningsAdjustmentRequest(
            WorkerId: _workerId,
            EarningsDate: new DateOnly(2026, 9, 3),
            GrossAmount: 100m,
            CurrencyId: _currencyId,
            WageType: "HOURLY",
            Quantity: 1m,
            ApproveImmediately: true,
            Description: "Festival bonus adjustment");

        var response = await service.CreateAdjustmentAsync(CreateActor(), request, "127.0.0.1");

        Assert.NotNull(response);
        Assert.Equal("ADJUSTMENT", response.EntryType);
        Assert.Equal(100m, response.GrossAmount);
        Assert.Equal("APPROVED", response.Status);
        Assert.Equal("Festival bonus adjustment", response.Description);
        Assert.Single(store.AuditLogs);
        Assert.Equal("WorkerEarnings.AdjustmentCreated", store.AuditLogs[0].Action);
    }

    [Fact]
    public async Task CreateAdjustmentAsync_WithZeroAmount_ThrowsValidationException()
    {
        var (store, _, _) = SetupDefaultScenario();
        var service = new WorkerEarningsLedgerService(store);

        var request = new CreateEarningsAdjustmentRequest(
            WorkerId: _workerId,
            EarningsDate: new DateOnly(2026, 9, 3),
            GrossAmount: 0m,
            CurrencyId: _currencyId);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAdjustmentAsync(CreateActor(), request));
    }

    #endregion

    #region Approval Tests

    [Fact]
    public async Task ApproveEarningAsync_WithCalculatedEntry_TransitionsToApproved()
    {
        var (store, _, _) = SetupDefaultScenario();
        var service = new WorkerEarningsLedgerService(store);

        var entry = WorkerEarningsLedger.CreateEarning(
            _organizationId,
            _workerId,
            new DateOnly(2026, 9, 1),
            WageType.FullDay,
            1m,
            500m,
            _currencyId,
            _userId);
        store.LedgerEntries.Add(entry);

        var response = await service.ApproveEarningAsync(CreateActor(), entry.Id, "127.0.0.1");

        Assert.Equal("APPROVED", response.Status);
        Assert.NotNull(response.FinalizedAt);
        Assert.Equal(_userId, response.FinalizedBy);
        Assert.Single(store.AuditLogs);
        Assert.Equal("WorkerEarnings.Approved", store.AuditLogs[0].Action);
    }

    [Fact]
    public async Task ApproveEarningAsync_WhenAlreadyReversed_ThrowsValidationException()
    {
        var (store, _, _) = SetupDefaultScenario();
        var service = new WorkerEarningsLedgerService(store);

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
        store.LedgerEntries.Add(entry);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.ApproveEarningAsync(CreateActor(), entry.Id));
    }

    #endregion

    #region Balance Calculation Tests

    [Fact]
    public async Task GetWorkerGrossEarnedBalanceAsync_CalculatesCorrectGrossBalanceWithReversalsAndAdjustments()
    {
        var (store, _, _) = SetupDefaultScenario();
        var service = new WorkerEarningsLedgerService(store);

        // Entry 1: 01-Sep Full Day ₹500 (Approved)
        var e1 = WorkerEarningsLedger.CreateEarning(
            _organizationId, _workerId, new DateOnly(2026, 9, 1),
            WageType.FullDay, 1m, 500m, _currencyId, _userId, approveImmediately: true);

        // Entry 2: 02-Sep Full Day ₹500 (Originally approved, then reversed)
        var e2 = WorkerEarningsLedger.CreateEarning(
            _organizationId, _workerId, new DateOnly(2026, 9, 2),
            WageType.FullDay, 1m, 500m, _currencyId, _userId, approveImmediately: true);
        // Entry 3: Reversal for Entry 2 (-₹500, Reversed)
        var r2 = WorkerEarningsLedger.CreateReversal(e2, _userId, DateTimeOffset.UtcNow);
        e2.MarkReversed(DateTimeOffset.UtcNow, _userId);

        // Entry 4: 02-Sep Corrected Half Day ₹250 (Approved)
        var e4 = WorkerEarningsLedger.CreateEarning(
            _organizationId, _workerId, new DateOnly(2026, 9, 2),
            WageType.HalfDay, 1m, 250m, _currencyId, _userId, approveImmediately: true, referenceLedgerId: e2.Id);

        // Entry 5: Adjustment +₹100 (Approved)
        var adj = WorkerEarningsLedger.CreateAdjustment(
            _organizationId, _workerId, new DateOnly(2026, 9, 3),
            WageType.Hourly, 1m, 100m, 100m, _currencyId, _userId, approveImmediately: true);

        // Entry 6: 04-Sep Calculated Earning ₹500 (NOT approved yet, draft)
        var pending = WorkerEarningsLedger.CreateEarning(
            _organizationId, _workerId, new DateOnly(2026, 9, 4),
            WageType.FullDay, 1m, 500m, _currencyId, _userId, approveImmediately: false);

        store.LedgerEntries.AddRange([e1, e2, r2, e4, adj, pending]);

        var balance = await service.GetWorkerGrossEarnedBalanceAsync(
            CreateActor(),
            _workerId,
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 30));

        Assert.NotNull(balance);
        Assert.Equal(_workerId, balance.WorkerId);
        // Gross earned = Approved Earnings (500 + 250 = 750) + Approved Adjustments (100) = 850
        Assert.Equal(850m, balance.GrossEarned);
        Assert.Equal(750m, balance.TotalApprovedEarnings);
        Assert.Equal(-500m, balance.TotalReversals);
        Assert.Equal(100m, balance.TotalAdjustments);
        Assert.Equal(500m, balance.TotalPendingCalculated);
        Assert.Equal(3, balance.ApprovedEntryCount); // e1, e4, adj
        Assert.Equal(2, balance.ReversedEntryCount); // e2, r2
        Assert.Equal(1, balance.CalculatedEntryCount); // pending
    }

    #endregion

    private static void SetEntityId<T>(T entity, Guid id)
    {
        var property = typeof(T).GetProperty("Id");
        property?.SetValue(entity, id);
    }

    #region Fake Store

    private sealed class FakeWorkerEarningsLedgerStore : IWorkerEarningsLedgerStore
    {
        public List<WorkerEarningsLedger> LedgerEntries { get; } = [];
        public List<Worker> Workers { get; } = [];
        public List<LaborWageRate> WageRates { get; } = [];
        public List<Currency> Currencies { get; } = [];
        public List<AuditLog> AuditLogs { get; } = [];

        public Task<WorkerEarningsLedger?> FindAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(LedgerEntries.FirstOrDefault(e => e.Id == id && e.OrganizationId == organizationId));

        public Task<PagedResponse<WorkerEarningsLedger>> ListByWorkerAsync(
            Guid organizationId,
            Guid workerId,
            DateOnly? fromDate,
            DateOnly? toDate,
            EarningsLedgerStatus? status,
            EarningsEntryType? entryType,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query = LedgerEntries.Where(e => e.OrganizationId == organizationId && e.WorkerId == workerId);
            if (fromDate.HasValue) query = query.Where(e => e.EarningsDate >= fromDate.Value);
            if (toDate.HasValue) query = query.Where(e => e.EarningsDate <= toDate.Value);
            if (status.HasValue) query = query.Where(e => e.Status == status.Value);
            if (entryType.HasValue) query = query.Where(e => e.EntryType == entryType.Value);

            var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return Task.FromResult(new PagedResponse<WorkerEarningsLedger>(items, query.Count(), page, pageSize));
        }

        public Task<IReadOnlyList<WorkerEarningsLedger>> ListForBalanceCalculationAsync(
            Guid organizationId,
            Guid workerId,
            DateOnly? fromDate,
            DateOnly? toDate,
            CancellationToken cancellationToken = default)
        {
            var query = LedgerEntries.Where(e => e.OrganizationId == organizationId && e.WorkerId == workerId);
            if (fromDate.HasValue) query = query.Where(e => e.EarningsDate >= fromDate.Value);
            if (toDate.HasValue) query = query.Where(e => e.EarningsDate <= toDate.Value);

            return Task.FromResult<IReadOnlyList<WorkerEarningsLedger>>(query.ToList());
        }

        public Task<bool> HasActiveReversalAsync(Guid originalLedgerId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(LedgerEntries.Any(e =>
                e.OrganizationId == organizationId &&
                e.ReferenceLedgerId == originalLedgerId &&
                e.EntryType == EarningsEntryType.Reversal));

        public Task<Worker?> FindWorkerAsync(Guid workerId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Workers.FirstOrDefault(w => w.Id == workerId && w.OrganizationId == organizationId));

        public Task<LaborWageRate?> FindApplicableWageRateAsync(
            Guid organizationId,
            Gender gender,
            WageType wageType,
            DateOnly businessDate,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(WageRates.FirstOrDefault(r =>
                r.OrganizationId == organizationId &&
                r.Gender == gender &&
                r.WageType == wageType &&
                r.IsActive &&
                businessDate >= r.EffectiveFrom &&
                (!r.EffectiveTo.HasValue || businessDate <= r.EffectiveTo.Value)));

        public Task<Currency?> FindCurrencyAsync(Guid currencyId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Currencies.FirstOrDefault(c => c.Id == currencyId));

        public void Add(WorkerEarningsLedger entry) => LedgerEntries.Add(entry);

        public void AddAuditLog(AuditLog auditLog) => AuditLogs.Add(auditLog);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    #endregion
}
