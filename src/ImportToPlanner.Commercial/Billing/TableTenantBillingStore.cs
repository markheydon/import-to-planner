using Azure;
using Azure.Data.Tables;
using ImportToPlanner.Commercial.Abstractions;
using ImportToPlanner.Commercial.Models;

namespace ImportToPlanner.Commercial.Billing;

/// <summary>
/// Azure Table Storage adapter for tenant Stripe billing metadata.
/// </summary>
internal sealed class TableTenantBillingStore(TableClient tableClient) : ITenantBillingStore, IDisposable
{
    private const string BillingRowKey = "billing";

    private readonly TableClient tableClient = tableClient ?? throw new ArgumentNullException(nameof(tableClient));
    private readonly SemaphoreSlim initialiseSemaphore = new(1, 1);
    private volatile bool tableCreated;

    /// <inheritdoc/>
    public async Task<TenantBillingMetadata?> GetAsync(string tenantId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        await EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var response = await tableClient
                .GetEntityAsync<TableEntity>(tenantId, BillingRowKey, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return ToModel(response.Value);
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task UpsertAsync(TenantBillingMetadata metadata, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(metadata.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(metadata.StripeCustomerId);

        await EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
        await tableClient
            .UpsertEntityAsync(ToEntity(metadata), TableUpdateMode.Replace, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public void Dispose() => initialiseSemaphore.Dispose();

    private async Task EnsureCreatedAsync(CancellationToken cancellationToken)
    {
        if (tableCreated)
        {
            return;
        }

        await initialiseSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (tableCreated)
            {
                return;
            }

            await tableClient.CreateIfNotExistsAsync(cancellationToken).ConfigureAwait(false);
            tableCreated = true;
        }
        finally
        {
            initialiseSemaphore.Release();
        }
    }

    private static TableEntity ToEntity(TenantBillingMetadata metadata)
    {
        var entity = new TableEntity(metadata.TenantId, BillingRowKey)
        {
            ["StripeCustomerId"] = metadata.StripeCustomerId,
            ["UpdatedUtc"] = metadata.UpdatedUtc,
        };

        if (!string.IsNullOrWhiteSpace(metadata.LastInvoiceOrReceiptId))
        {
            entity["LastInvoiceOrReceiptId"] = metadata.LastInvoiceOrReceiptId;
        }

        return entity;
    }

    private static TenantBillingMetadata ToModel(TableEntity entity)
    {
        return new TenantBillingMetadata(
            entity.PartitionKey,
            entity.GetString("StripeCustomerId") ?? string.Empty,
            entity.GetString("LastInvoiceOrReceiptId"),
            entity.GetDateTimeOffset("UpdatedUtc") ?? DateTimeOffset.UtcNow);
    }
}
