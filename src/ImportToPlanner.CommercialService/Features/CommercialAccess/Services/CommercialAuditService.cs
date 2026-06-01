using Azure;
using Azure.Data.Tables;
using ImportToPlanner.CommercialService.Features.CommercialAccess.Models;

namespace ImportToPlanner.CommercialService.Features.CommercialAccess.Services;

/// <summary>
/// Handles commercial audit events stored in Azure Table Storage.
/// </summary>
public class CommercialAuditService
{
    private const string TableName = "CommercialAccountAuditEvents";
    private readonly TableClient? tableClient;

    public CommercialAuditService(TableServiceClient tableServiceClient)
        : this(CreateTableClient(tableServiceClient))
    {
    }

    internal CommercialAuditService(TableClient tableClient)
    {
        this.tableClient = tableClient ?? throw new ArgumentNullException(nameof(tableClient));
    }

    protected CommercialAuditService()
    {
    }

    public virtual async Task AppendAsync(AccountAuditEvent auditEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);
        cancellationToken.ThrowIfCancellationRequested();

        await EnsureTableAsync(cancellationToken).ConfigureAwait(false);
        await TableClient.AddEntityAsync(ToEntity(auditEvent), cancellationToken).ConfigureAwait(false);
    }

    public virtual async Task<IReadOnlyList<AccountAuditEvent>> ListExpiredAsync(
        DateTimeOffset asOfUtc,
        int batchSize,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await EnsureTableAsync(cancellationToken).ConfigureAwait(false);

        var effectiveBatchSize = Math.Max(0, batchSize);
        if (effectiveBatchSize == 0)
        {
            return [];
        }

        var results = new List<AccountAuditEvent>(effectiveBatchSize);
        var queryFilter = TableClient.CreateQueryFilter($"{nameof(AccountAuditEvent.RetentionExpiresUtc)} le {asOfUtc}");

        await foreach (var entity in TableClient.QueryAsync<TableEntity>(filter: queryFilter, cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            results.Add(ToModel(entity));
            if (results.Count >= effectiveBatchSize)
            {
                break;
            }
        }

        return results;
    }

    public virtual async Task<int> PurgeExpiredAsync(DateTimeOffset asOfUtc, int batchSize, CancellationToken cancellationToken)
    {
        var expired = await ListExpiredAsync(asOfUtc, batchSize, cancellationToken).ConfigureAwait(false);

        foreach (var auditEvent in expired)
        {
            var rowKey = BuildRowKey(auditEvent);
            try
            {
                await TableClient.DeleteEntityAsync(auditEvent.TenantId, rowKey, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (RequestFailedException exception) when (exception.Status == 404)
            {
                // No-op when an event was concurrently removed.
            }
        }

        return expired.Count;
    }

    private TableClient TableClient => tableClient ?? throw new InvalidOperationException("The table client has not been initialised.");

    private async Task EnsureTableAsync(CancellationToken cancellationToken)
    {
        await TableClient.CreateIfNotExistsAsync(cancellationToken).ConfigureAwait(false);
    }

    private static TableEntity ToEntity(AccountAuditEvent model)
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

    private static AccountAuditEvent ToModel(TableEntity entity)
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

    private static TableClient CreateTableClient(TableServiceClient tableServiceClient)
    {
        ArgumentNullException.ThrowIfNull(tableServiceClient);
        return tableServiceClient.GetTableClient(TableName);
    }
}
