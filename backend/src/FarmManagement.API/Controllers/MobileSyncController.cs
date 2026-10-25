using FarmManagement.API.Helpers;
using FarmManagement.Application.DTOs.MobileSync;
using FarmManagement.Application.Interfaces.MobileSync;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmManagement.API.Controllers;

/// <summary>
/// Controller providing optimized data synchronization endpoints for the mobile client.
/// Supports both full tenant bootstrap and incremental delta sync operations.
/// </summary>
[ApiController]
[Route("api/mobile/sync")]
[Authorize]
public sealed class MobileSyncController(IMobileSyncService mobileSyncService) : ControllerBase
{
    /// <summary>
    /// Downloads the complete initial master data package for an organization.
    /// Can optionally be scoped to a specific farmId for land parcelation and operational entities.
    /// </summary>
    /// <param name="farmId">Optional farm identifier to scope parcelation and operational records.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A MobileSyncPayloadResponse containing all 15 master tables and current UTC timestamp.</returns>
    [HttpGet("bootstrap")]
    public async Task<ActionResult<MobileSyncPayloadResponse>> Bootstrap(
        [FromQuery] Guid? farmId,
        CancellationToken cancellationToken)
    {
        var (_, organizationId) = UserContextHelper.GetUserIds(User);

        var payload = await mobileSyncService.GetBootstrapPayloadAsync(
            organizationId,
            farmId,
            cancellationToken);

        return Ok(payload);
    }

    /// <summary>
    /// Downloads all master records created or modified since the client's previous sync checkpoint.
    /// Enables fast, low-bandwidth incremental updates for offline field workers.
    /// </summary>
    /// <param name="sinceUtc">The UTC timestamp threshold of the previous sync.</param>
    /// <param name="farmId">Optional farm identifier to scope parcelation and operational records.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A MobileSyncPayloadResponse containing only delta modified records and updated checkpoint.</returns>
    [HttpGet("delta")]
    public async Task<ActionResult<MobileSyncPayloadResponse>> Delta(
        [FromQuery] DateTimeOffset sinceUtc,
        [FromQuery] Guid? farmId,
        CancellationToken cancellationToken)
    {
        var (_, organizationId) = UserContextHelper.GetUserIds(User);

        var payload = await mobileSyncService.GetDeltaPayloadAsync(
            organizationId,
            farmId,
            sinceUtc,
            cancellationToken);

        return Ok(payload);
    }
}
