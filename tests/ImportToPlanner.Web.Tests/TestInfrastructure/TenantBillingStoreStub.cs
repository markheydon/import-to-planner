
namespace ImportToPlanner.Web.Tests.TestInfrastructure;

internal sealed class TenantBillingStoreStub : ITenantBillingStore
{
    private readonly Dictionary<string, TenantBillingMetadata> metadata = new(StringComparer.OrdinalIgnoreCase);

    public Task<TenantBillingMetadata?> GetAsync(string tenantId, CancellationToken cancellationToken)
        => Task.FromResult(metadata.TryGetValue(tenantId, out var value) ? value : null);

    public Task UpsertAsync(TenantBillingMetadata metadata, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        this.metadata[metadata.TenantId] = metadata;
        return Task.CompletedTask;
    }
}
