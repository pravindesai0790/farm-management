using FarmManagement.Application.DTOs.Expenses;
using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Interfaces.Expenses;

public interface IExpenseStore
{
    Task<int> CountAsync(
        Guid organizationId,
        ExpenseFilter filter,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Expense>> ListAsync(
        Guid organizationId,
        ExpenseFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<Expense?> FindAsync(
        Guid expenseId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<bool> FarmBelongsToOrganizationAsync(
        Guid farmId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<bool> CategoryExistsAndActiveAsync(
        Guid categoryId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<bool> CurrencyExistsAndActiveAsync(
        Guid currencyId,
        CancellationToken cancellationToken = default);

    Task<bool> SupplierBelongsToOrganizationAndActiveAsync(
        Guid supplierId,
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

    Task<bool> CropCycleBelongsToFarmAsync(
        Guid cropCycleId,
        Guid farmId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<bool> StageBelongsToCropCycleAsync(
        Guid cropCycleStageId,
        Guid cropCycleId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    void Add(Expense expense);

    void AddAuditLog(AuditLog auditLog);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
