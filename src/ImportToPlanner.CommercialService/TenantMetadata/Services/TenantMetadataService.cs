using Azure;
using Azure.Data.Tables;
using ImportToPlanner.Application.Consent.Models;
using ImportToPlanner.Application.TenantContext.Models;

namespace ImportToPlanner.CommercialService.TenantMetadata.Services;

/// <summary>
/// Handles tenant operational metadata stored in Azure Table Storage.
/// </summary>
public class TenantMetadataService
{
    private const string OperationalMetadataRowKey = "operational";
    private const string TableName = "TenantOperationalMetadata";
    private readonly TableClient? tableClient;

    public TenantMetadataService(TableServiceClient tableServiceClient)
        : this(CreateTableClient(tableServiceClient))
    {
    }

    internal TenantMetadataService(TableClient tableClient)
    {
        this.tableClient = tableClient ?? throw new ArgumentNullException(nameof(tableClient));
    }

    protected TenantMetadataService()
    {
    }

    public virtual async Task<TenantOperationalMetadata?> GetAsync(string tenantId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        await EnsureTableAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var response = await TableClient.GetEntityAsync<TenantOperationalMetadataEntity>(
                tenantId,
                OperationalMetadataRowKey,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return response.Value.ToModel();
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return null;
        }
    }

    public virtual async Task UpsertAsync(TenantOperationalMetadata metadata, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(metadata);

        await EnsureTableAsync(cancellationToken).ConfigureAwait(false);

        var entity = TenantOperationalMetadataEntity.FromModel(metadata);
        await TableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken).ConfigureAwait(false);
    }

    private TableClient TableClient => tableClient ?? throw new InvalidOperationException("The table client has not been initialised.");

    private async Task EnsureTableAsync(CancellationToken cancellationToken)
    {
        await TableClient.CreateIfNotExistsAsync(cancellationToken).ConfigureAwait(false);
    }

    private sealed class TenantOperationalMetadataEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = string.Empty;

        public string RowKey { get; set; } = OperationalMetadataRowKey;

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
                PartitionKey = model.TenantId,
                RowKey = OperationalMetadataRowKey,
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
                PartitionKey,
                consentStatus,
                ConfigurationState,
                LastConsentCheckUtc,
                LastSupportDiagnosticCode,
                LastUpdatedUtc == default ? DateTimeOffset.UtcNow : LastUpdatedUtc);
        }
    }

    private static TableClient CreateTableClient(TableServiceClient tableServiceClient)
    {
        ArgumentNullException.ThrowIfNull(tableServiceClient);
        return tableServiceClient.GetTableClient(TableName);
    }
}
