using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Labor;
using FarmManagement.Application.Interfaces.Labor;

namespace FarmManagement.Application.Services;

public sealed class AttendanceService(IAttendanceStore store) : IAttendanceService
{
    private const int DefaultPageSize = 20;
    private const int MaximumPageSize = 100;

    public async Task<PagedResponse<AttendanceEligibleWorkerResponse>> GetEligibleWorkersAsync(
        AttendanceActor actor,
        Guid farmId,
        DateOnly attendanceDate,
        string? search = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);

        if (farmId == Guid.Empty)
        {
            throw Validation("farmId", "Farm is required.");
        }

        if (attendanceDate == default)
        {
            throw Validation("attendanceDate", "Attendance date is required.");
        }

        if (page < 1)
        {
            throw Validation("page", "Page must be at least 1.");
        }

        pageSize = NormalizePageSize(pageSize);

        var farm = await store.FindFarmAsync(farmId, actor.OrganizationId, cancellationToken);
        if (farm is null)
        {
            throw new ResourceNotFoundException("The farm was not found.");
        }

        if (!farm.IsActive)
        {
            throw Validation("farmId", "Cannot load attendance for an inactive farm.");
        }

        var normalizedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        var totalCount = await store.CountEligibleWorkersAsync(
            actor.OrganizationId,
            farmId,
            attendanceDate,
            normalizedSearch,
            cancellationToken);

        if (totalCount == 0)
        {
            return new PagedResponse<AttendanceEligibleWorkerResponse>(
                [],
                page,
                pageSize,
                0);
        }

        var skip = (page - 1) * pageSize;
        var items = await store.ListEligibleWorkersAsync(
            actor.OrganizationId,
            farmId,
            attendanceDate,
            normalizedSearch,
            skip,
            pageSize,
            cancellationToken);

        return new PagedResponse<AttendanceEligibleWorkerResponse>(
            items,
            page,
            pageSize,
            totalCount);
    }

    private static int NormalizePageSize(int pageSize) =>
        pageSize == 0 ? DefaultPageSize : pageSize is < 1 or > MaximumPageSize
            ? throw Validation("pageSize", $"Page size must be between 1 and {MaximumPageSize}.")
            : pageSize;

    private static void ValidateActor(AttendanceActor actor)
    {
        if (actor.UserId == Guid.Empty || actor.OrganizationId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("The access token does not contain a valid user scope.");
        }
    }

    private static ValidationException Validation(string propertyName, string message) =>
        new("Validation failed.", new Dictionary<string, string[]> { [propertyName] = [message] });
}
