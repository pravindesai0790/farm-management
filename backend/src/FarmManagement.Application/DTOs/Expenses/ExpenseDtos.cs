using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.DTOs.Expenses;

public sealed record ExpenseResponse(
    Guid Id,
    Guid OrganizationId,
    Guid FarmId,
    string FarmName,
    Guid ExpenseCategoryId,
    string ExpenseCategoryName,
    DateOnly ExpenseDate,
    string Description,
    decimal Amount,
    Guid CurrencyId,
    string CurrencyCode,
    string CurrencySymbol,
    Guid? SupplierId,
    string? SupplierName,
    string? ReferenceNumber,
    Guid? FarmAreaId,
    string? FarmAreaName,
    Guid? PlantationId,
    string? PlantationName,
    Guid? CropCycleId,
    string? CropCycleName,
    Guid? CropCycleStageId,
    string? CropCycleStageName,
    string? AttachmentReference,
    ExpenseStatus Status,
    DateTimeOffset? PostedAt,
    Guid? PostedBy,
    DateTimeOffset? ReversedAt,
    Guid? ReversedBy,
    string? ReversalReason,
    DateTimeOffset CreatedAt,
    Guid CreatedBy,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedBy);

public sealed record CreateExpenseRequest(
    Guid FarmId,
    Guid ExpenseCategoryId,
    DateOnly ExpenseDate,
    string? Description,
    decimal Amount,
    Guid CurrencyId,
    Guid? SupplierId = null,
    string? ReferenceNumber = null,
    Guid? FarmAreaId = null,
    Guid? PlantationId = null,
    Guid? CropCycleId = null,
    Guid? CropCycleStageId = null,
    string? AttachmentReference = null);

public sealed record UpdateExpenseRequest(
    Guid FarmId,
    Guid ExpenseCategoryId,
    DateOnly ExpenseDate,
    string? Description,
    decimal Amount,
    Guid CurrencyId,
    Guid? SupplierId = null,
    string? ReferenceNumber = null,
    Guid? FarmAreaId = null,
    Guid? PlantationId = null,
    Guid? CropCycleId = null,
    Guid? CropCycleStageId = null,
    string? AttachmentReference = null);

public sealed record ReverseExpenseRequest(string? Reason);

public sealed record ExpenseFilter(
    DateOnly? From = null,
    DateOnly? To = null,
    Guid? FarmId = null,
    Guid? CategoryId = null,
    Guid? SupplierId = null,
    ExpenseStatus? Status = null,
    string? Search = null);
