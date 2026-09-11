using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.Labor;
using FarmManagement.Application.Interfaces.Labor;
using FarmManagement.Application.Services;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Services;

public sealed class LaborMasterServiceTests
{
    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    private WorkerActor CreateWorkerActor() => new(_userId, _organizationId);
    private ContractorActor CreateContractorActor() => new(_userId, _organizationId);
    private LaborCategoryActor CreateLaborCategoryActor() => new(_userId, _organizationId);

    #region WorkerService Tests

    [Fact]
    public async Task WorkerService_CreateAsync_WithValidPermanentWorker_Succeeds()
    {
        var store = new FakeWorkerStore();
        var service = new WorkerService(store);

        var request = new CreateWorkerRequest(
            FirstName: "Rajesh",
            LastName: "Patil",
            DisplayName: null,
            Gender: "MALE",
            EmploymentType: "PERMANENT",
            MobileNumber: "9876543210",
            AlternateMobileNumber: null,
            LaborCategoryId: null,
            ContractorId: null,
            JoiningDate: new DateOnly(2026, 1, 1),
            LeavingDate: null,
            Notes: "Senior worker");

        var response = await service.CreateAsync(CreateWorkerActor(), request, "127.0.0.1");

        Assert.NotNull(response);
        Assert.Equal("Rajesh", response.FirstName);
        Assert.Equal("Patil", response.LastName);
        Assert.Equal("Rajesh Patil", response.DisplayName);
        Assert.Equal("MALE", response.Gender);
        Assert.Equal("PERMANENT", response.EmploymentType);
        Assert.True(response.IsActive);
        Assert.Single(store.AuditLogs);
        Assert.Equal("Worker.Created", store.AuditLogs[0].Action);
    }

    [Fact]
    public async Task WorkerService_CreateAsync_WhenContractWithoutContractor_ThrowsValidationException()
    {
        var store = new FakeWorkerStore();
        var service = new WorkerService(store);

        var request = new CreateWorkerRequest(
            FirstName: "Contract",
            LastName: "Laborer",
            DisplayName: null,
            Gender: "MALE",
            EmploymentType: "CONTRACT",
            MobileNumber: null,
            AlternateMobileNumber: null,
            LaborCategoryId: null,
            ContractorId: null,
            JoiningDate: null,
            LeavingDate: null,
            Notes: null);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(CreateWorkerActor(), request, "127.0.0.1"));

        Assert.True(ex.Errors?.ContainsKey("contractorId"));
        Assert.Contains("contractor is required", ex.Errors!["contractorId"][0]);
    }

    [Fact]
    public async Task WorkerService_CreateAsync_WhenContractorNotFoundInOrg_ThrowsValidationException()
    {
        var store = new FakeWorkerStore();
        var service = new WorkerService(store);

        var request = new CreateWorkerRequest(
            FirstName: "Contract",
            LastName: "Laborer",
            DisplayName: null,
            Gender: "FEMALE",
            EmploymentType: "CONTRACT",
            MobileNumber: null,
            AlternateMobileNumber: null,
            LaborCategoryId: null,
            ContractorId: Guid.NewGuid(),
            JoiningDate: null,
            LeavingDate: null,
            Notes: null);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(CreateWorkerActor(), request, "127.0.0.1"));

        Assert.True(ex.Errors?.ContainsKey("contractorId"));
        Assert.Contains("contractor was not found", ex.Errors!["contractorId"][0]);
    }

    [Fact]
    public async Task WorkerService_CreateAsync_WhenContractorIsInactive_ThrowsValidationException()
    {
        var store = new FakeWorkerStore();
        var contractor = new Contractor(_organizationId, "Inactive Contractor", _userId);
        contractor.Deactivate(DateTimeOffset.UtcNow, _userId);
        store.Contractors.Add(contractor);

        var service = new WorkerService(store);

        var request = new CreateWorkerRequest(
            FirstName: "Contract",
            LastName: "Laborer",
            DisplayName: null,
            Gender: "MALE",
            EmploymentType: "CONTRACT",
            MobileNumber: null,
            AlternateMobileNumber: null,
            LaborCategoryId: null,
            ContractorId: contractor.Id,
            JoiningDate: null,
            LeavingDate: null,
            Notes: null);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(CreateWorkerActor(), request, "127.0.0.1"));

        Assert.True(ex.Errors?.ContainsKey("contractorId"));
        Assert.Contains("inactive contractor", ex.Errors!["contractorId"][0]);
    }

    [Fact]
    public async Task WorkerService_CreateAsync_WhenLeavingDateBeforeJoiningDate_ThrowsValidationException()
    {
        var store = new FakeWorkerStore();
        var service = new WorkerService(store);

        var request = new CreateWorkerRequest(
            FirstName: "Date",
            LastName: "Invalid",
            DisplayName: null,
            Gender: "MALE",
            EmploymentType: "SEASONAL",
            MobileNumber: null,
            AlternateMobileNumber: null,
            LaborCategoryId: null,
            ContractorId: null,
            JoiningDate: new DateOnly(2026, 6, 1),
            LeavingDate: new DateOnly(2026, 5, 1),
            Notes: null);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(CreateWorkerActor(), request, "127.0.0.1"));

        Assert.True(ex.Errors?.ContainsKey("leavingDate"));
    }

    [Fact]
    public async Task WorkerService_UpdateAsync_WhenWorkerNotFound_ThrowsResourceNotFoundException()
    {
        var store = new FakeWorkerStore();
        var service = new WorkerService(store);

        var request = new UpdateWorkerRequest(
            FirstName: "Updated",
            LastName: "Worker",
            DisplayName: null,
            Gender: "MALE",
            EmploymentType: "PERMANENT",
            MobileNumber: null,
            AlternateMobileNumber: null,
            LaborCategoryId: null,
            ContractorId: null,
            JoiningDate: null,
            LeavingDate: null,
            Notes: null);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            service.UpdateAsync(CreateWorkerActor(), Guid.NewGuid(), request, "127.0.0.1"));
    }

    [Fact]
    public async Task WorkerService_ActivateAndDeactivate_UpdatesStateAndAudits()
    {
        var store = new FakeWorkerStore();
        var worker = new Worker(_organizationId, "Test", "Worker", Gender.Male, EmploymentType.Permanent, _userId);
        store.Workers.Add(worker);

        var service = new WorkerService(store);

        // Deactivate
        var deactivated = await service.DeactivateAsync(CreateWorkerActor(), worker.Id, "127.0.0.1");
        Assert.True(deactivated);
        Assert.False(worker.IsActive);
        Assert.Contains(store.AuditLogs, a => a.Action == "Worker.Deactivated");

        // Deactivate again -> false (no change)
        var deactivatedAgain = await service.DeactivateAsync(CreateWorkerActor(), worker.Id, "127.0.0.1");
        Assert.False(deactivatedAgain);

        // Activate
        var activated = await service.ActivateAsync(CreateWorkerActor(), worker.Id, "127.0.0.1");
        Assert.True(activated);
        Assert.True(worker.IsActive);
        Assert.Contains(store.AuditLogs, a => a.Action == "Worker.Activated");
    }

    #endregion

    #region ContractorService Tests

    [Fact]
    public async Task ContractorService_CreateAsync_WhenValid_Succeeds()
    {
        var store = new FakeContractorStore();
        var service = new ContractorService(store);

        var request = new CreateContractorRequest(
            Name: "Agri Labor Partners",
            ContactPerson: "Suresh",
            PhoneNumber: "9876543210",
            Email: "suresh@agri.com",
            Address: "Agri Hub 1",
            Notes: "Notes");

        var response = await service.CreateAsync(CreateContractorActor(), request, "127.0.0.1");

        Assert.NotNull(response);
        Assert.Equal("Agri Labor Partners", response.Name);
        Assert.Equal(0, response.WorkerCount);
        Assert.True(response.IsActive);
        Assert.Single(store.AuditLogs);
        Assert.Equal("Contractor.Created", store.AuditLogs[0].Action);
    }

    [Fact]
    public async Task ContractorService_CreateAsync_WhenDuplicateNameInOrg_ThrowsConflictException()
    {
        var store = new FakeContractorStore();
        var existing = new Contractor(_organizationId, "Alpha Contractors", _userId);
        store.Contractors.Add(existing);

        var service = new ContractorService(store);

        var request = new CreateContractorRequest(
            Name: "alpha contractors",
            ContactPerson: null,
            PhoneNumber: null,
            Email: null,
            Address: null,
            Notes: null);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(CreateContractorActor(), request, "127.0.0.1"));
    }

    [Fact]
    public async Task ContractorService_UpdateAsync_WhenContractorNotFound_ThrowsResourceNotFoundException()
    {
        var store = new FakeContractorStore();
        var service = new ContractorService(store);

        var request = new UpdateContractorRequest(
            Name: "Updated",
            ContactPerson: null,
            PhoneNumber: null,
            Email: null,
            Address: null,
            Notes: null);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            service.UpdateAsync(CreateContractorActor(), Guid.NewGuid(), request, "127.0.0.1"));
    }

    #endregion

    #region LaborCategoryService Tests

    [Fact]
    public async Task LaborCategoryService_CreateAsync_CreatesTenantCategory()
    {
        var store = new FakeLaborCategoryStore();
        var service = new LaborCategoryService(store);

        var request = new CreateLaborCategoryRequest("Vineyard Pruning", "Custom Category");

        var response = await service.CreateAsync(CreateLaborCategoryActor(), request, "127.0.0.1");

        Assert.NotNull(response);
        Assert.Equal("Vineyard Pruning", response.Name);
        Assert.False(response.IsSystem);
        Assert.Equal(_organizationId, response.OrganizationId);
        Assert.Single(store.AuditLogs);
        Assert.Equal("LaborCategory.Created", store.AuditLogs[0].Action);
    }

    [Fact]
    public async Task LaborCategoryService_CreateAsync_WhenNameCollidesWithSystemCategory_ThrowsConflictException()
    {
        var store = new FakeLaborCategoryStore();
        var systemCategory = new LaborCategory(null, "Skilled", isSystem: true);
        store.Categories.Add(systemCategory);

        var service = new LaborCategoryService(store);

        var request = new CreateLaborCategoryRequest("skilled", null);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(CreateLaborCategoryActor(), request, "127.0.0.1"));
    }

    [Fact]
    public async Task LaborCategoryService_UpdateAsync_WhenSystemCategory_ThrowsForbiddenException()
    {
        var store = new FakeLaborCategoryStore();
        var systemCategory = new LaborCategory(null, "Operator", isSystem: true);
        store.Categories.Add(systemCategory);

        var service = new LaborCategoryService(store);

        var request = new UpdateLaborCategoryRequest("Operator Modified", null);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.UpdateAsync(CreateLaborCategoryActor(), systemCategory.Id, request, "127.0.0.1"));
    }

    [Fact]
    public async Task LaborCategoryService_DeactivateAsync_WhenSystemCategory_ThrowsForbiddenException()
    {
        var store = new FakeLaborCategoryStore();
        var systemCategory = new LaborCategory(null, "Specialized", isSystem: true);
        store.Categories.Add(systemCategory);

        var service = new LaborCategoryService(store);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.DeactivateAsync(CreateLaborCategoryActor(), systemCategory.Id, "127.0.0.1"));
    }

    #endregion

    #region Fake Stores

    private sealed class FakeWorkerStore : IWorkerStore
    {
        public List<Worker> Workers { get; } = [];
        public List<Contractor> Contractors { get; } = [];
        public List<LaborCategory> LaborCategories { get; } = [];
        public List<AuditLog> AuditLogs { get; } = [];

        public Task<int> CountAsync(
            Guid organizationId,
            string? search,
            bool? isActive,
            Guid? contractorId,
            Guid? laborCategoryId,
            Gender? gender,
            EmploymentType? employmentType,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Filter(organizationId, search, isActive, contractorId, laborCategoryId, gender, employmentType).Count());
        }

        public Task<IReadOnlyList<Worker>> ListAsync(
            Guid organizationId,
            int skip,
            int take,
            string? search,
            bool? isActive,
            Guid? contractorId,
            Guid? laborCategoryId,
            Gender? gender,
            EmploymentType? employmentType,
            CancellationToken cancellationToken = default)
        {
            var result = Filter(organizationId, search, isActive, contractorId, laborCategoryId, gender, employmentType)
                .Skip(skip)
                .Take(take)
                .ToList();
            return Task.FromResult<IReadOnlyList<Worker>>(result);
        }

        public Task<Worker?> FindAsync(Guid workerId, Guid organizationId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Workers.FirstOrDefault(w => w.Id == workerId && w.OrganizationId == organizationId));
        }

        public Task<Contractor?> FindContractorAsync(Guid contractorId, Guid organizationId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Contractors.FirstOrDefault(c => c.Id == contractorId && c.OrganizationId == organizationId));
        }

        public Task<LaborCategory?> FindLaborCategoryAsync(Guid laborCategoryId, Guid organizationId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(LaborCategories.FirstOrDefault(c => c.Id == laborCategoryId && (c.IsSystem || c.OrganizationId == organizationId)));
        }

        public void Add(Worker worker) => Workers.Add(worker);

        public void AddAuditLog(AuditLog auditLog) => AuditLogs.Add(auditLog);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        private IEnumerable<Worker> Filter(
            Guid organizationId,
            string? search,
            bool? isActive,
            Guid? contractorId,
            Guid? laborCategoryId,
            Gender? gender,
            EmploymentType? employmentType)
        {
            var query = Workers.Where(w => w.OrganizationId == organizationId);
            if (isActive.HasValue) query = query.Where(w => w.IsActive == isActive.Value);
            if (contractorId.HasValue) query = query.Where(w => w.ContractorId == contractorId.Value);
            if (laborCategoryId.HasValue) query = query.Where(w => w.LaborCategoryId == laborCategoryId.Value);
            if (gender.HasValue) query = query.Where(w => w.Gender == gender.Value);
            if (employmentType.HasValue) query = query.Where(w => w.EmploymentType == employmentType.Value);
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(w => w.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                         w.FirstName.Contains(search, StringComparison.OrdinalIgnoreCase));
            }
            return query;
        }
    }

    private sealed class FakeContractorStore : IContractorStore
    {
        public List<Contractor> Contractors { get; } = [];
        public List<AuditLog> AuditLogs { get; } = [];

        public Task<int> CountAsync(Guid organizationId, string? search, bool? isActive, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Filter(organizationId, search, isActive).Count());
        }

        public Task<IReadOnlyList<(Contractor Contractor, int WorkerCount)>> ListAsync(
            Guid organizationId,
            int skip,
            int take,
            string? search,
            bool? isActive,
            CancellationToken cancellationToken = default)
        {
            var result = Filter(organizationId, search, isActive)
                .Skip(skip)
                .Take(take)
                .Select(c => (c, 0))
                .ToList();
            return Task.FromResult<IReadOnlyList<(Contractor Contractor, int WorkerCount)>>(result);
        }

        public Task<Contractor?> FindAsync(Guid contractorId, Guid organizationId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Contractors.FirstOrDefault(c => c.Id == contractorId && c.OrganizationId == organizationId));
        }

        public Task<int> GetWorkerCountAsync(Guid contractorId, Guid organizationId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(0);
        }

        public Task<bool> NameExistsAsync(Guid organizationId, string name, Guid? excludingContractorId = null, CancellationToken cancellationToken = default)
        {
            var query = Contractors.Where(c => c.OrganizationId == organizationId && c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (excludingContractorId.HasValue)
            {
                query = query.Where(c => c.Id != excludingContractorId.Value);
            }
            return Task.FromResult(query.Any());
        }

        public void Add(Contractor contractor) => Contractors.Add(contractor);

        public void AddAuditLog(AuditLog auditLog) => AuditLogs.Add(auditLog);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        private IEnumerable<Contractor> Filter(Guid organizationId, string? search, bool? isActive)
        {
            var query = Contractors.Where(c => c.OrganizationId == organizationId);
            if (isActive.HasValue) query = query.Where(c => c.IsActive == isActive.Value);
            if (!string.IsNullOrWhiteSpace(search)) query = query.Where(c => c.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
            return query;
        }
    }

    private sealed class FakeLaborCategoryStore : ILaborCategoryStore
    {
        public List<LaborCategory> Categories { get; } = [];
        public List<AuditLog> AuditLogs { get; } = [];

        public Task<int> CountAsync(Guid organizationId, string? search, bool? isActive, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Filter(organizationId, search, isActive).Count());
        }

        public Task<IReadOnlyList<(LaborCategory Category, int WorkerCount)>> ListAsync(
            Guid organizationId,
            int skip,
            int take,
            string? search,
            bool? isActive,
            CancellationToken cancellationToken = default)
        {
            var result = Filter(organizationId, search, isActive)
                .Skip(skip)
                .Take(take)
                .Select(c => (c, 0))
                .ToList();
            return Task.FromResult<IReadOnlyList<(LaborCategory Category, int WorkerCount)>>(result);
        }

        public Task<LaborCategory?> FindAsync(Guid categoryId, Guid organizationId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Categories.FirstOrDefault(c => c.Id == categoryId && (c.IsSystem || c.OrganizationId == organizationId)));
        }

        public Task<int> GetWorkerCountAsync(Guid categoryId, Guid organizationId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(0);
        }

        public Task<bool> NameExistsAsync(Guid organizationId, string name, Guid? excludingCategoryId = null, CancellationToken cancellationToken = default)
        {
            var query = Categories.Where(c => (c.IsSystem || c.OrganizationId == organizationId) && c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (excludingCategoryId.HasValue)
            {
                query = query.Where(c => c.Id != excludingCategoryId.Value);
            }
            return Task.FromResult(query.Any());
        }

        public void Add(LaborCategory category) => Categories.Add(category);

        public void AddAuditLog(AuditLog auditLog) => AuditLogs.Add(auditLog);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        private IEnumerable<LaborCategory> Filter(Guid organizationId, string? search, bool? isActive)
        {
            var query = Categories.Where(c => c.IsSystem || c.OrganizationId == organizationId);
            if (isActive.HasValue) query = query.Where(c => c.IsActive == isActive.Value);
            if (!string.IsNullOrWhiteSpace(search)) query = query.Where(c => c.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
            return query;
        }
    }

    #endregion
}
