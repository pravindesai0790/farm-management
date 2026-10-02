namespace FarmManagement.Application.DTOs.Expenses;

public sealed record ExpenseCategoryResponse(
    Guid Id,
    Guid? OrganizationId,
    string Name,
    string? Code,
    string? Description,
    bool IsSystemDefault,
    bool IsActive,
    DateTimeOffset CreatedAt,
    Guid? CreatedBy,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedBy);

public sealed record CreateExpenseCategoryRequest(
    string? Name,
    string? Description);

public sealed record UpdateExpenseCategoryRequest(
    string? Name,
    string? Description);

public sealed record UpdateExpenseCategoryStatusRequest(bool IsActive);
