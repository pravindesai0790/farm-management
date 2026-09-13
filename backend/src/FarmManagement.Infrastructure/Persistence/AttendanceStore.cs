using FarmManagement.Application.DTOs.Labor;
using FarmManagement.Application.Interfaces.Labor;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FarmManagement.Infrastructure.Persistence;

public sealed class AttendanceStore(ApplicationDbContext dbContext) : IAttendanceStore
{
    public Task<Farm?> FindFarmAsync(
        Guid farmId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.Farms.SingleOrDefaultAsync(
            farm => farm.Id == farmId && farm.OrganizationId == organizationId,
            cancellationToken);

    public Task<int> CountEligibleWorkersAsync(
        Guid organizationId,
        Guid farmId,
        DateOnly attendanceDate,
        string? search,
        CancellationToken cancellationToken = default) =>
        BuildEligibleWorkersQuery(organizationId, farmId, attendanceDate, search)
            .CountAsync(cancellationToken);

    public async Task<IReadOnlyList<AttendanceEligibleWorkerResponse>> ListEligibleWorkersAsync(
        Guid organizationId,
        Guid farmId,
        DateOnly attendanceDate,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var workers = await BuildEligibleWorkersQuery(organizationId, farmId, attendanceDate, search)
            .Include(w => w.LaborCategory)
            .Include(w => w.Contractor)
            .Include(w => w.FarmAssignments)
            .OrderBy(w => w.DisplayName)
            .ThenBy(w => w.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return workers.Select(worker =>
        {
            var assignment = worker.FarmAssignments
                .Where(a =>
                    a.OrganizationId == organizationId &&
                    a.FarmId == farmId &&
                    a.IsActive &&
                    a.AssignedFrom <= attendanceDate &&
                    (!a.AssignedTo.HasValue || a.AssignedTo.Value >= attendanceDate))
                .OrderByDescending(a => a.AssignedFrom)
                .First();

            return new AttendanceEligibleWorkerResponse(
                WorkerId: worker.Id,
                DisplayName: worker.DisplayName,
                FirstName: worker.FirstName,
                LastName: worker.LastName,
                Gender: worker.Gender.ToString().ToUpperInvariant(),
                EmploymentType: ConvertEmploymentTypeToString(worker.EmploymentType),
                MobileNumber: worker.MobileNumber,
                LaborCategoryId: worker.LaborCategoryId,
                LaborCategoryName: worker.LaborCategory?.Name,
                ContractorId: worker.ContractorId,
                ContractorName: worker.Contractor?.Name,
                JoiningDate: worker.JoiningDate,
                LeavingDate: worker.LeavingDate,
                AssignmentId: assignment.Id,
                FarmId: assignment.FarmId,
                AssignedFrom: assignment.AssignedFrom,
                AssignedTo: assignment.AssignedTo);
        }).ToArray();
    }

    public Task<LaborAttendance?> FindAttendanceByIdAsync(
        Guid id,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.LaborAttendances
            .Include(a => a.Farm)
            .Include(a => a.Worker)
                .ThenInclude(w => w!.LaborCategory)
            .Include(a => a.Currency)
            .SingleOrDefaultAsync(
                a => a.Id == id && a.OrganizationId == organizationId,
                cancellationToken);

    public Task<LaborAttendance?> FindAttendanceByWorkerAndDateAsync(
        Guid organizationId,
        Guid workerId,
        DateOnly attendanceDate,
        CancellationToken cancellationToken = default) =>
        dbContext.LaborAttendances
            .Include(a => a.Farm)
            .Include(a => a.Worker)
                .ThenInclude(w => w!.LaborCategory)
            .Include(a => a.Currency)
            .SingleOrDefaultAsync(
                a => a.OrganizationId == organizationId &&
                     a.WorkerId == workerId &&
                     a.AttendanceDate == attendanceDate,
                cancellationToken);

    public async Task<IReadOnlyList<LaborAttendance>> ListDailyAttendanceAsync(
        Guid organizationId,
        Guid farmId,
        DateOnly attendanceDate,
        CancellationToken cancellationToken = default) =>
        await dbContext.LaborAttendances
            .AsNoTracking()
            .Include(a => a.Farm)
            .Include(a => a.Worker)
                .ThenInclude(w => w!.LaborCategory)
            .Include(a => a.Currency)
            .Where(a =>
                a.OrganizationId == organizationId &&
                a.FarmId == farmId &&
                a.AttendanceDate == attendanceDate)
            .OrderBy(a => a.Worker != null ? a.Worker.DisplayName : string.Empty)
            .ThenBy(a => a.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<LaborAttendance>> ListDailyAttendanceTrackedAsync(
        Guid organizationId,
        Guid farmId,
        DateOnly attendanceDate,
        CancellationToken cancellationToken = default) =>
        await dbContext.LaborAttendances
            .Include(a => a.Farm)
            .Include(a => a.Worker)
                .ThenInclude(w => w!.LaborCategory)
            .Include(a => a.Currency)
            .Where(a =>
                a.OrganizationId == organizationId &&
                a.FarmId == farmId &&
                a.AttendanceDate == attendanceDate)
            .OrderBy(a => a.Worker != null ? a.Worker.DisplayName : string.Empty)
            .ThenBy(a => a.Id)
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<LaborAttendance> Items, int TotalCount)> ListAttendanceHistoryAsync(
        Guid organizationId,
        Guid? farmId,
        Guid? workerId,
        DateOnly? fromDate,
        DateOnly? toDate,
        AttendanceType? attendanceType,
        AttendanceStatus? status,
        string? search,
        string? sortBy,
        bool sortDescending,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.LaborAttendances
            .AsNoTracking()
            .Where(a => a.OrganizationId == organizationId);

        if (farmId.HasValue && farmId.Value != Guid.Empty)
        {
            query = query.Where(a => a.FarmId == farmId.Value);
        }

        if (workerId.HasValue && workerId.Value != Guid.Empty)
        {
            query = query.Where(a => a.WorkerId == workerId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(a => a.AttendanceDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(a => a.AttendanceDate <= toDate.Value);
        }

        if (attendanceType.HasValue)
        {
            query = query.Where(a => a.AttendanceType == attendanceType.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToLower();
            query = query.Where(a =>
                (a.Worker != null && (
                    a.Worker.DisplayName.ToLower().Contains(normalized) ||
                    a.Worker.FirstName.ToLower().Contains(normalized) ||
                    (a.Worker.LastName != null && a.Worker.LastName.ToLower().Contains(normalized)) ||
                    (a.Worker.MobileNumber != null && a.Worker.MobileNumber.ToLower().Contains(normalized))
                )) ||
                (a.Farm != null && a.Farm.Name.ToLower().Contains(normalized)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = ApplyHistorySorting(query, sortBy, sortDescending);

        var items = await query
            .Include(a => a.Farm)
            .Include(a => a.Worker)
                .ThenInclude(w => w!.LaborCategory)
            .Include(a => a.Currency)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    private static IQueryable<LaborAttendance> ApplyHistorySorting(
        IQueryable<LaborAttendance> query,
        string? sortBy,
        bool sortDescending)
    {
        var normalizedSort = sortBy?.Trim().ToLowerInvariant();

        return (normalizedSort, sortDescending) switch
        {
            ("farm" or "farmname", false) => query.OrderBy(a => a.Farm != null ? a.Farm.Name : string.Empty).ThenByDescending(a => a.AttendanceDate),
            ("farm" or "farmname", true) => query.OrderByDescending(a => a.Farm != null ? a.Farm.Name : string.Empty).ThenByDescending(a => a.AttendanceDate),
            ("worker" or "workername" or "workerdisplayname", false) => query.OrderBy(a => a.Worker != null ? a.Worker.DisplayName : string.Empty).ThenByDescending(a => a.AttendanceDate),
            ("worker" or "workername" or "workerdisplayname", true) => query.OrderByDescending(a => a.Worker != null ? a.Worker.DisplayName : string.Empty).ThenByDescending(a => a.AttendanceDate),
            ("gender", false) => query.OrderBy(a => a.Worker != null ? a.Worker.Gender : Gender.Other).ThenByDescending(a => a.AttendanceDate),
            ("gender", true) => query.OrderByDescending(a => a.Worker != null ? a.Worker.Gender : Gender.Other).ThenByDescending(a => a.AttendanceDate),
            ("attendancetype" or "type", false) => query.OrderBy(a => a.AttendanceType).ThenByDescending(a => a.AttendanceDate),
            ("attendancetype" or "type", true) => query.OrderByDescending(a => a.AttendanceType).ThenByDescending(a => a.AttendanceDate),
            ("hours" or "workinghours", false) => query.OrderBy(a => a.WorkingHours ?? 0).ThenByDescending(a => a.AttendanceDate),
            ("hours" or "workinghours", true) => query.OrderByDescending(a => a.WorkingHours ?? 0).ThenByDescending(a => a.AttendanceDate),
            ("earnings" or "amount" or "calculatedamount", false) => query.OrderBy(a => a.CalculatedAmount ?? 0).ThenByDescending(a => a.AttendanceDate),
            ("earnings" or "amount" or "calculatedamount", true) => query.OrderByDescending(a => a.CalculatedAmount ?? 0).ThenByDescending(a => a.AttendanceDate),
            ("status", false) => query.OrderBy(a => a.Status).ThenByDescending(a => a.AttendanceDate),
            ("status", true) => query.OrderByDescending(a => a.Status).ThenByDescending(a => a.AttendanceDate),
            ("date" or "attendancedate", false) => query.OrderBy(a => a.AttendanceDate).ThenByDescending(a => a.CreatedAt),
            ("date" or "attendancedate", true) => query.OrderByDescending(a => a.AttendanceDate).ThenByDescending(a => a.CreatedAt),
            (_, true) => query.OrderByDescending(a => a.AttendanceDate).ThenByDescending(a => a.CreatedAt),
            _ => query.OrderByDescending(a => a.AttendanceDate).ThenByDescending(a => a.CreatedAt)
        };
    }

    public Task<Worker?> FindWorkerWithAssignmentAsync(
        Guid organizationId,
        Guid workerId,
        Guid farmId,
        DateOnly attendanceDate,
        CancellationToken cancellationToken = default) =>
        dbContext.Workers
            .Include(w => w.LaborCategory)
            .Include(w => w.FarmAssignments)
            .SingleOrDefaultAsync(
                w => w.Id == workerId &&
                     w.OrganizationId == organizationId &&
                     w.IsActive &&
                     (!w.JoiningDate.HasValue || w.JoiningDate.Value <= attendanceDate) &&
                     (!w.LeavingDate.HasValue || w.LeavingDate.Value >= attendanceDate) &&
                     w.FarmAssignments.Any(a =>
                         a.OrganizationId == organizationId &&
                         a.FarmId == farmId &&
                         a.IsActive &&
                         a.AssignedFrom <= attendanceDate &&
                         (!a.AssignedTo.HasValue || a.AssignedTo.Value >= attendanceDate)),
                cancellationToken);

    public Task<Worker?> FindWorkerWithDetailsAsync(
        Guid organizationId,
        Guid workerId,
        CancellationToken cancellationToken = default) =>
        dbContext.Workers
            .Include(w => w.LaborCategory)
            .Include(w => w.Contractor)
            .Include(w => w.FarmAssignments)
            .SingleOrDefaultAsync(
                w => w.Id == workerId && w.OrganizationId == organizationId,
                cancellationToken);

    public async Task<DateOnly?> FindPreviousAttendanceDateAsync(
        Guid organizationId,
        Guid farmId,
        DateOnly targetDate,
        CancellationToken cancellationToken = default) =>
        await dbContext.LaborAttendances
            .Where(a =>
                a.OrganizationId == organizationId &&
                a.FarmId == farmId &&
                a.AttendanceDate < targetDate)
            .Select(a => (DateOnly?)a.AttendanceDate)
            .OrderByDescending(d => d)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<WorkerEarningsLedger?> FindEarningsLedgerByAttendanceIdAsync(
        Guid organizationId,
        Guid attendanceId,
        CancellationToken cancellationToken = default) =>
        dbContext.WorkerEarningsLedgers
            .Include(l => l.Currency)
            .Include(l => l.Worker)
            .SingleOrDefaultAsync(
                l => l.OrganizationId == organizationId &&
                     l.AttendanceId == attendanceId &&
                     l.EntryType == EarningsEntryType.Earning,
                cancellationToken);

    public void AddAttendance(LaborAttendance attendance) =>
        dbContext.LaborAttendances.Add(attendance);

    public void RemoveAttendance(LaborAttendance attendance) =>
        dbContext.LaborAttendances.Remove(attendance);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.ReadCommitted,
            cancellationToken);
        try
        {
            var result = await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private IQueryable<Worker> BuildEligibleWorkersQuery(
        Guid organizationId,
        Guid farmId,
        DateOnly attendanceDate,
        string? search)
    {
        var query = dbContext.Workers
            .AsNoTracking()
            .Where(worker =>
                worker.OrganizationId == organizationId &&
                worker.IsActive &&
                (!worker.JoiningDate.HasValue || worker.JoiningDate.Value <= attendanceDate) &&
                (!worker.LeavingDate.HasValue || worker.LeavingDate.Value >= attendanceDate) &&
                worker.FarmAssignments.Any(assignment =>
                    assignment.OrganizationId == organizationId &&
                    assignment.FarmId == farmId &&
                    assignment.IsActive &&
                    assignment.AssignedFrom <= attendanceDate &&
                    (!assignment.AssignedTo.HasValue || assignment.AssignedTo.Value >= attendanceDate)));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLower();
            query = query.Where(worker =>
                worker.DisplayName.ToLower().Contains(normalizedSearch) ||
                worker.FirstName.ToLower().Contains(normalizedSearch) ||
                (worker.LastName != null && worker.LastName.ToLower().Contains(normalizedSearch)) ||
                (worker.MobileNumber != null && worker.MobileNumber.ToLower().Contains(normalizedSearch)));
        }

        return query;
    }

    private static string ConvertEmploymentTypeToString(EmploymentType type) => type switch
    {
        EmploymentType.Permanent => "PERMANENT",
        EmploymentType.Seasonal => "SEASONAL",
        EmploymentType.DailyWage => "DAILY_WAGE",
        EmploymentType.Contract => "CONTRACT",
        _ => type.ToString().ToUpperInvariant()
    };
}
