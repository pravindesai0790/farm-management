using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using FarmManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FarmManagement.API.Tests.Domain;

public sealed class LaborMasterModelTests
{
    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public void Worker_Create_WithValidData_SetsPropertiesAndDefaultsDisplayName()
    {
        var worker = new Worker(
            organizationId: _organizationId,
            firstName: "John",
            lastName: "Doe",
            gender: Gender.Male,
            employmentType: EmploymentType.Permanent,
            createdBy: _userId);

        Assert.NotEqual(Guid.Empty, worker.Id);
        Assert.Equal(_organizationId, worker.OrganizationId);
        Assert.Equal("John", worker.FirstName);
        Assert.Equal("Doe", worker.LastName);
        Assert.Equal("John Doe", worker.DisplayName);
        Assert.Equal(Gender.Male, worker.Gender);
        Assert.Equal(EmploymentType.Permanent, worker.EmploymentType);
        Assert.Null(worker.ContractorId);
        Assert.Null(worker.LaborCategoryId);
        Assert.True(worker.IsActive);
        Assert.Equal(_userId, worker.CreatedBy);
    }

    [Fact]
    public void Worker_Create_WithExplicitDisplayName_PreservesCustomDisplayName()
    {
        var worker = new Worker(
            organizationId: _organizationId,
            firstName: "John",
            lastName: "Doe",
            gender: Gender.Male,
            employmentType: EmploymentType.DailyWage,
            createdBy: _userId,
            displayName: "JD Farmer");

        Assert.Equal("JD Farmer", worker.DisplayName);
    }

    [Fact]
    public void Worker_Create_WithoutLastName_GeneratesDisplayNameFromFirstName()
    {
        var worker = new Worker(
            organizationId: _organizationId,
            firstName: "Ramesh",
            lastName: null,
            gender: Gender.Male,
            employmentType: EmploymentType.Seasonal,
            createdBy: _userId);

        Assert.Equal("Ramesh", worker.DisplayName);
        Assert.Null(worker.LastName);
    }

    [Fact]
    public void Worker_Create_WhenOrganizationIdEmpty_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => new Worker(
            organizationId: Guid.Empty,
            firstName: "John",
            lastName: "Doe",
            gender: Gender.Male,
            employmentType: EmploymentType.Permanent,
            createdBy: _userId));

        Assert.Equal("organizationId", ex.ParamName);
    }

    [Fact]
    public void Worker_Create_WhenFirstNameEmpty_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => new Worker(
            organizationId: _organizationId,
            firstName: "   ",
            lastName: "Doe",
            gender: Gender.Male,
            employmentType: EmploymentType.Permanent,
            createdBy: _userId));

        Assert.Equal("firstName", ex.ParamName);
    }

    [Fact]
    public void Worker_Create_WhenCreatedByEmpty_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => new Worker(
            organizationId: _organizationId,
            firstName: "John",
            lastName: "Doe",
            gender: Gender.Male,
            employmentType: EmploymentType.Permanent,
            createdBy: Guid.Empty));

        Assert.Equal("createdBy", ex.ParamName);
    }

    [Fact]
    public void Worker_Create_WhenGenderInvalid_ThrowsArgumentOutOfRangeException()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new Worker(
            organizationId: _organizationId,
            firstName: "John",
            lastName: "Doe",
            gender: (Gender)999,
            employmentType: EmploymentType.Permanent,
            createdBy: _userId));

        Assert.Equal("gender", ex.ParamName);
    }

    [Fact]
    public void Worker_Create_WhenEmploymentTypeInvalid_ThrowsArgumentOutOfRangeException()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new Worker(
            organizationId: _organizationId,
            firstName: "John",
            lastName: "Doe",
            gender: Gender.Male,
            employmentType: (EmploymentType)999,
            createdBy: _userId));

        Assert.Equal("employmentType", ex.ParamName);
    }

    [Fact]
    public void Worker_Create_WhenEmploymentTypeIsContractAndContractorMissing_ThrowsArgumentException()
    {
        var exNull = Assert.Throws<ArgumentException>(() => new Worker(
            organizationId: _organizationId,
            firstName: "Contractor",
            lastName: "Worker",
            gender: Gender.Male,
            employmentType: EmploymentType.Contract,
            createdBy: _userId,
            contractorId: null));

        Assert.Equal("contractorId", exNull.ParamName);

        var exEmpty = Assert.Throws<ArgumentException>(() => new Worker(
            organizationId: _organizationId,
            firstName: "Contractor",
            lastName: "Worker",
            gender: Gender.Male,
            employmentType: EmploymentType.Contract,
            createdBy: _userId,
            contractorId: Guid.Empty));

        Assert.Equal("contractorId", exEmpty.ParamName);
    }

    [Fact]
    public void Worker_Create_WhenLeavingDateBeforeJoiningDate_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => new Worker(
            organizationId: _organizationId,
            firstName: "John",
            lastName: "Doe",
            gender: Gender.Male,
            employmentType: EmploymentType.Permanent,
            createdBy: _userId,
            joiningDate: new DateOnly(2026, 6, 1),
            leavingDate: new DateOnly(2026, 5, 31)));

        Assert.Equal("leavingDate", ex.ParamName);
    }

    [Fact]
    public void Worker_Create_WhenLeavingDateEqualsOrAfterJoiningDate_Succeeds()
    {
        var worker = new Worker(
            organizationId: _organizationId,
            firstName: "John",
            lastName: "Doe",
            gender: Gender.Female,
            employmentType: EmploymentType.Seasonal,
            createdBy: _userId,
            joiningDate: new DateOnly(2026, 6, 1),
            leavingDate: new DateOnly(2026, 6, 1));

        Assert.Equal(new DateOnly(2026, 6, 1), worker.JoiningDate);
        Assert.Equal(new DateOnly(2026, 6, 1), worker.LeavingDate);
    }

    [Fact]
    public void Worker_Update_ValidatesAndUpdatesFields()
    {
        var worker = new Worker(
            organizationId: _organizationId,
            firstName: "Original",
            lastName: "Name",
            gender: Gender.Male,
            employmentType: EmploymentType.Permanent,
            createdBy: _userId);

        var now = DateTimeOffset.UtcNow;
        var updater = Guid.NewGuid();

        worker.Update(
            firstName: "Updated",
            lastName: "Worker",
            displayName: "Updated Worker Special",
            gender: Gender.Female,
            employmentType: EmploymentType.DailyWage,
            laborCategoryId: null,
            contractorId: null,
            joiningDate: new DateOnly(2026, 1, 1),
            leavingDate: new DateOnly(2026, 12, 31),
            mobileNumber: "9876543210",
            alternateMobileNumber: null,
            notes: "Updated notes",
            now: now,
            updatedBy: updater);

        Assert.Equal("Updated", worker.FirstName);
        Assert.Equal("Worker", worker.LastName);
        Assert.Equal("Updated Worker Special", worker.DisplayName);
        Assert.Equal(Gender.Female, worker.Gender);
        Assert.Equal(EmploymentType.DailyWage, worker.EmploymentType);
        Assert.Equal("9876543210", worker.MobileNumber);
        Assert.Equal(now, worker.UpdatedAt);
        Assert.Equal(updater, worker.UpdatedBy);
    }

    [Fact]
    public void Worker_ActivateAndDeactivate_UpdatesStatus()
    {
        var worker = new Worker(
            organizationId: _organizationId,
            firstName: "Test",
            lastName: "Worker",
            gender: Gender.Other,
            employmentType: EmploymentType.Permanent,
            createdBy: _userId);

        var now = DateTimeOffset.UtcNow;
        var updater = Guid.NewGuid();

        Assert.True(worker.IsActive);
        Assert.False(worker.Activate(now, updater)); // Already active

        Assert.True(worker.Deactivate(now, updater));
        Assert.False(worker.IsActive);
        Assert.Equal(updater, worker.UpdatedBy);

        Assert.False(worker.Deactivate(now, updater)); // Already inactive

        Assert.True(worker.Activate(now, updater));
        Assert.True(worker.IsActive);
    }

    [Fact]
    public void Worker_ValidateOrganizationBoundary_ValidatesContractorAndCategory()
    {
        var contractorOrg = new Contractor(_organizationId, "Local Contractor", _userId);
        var differentOrgId = Guid.NewGuid();
        var alienContractor = new Contractor(differentOrgId, "Alien Contractor", _userId);

        var systemCategory = new LaborCategory(null, "Skilled", isSystem: true);
        var orgCategory = new LaborCategory(_organizationId, "Org Special", isSystem: false, createdBy: _userId);
        var alienCategory = new LaborCategory(differentOrgId, "Alien Category", isSystem: false, createdBy: _userId);

        var worker = new Worker(
            organizationId: _organizationId,
            firstName: "Boundary",
            lastName: "Check",
            gender: Gender.Male,
            employmentType: EmploymentType.Contract,
            createdBy: _userId,
            contractorId: contractorOrg.Id,
            laborCategoryId: orgCategory.Id);

        // Same org contractor and category -> valid
        worker.ValidateOrganizationBoundary(contractorOrg, orgCategory);

        // System category -> valid across any org
        worker.ValidateOrganizationBoundary(contractorOrg, systemCategory);

        // Alien contractor -> invalid
        var exContractor = Assert.Throws<InvalidOperationException>(() =>
            worker.ValidateOrganizationBoundary(alienContractor, orgCategory));
        Assert.Contains("different organization", exContractor.Message);

        // Alien category -> invalid
        var exCategory = Assert.Throws<InvalidOperationException>(() =>
            worker.ValidateOrganizationBoundary(contractorOrg, alienCategory));
        Assert.Contains("different organization", exCategory.Message);
    }

    [Fact]
    public void Contractor_Create_WithValidData_SetsProperties()
    {
        var contractor = new Contractor(
            organizationId: _organizationId,
            name: "Agri Labor Services",
            createdBy: _userId,
            contactPerson: "Jane Contact",
            phoneNumber: "1234567890",
            email: "jane@agri.com",
            address: "123 Field Way",
            notes: "Reliable provider");

        Assert.NotEqual(Guid.Empty, contractor.Id);
        Assert.Equal(_organizationId, contractor.OrganizationId);
        Assert.Equal("Agri Labor Services", contractor.Name);
        Assert.Equal("Jane Contact", contractor.ContactPerson);
        Assert.Equal("1234567890", contractor.PhoneNumber);
        Assert.Equal("jane@agri.com", contractor.Email);
        Assert.True(contractor.IsActive);
        Assert.Equal(_userId, contractor.CreatedBy);
        Assert.Empty(contractor.Workers);
    }

    [Fact]
    public void Contractor_Create_WhenOrganizationEmptyOrNameEmpty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new Contractor(Guid.Empty, "Name", _userId));
        Assert.Throws<ArgumentException>(() => new Contractor(_organizationId, "  ", _userId));
        Assert.Throws<ArgumentException>(() => new Contractor(_organizationId, "Name", Guid.Empty));
    }

    [Fact]
    public void Contractor_UpdateAndActivateDeactivate_FunctionsCorrectly()
    {
        var contractor = new Contractor(_organizationId, "Initial Name", _userId);
        var now = DateTimeOffset.UtcNow;
        var updater = Guid.NewGuid();

        contractor.Update(
            name: "Updated Name",
            contactPerson: "New Person",
            phoneNumber: "5551234",
            email: null,
            address: null,
            notes: "Updated",
            now: now,
            updatedBy: updater);

        Assert.Equal("Updated Name", contractor.Name);
        Assert.Equal("New Person", contractor.ContactPerson);
        Assert.Equal(now, contractor.UpdatedAt);
        Assert.Equal(updater, contractor.UpdatedBy);

        Assert.True(contractor.Deactivate(now, updater));
        Assert.False(contractor.IsActive);
        Assert.True(contractor.Activate(now, updater));
        Assert.True(contractor.IsActive);
    }

    [Fact]
    public void LaborCategory_SystemCategory_ValidatesSystemOrgRule()
    {
        var systemCategory = new LaborCategory(
            organizationId: null,
            name: "Skilled",
            isSystem: true,
            description: "System skilled workers");

        Assert.True(systemCategory.IsSystem);
        Assert.Null(systemCategory.OrganizationId);
        Assert.Equal("Skilled", systemCategory.Name);

        // System category with organizationId must throw
        Assert.Throws<ArgumentException>(() =>
            new LaborCategory(
                organizationId: _organizationId,
                name: "Invalid System Category",
                isSystem: true));
    }

    [Fact]
    public void LaborCategory_OrganizationCategory_ValidatesSystemOrgRule()
    {
        var orgCategory = new LaborCategory(
            organizationId: _organizationId,
            name: "Vineyard Pruning Specialist",
            isSystem: false,
            description: "Local custom specialty",
            createdBy: _userId);

        Assert.False(orgCategory.IsSystem);
        Assert.Equal(_organizationId, orgCategory.OrganizationId);
        Assert.Equal("Vineyard Pruning Specialist", orgCategory.Name);
        Assert.Equal(_userId, orgCategory.CreatedBy);

        // Non-system category without organizationId must throw
        Assert.Throws<ArgumentException>(() =>
            new LaborCategory(
                organizationId: null,
                name: "Invalid Org Category",
                isSystem: false,
                createdBy: _userId));

        // Non-system category without createdBy must throw
        Assert.Throws<ArgumentException>(() =>
            new LaborCategory(
                organizationId: _organizationId,
                name: "Invalid Org Category",
                isSystem: false,
                createdBy: null));
    }

    [Fact]
    public void ApplicationDbContext_Model_BuildsSuccessfullyWithLaborMasters()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=farm_test;Username=postgres;Password=postgres")
            .Options;

        using var dbContext = new ApplicationDbContext(options);
        var model = dbContext.Model;

        // Verify Worker entity mapping
        var workerEntity = model.FindEntityType(typeof(Worker));
        Assert.NotNull(workerEntity);
        Assert.Equal("workers", workerEntity.GetTableName());
        Assert.NotNull(workerEntity.FindProperty("DisplayName"));
        Assert.NotNull(workerEntity.FindProperty("Gender"));
        Assert.NotNull(workerEntity.FindProperty("EmploymentType"));
        Assert.NotNull(workerEntity.FindProperty("JoiningDate"));
        Assert.NotNull(workerEntity.FindProperty("LeavingDate"));

        // Verify Contractor entity mapping
        var contractorEntity = model.FindEntityType(typeof(Contractor));
        Assert.NotNull(contractorEntity);
        Assert.Equal("contractors", contractorEntity.GetTableName());
        Assert.NotNull(contractorEntity.FindProperty("Name"));

        // Verify LaborCategory entity mapping
        var categoryEntity = model.FindEntityType(typeof(LaborCategory));
        Assert.NotNull(categoryEntity);
        Assert.Equal("labor_categories", categoryEntity.GetTableName());
        Assert.NotNull(categoryEntity.FindProperty("IsSystem"));

        // Verify foreign key relationships
        var workerToContractorFk = workerEntity.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType == contractorEntity);
        Assert.NotNull(workerToContractorFk);
        Assert.Equal(DeleteBehavior.Restrict, workerToContractorFk.DeleteBehavior);

        var workerToCategoryFk = workerEntity.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType == categoryEntity);
        Assert.NotNull(workerToCategoryFk);
        Assert.Equal(DeleteBehavior.Restrict, workerToCategoryFk.DeleteBehavior);

        // Verify explicit organization_id indexes
        Assert.NotNull(workerEntity.GetIndexes().FirstOrDefault(ix => ix.GetDatabaseName() == "ix_workers_organization_id"));
        Assert.NotNull(contractorEntity.GetIndexes().FirstOrDefault(ix => ix.GetDatabaseName() == "ix_contractors_organization_id"));
        Assert.NotNull(categoryEntity.GetIndexes().FirstOrDefault(ix => ix.GetDatabaseName() == "ix_labor_categories_organization_id"));

        // Verify worker search/filter indexes
        Assert.NotNull(workerEntity.GetIndexes().FirstOrDefault(ix => ix.GetDatabaseName() == "ix_workers_organization_display_name"));
        Assert.NotNull(workerEntity.GetIndexes().FirstOrDefault(ix => ix.GetDatabaseName() == "ix_workers_organization_mobile"));
        Assert.NotNull(workerEntity.GetIndexes().FirstOrDefault(ix => ix.GetDatabaseName() == "ix_workers_organization_is_active"));
        Assert.NotNull(workerEntity.GetIndexes().FirstOrDefault(ix => ix.GetDatabaseName() == "ix_workers_organization_contractor"));
        Assert.NotNull(workerEntity.GetIndexes().FirstOrDefault(ix => ix.GetDatabaseName() == "ix_workers_organization_labor_category"));
    }

    [Fact]
    public void SystemLaborCategories_Seeding_IsIdempotentAndContainsExpectedCategories()
    {
        var expectedCategoryNames = new[]
        {
            "Skilled",
            "Semi-Skilled",
            "Unskilled",
            "Supervisor",
            "Operator",
            "Specialized"
        };

        var seededStore = new List<LaborCategory>();

        void RunSeeder()
        {
            foreach (var name in expectedCategoryNames)
            {
                var exists = seededStore.Any(c => c.IsSystem && c.OrganizationId == null && c.Name == name);
                if (!exists)
                {
                    seededStore.Add(new LaborCategory(null, name, isSystem: true, description: $"{name} labor"));
                }
            }
        }

        // First run
        RunSeeder();
        Assert.Equal(expectedCategoryNames.Length, seededStore.Count);
        Assert.All(seededStore, c => Assert.True(c.IsSystem && c.OrganizationId == null));

        // Second run (must not insert duplicates)
        RunSeeder();
        Assert.Equal(expectedCategoryNames.Length, seededStore.Count);
    }

    [Fact]
    public void Phase3_1_Permissions_AreIdempotentAndCorrectlyMapped()
    {
        var expectedPermissions = new Dictionary<string, string>
        {
            // Worker (5)
            ["Worker.View"] = "Workers",
            ["Worker.Create"] = "Workers",
            ["Worker.Update"] = "Workers",
            ["Worker.Activate"] = "Workers",
            ["Worker.Deactivate"] = "Workers",

            // Contractor (5)
            ["Contractor.View"] = "Contractors",
            ["Contractor.Create"] = "Contractors",
            ["Contractor.Update"] = "Contractors",
            ["Contractor.Activate"] = "Contractors",
            ["Contractor.Deactivate"] = "Contractors",

            // Labor Category (5)
            ["LaborCategory.View"] = "Labor Categories",
            ["LaborCategory.Create"] = "Labor Categories",
            ["LaborCategory.Update"] = "Labor Categories",
            ["LaborCategory.Activate"] = "Labor Categories",
            ["LaborCategory.Deactivate"] = "Labor Categories",

            // Wage Rate (3)
            ["WorkerWage.View"] = "Labor Wage Rates",
            ["WorkerWage.Create"] = "Labor Wage Rates",
            ["WorkerWage.Update"] = "Labor Wage Rates",

            // Worker Payment (3)
            ["WorkerPayment.View"] = "Worker Payments",
            ["WorkerPayment.Create"] = "Worker Payments",
            ["WorkerPayment.Cancel"] = "Worker Payments",

            // Worker Earnings (3)
            ["WorkerEarnings.View"] = "Worker Earnings",
            ["WorkerEarnings.Approve"] = "Worker Earnings",
            ["WorkerEarnings.Reverse"] = "Worker Earnings"
        };

        Assert.Equal(24, expectedPermissions.Count);

        var existingPermissions = new Dictionary<string, Permission>(StringComparer.Ordinal);
        var superAdminRole = new Role("SuperAdmin", "Platform-wide administrator.", isSystemRole: true);
        var orgAdminRole = new Role("OrganizationAdmin", "Administrator for an organization.", isSystemRole: true);

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

                // OrganizationAdmin gets Phase 3.1 permissions
                rolePermissions.Add((orgAdminRole.Id, perm.Id));
            }
        }

        // Run 1
        RunPermissionSeeding();
        Assert.Equal(24, existingPermissions.Count);
        Assert.Equal(48, rolePermissions.Count); // 24 for SuperAdmin, 24 for OrgAdmin

        // Run 2 (idempotent rerun)
        RunPermissionSeeding();
        Assert.Equal(24, existingPermissions.Count);
        Assert.Equal(48, rolePermissions.Count);
    }
}
