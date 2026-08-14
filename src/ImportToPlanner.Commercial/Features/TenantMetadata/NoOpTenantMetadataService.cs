using ImportToPlanner.Application.TenantContext.Models;
using ImportToPlanner.Commercial.Features.TenantMetadata.Services;

namespace ImportToPlanner.Commercial.Features.TenantMetadata;

internal sealed class NoOpTenantMetadataService : ITenantMetadataService
{
    public Task<TenantOperationalMetadata?> GetAsync(string tenantId, CancellationToken cancellationToken)
        => Task.FromResult<TenantOperationalMetadata?>(null);

    public Task UpsertAsync(TenantOperationalMetadata metadata, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
