using ImportToPlanner.CommercialService.Models;

namespace ImportToPlanner.CommercialService.CommercialAccounts;

/// <summary>
/// Defines persistence operations for commercial account records.
/// </summary>
public interface ICommercialAccountStore
{
    /// <summary>
    /// Gets the account for a tenant and user identifier pair.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The account when found; otherwise <see langword="null"/>.</returns>
    Task<CommercialAccount?> GetAsync(string tenantId, string userId, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new commercial account record.
    /// </summary>
    /// <param name="account">The account to persist.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task CreateAsync(CommercialAccount account, CancellationToken cancellationToken);

    /// <summary>
    /// Marks an account as deleted and stores its retention deadline.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="deletedUtc">The UTC deletion timestamp.</param>
    /// <param name="retentionExpiresUtc">The UTC retention expiry timestamp.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task MarkDeletedAsync(
        string tenantId,
        string userId,
        DateTimeOffset deletedUtc,
        DateTimeOffset retentionExpiresUtc,
        CancellationToken cancellationToken);

    /// <summary>
    /// Restores a previously deleted account.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="restoredUtc">The UTC restore timestamp.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task RestoreAsync(string tenantId, string userId, DateTimeOffset restoredUtc, CancellationToken cancellationToken);

    /// <summary>
    /// Lists deleted accounts whose retention has expired.
    /// </summary>
    /// <param name="asOfUtc">The UTC cutoff timestamp.</param>
    /// <param name="batchSize">The maximum number of records to return.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of expired deleted accounts.</returns>
    Task<IReadOnlyList<CommercialAccount>> ListExpiredDeletedAsync(
        DateTimeOffset asOfUtc,
        int batchSize,
        CancellationToken cancellationToken);

    /// <summary>
    /// Permanently removes an account record.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task PurgeAsync(string tenantId, string userId, CancellationToken cancellationToken);
}
