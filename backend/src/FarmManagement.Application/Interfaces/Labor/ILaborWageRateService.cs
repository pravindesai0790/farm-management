using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Labor;
using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.Interfaces.Labor;

public interface ILaborWageRateService
{
    Task<PagedResponse<LaborWageRateResponse>> ListAsync(
        LaborWageRateActor actor,
        int page,
        int pageSize,
        string? gender,
        string? wageType,
        bool? isActive,
        DateOnly? businessDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LaborWageRateResponse>> ListAllAsync(
        LaborWageRateActor actor,
        string? gender,
        string? wageType,
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<LaborWageRateResponse> GetAsync(
        LaborWageRateActor actor,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<LaborWageRateResponse> CreateAsync(
        LaborWageRateActor actor,
        CreateLaborWageRateRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<LaborWageRateResponse> UpdateAsync(
        LaborWageRateActor actor,
        Guid id,
        UpdateLaborWageRateRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task ActivateAsync(
        LaborWageRateActor actor,
        Guid id,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task DeactivateAsync(
        LaborWageRateActor actor,
        Guid id,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<LaborWageRateResponse?> GetApplicableRateAsync(
        LaborWageRateActor actor,
        Gender gender,
        WageType wageType,
        DateOnly businessDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reusable lookup for attendance and payroll domain operations without HTTP actor context.
    /// </summary>
    Task<LaborWageRateResponse?> LookupApplicableRateAsync(
        Guid organizationId,
        Gender gender,
        WageType wageType,
        DateOnly businessDate,
        CancellationToken cancellationToken = default);
}
