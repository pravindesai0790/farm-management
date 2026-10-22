using FarmManagement.Application.DTOs.Dashboard;
using FarmManagement.Application.Interfaces.Dashboard;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FarmManagement.Infrastructure.Persistence;

public sealed class DashboardStore(ApplicationDbContext dbContext) : IDashboardStore
{
    public async Task<IReadOnlyList<Farm>> GetFarmsWithAreasAsync(
        Guid organizationId,
        Guid? farmId,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Farms
            .Include(f => f.AreaUnit)
            .Where(f => f.OrganizationId == organizationId && f.IsActive);

        if (farmId.HasValue)
        {
            query = query.Where(f => f.Id == farmId.Value);
        }

        return await query
            .AsNoTracking()
            .OrderBy(f => f.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<int> GetActiveFarmAreasCountAsync(
        Guid organizationId,
        Guid? farmId,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.FarmAreas
            .Where(a => a.OrganizationId == organizationId && a.IsActive);

        if (farmId.HasValue)
        {
            query = query.Where(a => a.FarmId == farmId.Value);
        }

        return query.CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CropPlantation>> GetPlantationsAsync(
        Guid organizationId,
        Guid? farmId,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.CropPlantations
            .Include(p => p.Crop)
            .Include(p => p.Variety)
            .Include(p => p.AreaUnit)
            .Include(p => p.Farm)
            .Include(p => p.FarmArea)
            .Where(p => p.OrganizationId == organizationId && p.IsActive);

        if (farmId.HasValue)
        {
            query = query.Where(p => p.FarmId == farmId.Value);
        }

        return await query
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CropCycle>> GetCyclesAsync(
        Guid organizationId,
        Guid? farmId,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.CropCycles
            .Include(c => c.Plantation)
                .ThenInclude(p => p!.Crop)
            .Include(c => c.Plantation)
                .ThenInclude(p => p!.Variety)
            .Include(c => c.Plantation)
                .ThenInclude(p => p!.Farm)
            .Include(c => c.Plantation)
                .ThenInclude(p => p!.FarmArea)
            .Include(c => c.Plantation)
                .ThenInclude(p => p!.AreaUnit)
            .Include(c => c.Stages)
            .Include(c => c.LifecycleTemplate)
                .ThenInclude(t => t!.Stages)
            .Where(c => c.OrganizationId == organizationId);

        if (farmId.HasValue)
        {
            query = query.Where(c => c.Plantation != null && c.Plantation.FarmId == farmId.Value);
        }

        return await query
            .AsNoTracking()
            .AsSplitQuery()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Unit>> GetAreaUnitsAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Units
            .Where(u => u.UnitCategory == UnitCategory.Area && u.IsActive)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<LaborDashboardRawData> GetLaborDashboardDataAsync(
        Guid organizationId,
        Guid? farmId,
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        var startDate = today.AddDays(-6);

        var assignmentQuery = dbContext.WorkerFarmAssignments
            .Where(a => a.OrganizationId == organizationId &&
                        a.IsActive &&
                        a.AssignedFrom <= today &&
                        (a.AssignedTo == null || a.AssignedTo >= today));

        if (farmId.HasValue)
        {
            assignmentQuery = assignmentQuery.Where(a => a.FarmId == farmId.Value);
        }

        var assignedCountsList = await assignmentQuery
            .AsNoTracking()
            .GroupBy(a => a.FarmId)
            .Select(g => new { FarmId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var farmAssignedCounts = assignedCountsList.ToDictionary(x => x.FarmId, x => x.Count);
        var totalAssignedCount = farmAssignedCounts.Values.Sum();

        var attendanceQuery = dbContext.LaborAttendances
            .Where(a => a.OrganizationId == organizationId &&
                        a.AttendanceDate >= startDate &&
                        a.AttendanceDate <= today);

        if (farmId.HasValue)
        {
            attendanceQuery = attendanceQuery.Where(a => a.FarmId == farmId.Value);
        }

        var attendanceItems = await attendanceQuery
            .AsNoTracking()
            .Select(a => new RawLaborAttendanceItem(
                a.FarmId,
                a.AttendanceDate,
                a.AttendanceType.ToString().ToUpperInvariant(),
                a.WorkingHours,
                a.CalculatedAmount,
                a.Status.ToString().ToUpperInvariant(),
                a.FinalizedAt,
                a.Worker != null && a.Worker.LaborCategory != null ? a.Worker.LaborCategory.Name : "General Labor",
                a.Currency != null ? a.Currency.Symbol : "₹"
            ))
            .ToListAsync(cancellationToken);

        return new LaborDashboardRawData(
            AttendanceItems: attendanceItems,
            FarmAssignedCounts: farmAssignedCounts,
            TotalAssignedCount: totalAssignedCount
        );
    }

    public async Task<DashboardPrioritiesRawData> GetPrioritiesRawDataAsync(
        Guid organizationId,
        Guid? farmId,
        DateOnly today,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        // 1. Sprays
        var sprayQuery = dbContext.Sprays
            .Include(s => s.Farm)
            .Include(s => s.FarmArea)
            .Include(s => s.CropCycle)
            .Include(s => s.Target)
            .Where(s => s.OrganizationId == organizationId && s.Status != SprayStatus.Cancelled);

        if (farmId.HasValue)
        {
            sprayQuery = sprayQuery.Where(s => s.FarmId == farmId.Value);
        }

        var sprays = await sprayQuery
            .AsNoTracking()
            .Select(s => new RawPrioritySpray(
                s.Id,
                s.Target != null ? s.Target.Name : null,
                s.FarmId,
                s.Farm != null ? s.Farm.Name : "Farm",
                s.FarmArea != null ? s.FarmArea.Name : null,
                s.CropCycle != null ? s.CropCycle.CycleName : null,
                s.PlannedDate,
                s.ScheduledDateTime,
                s.ActualApplicationDateTime,
                s.Status.ToString().ToUpperInvariant(),
                s.ActualApplicationDateTime.HasValue ? (DateTimeOffset?)s.ActualApplicationDateTime.Value.AddHours(24) : null
            ))
            .ToListAsync(cancellationToken);

        // 2. Irrigations
        var irrigationQuery = dbContext.IrrigationEvents
            .Include(i => i.Farm)
            .Include(i => i.FarmArea)
            .Include(i => i.CropCycle)
            .Include(i => i.PlannedWaterUnit)
            .Where(i => i.OrganizationId == organizationId && i.Status != IrrigationStatus.Cancelled);

        if (farmId.HasValue)
        {
            irrigationQuery = irrigationQuery.Where(i => i.FarmId == farmId.Value);
        }

        var irrigations = await irrigationQuery
            .AsNoTracking()
            .Select(i => new RawPriorityIrrigation(
                i.Id,
                i.FarmId,
                i.Farm != null ? i.Farm.Name : "Farm",
                i.FarmArea != null ? i.FarmArea.Name : null,
                i.CropCycle != null ? i.CropCycle.CycleName : null,
                i.PlannedAt,
                i.ScheduledAt,
                i.ActualStartedAt,
                i.Status.ToString().ToUpperInvariant(),
                i.PlannedWaterQuantity ?? i.ActualWaterQuantity,
                i.PlannedWaterUnit != null ? i.PlannedWaterUnit.Symbol : null
            ))
            .ToListAsync(cancellationToken);

        // 3. Fertilizers
        var fertilizerQuery = dbContext.FertilizerApplications
            .Include(f => f.Farm)
            .Include(f => f.FarmArea)
            .Include(f => f.CropCycle)
            .Include(f => f.ApplicationMethod)
            .Where(f => f.OrganizationId == organizationId && f.Status != FertilizerApplicationStatus.Cancelled);

        if (farmId.HasValue)
        {
            fertilizerQuery = fertilizerQuery.Where(f => f.FarmId == farmId.Value);
        }

        var fertilizers = await fertilizerQuery
            .AsNoTracking()
            .Select(f => new RawPriorityFertilizer(
                f.Id,
                f.ReferenceNumber,
                f.FarmId ?? Guid.Empty,
                f.Farm != null ? f.Farm.Name : "Farm",
                f.FarmArea != null ? f.FarmArea.Name : null,
                f.CropCycle != null ? f.CropCycle.CycleName : null,
                f.PlannedDate,
                f.ScheduledDate,
                f.ActualApplicationDate,
                f.Status.ToString().ToUpperInvariant(),
                f.ApplicationMethod != null ? f.ApplicationMethod.Name : null
            ))
            .ToListAsync(cancellationToken);

        // 4. Labor Activities
        var laborActQuery = dbContext.LaborActivities
            .Include(l => l.Farm)
            .Include(l => l.FarmArea)
            .Include(l => l.CropCycle)
            .Include(l => l.LaborActivityType)
            .Where(l => l.OrganizationId == organizationId && l.Status != LaborActivityStatus.Cancelled);

        if (farmId.HasValue)
        {
            laborActQuery = laborActQuery.Where(l => l.FarmId == farmId.Value);
        }

        var laborActivities = await laborActQuery
            .AsNoTracking()
            .Select(l => new RawPriorityLaborActivity(
                l.Id,
                l.LaborActivityType != null ? l.LaborActivityType.Name : "Labor Task",
                l.FarmId,
                l.Farm != null ? l.Farm.Name : "Farm",
                l.FarmArea != null ? l.FarmArea.Name : null,
                l.CropCycle != null ? l.CropCycle.CycleName : null,
                l.ActivityDate,
                l.Status.ToString().ToUpperInvariant()
            ))
            .ToListAsync(cancellationToken);

        // 5. Overdue Invoices
        var invoiceQuery = dbContext.PurchaseInvoices
            .Include(p => p.Farm)
            .Include(p => p.Supplier)
            .Include(p => p.Currency)
            .Include(p => p.PaymentAllocations)
                .ThenInclude(a => a.SupplierPayment)
            .Where(p => p.OrganizationId == organizationId &&
                        p.Status == PurchaseInvoiceStatus.Posted &&
                        p.DueDate.HasValue &&
                        p.DueDate.Value < today);

        if (farmId.HasValue)
        {
            invoiceQuery = invoiceQuery.Where(p => p.FarmId == farmId.Value);
        }

        var overdueInvoicesRaw = await invoiceQuery
            .AsNoTracking()
            .Select(p => new
            {
                p.Id,
                p.SupplierInvoiceNumber,
                SupplierName = p.Supplier != null ? p.Supplier.Name : "Unknown Supplier",
                p.FarmId,
                FarmName = p.Farm != null ? p.Farm.Name : "Farm",
                DueDate = p.DueDate!.Value,
                p.TotalAmount,
                AmountPaid = p.PaymentAllocations
                    .Where(a => a.SupplierPayment != null && a.SupplierPayment.Status == SupplierPaymentStatus.Completed)
                    .Sum(a => a.AllocatedAmount),
                CurrencySymbol = p.Currency != null ? p.Currency.Symbol : "₹",
                CurrencyCode = p.Currency != null ? p.Currency.Code : "INR"
            })
            .ToListAsync(cancellationToken);

        var overdueInvoices = overdueInvoicesRaw
            .Where(x => x.TotalAmount > x.AmountPaid)
            .Select(x => new RawPriorityOverdueInvoice(
                x.Id,
                x.SupplierInvoiceNumber,
                x.SupplierName,
                x.FarmId,
                x.FarmName,
                x.DueDate,
                x.TotalAmount,
                x.AmountPaid,
                x.TotalAmount - x.AmountPaid,
                x.CurrencySymbol,
                x.CurrencyCode
            ))
            .ToList();

        // 6. Labor Attendance
        var laborData = await GetLaborDashboardDataAsync(organizationId, farmId, today, cancellationToken);

        return new DashboardPrioritiesRawData(
            Sprays: sprays,
            Irrigations: irrigations,
            Fertilizers: fertilizers,
            LaborActivities: laborActivities,
            OverdueInvoices: overdueInvoices,
            LaborRawData: laborData
        );
    }

    public async Task<IReadOnlyList<RawUpcomingActivity>> GetUpcomingActivitiesAsync(
        Guid organizationId,
        Guid? farmId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        var startDateTime = new DateTimeOffset(startDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var endDateTime = new DateTimeOffset(endDate.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);

        var results = new List<RawUpcomingActivity>();

        // 1. Sprays
        var sprayQuery = dbContext.Sprays
            .Include(s => s.Farm)
            .Include(s => s.FarmArea)
            .Include(s => s.CropCycle)
            .Include(s => s.Target)
            .Where(s => s.OrganizationId == organizationId && s.Status != SprayStatus.Cancelled);

        if (farmId.HasValue)
        {
            sprayQuery = sprayQuery.Where(s => s.FarmId == farmId.Value);
        }

        var sprays = await sprayQuery
            .AsNoTracking()
            .Where(s => (s.ScheduledDateTime.HasValue && s.ScheduledDateTime.Value >= startDateTime && s.ScheduledDateTime.Value <= endDateTime) ||
                        (s.PlannedDate.HasValue && s.PlannedDate.Value >= startDate && s.PlannedDate.Value <= endDate))
            .Select(s => new
            {
                s.Id,
                TargetName = s.Target != null ? s.Target.Name : null,
                s.FarmId,
                FarmName = s.Farm != null ? s.Farm.Name : "Farm",
                FarmAreaName = s.FarmArea != null ? s.FarmArea.Name : null,
                CropCycleName = s.CropCycle != null ? s.CropCycle.CycleName : null,
                s.ScheduledDateTime,
                s.PlannedDate,
                Status = s.Status.ToString().ToUpperInvariant()
            })
            .ToListAsync(cancellationToken);

        foreach (var s in sprays)
        {
            var date = s.ScheduledDateTime.HasValue
                ? DateOnly.FromDateTime(s.ScheduledDateTime.Value.DateTime)
                : s.PlannedDate!.Value;
            var time = s.ScheduledDateTime?.ToString("HH:mm");
            results.Add(new RawUpcomingActivity(
                s.Id,
                "SPRAY",
                string.IsNullOrWhiteSpace(s.TargetName) ? "Spray Application" : $"Spray: {s.TargetName}",
                s.FarmId,
                s.FarmName,
                s.FarmAreaName,
                s.CropCycleName,
                date,
                time,
                s.Status,
                s.TargetName
            ));
        }

        // 2. Irrigations
        var irrigationQuery = dbContext.IrrigationEvents
            .Include(i => i.Farm)
            .Include(i => i.FarmArea)
            .Include(i => i.CropCycle)
            .Include(i => i.PlannedWaterUnit)
            .Where(i => i.OrganizationId == organizationId && i.Status != IrrigationStatus.Cancelled);

        if (farmId.HasValue)
        {
            irrigationQuery = irrigationQuery.Where(i => i.FarmId == farmId.Value);
        }

        var irrigations = await irrigationQuery
            .AsNoTracking()
            .Where(i => (i.ScheduledAt.HasValue && i.ScheduledAt.Value >= startDateTime && i.ScheduledAt.Value <= endDateTime) ||
                        (i.PlannedAt.HasValue && i.PlannedAt.Value >= startDateTime && i.PlannedAt.Value <= endDateTime))
            .Select(i => new
            {
                i.Id,
                i.FarmId,
                FarmName = i.Farm != null ? i.Farm.Name : "Farm",
                FarmAreaName = i.FarmArea != null ? i.FarmArea.Name : null,
                CropCycleName = i.CropCycle != null ? i.CropCycle.CycleName : null,
                i.ScheduledAt,
                i.PlannedAt,
                Status = i.Status.ToString().ToUpperInvariant(),
                WaterQuantity = i.PlannedWaterQuantity ?? i.ActualWaterQuantity,
                WaterUnitSymbol = i.PlannedWaterUnit != null ? i.PlannedWaterUnit.Symbol : null
            })
            .ToListAsync(cancellationToken);

        foreach (var i in irrigations)
        {
            var dt = i.ScheduledAt ?? i.PlannedAt!.Value;
            var date = DateOnly.FromDateTime(dt.DateTime);
            var time = dt.ToString("HH:mm");
            var details = i.WaterQuantity.HasValue && !string.IsNullOrWhiteSpace(i.WaterUnitSymbol)
                ? $"{i.WaterQuantity.Value:N0} {i.WaterUnitSymbol}"
                : null;
            results.Add(new RawUpcomingActivity(
                i.Id,
                "IRRIGATION",
                "Irrigation Event",
                i.FarmId,
                i.FarmName,
                i.FarmAreaName,
                i.CropCycleName,
                date,
                time,
                i.Status,
                details
            ));
        }

        // 3. Fertilizers
        var fertQuery = dbContext.FertilizerApplications
            .Include(f => f.Farm)
            .Include(f => f.FarmArea)
            .Include(f => f.CropCycle)
            .Include(f => f.ApplicationMethod)
            .Where(f => f.OrganizationId == organizationId && f.Status != FertilizerApplicationStatus.Cancelled);

        if (farmId.HasValue)
        {
            fertQuery = fertQuery.Where(f => f.FarmId == farmId.Value);
        }

        var fertilizers = await fertQuery
            .AsNoTracking()
            .Where(f => (f.ScheduledDate.HasValue && f.ScheduledDate.Value >= startDateTime && f.ScheduledDate.Value <= endDateTime) ||
                        (f.PlannedDate.HasValue && f.PlannedDate.Value >= startDateTime && f.PlannedDate.Value <= endDateTime))
            .Select(f => new
            {
                f.Id,
                f.ReferenceNumber,
                FarmId = f.FarmId ?? Guid.Empty,
                FarmName = f.Farm != null ? f.Farm.Name : "Farm",
                FarmAreaName = f.FarmArea != null ? f.FarmArea.Name : null,
                CropCycleName = f.CropCycle != null ? f.CropCycle.CycleName : null,
                f.ScheduledDate,
                f.PlannedDate,
                Status = f.Status.ToString().ToUpperInvariant(),
                ApplicationMethodName = f.ApplicationMethod != null ? f.ApplicationMethod.Name : null
            })
            .ToListAsync(cancellationToken);

        foreach (var f in fertilizers)
        {
            var dt = f.ScheduledDate ?? f.PlannedDate!.Value;
            var date = DateOnly.FromDateTime(dt.DateTime);
            var time = dt.ToString("HH:mm");
            results.Add(new RawUpcomingActivity(
                f.Id,
                "FERTILIZER",
                $"Fertilizer #{f.ReferenceNumber}",
                f.FarmId,
                f.FarmName,
                f.FarmAreaName,
                f.CropCycleName,
                date,
                time,
                f.Status,
                f.ApplicationMethodName
            ));
        }

        // 4. Labor Activities
        var laborQuery = dbContext.LaborActivities
            .Include(l => l.Farm)
            .Include(l => l.FarmArea)
            .Include(l => l.CropCycle)
            .Include(l => l.LaborActivityType)
            .Where(l => l.OrganizationId == organizationId && l.Status != LaborActivityStatus.Cancelled);

        if (farmId.HasValue)
        {
            laborQuery = laborQuery.Where(l => l.FarmId == farmId.Value);
        }

        var laborActivities = await laborQuery
            .AsNoTracking()
            .Where(l => l.ActivityDate >= startDate && l.ActivityDate <= endDate)
            .Select(l => new
            {
                l.Id,
                TypeName = l.LaborActivityType != null ? l.LaborActivityType.Name : "Labor Task",
                l.FarmId,
                FarmName = l.Farm != null ? l.Farm.Name : "Farm",
                FarmAreaName = l.FarmArea != null ? l.FarmArea.Name : null,
                CropCycleName = l.CropCycle != null ? l.CropCycle.CycleName : null,
                l.ActivityDate,
                Status = l.Status.ToString().ToUpperInvariant()
            })
            .ToListAsync(cancellationToken);

        foreach (var l in laborActivities)
        {
            results.Add(new RawUpcomingActivity(
                l.Id,
                "LABOR",
                $"Labor: {l.TypeName}",
                l.FarmId,
                l.FarmName,
                l.FarmAreaName,
                l.CropCycleName,
                l.ActivityDate,
                null,
                l.Status,
                l.TypeName
            ));
        }

        return results.OrderBy(x => x.ScheduledDate).ThenBy(x => x.ScheduledTime ?? string.Empty).ToList();
    }

    public async Task<DashboardInventoryRawData> GetInventoryRawDataAsync(
        Guid organizationId,
        Guid? farmId,
        DateOnly consumptionStartDate,
        CancellationToken cancellationToken = default)
    {
        // Out of stock
        var balanceQuery = dbContext.StockBalances
            .AsNoTracking()
            .Where(b => b.OrganizationId == organizationId && b.QuantityOnHand <= 0m);

        if (farmId.HasValue)
        {
            balanceQuery = balanceQuery.Where(b => b.FarmId == farmId.Value);
        }

        var outOfStockRaw = await balanceQuery
            .Select(b => new
            {
                b.InventoryItemId,
                ItemName = b.InventoryItem != null ? b.InventoryItem.Name : "Unknown Item",
                Sku = b.InventoryItem != null ? b.InventoryItem.Sku : null,
                CategoryName = b.InventoryItem != null && b.InventoryItem.Category != null ? b.InventoryItem.Category.Name : null,
                b.StorageLocationId,
                StorageLocationName = b.StorageLocation != null ? b.StorageLocation.Name : "Storage",
                b.FarmId,
                FarmName = b.Farm != null ? b.Farm.Name : "Farm",
                StockUnitSymbol = b.InventoryItem != null && b.InventoryItem.StockUnit != null ? b.InventoryItem.StockUnit.Symbol : "units",
                b.QuantityOnHand
            })
            .Take(15)
            .ToListAsync(cancellationToken);

        var outOfStockList = outOfStockRaw.Select(b => new RawOutOfStockItem(
            b.InventoryItemId,
            b.ItemName,
            b.Sku,
            b.CategoryName,
            b.StorageLocationId,
            b.StorageLocationName,
            b.FarmId,
            b.FarmName,
            b.StockUnitSymbol,
            b.QuantityOnHand
        )).ToList();

        // Recent consumption
        var movementQuery = dbContext.StockMovements
            .AsNoTracking()
            .Where(m => m.OrganizationId == organizationId &&
                        m.MovementType == StockMovementType.Issue &&
                        m.MovementDate >= consumptionStartDate &&
                        !m.IsReversed);

        if (farmId.HasValue)
        {
            movementQuery = movementQuery.Where(m => m.FarmId == farmId.Value);
        }

        var topConsumed = await movementQuery
            .GroupBy(m => m.InventoryItemId)
            .Select(g => new
            {
                InventoryItemId = g.Key,
                TotalQuantity = g.Sum(x => x.Quantity)
            })
            .OrderByDescending(x => x.TotalQuantity)
            .Take(5)
            .ToListAsync(cancellationToken);

        var itemIds = topConsumed.Select(x => x.InventoryItemId).ToList();
        var itemsDict = itemIds.Count == 0
            ? new Dictionary<Guid, InventoryItem>()
            : await dbContext.InventoryItems
                .AsNoTracking()
                .Include(i => i.Category)
                .Include(i => i.StockUnit)
                .Where(i => itemIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id, cancellationToken);

        var consumptionGroups = topConsumed.Select(c =>
        {
            itemsDict.TryGetValue(c.InventoryItemId, out var item);
            return new RawStockConsumption(
                c.InventoryItemId,
                item?.Name ?? "Unknown Item",
                item?.Category?.Name,
                c.TotalQuantity,
                item?.StockUnit?.Symbol ?? "units"
            );
        }).ToList();

        var totalItems = await dbContext.InventoryItems
            .CountAsync(i => i.OrganizationId == organizationId && i.IsActive, cancellationToken);

        var locationQuery = dbContext.StorageLocations
            .Where(l => l.OrganizationId == organizationId && l.IsActive);
        if (farmId.HasValue)
        {
            locationQuery = locationQuery.Where(l => l.FarmId == farmId.Value);
        }
        var totalLocations = await locationQuery.CountAsync(cancellationToken);

        return new DashboardInventoryRawData(
            OutOfStockItems: outOfStockList,
            RecentConsumptions: consumptionGroups,
            TotalItemsCount: totalItems,
            TotalLocationsCount: totalLocations
        );
    }

    public async Task<DashboardFinancialRawData> GetFinancialRawDataAsync(
        Guid organizationId,
        Guid? farmId,
        DateOnly monthStartDate,
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        // 1. Direct expenses this month
        var expenseQuery = dbContext.Expenses
            .Include(e => e.Currency)
            .Where(e => e.OrganizationId == organizationId &&
                        e.Status != ExpenseStatus.Reversed &&
                        e.ExpenseDate >= monthStartDate);

        if (farmId.HasValue)
        {
            expenseQuery = expenseQuery.Where(e => e.FarmId == farmId.Value);
        }

        var directExpenses = await expenseQuery
            .AsNoTracking()
            .Select(e => new RawDirectExpenseItem(
                e.CurrencyId,
                e.Currency != null ? e.Currency.Code : "INR",
                e.Currency != null ? e.Currency.Symbol : "₹",
                e.Amount
            ))
            .ToListAsync(cancellationToken);

        // 2. Invoices (posted)
        var invoiceQuery = dbContext.PurchaseInvoices
            .Include(p => p.Currency)
            .Include(p => p.PaymentAllocations)
                .ThenInclude(a => a.SupplierPayment)
            .Where(p => p.OrganizationId == organizationId && p.Status == PurchaseInvoiceStatus.Posted);

        if (farmId.HasValue)
        {
            invoiceQuery = invoiceQuery.Where(p => p.FarmId == farmId.Value);
        }

        var invoicesRaw = await invoiceQuery
            .AsNoTracking()
            .Select(p => new
            {
                p.CurrencyId,
                CurrencyCode = p.Currency != null ? p.Currency.Code : "INR",
                CurrencySymbol = p.Currency != null ? p.Currency.Symbol : "₹",
                p.TotalAmount,
                AmountPaid = p.PaymentAllocations
                    .Where(a => a.SupplierPayment != null && a.SupplierPayment.Status == SupplierPaymentStatus.Completed)
                    .Sum(a => a.AllocatedAmount),
                p.InvoiceDate,
                p.DueDate
            })
            .ToListAsync(cancellationToken);

        var invoices = invoicesRaw.Select(p =>
        {
            var outstanding = Math.Max(0m, p.TotalAmount - p.AmountPaid);
            var isOverdue = outstanding > 0m && p.DueDate.HasValue && p.DueDate.Value < today;
            var isThisMonth = p.InvoiceDate >= monthStartDate;
            return new RawInvoiceFinancialItem(
                p.CurrencyId,
                p.CurrencyCode,
                p.CurrencySymbol,
                p.TotalAmount,
                outstanding,
                isOverdue,
                isThisMonth
            );
        }).ToList();

        return new DashboardFinancialRawData(
            DirectExpenses: directExpenses,
            Invoices: invoices
        );
    }

    public async Task<IReadOnlyList<RawRecentActivity>> GetRecentActivitiesAsync(
        Guid organizationId,
        Guid? farmId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var list = new List<RawRecentActivity>();

        // 1. Completed sprays
        var sprayQuery = dbContext.Sprays
            .Include(s => s.Farm)
            .Include(s => s.Target)
            .Where(s => s.OrganizationId == organizationId && s.Status == SprayStatus.Completed);

        if (farmId.HasValue)
        {
            sprayQuery = sprayQuery.Where(s => s.FarmId == farmId.Value);
        }

        var sprays = await sprayQuery
            .AsNoTracking()
            .OrderByDescending(s => s.ActualApplicationDateTime ?? s.UpdatedAt ?? s.CreatedAt)
            .Take(limit)
            .Select(s => new RawRecentActivity(
                s.Id,
                "SPRAY_COMPLETED",
                s.Target != null ? $"Spray: {s.Target.Name}" : "Spray Application Completed",
                $"Treated {s.ActualTreatedArea:N1} area at {s.Farm!.Name}",
                s.ActualApplicationDateTime ?? s.UpdatedAt ?? s.CreatedAt,
                s.FarmId,
                s.Farm!.Name,
                null
            ))
            .ToListAsync(cancellationToken);
        list.AddRange(sprays);

        // 2. Completed irrigations
        var irrigationQuery = dbContext.IrrigationEvents
            .Include(i => i.Farm)
            .Include(i => i.FarmArea)
            .Where(i => i.OrganizationId == organizationId && i.Status == IrrigationStatus.Completed);

        if (farmId.HasValue)
        {
            irrigationQuery = irrigationQuery.Where(i => i.FarmId == farmId.Value);
        }

        var irrigations = await irrigationQuery
            .AsNoTracking()
            .OrderByDescending(i => i.ActualEndedAt ?? i.CompletedAt ?? i.CreatedAt)
            .Take(limit)
            .Select(i => new RawRecentActivity(
                i.Id,
                "IRRIGATION_COMPLETED",
                "Irrigation Completed",
                $"Completed irrigation at {(i.Farm != null ? i.Farm.Name : "Farm")} ({(i.FarmArea != null ? i.FarmArea.Name : "Plot")})",
                i.ActualEndedAt ?? i.CompletedAt ?? i.CreatedAt,
                i.FarmId,
                i.Farm != null ? i.Farm.Name : "Farm",
                null
            ))
            .ToListAsync(cancellationToken);
        list.AddRange(irrigations);

        // 3. Completed fertilizers
        var fertQuery = dbContext.FertilizerApplications
            .Include(f => f.Farm)
            .Where(f => f.OrganizationId == organizationId && f.Status == FertilizerApplicationStatus.Completed);

        if (farmId.HasValue)
        {
            fertQuery = fertQuery.Where(f => f.FarmId == farmId.Value);
        }

        var fertilizers = await fertQuery
            .AsNoTracking()
            .OrderByDescending(f => f.ActualApplicationDate ?? f.CompletedAt ?? f.CreatedAt)
            .Take(limit)
            .Select(f => new RawRecentActivity(
                f.Id,
                "FERTILIZER_COMPLETED",
                $"Fertilizer #{f.ReferenceNumber} Applied",
                $"Completed fertilizer application at {f.Farm!.Name}",
                f.ActualApplicationDate ?? f.CompletedAt ?? f.CreatedAt,
                f.FarmId ?? Guid.Empty,
                f.Farm!.Name,
                null
            ))
            .ToListAsync(cancellationToken);
        list.AddRange(fertilizers);

        // 4. Stock receipts
        var receiptQuery = dbContext.StockMovements
            .Include(m => m.Farm)
            .Include(m => m.InventoryItem)
            .Where(m => m.OrganizationId == organizationId &&
                        m.MovementType == StockMovementType.Receipt &&
                        !m.IsReversed);

        if (farmId.HasValue)
        {
            receiptQuery = receiptQuery.Where(m => m.FarmId == farmId.Value);
        }

        var receipts = await receiptQuery
            .AsNoTracking()
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .Select(m => new RawRecentActivity(
                m.Id,
                "STOCK_RECEIVED",
                $"Stock Received: {(m.InventoryItem != null ? m.InventoryItem.Name : "Item")}",
                $"Received {m.Quantity:N1} into inventory at {(m.Farm != null ? m.Farm.Name : "Farm")}",
                m.CreatedAt,
                m.FarmId,
                m.Farm != null ? m.Farm.Name : "Farm",
                null
            ))
            .ToListAsync(cancellationToken);
        list.AddRange(receipts);

        return list.OrderByDescending(x => x.Timestamp).Take(limit).ToList();
    }
}
