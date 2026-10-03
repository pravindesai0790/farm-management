using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Expenses;

namespace FarmManagement.Application.Interfaces.Expenses;

public interface IPurchaseInvoiceService
{
    Task<PagedResponse<PurchaseInvoiceResponse>> ListAsync(
        ExpenseActor actor,
        PurchaseInvoiceFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PurchaseInvoiceResponse> GetAsync(
        ExpenseActor actor,
        Guid invoiceId,
        CancellationToken cancellationToken = default);

    Task<PurchaseInvoiceResponse> CreateDraftAsync(
        ExpenseActor actor,
        CreatePurchaseInvoiceRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<PurchaseInvoiceResponse> UpdateDraftAsync(
        ExpenseActor actor,
        Guid invoiceId,
        UpdatePurchaseInvoiceRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<PurchaseInvoiceResponse> PostAsync(
        ExpenseActor actor,
        Guid invoiceId,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<PurchaseInvoiceResponse> ReverseAsync(
        ExpenseActor actor,
        Guid invoiceId,
        ReversePurchaseInvoiceRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<PurchaseInvoiceReceiptSummaryResponse> GetReceiptSummaryAsync(
        ExpenseActor actor,
        Guid invoiceId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PurchaseInvoiceRemainingLineResponse>> GetRemainingToReceiveAsync(
        ExpenseActor actor,
        Guid invoiceId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PurchaseInvoiceReceiptGroupResponse>> GetReceiptHistoryAsync(
        ExpenseActor actor,
        Guid invoiceId,
        CancellationToken cancellationToken = default);

    Task<PurchaseInvoiceReceiptGroupResponse> ReceiveItemsAsync(
        ExpenseActor actor,
        Guid invoiceId,
        ReceivePurchaseInvoiceItemsRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);
}
