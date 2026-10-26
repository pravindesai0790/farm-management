using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.Labor;
using FarmManagement.Application.Interfaces.Labor;
using FarmManagement.Application.Services;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Services;

public sealed class AttendanceIdempotencyTests
{
    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly Guid _userIdA = Guid.NewGuid();
    private readonly Guid _userIdB = Guid.NewGuid();
    private readonly Guid _ownershipTypeId = Guid.NewGuid();
    private readonly Guid _currencyId = Guid.NewGuid();

    private AttendanceActor CreateActorA() => new(_userIdA, _organizationId);
    private AttendanceActor CreateActorB() => new(_userIdB, _organizationId);

    private AttendanceService CreateService(
        FakeAttendanceStore store,
        FakeAttendanceEarningsIntegration? earnings = null) =>
        new(store, earnings ?? new FakeAttendanceEarningsIntegration(_currencyId));

    private Farm CreateFarm(string name = "Sun Valley Farm") =>
        new(_organizationId, name, _ownershipTypeId, _userIdA);

    private Worker CreateWorker(string firstName = "Suresh", string lastName = "Patil")
    {
        return new Worker(
            _organizationId,
            firstName,
            lastName,
            Gender.Male,
            EmploymentType.Permanent,
            _userIdA,
            displayName: $"{firstName} {lastName}");
    }

    private static WorkerFarmAssignment AssignWorker(Worker worker, Farm farm, DateOnly fromDate)
    {
        var assignment = new WorkerFarmAssignment(
            worker.OrganizationId,
            worker.Id,
            farm.Id,
            fromDate,
            worker.CreatedBy);
        worker.FarmAssignments.Add(assignment);
        return assignment;
    }

    [Fact]
    public async Task CreateDraftAsync_WithIdempotencyKey_FirstCallSucceeds_SecondCallReturnsCachedResponse()
    {
        var store = new FakeAttendanceStore();
        var earnings = new FakeAttendanceEarningsIntegration(_currencyId);
        var service = CreateService(store, earnings);

        var farm = CreateFarm();
        var worker = CreateWorker();
        var date = new DateOnly(2026, 10, 24);
        AssignWorker(worker, farm, date.AddDays(-10));

        store.Farms.Add(farm);
        store.Workers.Add(worker);

        var request = new CreateDraftAttendanceRequest(
            FarmId: farm.Id,
            WorkerId: worker.Id,
            AttendanceDate: date,
            AttendanceType: "FULL_DAY",
            Notes: "Morning shift",
            IdempotencyKey: "draft-key-001");

        // Act 1: Initial call
        var result1 = await service.CreateDraftAsync(CreateActorA(), request);

        Assert.NotNull(result1);
        Assert.Equal("DRAFT", result1.Status);
        Assert.Single(store.Attendances);
        Assert.Single(store.IdempotencyRecords);

        // Act 2: Retry with exact same key and payload
        var result2 = await service.CreateDraftAsync(CreateActorA(), request);

        Assert.NotNull(result2);
        Assert.Equal(result1.Id, result2.Id);
        Assert.Equal(result1.AttendanceDate, result2.AttendanceDate);
        Assert.Equal(result1.Status, result2.Status);
        // Ensure no second attendance record was created
        Assert.Single(store.Attendances);
    }

    [Fact]
    public async Task CreateDraftAsync_WithSameKeyDifferentPayload_ThrowsConflictException()
    {
        var store = new FakeAttendanceStore();
        var earnings = new FakeAttendanceEarningsIntegration(_currencyId);
        var service = CreateService(store, earnings);

        var farm = CreateFarm();
        var worker = CreateWorker();
        var date = new DateOnly(2026, 10, 24);
        AssignWorker(worker, farm, date.AddDays(-10));

        store.Farms.Add(farm);
        store.Workers.Add(worker);

        var request1 = new CreateDraftAttendanceRequest(
            FarmId: farm.Id,
            WorkerId: worker.Id,
            AttendanceDate: date,
            AttendanceType: "FULL_DAY",
            Notes: "Full day work",
            IdempotencyKey: "draft-key-conflict");

        await service.CreateDraftAsync(CreateActorA(), request1);

        // Same key, different notes
        var request2 = new CreateDraftAttendanceRequest(
            FarmId: farm.Id,
            WorkerId: worker.Id,
            AttendanceDate: date,
            AttendanceType: "FULL_DAY",
            Notes: "DIFFERENT NOTES",
            IdempotencyKey: "draft-key-conflict");

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateDraftAsync(CreateActorA(), request2));
    }

    [Fact]
    public async Task SaveDailyDraftBatchAsync_WithIdempotencyKey_FirstCallSucceeds_SecondCallReturnsCachedResponse()
    {
        var store = new FakeAttendanceStore();
        var earnings = new FakeAttendanceEarningsIntegration(_currencyId);
        var service = CreateService(store, earnings);

        var farm = CreateFarm();
        var worker1 = CreateWorker("Worker", "One");
        var worker2 = CreateWorker("Worker", "Two");
        var date = new DateOnly(2026, 10, 24);
        AssignWorker(worker1, farm, date.AddDays(-10));
        AssignWorker(worker2, farm, date.AddDays(-10));

        store.Farms.Add(farm);
        store.Workers.AddRange([worker1, worker2]);

        var request = new SaveDailyDraftAttendanceBatchRequest(
            FarmId: farm.Id,
            AttendanceDate: date,
            Items:
            [
                new(null, worker1.Id, "FULL_DAY"),
                new(null, worker2.Id, "HALF_DAY")
            ],
            RemoveOmittedDrafts: false,
            IdempotencyKey: "batch-draft-key-001");

        // Act 1: Initial call
        var result1 = await service.SaveDailyDraftBatchAsync(CreateActorA(), request);

        Assert.Equal(2, result1.Summary.TotalCount);
        Assert.Equal(2, store.Attendances.Count);
        Assert.Single(store.IdempotencyRecords);

        // Act 2: Identical retry
        var result2 = await service.SaveDailyDraftBatchAsync(CreateActorA(), request);

        Assert.NotNull(result2);
        Assert.Equal(result1.Summary.TotalCount, result2.Summary.TotalCount);
        Assert.Equal(result1.Records.Count, result2.Records.Count);
        Assert.Equal(2, store.Attendances.Count);
    }

    [Fact]
    public async Task SaveDailyDraftBatchAsync_WithSameKeyDifferentPayload_ThrowsConflictException()
    {
        var store = new FakeAttendanceStore();
        var earnings = new FakeAttendanceEarningsIntegration(_currencyId);
        var service = CreateService(store, earnings);

        var farm = CreateFarm();
        var worker1 = CreateWorker("Worker", "One");
        var worker2 = CreateWorker("Worker", "Two");
        var date = new DateOnly(2026, 10, 24);
        AssignWorker(worker1, farm, date.AddDays(-10));
        AssignWorker(worker2, farm, date.AddDays(-10));

        store.Farms.Add(farm);
        store.Workers.AddRange([worker1, worker2]);

        var request1 = new SaveDailyDraftAttendanceBatchRequest(
            FarmId: farm.Id,
            AttendanceDate: date,
            Items: [new(null, worker1.Id, "FULL_DAY")],
            IdempotencyKey: "batch-conflict-key");

        await service.SaveDailyDraftBatchAsync(CreateActorA(), request1);

        var request2 = new SaveDailyDraftAttendanceBatchRequest(
            FarmId: farm.Id,
            AttendanceDate: date,
            Items: [new(null, worker2.Id, "FULL_DAY")],
            IdempotencyKey: "batch-conflict-key");

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.SaveDailyDraftBatchAsync(CreateActorA(), request2));
    }

    [Fact]
    public async Task FinalizeAttendanceAsync_WhenNetworkRetriesWithSameKey_ReturnsOriginalResponseWithoutAlreadyFinalizedError()
    {
        // This reproduces the critical mobile app edge case:
        // A mobile app calls Finalize, server commits, network drops, mobile retries with same key.
        // Without idempotency, retry crashes with: "Cannot finalize attendance because N record(s) are already finalized."
        // With idempotency, retry succeeds and returns the original FinalizeAttendanceResponse!

        var store = new FakeAttendanceStore();
        var earnings = new FakeAttendanceEarningsIntegration(_currencyId);
        var service = CreateService(store, earnings);

        var farm = CreateFarm();
        var worker1 = CreateWorker("Worker", "One");
        var worker2 = CreateWorker("Worker", "Two");
        var date = new DateOnly(2026, 10, 24);
        AssignWorker(worker1, farm, date.AddDays(-10));
        AssignWorker(worker2, farm, date.AddDays(-10));

        store.Farms.Add(farm);
        store.Workers.AddRange([worker1, worker2]);

        // Pre-create 2 draft attendance records
        var draft1 = LaborAttendance.CreateDraft(_organizationId, farm.Id, worker1.Id, date, AttendanceType.FullDay, _userIdA);
        var draft2 = LaborAttendance.CreateDraft(_organizationId, farm.Id, worker2.Id, date, AttendanceType.HalfDay, _userIdA);
        store.Attendances.AddRange([draft1, draft2]);

        var finalizeRequest = new FinalizeAttendanceRequest(
            FarmId: farm.Id,
            AttendanceDate: date,
            IdempotencyKey: "finalize-mobile-retry-key");

        // Act 1: Initial finalize request from mobile
        var response1 = await service.FinalizeAttendanceAsync(CreateActorA(), finalizeRequest);

        Assert.NotNull(response1);
        Assert.Equal(2, response1.FinalizedCount);
        Assert.Equal(2, response1.PaidCount);
        Assert.Equal(2, earnings.CreatedEarnings.Count);
        Assert.All(store.Attendances, a => Assert.Equal(AttendanceStatus.Finalized, a.Status));
        Assert.Single(store.IdempotencyRecords);

        // Act 2: Mobile retries after network timeout with the SAME idempotency key
        var response2 = await service.FinalizeAttendanceAsync(CreateActorA(), finalizeRequest);

        Assert.NotNull(response2);
        Assert.Equal(response1.FinalizedCount, response2.FinalizedCount);
        Assert.Equal(response1.TotalEarnings, response2.TotalEarnings);
        Assert.Equal(response1.PaidCount, response2.PaidCount);

        // CRITICAL CHECK: Earnings ledger was NOT duplicated!
        Assert.Equal(2, earnings.CreatedEarnings.Count);
    }

    [Fact]
    public async Task FinalizeAttendanceAsync_WithSameKeyDifferentPayload_ThrowsConflictException()
    {
        var store = new FakeAttendanceStore();
        var earnings = new FakeAttendanceEarningsIntegration(_currencyId);
        var service = CreateService(store, earnings);

        var farm = CreateFarm();
        var worker = CreateWorker();
        var date = new DateOnly(2026, 10, 24);
        AssignWorker(worker, farm, date.AddDays(-10));

        store.Farms.Add(farm);
        store.Workers.Add(worker);

        var draft = LaborAttendance.CreateDraft(_organizationId, farm.Id, worker.Id, date, AttendanceType.FullDay, _userIdA);
        store.Attendances.Add(draft);

        var request1 = new FinalizeAttendanceRequest(
            FarmId: farm.Id,
            AttendanceDate: date,
            AttendanceIds: [draft.Id],
            IdempotencyKey: "fin-conflict-key");

        await service.FinalizeAttendanceAsync(CreateActorA(), request1);

        // Same key, different attendance IDs list
        var request2 = new FinalizeAttendanceRequest(
            FarmId: farm.Id,
            AttendanceDate: date,
            AttendanceIds: [Guid.NewGuid()],
            IdempotencyKey: "fin-conflict-key");

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.FinalizeAttendanceAsync(CreateActorA(), request2));
    }

    [Fact]
    public async Task DraftThenFinalize_AcrossDaysAndDifferentUsers_WorksSeamlessly()
    {
        // Tests the user's specific scenario:
        // "we are not going to mark/finalize the attendance on same day it will be on draft state.
        // one person can create draft and other person can finalize that same attendance after some time or day"

        var store = new FakeAttendanceStore();
        var earnings = new FakeAttendanceEarningsIntegration(_currencyId);
        var service = CreateService(store, earnings);

        var farm = CreateFarm();
        var worker = CreateWorker("Ramesh", "Patil");
        var attendanceDate = new DateOnly(2026, 10, 20);
        AssignWorker(worker, farm, attendanceDate.AddDays(-10));

        store.Farms.Add(farm);
        store.Workers.Add(worker);

        // Day 1: Person A (Field Supervisor on Mobile) creates draft
        var draftRequest = new SaveDailyDraftAttendanceBatchRequest(
            FarmId: farm.Id,
            AttendanceDate: attendanceDate,
            Items: [new(null, worker.Id, "FULL_DAY", Notes: "Day 1 draft by Person A")],
            RemoveOmittedDrafts: false,
            IdempotencyKey: "person-a-draft-uuid-001");

        var draftResponse = await service.SaveDailyDraftBatchAsync(CreateActorA(), draftRequest);
        Assert.Single(draftResponse.Records);
        Assert.Equal("DRAFT", draftResponse.Records[0].Status);
        Assert.Equal(_userIdA, draftResponse.Records[0].CreatedBy);

        // Day 2 (or later): Person B (Accountant/Manager on Web) reviews and finalizes
        var finalizeRequest = new FinalizeAttendanceRequest(
            FarmId: farm.Id,
            AttendanceDate: attendanceDate,
            IdempotencyKey: "person-b-finalize-uuid-002");

        var finalizeResponse = await service.FinalizeAttendanceAsync(CreateActorB(), finalizeRequest);

        // Verify finalization was successful
        Assert.Equal(1, finalizeResponse.FinalizedCount);
        Assert.Equal(1, finalizeResponse.PaidCount);
        Assert.Equal("FINALIZED", finalizeResponse.Records[0].Status);

        // Verify audit attribution
        var savedRecord = store.Attendances.Single(a => a.WorkerId == worker.Id);
        Assert.Equal(AttendanceStatus.Finalized, savedRecord.Status);
        Assert.Equal(_userIdA, savedRecord.CreatedBy);
        Assert.Equal(_userIdB, savedRecord.FinalizedBy);
        Assert.NotNull(savedRecord.FinalizedAt);

        // Verify two separate idempotency records were stored with different operation types
        Assert.Equal(2, store.IdempotencyRecords.Count);
        Assert.Contains(store.IdempotencyRecords, r => r.OperationType == "SAVE_DRAFT_BATCH" && r.IdempotencyKey == "person-a-draft-uuid-001");
        Assert.Contains(store.IdempotencyRecords, r => r.OperationType == "FINALIZE_ATTENDANCE" && r.IdempotencyKey == "person-b-finalize-uuid-002");
    }

    [Fact]
    public async Task CopyPreviousDayAsync_WithIdempotencyKey_FirstCallSucceeds_SecondCallReturnsCachedResponse()
    {
        var store = new FakeAttendanceStore();
        var earnings = new FakeAttendanceEarningsIntegration(_currencyId);
        var service = CreateService(store, earnings);

        var farm = CreateFarm();
        var worker = CreateWorker();
        var sourceDate = new DateOnly(2026, 10, 23);
        var targetDate = new DateOnly(2026, 10, 24);
        AssignWorker(worker, farm, sourceDate.AddDays(-10));

        store.Farms.Add(farm);
        store.Workers.Add(worker);

        // Source attendance on 2026-10-23
        var sourceAtt = LaborAttendance.CreateFinalized(
            _organizationId, farm.Id, worker.Id, sourceDate, AttendanceType.FullDay, _userIdA,
            workingHours: null, calculatedRate: 500m, calculatedAmount: 500m, currencyId: _currencyId);
        store.Attendances.Add(sourceAtt);

        var copyRequest = new CopyPreviousDayAttendanceRequest(
            FarmId: farm.Id,
            TargetDate: targetDate,
            SourceDate: sourceDate,
            OverwriteExistingDraft: false,
            IdempotencyKey: "copy-day-key-001");

        // Act 1: Initial call
        var result1 = await service.CopyPreviousDayAsync(CreateActorA(), copyRequest);
        Assert.Equal(1, result1.CopiedCount);
        Assert.Single(store.IdempotencyRecords);

        // Act 2: Idempotent retry
        var result2 = await service.CopyPreviousDayAsync(CreateActorA(), copyRequest);
        Assert.Equal(result1.CopiedCount, result2.CopiedCount);
        Assert.Equal(result1.TargetDate, result2.TargetDate);
        // Only 1 target attendance record created (plus original source record = 2 total)
        Assert.Equal(2, store.Attendances.Count);
    }

    #region Fakes

    private sealed class FakeAttendanceEarningsIntegration(Guid currencyId) : IAttendanceEarningsIntegration
    {
        public List<WorkerEarningsLedgerResponse> CreatedEarnings { get; } = [];

        public Task<AttendanceEarningsCalculationResult> CalculateAttendanceEarningsAsync(
            EarningsActor actor,
            CalculateAttendanceEarningsRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new AttendanceEarningsCalculationResult(
                WorkerId: request.WorkerId,
                WorkerDisplayName: "Worker",
                Gender: Gender.Male,
                AttendanceType: request.AttendanceType,
                WageType: request.AttendanceType == "HALF_DAY" ? "HALF_DAY" : "FULL_DAY",
                Quantity: request.Quantity,
                WageRate: 500m,
                GrossAmount: request.AttendanceType == "HALF_DAY" ? 250m : 500m,
                CurrencyId: currencyId,
                CurrencyCode: "INR",
                CurrencySymbol: "₹",
                IsEarningEligible: true,
                IsWorkerEligible: true));

        public Task<WorkerEarningsLedgerResponse?> ProcessAttendanceEarningsAsync(
            EarningsActor actor,
            ProcessAttendanceEarningsRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<WorkerEarningsLedgerResponse?>(null);

        public Task<WorkerEarningsLedgerResponse> CreateEarningFromFinalizedAttendanceAsync(
            EarningsActor actor,
            FinalizedAttendanceRecord attendance,
            CancellationToken cancellationToken = default)
        {
            var grossAmount = attendance.WageType == WageType.HalfDay ? 250m : 500m;
            var response = new WorkerEarningsLedgerResponse(
                Id: Guid.NewGuid(),
                OrganizationId: attendance.OrganizationId,
                WorkerId: attendance.WorkerId,
                WorkerDisplayName: "Worker",
                AttendanceId: attendance.AttendanceId,
                EarningsDate: attendance.AttendanceDate,
                WageType: attendance.WageType.ToString(),
                Quantity: attendance.Quantity,
                WageRate: 500m,
                GrossAmount: grossAmount,
                CurrencyId: currencyId,
                CurrencyCode: "INR",
                CurrencySymbol: "₹",
                Status: "APPROVED",
                EntryType: "EARNING",
                ReferenceLedgerId: null,
                Description: attendance.Description,
                FinalizedAt: DateTimeOffset.UtcNow,
                FinalizedBy: actor.UserId,
                CreatedAt: DateTimeOffset.UtcNow,
                CreatedBy: actor.UserId,
                UpdatedAt: null,
                UpdatedBy: null);

            CreatedEarnings.Add(response);
            return Task.FromResult(response);
        }
    }

    private sealed class FakeAttendanceStore : IAttendanceStore
    {
        public List<Worker> Workers { get; } = [];
        public List<Farm> Farms { get; } = [];
        public List<LaborAttendance> Attendances { get; } = [];
        public List<LaborAttendanceOperationIdempotencyRecord> IdempotencyRecords { get; } = [];

        public Task<Farm?> FindFarmAsync(Guid farmId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Farms.FirstOrDefault(f => f.Id == farmId && f.OrganizationId == organizationId));

        public Task<int> CountEligibleWorkersAsync(Guid organizationId, Guid farmId, DateOnly attendanceDate, string? search, CancellationToken cancellationToken = default) =>
            Task.FromResult(Workers.Count(w => w.OrganizationId == organizationId));

        public Task<IReadOnlyList<AttendanceEligibleWorkerResponse>> ListEligibleWorkersAsync(Guid organizationId, Guid farmId, DateOnly attendanceDate, string? search, int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AttendanceEligibleWorkerResponse>>([]);

        public Task<LaborAttendance?> FindAttendanceByIdAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default)
        {
            var att = Attendances.FirstOrDefault(a => a.Id == id && a.OrganizationId == organizationId);
            if (att != null)
            {
                AttachNavigations(att);
            }
            return Task.FromResult(att);
        }

        public Task<LaborAttendance?> FindAttendanceByWorkerAndDateAsync(Guid organizationId, Guid workerId, DateOnly attendanceDate, CancellationToken cancellationToken = default)
        {
            var att = Attendances.FirstOrDefault(a => a.OrganizationId == organizationId && a.WorkerId == workerId && a.AttendanceDate == attendanceDate);
            if (att != null)
            {
                AttachNavigations(att);
            }
            return Task.FromResult(att);
        }

        public Task<IReadOnlyList<LaborAttendance>> ListDailyAttendanceAsync(Guid organizationId, Guid farmId, DateOnly attendanceDate, CancellationToken cancellationToken = default)
        {
            var list = Attendances.Where(a => a.OrganizationId == organizationId && a.FarmId == farmId && a.AttendanceDate == attendanceDate).ToList();
            foreach (var a in list)
            {
                AttachNavigations(a);
            }
            return Task.FromResult<IReadOnlyList<LaborAttendance>>(list);
        }

        public Task<IReadOnlyList<LaborAttendance>> ListDailyAttendanceTrackedAsync(Guid organizationId, Guid farmId, DateOnly attendanceDate, CancellationToken cancellationToken = default) =>
            ListDailyAttendanceAsync(organizationId, farmId, attendanceDate, cancellationToken);

        public Task<(IReadOnlyList<LaborAttendance> Items, int TotalCount)> ListAttendanceHistoryAsync(Guid organizationId, Guid? farmId, Guid? workerId, DateOnly? fromDate, DateOnly? toDate, AttendanceType? attendanceType, AttendanceStatus? status, string? search, string? sortBy, bool sortDescending, int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<LaborAttendance>, int)>(([], 0));

        public Task<Worker?> FindWorkerWithAssignmentAsync(Guid organizationId, Guid workerId, Guid farmId, DateOnly attendanceDate, CancellationToken cancellationToken = default) =>
            Task.FromResult(Workers.FirstOrDefault(w => w.Id == workerId && w.OrganizationId == organizationId && w.FarmAssignments.Any(a => a.FarmId == farmId && a.IsActive)));

        public Task<Worker?> FindWorkerWithDetailsAsync(Guid organizationId, Guid workerId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Workers.FirstOrDefault(w => w.Id == workerId && w.OrganizationId == organizationId));

        public Task<DateOnly?> FindPreviousAttendanceDateAsync(Guid organizationId, Guid farmId, DateOnly targetDate, CancellationToken cancellationToken = default)
        {
            var date = Attendances
                .Where(a => a.OrganizationId == organizationId && a.FarmId == farmId && a.AttendanceDate < targetDate)
                .Select(a => (DateOnly?)a.AttendanceDate)
                .OrderByDescending(d => d)
                .FirstOrDefault();
            return Task.FromResult(date);
        }

        public Task<WorkerEarningsLedger?> FindEarningsLedgerByAttendanceIdAsync(Guid organizationId, Guid attendanceId, CancellationToken cancellationToken = default) =>
            Task.FromResult<WorkerEarningsLedger?>(null);

        public void AddAttendance(LaborAttendance attendance) => Attendances.Add(attendance);
        public void RemoveAttendance(LaborAttendance attendance) => Attendances.Remove(attendance);

        public Task<LaborAttendanceOperationIdempotencyRecord?> FindIdempotencyRecordAsync(Guid organizationId, string operationType, string idempotencyKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(IdempotencyRecords.FirstOrDefault(r => r.OrganizationId == organizationId && r.OperationType == operationType && r.IdempotencyKey == idempotencyKey));

        public void AddIdempotencyRecord(LaborAttendanceOperationIdempotencyRecord record) =>
            IdempotencyRecords.Add(record);

        public Task AcquireAttendanceLockAsync(Guid farmId, DateOnly attendanceDate, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);

        public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) =>
            await operation(cancellationToken);

        private void AttachNavigations(LaborAttendance attendance)
        {
            if (attendance.Farm == null)
            {
                var farm = Farms.FirstOrDefault(f => f.Id == attendance.FarmId);
                if (farm != null)
                {
                    typeof(LaborAttendance).GetProperty(nameof(LaborAttendance.Farm))!.SetValue(attendance, farm);
                }
            }

            if (attendance.Worker == null)
            {
                var worker = Workers.FirstOrDefault(w => w.Id == attendance.WorkerId);
                if (worker != null)
                {
                    typeof(LaborAttendance).GetProperty(nameof(LaborAttendance.Worker))!.SetValue(attendance, worker);
                }
            }
        }
    }

    #endregion
}
