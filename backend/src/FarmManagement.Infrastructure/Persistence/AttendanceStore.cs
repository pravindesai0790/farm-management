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
