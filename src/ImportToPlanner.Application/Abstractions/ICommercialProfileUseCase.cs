using ImportToPlanner.Application.Models;

namespace ImportToPlanner.Application.Abstractions;

/// <summary>
/// Defines profile and lifecycle operations for commercial accounts.
/// </summary>
public interface ICommercialProfileUseCase
{
    /// <summary>
    /// Gets the persisted profile for the current session identity.
    /// </summary>
    /// <param name="sessionIdentity">The signed-in session identity context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The persisted commercial account profile when available; otherwise <see langword="null"/>.</returns>
    Task<CommercialAccount?> GetProfileAsync(SessionIdentityContext sessionIdentity, CancellationToken cancellationToken);

    /// <summary>
    /// Marks an account as deleted.
    /// </summary>
    /// <param name="sessionIdentity">The signed-in session identity context.</param>
    /// <param name="occurredUtc">The UTC timestamp when deletion was requested.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task DeleteAccountAsync(
        SessionIdentityContext sessionIdentity,
        DateTimeOffset occurredUtc,
        CancellationToken cancellationToken);

    /// <summary>
    /// Restores a previously deleted account.
    /// </summary>
    /// <param name="sessionIdentity">The signed-in session identity context.</param>
    /// <param name="occurredUtc">The UTC timestamp when restoration was requested.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The restore outcome.</returns>
    Task<CommercialAccountRestoreResult> RestoreAccountAsync(
        SessionIdentityContext sessionIdentity,
        DateTimeOffset occurredUtc,
        CancellationToken cancellationToken);

    /// <summary>
    /// Purges account and audit records whose retention has expired.
    /// </summary>
    /// <param name="asOfUtc">The UTC retention cutoff timestamp.</param>
    /// <param name="batchSize">The maximum number of records to process.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of purged account records.</returns>
    Task<int> PurgeExpiredAsync(DateTimeOffset asOfUtc, int batchSize, CancellationToken cancellationToken);
}
