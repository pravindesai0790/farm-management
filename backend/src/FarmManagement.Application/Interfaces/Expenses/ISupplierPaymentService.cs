using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Expenses;

namespace FarmManagement.Application.Interfaces.Expenses;

public interface ISupplierPaymentService
{
    Task<PagedResponse<SupplierPaymentResponse>> ListAsync(
        ExpenseActor actor,
        SupplierPaymentFilter filter,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<SupplierPaymentResponse> GetByIdAsync(
        ExpenseActor actor,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<SupplierPaymentResponse> RecordPaymentAsync(
        ExpenseActor actor,
        RecordSupplierPaymentRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task<SupplierPaymentResponse> ReversePaymentAsync(
        ExpenseActor actor,
        Guid id,
        ReverseSupplierPaymentRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UnpaidPurchaseInvoiceSummaryResponse>> GetUnpaidInvoicesAsync(
        ExpenseActor actor,
        Guid supplierId,
        Guid? currencyId = null,
        CancellationToken cancellationToken = default);
}
