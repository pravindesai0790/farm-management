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
