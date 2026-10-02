using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Interfaces.Expenses;

public interface IExpenseCategoryStore
{
    Task<int> CountAsync(
        Guid organizationId,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExpenseCategory>> ListAsync(
        Guid organizationId,
        int skip,
        int take,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<ExpenseCategory?> FindAsync(
        Guid categoryId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(
        Guid organizationId,
        string name,
        Guid? excludingCategoryId = null,
        CancellationToken cancellationToken = default);

    Task<bool> HasHistoricalReferencesAsync(
        Guid categoryId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    void Add(ExpenseCategory category);

    void AddAuditLog(AuditLog auditLog);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
