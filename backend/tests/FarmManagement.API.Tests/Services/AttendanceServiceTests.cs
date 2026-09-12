using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.Labor;
using FarmManagement.Application.Interfaces.Labor;
using FarmManagement.Application.Services;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Services;

public sealed class AttendanceServiceTests
{
    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly Guid _otherOrgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _ownershipTypeId = Guid.NewGuid();

    private AttendanceActor CreateActor(Guid? organizationId = null, Guid? userId = null) =>
        new(userId ?? _userId, organizationId ?? _organizationId);

    private AttendanceService CreateService(
        FakeAttendanceStore store,
        FakeAttendanceEarningsIntegration? earnings = null) =>
        new(store, earnings ?? new FakeAttendanceEarningsIntegration());

    private Farm CreateFarm(Guid? organizationId = null, bool isActive = true, string code = "F-01")
    {
        var farm = new Farm(
            organizationId ?? _organizationId,
            code,
            "Green Valley Farm",
            _ownershipTypeId,
            _userId);

        if (!isActive)
        {
            farm.Deactivate(DateTimeOffset.UtcNow, _userId);
        }

        return farm;
    }

    private Worker CreateWorker(
        Guid? organizationId = null,
        string firstName = "Ramesh",
        string? lastName = "Patil",
        string? displayName = null,
        string? mobileNumber = "9876543210",
        Gender gender = Gender.Male,
        EmploymentType employmentType = EmploymentType.Permanent,
        DateOnly? joiningDate = null,
        DateOnly? leavingDate = null,
        bool isActive = true)
    {
        var worker = new Worker(
            organizationId ?? _organizationId,
            firstName,
            lastName,
            gender,
            employmentType,
            _userId,
            displayName: displayName,
            mobileNumber: mobileNumber,
            joiningDate: joiningDate,
            leavingDate: leavingDate);

        if (!isActive)
        {
            worker.Deactivate(DateTimeOffset.UtcNow, _userId);
        }

        return worker;
    }

    private static WorkerFarmAssignment AssignWorkerToFarm(
        Worker worker,
        Farm farm,
        DateOnly assignedFrom,
        DateOnly? assignedTo = null,
        bool isActive = true)
    {
        var assignment = new WorkerFarmAssignment(
            worker.OrganizationId,
            worker.Id,
            farm.Id,
            assignedFrom,
            worker.CreatedBy,
            assignedTo: assignedTo);

        if (!isActive)
        {
            assignment.Deactivate(DateTimeOffset.UtcNow, worker.CreatedBy);
        }

        worker.FarmAssignments.Add(assignment);
        return assignment;
    }

    [Fact]
    public async Task GetEligibleWorkersAsync_WhenFarmNotFound_ThrowsResourceNotFoundException()
    {
        var store = new FakeAttendanceStore();
        var service = CreateService(store);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            service.GetEligibleWorkersAsync(CreateActor(), Guid.NewGuid(), new DateOnly(2026, 9, 12)));
    }

    [Fact]
    public async Task GetEligibleWorkersAsync_WhenFarmFromDifferentOrg_ThrowsResourceNotFoundException()
    {
        var store = new FakeAttendanceStore();
        var farm = CreateFarm(organizationId: _otherOrgId);
        store.Farms.Add(farm);

        var service = CreateService(store);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            service.GetEligibleWorkersAsync(CreateActor(), farm.Id, new DateOnly(2026, 9, 12)));
    }

    [Fact]
    public async Task GetEligibleWorkersAsync_WhenFarmInactive_ThrowsValidationException()
    {
        var store = new FakeAttendanceStore();
        var farm = CreateFarm(isActive: false);
        store.Farms.Add(farm);

        var service = CreateService(store);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.GetEligibleWorkersAsync(CreateActor(), farm.Id, new DateOnly(2026, 9, 12)));

        Assert.NotNull(ex.Errors);
        Assert.True(ex.Errors.ContainsKey("farmId"));
    }

    [Fact]
    public async Task GetEligibleWorkersAsync_WhenActorInvalid_ThrowsUnauthorizedAccessException()
    {
        var store = new FakeAttendanceStore();
        var service = CreateService(store);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.GetEligibleWorkersAsync(new AttendanceActor(Guid.Empty, _organizationId), Guid.NewGuid(), new DateOnly(2026, 9, 12)));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.GetEligibleWorkersAsync(new AttendanceActor(_userId, Guid.Empty), Guid.NewGuid(), new DateOnly(2026, 9, 12)));
    }

    [Fact]
    public async Task GetEligibleWorkersAsync_WhenFarmIdEmpty_ThrowsValidationException()
    {
        var store = new FakeAttendanceStore();
        var service = CreateService(store);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.GetEligibleWorkersAsync(CreateActor(), Guid.Empty, new DateOnly(2026, 9, 12)));

        Assert.NotNull(ex.Errors);
        Assert.True(ex.Errors.ContainsKey("farmId"));
    }

    [Fact]
    public async Task GetEligibleWorkersAsync_WhenAttendanceDateDefault_ThrowsValidationException()
    {
        var store = new FakeAttendanceStore();
        var farm = CreateFarm();
        store.Farms.Add(farm);

        var service = CreateService(store);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.GetEligibleWorkersAsync(CreateActor(), farm.Id, default));

        Assert.NotNull(ex.Errors);
        Assert.True(ex.Errors.ContainsKey("attendanceDate"));
    }

    [Fact]
    public async Task GetEligibleWorkersAsync_WhenPageLessThanOne_ThrowsValidationException()
    {
        var store = new FakeAttendanceStore();
        var farm = CreateFarm();
        store.Farms.Add(farm);

        var service = CreateService(store);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.GetEligibleWorkersAsync(CreateActor(), farm.Id, new DateOnly(2026, 9, 12), page: 0));

        Assert.NotNull(ex.Errors);
        Assert.True(ex.Errors.ContainsKey("page"));
    }

    [Fact]
    public async Task GetEligibleWorkersAsync_ReturnsEligibleWorkers_AndCorrectFieldMappings()
    {
        var store = new FakeAttendanceStore();
        var farm = CreateFarm();
        store.Farms.Add(farm);

        var attendanceDate = new DateOnly(2026, 9, 12);

        var worker = CreateWorker(
            firstName: "Sita",
            lastName: "Devi",
            displayName: "Sita Devi",
            gender: Gender.Female,
            employmentType: EmploymentType.DailyWage,
            mobileNumber: "9876500001",
            joiningDate: new DateOnly(2026, 1, 1));

        var assignment = AssignWorkerToFarm(
            worker,
            farm,
            assignedFrom: new DateOnly(2026, 6, 1),
            assignedTo: new DateOnly(2026, 12, 31));

        store.Workers.Add(worker);

        var service = CreateService(store);
        var result = await service.GetEligibleWorkersAsync(CreateActor(), farm.Id, attendanceDate);

        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);

        var item = result.Items[0];
        Assert.Equal(worker.Id, item.WorkerId);
        Assert.Equal("Sita Devi", item.DisplayName);
        Assert.Equal("Sita", item.FirstName);
        Assert.Equal("Devi", item.LastName);
        Assert.Equal("FEMALE", item.Gender);
        Assert.Equal("DAILY_WAGE", item.EmploymentType);
        Assert.Equal("9876500001", item.MobileNumber);
        Assert.Equal(assignment.Id, item.AssignmentId);
        Assert.Equal(farm.Id, item.FarmId);
        Assert.Equal(new DateOnly(2026, 6, 1), item.AssignedFrom);
        Assert.Equal(new DateOnly(2026, 12, 31), item.AssignedTo);
    }

    [Fact]
    public async Task GetEligibleWorkersAsync_ExcludesIneligibleWorkers()
    {
        var store = new FakeAttendanceStore();
        var farm = CreateFarm();
        var otherFarm = CreateFarm(code: "F-02");
        store.Farms.Add(farm);
        store.Farms.Add(otherFarm);

        var attendanceDate = new DateOnly(2026, 9, 12);

        // 1. Eligible worker
        var eligibleWorker = CreateWorker(firstName: "Eligible", displayName: "Eligible Worker");
        AssignWorkerToFarm(eligibleWorker, farm, new DateOnly(2026, 1, 1));
        store.Workers.Add(eligibleWorker);

        // 2. Inactive worker
        var inactiveWorker = CreateWorker(firstName: "Inactive", displayName: "Inactive Worker", isActive: false);
        AssignWorkerToFarm(inactiveWorker, farm, new DateOnly(2026, 1, 1));
        store.Workers.Add(inactiveWorker);

        // 3. Worker joined after attendance date
        var futureJoinedWorker = CreateWorker(
            firstName: "Future",
            displayName: "Future Worker",
            joiningDate: new DateOnly(2026, 10, 1));
        AssignWorkerToFarm(futureJoinedWorker, farm, new DateOnly(2026, 1, 1));
        store.Workers.Add(futureJoinedWorker);

        // 4. Worker left before attendance date
        var pastLeftWorker = CreateWorker(
            firstName: "Past",
            displayName: "Past Worker",
            leavingDate: new DateOnly(2026, 8, 31));
        AssignWorkerToFarm(pastLeftWorker, farm, new DateOnly(2026, 1, 1));
        store.Workers.Add(pastLeftWorker);

        // 5. Worker assigned to a different farm
        var otherFarmWorker = CreateWorker(firstName: "OtherFarm", displayName: "OtherFarm Worker");
        AssignWorkerToFarm(otherFarmWorker, otherFarm, new DateOnly(2026, 1, 1));
        store.Workers.Add(otherFarmWorker);

        // 6. Worker whose farm assignment expired before attendance date
        var expiredAssignmentWorker = CreateWorker(firstName: "Expired", displayName: "Expired Assignment Worker");
        AssignWorkerToFarm(
            expiredAssignmentWorker,
            farm,
            assignedFrom: new DateOnly(2026, 1, 1),
            assignedTo: new DateOnly(2026, 8, 31));
        store.Workers.Add(expiredAssignmentWorker);

        // 7. Worker whose farm assignment starts in the future
        var futureAssignmentWorker = CreateWorker(firstName: "FutureAssign", displayName: "Future Assign Worker");
        AssignWorkerToFarm(
            futureAssignmentWorker,
            farm,
            assignedFrom: new DateOnly(2026, 10, 1));
        store.Workers.Add(futureAssignmentWorker);

        // 8. Worker with deactivated farm assignment
        var deactivatedAssignmentWorker = CreateWorker(firstName: "DeactivatedAssign", displayName: "Deactivated Assign Worker");
        AssignWorkerToFarm(
            deactivatedAssignmentWorker,
            farm,
            assignedFrom: new DateOnly(2026, 1, 1),
            isActive: false);
        store.Workers.Add(deactivatedAssignmentWorker);

        // 9. Worker belonging to a different organization
        var otherOrgWorker = CreateWorker(
            organizationId: _otherOrgId,
            firstName: "OtherOrg",
            displayName: "OtherOrg Worker");
        AssignWorkerToFarm(otherOrgWorker, farm, new DateOnly(2026, 1, 1));
        store.Workers.Add(otherOrgWorker);

        var service = CreateService(store);
        var result = await service.GetEligibleWorkersAsync(CreateActor(), farm.Id, attendanceDate);

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal(eligibleWorker.Id, result.Items[0].WorkerId);
    }

    [Fact]
    public async Task GetEligibleWorkersAsync_WithSearch_FiltersCorrectly()
    {
        var store = new FakeAttendanceStore();
        var farm = CreateFarm();
        store.Farms.Add(farm);

        var attendanceDate = new DateOnly(2026, 9, 12);

        var worker1 = CreateWorker(firstName: "Ramesh", lastName: "Patil", displayName: "Ramesh Patil", mobileNumber: "9800000001");
        var worker2 = CreateWorker(firstName: "Suresh", lastName: "Shinde", displayName: "Suresh Shinde", mobileNumber: "9800000002");
        var worker3 = CreateWorker(firstName: "Sunita", lastName: "Kulkarni", displayName: "Sunita Kulkarni", mobileNumber: "9900000003");

        AssignWorkerToFarm(worker1, farm, new DateOnly(2026, 1, 1));
        AssignWorkerToFarm(worker2, farm, new DateOnly(2026, 1, 1));
        AssignWorkerToFarm(worker3, farm, new DateOnly(2026, 1, 1));

        store.Workers.Add(worker1);
        store.Workers.Add(worker2);
        store.Workers.Add(worker3);

        var service = CreateService(store);

        // Search by first name / display name
        var result1 = await service.GetEligibleWorkersAsync(CreateActor(), farm.Id, attendanceDate, search: "ramesh");
        Assert.Equal(1, result1.TotalCount);
        Assert.Equal(worker1.Id, result1.Items[0].WorkerId);

        // Search by last name
        var result2 = await service.GetEligibleWorkersAsync(CreateActor(), farm.Id, attendanceDate, search: "kulkarni");
        Assert.Equal(1, result2.TotalCount);
        Assert.Equal(worker3.Id, result2.Items[0].WorkerId);

        // Search by mobile
        var result3 = await service.GetEligibleWorkersAsync(CreateActor(), farm.Id, attendanceDate, search: "9800000002");
        Assert.Equal(1, result3.TotalCount);
        Assert.Equal(worker2.Id, result3.Items[0].WorkerId);

        // Non-matching search
        var result4 = await service.GetEligibleWorkersAsync(CreateActor(), farm.Id, attendanceDate, search: "nonexistent");
        Assert.Equal(0, result4.TotalCount);
        Assert.Empty(result4.Items);
    }

    [Fact]
    public async Task GetEligibleWorkersAsync_WithPagination_ReturnsPagedResults()
    {
        var store = new FakeAttendanceStore();
        var farm = CreateFarm();
        store.Farms.Add(farm);

        var attendanceDate = new DateOnly(2026, 9, 12);

        for (var i = 1; i <= 5; i++)
        {
            var worker = CreateWorker(
                firstName: $"Worker{i:D2}",
                displayName: $"Worker {i:D2}");
            AssignWorkerToFarm(worker, farm, new DateOnly(2026, 1, 1));
            store.Workers.Add(worker);
        }

        var service = CreateService(store);

        // Page 1 with pageSize 2
        var page1 = await service.GetEligibleWorkersAsync(CreateActor(), farm.Id, attendanceDate, page: 1, pageSize: 2);
        Assert.Equal(5, page1.TotalCount);
        Assert.Equal(3, page1.TotalPages);
        Assert.Equal(2, page1.Items.Count);
        Assert.Equal("Worker 01", page1.Items[0].DisplayName);
        Assert.Equal("Worker 02", page1.Items[1].DisplayName);

        // Page 2 with pageSize 2
        var page2 = await service.GetEligibleWorkersAsync(CreateActor(), farm.Id, attendanceDate, page: 2, pageSize: 2);
        Assert.Equal(5, page2.TotalCount);
        Assert.Equal(2, page2.Items.Count);
        Assert.Equal("Worker 03", page2.Items[0].DisplayName);
        Assert.Equal("Worker 04", page2.Items[1].DisplayName);

        // Page 3 with pageSize 2
        var page3 = await service.GetEligibleWorkersAsync(CreateActor(), farm.Id, attendanceDate, page: 3, pageSize: 2);
        Assert.Equal(5, page3.TotalCount);
        Assert.Single(page3.Items);
        Assert.Equal("Worker 05", page3.Items[0].DisplayName);
    }

    [Fact]
    public async Task GetEligibleWorkersAsync_NeverCreatesAttendanceOrEarnings()
    {
        var store = new FakeAttendanceStore();
        var farm = CreateFarm();
        store.Farms.Add(farm);

        var worker = CreateWorker();
        AssignWorkerToFarm(worker, farm, new DateOnly(2026, 1, 1));
        store.Workers.Add(worker);

        var service = CreateService(store);
        var result = await service.GetEligibleWorkersAsync(CreateActor(), farm.Id, new DateOnly(2026, 9, 12));

        Assert.Single(result.Items);
    }

    #region Daily Attendance Tests

    [Fact]
    public async Task GetDailyAttendanceAsync_WhenFarmNotFound_ThrowsResourceNotFoundException()
    {
        var store = new FakeAttendanceStore();
        var service = CreateService(store);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            service.GetDailyAttendanceAsync(CreateActor(), Guid.NewGuid(), new DateOnly(2026, 9, 12)));
    }

    [Fact]
    public async Task GetDailyAttendanceAsync_WhenFarmFromDifferentOrg_ThrowsResourceNotFoundException()
    {
        var store = new FakeAttendanceStore();
        var farm = CreateFarm(organizationId: _otherOrgId);
        store.Farms.Add(farm);

        var service = CreateService(store);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            service.GetDailyAttendanceAsync(CreateActor(), farm.Id, new DateOnly(2026, 9, 12)));
    }

    [Fact]
    public async Task GetDailyAttendanceAsync_WhenFarmInactive_ThrowsValidationException()
    {
        var store = new FakeAttendanceStore();
        var farm = CreateFarm(isActive: false);
        store.Farms.Add(farm);

        var service = CreateService(store);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.GetDailyAttendanceAsync(CreateActor(), farm.Id, new DateOnly(2026, 9, 12)));

        Assert.NotNull(ex.Errors);
        Assert.True(ex.Errors.ContainsKey("farmId"));
    }

    [Fact]
    public async Task GetDailyAttendanceAsync_WhenEmptyRoster_ReturnsZeroSummaryAndEmptyRecords()
    {
        var store = new FakeAttendanceStore();
        var farm = CreateFarm();
        store.Farms.Add(farm);

        var service = CreateService(store);
        var result = await service.GetDailyAttendanceAsync(CreateActor(), farm.Id, new DateOnly(2026, 9, 12));

        Assert.Equal(farm.Id, result.FarmId);
        Assert.Equal(new DateOnly(2026, 9, 12), result.AttendanceDate);
        Assert.Equal(0, result.Summary.TotalCount);
        Assert.Equal(0, result.Summary.WorkedCount);
        Assert.Equal(0, result.Summary.FullDayCount);
        Assert.Equal(0, result.Summary.HalfDayCount);
        Assert.Equal(0, result.Summary.HourlyCount);
        Assert.Equal(0, result.Summary.NotWorkedCount);
        Assert.Equal(0m, result.Summary.EstimatedEarnings);
        Assert.Equal("DRAFT", result.Summary.Status);
        Assert.Empty(result.Records);
    }

    [Fact]
    public async Task GetDailyAttendanceAsync_WhenRecordsPresent_ReturnsSummaryAndRecords()
    {
        var store = new FakeAttendanceStore();
        var farm = CreateFarm();
        store.Farms.Add(farm);

        var date = new DateOnly(2026, 9, 12);
        var w1 = CreateWorker(firstName: "Worker1");
        var w2 = CreateWorker(firstName: "Worker2");
        var w3 = CreateWorker(firstName: "Worker3");
        var w4 = CreateWorker(firstName: "Worker4");

        store.Workers.AddRange([w1, w2, w3, w4]);

        var att1 = LaborAttendance.CreateDraft(_organizationId, farm.Id, w1.Id, date, AttendanceType.FullDay, _userId, null, 500m, 500m);
        var att2 = LaborAttendance.CreateDraft(_organizationId, farm.Id, w2.Id, date, AttendanceType.HalfDay, _userId, null, 250m, 250m);
        var att3 = LaborAttendance.CreateDraft(_organizationId, farm.Id, w3.Id, date, AttendanceType.Hourly, _userId, 6m, 75m, 450m);
        var att4 = LaborAttendance.CreateDraft(_organizationId, farm.Id, w4.Id, date, AttendanceType.NotWorked, _userId, null, null, 0m);

        store.Attendances.AddRange([att1, att2, att3, att4]);

        var service = CreateService(store);
        var result = await service.GetDailyAttendanceAsync(CreateActor(), farm.Id, date);

        Assert.Equal(4, result.Summary.TotalCount);
        Assert.Equal(3, result.Summary.WorkedCount);
        Assert.Equal(1, result.Summary.FullDayCount);
        Assert.Equal(1, result.Summary.HalfDayCount);
        Assert.Equal(1, result.Summary.HourlyCount);
        Assert.Equal(1, result.Summary.NotWorkedCount);
        Assert.Equal(1200m, result.Summary.EstimatedEarnings);
        Assert.Equal("DRAFT", result.Summary.Status);
        Assert.Equal(4, result.Records.Count);
    }

    [Fact]
    public async Task GetDailyAttendanceAsync_WhenAllFinalized_ReturnsFinalizedStatus()
    {
        var store = new FakeAttendanceStore();
        var farm = CreateFarm();
        store.Farms.Add(farm);

        var date = new DateOnly(2026, 9, 12);
        var w1 = CreateWorker(firstName: "Worker1");
        store.Workers.Add(w1);

        var att1 = LaborAttendance.CreateFinalized(_organizationId, farm.Id, w1.Id, date, AttendanceType.FullDay, _userId, null, 500m, 500m);
        store.Attendances.Add(att1);

        var service = CreateService(store);
        var result = await service.GetDailyAttendanceAsync(CreateActor(), farm.Id, date);

        Assert.Equal("FINALIZED", result.Summary.Status);
    }

    #endregion

    #region Create Draft Tests

    [Fact]
    public async Task CreateDraftAsync_WhenWorkerLacksActiveAssignment_ThrowsValidationException()
    {
        var store = new FakeAttendanceStore();
        var farm = CreateFarm();
        store.Farms.Add(farm);

        var date = new DateOnly(2026, 9, 12);
        var worker = CreateWorker();
        store.Workers.Add(worker); // No farm assignment

        var service = CreateService(store);
        var req = new CreateDraftAttendanceRequest(farm.Id, worker.Id, date, "FULL_DAY");

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateDraftAsync(CreateActor(), req));

        Assert.NotNull(ex.Errors);
        Assert.True(ex.Errors.ContainsKey("workerId"));
    }

    [Fact]
    public async Task CreateDraftAsync_WhenDuplicateAttendanceForWorkerOnDate_ThrowsValidationException()
    {
        var store = new FakeAttendanceStore();
        var farm = CreateFarm();
        store.Farms.Add(farm);

        var date = new DateOnly(2026, 9, 12);
        var worker = CreateWorker();
        AssignWorkerToFarm(worker, farm, new DateOnly(2026, 1, 1));
        store.Workers.Add(worker);

        var existing = LaborAttendance.CreateDraft(_organizationId, farm.Id, worker.Id, date, AttendanceType.FullDay, _userId);
        store.Attendances.Add(existing);

        var service = CreateService(store);
        var req = new CreateDraftAttendanceRequest(farm.Id, worker.Id, date, "FULL_DAY");

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateDraftAsync(CreateActor(), req));

        Assert.NotNull(ex.Errors);
        Assert.True(ex.Errors.ContainsKey("workerId"));
    }

    [Fact]
    public async Task CreateDraftAsync_WhenHourlyLacksPositiveHours_ThrowsValidationException()
    {
        var store = new FakeAttendanceStore();
        var farm = CreateFarm();
        store.Farms.Add(farm);

        var date = new DateOnly(2026, 9, 12);
        var worker = CreateWorker();
        AssignWorkerToFarm(worker, farm, new DateOnly(2026, 1, 1));
        store.Workers.Add(worker);

        var service = CreateService(store);
        var reqNullHours = new CreateDraftAttendanceRequest(farm.Id, worker.Id, date, "HOURLY", WorkingHours: null);

        var ex1 = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateDraftAsync(CreateActor(), reqNullHours));
        Assert.True(ex1.Errors?.ContainsKey("workingHours"));

        var reqZeroHours = new CreateDraftAttendanceRequest(farm.Id, worker.Id, date, "HOURLY", WorkingHours: 0m);
        var ex2 = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateDraftAsync(CreateActor(), reqZeroHours));
        Assert.True(ex2.Errors?.ContainsKey("workingHours"));
    }

    [Fact]
    public async Task CreateDraftAsync_WhenFullDayProvidesHours_ThrowsValidationException()
    {
        var store = new FakeAttendanceStore();
        var farm = CreateFarm();
        store.Farms.Add(farm);

        var date = new DateOnly(2026, 9, 12);
        var worker = CreateWorker();
        AssignWorkerToFarm(worker, farm, new DateOnly(2026, 1, 1));
        store.Workers.Add(worker);

        var service = CreateService(store);
        var req = new CreateDraftAttendanceRequest(farm.Id, worker.Id, date, "FULL_DAY", WorkingHours: 5m);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateDraftAsync(CreateActor(), req));

        Assert.NotNull(ex.Errors);
        Assert.True(ex.Errors.ContainsKey("workingHours"));
    }

    [Fact]
    public async Task CreateDraftAsync_WhenNotWorked_SetsZeroEarningsWithoutCallingWageLookup()
    {
        var store = new FakeAttendanceStore();
        var farm = CreateFarm();
        store.Farms.Add(farm);

        var date = new DateOnly(2026, 9, 12);
        var worker = CreateWorker();
        AssignWorkerToFarm(worker, farm, new DateOnly(2026, 1, 1));
        store.Workers.Add(worker);

        var earnings = new FakeAttendanceEarningsIntegration();
        var service = CreateService(store, earnings);

        var req = new CreateDraftAttendanceRequest(farm.Id, worker.Id, date, "NOT_WORKED", Notes: "Did not report");
        var result = await service.CreateDraftAsync(CreateActor(), req);

        Assert.Equal("NOT_WORKED", result.AttendanceType);
        Assert.Null(result.WorkingHours);
        Assert.Null(result.CalculatedRate);
        Assert.Equal(0m, result.CalculatedAmount);
        Assert.Null(result.CurrencyId);
        Assert.Equal("DRAFT", result.Status);
        Assert.Equal("Did not report", result.Notes);
        Assert.Equal(0, earnings.CalculationCallCount);
    }

    [Fact]
    public async Task CreateDraftAsync_WhenPaidAttendance_CallsEarningsIntegrationAndCalculatesRateAndAmount()
    {
        var store = new FakeAttendanceStore();
        var farm = CreateFarm();
        store.Farms.Add(farm);

        var date = new DateOnly(2026, 9, 12);
        var worker = CreateWorker();
        AssignWorkerToFarm(worker, farm, new DateOnly(2026, 1, 1));
        store.Workers.Add(worker);

        var earnings = new FakeAttendanceEarningsIntegration
        {
            HourlyRate = 80m
        };

        var service = CreateService(store, earnings);
        var req = new CreateDraftAttendanceRequest(farm.Id, worker.Id, date, "HOURLY", WorkingHours: 6.5m);

        var result = await service.CreateDraftAsync(CreateActor(), req);

        Assert.Equal("HOURLY", result.AttendanceType);
        Assert.Equal(6.5m, result.WorkingHours);
        Assert.Equal(80m, result.CalculatedRate);
        Assert.Equal(520m, result.CalculatedAmount); // 6.5 * 80
        Assert.Equal(earnings.CurrencyId, result.CurrencyId);
        Assert.Equal("DRAFT", result.Status);
        Assert.Equal(1, earnings.CalculationCallCount);
        Assert.Equal(0, earnings.LedgerWriteCallCount);
    }

    #endregion

    #region Update Draft Tests

    [Fact]
    public async Task UpdateDraftAsync_WhenRecordNotFound_ThrowsResourceNotFoundException()
    {
        var store = new FakeAttendanceStore();
        var service = CreateService(store);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            service.UpdateDraftAsync(CreateActor(), Guid.NewGuid(), new UpdateDraftAttendanceRequest("FULL_DAY")));
    }

    [Fact]
    public async Task UpdateDraftAsync_WhenRecordFinalized_ThrowsValidationException()
    {
        var store = new FakeAttendanceStore();
        var farm = CreateFarm();
        store.Farms.Add(farm);

        var date = new DateOnly(2026, 9, 12);
        var worker = CreateWorker();
        AssignWorkerToFarm(worker, farm, new DateOnly(2026, 1, 1));
        store.Workers.Add(worker);

        var finalized = LaborAttendance.CreateFinalized(_organizationId, farm.Id, worker.Id, date, AttendanceType.FullDay, _userId, null, 500m, 500m);
        store.Attendances.Add(finalized);

        var service = CreateService(store);
        var req = new UpdateDraftAttendanceRequest("HALF_DAY");

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpdateDraftAsync(CreateActor(), finalized.Id, req));

        Assert.NotNull(ex.Errors);
        Assert.True(ex.Errors.ContainsKey("status"));
    }

    [Fact]
    public async Task UpdateDraftAsync_WhenValidDraft_UpdatesAttendanceAndRecalculatesEarnings()
    {
        var store = new FakeAttendanceStore();
        var farm = CreateFarm();
        store.Farms.Add(farm);

        var date = new DateOnly(2026, 9, 12);
        var worker = CreateWorker();
        AssignWorkerToFarm(worker, farm, new DateOnly(2026, 1, 1));
        store.Workers.Add(worker);

        var draft = LaborAttendance.CreateDraft(_organizationId, farm.Id, worker.Id, date, AttendanceType.FullDay, _userId, null, 500m, 500m);
        store.Attendances.Add(draft);

        var earnings = new FakeAttendanceEarningsIntegration
        {
            HalfDayRate = 220m
        };

        var service = CreateService(store, earnings);
        var req = new UpdateDraftAttendanceRequest("HALF_DAY", Notes: "Switched to half day");

        var result = await service.UpdateDraftAsync(CreateActor(), draft.Id, req);

        Assert.Equal("HALF_DAY", result.AttendanceType);
        Assert.Null(result.WorkingHours);
        Assert.Equal(220m, result.CalculatedRate);
        Assert.Equal(220m, result.CalculatedAmount);
        Assert.Equal("Switched to half day", result.Notes);
    }

    #endregion

    #region Delete Draft Tests

    [Fact]
    public async Task DeleteDraftAsync_WhenRecordNotFound_ThrowsResourceNotFoundException()
    {
        var store = new FakeAttendanceStore();
        var service = CreateService(store);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            service.DeleteDraftAsync(CreateActor(), Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteDraftAsync_WhenRecordFinalized_ThrowsValidationException()
    {
        var store = new FakeAttendanceStore();
        var farm = CreateFarm();
        store.Farms.Add(farm);

        var date = new DateOnly(2026, 9, 12);
        var worker = CreateWorker();
        AssignWorkerToFarm(worker, farm, new DateOnly(2026, 1, 1));
        store.Workers.Add(worker);

        var finalized = LaborAttendance.CreateFinalized(_organizationId, farm.Id, worker.Id, date, AttendanceType.FullDay, _userId, null, 500m, 500m);
        store.Attendances.Add(finalized);

        var service = CreateService(store);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.DeleteDraftAsync(CreateActor(), finalized.Id));

        Assert.NotNull(ex.Errors);
        Assert.True(ex.Errors.ContainsKey("status"));
    }

    [Fact]
    public async Task DeleteDraftAsync_WhenValidDraft_RemovesRecord()
    {
        var store = new FakeAttendanceStore();
        var farm = CreateFarm();
        store.Farms.Add(farm);

        var date = new DateOnly(2026, 9, 12);
        var worker = CreateWorker();
        AssignWorkerToFarm(worker, farm, new DateOnly(2026, 1, 1));
        store.Workers.Add(worker);

        var draft = LaborAttendance.CreateDraft(_organizationId, farm.Id, worker.Id, date, AttendanceType.FullDay, _userId);
        store.Attendances.Add(draft);

        var service = CreateService(store);
        await service.DeleteDraftAsync(CreateActor(), draft.Id);

        Assert.Empty(store.Attendances);
    }

    #endregion

    #region Batch Draft Tests

    [Fact]
    public async Task SaveDailyDraftBatchAsync_WhenDuplicateWorkersInBatch_ThrowsValidationException()
    {
        var store = new FakeAttendanceStore();
        var farm = CreateFarm();
        store.Farms.Add(farm);

        var workerId = Guid.NewGuid();
        var req = new SaveDailyDraftAttendanceBatchRequest(
            farm.Id,
            new DateOnly(2026, 9, 12),
            [
                new DailyDraftAttendanceItemRequest(null, workerId, "FULL_DAY"),
                new DailyDraftAttendanceItemRequest(null, workerId, "HALF_DAY")
            ]);

        var service = CreateService(store);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.SaveDailyDraftBatchAsync(CreateActor(), req));

        Assert.NotNull(ex.Errors);
        Assert.True(ex.Errors.ContainsKey("items"));
    }

    [Fact]
    public async Task SaveDailyDraftBatchAsync_WhenWorkerFinalized_ThrowsValidationException()
    {
        var store = new FakeAttendanceStore();
        var farm = CreateFarm();
        store.Farms.Add(farm);

        var date = new DateOnly(2026, 9, 12);
        var worker = CreateWorker();
        AssignWorkerToFarm(worker, farm, new DateOnly(2026, 1, 1));
        store.Workers.Add(worker);

        var finalized = LaborAttendance.CreateFinalized(_organizationId, farm.Id, worker.Id, date, AttendanceType.FullDay, _userId, null, 500m, 500m);
        store.Attendances.Add(finalized);

        var req = new SaveDailyDraftAttendanceBatchRequest(
            farm.Id,
            date,
            [
                new DailyDraftAttendanceItemRequest(null, worker.Id, "HALF_DAY")
            ]);

        var service = CreateService(store);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.SaveDailyDraftBatchAsync(CreateActor(), req));

        Assert.NotNull(ex.Errors);
        Assert.True(ex.Errors.ContainsKey("status"));
    }

    [Fact]
    public async Task SaveDailyDraftBatchAsync_UpsertsBatchSuccessfully()
    {
        var store = new FakeAttendanceStore();
        var farm = CreateFarm();
        store.Farms.Add(farm);

        var date = new DateOnly(2026, 9, 12);
        var w1 = CreateWorker(firstName: "Worker1");
        var w2 = CreateWorker(firstName: "Worker2");
        AssignWorkerToFarm(w1, farm, new DateOnly(2026, 1, 1));
        AssignWorkerToFarm(w2, farm, new DateOnly(2026, 1, 1));
        store.Workers.AddRange([w1, w2]);

        // w1 already has an existing draft
        var existingDraft = LaborAttendance.CreateDraft(_organizationId, farm.Id, w1.Id, date, AttendanceType.FullDay, _userId, null, 500m, 500m);
        store.Attendances.Add(existingDraft);

        var req = new SaveDailyDraftAttendanceBatchRequest(
            farm.Id,
            date,
            [
                new DailyDraftAttendanceItemRequest(existingDraft.Id, w1.Id, "HALF_DAY"),
                new DailyDraftAttendanceItemRequest(null, w2.Id, "HOURLY", WorkingHours: 5m)
            ]);

        var service = CreateService(store);
        var result = await service.SaveDailyDraftBatchAsync(CreateActor(), req);

        Assert.Equal(2, result.Summary.TotalCount);
        Assert.Equal(2, result.Summary.WorkedCount);
        Assert.Equal(1, result.Summary.HalfDayCount);
        Assert.Equal(1, result.Summary.HourlyCount);
        Assert.Equal(2, store.Attendances.Count);
    }

    [Fact]
    public async Task SaveDailyDraftBatchAsync_WithRemoveOmittedDrafts_DeletesOmittedDraftRecords()
    {
        var store = new FakeAttendanceStore();
        var farm = CreateFarm();
        store.Farms.Add(farm);

        var date = new DateOnly(2026, 9, 12);
        var w1 = CreateWorker(firstName: "Worker1");
        var w2 = CreateWorker(firstName: "Worker2");
        AssignWorkerToFarm(w1, farm, new DateOnly(2026, 1, 1));
        AssignWorkerToFarm(w2, farm, new DateOnly(2026, 1, 1));
        store.Workers.AddRange([w1, w2]);

        // Both w1 and w2 exist as drafts
        var draft1 = LaborAttendance.CreateDraft(_organizationId, farm.Id, w1.Id, date, AttendanceType.FullDay, _userId);
        var draft2 = LaborAttendance.CreateDraft(_organizationId, farm.Id, w2.Id, date, AttendanceType.FullDay, _userId);
        store.Attendances.AddRange([draft1, draft2]);

        // Batch only submits w1 with RemoveOmittedDrafts = true
        var req = new SaveDailyDraftAttendanceBatchRequest(
            farm.Id,
            date,
            [
                new DailyDraftAttendanceItemRequest(draft1.Id, w1.Id, "FULL_DAY")
            ],
            RemoveOmittedDrafts: true);

        var service = CreateService(store);
        var result = await service.SaveDailyDraftBatchAsync(CreateActor(), req);

        Assert.Equal(1, result.Summary.TotalCount);
        Assert.Single(store.Attendances);
        Assert.Equal(w1.Id, store.Attendances[0].WorkerId);
    }

    #endregion

    #region Attendance Wage Preview Tests

    [Fact]
    public async Task PreviewWageAsync_FullDay_ReturnsFullDayRateAndCalculatedAmount()
    {
        var store = new FakeAttendanceStore();
        var earnings = new FakeAttendanceEarningsIntegration { FullDayRate = 500m };
        var worker = CreateWorker(gender: Gender.Male);
        store.Workers.Add(worker);
        var service = CreateService(store, earnings);

        var request = new AttendanceWagePreviewRequest(
            WorkerId: worker.Id,
            AttendanceDate: new DateOnly(2026, 9, 12),
            AttendanceType: "FULL_DAY");

        var response = await service.PreviewWageAsync(CreateActor(), request);

        Assert.NotNull(response);
        Assert.Equal(worker.Id, response.WorkerId);
        Assert.Equal("FULL_DAY", response.AttendanceType);
        Assert.Equal("FULL_DAY", response.ResolvedWageType);
        Assert.Equal("FULL_DAY", response.WageType);
        Assert.Equal(500m, response.Rate);
        Assert.Equal(1m, response.Quantity);
        Assert.Null(response.WorkingHours);
        Assert.Equal(500m, response.CalculatedAmount);
        Assert.Equal("INR", response.CurrencyCode);
        Assert.Equal("₹", response.CurrencySymbol);
        Assert.Equal("INR", response.Currency);
        Assert.True(response.IsEarningEligible);
        Assert.True(response.IsWorkerEligible);
        Assert.Equal(0, earnings.LedgerWriteCallCount);
    }

    [Fact]
    public async Task PreviewWageAsync_HalfDay_ReturnsHalfDayRateAndCalculatedAmount()
    {
        var store = new FakeAttendanceStore();
        var earnings = new FakeAttendanceEarningsIntegration { HalfDayRate = 275m };
        var worker = CreateWorker(gender: Gender.Female);
        store.Workers.Add(worker);
        var service = CreateService(store, earnings);

        var request = new AttendanceWagePreviewRequest(
            WorkerId: worker.Id,
            AttendanceDate: new DateOnly(2026, 9, 12),
            AttendanceType: "HALF_DAY");

        var response = await service.PreviewWageAsync(CreateActor(), request);

        Assert.NotNull(response);
        Assert.Equal("HALF_DAY", response.AttendanceType);
        Assert.Equal("HALF_DAY", response.ResolvedWageType);
        Assert.Equal(275m, response.Rate);
        Assert.Equal(1m, response.Quantity);
        Assert.Null(response.WorkingHours);
        Assert.Equal(275m, response.CalculatedAmount);
        Assert.True(response.IsEarningEligible);
        Assert.Equal(0, earnings.LedgerWriteCallCount);
    }

    [Fact]
    public async Task PreviewWageAsync_Hourly_CalculatesHoursMultipliedByRate()
    {
        var store = new FakeAttendanceStore();
        var earnings = new FakeAttendanceEarningsIntegration { HourlyRate = 60m };
        var worker = CreateWorker();
        store.Workers.Add(worker);
        var service = CreateService(store, earnings);

        var request = new AttendanceWagePreviewRequest(
            WorkerId: worker.Id,
            AttendanceDate: new DateOnly(2026, 9, 12),
            AttendanceType: "HOURLY",
            WorkingHours: 6.5m);

        var response = await service.PreviewWageAsync(CreateActor(), request);

        Assert.NotNull(response);
        Assert.Equal("HOURLY", response.AttendanceType);
        Assert.Equal("HOURLY", response.ResolvedWageType);
        Assert.Equal(60m, response.Rate);
        Assert.Equal(6.5m, response.Quantity);
        Assert.Equal(6.5m, response.WorkingHours);
        Assert.Equal(390m, response.CalculatedAmount);
        Assert.True(response.IsEarningEligible);
        Assert.Equal(0, earnings.LedgerWriteCallCount);
    }

    [Fact]
    public async Task PreviewWageAsync_NotWorked_ReturnsZeroEarningsAndZeroRate()
    {
        var store = new FakeAttendanceStore();
        var earnings = new FakeAttendanceEarningsIntegration();
        var worker = CreateWorker();
        store.Workers.Add(worker);
        var service = CreateService(store, earnings);

        var request = new AttendanceWagePreviewRequest(
            WorkerId: worker.Id,
            AttendanceDate: new DateOnly(2026, 9, 12),
            AttendanceType: "NOT_WORKED");

        var response = await service.PreviewWageAsync(CreateActor(), request);

        Assert.NotNull(response);
        Assert.Equal("NOT_WORKED", response.AttendanceType);
        Assert.Null(response.ResolvedWageType);
        Assert.Null(response.WageType);
        Assert.Equal(0m, response.Rate);
        Assert.Equal(0m, response.Quantity);
        Assert.Null(response.WorkingHours);
        Assert.Equal(0m, response.CalculatedAmount);
        Assert.False(response.IsEarningEligible);
        Assert.Equal(0, earnings.LedgerWriteCallCount);
    }

    [Fact]
    public async Task PreviewWageAsync_WhenHourlyMissingWorkingHours_ThrowsValidationException()
    {
        var store = new FakeAttendanceStore();
        var service = CreateService(store);

        var request = new AttendanceWagePreviewRequest(
            WorkerId: Guid.NewGuid(),
            AttendanceDate: new DateOnly(2026, 9, 12),
            AttendanceType: "HOURLY",
            WorkingHours: null);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.PreviewWageAsync(CreateActor(), request));
        Assert.NotNull(ex.Errors);
        Assert.True(ex.Errors.ContainsKey("workingHours"));
    }

    [Fact]
    public async Task PreviewWageAsync_WhenWorkingHoursGivenForFullDay_ThrowsValidationException()
    {
        var store = new FakeAttendanceStore();
        var service = CreateService(store);

        var request = new AttendanceWagePreviewRequest(
            WorkerId: Guid.NewGuid(),
            AttendanceDate: new DateOnly(2026, 9, 12),
            AttendanceType: "FULL_DAY",
            WorkingHours: 5m);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.PreviewWageAsync(CreateActor(), request));
        Assert.NotNull(ex.Errors);
        Assert.True(ex.Errors.ContainsKey("workingHours"));
    }

    [Fact]
    public async Task PreviewWageAsync_WhenFarmIdProvidedAndWorkerNotAssigned_ThrowsValidationException()
    {
        var store = new FakeAttendanceStore();
        var farm = CreateFarm();
        var worker = CreateWorker();
        store.Farms.Add(farm);
        store.Workers.Add(worker); // Not assigned to farm
        var service = CreateService(store);

        var request = new AttendanceWagePreviewRequest(
            WorkerId: worker.Id,
            AttendanceDate: new DateOnly(2026, 9, 12),
            AttendanceType: "FULL_DAY",
            FarmId: farm.Id);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.PreviewWageAsync(CreateActor(), request));
        Assert.NotNull(ex.Errors);
        Assert.True(ex.Errors.ContainsKey("workerId"));
    }

    [Fact]
    public async Task PreviewWageAsync_WhenFarmIdProvidedAndWorkerAssigned_Succeeds()
    {
        var store = new FakeAttendanceStore();
        var earnings = new FakeAttendanceEarningsIntegration { FullDayRate = 500m };
        var farm = CreateFarm();
        var worker = CreateWorker();
        AssignWorkerToFarm(worker, farm, new DateOnly(2026, 1, 1));
        store.Farms.Add(farm);
        store.Workers.Add(worker);
        var service = CreateService(store, earnings);

        var request = new AttendanceWagePreviewRequest(
            WorkerId: worker.Id,
            AttendanceDate: new DateOnly(2026, 9, 12),
            AttendanceType: "FULL_DAY",
            FarmId: farm.Id);

        var response = await service.PreviewWageAsync(CreateActor(), request);

        Assert.NotNull(response);
        Assert.Equal(500m, response.CalculatedAmount);
    }

    [Fact]
    public async Task PreviewWageAsync_DoesNotWriteToEarningsLedger()
    {
        var store = new FakeAttendanceStore();
        var earnings = new FakeAttendanceEarningsIntegration();
        var worker = CreateWorker();
        store.Workers.Add(worker);
        var service = CreateService(store, earnings);

        var request = new AttendanceWagePreviewRequest(
            WorkerId: worker.Id,
            AttendanceDate: new DateOnly(2026, 9, 12),
            AttendanceType: "FULL_DAY");

        await service.PreviewWageAsync(CreateActor(), request);

        Assert.Equal(1, earnings.CalculationCallCount);
        Assert.Equal(0, earnings.LedgerWriteCallCount);
        Assert.Empty(store.Attendances);
    }

    [Fact]
    public async Task PreviewWageBatchAsync_WithMixedAttendanceTypes_CalculatesRosterPreviewAndSummaryTotals()
    {
        var store = new FakeAttendanceStore();
        var earnings = new FakeAttendanceEarningsIntegration
        {
            FullDayRate = 500m,
            HalfDayRate = 250m,
            HourlyRate = 60m
        };
        var farm = CreateFarm();
        var w1 = CreateWorker(firstName: "Worker1");
        var w2 = CreateWorker(firstName: "Worker2");
        var w3 = CreateWorker(firstName: "Worker3");
        var w4 = CreateWorker(firstName: "Worker4");
        AssignWorkerToFarm(w1, farm, new DateOnly(2026, 1, 1));
        AssignWorkerToFarm(w2, farm, new DateOnly(2026, 1, 1));
        AssignWorkerToFarm(w3, farm, new DateOnly(2026, 1, 1));
        AssignWorkerToFarm(w4, farm, new DateOnly(2026, 1, 1));
        store.Farms.Add(farm);
        store.Workers.AddRange([w1, w2, w3, w4]);

        var service = CreateService(store, earnings);
        var date = new DateOnly(2026, 9, 12);

        var request = new AttendanceWagePreviewBatchRequest(
            AttendanceDate: date,
            FarmId: farm.Id,
            Items:
            [
                new AttendanceWagePreviewBatchItemRequest(w1.Id, "FULL_DAY"),
                new AttendanceWagePreviewBatchItemRequest(w2.Id, "HALF_DAY"),
                new AttendanceWagePreviewBatchItemRequest(w3.Id, "HOURLY", WorkingHours: 5m),
                new AttendanceWagePreviewBatchItemRequest(w4.Id, "NOT_WORKED")
            ]);

        var response = await service.PreviewWageBatchAsync(CreateActor(), request);

        Assert.NotNull(response);
        Assert.Equal(date, response.AttendanceDate);
        Assert.Equal(4, response.TotalCount);
        Assert.Equal(3, response.WorkedCount);
        Assert.Equal(1, response.FullDayCount);
        Assert.Equal(1, response.HalfDayCount);
        Assert.Equal(1, response.HourlyCount);
        Assert.Equal(1, response.NotWorkedCount);
        // 500 (full) + 250 (half) + 300 (5 * 60) + 0 (not worked) = 1050
        Assert.Equal(1050m, response.TotalEstimatedEarnings);
        Assert.Equal(4, response.Items.Count);
        Assert.Equal(0, earnings.LedgerWriteCallCount);
    }

    #endregion

    #region Attendance Finalization Tests

    [Fact]
    public async Task FinalizeAttendanceAsync_WhenValidDraftBatch_FinalizesAllAndGeneratesApprovedEarnings()
    {
        var store = new FakeAttendanceStore();
        var earnings = new FakeAttendanceEarningsIntegration
        {
            FullDayRate = 500m,
            HalfDayRate = 250m,
            HourlyRate = 75m
        };
        var farm = CreateFarm();
        var w1 = CreateWorker(firstName: "Worker1");
        var w2 = CreateWorker(firstName: "Worker2");
        var w3 = CreateWorker(firstName: "Worker3");
        var date = new DateOnly(2026, 9, 12);
        AssignWorkerToFarm(w1, farm, new DateOnly(2026, 1, 1));
        AssignWorkerToFarm(w2, farm, new DateOnly(2026, 1, 1));
        AssignWorkerToFarm(w3, farm, new DateOnly(2026, 1, 1));
        store.Farms.Add(farm);
        store.Workers.AddRange([w1, w2, w3]);

        var d1 = LaborAttendance.CreateDraft(_organizationId, farm.Id, w1.Id, date, AttendanceType.FullDay, _userId);
        var d2 = LaborAttendance.CreateDraft(_organizationId, farm.Id, w2.Id, date, AttendanceType.HalfDay, _userId);
        var d3 = LaborAttendance.CreateDraft(_organizationId, farm.Id, w3.Id, date, AttendanceType.Hourly, _userId, workingHours: 6m);
        store.Attendances.AddRange([d1, d2, d3]);

        var service = CreateService(store, earnings);
        var request = new FinalizeAttendanceRequest(farm.Id, date);

        var response = await service.FinalizeAttendanceAsync(CreateActor(), request);

        Assert.NotNull(response);
        Assert.Equal(3, response.FinalizedCount);
        Assert.Equal(3, response.PaidCount);
        Assert.Equal(0, response.NotWorkedCount);
        // 500 + 250 + (6 * 75 = 450) = 1200
        Assert.Equal(1200m, response.TotalEarnings);
        Assert.Equal(3, earnings.LedgerWriteCallCount);
        Assert.Equal(3, earnings.CreatedEarnings.Count);
        Assert.All(earnings.CreatedEarnings, e => Assert.Equal("APPROVED", e.Status));

        Assert.All(store.Attendances, a =>
        {
            Assert.Equal(AttendanceStatus.Finalized, a.Status);
            Assert.NotNull(a.FinalizedAt);
            Assert.Equal(_userId, a.FinalizedBy);
        });
    }

    [Fact]
    public async Task FinalizeAttendanceAsync_WithNotWorkedRow_FinalizesWithoutCreatingEarningsEntry()
    {
        var store = new FakeAttendanceStore();
        var earnings = new FakeAttendanceEarningsIntegration { FullDayRate = 500m };
        var farm = CreateFarm();
        var w1 = CreateWorker(firstName: "WorkedWorker");
        var w2 = CreateWorker(firstName: "NotWorkedWorker");
        var date = new DateOnly(2026, 9, 12);
        AssignWorkerToFarm(w1, farm, new DateOnly(2026, 1, 1));
        AssignWorkerToFarm(w2, farm, new DateOnly(2026, 1, 1));
        store.Farms.Add(farm);
        store.Workers.AddRange([w1, w2]);

        var d1 = LaborAttendance.CreateDraft(_organizationId, farm.Id, w1.Id, date, AttendanceType.FullDay, _userId);
        var d2 = LaborAttendance.CreateDraft(_organizationId, farm.Id, w2.Id, date, AttendanceType.NotWorked, _userId);
        store.Attendances.AddRange([d1, d2]);

        var service = CreateService(store, earnings);
        var request = new FinalizeAttendanceRequest(farm.Id, date);

        var response = await service.FinalizeAttendanceAsync(CreateActor(), request);

        Assert.NotNull(response);
        Assert.Equal(2, response.FinalizedCount);
        Assert.Equal(1, response.PaidCount);
        Assert.Equal(1, response.NotWorkedCount);
        Assert.Equal(500m, response.TotalEarnings);
        // Only 1 ledger write for the paid worker, NOT for NOT_WORKED!
        Assert.Equal(1, earnings.LedgerWriteCallCount);

        var finalizedNotWorked = store.Attendances.Single(a => a.WorkerId == w2.Id);
        Assert.Equal(AttendanceStatus.Finalized, finalizedNotWorked.Status);
        Assert.Null(finalizedNotWorked.CalculatedRate);
        Assert.Equal(0m, finalizedNotWorked.CalculatedAmount);
    }

    [Fact]
    public async Task FinalizeAttendanceAsync_WhenWorkerMissingFarmAssignment_RollsBackAllAndCreatesNoEarnings()
    {
        var store = new FakeAttendanceStore();
        var earnings = new FakeAttendanceEarningsIntegration();
        var farm = CreateFarm();
        var w1 = CreateWorker(firstName: "ValidWorker");
        var w2 = CreateWorker(firstName: "UnassignedWorker");
        var date = new DateOnly(2026, 9, 12);
        AssignWorkerToFarm(w1, farm, new DateOnly(2026, 1, 1));
        // w2 has NO farm assignment
        store.Farms.Add(farm);
        store.Workers.AddRange([w1, w2]);

        var d1 = LaborAttendance.CreateDraft(_organizationId, farm.Id, w1.Id, date, AttendanceType.FullDay, _userId);
        var d2 = LaborAttendance.CreateDraft(_organizationId, farm.Id, w2.Id, date, AttendanceType.FullDay, _userId);
        store.Attendances.AddRange([d1, d2]);

        var service = CreateService(store, earnings);
        var request = new FinalizeAttendanceRequest(farm.Id, date);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.FinalizeAttendanceAsync(CreateActor(), request));
        Assert.NotNull(ex.Errors);
        Assert.True(ex.Errors.ContainsKey("workerId"));

        // Atomicity check: w1 must NOT be left finalized!
        Assert.All(store.Attendances, a => Assert.Equal(AttendanceStatus.Draft, a.Status));
        Assert.Equal(0, earnings.LedgerWriteCallCount);
    }

    [Fact]
    public async Task FinalizeAttendanceAsync_WhenEarningsCreationFails_RollsBackAllAttendanceRows()
    {
        var store = new FakeAttendanceStore();
        var earnings = new FakeAttendanceEarningsIntegration { ThrowOnLedgerWrite = true };
        var farm = CreateFarm();
        var w1 = CreateWorker(firstName: "Worker1");
        var date = new DateOnly(2026, 9, 12);
        AssignWorkerToFarm(w1, farm, new DateOnly(2026, 1, 1));
        store.Farms.Add(farm);
        store.Workers.Add(w1);

        var d1 = LaborAttendance.CreateDraft(_organizationId, farm.Id, w1.Id, date, AttendanceType.FullDay, _userId);
        store.Attendances.Add(d1);

        var service = CreateService(store, earnings);
        var request = new FinalizeAttendanceRequest(farm.Id, date);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.FinalizeAttendanceAsync(CreateActor(), request));

        // Attendance remains draft
        Assert.Equal(AttendanceStatus.Draft, d1.Status);
        Assert.Null(d1.FinalizedAt);
    }

    [Fact]
    public async Task FinalizeAttendanceAsync_WhenNoDraftRowsExist_ThrowsValidationException()
    {
        var store = new FakeAttendanceStore();
        var farm = CreateFarm();
        store.Farms.Add(farm);
        var service = CreateService(store);

        var request = new FinalizeAttendanceRequest(farm.Id, new DateOnly(2026, 9, 12));

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.FinalizeAttendanceAsync(CreateActor(), request));
        Assert.NotNull(ex.Errors);
        Assert.True(ex.Errors.ContainsKey("attendance") || ex.Errors.ContainsKey("status"));
    }

    [Fact]
    public async Task FinalizeAttendanceAsync_WhenAlreadyFinalized_ThrowsValidationException()
    {
        var store = new FakeAttendanceStore();
        var farm = CreateFarm();
        var worker = CreateWorker();
        var date = new DateOnly(2026, 9, 12);
        AssignWorkerToFarm(worker, farm, new DateOnly(2026, 1, 1));
        store.Farms.Add(farm);
        store.Workers.Add(worker);

        var att = LaborAttendance.CreateFinalized(_organizationId, farm.Id, worker.Id, date, AttendanceType.FullDay, _userId);
        store.Attendances.Add(att);

        var service = CreateService(store);
        var request = new FinalizeAttendanceRequest(farm.Id, date);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.FinalizeAttendanceAsync(CreateActor(), request));
        Assert.NotNull(ex.Errors);
        Assert.True(ex.Errors.ContainsKey("status"));
    }

    [Fact]
    public async Task FinalizeSingleAttendanceAsync_WhenValidDraft_FinalizesSingleAndCreatesEarnings()
    {
        var store = new FakeAttendanceStore();
        var earnings = new FakeAttendanceEarningsIntegration { FullDayRate = 500m };
        var farm = CreateFarm();
        var worker = CreateWorker();
        var date = new DateOnly(2026, 9, 12);
        AssignWorkerToFarm(worker, farm, new DateOnly(2026, 1, 1));
        store.Farms.Add(farm);
        store.Workers.Add(worker);

        var draft = LaborAttendance.CreateDraft(_organizationId, farm.Id, worker.Id, date, AttendanceType.FullDay, _userId);
        store.Attendances.Add(draft);

        var service = CreateService(store, earnings);

        var response = await service.FinalizeSingleAttendanceAsync(CreateActor(), draft.Id);

        Assert.NotNull(response);
        Assert.Equal(draft.Id, response.Id);
        Assert.Equal("FINALIZED", response.Status);
        Assert.Equal(500m, response.CalculatedAmount);
        Assert.Equal(1, earnings.LedgerWriteCallCount);
        Assert.Equal(AttendanceStatus.Finalized, draft.Status);
    }

    [Fact]
    public async Task UpdateDraftAsync_AfterFinalization_ThrowsValidationException()
    {
        var store = new FakeAttendanceStore();
        var earnings = new FakeAttendanceEarningsIntegration { FullDayRate = 500m };
        var farm = CreateFarm();
        var worker = CreateWorker();
        var date = new DateOnly(2026, 9, 12);
        AssignWorkerToFarm(worker, farm, new DateOnly(2026, 1, 1));
        store.Farms.Add(farm);
        store.Workers.Add(worker);

        var draft = LaborAttendance.CreateDraft(_organizationId, farm.Id, worker.Id, date, AttendanceType.FullDay, _userId);
        store.Attendances.Add(draft);

        var service = CreateService(store, earnings);

        // Finalize attendance
        await service.FinalizeSingleAttendanceAsync(CreateActor(), draft.Id);

        // Now attempt to update it
        var updateRequest = new UpdateDraftAttendanceRequest("HALF_DAY");
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpdateDraftAsync(CreateActor(), draft.Id, updateRequest));
        Assert.NotNull(ex.Errors);
        Assert.True(ex.Errors.ContainsKey("status"));
    }

    [Fact]
    public async Task DeleteDraftAsync_AfterFinalization_ThrowsValidationException()
    {
        var store = new FakeAttendanceStore();
        var earnings = new FakeAttendanceEarningsIntegration { FullDayRate = 500m };
        var farm = CreateFarm();
        var worker = CreateWorker();
        var date = new DateOnly(2026, 9, 12);
        AssignWorkerToFarm(worker, farm, new DateOnly(2026, 1, 1));
        store.Farms.Add(farm);
        store.Workers.Add(worker);

        var draft = LaborAttendance.CreateDraft(_organizationId, farm.Id, worker.Id, date, AttendanceType.FullDay, _userId);
        store.Attendances.Add(draft);

        var service = CreateService(store, earnings);

        // Finalize attendance
        await service.FinalizeSingleAttendanceAsync(CreateActor(), draft.Id);

        // Now attempt to delete it
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.DeleteDraftAsync(CreateActor(), draft.Id));
        Assert.NotNull(ex.Errors);
        Assert.True(ex.Errors.ContainsKey("status"));
    }

    #endregion

    #region Fake Store & Integration

    private sealed class FakeAttendanceEarningsIntegration : IAttendanceEarningsIntegration
    {
        public decimal FullDayRate { get; set; } = 500m;
        public decimal HalfDayRate { get; set; } = 250m;
        public decimal HourlyRate { get; set; } = 75m;
        public Guid CurrencyId { get; set; } = Guid.NewGuid();
        public int CalculationCallCount { get; private set; }
        public int LedgerWriteCallCount { get; private set; }

        public Task<AttendanceEarningsCalculationResult> CalculateAttendanceEarningsAsync(
            EarningsActor actor,
            CalculateAttendanceEarningsRequest request,
            CancellationToken cancellationToken = default)
        {
            CalculationCallCount++;
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

        public bool ThrowOnLedgerWrite { get; set; }
        public List<WorkerEarningsLedgerResponse> CreatedEarnings { get; } = [];

        public Task<WorkerEarningsLedgerResponse?> ProcessAttendanceEarningsAsync(
            EarningsActor actor,
            ProcessAttendanceEarningsRequest request,
            CancellationToken cancellationToken = default)
        {
            LedgerWriteCallCount++;
            throw new NotSupportedException("Ordinary draft operations must never invoke ProcessAttendanceEarningsAsync.");
        }

        public Task<WorkerEarningsLedgerResponse> CreateEarningFromFinalizedAttendanceAsync(
            EarningsActor actor,
            FinalizedAttendanceRecord attendance,
            CancellationToken cancellationToken = default)
        {
            LedgerWriteCallCount++;
            if (ThrowOnLedgerWrite)
            {
                throw new InvalidOperationException("Simulated earnings creation failure.");
            }

            var rate = attendance.WageType switch
            {
                WageType.FullDay => FullDayRate,
                WageType.HalfDay => HalfDayRate,
                WageType.Hourly => HourlyRate,
                _ => 100m
            };

            var amount = Math.Round(rate * attendance.Quantity, 2);
            var response = new WorkerEarningsLedgerResponse(
                Id: Guid.NewGuid(),
                OrganizationId: actor.OrganizationId,
                WorkerId: attendance.WorkerId,
                WorkerDisplayName: "Worker",
                AttendanceId: attendance.AttendanceId,
                EarningsDate: attendance.AttendanceDate,
                WageType: attendance.WageType.ToString().ToUpperInvariant(),
                Quantity: attendance.Quantity,
                WageRate: rate,
                CurrencyId: CurrencyId,
                CurrencyCode: "INR",
                CurrencySymbol: "₹",
                GrossAmount: amount,
                EntryType: "EARNING",
                Status: "APPROVED",
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

        public Task<Farm?> FindFarmAsync(
            Guid farmId,
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            var farm = Farms.FirstOrDefault(f => f.Id == farmId && f.OrganizationId == organizationId);
            return Task.FromResult(farm);
        }

        public Task<int> CountEligibleWorkersAsync(
            Guid organizationId,
            Guid farmId,
            DateOnly attendanceDate,
            string? search,
            CancellationToken cancellationToken = default)
        {
            var matching = FilterWorkers(organizationId, farmId, attendanceDate, search);
            return Task.FromResult(matching.Count);
        }

        public Task<IReadOnlyList<AttendanceEligibleWorkerResponse>> ListEligibleWorkersAsync(
            Guid organizationId,
            Guid farmId,
            DateOnly attendanceDate,
            string? search,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            var matching = FilterWorkers(organizationId, farmId, attendanceDate, search)
                .OrderBy(w => w.DisplayName)
                .ThenBy(w => w.Id)
                .Skip(skip)
                .Take(take)
                .Select(w =>
                {
                    var assignment = w.FarmAssignments
                        .Where(a =>
                            a.OrganizationId == organizationId &&
                            a.FarmId == farmId &&
                            a.IsActive &&
                            a.AssignedFrom <= attendanceDate &&
                            (!a.AssignedTo.HasValue || a.AssignedTo.Value >= attendanceDate))
                        .OrderByDescending(a => a.AssignedFrom)
                        .First();

                    return new AttendanceEligibleWorkerResponse(
                        WorkerId: w.Id,
                        DisplayName: w.DisplayName,
                        FirstName: w.FirstName,
                        LastName: w.LastName,
                        Gender: w.Gender.ToString().ToUpperInvariant(),
                        EmploymentType: w.EmploymentType switch
                        {
                            EmploymentType.Permanent => "PERMANENT",
                            EmploymentType.Seasonal => "SEASONAL",
                            EmploymentType.DailyWage => "DAILY_WAGE",
                            EmploymentType.Contract => "CONTRACT",
                            _ => w.EmploymentType.ToString().ToUpperInvariant()
                        },
                        MobileNumber: w.MobileNumber,
                        LaborCategoryId: w.LaborCategoryId,
                        LaborCategoryName: w.LaborCategory?.Name,
                        ContractorId: w.ContractorId,
                        ContractorName: w.Contractor?.Name,
                        JoiningDate: w.JoiningDate,
                        LeavingDate: w.LeavingDate,
                        AssignmentId: assignment.Id,
                        FarmId: assignment.FarmId,
                        AssignedFrom: assignment.AssignedFrom,
                        AssignedTo: assignment.AssignedTo);
                })
                .ToList();

            return Task.FromResult<IReadOnlyList<AttendanceEligibleWorkerResponse>>(matching);
        }

        public Task<LaborAttendance?> FindAttendanceByIdAsync(
            Guid id,
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            var attendance = Attendances.FirstOrDefault(a => a.Id == id && a.OrganizationId == organizationId);
            return Task.FromResult(attendance);
        }

        public Task<LaborAttendance?> FindAttendanceByWorkerAndDateAsync(
            Guid organizationId,
            Guid workerId,
            DateOnly attendanceDate,
            CancellationToken cancellationToken = default)
        {
            var attendance = Attendances.FirstOrDefault(a =>
                a.OrganizationId == organizationId &&
                a.WorkerId == workerId &&
                a.AttendanceDate == attendanceDate);
            return Task.FromResult(attendance);
        }

        public Task<IReadOnlyList<LaborAttendance>> ListDailyAttendanceAsync(
            Guid organizationId,
            Guid farmId,
            DateOnly attendanceDate,
            CancellationToken cancellationToken = default)
        {
            var results = Attendances
                .Where(a =>
                    a.OrganizationId == organizationId &&
                    a.FarmId == farmId &&
                    a.AttendanceDate == attendanceDate)
                .OrderBy(a => a.Worker != null ? a.Worker.DisplayName : string.Empty)
                .ThenBy(a => a.Id)
                .ToList();
            return Task.FromResult<IReadOnlyList<LaborAttendance>>(results);
        }

        public Task<IReadOnlyList<LaborAttendance>> ListDailyAttendanceTrackedAsync(
            Guid organizationId,
            Guid farmId,
            DateOnly attendanceDate,
            CancellationToken cancellationToken = default) =>
            ListDailyAttendanceAsync(organizationId, farmId, attendanceDate, cancellationToken);

        public Task<Worker?> FindWorkerWithAssignmentAsync(
            Guid organizationId,
            Guid workerId,
            Guid farmId,
            DateOnly attendanceDate,
            CancellationToken cancellationToken = default)
        {
            var worker = Workers.FirstOrDefault(w =>
                w.Id == workerId &&
                w.OrganizationId == organizationId &&
                w.IsActive &&
                (!w.JoiningDate.HasValue || w.JoiningDate.Value <= attendanceDate) &&
                (!w.LeavingDate.HasValue || w.LeavingDate.Value >= attendanceDate) &&
                w.FarmAssignments.Any(a =>
                    a.OrganizationId == organizationId &&
                    a.FarmId == farmId &&
                    a.IsActive &&
                    a.AssignedFrom <= attendanceDate &&
                    (!a.AssignedTo.HasValue || a.AssignedTo.Value >= attendanceDate)));
            return Task.FromResult(worker);
        }

        public void AddAttendance(LaborAttendance attendance) => Attendances.Add(attendance);

        public void RemoveAttendance(LaborAttendance attendance) => Attendances.Remove(attendance);

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);

        public async Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            var snapshot = Attendances.Select(a => (
                a.Id,
                a.Status,
                a.CalculatedRate,
                a.CalculatedAmount,
                a.CurrencyId,
                a.FinalizedAt,
                a.FinalizedBy
            )).ToList();

            try
            {
                return await operation(cancellationToken);
            }
            catch
            {
                foreach (var snap in snapshot)
                {
                    var att = Attendances.FirstOrDefault(a => a.Id == snap.Id);
                    if (att != null)
                    {
                        typeof(LaborAttendance).GetProperty(nameof(LaborAttendance.Status))!
                            .SetValue(att, snap.Status);
                        typeof(LaborAttendance).GetProperty(nameof(LaborAttendance.CalculatedRate))!
                            .SetValue(att, snap.CalculatedRate);
                        typeof(LaborAttendance).GetProperty(nameof(LaborAttendance.CalculatedAmount))!
                            .SetValue(att, snap.CalculatedAmount);
                        typeof(LaborAttendance).GetProperty(nameof(LaborAttendance.CurrencyId))!
                            .SetValue(att, snap.CurrencyId);
                        typeof(LaborAttendance).GetProperty(nameof(LaborAttendance.FinalizedAt))!
                            .SetValue(att, snap.FinalizedAt);
                        typeof(LaborAttendance).GetProperty(nameof(LaborAttendance.FinalizedBy))!
                            .SetValue(att, snap.FinalizedBy);
                    }
                }
                throw;
            }
        }

        private List<Worker> FilterWorkers(
            Guid organizationId,
            Guid farmId,
            DateOnly attendanceDate,
            string? search)
        {
            var query = Workers.Where(w =>
                w.OrganizationId == organizationId &&
                w.IsActive &&
                (!w.JoiningDate.HasValue || w.JoiningDate.Value <= attendanceDate) &&
                (!w.LeavingDate.HasValue || w.LeavingDate.Value >= attendanceDate) &&
                w.FarmAssignments.Any(a =>
                    a.OrganizationId == organizationId &&
                    a.FarmId == farmId &&
                    a.IsActive &&
                    a.AssignedFrom <= attendanceDate &&
                    (!a.AssignedTo.HasValue || a.AssignedTo.Value >= attendanceDate)));

            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalized = search.Trim().ToLower();
                query = query.Where(w =>
                    w.DisplayName.ToLower().Contains(normalized) ||
                    w.FirstName.ToLower().Contains(normalized) ||
                    (w.LastName != null && w.LastName.ToLower().Contains(normalized)) ||
                    (w.MobileNumber != null && w.MobileNumber.ToLower().Contains(normalized)));
            }

            return query.ToList();
        }
    }

    #endregion
}
