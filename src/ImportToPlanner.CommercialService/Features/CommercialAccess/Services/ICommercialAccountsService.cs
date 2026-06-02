using ImportToPlanner.CommercialService.Features.CommercialAccess.Models;

namespace ImportToPlanner.CommercialService.Features.CommercialAccess.Services;

/// <summary>
/// Defines operations for managing commercial account records in storage.
/// </summary>
public interface ICommercialAccountsService
{
    /// <summary>
    /// Retrieves a commercial account for the specified tenant and user.
    /// </summary>
    /// <param name="tenantId">The ID of the tenant.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The commercial account if found; otherwise, <see langword="null"/>.</returns>
    Task<CommercialAccount?> GetAsync(
        string tenantId,
        string userId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new commercial account.
    /// </summary>
    /// <param name="account">The commercial account to create.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task CreateAsync(
        CommercialAccount account,
        CancellationToken cancellationToken);

    /// <summary>
    /// Marks a commercial account as deleted.
    /// </summary>
    /// <param name="tenantId">The ID of the tenant.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="deletedUtc">The UTC time when the account was deleted.</param>
    /// <param name="retentionExpiresUtc">The UTC time when the retention period expires.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task MarkDeletedAsync(
        string tenantId,
        string userId,
        DateTimeOffset deletedUtc,
        DateTimeOffset retentionExpiresUtc,
        CancellationToken cancellationToken);

    /// <summary>
    /// Restores a previously deleted commercial account and marks it as active.
    /// </summary>
    /// <param name="tenantId">The ID of the tenant.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="restoredUtc">The UTC time when the account was restored.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task RestoreAsync(
        string tenantId,
        string userId,
        DateTimeOffset restoredUtc,
        CancellationToken cancellationToken);

    /// <summary>
    /// Lists deleted commercial accounts whose retention period has expired.
    /// </summary>
    /// <param name="asOfUtc">The UTC time used to determine expiration.</param>
    /// <param name="batchSize">The maximum number of accounts to return.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task containing expired deleted commercial accounts.</returns>
    Task<IReadOnlyList<CommercialAccount>> ListExpiredDeletedAsync(
        DateTimeOffset asOfUtc,
        int batchSize,
        CancellationToken cancellationToken);

    /// <summary>
    /// Permanently deletes a commercial account from storage.
    /// </summary>
    /// <param name="tenantId">The ID of the tenant.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PurgeAsync(
        string tenantId,
        string userId,
        CancellationToken cancellationToken);
}
