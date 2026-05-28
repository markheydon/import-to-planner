using ImportToPlanner.Application.Models;

namespace ImportToPlanner.Application.Abstractions;

/// <summary>
/// Defines persistence operations for commercial account audit events.
/// </summary>
public interface ICommercialAuditStore
{
    /// <summary>
    /// Appends an audit event.
    /// </summary>
    /// <param name="auditEvent">The event to append.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task AppendAsync(AccountAuditEvent auditEvent, CancellationToken cancellationToken);

    /// <summary>
    /// Lists audit events whose retention has expired.
    /// </summary>
    /// <param name="asOfUtc">The UTC cutoff timestamp.</param>
    /// <param name="batchSize">The maximum number of records to return.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of expired audit events.</returns>
    Task<IReadOnlyList<AccountAuditEvent>> ListExpiredAsync(
        DateTimeOffset asOfUtc,
        int batchSize,
        CancellationToken cancellationToken);

    /// <summary>
    /// Permanently removes expired audit events.
    /// </summary>
    /// <param name="asOfUtc">The UTC cutoff timestamp.</param>
    /// <param name="batchSize">The maximum number of events to purge.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of removed events.</returns>
    Task<int> PurgeExpiredAsync(DateTimeOffset asOfUtc, int batchSize, CancellationToken cancellationToken);
}
