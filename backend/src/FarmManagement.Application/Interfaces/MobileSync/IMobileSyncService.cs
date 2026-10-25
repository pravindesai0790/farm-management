using FarmManagement.Application.DTOs.MobileSync;

namespace FarmManagement.Application.Interfaces.MobileSync;

/// <summary>
/// Service contract responsible for aggregating tenant master data and delta changes
/// for offline-first mobile client synchronization.
/// </summary>
public interface IMobileSyncService
{
    /// <summary>
    /// Fetches the complete initial bootstrap payload for an organization.
    /// If farmId is specified, farm-scoped entities (areas, plantations, cycles, assignments, locations)
    /// are filtered to that specific farm, while tenant-wide lookups are always included.
    /// </summary>
    /// <param name="organizationId">The authenticated tenant organization ID.</param>
    /// <param name="farmId">Optional farm identifier to scope parcelation and operational entities.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Complete multi-table sync payload with current UTC checkpoint.</returns>
    Task<MobileSyncPayloadResponse> GetBootstrapPayloadAsync(
        Guid organizationId,
        Guid? farmId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches all records that have been created or modified since the specified timestamp.
    /// Captures edits, additions, and soft-deletions/deactivations since sinceUtc.
    /// </summary>
    /// <param name="organizationId">The authenticated tenant organization ID.</param>
    /// <param name="farmId">Optional farm identifier to scope parcelation and operational entities.</param>
    /// <param name="sinceUtc">The UTC timestamp threshold of the client's previous sync.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Delta sync payload containing only modified records with new UTC checkpoint.</returns>
    Task<MobileSyncPayloadResponse> GetDeltaPayloadAsync(
        Guid organizationId,
        Guid? farmId,
        DateTimeOffset sinceUtc,
        CancellationToken cancellationToken = default);
}
