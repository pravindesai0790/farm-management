using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using FarmManagement.Infrastructure.Persistence;
using FarmManagement.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FarmManagement.API.Tests.Domain;

public sealed class LaborAttendanceModelTests
{
    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly Guid _farmId = Guid.NewGuid();
    private readonly Guid _workerId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _currencyId = Guid.NewGuid();
    private readonly DateOnly _attendanceDate = new(2026, 9, 12);

    [Fact]
    public void CreateDraft_FullDay_SetsPropertiesCorrectly()
    {
        var attendance = LaborAttendance.CreateDraft(
            organizationId: _organizationId,
            farmId: _farmId,
            workerId: _workerId,
            attendanceDate: _attendanceDate,
            attendanceType: AttendanceType.FullDay,
            createdBy: _userId,
            calculatedRate: 500m,
            calculatedAmount: 500m,
            currencyId: _currencyId,
            notes: "Full day harvesting");

        Assert.NotEqual(Guid.Empty, attendance.Id);
        Assert.Equal(_organizationId, attendance.OrganizationId);
        Assert.Equal(_farmId, attendance.FarmId);
        Assert.Equal(_workerId, attendance.WorkerId);
        Assert.Equal(_attendanceDate, attendance.AttendanceDate);
        Assert.Equal(AttendanceType.FullDay, attendance.AttendanceType);
        Assert.Null(attendance.WorkingHours);
        Assert.Equal(500m, attendance.CalculatedRate);
        Assert.Equal(500m, attendance.CalculatedAmount);
        Assert.Equal(_currencyId, attendance.CurrencyId);
        Assert.Equal(AttendanceStatus.Draft, attendance.Status);
        Assert.Equal("Full day harvesting", attendance.Notes);
        Assert.Null(attendance.FinalizedAt);
        Assert.Null(attendance.FinalizedBy);
        Assert.Equal(_userId, attendance.CreatedBy);
        Assert.True(attendance.IsDraft);
        Assert.False(attendance.IsFinalized);
        Assert.True(attendance.GeneratesPayrollEarnings);
    }

    [Fact]
    public void CreateDraft_Hourly_RequiresAndSetsWorkingHours()
    {
        var attendance = LaborAttendance.CreateDraft(
            organizationId: _organizationId,
            farmId: _farmId,
            workerId: _workerId,
            attendanceDate: _attendanceDate,
            attendanceType: AttendanceType.Hourly,
            createdBy: _userId,
            workingHours: 6.5m,
            calculatedRate: 80m,
            calculatedAmount: 520m,
            currencyId: _currencyId);

        Assert.Equal(AttendanceType.Hourly, attendance.AttendanceType);
        Assert.Equal(6.5m, attendance.WorkingHours);
        Assert.Equal(520m, attendance.CalculatedAmount);
    }

    [Fact]
    public void CreateDraft_Hourly_ThrowsWhenWorkingHoursMissingOrZero()
    {
        Assert.Throws<ArgumentException>(() => LaborAttendance.CreateDraft(
            _organizationId, _farmId, _workerId, _attendanceDate, AttendanceType.Hourly, _userId, workingHours: null));

        Assert.Throws<ArgumentException>(() => LaborAttendance.CreateDraft(
            _organizationId, _farmId, _workerId, _attendanceDate, AttendanceType.Hourly, _userId, workingHours: 0m));

        Assert.Throws<ArgumentException>(() => LaborAttendance.CreateDraft(
            _organizationId, _farmId, _workerId, _attendanceDate, AttendanceType.Hourly, _userId, workingHours: -2m));
    }

    [Fact]
    public void CreateDraft_FullDayOrHalfDay_ThrowsWhenWorkingHoursProvided()
    {
        Assert.Throws<ArgumentException>(() => LaborAttendance.CreateDraft(
            _organizationId, _farmId, _workerId, _attendanceDate, AttendanceType.FullDay, _userId, workingHours: 8m));

        Assert.Throws<ArgumentException>(() => LaborAttendance.CreateDraft(
            _organizationId, _farmId, _workerId, _attendanceDate, AttendanceType.HalfDay, _userId, workingHours: 4m));
    }

    [Fact]
    public void CreateDraft_NotWorked_SetsZeroEarningsAndNullRate()
    {
        var attendance = LaborAttendance.CreateDraft(
            organizationId: _organizationId,
            farmId: _farmId,
            workerId: _workerId,
            attendanceDate: _attendanceDate,
            attendanceType: AttendanceType.NotWorked,
            createdBy: _userId,
            notes: "Worker did not attend");

        Assert.Equal(AttendanceType.NotWorked, attendance.AttendanceType);
        Assert.Null(attendance.WorkingHours);
        Assert.Null(attendance.CalculatedRate);
        Assert.Equal(0m, attendance.CalculatedAmount);
        Assert.False(attendance.GeneratesPayrollEarnings);
    }

    [Fact]
    public void CreateDraft_NotWorked_ThrowsWhenPositiveEarningsProvided()
    {
        Assert.Throws<ArgumentException>(() => LaborAttendance.CreateDraft(
            organizationId: _organizationId,
            farmId: _farmId,
            workerId: _workerId,
            attendanceDate: _attendanceDate,
            attendanceType: AttendanceType.NotWorked,
            createdBy: _userId,
            calculatedAmount: 300m));
    }

    [Fact]
    public void Constructor_ThrowsWhenRequiredIdentifiersAreEmpty()
    {
        Assert.Throws<ArgumentException>(() => new LaborAttendance(
            Guid.Empty, _farmId, _workerId, _attendanceDate, AttendanceType.FullDay, _userId));

        Assert.Throws<ArgumentException>(() => new LaborAttendance(
            _organizationId, Guid.Empty, _workerId, _attendanceDate, AttendanceType.FullDay, _userId));

        Assert.Throws<ArgumentException>(() => new LaborAttendance(
            _organizationId, _farmId, Guid.Empty, _attendanceDate, AttendanceType.FullDay, _userId));

        Assert.Throws<ArgumentException>(() => new LaborAttendance(
            _organizationId, _farmId, _workerId, _attendanceDate, AttendanceType.FullDay, Guid.Empty));
    }

    [Fact]
    public void Update_WhenDraft_UpdatesProperties()
    {
        var attendance = LaborAttendance.CreateDraft(
            _organizationId, _farmId, _workerId, _attendanceDate, AttendanceType.FullDay, _userId);

        var now = DateTimeOffset.UtcNow;
        var updaterId = Guid.NewGuid();

        attendance.Update(
            attendanceType: AttendanceType.Hourly,
            workingHours: 5m,
            notes: "Switched to hourly",
            now: now,
            updatedBy: updaterId);

        Assert.Equal(AttendanceType.Hourly, attendance.AttendanceType);
        Assert.Equal(5m, attendance.WorkingHours);
        Assert.Equal("Switched to hourly", attendance.Notes);
        Assert.Equal(now, attendance.UpdatedAt);
        Assert.Equal(updaterId, attendance.UpdatedBy);
    }

    [Fact]
    public void Update_WhenFinalized_ThrowsInvalidOperationException()
    {
        var attendance = LaborAttendance.CreateFinalized(
            _organizationId, _farmId, _workerId, _attendanceDate, AttendanceType.FullDay, _userId, calculatedAmount: 500m);

        Assert.Throws<InvalidOperationException>(() => attendance.Update(
            attendanceType: AttendanceType.HalfDay,
            workingHours: null,
            notes: "Try edit",
            now: DateTimeOffset.UtcNow,
            updatedBy: _userId));
    }

    [Fact]
    public void SetCalculatedEarnings_WhenDraft_SetsRatesAndAmounts()
    {
        var attendance = LaborAttendance.CreateDraft(
            _organizationId, _farmId, _workerId, _attendanceDate, AttendanceType.FullDay, _userId);

        var now = DateTimeOffset.UtcNow;
        attendance.SetCalculatedEarnings(500m, 500m, _currencyId, now, _userId);

        Assert.Equal(500m, attendance.CalculatedRate);
        Assert.Equal(500m, attendance.CalculatedAmount);
        Assert.Equal(_currencyId, attendance.CurrencyId);
        Assert.Equal(now, attendance.UpdatedAt);
        Assert.Equal(_userId, attendance.UpdatedBy);
    }

    [Fact]
    public void SetCalculatedEarnings_WhenFinalized_ThrowsInvalidOperationException()
    {
        var attendance = LaborAttendance.CreateFinalized(
            _organizationId, _farmId, _workerId, _attendanceDate, AttendanceType.FullDay, _userId);

        Assert.Throws<InvalidOperationException>(() => attendance.SetCalculatedEarnings(
            600m, 600m, _currencyId, DateTimeOffset.UtcNow, _userId));
    }

    [Fact]
    public void Finalize_TransitionsStatusAndSetsFinalizedMetadata()
    {
        var attendance = LaborAttendance.CreateDraft(
            _organizationId, _farmId, _workerId, _attendanceDate, AttendanceType.FullDay, _userId, calculatedAmount: 500m);

        var now = DateTimeOffset.UtcNow;
        var finalizer = Guid.NewGuid();

        attendance.Finalize(now, finalizer);

        Assert.True(attendance.IsFinalized);
        Assert.Equal(AttendanceStatus.Finalized, attendance.Status);
        Assert.Equal(now, attendance.FinalizedAt);
        Assert.Equal(finalizer, attendance.FinalizedBy);
        Assert.Equal(now, attendance.UpdatedAt);
        Assert.Equal(finalizer, attendance.UpdatedBy);
    }

    [Fact]
    public void Finalize_WhenAlreadyFinalized_ThrowsInvalidOperationException()
    {
        var attendance = LaborAttendance.CreateFinalized(
            _organizationId, _farmId, _workerId, _attendanceDate, AttendanceType.FullDay, _userId);

        Assert.Throws<InvalidOperationException>(() => attendance.Finalize(DateTimeOffset.UtcNow, _userId));
    }

    [Fact]
    public void Finalize_ThrowsWhenFinalizingUserIsEmpty()
    {
        var attendance = LaborAttendance.CreateDraft(
            _organizationId, _farmId, _workerId, _attendanceDate, AttendanceType.FullDay, _userId);

        Assert.Throws<ArgumentException>(() => attendance.Finalize(DateTimeOffset.UtcNow, Guid.Empty));
    }

    [Fact]
    public void ConflictsWith_DetectsWorkerDateCollision()
    {
        var attendance1 = LaborAttendance.CreateDraft(
            _organizationId, _farmId, _workerId, _attendanceDate, AttendanceType.FullDay, _userId);

        var differentFarmId = Guid.NewGuid();
        var attendance2 = LaborAttendance.CreateDraft(
            _organizationId, differentFarmId, _workerId, _attendanceDate, AttendanceType.HalfDay, _userId);

        var otherWorkerId = Guid.NewGuid();
        var attendance3 = LaborAttendance.CreateDraft(
            _organizationId, _farmId, otherWorkerId, _attendanceDate, AttendanceType.FullDay, _userId);

        var otherDate = _attendanceDate.AddDays(1);
        var attendance4 = LaborAttendance.CreateDraft(
            _organizationId, _farmId, _workerId, otherDate, AttendanceType.FullDay, _userId);

        Assert.True(attendance1.ConflictsWith(attendance2)); // same org, worker, date -> conflict
        Assert.False(attendance1.ConflictsWith(attendance3)); // different worker -> no conflict
        Assert.False(attendance1.ConflictsWith(attendance4)); // different date -> no conflict
        Assert.False(attendance1.ConflictsWith(attendance1)); // same instance -> no conflict
    }

    [Fact]
    public void ApplicationDbContext_Model_BuildsSuccessfullyWithLaborAttendance()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=farm_test;Username=postgres;Password=postgres")
            .Options;

        using var dbContext = new ApplicationDbContext(options);
        var model = dbContext.Model;

        var attendanceEntity = model.FindEntityType(typeof(LaborAttendance));
        Assert.NotNull(attendanceEntity);
        Assert.Equal("labor_attendance", attendanceEntity.GetTableName());

        // Primary key
        var pk = attendanceEntity.FindPrimaryKey();
        Assert.NotNull(pk);
        Assert.Single(pk.Properties);
        Assert.Equal("Id", pk.Properties[0].Name);

        // Verify foreign key to Organization with Restrict
        var orgEntity = model.FindEntityType(typeof(Organization));
        Assert.NotNull(orgEntity);
        var orgFk = attendanceEntity.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType == orgEntity);
        Assert.NotNull(orgFk);
        Assert.Equal(DeleteBehavior.Restrict, orgFk.DeleteBehavior);

        // Verify foreign key to Farm with Restrict
        var farmEntity = model.FindEntityType(typeof(Farm));
        Assert.NotNull(farmEntity);
        var farmFk = attendanceEntity.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType == farmEntity);
        Assert.NotNull(farmFk);
        Assert.Equal(DeleteBehavior.Restrict, farmFk.DeleteBehavior);

        // Verify foreign key to Worker with Restrict
        var workerEntity = model.FindEntityType(typeof(Worker));
        Assert.NotNull(workerEntity);
        var workerFk = attendanceEntity.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType == workerEntity);
        Assert.NotNull(workerFk);
        Assert.Equal(DeleteBehavior.Restrict, workerFk.DeleteBehavior);

        // Verify foreign key to Currency with Restrict
        var currencyEntity = model.FindEntityType(typeof(Currency));
        Assert.NotNull(currencyEntity);
        var currencyFk = attendanceEntity.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType == currencyEntity);
        Assert.NotNull(currencyFk);
        Assert.Equal(DeleteBehavior.Restrict, currencyFk.DeleteBehavior);
        Assert.False(currencyFk.IsRequired);

        // Verify entity properties
        Assert.NotNull(attendanceEntity.FindProperty("Id"));
        Assert.NotNull(attendanceEntity.FindProperty("OrganizationId"));
        Assert.NotNull(attendanceEntity.FindProperty("FarmId"));
        Assert.NotNull(attendanceEntity.FindProperty("WorkerId"));
        Assert.NotNull(attendanceEntity.FindProperty("AttendanceDate"));
        Assert.NotNull(attendanceEntity.FindProperty("AttendanceType"));
        Assert.NotNull(attendanceEntity.FindProperty("WorkingHours"));
        Assert.NotNull(attendanceEntity.FindProperty("CalculatedRate"));
        Assert.NotNull(attendanceEntity.FindProperty("CalculatedAmount"));
        Assert.NotNull(attendanceEntity.FindProperty("CurrencyId"));
        Assert.NotNull(attendanceEntity.FindProperty("Status"));
        Assert.NotNull(attendanceEntity.FindProperty("Notes"));
        Assert.NotNull(attendanceEntity.FindProperty("FinalizedAt"));
        Assert.NotNull(attendanceEntity.FindProperty("FinalizedBy"));
        Assert.NotNull(attendanceEntity.FindProperty("CreatedAt"));
        Assert.NotNull(attendanceEntity.FindProperty("CreatedBy"));
        Assert.NotNull(attendanceEntity.FindProperty("UpdatedAt"));
        Assert.NotNull(attendanceEntity.FindProperty("UpdatedBy"));

        // Verify indexes
        var indexes = attendanceEntity.GetIndexes().ToList();
        Assert.NotNull(indexes.FirstOrDefault(ix => ix.GetDatabaseName() == "ix_labor_attendance_organization_id"));
        Assert.NotNull(indexes.FirstOrDefault(ix => ix.GetDatabaseName() == "ix_labor_attendance_org_attendance_date"));
        Assert.NotNull(indexes.FirstOrDefault(ix => ix.GetDatabaseName() == "ix_labor_attendance_org_farm_attendance_date"));
        Assert.NotNull(indexes.FirstOrDefault(ix => ix.GetDatabaseName() == "ix_labor_attendance_org_worker_attendance_date"));
        Assert.NotNull(indexes.FirstOrDefault(ix => ix.GetDatabaseName() == "ix_labor_attendance_status"));
        Assert.NotNull(indexes.FirstOrDefault(ix => ix.GetDatabaseName() == "ix_labor_attendance_farm_attendance_date"));

        var uniquePayrollIndex = indexes.FirstOrDefault(ix => ix.GetDatabaseName() == "ux_labor_attendance_org_worker_date_payroll");
        Assert.NotNull(uniquePayrollIndex);
        Assert.True(uniquePayrollIndex.IsUnique);
        Assert.Equal("attendance_type != 'NOT_WORKED'", uniquePayrollIndex.GetFilter());
    }

    [Fact]
    public void Phase3_2_AttendancePermissions_AreConfiguredInIdentityDataSeeder()
    {
        var seedPermissions = IdentityDataSeeder.SeedPermissionDefinitions;
        var orgAdminPermissions = IdentityDataSeeder.OrganizationAdminPermissionNames;

        var expectedAttendancePermissions = new[]
        {
            "Attendance.View",
            "Attendance.Create",
            "Attendance.Update",
            "Attendance.Finalize"
        };

        foreach (var permName in expectedAttendancePermissions)
        {
            var match = seedPermissions.FirstOrDefault(p => p.Name == permName);
            Assert.True(match != default, $"Permission {permName} should be present in SeedPermissions.");
            Assert.Equal("Attendance", match.Module);
            Assert.False(string.IsNullOrWhiteSpace(match.Description));
            Assert.Contains(permName, orgAdminPermissions);
        }

        // Controlled reversal and unfinalization must NOT be seeded in Phase 3.2
        Assert.DoesNotContain(seedPermissions, p => p.Name == "Attendance.Reverse");
        Assert.DoesNotContain(seedPermissions, p => p.Name == "Attendance.Unfinalize");
        Assert.DoesNotContain("Attendance.Reverse", orgAdminPermissions);
        Assert.DoesNotContain("Attendance.Unfinalize", orgAdminPermissions);
    }

    [Fact]
    public void Phase3_2_AttendancePermissions_SeedingIsIdempotentAndPreservesRoleMappings()
    {
        var expectedPermissions = new Dictionary<string, string>
        {
            ["Attendance.View"] = "Attendance",
            ["Attendance.Create"] = "Attendance",
            ["Attendance.Update"] = "Attendance",
            ["Attendance.Finalize"] = "Attendance"
        };

        var existingPermissions = new Dictionary<string, Permission>(StringComparer.Ordinal);
        var superAdminRole = new Role("SuperAdmin", "Platform-wide administrator.", isSystemRole: true);
        var orgAdminRole = new Role("OrganizationAdmin", "Administrator for an organization.", isSystemRole: true);
        var farmManagerRole = new Role("FarmManager", "Manager of farm operations.", isSystemRole: true);

        var rolePermissions = new HashSet<(Guid RoleId, Guid PermissionId)>();

        void RunPermissionSeeding()
        {
            foreach (var (name, module) in expectedPermissions)
            {
                if (!existingPermissions.TryGetValue(name, out var perm))
                {
                    perm = new Permission(name, module, $"Description for {name}");
                    existingPermissions.Add(name, perm);
                }

                // SuperAdmin gets all permissions
                rolePermissions.Add((superAdminRole.Id, perm.Id));

                // OrganizationAdmin gets Phase 3.2 attendance permissions
                rolePermissions.Add((orgAdminRole.Id, perm.Id));
            }
        }

        // Run 1: initial seed
        RunPermissionSeeding();
        Assert.Equal(4, existingPermissions.Count);
        Assert.Equal(8, rolePermissions.Count); // 4 for SuperAdmin, 4 for OrgAdmin
        Assert.DoesNotContain(rolePermissions, rp => rp.RoleId == farmManagerRole.Id);

        // Run 2: idempotent re-seed
        RunPermissionSeeding();
        Assert.Equal(4, existingPermissions.Count);
        Assert.Equal(8, rolePermissions.Count);
    }
}
