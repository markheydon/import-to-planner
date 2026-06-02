using Azure.Data.Tables;
using ImportToPlanner.CommercialService.Common.Storage;
using ImportToPlanner.CommercialService.Features.CommercialAccess.Models;

namespace ImportToPlanner.CommercialService.Features.CommercialAccess.Services;

/// <summary>
/// Handles commercial audit events stored in Azure Table Storage.
/// </summary>
public class CommercialAuditService
    : TableRepositoryBase<AccountAuditEvent>, ICommercialAuditService
{
    private const string TableName = "CommercialAccountAuditEvents";

    /// <summary>
    /// Initialises a new instance of the <see cref="CommercialAuditService"/> class with the specified Azure Table service client.
    /// </summary>
    /// <param name="tableServiceClient">The Azure Table service client.</param>
    public CommercialAuditService(TableServiceClient tableServiceClient)
        : base(tableServiceClient, TableName)
    {
    }

    /// <summary>
    /// Initialises a new instance of the <see cref="CommercialAuditService"/> class with a table client.
    /// This constructor is primarily intended for tests.
    /// </summary>
    /// <param name="tableClient">The table client.</param>
    protected CommercialAuditService(TableClient tableClient)
        : base(tableClient)
    {
    }

    /// <summary>
    /// Appends an audit event.
    /// </summary>
    /// <param name="auditEvent">The audit event to append.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual async Task AppendAsync(AccountAuditEvent auditEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);
        cancellationToken.ThrowIfCancellationRequested();

        await AddAsync(auditEvent, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Lists expired audit events up to the specified batch size.
    /// </summary>
    /// <param name="asOfUtc">The UTC time to compare retention expiry against.</param>
    /// <param name="batchSize">The maximum number of items to return.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains matching expired audit events.</returns>
    public virtual Task<IReadOnlyList<AccountAuditEvent>> ListExpiredAsync(
        DateTimeOffset asOfUtc,
        int batchSize,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return QueryAsync(
            $"{nameof(AccountAuditEvent.RetentionExpiresUtc)} le {asOfUtc}",
            batchSize,
            cancellationToken);
    }

    /// <summary>
    /// Permanently deletes expired audit events and returns the number removed.
    /// </summary>
    /// <param name="asOfUtc">The UTC time to compare retention expiry against.</param>
    /// <param name="batchSize">The maximum number of expired events to purge.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the number of purged events.</returns>
    public virtual async Task<int> PurgeExpiredAsync(DateTimeOffset asOfUtc, int batchSize, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var expired = await ListExpiredAsync(asOfUtc, batchSize, cancellationToken).ConfigureAwait(false);

        foreach (var auditEvent in expired)
        {
            var rowKey = BuildRowKey(auditEvent);
            await DeleteIgnoreNotFoundAsync(auditEvent.TenantId, rowKey, cancellationToken).ConfigureAwait(false);
        }

        return expired.Count;
    }

    /// <summary>
    /// Converts an <see cref="AccountAuditEvent"/> model to an Azure Table <see cref="TableEntity"/> for storage.
    /// </summary>
    /// <param name="model">The audit event model to convert.</param>
    /// <returns>A <see cref="TableEntity"/> representing the audit event.</returns>
    protected override TableEntity ToEntity(AccountAuditEvent model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return new TableEntity(model.TenantId, BuildRowKey(model))
        {
            ["UserId"] = model.UserId,
            [nameof(AccountAuditEvent.OccurredUtc)] = model.OccurredUtc,
            [nameof(AccountAuditEvent.EventType)] = model.EventType.ToString(),
            [nameof(AccountAuditEvent.Outcome)] = model.Outcome,
            [nameof(AccountAuditEvent.RetentionExpiresUtc)] = model.RetentionExpiresUtc,
        };
    }

    /// <summary>
    /// Converts an Azure Table <see cref="TableEntity"/> to an <see cref="AccountAuditEvent"/> model.
    /// </summary>
    /// <param name="entity">The table entity to convert.</param>
    /// <returns>An <see cref="AccountAuditEvent"/> model representing the table entity.</returns>
    protected override AccountAuditEvent ToModel(TableEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var eventTypeValue = entity.GetString(nameof(AccountAuditEvent.EventType));
        var parsedEventType = Enum.TryParse<AccountAuditEventType>(eventTypeValue, ignoreCase: true, out var eventType)
            ? eventType
            : AccountAuditEventType.SignInOutcome;

        return new AccountAuditEvent(
            entity.PartitionKey,
            entity.GetString("UserId") ?? string.Empty,
            entity.GetDateTimeOffset(nameof(AccountAuditEvent.OccurredUtc)) ?? DateTimeOffset.UtcNow,
            parsedEventType,
            entity.GetString(nameof(AccountAuditEvent.Outcome)) ?? string.Empty,
            entity.GetDateTimeOffset(nameof(AccountAuditEvent.RetentionExpiresUtc)) ?? DateTimeOffset.UtcNow.AddMonths(12));
    }

    private static string BuildRowKey(AccountAuditEvent model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return $"{model.UserId}|{model.OccurredUtc.UtcTicks:D19}|{model.EventType}";
    }
}
