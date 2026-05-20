using Azure;
using Azure.Data.Tables;
using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Models;

namespace ImportToPlanner.Infrastructure.Graph.TenantMetadata;

/// <summary>
/// Provides Azure Table Storage persistence for tenant-scoped operational metadata.
/// </summary>
public sealed class TableTenantOperationalMetadataStore : ITenantOperationalMetadataStore
{
    private const string TenantPartitionKey = "tenant";
    private readonly TableClient tableClient;

    /// <summary>
    /// Initialises a new instance of the <see cref="TableTenantOperationalMetadataStore"/> class.
    /// </summary>
    /// <param name="connectionString">The storage connection string.</param>
    /// <param name="tableName">The table name used for tenant metadata records.</param>
    public TableTenantOperationalMetadataStore(string connectionString, string tableName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        tableClient = new TableClient(connectionString, tableName);
    }

    /// <inheritdoc/>
    public async Task<TenantOperationalMetadata?> GetAsync(string tenantId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        await EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var response = await tableClient.GetEntityAsync<TenantOperationalMetadataEntity>(
                TenantPartitionKey,
                tenantId,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return response.Value.ToModel();
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task UpsertAsync(TenantOperationalMetadata metadata, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(metadata);

        await EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

        var entity = TenantOperationalMetadataEntity.FromModel(metadata);
        await tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureCreatedAsync(CancellationToken cancellationToken)
    {
        await tableClient.CreateIfNotExistsAsync(cancellationToken).ConfigureAwait(false);
    }

    private sealed class TenantOperationalMetadataEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = TenantPartitionKey;

        public string RowKey { get; set; } = string.Empty;

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }

        public string ConsentStatus { get; set; } = ConsentResolutionStatus.Unknown.ToString();

        public string? ConfigurationState { get; set; }

        public DateTimeOffset? LastConsentCheckUtc { get; set; }

        public string? LastSupportDiagnosticCode { get; set; }

        public DateTimeOffset LastUpdatedUtc { get; set; }

        public static TenantOperationalMetadataEntity FromModel(TenantOperationalMetadata model)
        {
            ArgumentNullException.ThrowIfNull(model);
            return new TenantOperationalMetadataEntity
            {
                PartitionKey = TenantPartitionKey,
                RowKey = model.TenantId,
                ConsentStatus = model.ConsentStatus.ToString(),
                ConfigurationState = model.ConfigurationState,
                LastConsentCheckUtc = model.LastConsentCheckUtc,
                LastSupportDiagnosticCode = model.LastSupportDiagnosticCode,
                LastUpdatedUtc = model.LastUpdatedUtc,
            };
        }

        public TenantOperationalMetadata ToModel()
        {
            var consentStatus = Enum.TryParse<ConsentResolutionStatus>(ConsentStatus, true, out var parsedStatus)
                ? parsedStatus
                : ConsentResolutionStatus.Unknown;

            return new TenantOperationalMetadata(
                RowKey,
                consentStatus,
                ConfigurationState,
                LastConsentCheckUtc,
                LastSupportDiagnosticCode,
                LastUpdatedUtc == default ? DateTimeOffset.UtcNow : LastUpdatedUtc);
        }
    }
}
