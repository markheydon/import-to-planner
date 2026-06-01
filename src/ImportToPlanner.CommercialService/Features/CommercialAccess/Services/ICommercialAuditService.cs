using ImportToPlanner.CommercialService.Features.CommercialAccess.Models;

namespace ImportToPlanner.CommercialService.Features.CommercialAccess.Services;

/// <summary>
/// Defines operations for storing and managing commercial account audit events.
/// </summary>
public interface ICommercialAuditService
{
    /// <summary>
    /// Appends an audit event.
    /// </summary>
    /// <param name="auditEvent">The audit event to append.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task AppendAsync(AccountAuditEvent auditEvent, CancellationToken cancellationToken);

    /// <summary>
    /// Lists expired audit events up to the specified batch size.
    /// </summary>
    /// <param name="asOfUtc">The UTC time to compare retention expiry against.</param>
    /// <param name="batchSize">The maximum number of items to return.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains matching expired audit events.</returns>
    Task<IReadOnlyList<AccountAuditEvent>> ListExpiredAsync(
        DateTimeOffset asOfUtc,
        int batchSize,
        CancellationToken cancellationToken);

    /// <summary>
    /// Permanently deletes expired audit events and returns the number removed.
    /// </summary>
    /// <param name="asOfUtc">The UTC time to compare retention expiry against.</param>
    /// <param name="batchSize">The maximum number of expired events to purge.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the number of purged events.</returns>
    Task<int> PurgeExpiredAsync(DateTimeOffset asOfUtc, int batchSize, CancellationToken cancellationToken);
}
