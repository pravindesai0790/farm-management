namespace FarmManagement.Application.DTOs.Labor;

public sealed record LaborCategoryResponse(
    Guid Id,
    Guid? OrganizationId,
    string Name,
    string? Description,
    bool IsSystem,
    bool IsActive,
    int WorkerCount,
    DateTimeOffset CreatedAt,
    Guid? CreatedBy,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedBy);

public sealed record CreateLaborCategoryRequest(
    string? Name,
    string? Description);

public sealed record UpdateLaborCategoryRequest(
    string? Name,
    string? Description);

public sealed record LaborCategoryActor(Guid UserId, Guid OrganizationId);
