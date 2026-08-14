using ImportToPlanner.Application.TenantContext.Abstractions;
using ImportToPlanner.Application.TenantContext.Models;
using ImportToPlanner.Commercial.Features.TenantMetadata.Services;

namespace ImportToPlanner.Commercial.Features.TenantMetadata;

/// <summary>
/// Adapts commercial tenant metadata persistence to the application metadata store contract.
/// </summary>
internal sealed class CommercialTenantOperationalMetadataStore(ITenantMetadataService tenantMetadataService)
    : ITenantOperationalMetadataStore
{
    public Task<TenantOperationalMetadata?> GetAsync(string tenantId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        return tenantMetadataService.GetAsync(tenantId, cancellationToken);
    }

    public Task UpsertAsync(TenantOperationalMetadata metadata, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        return tenantMetadataService.UpsertAsync(metadata, cancellationToken);
    }
}
