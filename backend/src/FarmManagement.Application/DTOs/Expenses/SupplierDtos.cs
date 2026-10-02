namespace FarmManagement.Application.DTOs.Expenses;

public sealed record SupplierResponse(
    Guid Id,
    Guid OrganizationId,
    string Name,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string? Address,
    string? RegistrationIdentifier,
    string? Notes,
    bool IsActive,
    DateTimeOffset CreatedAt,
    Guid CreatedBy,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedBy);

public sealed record CreateSupplierRequest(
    string? Name,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string? Address,
    string? RegistrationIdentifier,
    string? Notes);

public sealed record UpdateSupplierRequest(
    string? Name,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string? Address,
    string? RegistrationIdentifier,
    string? Notes);

public sealed record UpdateSupplierStatusRequest(bool IsActive);
