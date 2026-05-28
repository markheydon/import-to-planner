using ImportToPlanner.Application.Models;

namespace ImportToPlanner.Application.Abstractions;

/// <summary>
/// Defines commercial access resolution operations for authenticated sessions.
/// </summary>
public interface ICommercialAccessUseCase
{
    /// <summary>
    /// Resolves the commercial access decision for a signed-in identity context.
    /// </summary>
    /// <param name="sessionIdentity">The signed-in session identity context.</param>
    /// <param name="commercialModeEnabled">Indicates whether commercial mode is enabled in the outer layer.</param>
    /// <param name="occurredUtc">The UTC timestamp for this access resolution operation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The resolved access decision.</returns>
    Task<CommercialAccessDecision> ResolveAccessAsync(
        SessionIdentityContext sessionIdentity,
        bool commercialModeEnabled,
        DateTimeOffset occurredUtc,
        CancellationToken cancellationToken);
}
