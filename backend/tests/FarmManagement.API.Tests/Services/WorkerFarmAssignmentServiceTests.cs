using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.Labor;
using FarmManagement.Application.Interfaces.Labor;
using FarmManagement.Application.Services;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Services;

public sealed class WorkerFarmAssignmentServiceTests
{
    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly Guid _otherOrgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _ownershipTypeId = Guid.NewGuid();

    private WorkerActor CreateActor(Guid? organizationId = null) =>
        new(_userId, organizationId ?? _organizationId);

    private Worker CreateWorker(Guid? organizationId = null, bool isActive = true)
    {
        var worker = new Worker(
            organizationId ?? _organizationId,
            "Ramesh",
            "Patil",
            Gender.Male,
            EmploymentType.Permanent,
            _userId);

        if (!isActive)
        {
            worker.Deactivate(DateTimeOffset.UtcNow, _userId);
        }

        return worker;
    }

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

    [Fact]
    public async Task ListByWorkerAsync_WhenWorkerNotFound_ThrowsResourceNotFoundException()
    {
        var store = new FakeWorkerFarmAssignmentStore();
        var service = new WorkerFarmAssignmentService(store);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            service.ListByWorkerAsync(CreateActor(), Guid.NewGuid(), null));
    }

    [Fact]
    public async Task ListByWorkerAsync_WhenWorkerFromDifferentOrg_ThrowsResourceNotFoundException()
    {
        var store = new FakeWorkerFarmAssignmentStore();
        var service = new WorkerFarmAssignmentService(store);
        var worker = CreateWorker(organizationId: _otherOrgId);
        store.Workers.Add(worker);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            service.ListByWorkerAsync(CreateActor(), worker.Id, null));
    }

    [Fact]
    public async Task ListByWorkerAsync_WithFilter_ReturnsMatchingAssignments()
    {
        var store = new FakeWorkerFarmAssignmentStore();
        var service = new WorkerFarmAssignmentService(store);
        var worker = CreateWorker();
        var farm1 = CreateFarm(code: "F-01");
        var farm2 = CreateFarm(code: "F-02");
        store.Workers.Add(worker);
        store.Farms.Add(farm1);
        store.Farms.Add(farm2);

        var activeAssignment = new WorkerFarmAssignment(
            _organizationId, worker.Id, farm1.Id, new DateOnly(2026, 1, 1), _userId);
        var inactiveAssignment = new WorkerFarmAssignment(
            _organizationId, worker.Id, farm2.Id, new DateOnly(2025, 1, 1), _userId, assignedTo: new DateOnly(2025, 12, 31));
        inactiveAssignment.Deactivate(DateTimeOffset.UtcNow, _userId);

        store.Assignments.Add(activeAssignment);
        store.Assignments.Add(inactiveAssignment);

        var all = await service.ListByWorkerAsync(CreateActor(), worker.Id, null);
        Assert.Equal(2, all.Count);

        var activeOnly = await service.ListByWorkerAsync(CreateActor(), worker.Id, true);
        Assert.Single(activeOnly);
        Assert.Equal(activeAssignment.Id, activeOnly[0].Id);

        var inactiveOnly = await service.ListByWorkerAsync(CreateActor(), worker.Id, false);
        Assert.Single(inactiveOnly);
        Assert.Equal(inactiveAssignment.Id, inactiveOnly[0].Id);
    }

    [Fact]
    public async Task ListByFarmAsync_WhenFarmNotFound_ThrowsResourceNotFoundException()
    {
        var store = new FakeWorkerFarmAssignmentStore();
        var service = new WorkerFarmAssignmentService(store);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            service.ListByFarmAsync(CreateActor(), Guid.NewGuid(), null));
    }

    [Fact]
    public async Task ListByFarmAsync_WhenFarmFromDifferentOrg_ThrowsResourceNotFoundException()
    {
        var store = new FakeWorkerFarmAssignmentStore();
        var service = new WorkerFarmAssignmentService(store);
        var farm = CreateFarm(organizationId: _otherOrgId);
        store.Farms.Add(farm);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            service.ListByFarmAsync(CreateActor(), farm.Id, null));
    }

    [Fact]
    public async Task ListByFarmAsync_WithFilter_ReturnsMatchingAssignments()
    {
        var store = new FakeWorkerFarmAssignmentStore();
        var service = new WorkerFarmAssignmentService(store);
        var farm = CreateFarm();
        var worker1 = CreateWorker();
        var worker2 = CreateWorker();
        store.Farms.Add(farm);
        store.Workers.Add(worker1);
        store.Workers.Add(worker2);

        var assignment1 = new WorkerFarmAssignment(
            _organizationId, worker1.Id, farm.Id, new DateOnly(2026, 1, 1), _userId);
        var assignment2 = new WorkerFarmAssignment(
            _organizationId, worker2.Id, farm.Id, new DateOnly(2026, 2, 1), _userId);
        assignment2.Deactivate(DateTimeOffset.UtcNow, _userId);

        store.Assignments.Add(assignment1);
        store.Assignments.Add(assignment2);

        var all = await service.ListByFarmAsync(CreateActor(), farm.Id, null);
        Assert.Equal(2, all.Count);

        var activeOnly = await service.ListByFarmAsync(CreateActor(), farm.Id, true);
        Assert.Single(activeOnly);
        Assert.Equal(assignment1.Id, activeOnly[0].Id);
    }

    [Fact]
    public async Task GetAsync_WhenAssignmentNotFound_ThrowsResourceNotFoundException()
    {
        var store = new FakeWorkerFarmAssignmentStore();
        var service = new WorkerFarmAssignmentService(store);
        var worker = CreateWorker();
        store.Workers.Add(worker);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            service.GetAsync(CreateActor(), worker.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task GetAsync_WhenAssignmentBelongsToDifferentWorker_ThrowsResourceNotFoundException()
    {
        var store = new FakeWorkerFarmAssignmentStore();
        var service = new WorkerFarmAssignmentService(store);
        var worker1 = CreateWorker();
        var worker2 = CreateWorker();
        var farm = CreateFarm();
        store.Workers.Add(worker1);
        store.Workers.Add(worker2);
        store.Farms.Add(farm);

        var assignment = new WorkerFarmAssignment(
            _organizationId, worker1.Id, farm.Id, new DateOnly(2026, 1, 1), _userId);
        store.Assignments.Add(assignment);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            service.GetAsync(CreateActor(), worker2.Id, assignment.Id));
    }

    [Fact]
    public async Task GetAsync_WhenValid_ReturnsAssignmentResponse()
    {
        var store = new FakeWorkerFarmAssignmentStore();
        var service = new WorkerFarmAssignmentService(store);
        var worker = CreateWorker();
        var farm = CreateFarm();
        store.Workers.Add(worker);
        store.Farms.Add(farm);

        var assignment = new WorkerFarmAssignment(
            _organizationId, worker.Id, farm.Id, new DateOnly(2026, 1, 1), _userId, notes: "Test assignment");
        store.Assignments.Add(assignment);

        var result = await service.GetAsync(CreateActor(), worker.Id, assignment.Id);

        Assert.NotNull(result);
        Assert.Equal(assignment.Id, result.Id);
        Assert.Equal(worker.Id, result.WorkerId);
        Assert.Equal(farm.Id, result.FarmId);
        Assert.Equal("Test assignment", result.Notes);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesAssignmentAndAuditLog()
    {
        var store = new FakeWorkerFarmAssignmentStore();
        var service = new WorkerFarmAssignmentService(store);
        var worker = CreateWorker();
        var farm = CreateFarm();
        store.Workers.Add(worker);
        store.Farms.Add(farm);

        var request = new CreateWorkerFarmAssignmentRequest(
            FarmId: farm.Id,
            AssignedFrom: new DateOnly(2026, 3, 1),
            AssignedTo: new DateOnly(2026, 9, 30),
            Notes: "Seasonal assignment");

        var response = await service.CreateAsync(CreateActor(), worker.Id, request, "127.0.0.1");

        Assert.NotNull(response);
        Assert.Equal(_organizationId, response.OrganizationId);
        Assert.Equal(worker.Id, response.WorkerId);
        Assert.Equal(farm.Id, response.FarmId);
        Assert.Equal(new DateOnly(2026, 3, 1), response.AssignedFrom);
        Assert.Equal(new DateOnly(2026, 9, 30), response.AssignedTo);
        Assert.Equal("Seasonal assignment", response.Notes);
        Assert.True(response.IsActive);

        Assert.Single(store.Assignments);
        Assert.Single(store.AuditLogs);
        Assert.Equal("WorkerFarmAssignment.Created", store.AuditLogs[0].Action);
        Assert.Equal("WorkerFarmAssignment", store.AuditLogs[0].EntityType);
    }

    [Fact]
    public async Task CreateAsync_WhenNullRequest_ThrowsValidationException()
    {
        var store = new FakeWorkerFarmAssignmentStore();
        var service = new WorkerFarmAssignmentService(store);
        var worker = CreateWorker();
        store.Workers.Add(worker);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(CreateActor(), worker.Id, null!, "127.0.0.1"));

        Assert.NotNull(ex.Errors);
        Assert.Contains("request", ex.Errors.Keys);
    }

    [Fact]
    public async Task CreateAsync_WhenFarmIdMissing_ThrowsValidationException()
    {
        var store = new FakeWorkerFarmAssignmentStore();
        var service = new WorkerFarmAssignmentService(store);
        var worker = CreateWorker();
        store.Workers.Add(worker);

        var request = new CreateWorkerFarmAssignmentRequest(
            FarmId: null,
            AssignedFrom: new DateOnly(2026, 1, 1),
            AssignedTo: null,
            Notes: null);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(CreateActor(), worker.Id, request, "127.0.0.1"));

        Assert.NotNull(ex.Errors);
        Assert.Contains("farmId", ex.Errors.Keys);
    }

    [Fact]
    public async Task CreateAsync_WhenAssignedFromMissing_ThrowsValidationException()
    {
        var store = new FakeWorkerFarmAssignmentStore();
        var service = new WorkerFarmAssignmentService(store);
        var worker = CreateWorker();
        var farm = CreateFarm();
        store.Workers.Add(worker);
        store.Farms.Add(farm);

        var request = new CreateWorkerFarmAssignmentRequest(
            FarmId: farm.Id,
            AssignedFrom: null,
            AssignedTo: null,
            Notes: null);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(CreateActor(), worker.Id, request, "127.0.0.1"));

        Assert.NotNull(ex.Errors);
        Assert.Contains("assignedFrom", ex.Errors.Keys);
    }

    [Fact]
    public async Task CreateAsync_WhenWorkerNotFound_ThrowsResourceNotFoundException()
    {
        var store = new FakeWorkerFarmAssignmentStore();
        var service = new WorkerFarmAssignmentService(store);
        var farm = CreateFarm();
        store.Farms.Add(farm);

        var request = new CreateWorkerFarmAssignmentRequest(
            FarmId: farm.Id,
            AssignedFrom: new DateOnly(2026, 1, 1),
            AssignedTo: null,
            Notes: null);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            service.CreateAsync(CreateActor(), Guid.NewGuid(), request, "127.0.0.1"));
    }

    [Fact]
    public async Task CreateAsync_WhenWorkerInactive_ThrowsValidationException()
    {
        var store = new FakeWorkerFarmAssignmentStore();
        var service = new WorkerFarmAssignmentService(store);
        var worker = CreateWorker(isActive: false);
        var farm = CreateFarm();
        store.Workers.Add(worker);
        store.Farms.Add(farm);

        var request = new CreateWorkerFarmAssignmentRequest(
            FarmId: farm.Id,
            AssignedFrom: new DateOnly(2026, 1, 1),
            AssignedTo: null,
            Notes: null);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(CreateActor(), worker.Id, request, "127.0.0.1"));

        Assert.NotNull(ex.Errors);
        Assert.Contains("workerId", ex.Errors.Keys);
    }

    [Fact]
    public async Task CreateAsync_WhenFarmNotFound_ThrowsValidationException()
    {
        var store = new FakeWorkerFarmAssignmentStore();
        var service = new WorkerFarmAssignmentService(store);
        var worker = CreateWorker();
        store.Workers.Add(worker);

        var request = new CreateWorkerFarmAssignmentRequest(
            FarmId: Guid.NewGuid(),
            AssignedFrom: new DateOnly(2026, 1, 1),
            AssignedTo: null,
            Notes: null);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(CreateActor(), worker.Id, request, "127.0.0.1"));

        Assert.NotNull(ex.Errors);
        Assert.Contains("farmId", ex.Errors.Keys);
    }

    [Fact]
    public async Task CreateAsync_WhenFarmInactive_ThrowsValidationException()
    {
        var store = new FakeWorkerFarmAssignmentStore();
        var service = new WorkerFarmAssignmentService(store);
        var worker = CreateWorker();
        var farm = CreateFarm(isActive: false);
        store.Workers.Add(worker);
        store.Farms.Add(farm);

        var request = new CreateWorkerFarmAssignmentRequest(
            FarmId: farm.Id,
            AssignedFrom: new DateOnly(2026, 1, 1),
            AssignedTo: null,
            Notes: null);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(CreateActor(), worker.Id, request, "127.0.0.1"));

        Assert.NotNull(ex.Errors);
        Assert.Contains("farmId", ex.Errors.Keys);
    }

    [Fact]
    public async Task CreateAsync_WhenAssignedToBeforeAssignedFrom_ThrowsValidationException()
    {
        var store = new FakeWorkerFarmAssignmentStore();
        var service = new WorkerFarmAssignmentService(store);
        var worker = CreateWorker();
        var farm = CreateFarm();
        store.Workers.Add(worker);
        store.Farms.Add(farm);

        var request = new CreateWorkerFarmAssignmentRequest(
            FarmId: farm.Id,
            AssignedFrom: new DateOnly(2026, 6, 1),
            AssignedTo: new DateOnly(2026, 5, 1),
            Notes: null);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(CreateActor(), worker.Id, request, "127.0.0.1"));

        Assert.NotNull(ex.Errors);
        Assert.Contains("assignedTo", ex.Errors.Keys);
    }

    [Fact]
    public async Task CreateAsync_WhenWorkerFromDifferentOrganization_ThrowsResourceNotFoundException()
    {
        var store = new FakeWorkerFarmAssignmentStore();
        var service = new WorkerFarmAssignmentService(store);
        var worker = CreateWorker(organizationId: _otherOrgId);
        var farm = CreateFarm();
        store.Workers.Add(worker);
        store.Farms.Add(farm);

        var request = new CreateWorkerFarmAssignmentRequest(
            FarmId: farm.Id,
            AssignedFrom: new DateOnly(2026, 1, 1),
            AssignedTo: null,
            Notes: null);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            service.CreateAsync(CreateActor(), worker.Id, request, "127.0.0.1"));
    }

    [Fact]
    public async Task CreateAsync_WhenFarmFromDifferentOrganization_ThrowsValidationException()
    {
        var store = new FakeWorkerFarmAssignmentStore();
        var service = new WorkerFarmAssignmentService(store);
        var worker = CreateWorker();
        var farm = CreateFarm(organizationId: _otherOrgId);
        store.Workers.Add(worker);
        store.Farms.Add(farm);

        var request = new CreateWorkerFarmAssignmentRequest(
            FarmId: farm.Id,
            AssignedFrom: new DateOnly(2026, 1, 1),
            AssignedTo: null,
            Notes: null);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(CreateActor(), worker.Id, request, "127.0.0.1"));

        Assert.NotNull(ex.Errors);
        Assert.Contains("farmId", ex.Errors.Keys);
    }

    [Fact]
    public async Task UpdateAsync_WithValidRequest_UpdatesDatesAndNotesAndAuditLog()
    {
        var store = new FakeWorkerFarmAssignmentStore();
        var service = new WorkerFarmAssignmentService(store);
        var worker = CreateWorker();
        var farm = CreateFarm();
        store.Workers.Add(worker);
        store.Farms.Add(farm);

        var assignment = new WorkerFarmAssignment(
            _organizationId, worker.Id, farm.Id, new DateOnly(2026, 1, 1), _userId);
        store.Assignments.Add(assignment);

        var request = new UpdateWorkerFarmAssignmentRequest(
            AssignedFrom: new DateOnly(2026, 2, 1),
            AssignedTo: new DateOnly(2026, 11, 30),
            Notes: "Updated assignment notes");

        var response = await service.UpdateAsync(CreateActor(), worker.Id, assignment.Id, request, "127.0.0.1");

        Assert.NotNull(response);
        Assert.Equal(new DateOnly(2026, 2, 1), response.AssignedFrom);
        Assert.Equal(new DateOnly(2026, 11, 30), response.AssignedTo);
        Assert.Equal("Updated assignment notes", response.Notes);

        Assert.Single(store.AuditLogs);
        Assert.Equal("WorkerFarmAssignment.Updated", store.AuditLogs[0].Action);
    }

    [Fact]
    public async Task UpdateAsync_WhenAssignedToBeforeAssignedFrom_ThrowsValidationException()
    {
        var store = new FakeWorkerFarmAssignmentStore();
        var service = new WorkerFarmAssignmentService(store);
        var worker = CreateWorker();
        var farm = CreateFarm();
        store.Workers.Add(worker);
        store.Farms.Add(farm);

        var assignment = new WorkerFarmAssignment(
            _organizationId, worker.Id, farm.Id, new DateOnly(2026, 1, 1), _userId);
        store.Assignments.Add(assignment);

        var request = new UpdateWorkerFarmAssignmentRequest(
            AssignedFrom: new DateOnly(2026, 6, 1),
            AssignedTo: new DateOnly(2026, 5, 1),
            Notes: null);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpdateAsync(CreateActor(), worker.Id, assignment.Id, request, "127.0.0.1"));

        Assert.NotNull(ex.Errors);
        Assert.Contains("assignedTo", ex.Errors.Keys);
    }

    [Fact]
    public async Task EndAssignmentAsync_WithExplicitEndDate_SetsEndDateAndDeactivatesAndLogsAudit()
    {
        var store = new FakeWorkerFarmAssignmentStore();
        var service = new WorkerFarmAssignmentService(store);
        var worker = CreateWorker();
        var farm = CreateFarm();
        store.Workers.Add(worker);
        store.Farms.Add(farm);

        var assignment = new WorkerFarmAssignment(
            _organizationId, worker.Id, farm.Id, new DateOnly(2026, 1, 1), _userId);
        store.Assignments.Add(assignment);

        var request = new EndWorkerFarmAssignmentRequest(new DateOnly(2026, 5, 31));

        var response = await service.EndAssignmentAsync(CreateActor(), worker.Id, assignment.Id, request, "127.0.0.1");

        Assert.NotNull(response);
        Assert.False(response.IsActive);
        Assert.Equal(new DateOnly(2026, 5, 31), response.AssignedTo);

        Assert.Single(store.AuditLogs);
        Assert.Equal("WorkerFarmAssignment.Ended", store.AuditLogs[0].Action);
    }

    [Fact]
    public async Task EndAssignmentAsync_WhenEndDateBeforeAssignedFrom_ThrowsValidationException()
    {
        var store = new FakeWorkerFarmAssignmentStore();
        var service = new WorkerFarmAssignmentService(store);
        var worker = CreateWorker();
        var farm = CreateFarm();
        store.Workers.Add(worker);
        store.Farms.Add(farm);

        var assignment = new WorkerFarmAssignment(
            _organizationId, worker.Id, farm.Id, new DateOnly(2026, 6, 1), _userId);
        store.Assignments.Add(assignment);

        var request = new EndWorkerFarmAssignmentRequest(new DateOnly(2026, 5, 31));

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.EndAssignmentAsync(CreateActor(), worker.Id, assignment.Id, request, "127.0.0.1"));

        Assert.NotNull(ex.Errors);
        Assert.Contains("endDate", ex.Errors.Keys);
    }

    [Fact]
    public async Task DeactivateAsync_WhenActive_DeactivatesAndLogsAudit()
    {
        var store = new FakeWorkerFarmAssignmentStore();
        var service = new WorkerFarmAssignmentService(store);
        var worker = CreateWorker();
        var farm = CreateFarm();
        store.Workers.Add(worker);
        store.Farms.Add(farm);

        var assignment = new WorkerFarmAssignment(
            _organizationId, worker.Id, farm.Id, new DateOnly(2026, 1, 1), _userId);
        store.Assignments.Add(assignment);

        var result = await service.DeactivateAsync(CreateActor(), worker.Id, assignment.Id, "127.0.0.1");

        Assert.True(result);
        Assert.False(assignment.IsActive);
        Assert.Single(store.AuditLogs);
        Assert.Equal("WorkerFarmAssignment.Deactivated", store.AuditLogs[0].Action);

        // Deactivating again returns false and produces no new audit log
        var secondResult = await service.DeactivateAsync(CreateActor(), worker.Id, assignment.Id, "127.0.0.1");
        Assert.False(secondResult);
        Assert.Single(store.AuditLogs);
    }

    [Fact]
    public async Task ActivateAsync_WhenInactive_ActivatesAndLogsAudit()
    {
        var store = new FakeWorkerFarmAssignmentStore();
        var service = new WorkerFarmAssignmentService(store);
        var worker = CreateWorker();
        var farm = CreateFarm();
        store.Workers.Add(worker);
        store.Farms.Add(farm);

        var assignment = new WorkerFarmAssignment(
            _organizationId, worker.Id, farm.Id, new DateOnly(2026, 1, 1), _userId);
        assignment.Deactivate(DateTimeOffset.UtcNow, _userId);
        store.Assignments.Add(assignment);

        var result = await service.ActivateAsync(CreateActor(), worker.Id, assignment.Id, "127.0.0.1");

        Assert.True(result);
        Assert.True(assignment.IsActive);
        Assert.Single(store.AuditLogs);
        Assert.Equal("WorkerFarmAssignment.Activated", store.AuditLogs[0].Action);

        // Activating again returns false
        var secondResult = await service.ActivateAsync(CreateActor(), worker.Id, assignment.Id, "127.0.0.1");
        Assert.False(secondResult);
        Assert.Single(store.AuditLogs);
    }

    #region Fake Store

    private sealed class FakeWorkerFarmAssignmentStore : IWorkerFarmAssignmentStore
    {
        public List<WorkerFarmAssignment> Assignments { get; } = [];
        public List<Worker> Workers { get; } = [];
        public List<Farm> Farms { get; } = [];
        public List<AuditLog> AuditLogs { get; } = [];

        public Task<IReadOnlyList<WorkerFarmAssignment>> ListByWorkerAsync(
            Guid organizationId,
            Guid workerId,
            bool? isActive,
            CancellationToken cancellationToken = default)
        {
            var query = Assignments.Where(a => a.OrganizationId == organizationId && a.WorkerId == workerId);
            if (isActive.HasValue)
            {
                query = query.Where(a => a.IsActive == isActive.Value);
            }

            var result = query.OrderByDescending(a => a.AssignedFrom).ToList();
            return Task.FromResult<IReadOnlyList<WorkerFarmAssignment>>(result);
        }

        public Task<IReadOnlyList<WorkerFarmAssignment>> ListByFarmAsync(
            Guid organizationId,
            Guid farmId,
            bool? isActive,
            CancellationToken cancellationToken = default)
        {
            var query = Assignments.Where(a => a.OrganizationId == organizationId && a.FarmId == farmId);
            if (isActive.HasValue)
            {
                query = query.Where(a => a.IsActive == isActive.Value);
            }

            var result = query.OrderByDescending(a => a.AssignedFrom).ToList();
            return Task.FromResult<IReadOnlyList<WorkerFarmAssignment>>(result);
        }

        public Task<WorkerFarmAssignment?> FindAsync(
            Guid assignmentId,
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            var assignment = Assignments.FirstOrDefault(a => a.Id == assignmentId && a.OrganizationId == organizationId);
            return Task.FromResult(assignment);
        }

        public Task<Worker?> FindWorkerAsync(
            Guid workerId,
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            var worker = Workers.FirstOrDefault(w => w.Id == workerId && w.OrganizationId == organizationId);
            return Task.FromResult(worker);
        }

        public Task<Farm?> FindFarmAsync(
            Guid farmId,
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            var farm = Farms.FirstOrDefault(f => f.Id == farmId && f.OrganizationId == organizationId);
            return Task.FromResult(farm);
        }

        public void Add(WorkerFarmAssignment assignment) => Assignments.Add(assignment);

        public void AddAuditLog(AuditLog auditLog) => AuditLogs.Add(auditLog);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    #endregion
}
