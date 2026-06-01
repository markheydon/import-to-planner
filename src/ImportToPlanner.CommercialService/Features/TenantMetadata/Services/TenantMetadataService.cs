using Azure.Data.Tables;
using ImportToPlanner.Application.Consent.Models;
using ImportToPlanner.Application.TenantContext.Models;
using ImportToPlanner.CommercialService.Common.Storage;

namespace ImportToPlanner.CommercialService.Features.TenantMetadata.Services;

/// <summary>
/// Handles tenant operational metadata stored in Azure Table Storage.
/// </summary>
public class TenantMetadataService
    : TableRepositoryBase<TenantOperationalMetadata>, ITenantMetadataService
{
    private const string OperationalMetadataRowKey = "operational";
    private const string TableName = "TenantOperationalMetadata";

    /// <summary>
    /// Initialises a new instance of the <see cref="TenantMetadataService"/> class with the specified Azure Table service client.
    /// </summary>
    /// <param name="tableServiceClient">The Azure Table service client.</param>
    public TenantMetadataService(TableServiceClient tableServiceClient)
        : base(tableServiceClient, TableName)
    {
    }

    /// <summary>
    /// Initialises a new instance of the <see cref="TenantMetadataService"/> class with a table client.
    /// This constructor is primarily intended for tests.
    /// </summary>
    /// <param name="tableClient">The table client.</param>
    protected TenantMetadataService(TableClient tableClient)
        : base(tableClient)
    {
    }

    /// <summary>
    /// Retrieves tenant operational metadata for the specified tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The tenant operational metadata if present; otherwise, null.</returns>
    public virtual Task<TenantOperationalMetadata?> GetAsync(string tenantId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        return GetByKeysAsync(tenantId, OperationalMetadataRowKey, cancellationToken);
    }

    /// <summary>
    /// Upserts tenant operational metadata.
    /// </summary>
    /// <param name="metadata">The metadata to upsert.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual Task UpsertAsync(TenantOperationalMetadata metadata, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(metadata);

        return UpsertReplaceAsync(metadata, cancellationToken);
    }

    /// <summary>
    /// Converts a <see cref="TenantOperationalMetadata"/> model to an Azure Table <see cref="TableEntity"/> for storage.
    /// </summary>
    /// <param name="model">The tenant metadata model to convert.</param>
    /// <returns>A <see cref="TableEntity"/> representing the tenant metadata.</returns>
    protected override TableEntity ToEntity(TenantOperationalMetadata model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return new TableEntity(model.TenantId, OperationalMetadataRowKey)
        {
            [nameof(TenantOperationalMetadata.ConsentStatus)] = model.ConsentStatus.ToString(),
            [nameof(TenantOperationalMetadata.ConfigurationState)] = model.ConfigurationState,
            [nameof(TenantOperationalMetadata.LastConsentCheckUtc)] = model.LastConsentCheckUtc,
            [nameof(TenantOperationalMetadata.LastSupportDiagnosticCode)] = model.LastSupportDiagnosticCode,
            [nameof(TenantOperationalMetadata.LastUpdatedUtc)] = model.LastUpdatedUtc,
        };
    }

    /// <summary>
    /// Converts an Azure Table <see cref="TableEntity"/> to a <see cref="TenantOperationalMetadata"/> model.
    /// </summary>
    /// <param name="entity">The table entity to convert.</param>
    /// <returns>A <see cref="TenantOperationalMetadata"/> model representing the table entity.</returns>
    protected override TenantOperationalMetadata ToModel(TableEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var consentStatusValue = entity.GetString(nameof(TenantOperationalMetadata.ConsentStatus));
        var consentStatus = Enum.TryParse<ConsentResolutionStatus>(consentStatusValue, true, out var parsedStatus)
            ? parsedStatus
            : ConsentResolutionStatus.Unknown;

        return new TenantOperationalMetadata(
            entity.PartitionKey,
            consentStatus,
            entity.GetString(nameof(TenantOperationalMetadata.ConfigurationState)),
            entity.GetDateTimeOffset(nameof(TenantOperationalMetadata.LastConsentCheckUtc)),
            entity.GetString(nameof(TenantOperationalMetadata.LastSupportDiagnosticCode)),
            entity.GetDateTimeOffset(nameof(TenantOperationalMetadata.LastUpdatedUtc)) ?? DateTimeOffset.UtcNow);
    }
}
