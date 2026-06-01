using ImportToPlanner.Application.TenantContext.Abstractions;
using ImportToPlanner.Application.TenantContext.Models;

namespace ImportToPlanner.Web.Features.CommercialAccounts.Backend;

internal sealed class BackendTenantOperationalMetadataStore(CommercialApiServiceClient commercialApiServiceClient) : ITenantOperationalMetadataStore
{
    public Task<TenantOperationalMetadata?> GetAsync(string tenantId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        return commercialApiServiceClient.GetTenantOperationalMetadataAsync(
            new GetTenantOperationalMetadataRequest(tenantId),
            cancellationToken);
    }

    public Task UpsertAsync(TenantOperationalMetadata metadata, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        return commercialApiServiceClient.UpsertTenantOperationalMetadataAsync(
            new UpsertTenantOperationalMetadataRequest(metadata),
            cancellationToken);
    }
}
