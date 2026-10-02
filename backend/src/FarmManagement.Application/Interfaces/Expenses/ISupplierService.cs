using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Expenses;

namespace FarmManagement.Application.Interfaces.Expenses;

public interface ISupplierService
{
    Task<PagedResponse<SupplierResponse>> ListAsync(
        ExpenseActor actor,
        int page,
        int pageSize,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<SupplierResponse> GetAsync(
        ExpenseActor actor,
        Guid supplierId,
        CancellationToken cancellationToken = default);

    Task<SupplierResponse> CreateAsync(
        ExpenseActor actor,
        CreateSupplierRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<SupplierResponse> UpdateAsync(
        ExpenseActor actor,
        Guid supplierId,
        UpdateSupplierRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<bool> ActivateAsync(
        ExpenseActor actor,
        Guid supplierId,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(
        ExpenseActor actor,
        Guid supplierId,
        string? ipAddress,
        CancellationToken cancellationToken = default);
}
