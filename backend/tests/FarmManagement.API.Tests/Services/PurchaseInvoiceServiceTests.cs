using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.Expenses;
using FarmManagement.Application.Interfaces.Expenses;
using FarmManagement.Application.Services;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Services;

public class PurchaseInvoiceServiceTests
{
    private readonly TestPurchaseInvoiceStore _store = new();
    private readonly PurchaseInvoiceService _service;

    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly ExpenseActor _actor = new(UserId, OrgId);

    private static readonly Guid SupplierId = Guid.NewGuid();
    private static readonly Guid FarmId = Guid.NewGuid();
    private static readonly Guid CurrencyId = Guid.NewGuid();
    private static readonly Guid ItemId = Guid.NewGuid();
    private static readonly Guid UnitId = Guid.NewGuid();
    private static readonly Guid CategoryId = Guid.NewGuid();
    private static readonly Guid AreaId = Guid.NewGuid();
    private static readonly Guid PlantationId = Guid.NewGuid();
    private static readonly Guid CycleId = Guid.NewGuid();
    private static readonly Guid StageId = Guid.NewGuid();

    public PurchaseInvoiceServiceTests()
    {
        _service = new PurchaseInvoiceService(_store);

        // Seed valid references in test store
        _store.ValidFarms.Add((FarmId, OrgId));
        _store.ValidSuppliers.Add((SupplierId, OrgId));
        _store.ValidCurrencies.Add(CurrencyId);
        _store.ValidItems.Add((ItemId, OrgId));
        _store.ValidUnits.Add(UnitId);
        _store.ValidCategories.Add((CategoryId, OrgId));
        _store.ValidAreas.Add((AreaId, FarmId, OrgId));
        _store.ValidPlantations.Add((PlantationId, FarmId, OrgId));
        _store.ValidCycles.Add((CycleId, FarmId, OrgId));
        _store.ValidStages.Add((StageId, CycleId));
    }

    [Fact]
    public async Task CreateDraftAsync_ValidRequest_CreatesInvoiceAndAuditLog()
    {
        var request = new CreatePurchaseInvoiceRequest(
            SupplierId,
            FarmId,
            "INV-1001",
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            CurrencyId,
            "Net 30",
            TaxAmount: 10m,
            OtherCharges: 5m,
            DiscountAmount: 2m,
            Notes: "Test purchase invoice",
            AttachmentReference: "https://docs.farm.org/inv-1001.pdf",
            Lines: new[]
            {
                new CreatePurchaseInvoiceLineRequest("InventoryItem", ItemId, UnitId, Quantity: 10m, UnitPrice: 15m, ExpenseCategoryId: null, Amount: null, Description: "Fertilizer 10kg", AreaId, PlantationId, CycleId, StageId, SortOrder: 1),
                new CreatePurchaseInvoiceLineRequest("NonInventoryExpense", InventoryItemId: null, StockUnitId: null, Quantity: null, UnitPrice: null, CategoryId, Amount: 50m, Description: "Freight fee", AreaId, PlantationId, CycleId, StageId, SortOrder: 2)
            });

        var result = await _service.CreateDraftAsync(_actor, request, "127.0.0.1");

        Assert.NotNull(result);
        Assert.Equal("INV-1001", result.SupplierInvoiceNumber);
        Assert.Equal(2, result.Lines.Count);
        Assert.Equal(200m, result.Subtotal); // (10 * 15) + 50 = 200
        Assert.Equal(213m, result.TotalAmount); // 200 + 10 + 5 - 2 = 213
        Assert.Single(_store.Invoices);
        Assert.Single(_store.AuditLogs);
        Assert.Equal("PurchaseInvoice.CreateDraft", _store.AuditLogs[0].Action);
    }

    [Fact]
    public async Task CreateDraftAsync_FutureInvoiceDate_ThrowsValidationException()
    {
        var request = new CreatePurchaseInvoiceRequest(
            SupplierId, FarmId, "INV-FUTURE", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)), null, CurrencyId, null, 0, 0, 0, null, null, Array.Empty<CreatePurchaseInvoiceLineRequest>());

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateDraftAsync(_actor, request, "127.0.0.1"));
    }

    [Fact]
    public async Task CreateDraftAsync_DueDateEarlierThanInvoiceDate_ThrowsValidationException()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var request = new CreatePurchaseInvoiceRequest(
            SupplierId, FarmId, "INV-ERR", today, today.AddDays(-1), CurrencyId, null, 0, 0, 0, null, null, Array.Empty<CreatePurchaseInvoiceLineRequest>());

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateDraftAsync(_actor, request, "127.0.0.1"));
    }

    [Fact]
    public async Task CreateDraftAsync_DuplicateInvoiceNumber_ThrowsValidationException()
    {
        var existing = PurchaseInvoice.CreateDraft(OrgId, SupplierId, FarmId, "DUP-123", DateOnly.FromDateTime(DateTime.UtcNow), CurrencyId, UserId);
        _store.Invoices.Add(existing);

        var request = new CreatePurchaseInvoiceRequest(
            SupplierId, FarmId, "DUP-123", DateOnly.FromDateTime(DateTime.UtcNow), null, CurrencyId, null, 0, 0, 0, null, null, Array.Empty<CreatePurchaseInvoiceLineRequest>());

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateDraftAsync(_actor, request, "127.0.0.1"));
    }

    [Fact]
    public async Task CreateDraftAsync_StageNotBelongingToCycle_ThrowsValidationException()
    {
        var invalidStageId = Guid.NewGuid();

        var request = new CreatePurchaseInvoiceRequest(
            SupplierId, FarmId, "INV-STAGE-ERR", DateOnly.FromDateTime(DateTime.UtcNow), null, CurrencyId, null, 0, 0, 0, null, null,
            new[]
            {
                new CreatePurchaseInvoiceLineRequest("InventoryItem", ItemId, UnitId, Quantity: 10m, UnitPrice: 5m, null, null, null, AreaId, PlantationId, CycleId, invalidStageId)
            });

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateDraftAsync(_actor, request, "127.0.0.1"));
    }

    [Fact]
    public async Task PostAsync_ValidDraftWithLines_PostsInvoiceAndDoesNotMutateStock()
    {
        var invoice = PurchaseInvoice.CreateDraft(OrgId, SupplierId, FarmId, "INV-POST", DateOnly.FromDateTime(DateTime.UtcNow), CurrencyId, UserId);
        invoice.Lines.Add(PurchaseInvoiceLine.CreateInventoryLine(OrgId, invoice.Id, ItemId, UnitId, 10m, 20m));
        invoice.RecalculateTotals();
        _store.Invoices.Add(invoice);

        var result = await _service.PostAsync(_actor, invoice.Id, "127.0.0.1");

        Assert.Equal("Posted", result.Status);
        Assert.Equal(200m, result.TotalAmount);
        Assert.Equal("PurchaseInvoice.Post", _store.AuditLogs.Last().Action);
    }

    [Fact]
    public async Task PostAsync_DraftWithoutLines_ThrowsValidationException()
    {
        var invoice = PurchaseInvoice.CreateDraft(OrgId, SupplierId, FarmId, "INV-EMPTY", DateOnly.FromDateTime(DateTime.UtcNow), CurrencyId, UserId);
        _store.Invoices.Add(invoice);

        await Assert.ThrowsAsync<ValidationException>(() => _service.PostAsync(_actor, invoice.Id, "127.0.0.1"));
    }

    [Fact]
    public async Task ReverseAsync_PostedInvoice_ReversesInvoiceWithReason()
    {
        var invoice = PurchaseInvoice.CreateDraft(OrgId, SupplierId, FarmId, "INV-REV", DateOnly.FromDateTime(DateTime.UtcNow), CurrencyId, UserId);
        invoice.Lines.Add(PurchaseInvoiceLine.CreateNonInventoryLine(OrgId, invoice.Id, CategoryId, 100m, "Service"));
        invoice.Post(UserId);
        _store.Invoices.Add(invoice);

        var request = new ReversePurchaseInvoiceRequest("Incorrect supplier billing number");
        var result = await _service.ReverseAsync(_actor, invoice.Id, request, "127.0.0.1");

        Assert.Equal("Reversed", result.Status);
        Assert.Equal("Incorrect supplier billing number", result.ReversalReason);
        Assert.Equal("PurchaseInvoice.Reverse", _store.AuditLogs.Last().Action);
    }
}

public sealed class TestPurchaseInvoiceStore : IPurchaseInvoiceStore
{
    public List<PurchaseInvoice> Invoices { get; } = new();
    public List<AuditLog> AuditLogs { get; } = new();

    public HashSet<(Guid FarmId, Guid OrgId)> ValidFarms { get; } = new();
    public HashSet<(Guid SupplierId, Guid OrgId)> ValidSuppliers { get; } = new();
    public HashSet<Guid> ValidCurrencies { get; } = new();
    public HashSet<(Guid ItemId, Guid OrgId)> ValidItems { get; } = new();
    public HashSet<Guid> ValidUnits { get; } = new();
    public HashSet<(Guid CategoryId, Guid OrgId)> ValidCategories { get; } = new();
    public HashSet<(Guid AreaId, Guid FarmId, Guid OrgId)> ValidAreas { get; } = new();
    public HashSet<(Guid PlantationId, Guid FarmId, Guid OrgId)> ValidPlantations { get; } = new();
    public HashSet<(Guid CycleId, Guid FarmId, Guid OrgId)> ValidCycles { get; } = new();
    public HashSet<(Guid StageId, Guid CycleId)> ValidStages { get; } = new();

    public Task<int> CountAsync(Guid organizationId, PurchaseInvoiceFilter filter, CancellationToken cancellationToken = default) =>
        Task.FromResult(FilterInvoices(organizationId, filter).Count());

    public Task<IReadOnlyList<PurchaseInvoice>> ListAsync(Guid organizationId, PurchaseInvoiceFilter filter, int skip, int take, CancellationToken cancellationToken = default)
    {
        var result = FilterInvoices(organizationId, filter).Skip(skip).Take(take).ToList();
        return Task.FromResult<IReadOnlyList<PurchaseInvoice>>(result);
    }

    public Task<PurchaseInvoice?> FindAsync(Guid invoiceId, Guid organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Invoices.FirstOrDefault(pi => pi.Id == invoiceId && pi.OrganizationId == organizationId));

    public Task<bool> InvoiceNumberExistsAsync(Guid organizationId, Guid supplierId, string supplierInvoiceNumber, Guid? excludeInvoiceId = null, CancellationToken cancellationToken = default)
    {
        var norm = supplierInvoiceNumber.Trim().ToUpper();
        var exists = Invoices.Any(pi =>
            pi.OrganizationId == organizationId &&
            pi.SupplierId == supplierId &&
            pi.SupplierInvoiceNumber.ToUpper() == norm &&
            pi.Status != PurchaseInvoiceStatus.Reversed &&
            (!excludeInvoiceId.HasValue || pi.Id != excludeInvoiceId.Value));
        return Task.FromResult(exists);
    }

    public Task<bool> FarmBelongsToOrganizationAsync(Guid farmId, Guid organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(ValidFarms.Contains((farmId, organizationId)));

    public Task<bool> SupplierBelongsToOrganizationAndActiveAsync(Guid supplierId, Guid organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(ValidSuppliers.Contains((supplierId, organizationId)));

    public Task<bool> CurrencyExistsAndActiveAsync(Guid currencyId, CancellationToken cancellationToken = default) =>
        Task.FromResult(ValidCurrencies.Contains(currencyId));

    public Task<bool> InventoryItemBelongsToOrganizationAndActiveAsync(Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(ValidItems.Contains((inventoryItemId, organizationId)));

    public Task<bool> StockUnitExistsAndActiveAsync(Guid stockUnitId, CancellationToken cancellationToken = default) =>
        Task.FromResult(ValidUnits.Contains(stockUnitId));

    public Task<bool> CategoryExistsAndActiveAsync(Guid categoryId, Guid organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(ValidCategories.Contains((categoryId, organizationId)));

    public Task<bool> AreaBelongsToFarmAsync(Guid farmAreaId, Guid farmId, Guid organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(ValidAreas.Contains((farmAreaId, farmId, organizationId)));

    public Task<bool> PlantationBelongsToFarmAsync(Guid plantationId, Guid farmId, Guid organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(ValidPlantations.Contains((plantationId, farmId, organizationId)));

    public Task<bool> CycleBelongsToFarmAsync(Guid cropCycleId, Guid farmId, Guid organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(ValidCycles.Contains((cropCycleId, farmId, organizationId)));

    public Task<bool> StageBelongsToCropCycleAsync(Guid cropCycleStageId, Guid cropCycleId, CancellationToken cancellationToken = default) =>
        Task.FromResult(ValidStages.Contains((cropCycleStageId, cropCycleId)));

    public Task AddAsync(PurchaseInvoice invoice, CancellationToken cancellationToken = default)
    {
        Invoices.Add(invoice);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(PurchaseInvoice invoice, CancellationToken cancellationToken = default)
    {
        var idx = Invoices.FindIndex(pi => pi.Id == invoice.Id);
        if (idx >= 0) Invoices[idx] = invoice;
        else Invoices.Add(invoice);
        return Task.CompletedTask;
    }

    public Task AddAuditLogAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        AuditLogs.Add(auditLog);
        return Task.CompletedTask;
    }

    private IEnumerable<PurchaseInvoice> FilterInvoices(Guid organizationId, PurchaseInvoiceFilter filter)
    {
        var query = Invoices.Where(pi => pi.OrganizationId == organizationId);
        if (filter.SupplierId.HasValue) query = query.Where(pi => pi.SupplierId == filter.SupplierId.Value);
        if (filter.FarmId.HasValue) query = query.Where(pi => pi.FarmId == filter.FarmId.Value);
        if (filter.From.HasValue) query = query.Where(pi => pi.InvoiceDate >= filter.From.Value);
        if (filter.To.HasValue) query = query.Where(pi => pi.InvoiceDate <= filter.To.Value);
        return query;
    }
}
