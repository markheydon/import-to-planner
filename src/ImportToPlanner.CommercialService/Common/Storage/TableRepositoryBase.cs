using Azure;
using Azure.Data.Tables;

namespace ImportToPlanner.CommercialService.Common.Storage;

/// <summary>
/// Provides common Azure Table Storage operations for table-backed services.
/// Derived classes are responsible for mapping between domain models and <see cref="TableEntity"/> instances.
/// </summary>
/// <typeparam name="TModel">The domain model stored in the table.</typeparam>
public abstract class TableRepositoryBase<TModel>
{
    private readonly TableClient _tableClient;
    private Task? _initialiseTask;

    /// <summary>
    /// Initialises a new instance of the <see cref="TableRepositoryBase{TModel}"/> class
    /// using an Azure Table service client and a table name.
    /// </summary>
    /// <param name="tableServiceClient">The Azure Table service client.</param>
    /// <param name="tableName">The table name.</param>
    protected TableRepositoryBase(TableServiceClient tableServiceClient, string tableName)
    {
        ArgumentNullException.ThrowIfNull(tableServiceClient);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        _tableClient = tableServiceClient.GetTableClient(tableName);
    }

    /// <summary>
    /// Initialises a new instance of the <see cref="TableRepositoryBase{TModel}"/> class
    /// using an already-created table client. This is primarily useful for testing.
    /// </summary>
    /// <param name="tableClient">The table client.</param>
    protected TableRepositoryBase(TableClient tableClient)
    {
        _tableClient = tableClient ?? throw new ArgumentNullException(nameof(tableClient));
    }

    /// <summary>
    /// Gets the underlying Azure Table client for derived classes that need direct access.
    /// </summary>
    protected TableClient TableClient => _tableClient;

    /// <summary>
    /// Ensures that the underlying table exists.
    /// The creation call is only initiated once, even if called multiple times concurrently.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected Task EnsureTableAsync(CancellationToken cancellationToken)
    {
        return _initialiseTask ??= _tableClient.CreateIfNotExistsAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves an entity by partition key and row key, returning <c>null</c> if the entity does not exist.
    /// </summary>
    /// <param name="partitionKey">The partition key.</param>
    /// <param name="rowKey">The row key.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The mapped domain model if found; otherwise, <c>null</c>.</returns>
    protected async Task<TModel?> GetByKeysAsync(
        string partitionKey,
        string rowKey,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(partitionKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(rowKey);

        await EnsureTableAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var response = await _tableClient
                .GetEntityAsync<TableEntity>(partitionKey, rowKey, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return ToModel(response.Value);
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return default;
        }
    }

    /// <summary>
    /// Adds a new entity to the table.
    /// </summary>
    /// <param name="model">The model to add.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task AddAsync(
        TModel model,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);
        cancellationToken.ThrowIfCancellationRequested();

        await EnsureTableAsync(cancellationToken).ConfigureAwait(false);

        await _tableClient
            .AddEntityAsync(ToEntity(model), cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Inserts or replaces an entity in the table.
    /// </summary>
    /// <param name="model">The model to upsert.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task UpsertReplaceAsync(
        TModel model,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);
        cancellationToken.ThrowIfCancellationRequested();

        await EnsureTableAsync(cancellationToken).ConfigureAwait(false);

        await _tableClient
            .UpsertEntityAsync(ToEntity(model), TableUpdateMode.Replace, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes an entity by partition key and row key. Missing entities are ignored.
    /// </summary>
    /// <param name="partitionKey">The partition key.</param>
    /// <param name="rowKey">The row key.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task DeleteIgnoreNotFoundAsync(
        string partitionKey,
        string rowKey,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(partitionKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(rowKey);

        await EnsureTableAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await _tableClient
                .DeleteEntityAsync(partitionKey, rowKey, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            // No-op when the entity has already been removed.
        }
    }

    /// <summary>
    /// Queries the table and maps matching entities to domain models.
    /// </summary>
    /// <param name="filter">The OData filter expression as an interpolated string.</param>
    /// <param name="batchSize">The maximum number of items to return. Use 0 to return none.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <param name="predicate">
    /// Optional in-memory predicate applied after mapping.
    /// Useful when the table query cannot fully express the logic.
    /// </param>
    /// <returns>A read-only list of matching models.</returns>
    protected async Task<IReadOnlyList<TModel>> QueryAsync(
        FormattableString filter,
        int batchSize,
        CancellationToken cancellationToken,
        Func<TModel, bool>? predicate = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(filter);

        await EnsureTableAsync(cancellationToken).ConfigureAwait(false);

        var effectiveBatchSize = Math.Max(0, batchSize);
        if (effectiveBatchSize == 0)
        {
            return [];
        }

        var results = new List<TModel>(effectiveBatchSize);
        var queryFilter = TableClient.CreateQueryFilter(filter);

        await foreach (var entity in _tableClient
            .QueryAsync<TableEntity>(filter: queryFilter, cancellationToken: cancellationToken)
            .ConfigureAwait(false))
        {
            var model = ToModel(entity);

            if (predicate is not null && !predicate(model))
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

    /// <summary>
    /// Converts a domain model to a table entity.
    /// </summary>
    /// <param name="model">The domain model.</param>
    /// <returns>A table entity representing the model.</returns>
    protected abstract TableEntity ToEntity(TModel model);

    /// <summary>
    /// Converts a table entity to a domain model.
    /// </summary>
    /// <param name="entity">The table entity.</param>
    /// <returns>The mapped domain model.</returns>
    protected abstract TModel ToModel(TableEntity entity);
}
