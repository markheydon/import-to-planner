using Azure;
using Azure.Data.Tables;
using ImportToPlanner.CommercialService.CommercialAccounts.Models;

namespace ImportToPlanner.CommercialService.CommercialAccounts.Services;

/// <summary>
/// Handles commercial account records stored in Azure Table Storage.
/// </summary>
public class CommercialAccountsService
{
    private const string TableName = "CommercialAccounts";
    private readonly TableClient? tableClient;

    public CommercialAccountsService(TableServiceClient tableServiceClient)
        : this(CreateTableClient(tableServiceClient))
    {
    }

    internal CommercialAccountsService(TableClient tableClient)
    {
        this.tableClient = tableClient ?? throw new ArgumentNullException(nameof(tableClient));
    }

    protected CommercialAccountsService()
    {
    }

    public virtual async Task<CommercialAccount?> GetAsync(string tenantId, string userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        await EnsureTableAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var response = await TableClient
                .GetEntityAsync<TableEntity>(tenantId, userId, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return ToModel(response.Value);
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return null;
        }
    }

    public virtual async Task CreateAsync(CommercialAccount account, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(account);
        cancellationToken.ThrowIfCancellationRequested();

        await EnsureTableAsync(cancellationToken).ConfigureAwait(false);
        await TableClient.AddEntityAsync(ToEntity(account), cancellationToken).ConfigureAwait(false);
    }

    public virtual async Task MarkDeletedAsync(
        string tenantId,
        string userId,
        DateTimeOffset deletedUtc,
        DateTimeOffset retentionExpiresUtc,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var current = await GetAsync(tenantId, userId, cancellationToken).ConfigureAwait(false);
        if (current is null)
        {
            return;
        }

        var updated = current with
        {
            Status = CommercialAccountStatus.Deleted,
            DeletedUtc = deletedUtc,
            RetentionExpiresUtc = retentionExpiresUtc,
        };

        await TableClient
            .UpsertEntityAsync(ToEntity(updated), TableUpdateMode.Replace, cancellationToken)
            .ConfigureAwait(false);
    }

    public virtual async Task RestoreAsync(string tenantId, string userId, DateTimeOffset restoredUtc, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var current = await GetAsync(tenantId, userId, cancellationToken).ConfigureAwait(false);
        if (current is null)
        {
            return;
        }

        var updated = current with
        {
            Status = CommercialAccountStatus.Active,
            DeletedUtc = null,
            RetentionExpiresUtc = null,
            RestoredUtc = restoredUtc,
        };

        await TableClient
            .UpsertEntityAsync(ToEntity(updated), TableUpdateMode.Replace, cancellationToken)
            .ConfigureAwait(false);
    }

    public virtual async Task<IReadOnlyList<CommercialAccount>> ListExpiredDeletedAsync(
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

        var results = new List<CommercialAccount>(effectiveBatchSize);
        var queryFilter = TableClient.CreateQueryFilter(
            $"{nameof(CommercialAccount.Status)} eq {CommercialAccountStatus.Deleted.ToString()} and {nameof(CommercialAccount.RetentionExpiresUtc)} le {asOfUtc}");

        await foreach (var entity in TableClient.QueryAsync<TableEntity>(filter: queryFilter, cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            var model = ToModel(entity);
            if (model.RetentionExpiresUtc is null)
            {
                continue;
            }

            results.Add(model);
            if (results.Count >= effectiveBatchSize)
            {
                break;
            }
        }

        return results;
    }

    public virtual async Task PurgeAsync(string tenantId, string userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        await EnsureTableAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await TableClient.DeleteEntityAsync(tenantId, userId, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            // No-op when the account has already been removed.
        }
    }

    private TableClient TableClient => tableClient ?? throw new InvalidOperationException("The table client has not been initialised.");

    private async Task EnsureTableAsync(CancellationToken cancellationToken)
    {
        await TableClient.CreateIfNotExistsAsync(cancellationToken).ConfigureAwait(false);
    }

    private static TableEntity ToEntity(CommercialAccount model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return new TableEntity(model.TenantId, model.UserId)
        {
            [nameof(CommercialAccount.CreatedUtc)] = model.CreatedUtc,
            [nameof(CommercialAccount.Status)] = model.Status.ToString(),
            [nameof(CommercialAccount.DeletedUtc)] = model.DeletedUtc,
            [nameof(CommercialAccount.RetentionExpiresUtc)] = model.RetentionExpiresUtc,
            [nameof(CommercialAccount.RestoredUtc)] = model.RestoredUtc,
            [nameof(CommercialAccount.LastSignInOutcomeUtc)] = model.LastSignInOutcomeUtc,
        };
    }

    private static CommercialAccount ToModel(TableEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var statusValue = entity.GetString(nameof(CommercialAccount.Status));
        var parsedStatus = Enum.TryParse<CommercialAccountStatus>(statusValue, ignoreCase: true, out var status)
            ? status
            : CommercialAccountStatus.Active;

        return new CommercialAccount(
            entity.PartitionKey,
            entity.RowKey,
            entity.GetDateTimeOffset(nameof(CommercialAccount.CreatedUtc)) ?? DateTimeOffset.UtcNow,
            parsedStatus,
            entity.GetDateTimeOffset(nameof(CommercialAccount.DeletedUtc)),
            entity.GetDateTimeOffset(nameof(CommercialAccount.RetentionExpiresUtc)),
            entity.GetDateTimeOffset(nameof(CommercialAccount.RestoredUtc)),
            entity.GetDateTimeOffset(nameof(CommercialAccount.LastSignInOutcomeUtc)));
    }

    private static TableClient CreateTableClient(TableServiceClient tableServiceClient)
    {
        ArgumentNullException.ThrowIfNull(tableServiceClient);
        return tableServiceClient.GetTableClient(TableName);
    }
}
