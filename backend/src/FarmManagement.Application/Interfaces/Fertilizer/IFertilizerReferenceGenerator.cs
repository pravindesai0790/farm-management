namespace FarmManagement.Application.Interfaces.Fertilizer;

/// <summary>
/// Service responsible for allocating sequential, collision-free, year-scoped reference numbers
/// for fertilizer applications in the format FERT-YYYY-NNNNNN per organization.
/// </summary>
public interface IFertilizerReferenceGenerator
{
    /// <summary>
    /// Generates the next sequential reference number for the specified organization and creation year.
    /// Format: FERT-YYYY-NNNNNN (e.g. FERT-2026-000001).
    /// </summary>
    /// <param name="organizationId">The organization identifier.</param>
    /// <param name="year">The four-digit calendar year of creation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The allocated reference number string.</returns>
    Task<string> NextReferenceNumberAsync(Guid organizationId, int year, CancellationToken cancellationToken = default);
}
