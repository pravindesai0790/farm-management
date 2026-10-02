using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.Expenses;
using FarmManagement.Application.Interfaces.Expenses;
using FarmManagement.Application.Services;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Services;

public class ExpenseServiceTests
{
    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _farmId = Guid.NewGuid();
    private readonly Guid _categoryId = Guid.NewGuid();
    private readonly Guid _currencyId = Guid.NewGuid();
    private readonly Guid _supplierId = Guid.NewGuid();
    private readonly Guid _farmAreaId = Guid.NewGuid();
    private readonly Guid _plantationId = Guid.NewGuid();
    private readonly Guid _cropCycleId = Guid.NewGuid();

    private ExpenseActor CreateActor(Guid? organizationId = null) =>
        new(_userId, organizationId ?? _organizationId);

    private FakeExpenseStore CreatePopulatedStore()
    {
        var store = new FakeExpenseStore();
        store.ValidFarms.Add((_farmId, _organizationId));
        store.ValidCategories.Add((_categoryId, _organizationId));
        store.ValidSuppliers.Add((_supplierId, _organizationId));
        store.ValidCurrencies.Add(_currencyId);
        store.ValidFarmAreas.Add((_farmAreaId, _farmId, _organizationId));
        store.ValidPlantations.Add((_plantationId, _farmId, _organizationId));
        store.ValidCropCycles.Add((_cropCycleId, _farmId, _organizationId));
        return store;
    }

    [Fact]
    public async Task CreateDraftAsync_ValidRequest_CreatesDraftAndAuditLog()
    {
        // Arrange
        var store = CreatePopulatedStore();
        var service = new ExpenseService(store);
        var request = new CreateExpenseRequest(
            _farmId,
            _categoryId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            "Direct purchase of organic fertilizer",
            150.50m,
            _currencyId,
            _supplierId,
            "INV-9999",
            _farmAreaId,
            _plantationId,
            _cropCycleId,
            null,
            "https://storage.example.com/receipt.pdf");

        // Act
        var result = await service.CreateDraftAsync(CreateActor(), request, "127.0.0.1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_farmId, result.FarmId);
        Assert.Equal(_categoryId, result.ExpenseCategoryId);
        Assert.Equal(150.50m, result.Amount);
        Assert.Equal(_currencyId, result.CurrencyId);
        Assert.Equal("Direct purchase of organic fertilizer", result.Description);
        Assert.Equal(ExpenseStatus.Draft, result.Status);
        Assert.Equal(_supplierId, result.SupplierId);
        Assert.Equal("INV-9999", result.ReferenceNumber);
        Assert.Equal(_farmAreaId, result.FarmAreaId);
        Assert.Equal(_plantationId, result.PlantationId);
        Assert.Equal(_cropCycleId, result.CropCycleId);
        Assert.Single(store.Expenses);
        Assert.Single(store.AuditLogs);
        Assert.Equal("Expense.Created", store.AuditLogs[0].Action);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateDraftAsync_BlankDescription_ThrowsValidationException(string? description)
    {
        var store = CreatePopulatedStore();
        var service = new ExpenseService(store);
        var request = new CreateExpenseRequest(
            _farmId,
            _categoryId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            description!,
            100m,
            _currencyId,
            null, null, null, null, null, null);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateDraftAsync(CreateActor(), request, "127.0.0.1"));

        Assert.True(ex.Errors.ContainsKey("description"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-15)]
    public async Task CreateDraftAsync_NonPositiveAmount_ThrowsValidationException(decimal amount)
    {
        var store = CreatePopulatedStore();
        var service = new ExpenseService(store);
        var request = new CreateExpenseRequest(
            _farmId,
            _categoryId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            "Valid description",
            amount,
            _currencyId,
            null, null, null, null, null, null);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateDraftAsync(CreateActor(), request, "127.0.0.1"));

        Assert.True(ex.Errors.ContainsKey("amount"));
    }

    [Fact]
    public async Task CreateDraftAsync_FutureDate_ThrowsValidationException()
    {
        var store = CreatePopulatedStore();
        var service = new ExpenseService(store);
        var request = new CreateExpenseRequest(
            _farmId,
            _categoryId,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            "Valid description",
            100m,
            _currencyId,
            null, null, null, null, null, null);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateDraftAsync(CreateActor(), request, "127.0.0.1"));

        Assert.True(ex.Errors.ContainsKey("expenseDate"));
    }

    [Fact]
    public async Task CreateDraftAsync_InvalidFarm_ThrowsValidationException()
    {
        var store = CreatePopulatedStore();
        var service = new ExpenseService(store);
        var request = new CreateExpenseRequest(
            Guid.NewGuid(), // unknown farm
            _categoryId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            "Valid description",
            100m,
            _currencyId,
            null, null, null, null, null, null);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateDraftAsync(CreateActor(), request, "127.0.0.1"));

        Assert.True(ex.Errors.ContainsKey("farmId"));
    }

    [Fact]
    public async Task CreateDraftAsync_AreaNotBelongingToFarm_ThrowsValidationException()
    {
        var store = CreatePopulatedStore();
        var service = new ExpenseService(store);
        var request = new CreateExpenseRequest(
            _farmId,
            _categoryId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            "Valid description",
            100m,
            _currencyId,
            null, null,
            Guid.NewGuid(), // unknown or different farm area
            null, null, null);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateDraftAsync(CreateActor(), request, "127.0.0.1"));

        Assert.True(ex.Errors.ContainsKey("farmAreaId"));
    }

    [Fact]
    public async Task CreateDraftAsync_StageNotBelongingToCycle_ThrowsValidationException()
    {
        var store = CreatePopulatedStore();
        var service = new ExpenseService(store);
        var request = new CreateExpenseRequest(
            _farmId,
            _categoryId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            "Valid description",
            100m,
            _currencyId,
            null, null,
            _farmAreaId,
            _plantationId,
            _cropCycleId,
            Guid.NewGuid(), // unknown stage
            null);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateDraftAsync(CreateActor(), request, "127.0.0.1"));

        Assert.True(ex.Errors.ContainsKey("cropCycleStageId"));
    }

    [Fact]
    public async Task UpdateDraftAsync_ValidDraft_UpdatesFields()
    {
        // Arrange
        var store = CreatePopulatedStore();
        var expense = Expense.CreateDraft(
            _organizationId,
            _farmId,
            _categoryId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            "Initial desc",
            100m,
            _currencyId,
            _userId);
        store.Expenses.Add(expense);

        var service = new ExpenseService(store);
        var request = new UpdateExpenseRequest(
            _farmId,
            _categoryId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            "Updated desc",
            250m,
            _currencyId,
            _supplierId,
            "REF-123",
            null, null, null, null);

        // Act
        var result = await service.UpdateDraftAsync(CreateActor(), expense.Id, request, "127.0.0.1");

        // Assert
        Assert.Equal(250m, result.Amount);
        Assert.Equal("Updated desc", result.Description);
        Assert.Equal(_supplierId, result.SupplierId);
        Assert.Equal("REF-123", result.ReferenceNumber);
        Assert.Equal("Expense.Updated", store.AuditLogs[^1].Action);
    }

    [Fact]
    public async Task UpdateDraftAsync_WhenAlreadyPosted_ThrowsValidationException()
    {
        // Arrange
        var store = CreatePopulatedStore();
        var expense = Expense.CreateDraft(
            _organizationId,
            _farmId,
            _categoryId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            "Initial desc",
            100m,
            _currencyId,
            _userId);
        expense.Post(_userId);
        store.Expenses.Add(expense);

        var service = new ExpenseService(store);
        var request = new UpdateExpenseRequest(
            _farmId,
            _categoryId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            "Updated desc",
            250m,
            _currencyId,
            null, null, null, null, null, null);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpdateDraftAsync(CreateActor(), expense.Id, request, "127.0.0.1"));
    }

    [Fact]
    public async Task PostAsync_ValidDraft_TransitionsToPosted()
    {
        // Arrange
        var store = CreatePopulatedStore();
        var expense = Expense.CreateDraft(
            _organizationId,
            _farmId,
            _categoryId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            "Initial desc",
            100m,
            _currencyId,
            _userId);
        store.Expenses.Add(expense);

        var service = new ExpenseService(store);

        // Act
        var result = await service.PostAsync(CreateActor(), expense.Id, "127.0.0.1");

        // Assert
        Assert.Equal(ExpenseStatus.Posted, result.Status);
        Assert.NotNull(result.PostedAt);
        Assert.Equal(_userId, result.PostedBy);
        Assert.Equal("Expense.Posted", store.AuditLogs[^1].Action);
    }

    [Fact]
    public async Task PostAsync_AlreadyPosted_ThrowsValidationException()
    {
        // Arrange
        var store = CreatePopulatedStore();
        var expense = Expense.CreateDraft(
            _organizationId,
            _farmId,
            _categoryId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            "Initial desc",
            100m,
            _currencyId,
            _userId);
        expense.Post(_userId);
        store.Expenses.Add(expense);

        var service = new ExpenseService(store);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            service.PostAsync(CreateActor(), expense.Id, "127.0.0.1"));
    }

    [Fact]
    public async Task ReverseAsync_ValidPostedExpense_TransitionsToReversed()
    {
        // Arrange
        var store = CreatePopulatedStore();
        var expense = Expense.CreateDraft(
            _organizationId,
            _farmId,
            _categoryId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            "Initial desc",
            100m,
            _currencyId,
            _userId);
        expense.Post(_userId);
        store.Expenses.Add(expense);

        var service = new ExpenseService(store);
        var request = new ReverseExpenseRequest("Wrong billing category");

        // Act
        var result = await service.ReverseAsync(CreateActor(), expense.Id, request, "127.0.0.1");

        // Assert
        Assert.Equal(ExpenseStatus.Reversed, result.Status);
        Assert.Equal("Wrong billing category", result.ReversalReason);
        Assert.NotNull(result.ReversedAt);
        Assert.Equal(_userId, result.ReversedBy);
        Assert.Equal("Expense.Reversed", store.AuditLogs[^1].Action);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ReverseAsync_BlankReason_ThrowsValidationException(string? reason)
    {
        // Arrange
        var store = CreatePopulatedStore();
        var expense = Expense.CreateDraft(
            _organizationId,
            _farmId,
            _categoryId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            "Initial desc",
            100m,
            _currencyId,
            _userId);
        expense.Post(_userId);
        store.Expenses.Add(expense);

        var service = new ExpenseService(store);
        var request = new ReverseExpenseRequest(reason!);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.ReverseAsync(CreateActor(), expense.Id, request, "127.0.0.1"));

        Assert.True(ex.Errors.ContainsKey("reason"));
    }

    [Fact]
    public async Task ReverseAsync_WhenDraft_ThrowsValidationException()
    {
        // Arrange
        var store = CreatePopulatedStore();
        var expense = Expense.CreateDraft(
            _organizationId,
            _farmId,
            _categoryId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            "Initial desc",
            100m,
            _currencyId,
            _userId);
        store.Expenses.Add(expense);

        var service = new ExpenseService(store);
        var request = new ReverseExpenseRequest("Cannot reverse draft");

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            service.ReverseAsync(CreateActor(), expense.Id, request, "127.0.0.1"));
    }

    [Fact]
    public async Task GetAsync_CrossOrganization_ThrowsResourceNotFoundException()
    {
        // Arrange
        var store = CreatePopulatedStore();
        var otherOrgId = Guid.NewGuid();
        var expense = Expense.CreateDraft(
            otherOrgId,
            _farmId,
            _categoryId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            "Other org expense",
            100m,
            _currencyId,
            _userId);
        store.Expenses.Add(expense);

        var service = new ExpenseService(store);

        // Act & Assert
        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            service.GetAsync(CreateActor(), expense.Id));
    }
}

public class FakeExpenseStore : IExpenseStore
{
    public List<Expense> Expenses { get; } = new();
    public List<AuditLog> AuditLogs { get; } = new();
    public HashSet<(Guid FarmId, Guid OrgId)> ValidFarms { get; } = new();
    public HashSet<(Guid CategoryId, Guid OrgId)> ValidCategories { get; } = new();
    public HashSet<(Guid SupplierId, Guid OrgId)> ValidSuppliers { get; } = new();
    public HashSet<Guid> ValidCurrencies { get; } = new();
    public HashSet<(Guid FarmAreaId, Guid FarmId, Guid OrgId)> ValidFarmAreas { get; } = new();
    public HashSet<(Guid PlantationId, Guid FarmId, Guid OrgId)> ValidPlantations { get; } = new();
    public HashSet<(Guid CropCycleId, Guid FarmId, Guid OrgId)> ValidCropCycles { get; } = new();
    public HashSet<(Guid StageId, Guid CropCycleId, Guid OrgId)> ValidCropCycleStages { get; } = new();

    public Task<int> CountAsync(Guid organizationId, ExpenseFilter filter, CancellationToken cancellationToken = default)
    {
        var count = FilterExpenses(organizationId, filter).Count();
        return Task.FromResult(count);
    }

    public Task<IReadOnlyList<Expense>> ListAsync(
        Guid organizationId,
        ExpenseFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var list = FilterExpenses(organizationId, filter).Skip(skip).Take(take).ToList();
        return Task.FromResult<IReadOnlyList<Expense>>(list);
    }

    public Task<Expense?> FindAsync(Guid expenseId, Guid organizationId, CancellationToken cancellationToken = default)
    {
        var expense = Expenses.FirstOrDefault(e => e.Id == expenseId && e.OrganizationId == organizationId);
        return Task.FromResult(expense);
    }

    public Task<bool> FarmBelongsToOrganizationAsync(Guid farmId, Guid organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(ValidFarms.Contains((farmId, organizationId)));

    public Task<bool> CategoryExistsAndActiveAsync(Guid categoryId, Guid organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(ValidCategories.Contains((categoryId, organizationId)));

    public Task<bool> CurrencyExistsAndActiveAsync(Guid currencyId, CancellationToken cancellationToken = default) =>
        Task.FromResult(ValidCurrencies.Contains(currencyId));

    public Task<bool> SupplierBelongsToOrganizationAndActiveAsync(Guid supplierId, Guid organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(ValidSuppliers.Contains((supplierId, organizationId)));

    public Task<bool> AreaBelongsToFarmAsync(Guid farmAreaId, Guid farmId, Guid organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(ValidFarmAreas.Contains((farmAreaId, farmId, organizationId)));

    public Task<bool> PlantationBelongsToFarmAsync(Guid plantationId, Guid farmId, Guid organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(ValidPlantations.Contains((plantationId, farmId, organizationId)));

    public Task<bool> CropCycleBelongsToFarmAsync(Guid cropCycleId, Guid farmId, Guid organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(ValidCropCycles.Contains((cropCycleId, farmId, organizationId)));

    public Task<bool> StageBelongsToCropCycleAsync(Guid cropCycleStageId, Guid cropCycleId, Guid organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(ValidCropCycleStages.Contains((cropCycleStageId, cropCycleId, organizationId)));

    public void Add(Expense expense) => Expenses.Add(expense);

    public void AddAuditLog(AuditLog auditLog) => AuditLogs.Add(auditLog);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    private IEnumerable<Expense> FilterExpenses(Guid organizationId, ExpenseFilter filter)
    {
        var query = Expenses.Where(e => e.OrganizationId == organizationId);
        if (filter.FarmId.HasValue) query = query.Where(e => e.FarmId == filter.FarmId.Value);
        if (filter.CategoryId.HasValue) query = query.Where(e => e.ExpenseCategoryId == filter.CategoryId.Value);
        if (filter.SupplierId.HasValue) query = query.Where(e => e.SupplierId == filter.SupplierId.Value);
        if (filter.Status.HasValue) query = query.Where(e => e.Status == filter.Status.Value);
        if (filter.From.HasValue) query = query.Where(e => e.ExpenseDate >= filter.From.Value);
        if (filter.To.HasValue) query = query.Where(e => e.ExpenseDate <= filter.To.Value);
        return query;
    }
}
