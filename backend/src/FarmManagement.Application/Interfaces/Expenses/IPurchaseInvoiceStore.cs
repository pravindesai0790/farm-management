using FarmManagement.Application.DTOs.Expenses;
using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Interfaces.Expenses;

public interface IPurchaseInvoiceStore
{
    Task<int> CountAsync(
        Guid organizationId,
        PurchaseInvoiceFilter filter,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PurchaseInvoice>> ListAsync(
        Guid organizationId,
        PurchaseInvoiceFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<PurchaseInvoice?> FindAsync(
        Guid invoiceId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<bool> InvoiceNumberExistsAsync(
        Guid organizationId,
        Guid supplierId,
        string supplierInvoiceNumber,
        Guid? excludeInvoiceId = null,
        CancellationToken cancellationToken = default);

    Task<bool> FarmBelongsToOrganizationAsync(
        Guid farmId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<bool> SupplierBelongsToOrganizationAndActiveAsync(
        Guid supplierId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<bool> CurrencyExistsAndActiveAsync(
        Guid currencyId,
        CancellationToken cancellationToken = default);

    Task<bool> InventoryItemBelongsToOrganizationAndActiveAsync(
        Guid inventoryItemId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<bool> StockUnitExistsAndActiveAsync(
        Guid stockUnitId,
        CancellationToken cancellationToken = default);

    Task<bool> CategoryExistsAndActiveAsync(
        Guid categoryId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<bool> AreaBelongsToFarmAsync(
        Guid farmAreaId,
        Guid farmId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<bool> PlantationBelongsToFarmAsync(
        Guid plantationId,
        Guid farmId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<bool> CycleBelongsToFarmAsync(
        Guid cropCycleId,
        Guid farmId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<bool> StageBelongsToCropCycleAsync(
        Guid cropCycleStageId,
        Guid cropCycleId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        PurchaseInvoice invoice,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        PurchaseInvoice invoice,
        CancellationToken cancellationToken = default);

    Task AddAuditLogAsync(
        AuditLog auditLog,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PurchaseInvoiceReceiptLine>> GetReceiptLinesByInvoiceAsync(
        Guid invoiceId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PurchaseInvoiceReceiptLine>> FindReceiptGroupByInvoiceAndIdempotencyKeyAsync(
        Guid invoiceId,
        Guid organizationId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task AddReceiptLinesAsync(
        IEnumerable<PurchaseInvoiceReceiptLine> receiptLines,
        CancellationToken cancellationToken = default);

    Task<bool> StorageLocationBelongsToFarmAndActiveAsync(
        Guid storageLocationId,
        Guid farmId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PurchaseInvoice>> GetInvoicesWithAllocationsAsync(
        IEnumerable<Guid> invoiceIds,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PurchaseInvoice>> GetUnpaidInvoicesForSupplierAsync(
        Guid supplierId,
        Guid organizationId,
        Guid? currencyId = null,
        CancellationToken cancellationToken = default);
}
