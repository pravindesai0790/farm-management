using FarmManagement.Domain.Entities;
using FarmManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FarmManagement.API.Tests.Domain;

public sealed class WorkerFarmAssignmentModelTests
{
    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly Guid _workerId = Guid.NewGuid();
    private readonly Guid _farmId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public void WorkerFarmAssignment_Create_WithValidData_SetsPropertiesAndDefaultsActive()
    {
        var from = new DateOnly(2026, 1, 1);
        var to = new DateOnly(2026, 6, 30);
        var assignment = new WorkerFarmAssignment(
            organizationId: _organizationId,
            workerId: _workerId,
            farmId: _farmId,
            assignedFrom: from,
            createdBy: _userId,
            assignedTo: to,
            notes: "Assigned to main orchard");

        Assert.NotEqual(Guid.Empty, assignment.Id);
        Assert.Equal(_organizationId, assignment.OrganizationId);
        Assert.Equal(_workerId, assignment.WorkerId);
        Assert.Equal(_farmId, assignment.FarmId);
        Assert.Equal(from, assignment.AssignedFrom);
        Assert.Equal(to, assignment.AssignedTo);
        Assert.Equal("Assigned to main orchard", assignment.Notes);
        Assert.True(assignment.IsActive);
        Assert.Equal(_userId, assignment.CreatedBy);
        Assert.True(assignment.CreatedAt <= DateTimeOffset.UtcNow);
        Assert.Null(assignment.UpdatedAt);
        Assert.Null(assignment.UpdatedBy);
    }

    [Fact]
    public void WorkerFarmAssignment_Create_WithNullOptionalFields_SetsNulls()
    {
        var from = new DateOnly(2026, 1, 1);
        var assignment = new WorkerFarmAssignment(
            organizationId: _organizationId,
            workerId: _workerId,
            farmId: _farmId,
            assignedFrom: from,
            createdBy: _userId);

        Assert.Null(assignment.AssignedTo);
        Assert.Null(assignment.Notes);
        Assert.True(assignment.IsActive);
    }

    [Fact]
    public void WorkerFarmAssignment_Create_ThrowsWhenRequiredGuidsEmpty()
    {
        var from = new DateOnly(2026, 1, 1);

        Assert.Throws<ArgumentException>(() => new WorkerFarmAssignment(Guid.Empty, _workerId, _farmId, from, _userId));
        Assert.Throws<ArgumentException>(() => new WorkerFarmAssignment(_organizationId, Guid.Empty, _farmId, from, _userId));
        Assert.Throws<ArgumentException>(() => new WorkerFarmAssignment(_organizationId, _workerId, Guid.Empty, from, _userId));
        Assert.Throws<ArgumentException>(() => new WorkerFarmAssignment(_organizationId, _workerId, _farmId, from, Guid.Empty));
    }

    [Fact]
    public void WorkerFarmAssignment_Create_ThrowsWhenAssignedToBeforeAssignedFrom()
    {
        var from = new DateOnly(2026, 6, 1);
        var to = new DateOnly(2026, 5, 31);

        var ex = Assert.Throws<ArgumentException>(() =>
            new WorkerFarmAssignment(_organizationId, _workerId, _farmId, from, _userId, assignedTo: to));

        Assert.Equal("assignedTo", ex.ParamName);
    }

    [Fact]
    public void WorkerFarmAssignment_Update_UpdatesPropertiesAndDates()
    {
        var assignment = new WorkerFarmAssignment(
            _organizationId, _workerId, _farmId, new DateOnly(2026, 1, 1), _userId);

        var newFrom = new DateOnly(2026, 2, 1);
        var newTo = new DateOnly(2026, 12, 31);
        var now = DateTimeOffset.UtcNow;
        var updaterId = Guid.NewGuid();

        assignment.Update(newFrom, newTo, "Updated notes", now, updaterId);

        Assert.Equal(newFrom, assignment.AssignedFrom);
        Assert.Equal(newTo, assignment.AssignedTo);
        Assert.Equal("Updated notes", assignment.Notes);
        Assert.Equal(now, assignment.UpdatedAt);
        Assert.Equal(updaterId, assignment.UpdatedBy);
    }

    [Fact]
    public void WorkerFarmAssignment_Update_ThrowsWhenAssignedToBeforeAssignedFrom()
    {
        var assignment = new WorkerFarmAssignment(
            _organizationId, _workerId, _farmId, new DateOnly(2026, 1, 1), _userId);

        var ex = Assert.Throws<ArgumentException>(() =>
            assignment.Update(new DateOnly(2026, 6, 1), new DateOnly(2026, 5, 1), null, DateTimeOffset.UtcNow, _userId));

        Assert.Equal("assignedTo", ex.ParamName);
    }

    [Fact]
    public void WorkerFarmAssignment_Update_ThrowsWhenUserGuidEmpty()
    {
        var assignment = new WorkerFarmAssignment(
            _organizationId, _workerId, _farmId, new DateOnly(2026, 1, 1), _userId);

        Assert.Throws<ArgumentException>(() =>
            assignment.Update(new DateOnly(2026, 1, 1), null, null, DateTimeOffset.UtcNow, Guid.Empty));
    }

    [Fact]
    public void WorkerFarmAssignment_EndAssignment_SetsAssignedToAndDeactivates()
    {
        var assignment = new WorkerFarmAssignment(
            _organizationId, _workerId, _farmId, new DateOnly(2026, 1, 1), _userId);

        var endDate = new DateOnly(2026, 3, 31);
        var now = DateTimeOffset.UtcNow;
        var updaterId = Guid.NewGuid();

        var result = assignment.EndAssignment(endDate, now, updaterId);

        Assert.True(result);
        Assert.False(assignment.IsActive);
        Assert.Equal(endDate, assignment.AssignedTo);
        Assert.Equal(now, assignment.UpdatedAt);
        Assert.Equal(updaterId, assignment.UpdatedBy);
    }

    [Fact]
    public void WorkerFarmAssignment_EndAssignment_ThrowsWhenEndDateBeforeAssignedFrom()
    {
        var assignment = new WorkerFarmAssignment(
            _organizationId, _workerId, _farmId, new DateOnly(2026, 6, 1), _userId);

        var ex = Assert.Throws<ArgumentException>(() =>
            assignment.EndAssignment(new DateOnly(2026, 5, 31), DateTimeOffset.UtcNow, _userId));

        Assert.Equal("endDate", ex.ParamName);
    }

    [Fact]
    public void WorkerFarmAssignment_EndAssignment_ThrowsWhenUserGuidEmpty()
    {
        var assignment = new WorkerFarmAssignment(
            _organizationId, _workerId, _farmId, new DateOnly(2026, 1, 1), _userId);

        Assert.Throws<ArgumentException>(() =>
            assignment.EndAssignment(new DateOnly(2026, 3, 31), DateTimeOffset.UtcNow, Guid.Empty));
    }

    [Fact]
    public void WorkerFarmAssignment_Deactivate_WhenActive_DeactivatesAndReturnsTrue()
    {
        var assignment = new WorkerFarmAssignment(
            _organizationId, _workerId, _farmId, new DateOnly(2026, 1, 1), _userId);

        var now = DateTimeOffset.UtcNow;
        var updaterId = Guid.NewGuid();

        var changed = assignment.Deactivate(now, updaterId);

        Assert.True(changed);
        Assert.False(assignment.IsActive);
        Assert.Equal(now, assignment.UpdatedAt);
        Assert.Equal(updaterId, assignment.UpdatedBy);

        // Subsequent deactivation should return false
        Assert.False(assignment.Deactivate(now, updaterId));
    }

    [Fact]
    public void WorkerFarmAssignment_Activate_WhenInactive_ActivatesAndReturnsTrue()
    {
        var assignment = new WorkerFarmAssignment(
            _organizationId, _workerId, _farmId, new DateOnly(2026, 1, 1), _userId);
        var now = DateTimeOffset.UtcNow;
        var updaterId = Guid.NewGuid();

        // Already active initially
        Assert.False(assignment.Activate(now, updaterId));

        assignment.Deactivate(now, updaterId);
        Assert.False(assignment.IsActive);

        var changed = assignment.Activate(now, updaterId);
        Assert.True(changed);
        Assert.True(assignment.IsActive);
    }

    [Fact]
    public void ApplicationDbContext_Model_BuildsSuccessfullyWithWorkerFarmAssignments()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=farm_test;Username=postgres;Password=postgres")
            .Options;

        using var dbContext = new ApplicationDbContext(options);
        var model = dbContext.Model;

        var assignmentEntity = model.FindEntityType(typeof(WorkerFarmAssignment));
        Assert.NotNull(assignmentEntity);
        Assert.Equal("worker_farm_assignments", assignmentEntity.GetTableName());

        // Invariant: workers table must NOT contain a farm_id column
        var workerEntity = model.FindEntityType(typeof(Worker));
        Assert.NotNull(workerEntity);
        Assert.Null(workerEntity.FindProperty("farm_id"));
        Assert.Null(workerEntity.FindProperty("FarmId"));

        // Verify foreign key to Organization with Restrict
        var orgEntity = model.FindEntityType(typeof(Organization));
        Assert.NotNull(orgEntity);
        var orgFk = assignmentEntity.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType == orgEntity);
        Assert.NotNull(orgFk);
        Assert.Equal(DeleteBehavior.Restrict, orgFk.DeleteBehavior);

        // Verify foreign key to Worker with Restrict
        var workerFk = assignmentEntity.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType == workerEntity);
        Assert.NotNull(workerFk);
        Assert.Equal(DeleteBehavior.Restrict, workerFk.DeleteBehavior);

        // Verify foreign key to Farm with Restrict
        var farmEntity = model.FindEntityType(typeof(Farm));
        Assert.NotNull(farmEntity);
        var farmFk = assignmentEntity.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType == farmEntity);
        Assert.NotNull(farmFk);
        Assert.Equal(DeleteBehavior.Restrict, farmFk.DeleteBehavior);

        // Verify entity properties
        Assert.NotNull(assignmentEntity.FindProperty("Id"));
        Assert.NotNull(assignmentEntity.FindProperty("OrganizationId"));
        Assert.NotNull(assignmentEntity.FindProperty("WorkerId"));
        Assert.NotNull(assignmentEntity.FindProperty("FarmId"));
        Assert.NotNull(assignmentEntity.FindProperty("AssignedFrom"));
        Assert.NotNull(assignmentEntity.FindProperty("AssignedTo"));
        Assert.NotNull(assignmentEntity.FindProperty("Notes"));
        Assert.NotNull(assignmentEntity.FindProperty("IsActive"));
        Assert.NotNull(assignmentEntity.FindProperty("CreatedAt"));
        Assert.NotNull(assignmentEntity.FindProperty("CreatedBy"));
        Assert.NotNull(assignmentEntity.FindProperty("UpdatedAt"));
        Assert.NotNull(assignmentEntity.FindProperty("UpdatedBy"));

        // Verify navigations on Worker and Farm
        Assert.NotNull(workerEntity.FindNavigation("FarmAssignments"));
        Assert.NotNull(farmEntity.FindNavigation("WorkerAssignments"));

        // Verify indexes
        var indexes = assignmentEntity.GetIndexes().ToList();
        Assert.NotNull(indexes.FirstOrDefault(ix => ix.GetDatabaseName() == "ix_worker_farm_assignments_organization_id"));
        Assert.NotNull(indexes.FirstOrDefault(ix => ix.GetDatabaseName() == "ix_worker_farm_assignments_org_worker"));
        Assert.NotNull(indexes.FirstOrDefault(ix => ix.GetDatabaseName() == "ix_worker_farm_assignments_org_farm"));
        Assert.NotNull(indexes.FirstOrDefault(ix => ix.GetDatabaseName() == "ix_worker_farm_assignments_org_worker_active"));
        Assert.NotNull(indexes.FirstOrDefault(ix => ix.GetDatabaseName() == "ix_worker_farm_assignments_org_farm_active"));
    }
}
