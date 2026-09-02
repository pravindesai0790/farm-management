using FarmManagement.Application.Interfaces.Farms;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FarmManagement.Infrastructure.Persistence;

public sealed class FarmStore(ApplicationDbContext dbContext) : IFarmStore
{
    public Task<int> CountAsync(Guid organizationId, string? search, bool? isActive, CancellationToken cancellationToken = default) =>
        BuildQuery(organizationId, search, isActive).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<Farm>> ListAsync(
        Guid organizationId,
        int skip,
        int take,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken = default) =>
        await BuildQuery(organizationId, search, isActive)
            .AsNoTracking()
            .OrderBy(farm => farm.Name)
            .ThenBy(farm => farm.Code)
            .ThenBy(farm => farm.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task<Farm?> FindAsync(Guid farmId, Guid organizationId, CancellationToken cancellationToken = default) =>
        BuildQuery(organizationId, null, null).SingleOrDefaultAsync(farm => farm.Id == farmId, cancellationToken);

    public Task<bool> CodeExistsAsync(
        Guid organizationId,
        string code,
        Guid? excludingFarmId = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Farms.Where(farm => farm.OrganizationId == organizationId && farm.Code == code);
        if (excludingFarmId is not null)
        {
            query = query.Where(farm => farm.Id != excludingFarmId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task<FarmOwnershipType?> FindOwnershipTypeAsync(Guid ownershipTypeId, CancellationToken cancellationToken = default) =>
        dbContext.FarmOwnershipTypes.SingleOrDefaultAsync(item => item.Id == ownershipTypeId, cancellationToken);

    public Task<Unit?> FindAreaUnitAsync(Guid unitId, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.Units.SingleOrDefaultAsync(
            unit => unit.Id == unitId &&
                    unit.IsActive &&
                    unit.UnitCategory == UnitCategory.Area &&
                    (unit.OrganizationId == null || unit.OrganizationId == organizationId),
            cancellationToken);

    public void Add(Farm farm) => dbContext.Farms.Add(farm);

    public void AddAuditLog(AuditLog auditLog) => dbContext.AuditLogs.Add(auditLog);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => dbContext.SaveChangesAsync(cancellationToken);

    private IQueryable<Farm> BuildQuery(Guid organizationId, string? search, bool? isActive)
    {
        var query = dbContext.Farms
            .Include(farm => farm.OwnershipType)
            .Include(farm => farm.AreaUnit)
            .Where(farm => farm.OrganizationId == organizationId);

        if (isActive is not null)
        {
            query = query.Where(farm => farm.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLowerInvariant();
            query = query.Where(farm =>
                farm.Code.ToLower().Contains(normalizedSearch) ||
                farm.Name.ToLower().Contains(normalizedSearch));
        }

        return query;
    }
}
