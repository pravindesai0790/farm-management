using FarmManagement.Application.DTOs.Expenses;
using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Interfaces.Expenses;

public interface ISupplierPaymentStore
{
    Task<IReadOnlyList<SupplierPayment>> ListAsync(
        Guid organizationId,
        SupplierPaymentFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        Guid organizationId,
        SupplierPaymentFilter filter,
        CancellationToken cancellationToken = default);

    Task<SupplierPayment?> FindByIdWithDetailsAsync(
        Guid id,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<SupplierPayment?> FindByIdempotencyKeyAsync(
        Guid organizationId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task AddPaymentWithAllocationsAsync(
        SupplierPayment payment,
        IEnumerable<SupplierPaymentAllocation> allocations,
        CancellationToken cancellationToken = default);

    Task UpdatePaymentAsync(
        SupplierPayment payment,
        CancellationToken cancellationToken = default);

    Task AddAuditLogAsync(
        AuditLog auditLog,
        CancellationToken cancellationToken = default);

    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default);
}
