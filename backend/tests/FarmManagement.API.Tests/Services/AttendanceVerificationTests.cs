using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.Labor;
using FarmManagement.Application.Interfaces.Labor;
using FarmManagement.Application.Services;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Services;

public sealed class AttendanceVerificationTests
{
    private readonly Guid _organizationIdA = Guid.NewGuid();
    private readonly Guid _organizationIdB = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _ownershipTypeId = Guid.NewGuid();

    private AttendanceActor ActorOrgA => new(_userId, _organizationIdA);
    private AttendanceActor ActorOrgB => new(_userId, _organizationIdB);

    private Farm CreateFarm(Guid organizationId, string code = "F-01") =>
        new(organizationId, code, "Test Farm " + code, _ownershipTypeId, _userId);

    private Worker CreateWorker(Guid organizationId, string name = "Worker", Gender gender = Gender.Male) =>
        new(organizationId, name, "Patil", gender, EmploymentType.Permanent, _userId);

    private static WorkerFarmAssignment AssignWorkerToFarm(Worker worker, Farm farm, DateOnly from, DateOnly? to = null)
    {
        var assignment = new WorkerFarmAssignment(worker.OrganizationId, worker.Id, farm.Id, from, worker.CreatedBy, assignedTo: to);
        worker.FarmAssignments.Add(assignment);
        return assignment;
    }

    [Fact]
    public async Task Rule1_UserFromOrgA_CannotAccessOrgBAttendance()
    {
        var store = new FakeAttendanceStore();
        var farmB = CreateFarm(_organizationIdB);
        var workerB = CreateWorker(_organizationIdB);
        AssignWorkerToFarm(workerB, farmB, new DateOnly(2026, 1, 1));
        store.Farms.Add(farmB);
        store.Workers.Add(workerB);

        var attB = LaborAttendance.CreateDraft(_organizationIdB, farmB.Id, workerB.Id, new DateOnly(2026, 9, 12), AttendanceType.FullDay, _userId);
        store.Attendances.Add(attB);

        var service = new AttendanceService(store, new FakeAttendanceEarningsIntegration());

        // Org A user trying to access Org B farm attendance -> 404
        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            service.GetDailyAttendanceAsync(ActorOrgA, farmB.Id, new DateOnly(2026, 9, 12)));

        // Org A user trying to access Org B attendance record by ID -> 404
        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            service.GetAttendanceByIdAsync(ActorOrgA, attB.Id));
    }

    [Fact]
    public async Task Rule2_WorkerFromOrgA_CannotBePairedWithFarmB()
    {
        var store = new FakeAttendanceStore();
        var farmB = CreateFarm(_organizationIdB);
        var workerA = CreateWorker(_organizationIdA);
        store.Farms.Add(farmB);
        store.Workers.Add(workerA);

        var service = new AttendanceService(store, new FakeAttendanceEarningsIntegration());
        var request = new CreateDraftAttendanceRequest(farmB.Id, workerA.Id, new DateOnly(2026, 9, 12), "FULL_DAY");

        // Org A actor cannot see Farm B
        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            service.CreateDraftAsync(ActorOrgA, request));

        // Org B actor cannot see Worker A
        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateDraftAsync(ActorOrgB, request));
    }

    [Fact]
    public async Task Rule3_WorkerCannotBeAddedToFarmDate_WithoutValidAssignment()
    {
        var store = new FakeAttendanceStore();
        var farmA = CreateFarm(_organizationIdA);
        var workerA = CreateWorker(_organizationIdA);
        store.Farms.Add(farmA);
        store.Workers.Add(workerA);
        // Worker has NO farm assignment

        var service = new AttendanceService(store, new FakeAttendanceEarningsIntegration());
        var request = new CreateDraftAttendanceRequest(farmA.Id, workerA.Id, new DateOnly(2026, 9, 12), "FULL_DAY");

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateDraftAsync(ActorOrgA, request));
        Assert.NotNull(ex.Errors);
        Assert.True(ex.Errors.ContainsKey("workerId"));
    }

    [Fact]
    public async Task Rule4_DuplicateWorkerDateAttendance_IsRejected()
    {
        var store = new FakeAttendanceStore();
        var farmA = CreateFarm(_organizationIdA);
        var workerA = CreateWorker(_organizationIdA);
        AssignWorkerToFarm(workerA, farmA, new DateOnly(2026, 1, 1));
        store.Farms.Add(farmA);
        store.Workers.Add(workerA);

        var date = new DateOnly(2026, 9, 12);
        store.Attendances.Add(LaborAttendance.CreateDraft(_organizationIdA, farmA.Id, workerA.Id, date, AttendanceType.FullDay, _userId));

        var service = new AttendanceService(store, new FakeAttendanceEarningsIntegration());
        var request = new CreateDraftAttendanceRequest(farmA.Id, workerA.Id, date, "HALF_DAY");

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateDraftAsync(ActorOrgA, request));
        Assert.NotNull(ex.Errors);
        Assert.True(ex.Errors.ContainsKey("workerId"));
    }

    [Fact]
    public async Task Rule5_DuplicatePayrollEarnings_CannotBeCreatedByRepeatedFinalization()
    {
        var store = new FakeAttendanceStore();
        var earnings = new FakeAttendanceEarningsIntegration();
        var farmA = CreateFarm(_organizationIdA);
        var workerA = CreateWorker(_organizationIdA);
        var date = new DateOnly(2026, 9, 12);
        AssignWorkerToFarm(workerA, farmA, new DateOnly(2026, 1, 1));
        store.Farms.Add(farmA);
        store.Workers.Add(workerA);

        var draft = LaborAttendance.CreateDraft(_organizationIdA, farmA.Id, workerA.Id, date, AttendanceType.FullDay, _userId);
        store.Attendances.Add(draft);

        var service = new AttendanceService(store, earnings);

        // First finalization
        await service.FinalizeSingleAttendanceAsync(ActorOrgA, draft.Id);
        Assert.Equal(1, earnings.LedgerWriteCallCount);

        // Second finalization call -> rejected
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.FinalizeSingleAttendanceAsync(ActorOrgA, draft.Id));
        Assert.NotNull(ex.Errors);
        Assert.True(ex.Errors.ContainsKey("status"));
        Assert.Equal(1, earnings.LedgerWriteCallCount); // No duplicate earning written
    }

    [Fact]
    public async Task Rule6_FinalizedAttendance_CannotBeEditedNormally()
    {
        var store = new FakeAttendanceStore();
        var farmA = CreateFarm(_organizationIdA);
        var workerA = CreateWorker(_organizationIdA);
        var date = new DateOnly(2026, 9, 12);
        AssignWorkerToFarm(workerA, farmA, new DateOnly(2026, 1, 1));
        store.Farms.Add(farmA);
        store.Workers.Add(workerA);

        var finalized = LaborAttendance.CreateFinalized(_organizationIdA, farmA.Id, workerA.Id, date, AttendanceType.FullDay, _userId);
        store.Attendances.Add(finalized);

        var service = new AttendanceService(store, new FakeAttendanceEarningsIntegration());

        // Attempt update -> rejected
        await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpdateDraftAsync(ActorOrgA, finalized.Id, new UpdateDraftAttendanceRequest("HALF_DAY")));

        // Attempt delete -> rejected
        await Assert.ThrowsAsync<ValidationException>(() =>
            service.DeleteDraftAsync(ActorOrgA, finalized.Id));
    }

    [Fact]
    public async Task Rule7_WorkerWithNoAttendance_IsNotTreatedAsAutomaticAbsence()
    {
        var store = new FakeAttendanceStore();
        var farmA = CreateFarm(_organizationIdA);
        var workerA = CreateWorker(_organizationIdA);
        AssignWorkerToFarm(workerA, farmA, new DateOnly(2026, 1, 1));
        store.Farms.Add(farmA);
        store.Workers.Add(workerA);

        var service = new AttendanceService(store, new FakeAttendanceEarningsIntegration());
        var daily = await service.GetDailyAttendanceAsync(ActorOrgA, farmA.Id, new DateOnly(2026, 9, 12));

        Assert.Equal(0, daily.Summary.TotalCount);
        Assert.Empty(daily.Records);
    }

    [Fact]
    public async Task Rule8_NotWorked_DoesNotGenerateEarnings()
    {
        var store = new FakeAttendanceStore();
        var earnings = new FakeAttendanceEarningsIntegration();
        var farmA = CreateFarm(_organizationIdA);
        var workerA = CreateWorker(_organizationIdA);
        var date = new DateOnly(2026, 9, 12);
        AssignWorkerToFarm(workerA, farmA, new DateOnly(2026, 1, 1));
        store.Farms.Add(farmA);
        store.Workers.Add(workerA);

        var draft = LaborAttendance.CreateDraft(_organizationIdA, farmA.Id, workerA.Id, date, AttendanceType.NotWorked, _userId);
        store.Attendances.Add(draft);

        var service = new AttendanceService(store, earnings);
        var result = await service.FinalizeAttendanceAsync(ActorOrgA, new FinalizeAttendanceRequest(farmA.Id, date));

        Assert.Equal(0m, result.TotalEarnings);
        Assert.Equal(1, result.NotWorkedCount);
        Assert.Equal(0, earnings.LedgerWriteCallCount);
    }

    [Fact]
    public async Task Rule9_FullDay_UsesFullDayWageRate()
    {
        var store = new FakeAttendanceStore();
        var earnings = new FakeAttendanceEarningsIntegration { FullDayRate = 500m, HalfDayRate = 275m };
        var farmA = CreateFarm(_organizationIdA);
        var workerA = CreateWorker(_organizationIdA);
        var date = new DateOnly(2026, 9, 12);
        AssignWorkerToFarm(workerA, farmA, new DateOnly(2026, 1, 1));
        store.Farms.Add(farmA);
        store.Workers.Add(workerA);

        var draft = LaborAttendance.CreateDraft(_organizationIdA, farmA.Id, workerA.Id, date, AttendanceType.FullDay, _userId);
        store.Attendances.Add(draft);

        var service = new AttendanceService(store, earnings);
        var result = await service.FinalizeAttendanceAsync(ActorOrgA, new FinalizeAttendanceRequest(farmA.Id, date));

        Assert.Equal(500m, result.TotalEarnings);
        Assert.Single(earnings.CreatedEarnings);
        Assert.Equal(500m, earnings.CreatedEarnings[0].WageRate);
        Assert.Equal("FULL_DAY", earnings.CreatedEarnings[0].WageType);
    }

    [Fact]
    public async Task Rule10_HalfDay_UsesHalfDayWageRate_Not50PercentCalculation()
    {
        var store = new FakeAttendanceStore();
        var earnings = new FakeAttendanceEarningsIntegration { FullDayRate = 500m, HalfDayRate = 275m }; // 275 is NOT 50% of 500
        var farmA = CreateFarm(_organizationIdA);
        var workerA = CreateWorker(_organizationIdA);
        var date = new DateOnly(2026, 9, 12);
        AssignWorkerToFarm(workerA, farmA, new DateOnly(2026, 1, 1));
        store.Farms.Add(farmA);
        store.Workers.Add(workerA);

        var draft = LaborAttendance.CreateDraft(_organizationIdA, farmA.Id, workerA.Id, date, AttendanceType.HalfDay, _userId);
        store.Attendances.Add(draft);

        var service = new AttendanceService(store, earnings);
        var result = await service.FinalizeAttendanceAsync(ActorOrgA, new FinalizeAttendanceRequest(farmA.Id, date));

        Assert.Equal(275m, result.TotalEarnings);
        Assert.Single(earnings.CreatedEarnings);
        Assert.Equal(275m, earnings.CreatedEarnings[0].WageRate);
        Assert.Equal("HALF_DAY", earnings.CreatedEarnings[0].WageType);
    }

    [Fact]
    public async Task Rule11_Hourly_UsesHourlyRateMultipliedByHours()
    {
        var store = new FakeAttendanceStore();
        var earnings = new FakeAttendanceEarningsIntegration { HourlyRate = 80m };
        var farmA = CreateFarm(_organizationIdA);
        var workerA = CreateWorker(_organizationIdA);
        var date = new DateOnly(2026, 9, 12);
        AssignWorkerToFarm(workerA, farmA, new DateOnly(2026, 1, 1));
        store.Farms.Add(farmA);
        store.Workers.Add(workerA);

        var draft = LaborAttendance.CreateDraft(_organizationIdA, farmA.Id, workerA.Id, date, AttendanceType.Hourly, _userId, workingHours: 6.5m);
        store.Attendances.Add(draft);

        var service = new AttendanceService(store, earnings);
        var result = await service.FinalizeAttendanceAsync(ActorOrgA, new FinalizeAttendanceRequest(farmA.Id, date));

        Assert.Equal(520m, result.TotalEarnings); // 6.5 * 80 = 520
        Assert.Single(earnings.CreatedEarnings);
        Assert.Equal(6.5m, earnings.CreatedEarnings[0].Quantity);
        Assert.Equal(80m, earnings.CreatedEarnings[0].WageRate);
    }

    [Fact]
    public void Rule12_NoOvertime_IsSupportedInDomain()
    {
        var properties = typeof(LaborAttendance).GetProperties();
        Assert.DoesNotContain(properties, p => p.Name.Contains("Overtime", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Rule15_LaborActivity_IsNeverInvoked()
    {
        var ctors = typeof(AttendanceService).GetConstructors();
        foreach (var ctor in ctors)
        {
            var paramTypes = ctor.GetParameters().Select(p => p.ParameterType);
            Assert.DoesNotContain(paramTypes, t => t.Name.Contains("LaborActivity", StringComparison.OrdinalIgnoreCase));
        }
    }

    private sealed class FakeAttendanceEarningsIntegration : IAttendanceEarningsIntegration
    {
        public decimal FullDayRate { get; set; } = 500m;
        public decimal HalfDayRate { get; set; } = 250m;
        public decimal HourlyRate { get; set; } = 75m;
        public Guid CurrencyId { get; set; } = Guid.NewGuid();
        public int LedgerWriteCallCount { get; private set; }
        public List<WorkerEarningsLedgerResponse> CreatedEarnings { get; } = [];

        public Task<AttendanceEarningsCalculationResult> CalculateAttendanceEarningsAsync(
            EarningsActor actor, CalculateAttendanceEarningsRequest request, CancellationToken cancellationToken = default)
        {
            var norm = request.AttendanceType.Trim().ToUpperInvariant();
            if (norm is "NOT_WORKED" or "NOTWORKED")
            {
                return Task.FromResult(new AttendanceEarningsCalculationResult(
                    request.WorkerId, "Worker", Gender.Male, "NOT_WORKED", null, request.Quantity, 0m, 0m, null, "INR", "₹", false, true));
            }

            var rate = norm switch
            {
                "FULL_DAY" or "FULLDAY" => FullDayRate,
                "HALF_DAY" or "HALFDAY" => HalfDayRate,
                "HOURLY" => HourlyRate,
                _ => 100m
            };

            var amount = Math.Round(rate * request.Quantity, 2);
            return Task.FromResult(new AttendanceEarningsCalculationResult(
                request.WorkerId, "Worker", Gender.Male, norm, norm, request.Quantity, rate, amount, CurrencyId, "INR", "₹", true, true));
        }

        public Task<WorkerEarningsLedgerResponse?> ProcessAttendanceEarningsAsync(
            EarningsActor actor, ProcessAttendanceEarningsRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<WorkerEarningsLedgerResponse> CreateEarningFromFinalizedAttendanceAsync(
            EarningsActor actor, FinalizedAttendanceRecord attendance, CancellationToken cancellationToken = default)
        {
            LedgerWriteCallCount++;
            var rate = attendance.WageType switch
            {
                WageType.FullDay => FullDayRate,
                WageType.HalfDay => HalfDayRate,
                WageType.Hourly => HourlyRate,
                _ => 100m
            };
            var amount = Math.Round(rate * attendance.Quantity, 2);
            var wageTypeStr = attendance.WageType switch
            {
                WageType.FullDay => "FULL_DAY",
                WageType.HalfDay => "HALF_DAY",
                WageType.Hourly => "HOURLY",
                _ => attendance.WageType.ToString().ToUpperInvariant()
            };
            var response = new WorkerEarningsLedgerResponse(
                Guid.NewGuid(), actor.OrganizationId, attendance.WorkerId, "Worker", attendance.AttendanceId,
                attendance.AttendanceDate, wageTypeStr, attendance.Quantity,
                rate, CurrencyId, "INR", "₹", amount, "EARNING", "APPROVED", null, attendance.Description,
                DateTimeOffset.UtcNow, actor.UserId, DateTimeOffset.UtcNow, actor.UserId, null, null);

            CreatedEarnings.Add(response);
            return Task.FromResult(response);
        }
    }

    private sealed class FakeAttendanceStore : IAttendanceStore
    {
        public List<Worker> Workers { get; } = [];
        public List<Farm> Farms { get; } = [];
        public List<LaborAttendance> Attendances { get; } = [];

        public Task<Farm?> FindFarmAsync(Guid farmId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Farms.FirstOrDefault(f => f.Id == farmId && f.OrganizationId == organizationId));

        public Task<int> CountEligibleWorkersAsync(Guid organizationId, Guid farmId, DateOnly attendanceDate, string? search, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<IReadOnlyList<AttendanceEligibleWorkerResponse>> ListEligibleWorkersAsync(Guid organizationId, Guid farmId, DateOnly attendanceDate, string? search, int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AttendanceEligibleWorkerResponse>>([]);

        public Task<LaborAttendance?> FindAttendanceByIdAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Attendances.FirstOrDefault(a => a.Id == id && a.OrganizationId == organizationId));

        public Task<LaborAttendance?> FindAttendanceByWorkerAndDateAsync(Guid organizationId, Guid workerId, DateOnly attendanceDate, CancellationToken cancellationToken = default) =>
            Task.FromResult(Attendances.FirstOrDefault(a => a.OrganizationId == organizationId && a.WorkerId == workerId && a.AttendanceDate == attendanceDate));

        public Task<IReadOnlyList<LaborAttendance>> ListDailyAttendanceAsync(Guid organizationId, Guid farmId, DateOnly attendanceDate, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<LaborAttendance>>(Attendances.Where(a => a.OrganizationId == organizationId && a.FarmId == farmId && a.AttendanceDate == attendanceDate).ToList());

        public Task<IReadOnlyList<LaborAttendance>> ListDailyAttendanceTrackedAsync(Guid organizationId, Guid farmId, DateOnly attendanceDate, CancellationToken cancellationToken = default) =>
            ListDailyAttendanceAsync(organizationId, farmId, attendanceDate, cancellationToken);

        public Task<(IReadOnlyList<LaborAttendance> Items, int TotalCount)> ListAttendanceHistoryAsync(Guid organizationId, Guid? farmId, Guid? workerId, DateOnly? fromDate, DateOnly? toDate, AttendanceType? attendanceType, AttendanceStatus? status, string? search, string? sortBy, bool sortDescending, int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<LaborAttendance>, int)>(([], 0));

        public Task<Worker?> FindWorkerWithAssignmentAsync(Guid organizationId, Guid workerId, Guid farmId, DateOnly attendanceDate, CancellationToken cancellationToken = default) =>
            Task.FromResult(Workers.FirstOrDefault(w => w.Id == workerId && w.OrganizationId == organizationId && w.FarmAssignments.Any(a => a.OrganizationId == organizationId && a.FarmId == farmId && a.IsActive)));

        public Task<Worker?> FindWorkerWithDetailsAsync(Guid organizationId, Guid workerId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Workers.FirstOrDefault(w => w.Id == workerId && w.OrganizationId == organizationId));

        public Task<DateOnly?> FindPreviousAttendanceDateAsync(Guid organizationId, Guid farmId, DateOnly targetDate, CancellationToken cancellationToken = default) =>
            Task.FromResult<DateOnly?>(null);

        public Task<WorkerEarningsLedger?> FindEarningsLedgerByAttendanceIdAsync(Guid organizationId, Guid attendanceId, CancellationToken cancellationToken = default) =>
            Task.FromResult<WorkerEarningsLedger?>(null);

        public void AddAttendance(LaborAttendance attendance) => Attendances.Add(attendance);
        public void RemoveAttendance(LaborAttendance attendance) => Attendances.Remove(attendance);
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
        public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) =>
            await operation(cancellationToken);
    }
}
