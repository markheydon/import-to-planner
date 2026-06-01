using Azure.Data.Tables;
using ImportToPlanner.CommercialService.Common.Storage;
using ImportToPlanner.CommercialService.Features.CommercialAccess.Models;

namespace ImportToPlanner.CommercialService.Features.CommercialAccess.Services;

/// <summary>
/// Handles commercial account records stored in Azure Table Storage.
/// </summary>
public class CommercialAccountsService
    : TableRepositoryBase<CommercialAccount>, ICommercialAccountsService
{
    // The name of the Azure Table used to store commercial account records.
    private const string TableName = "CommercialAccounts";

    /// <summary>
    /// Initialises a new instance of the <see cref="CommercialAccountsService"/> class with the specified Azure Table service client.
    /// </summary>
    /// <param name="tableServiceClient">The Azure Table service client used to interact with the table storage.</param>
    public CommercialAccountsService(TableServiceClient tableServiceClient)
        : base(tableServiceClient, TableName)
    {
    }

    /// <summary>
    /// Initialises a new instance of the <see cref="CommercialAccountsService"/> class with a table client.
    /// This constructor is primarily intended for tests.
    /// </summary>
    /// <param name="tableClient">The table client.</param>
    protected CommercialAccountsService(TableClient tableClient)
        : base(tableClient)
    {
    }

    /// <summary>
    /// Retrieves a commercial account for the specified tenant and user. If no account exists, null is returned.
    /// </summary>
    /// <param name="tenantId">The ID of the tenant.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The commercial account if found; otherwise, null.</returns>
    public virtual Task<CommercialAccount?> GetAsync(
        string tenantId,
        string userId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        return GetByKeysAsync(tenantId, userId, cancellationToken);
    }

    /// <summary>
    /// Creates a new commercial account.
    /// </summary>
    /// <param name="account">The commercial account to create.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual Task CreateAsync(
        CommercialAccount account,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(account);
        cancellationToken.ThrowIfCancellationRequested();

        return AddAsync(account, cancellationToken);
    }

    /// <summary>
    /// Marks a commercial account as deleted.
    /// </summary>
    /// <param name="tenantId">The ID of the tenant.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="deletedUtc">The UTC time when the account was deleted.</param>
    /// <param name="retentionExpiresUtc">The UTC time when the retention period expires.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

        var current = await GetByKeysAsync(tenantId, userId, cancellationToken).ConfigureAwait(false);
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

        await UpsertReplaceAsync(updated, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Restores a previously deleted commercial account, marking it as active again and clearing deletion-related timestamps.
    /// </summary>
    /// <param name="tenantId">The ID of the tenant.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="restoredUtc">The UTC time when the account was restored.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual async Task RestoreAsync(
        string tenantId,
        string userId,
        DateTimeOffset restoredUtc,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var current = await GetByKeysAsync(tenantId, userId, cancellationToken).ConfigureAwait(false);
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

        await UpsertReplaceAsync(updated, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Lists commercial accounts that are marked as deleted and have a retention expiration date that has passed, up to the specified batch size.
    /// </summary>
    /// <param name="asOfUtc">The UTC time to check for expired deletions.</param>
    /// <param name="batchSize">The maximum number of accounts to return.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a read-only list of expired deleted commercial accounts.</returns>
    public virtual Task<IReadOnlyList<CommercialAccount>> ListExpiredDeletedAsync(
        DateTimeOffset asOfUtc,
        int batchSize,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return QueryAsync(
            $"{nameof(CommercialAccount.Status)} eq {CommercialAccountStatus.Deleted} and {nameof(CommercialAccount.RetentionExpiresUtc)} le {asOfUtc}",
            batchSize,
            cancellationToken,
            model => model.RetentionExpiresUtc is not null);
    }

    /// <summary>
    /// Permanently deletes a commercial account from the Azure Table Storage. This operation is irreversible and
    /// should only be performed after the retention period has expired.
    /// </summary>
    /// <param name="tenantId">The ID of the tenant.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual Task PurgeAsync(
        string tenantId,
        string userId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        return DeleteIgnoreNotFoundAsync(tenantId, userId, cancellationToken);
    }

    /// <summary>
    /// Converts a <see cref="CommercialAccount"/> model to an Azure Table <see cref="TableEntity"/> for storage.
    /// The tenant ID is used as the partition key and the user ID is used as the row key.
    /// </summary>
    /// <param name="model">The commercial account model to convert.</param>
    /// <returns>A <see cref="TableEntity"/> representing the commercial account.</returns>
    protected override TableEntity ToEntity(CommercialAccount model)
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

    /// <summary>
    /// Converts an Azure Table <see cref="TableEntity"/> to a <see cref="CommercialAccount"/> model.
    /// </summary>
    /// <param name="entity">The table entity to convert.</param>
    /// <returns>A <see cref="CommercialAccount"/> model representing the table entity.</returns>
    protected override CommercialAccount ToModel(TableEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var statusValue = entity.GetString(nameof(CommercialAccount.Status));

        var parsedStatus = Enum.TryParse<CommercialAccountStatus>(
            statusValue,
            ignoreCase: true,
            out var status)
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
}
