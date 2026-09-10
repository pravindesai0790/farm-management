using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.LaborActivities;

namespace FarmManagement.Application.Interfaces.LaborActivities;

public interface ILaborActivityService
{
    Task<PagedResponse<LaborActivityResponse>> ListAsync(
        LaborActivityActor actor,
        int page,
        int pageSize,
        Guid? farmId,
        Guid? farmAreaId,
        Guid? plantationId,
        Guid? cropCycleId,
        Guid? activityTypeId,
        DateOnly? fromDate,
        DateOnly? toDate,
        string? status,
        CancellationToken cancellationToken = default);

    Task<LaborActivityResponse> GetAsync(
        LaborActivityActor actor,
        Guid activityId,
        CancellationToken cancellationToken = default);

    Task<LaborActivityResponse> CreateAsync(
        LaborActivityActor actor,
        CreateLaborActivityRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<LaborActivityResponse> UpdateAsync(
        LaborActivityActor actor,
        Guid activityId,
        UpdateLaborActivityRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task CancelAsync(
        LaborActivityActor actor,
        Guid activityId,
        CancelLaborActivityRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NamedReferenceResponse>> ListTypesAsync(
        LaborActivityActor actor,
        CancellationToken cancellationToken = default);
}
