using Azure;
using Azure.Data.Tables;
using ImportToPlanner.CommercialService.Models;

namespace ImportToPlanner.CommercialService.CommercialAccounts.Storage;

/// <summary>
/// Persists commercial account audit events in Azure Table Storage.
/// </summary>
public sealed class TableCommercialAuditStore(TableClient tableClient) : ICommercialAuditStore, IDisposable
{
    private readonly TableClient tableClient = tableClient ?? throw new ArgumentNullException(nameof(tableClient));
    private readonly SemaphoreSlim initialiseSemaphore = new(1, 1);
    private volatile bool tableCreated;

    public async Task AppendAsync(AccountAuditEvent auditEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);
        cancellationToken.ThrowIfCancellationRequested();

        await EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
        await tableClient.AddEntityAsync(ToEntity(auditEvent), cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AccountAuditEvent>> ListExpiredAsync(
        DateTimeOffset asOfUtc,
        int batchSize,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

        var effectiveBatchSize = Math.Max(0, batchSize);
        if (effectiveBatchSize == 0)
        {
            return [];
        }

        var results = new List<AccountAuditEvent>(effectiveBatchSize);
        var queryFilter = TableClient.CreateQueryFilter($"{nameof(AccountAuditEvent.RetentionExpiresUtc)} le {asOfUtc}");

        await foreach (var entity in tableClient.QueryAsync<TableEntity>(filter: queryFilter, cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            var model = ToModel(entity);
            results.Add(model);
            if (results.Count >= effectiveBatchSize)
            {
                break;
            }
        }

        return results;
    }

    public async Task<int> PurgeExpiredAsync(DateTimeOffset asOfUtc, int batchSize, CancellationToken cancellationToken)
    {
        var expired = await ListExpiredAsync(asOfUtc, batchSize, cancellationToken).ConfigureAwait(false);

        foreach (var auditEvent in expired)
        {
            var rowKey = BuildRowKey(auditEvent);
            try
            {
                await tableClient.DeleteEntityAsync(auditEvent.TenantId, rowKey, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (RequestFailedException exception) when (exception.Status == 404)
            {
                // No-op when an event was concurrently removed.
            }
        }

        return expired.Count;
    }

    public void Dispose()
    {
        initialiseSemaphore.Dispose();
    }

    private async Task EnsureCreatedAsync(CancellationToken cancellationToken)
    {
        if (tableCreated)
        {
            return;
        }

        await initialiseSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!tableCreated)
            {
                await tableClient.CreateIfNotExistsAsync(cancellationToken).ConfigureAwait(false);
                tableCreated = true;
            }
        }
        finally
        {
            initialiseSemaphore.Release();
        }
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
}
