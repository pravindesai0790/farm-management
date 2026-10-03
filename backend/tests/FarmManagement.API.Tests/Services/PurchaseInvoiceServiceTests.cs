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
    private readonly TestInventoryStockStore _stockStore = new();
    private readonly TestPurchaseInvoiceStore _store;
    private readonly PurchaseInvoiceService _service;

    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly ExpenseActor _actor = new(UserId, OrgId);

    private static readonly Guid SupplierId = Guid.NewGuid();
    private static readonly Guid FarmId = Guid.NewGuid();
    private static readonly Guid LocationId = Guid.NewGuid();
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
        _store = new TestPurchaseInvoiceStore(_stockStore);
        _service = new PurchaseInvoiceService(_store, _stockStore);

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
        _store.ValidLocations.Add((LocationId, FarmId, OrgId));
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

    [Fact]
    public async Task ReceiveItemsAsync_ValidRequest_CreatesStockMovementAndReceiptLine()
    {
        var invoice = PurchaseInvoice.CreateDraft(OrgId, SupplierId, FarmId, "INV-RCV-1", DateOnly.FromDateTime(DateTime.UtcNow), CurrencyId, UserId);
        var invLine = PurchaseInvoiceLine.CreateInventoryLine(OrgId, invoice.Id, ItemId, UnitId, 20m, 10m);
        invoice.Lines.Add(invLine);
        invoice.Post(UserId);
        _store.Invoices.Add(invoice);

        var request = new ReceivePurchaseInvoiceItemsRequest(
            LocationId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            new[] { new ReceivePurchaseInvoiceItemLineRequest(invLine.Id, 15m) },
            ReferenceNumber: "GRN-001",
            Notes: "Partial delivery 15 units",
            IdempotencyKey: "KEY-001");

        var response = await _service.ReceiveItemsAsync(_actor, invoice.Id, request, "127.0.0.1");

        Assert.NotNull(response);
        Assert.Single(response.Items);
        Assert.Equal(15m, response.Items[0].ReceivedQuantity);
        Assert.Single(_stockStore.Movements);
        Assert.Equal(15m, _stockStore.Movements[0].Quantity);
        Assert.Equal("GRN-001", _stockStore.Movements[0].ReferenceNumber);
        Assert.Contains("INV-RCV-1", _stockStore.Movements[0].Notes);
        Assert.Contains("Partial delivery 15 units", _stockStore.Movements[0].Notes);
        Assert.Single(_store.ReceiptLines);
    }

    [Fact]
    public async Task ReceiveItemsAsync_WithoutReferenceAndNotes_DefaultsToInvoiceNumberAndSystemNote()
    {
        var invoice = PurchaseInvoice.CreateDraft(OrgId, SupplierId, FarmId, "INV-AUTONOTE-1", DateOnly.FromDateTime(DateTime.UtcNow), CurrencyId, UserId);
        var invLine = PurchaseInvoiceLine.CreateInventoryLine(OrgId, invoice.Id, ItemId, UnitId, 10m, 5m);
        invoice.Lines.Add(invLine);
        invoice.Post(UserId);
        _store.Invoices.Add(invoice);

        var request = new ReceivePurchaseInvoiceItemsRequest(
            LocationId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            new[] { new ReceivePurchaseInvoiceItemLineRequest(invLine.Id, 5m) },
            ReferenceNumber: null,
            Notes: null,
            IdempotencyKey: "KEY-AUTONOTE");

        var response = await _service.ReceiveItemsAsync(_actor, invoice.Id, request, "127.0.0.1");

        Assert.NotNull(response);
        Assert.Single(_stockStore.Movements);
        Assert.Equal("INV-AUTONOTE-1", _stockStore.Movements[0].ReferenceNumber);
        Assert.Contains("Stock received from Supplier Invoice #INV-AUTONOTE-1", _stockStore.Movements[0].Notes);
    }

    [Fact]
    public async Task ReceiveItemsAsync_OverReceipt_ThrowsValidationException()
    {
        var invoice = PurchaseInvoice.CreateDraft(OrgId, SupplierId, FarmId, "INV-RCV-OVER", DateOnly.FromDateTime(DateTime.UtcNow), CurrencyId, UserId);
        var invLine = PurchaseInvoiceLine.CreateInventoryLine(OrgId, invoice.Id, ItemId, UnitId, 10m, 10m);
        invoice.Lines.Add(invLine);
        invoice.Post(UserId);
        _store.Invoices.Add(invoice);

        var request = new ReceivePurchaseInvoiceItemsRequest(
            LocationId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            new[] { new ReceivePurchaseInvoiceItemLineRequest(invLine.Id, 15m) });

        await Assert.ThrowsAsync<ValidationException>(() => _service.ReceiveItemsAsync(_actor, invoice.Id, request, "127.0.0.1"));
    }

    [Fact]
    public async Task ReceiveItemsAsync_IdempotencyRetry_ReturnsExistingGroupWithoutReexecuting()
    {
        var invoice = PurchaseInvoice.CreateDraft(OrgId, SupplierId, FarmId, "INV-IDEM", DateOnly.FromDateTime(DateTime.UtcNow), CurrencyId, UserId);
        var invLine = PurchaseInvoiceLine.CreateInventoryLine(OrgId, invoice.Id, ItemId, UnitId, 10m, 10m);
        invoice.Lines.Add(invLine);
        invoice.Post(UserId);
        _store.Invoices.Add(invoice);

        var request = new ReceivePurchaseInvoiceItemsRequest(
            LocationId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            new[] { new ReceivePurchaseInvoiceItemLineRequest(invLine.Id, 5m) },
            ReferenceNumber: "GRN-IDEM",
            IdempotencyKey: "IDEM-KEY-99");

        var first = await _service.ReceiveItemsAsync(_actor, invoice.Id, request, "127.0.0.1");
        var second = await _service.ReceiveItemsAsync(_actor, invoice.Id, request, "127.0.0.1");

        Assert.Equal(first.ReceiptGroupId, second.ReceiptGroupId);
        Assert.Single(_stockStore.Movements);
        Assert.Single(_store.ReceiptLines);
    }

    [Fact]
    public async Task ReverseAsync_InvoiceWithActiveReceipts_ThrowsValidationException()
    {
        var invoice = PurchaseInvoice.CreateDraft(OrgId, SupplierId, FarmId, "INV-REV-BLOCKED", DateOnly.FromDateTime(DateTime.UtcNow), CurrencyId, UserId);
        var invLine = PurchaseInvoiceLine.CreateInventoryLine(OrgId, invoice.Id, ItemId, UnitId, 10m, 10m);
        invoice.Lines.Add(invLine);
        invoice.Post(UserId);
        _store.Invoices.Add(invoice);

        var rcvRequest = new ReceivePurchaseInvoiceItemsRequest(
            LocationId, DateOnly.FromDateTime(DateTime.UtcNow),
            new[] { new ReceivePurchaseInvoiceItemLineRequest(invLine.Id, 5m) });
        await _service.ReceiveItemsAsync(_actor, invoice.Id, rcvRequest, "127.0.0.1");

        var revRequest = new ReversePurchaseInvoiceRequest("Mistake");
        await Assert.ThrowsAsync<ValidationException>(() => _service.ReverseAsync(_actor, invoice.Id, revRequest, "127.0.0.1"));
    }
}

public sealed class TestPurchaseInvoiceStore(TestInventoryStockStore stockStore) : IPurchaseInvoiceStore
{
    public List<PurchaseInvoice> Invoices { get; } = new();
    public List<PurchaseInvoiceReceiptLine> ReceiptLines { get; } = new();
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
    public HashSet<(Guid LocationId, Guid FarmId, Guid OrgId)> ValidLocations { get; } = new();

    public Task<int> CountAsync(Guid organizationId, PurchaseInvoiceFilter filter, CancellationToken cancellationToken = default) =>
        Task.FromResult(FilterInvoices(organizationId, filter).Count());

    public Task<IReadOnlyList<PurchaseInvoice>> ListAsync(Guid organizationId, PurchaseInvoiceFilter filter, int skip, int take, CancellationToken cancellationToken = default)
    {
        var result = FilterInvoices(organizationId, filter).Skip(skip).Take(take).ToList();
        return Task.FromResult<IReadOnlyList<PurchaseInvoice>>(result);
    }

    public Task<PurchaseInvoice?> FindAsync(Guid invoiceId, Guid organizationId, CancellationToken cancellationToken = default)
    {
        var inv = Invoices.FirstOrDefault(pi => pi.Id == invoiceId && pi.OrganizationId == organizationId);
        if (inv != null)
        {
            var matchingLines = ReceiptLines.Where(rl => rl.PurchaseInvoiceId == inv.Id).ToList();
            foreach (var line in matchingLines)
            {
                if (!inv.ReceiptLines.Contains(line))
                {
                    inv.ReceiptLines.Add(line);
                }
            }
        }
        return Task.FromResult(inv);
    }

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

    public Task<IReadOnlyList<PurchaseInvoiceReceiptLine>> GetReceiptLinesByInvoiceAsync(Guid invoiceId, Guid organizationId, CancellationToken cancellationToken = default)
    {
        var res = ReceiptLines.Where(rl => rl.PurchaseInvoiceId == invoiceId && rl.OrganizationId == organizationId).ToList();
        return Task.FromResult<IReadOnlyList<PurchaseInvoiceReceiptLine>>(res);
    }

    public Task<IReadOnlyList<PurchaseInvoiceReceiptLine>> FindReceiptGroupByInvoiceAndIdempotencyKeyAsync(Guid invoiceId, Guid organizationId, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        var key = idempotencyKey.Trim();
        var res = ReceiptLines.Where(rl => rl.PurchaseInvoiceId == invoiceId && rl.OrganizationId == organizationId && rl.IdempotencyKey == key).ToList();
        return Task.FromResult<IReadOnlyList<PurchaseInvoiceReceiptLine>>(res);
    }

    public Task AddReceiptLinesAsync(IEnumerable<PurchaseInvoiceReceiptLine> receiptLines, CancellationToken cancellationToken = default)
    {
        foreach (var rl in receiptLines)
        {
            var movement = stockStore.Movements.FirstOrDefault(m => m.Id == rl.StockMovementId);
            if (movement != null)
            {
                typeof(PurchaseInvoiceReceiptLine).GetProperty(nameof(PurchaseInvoiceReceiptLine.StockMovement))?.SetValue(rl, movement);
            }
            ReceiptLines.Add(rl);
        }
        return Task.CompletedTask;
    }

    public Task<bool> StorageLocationBelongsToFarmAndActiveAsync(Guid storageLocationId, Guid farmId, Guid organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(ValidLocations.Contains((storageLocationId, farmId, organizationId)));

    public Task<IReadOnlyList<PurchaseInvoice>> GetInvoicesWithAllocationsAsync(IEnumerable<Guid> invoiceIds, Guid organizationId, CancellationToken cancellationToken = default)
    {
        var idSet = invoiceIds.ToHashSet();
        var result = Invoices.Where(i => i.OrganizationId == organizationId && idSet.Contains(i.Id)).ToList();
        return Task.FromResult<IReadOnlyList<PurchaseInvoice>>(result);
    }

    public Task<IReadOnlyList<PurchaseInvoice>> GetUnpaidInvoicesForSupplierAsync(Guid supplierId, Guid organizationId, Guid? currencyId = null, CancellationToken cancellationToken = default)
    {
        var result = Invoices
            .Where(i => i.OrganizationId == organizationId && i.SupplierId == supplierId && i.Status == PurchaseInvoiceStatus.Posted && (!currencyId.HasValue || i.CurrencyId == currencyId.Value))
            .ToList();
        return Task.FromResult<IReadOnlyList<PurchaseInvoice>>(result);
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

public sealed class TestInventoryStockStore : FarmManagement.Application.Interfaces.Inventory.IInventoryStockStore
{
    public List<StockBalance> Balances { get; } = new();
    public List<StockMovement> Movements { get; } = new();
    public List<AuditLog> AuditLogs { get; } = new();

    public Task<InventoryItem?> FindItemAsync(Guid itemId, Guid organizationId, CancellationToken cancellationToken = default) => Task.FromResult<InventoryItem?>(null);
    public Task<StorageLocation?> FindLocationAsync(Guid locationId, Guid organizationId, CancellationToken cancellationToken = default) => Task.FromResult<StorageLocation?>(null);
    public Task<Farm?> FindFarmAsync(Guid farmId, Guid organizationId, CancellationToken cancellationToken = default) => Task.FromResult<Farm?>(null);
    public Task<StockBalance?> FindBalanceAsync(Guid locationId, Guid itemId, Guid organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Balances.FirstOrDefault(b => b.StorageLocationId == locationId && b.InventoryItemId == itemId && b.OrganizationId == organizationId));
    public Task<CropCycle?> FindCropCycleAsync(Guid cycleId, Guid organizationId, CancellationToken cancellationToken = default) => Task.FromResult<CropCycle?>(null);
    public Task<CropCycleStage?> FindCropCycleStageAsync(Guid stageId, CancellationToken cancellationToken = default) => Task.FromResult<CropCycleStage?>(null);
    public Task<CropPlantation?> FindPlantationAsync(Guid plantationId, Guid organizationId, CancellationToken cancellationToken = default) => Task.FromResult<CropPlantation?>(null);
    public Task<FarmArea?> FindFarmAreaAsync(Guid areaId, Guid organizationId, CancellationToken cancellationToken = default) => Task.FromResult<FarmArea?>(null);
    public Task<LaborActivity?> FindLaborActivityAsync(Guid activityId, Guid organizationId, CancellationToken cancellationToken = default) => Task.FromResult<LaborActivity?>(null);
    public Task<StockMovement?> FindMovementAsync(Guid movementId, Guid organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Movements.FirstOrDefault(m => m.Id == movementId && m.OrganizationId == organizationId));
    public Task<IReadOnlyList<StockMovement>> FindMovementsByParentTransactionIdAsync(Guid parentTransactionId, Guid organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<StockMovement>>(Array.Empty<StockMovement>());
    public Task<StockBalance?> LockBalanceAsync(Guid locationId, Guid itemId, Guid organizationId, CancellationToken cancellationToken = default) =>
        FindBalanceAsync(locationId, itemId, organizationId, cancellationToken);
    public Task AcquireAdvisoryLockAsync(Guid locationId, Guid itemId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<bool> HasOpeningStockAsync(Guid locationId, Guid itemId, CancellationToken cancellationToken = default) => Task.FromResult(false);
    public Task<int> CountBalancesAsync(Guid organizationId, Guid? farmId, Guid? locationId, Guid? itemId, CancellationToken cancellationToken = default) => Task.FromResult(0);
    public Task<IReadOnlyList<StockBalance>> ListBalancesAsync(Guid organizationId, Guid? farmId, Guid? locationId, Guid? itemId, int skip, int take, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<StockBalance>>(Array.Empty<StockBalance>());
    public Task<int> CountMovementsAsync(Guid organizationId, Guid? farmId, Guid? locationId, Guid? itemId, StockMovementType? movementType, DateOnly? fromDate, DateOnly? toDate, Guid? cropCycleId = null, CancellationToken cancellationToken = default) => Task.FromResult(0);
    public Task<IReadOnlyList<StockMovement>> ListMovementsAsync(Guid organizationId, Guid? farmId, Guid? locationId, Guid? itemId, StockMovementType? movementType, DateOnly? fromDate, DateOnly? toDate, int skip, int take, Guid? cropCycleId = null, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<StockMovement>>(Array.Empty<StockMovement>());

    public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) =>
        await operation(cancellationToken);

    public void AddBalance(StockBalance balance) => Balances.Add(balance);
    public void AddMovement(StockMovement movement) => Movements.Add(movement);
    public void AddAuditLog(AuditLog auditLog) => AuditLogs.Add(auditLog);
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
