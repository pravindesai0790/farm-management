using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.Expenses;
using FarmManagement.Application.Interfaces.Expenses;
using FarmManagement.Application.Services;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Services;

public class SupplierPaymentServiceTests
{
    private readonly TestSupplierPaymentStore _paymentStore = new();
    private readonly TestPurchaseInvoiceStoreForPayment _invoiceStore = new();
    private readonly SupplierPaymentService _service;

    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly ExpenseActor Actor = new(UserId, OrgId);

    private static readonly Guid SupplierId = Guid.NewGuid();
    private static readonly Guid FarmId = Guid.NewGuid();
    private static readonly Guid CurrencyId = Guid.NewGuid();

    public SupplierPaymentServiceTests()
    {
        _service = new SupplierPaymentService(_paymentStore, _invoiceStore);
        _paymentStore.ValidSuppliers.Add((SupplierId, OrgId));
        _paymentStore.ValidCurrencies.Add(CurrencyId);
    }

    [Fact]
    public async Task RecordPaymentAsync_WhenValidAllocations_Succeeds()
    {
        // Arrange
        var invoice = CreatePostedInvoice(TotalAmount: 1000m);
        _invoiceStore.Invoices.Add(invoice);

        var request = new RecordSupplierPaymentRequest(
            SupplierId: SupplierId,
            PaymentDate: DateOnly.FromDateTime(DateTime.UtcNow),
            Amount: 1000m,
            CurrencyId: CurrencyId,
            PaymentMethod: PaymentMethod.BankTransfer,
            Allocations: new[] { new SupplierPaymentAllocationRequest(invoice.Id, 1000m) },
            ReferenceNumber: "TXN12345");

        // Act
        var response = await _service.RecordPaymentAsync(Actor, request);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(1000m, response.Amount);
        Assert.Equal("Completed", response.Status);
        Assert.Single(response.Allocations);
        Assert.Equal(1000m, response.Allocations[0].AllocatedAmount);
    }

    [Fact]
    public async Task RecordPaymentAsync_WhenAllocationsSumDoesNotEqualAmount_ThrowsValidationException()
    {
        // Arrange
        var invoice = CreatePostedInvoice(TotalAmount: 1000m);
        _invoiceStore.Invoices.Add(invoice);

        var request = new RecordSupplierPaymentRequest(
            SupplierId: SupplierId,
            PaymentDate: DateOnly.FromDateTime(DateTime.UtcNow),
            Amount: 1000m,
            CurrencyId: CurrencyId,
            PaymentMethod: PaymentMethod.BankTransfer,
            Allocations: new[] { new SupplierPaymentAllocationRequest(invoice.Id, 500m) });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.RecordPaymentAsync(Actor, request));
        Assert.Contains("must equal", ex.Message);
    }

    [Fact]
    public async Task RecordPaymentAsync_WhenAllocatedAmountExceedsInvoiceOutstandingBalance_ThrowsValidationException()
    {
        // Arrange
        var invoice = CreatePostedInvoice(TotalAmount: 500m);
        _invoiceStore.Invoices.Add(invoice);

        var request = new RecordSupplierPaymentRequest(
            SupplierId: SupplierId,
            PaymentDate: DateOnly.FromDateTime(DateTime.UtcNow),
            Amount: 1000m,
            CurrencyId: CurrencyId,
            PaymentMethod: PaymentMethod.BankTransfer,
            Allocations: new[] { new SupplierPaymentAllocationRequest(invoice.Id, 1000m) });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.RecordPaymentAsync(Actor, request));
        Assert.Contains("exceeds outstanding balance", ex.Message);
    }

    [Fact]
    public async Task RecordPaymentAsync_WhenCurrencyMismatch_ThrowsValidationException()
    {
        // Arrange
        var differentCurrencyId = Guid.NewGuid();
        var invoice = CreatePostedInvoice(TotalAmount: 1000m);
        _invoiceStore.Invoices.Add(invoice);
        _paymentStore.ValidCurrencies.Add(differentCurrencyId);

        var request = new RecordSupplierPaymentRequest(
            SupplierId: SupplierId,
            PaymentDate: DateOnly.FromDateTime(DateTime.UtcNow),
            Amount: 1000m,
            CurrencyId: differentCurrencyId,
            PaymentMethod: PaymentMethod.BankTransfer,
            Allocations: new[] { new SupplierPaymentAllocationRequest(invoice.Id, 1000m) });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.RecordPaymentAsync(Actor, request));
        Assert.Contains("does not match payment currency", ex.Message);
    }

    [Fact]
    public async Task RecordPaymentAsync_WhenInvoiceNotPosted_ThrowsValidationException()
    {
        // Arrange
        var invoice = PurchaseInvoice.CreateDraft(
            OrgId, SupplierId, FarmId, "INV-DRAFT", DateOnly.FromDateTime(DateTime.UtcNow), CurrencyId, UserId);
        _invoiceStore.Invoices.Add(invoice);

        var request = new RecordSupplierPaymentRequest(
            SupplierId: SupplierId,
            PaymentDate: DateOnly.FromDateTime(DateTime.UtcNow),
            Amount: 500m,
            CurrencyId: CurrencyId,
            PaymentMethod: PaymentMethod.Cash,
            Allocations: new[] { new SupplierPaymentAllocationRequest(invoice.Id, 500m) });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.RecordPaymentAsync(Actor, request));
        Assert.Contains("Draft", ex.Message);
    }

    [Fact]
    public async Task RecordPaymentAsync_WhenFutureDate_ThrowsValidationException()
    {
        // Arrange
        var invoice = CreatePostedInvoice(TotalAmount: 1000m);
        _invoiceStore.Invoices.Add(invoice);

        var request = new RecordSupplierPaymentRequest(
            SupplierId: SupplierId,
            PaymentDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
            Amount: 1000m,
            CurrencyId: CurrencyId,
            PaymentMethod: PaymentMethod.Cheque,
            Allocations: new[] { new SupplierPaymentAllocationRequest(invoice.Id, 1000m) });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.RecordPaymentAsync(Actor, request));
        Assert.Contains("cannot be in the future", ex.Message);
    }

    [Fact]
    public async Task ReversePaymentAsync_WhenCompleted_Succeeds()
    {
        // Arrange
        var invoice = CreatePostedInvoice(TotalAmount: 1000m);
        _invoiceStore.Invoices.Add(invoice);

        var recordReq = new RecordSupplierPaymentRequest(
            SupplierId: SupplierId,
            PaymentDate: DateOnly.FromDateTime(DateTime.UtcNow),
            Amount: 1000m,
            CurrencyId: CurrencyId,
            PaymentMethod: PaymentMethod.BankTransfer,
            Allocations: new[] { new SupplierPaymentAllocationRequest(invoice.Id, 1000m) });

        var created = await _service.RecordPaymentAsync(Actor, recordReq);

        // Act
        var response = await _service.ReversePaymentAsync(Actor, created.Id, new ReverseSupplierPaymentRequest("Duplicate payment entered in error"));

        // Assert
        Assert.Equal("Reversed", response.Status);
        Assert.NotNull(response.ReversedAt);
        Assert.Equal("Duplicate payment entered in error", response.ReversalReason);
    }

    private static PurchaseInvoice CreatePostedInvoice(decimal TotalAmount)
    {
        var inv = PurchaseInvoice.CreateDraft(
            OrgId, SupplierId, FarmId, $"INV-{Guid.NewGuid():N}", DateOnly.FromDateTime(DateTime.UtcNow), CurrencyId, UserId);
        var line = PurchaseInvoiceLine.CreateNonInventoryLine(
            OrgId, inv.Id, Guid.NewGuid(), TotalAmount, "Test Item");
        inv.Lines.Add(line);
        inv.RecalculateTotals();
        inv.Post(UserId);
        return inv;
    }
}

public sealed class TestSupplierPaymentStore : ISupplierPaymentStore
{
    public List<SupplierPayment> Payments { get; } = new();
    public List<AuditLog> AuditLogs { get; } = new();
    public HashSet<(Guid SupplierId, Guid OrgId)> ValidSuppliers { get; } = new();
    public HashSet<Guid> ValidCurrencies { get; } = new();

    public Task<int> CountAsync(Guid organizationId, SupplierPaymentFilter filter, CancellationToken cancellationToken = default) =>
        Task.FromResult(Payments.Count(p => p.OrganizationId == organizationId));

    public Task<IReadOnlyList<SupplierPayment>> ListAsync(Guid organizationId, SupplierPaymentFilter filter, int skip, int take, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SupplierPayment>>(Payments.Where(p => p.OrganizationId == organizationId).Skip(skip).Take(take).ToList());

    public Task<SupplierPayment?> FindByIdWithDetailsAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Payments.FirstOrDefault(p => p.Id == id && p.OrganizationId == organizationId));

    public Task<SupplierPayment?> FindByIdempotencyKeyAsync(Guid organizationId, string idempotencyKey, CancellationToken cancellationToken = default) =>
        Task.FromResult(Payments.FirstOrDefault(p => p.OrganizationId == organizationId && p.IdempotencyKey == idempotencyKey));

    public Task<bool> SupplierBelongsToOrganizationAndActiveAsync(Guid supplierId, Guid organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(ValidSuppliers.Contains((supplierId, organizationId)));

    public Task<bool> CurrencyExistsAndActiveAsync(Guid currencyId, CancellationToken cancellationToken = default) =>
        Task.FromResult(ValidCurrencies.Contains(currencyId));

    public Task AddPaymentWithAllocationsAsync(SupplierPayment payment, IEnumerable<SupplierPaymentAllocation> allocations, CancellationToken cancellationToken = default)
    {
        Payments.Add(payment);
        return Task.CompletedTask;
    }

    public Task UpdatePaymentAsync(SupplierPayment payment, CancellationToken cancellationToken = default)
    {
        var idx = Payments.FindIndex(p => p.Id == payment.Id);
        if (idx >= 0) Payments[idx] = payment;
        else Payments.Add(payment);
        return Task.CompletedTask;
    }

    public Task AddAuditLogAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        AuditLogs.Add(auditLog);
        return Task.CompletedTask;
    }

    public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default) =>
        action(cancellationToken);
}

public sealed class TestPurchaseInvoiceStoreForPayment : IPurchaseInvoiceStore
{
    public List<PurchaseInvoice> Invoices { get; } = new();

    public Task AddAsync(PurchaseInvoice invoice, CancellationToken cancellationToken = default) { Invoices.Add(invoice); return Task.CompletedTask; }
    public Task AddAuditLogAsync(AuditLog auditLog, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task AddReceiptLinesAsync(IEnumerable<PurchaseInvoiceReceiptLine> receiptLines, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<bool> AreaBelongsToFarmAsync(Guid farmAreaId, Guid farmId, Guid organizationId, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<bool> CategoryExistsAndActiveAsync(Guid categoryId, Guid organizationId, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<int> CountAsync(Guid organizationId, PurchaseInvoiceFilter filter, CancellationToken cancellationToken = default) => Task.FromResult(Invoices.Count);
    public Task<bool> CurrencyExistsAndActiveAsync(Guid currencyId, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<bool> CycleBelongsToFarmAsync(Guid cropCycleId, Guid farmId, Guid organizationId, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<bool> FarmBelongsToOrganizationAsync(Guid farmId, Guid organizationId, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<PurchaseInvoice?> FindAsync(Guid invoiceId, Guid organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Invoices.FirstOrDefault(i => i.Id == invoiceId && i.OrganizationId == organizationId));
    public Task<IReadOnlyList<PurchaseInvoiceReceiptLine>> FindReceiptGroupByInvoiceAndIdempotencyKeyAsync(Guid invoiceId, Guid organizationId, string idempotencyKey, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PurchaseInvoiceReceiptLine>>(Array.Empty<PurchaseInvoiceReceiptLine>());
    public Task<IReadOnlyList<PurchaseInvoice>> GetInvoicesWithAllocationsAsync(IEnumerable<Guid> invoiceIds, Guid organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PurchaseInvoice>>(Invoices.Where(i => i.OrganizationId == organizationId && invoiceIds.Contains(i.Id)).ToList());
    public Task<IReadOnlyList<PurchaseInvoiceReceiptLine>> GetReceiptLinesByInvoiceAsync(Guid invoiceId, Guid organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PurchaseInvoiceReceiptLine>>(Array.Empty<PurchaseInvoiceReceiptLine>());
    public Task<IReadOnlyList<PurchaseInvoice>> GetUnpaidInvoicesForSupplierAsync(Guid supplierId, Guid organizationId, Guid? currencyId = null, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PurchaseInvoice>>(Invoices.Where(i => i.OrganizationId == organizationId && i.SupplierId == supplierId).ToList());
    public Task<bool> InventoryItemBelongsToOrganizationAndActiveAsync(Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<bool> InvoiceNumberExistsAsync(Guid organizationId, Guid supplierId, string supplierInvoiceNumber, Guid? excludeInvoiceId = null, CancellationToken cancellationToken = default) => Task.FromResult(false);
    public Task<IReadOnlyList<PurchaseInvoice>> ListAsync(Guid organizationId, PurchaseInvoiceFilter filter, int skip, int take, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PurchaseInvoice>>(Invoices.Skip(skip).Take(take).ToList());
    public Task<bool> PlantationBelongsToFarmAsync(Guid plantationId, Guid farmId, Guid organizationId, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<bool> StageBelongsToCropCycleAsync(Guid cropCycleStageId, Guid cropCycleId, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<bool> StockUnitExistsAndActiveAsync(Guid stockUnitId, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<bool> StorageLocationBelongsToFarmAndActiveAsync(Guid storageLocationId, Guid farmId, Guid organizationId, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<bool> SupplierBelongsToOrganizationAndActiveAsync(Guid supplierId, Guid organizationId, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task UpdateAsync(PurchaseInvoice invoice, CancellationToken cancellationToken = default) { return Task.CompletedTask; }
}
