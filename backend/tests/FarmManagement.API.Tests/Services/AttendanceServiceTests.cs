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
        var service = new AttendanceService(store);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            service.GetEligibleWorkersAsync(CreateActor(), Guid.NewGuid(), new DateOnly(2026, 9, 12)));
    }

    [Fact]
    public async Task GetEligibleWorkersAsync_WhenFarmFromDifferentOrg_ThrowsResourceNotFoundException()
    {
        var store = new FakeAttendanceStore();
        var farm = CreateFarm(organizationId: _otherOrgId);
        store.Farms.Add(farm);

        var service = new AttendanceService(store);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            service.GetEligibleWorkersAsync(CreateActor(), farm.Id, new DateOnly(2026, 9, 12)));
    }

    [Fact]
    public async Task GetEligibleWorkersAsync_WhenFarmInactive_ThrowsValidationException()
    {
        var store = new FakeAttendanceStore();
        var farm = CreateFarm(isActive: false);
        store.Farms.Add(farm);

        var service = new AttendanceService(store);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.GetEligibleWorkersAsync(CreateActor(), farm.Id, new DateOnly(2026, 9, 12)));

        Assert.NotNull(ex.Errors);
        Assert.True(ex.Errors.ContainsKey("farmId"));
    }

    [Fact]
    public async Task GetEligibleWorkersAsync_WhenActorInvalid_ThrowsUnauthorizedAccessException()
    {
        var store = new FakeAttendanceStore();
        var service = new AttendanceService(store);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.GetEligibleWorkersAsync(new AttendanceActor(Guid.Empty, _organizationId), Guid.NewGuid(), new DateOnly(2026, 9, 12)));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.GetEligibleWorkersAsync(new AttendanceActor(_userId, Guid.Empty), Guid.NewGuid(), new DateOnly(2026, 9, 12)));
    }

    [Fact]
    public async Task GetEligibleWorkersAsync_WhenFarmIdEmpty_ThrowsValidationException()
    {
        var store = new FakeAttendanceStore();
        var service = new AttendanceService(store);

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

        var service = new AttendanceService(store);

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

        var service = new AttendanceService(store);

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

        var service = new AttendanceService(store);
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

        var service = new AttendanceService(store);
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

        var service = new AttendanceService(store);

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

        var service = new AttendanceService(store);

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

        var service = new AttendanceService(store);
        var result = await service.GetEligibleWorkersAsync(CreateActor(), farm.Id, new DateOnly(2026, 9, 12));

        Assert.Single(result.Items);
        Assert.Equal(0, store.AttendanceCreatedCount);
        Assert.Equal(0, store.EarningsCreatedCount);
    }

    #region Fake Store

    private sealed class FakeAttendanceStore : IAttendanceStore
    {
        public List<Worker> Workers { get; } = [];
        public List<Farm> Farms { get; } = [];
        public int AttendanceCreatedCount { get; private set; }
        public int EarningsCreatedCount { get; private set; }

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
