using FarmManagement.Application.Common.Models;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.Interfaces.Labor;

public interface ILaborWageRateStore
{
    Task<PagedResponse<LaborWageRate>> ListAsync(
        Guid organizationId,
        int page,
        int pageSize,
        Gender? gender,
        WageType? wageType,
        bool? isActive,
        DateOnly? businessDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LaborWageRate>> ListAllAsync(
        Guid organizationId,
        Gender? gender,
        WageType? wageType,
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<LaborWageRate?> FindAsync(
        Guid id,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LaborWageRate>> GetActiveRatesForOrganizationAsync(
        Guid organizationId,
        Gender gender,
        WageType wageType,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);

    Task<LaborWageRate?> FindApplicableRateAsync(
        Guid organizationId,
        Gender gender,
        WageType wageType,
        DateOnly businessDate,
        CancellationToken cancellationToken = default);

    Task<Currency?> FindCurrencyAsync(
        Guid currencyId,
        CancellationToken cancellationToken = default);

    void Add(LaborWageRate wageRate);

    void AddAuditLog(AuditLog auditLog);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
