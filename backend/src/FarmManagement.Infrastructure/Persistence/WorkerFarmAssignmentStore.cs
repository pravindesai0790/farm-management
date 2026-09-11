using FarmManagement.Application.Interfaces.Labor;
using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FarmManagement.Infrastructure.Persistence;

public sealed class WorkerFarmAssignmentStore(ApplicationDbContext dbContext) : IWorkerFarmAssignmentStore
{
    public async Task<IReadOnlyList<WorkerFarmAssignment>> ListByWorkerAsync(
        Guid organizationId,
        Guid workerId,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.WorkerFarmAssignments
            .Include(assignment => assignment.Worker)
            .Include(assignment => assignment.Farm)
            .Where(assignment =>
                assignment.OrganizationId == organizationId &&
                assignment.WorkerId == workerId);

        if (isActive.HasValue)
        {
            query = query.Where(assignment => assignment.IsActive == isActive.Value);
        }

        return await query
            .AsNoTracking()
            .OrderByDescending(assignment => assignment.IsActive)
            .ThenByDescending(assignment => assignment.AssignedFrom)
            .ThenBy(assignment => assignment.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkerFarmAssignment>> ListByFarmAsync(
        Guid organizationId,
        Guid farmId,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.WorkerFarmAssignments
            .Include(assignment => assignment.Worker)
            .Include(assignment => assignment.Farm)
            .Where(assignment =>
                assignment.OrganizationId == organizationId &&
                assignment.FarmId == farmId);

        if (isActive.HasValue)
        {
            query = query.Where(assignment => assignment.IsActive == isActive.Value);
        }

        return await query
            .AsNoTracking()
            .OrderByDescending(assignment => assignment.IsActive)
            .ThenByDescending(assignment => assignment.AssignedFrom)
            .ThenBy(assignment => assignment.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<WorkerFarmAssignment?> FindAsync(
        Guid assignmentId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.WorkerFarmAssignments
            .Include(assignment => assignment.Worker)
            .Include(assignment => assignment.Farm)
            .SingleOrDefaultAsync(
                assignment => assignment.Id == assignmentId && assignment.OrganizationId == organizationId,
                cancellationToken);

    public Task<Worker?> FindWorkerAsync(
        Guid workerId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.Workers.SingleOrDefaultAsync(
            worker => worker.Id == workerId && worker.OrganizationId == organizationId,
            cancellationToken);

    public Task<Farm?> FindFarmAsync(
        Guid farmId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.Farms.SingleOrDefaultAsync(
            farm => farm.Id == farmId && farm.OrganizationId == organizationId,
            cancellationToken);

    public void Add(WorkerFarmAssignment assignment) =>
        dbContext.WorkerFarmAssignments.Add(assignment);

    public void AddAuditLog(AuditLog auditLog) =>
        dbContext.AuditLogs.Add(auditLog);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
