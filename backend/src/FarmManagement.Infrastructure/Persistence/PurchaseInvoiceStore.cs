using FarmManagement.Application.DTOs.Expenses;
using FarmManagement.Application.Interfaces.Expenses;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FarmManagement.Infrastructure.Persistence;

public sealed class PurchaseInvoiceStore(ApplicationDbContext dbContext) : IPurchaseInvoiceStore
{
    public Task<int> CountAsync(
        Guid organizationId,
        PurchaseInvoiceFilter filter,
        CancellationToken cancellationToken = default) =>
        BuildQuery(organizationId, filter).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<PurchaseInvoice>> ListAsync(
        Guid organizationId,
        PurchaseInvoiceFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        return await BuildQuery(organizationId, filter)
            .AsNoTracking()
            .Include(pi => pi.Supplier)
            .Include(pi => pi.Farm)
            .Include(pi => pi.Currency)
            .Include(pi => pi.Lines)
                .ThenInclude(l => l.InventoryItem)
            .Include(pi => pi.Lines)
                .ThenInclude(l => l.StockUnit)
            .Include(pi => pi.Lines)
                .ThenInclude(l => l.ExpenseCategory)
            .Include(pi => pi.Lines)
                .ThenInclude(l => l.FarmArea)
            .Include(pi => pi.Lines)
                .ThenInclude(l => l.Plantation)
            .Include(pi => pi.Lines)
                .ThenInclude(l => l.CropCycle)
            .Include(pi => pi.Lines)
                .ThenInclude(l => l.CropCycleStage)
            .OrderByDescending(pi => pi.InvoiceDate)
            .ThenByDescending(pi => pi.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<PurchaseInvoice?> FindAsync(
        Guid invoiceId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.PurchaseInvoices
            .Include(pi => pi.Supplier)
            .Include(pi => pi.Farm)
            .Include(pi => pi.Currency)
            .Include(pi => pi.Lines)
                .ThenInclude(l => l.InventoryItem)
            .Include(pi => pi.Lines)
                .ThenInclude(l => l.StockUnit)
            .Include(pi => pi.Lines)
                .ThenInclude(l => l.ExpenseCategory)
            .Include(pi => pi.Lines)
                .ThenInclude(l => l.FarmArea)
            .Include(pi => pi.Lines)
                .ThenInclude(l => l.Plantation)
            .Include(pi => pi.Lines)
                .ThenInclude(l => l.CropCycle)
            .Include(pi => pi.Lines)
                .ThenInclude(l => l.CropCycleStage)
            .Include(pi => pi.ReceiptLines)
                .ThenInclude(rl => rl.StockMovement)
            .SingleOrDefaultAsync(pi => pi.Id == invoiceId && pi.OrganizationId == organizationId, cancellationToken);
    }

    public Task<bool> InvoiceNumberExistsAsync(
        Guid organizationId,
        Guid supplierId,
        string supplierInvoiceNumber,
        Guid? excludeInvoiceId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedNumber = supplierInvoiceNumber.Trim().ToUpper();
        return dbContext.PurchaseInvoices.AnyAsync(
            pi => pi.OrganizationId == organizationId &&
                  pi.SupplierId == supplierId &&
                  pi.SupplierInvoiceNumber.ToUpper() == normalizedNumber &&
                  pi.Status != PurchaseInvoiceStatus.Reversed &&
                  (!excludeInvoiceId.HasValue || pi.Id != excludeInvoiceId.Value),
            cancellationToken);
    }

    public Task<bool> FarmBelongsToOrganizationAsync(
        Guid farmId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.Farms.AnyAsync(f => f.Id == farmId && f.OrganizationId == organizationId, cancellationToken);

    public Task<bool> SupplierBelongsToOrganizationAndActiveAsync(
        Guid supplierId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.Suppliers.AnyAsync(
            s => s.Id == supplierId && s.OrganizationId == organizationId && s.IsActive,
            cancellationToken);

    public Task<bool> CurrencyExistsAndActiveAsync(
        Guid currencyId,
        CancellationToken cancellationToken = default) =>
        dbContext.Currencies.AnyAsync(c => c.Id == currencyId && c.IsActive, cancellationToken);

    public Task<bool> InventoryItemBelongsToOrganizationAndActiveAsync(
        Guid inventoryItemId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.InventoryItems.AnyAsync(
            i => i.Id == inventoryItemId && i.OrganizationId == organizationId && i.IsActive,
            cancellationToken);

    public Task<bool> StockUnitExistsAndActiveAsync(
        Guid stockUnitId,
        CancellationToken cancellationToken = default) =>
        dbContext.Units.AnyAsync(u => u.Id == stockUnitId && u.IsActive, cancellationToken);

    public Task<bool> CategoryExistsAndActiveAsync(
        Guid categoryId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.ExpenseCategories.AnyAsync(
            c => c.Id == categoryId &&
                 (c.IsSystemDefault || c.OrganizationId == organizationId) &&
                 c.IsActive,
            cancellationToken);

    public Task<bool> AreaBelongsToFarmAsync(
        Guid farmAreaId,
        Guid farmId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.FarmAreas.AnyAsync(
            a => a.Id == farmAreaId && a.FarmId == farmId && a.OrganizationId == organizationId,
            cancellationToken);

    public Task<bool> PlantationBelongsToFarmAsync(
        Guid plantationId,
        Guid farmId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.CropPlantations.AnyAsync(
            p => p.Id == plantationId && p.FarmId == farmId && p.OrganizationId == organizationId,
            cancellationToken);

    public Task<bool> CycleBelongsToFarmAsync(
        Guid cropCycleId,
        Guid farmId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.CropCycles.AnyAsync(
            c => c.Id == cropCycleId && c.Plantation != null && c.Plantation.FarmId == farmId && c.OrganizationId == organizationId,
            cancellationToken);

    public Task<bool> StageBelongsToCropCycleAsync(
        Guid cropCycleStageId,
        Guid cropCycleId,
        CancellationToken cancellationToken = default) =>
        dbContext.CropCycleStages.AnyAsync(
            s => s.Id == cropCycleStageId && s.CropCycleId == cropCycleId,
            cancellationToken);

    public async Task AddAsync(
        PurchaseInvoice invoice,
        CancellationToken cancellationToken = default)
    {
        await dbContext.PurchaseInvoices.AddAsync(invoice, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        PurchaseInvoice invoice,
        CancellationToken cancellationToken = default)
    {
        dbContext.PurchaseInvoices.Update(invoice);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddAuditLogAsync(
        AuditLog auditLog,
        CancellationToken cancellationToken = default)
    {
        await dbContext.AuditLogs.AddAsync(auditLog, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PurchaseInvoiceReceiptLine>> GetReceiptLinesByInvoiceAsync(
        Guid invoiceId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.PurchaseInvoiceReceiptLines
            .AsNoTracking()
            .Include(rl => rl.PurchaseInvoiceLine)
                .ThenInclude(l => l.InventoryItem)
            .Include(rl => rl.PurchaseInvoiceLine)
                .ThenInclude(l => l.StockUnit)
            .Include(rl => rl.StockMovement)
                .ThenInclude(sm => sm.StorageLocation)
            .Where(rl => rl.PurchaseInvoiceId == invoiceId && rl.OrganizationId == organizationId)
            .OrderBy(rl => rl.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PurchaseInvoiceReceiptLine>> FindReceiptGroupByInvoiceAndIdempotencyKeyAsync(
        Guid invoiceId,
        Guid organizationId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var normalizedKey = idempotencyKey.Trim();
        return await dbContext.PurchaseInvoiceReceiptLines
            .Include(rl => rl.PurchaseInvoiceLine)
                .ThenInclude(l => l.InventoryItem)
            .Include(rl => rl.PurchaseInvoiceLine)
                .ThenInclude(l => l.StockUnit)
            .Include(rl => rl.StockMovement)
                .ThenInclude(sm => sm.StorageLocation)
            .Where(rl => rl.PurchaseInvoiceId == invoiceId && rl.OrganizationId == organizationId && rl.IdempotencyKey == normalizedKey)
            .OrderBy(rl => rl.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddReceiptLinesAsync(
        IEnumerable<PurchaseInvoiceReceiptLine> receiptLines,
        CancellationToken cancellationToken = default)
    {
        await dbContext.PurchaseInvoiceReceiptLines.AddRangeAsync(receiptLines, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> StorageLocationBelongsToFarmAndActiveAsync(
        Guid storageLocationId,
        Guid farmId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.StorageLocations.AnyAsync(
            loc => loc.Id == storageLocationId && loc.FarmId == farmId && loc.OrganizationId == organizationId && loc.IsActive,
            cancellationToken);
    }

    private IQueryable<PurchaseInvoice> BuildQuery(Guid organizationId, PurchaseInvoiceFilter filter)
    {
        var query = dbContext.PurchaseInvoices.Where(pi => pi.OrganizationId == organizationId);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(pi =>
                pi.SupplierInvoiceNumber.ToLower().Contains(search) ||
                (pi.Notes != null && pi.Notes.ToLower().Contains(search)) ||
                (pi.Supplier != null && pi.Supplier.Name.ToLower().Contains(search)) ||
                (pi.Farm != null && pi.Farm.Name.ToLower().Contains(search)));
        }

        if (filter.SupplierId.HasValue)
        {
            query = query.Where(pi => pi.SupplierId == filter.SupplierId.Value);
        }

        if (filter.FarmId.HasValue)
        {
            query = query.Where(pi => pi.FarmId == filter.FarmId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status) && Enum.TryParse<PurchaseInvoiceStatus>(filter.Status, true, out var status))
        {
            query = query.Where(pi => pi.Status == status);
        }

        if (filter.From.HasValue)
        {
            query = query.Where(pi => pi.InvoiceDate >= filter.From.Value);
        }

        if (filter.To.HasValue)
        {
            query = query.Where(pi => pi.InvoiceDate <= filter.To.Value);
        }

        return query;
    }
}
