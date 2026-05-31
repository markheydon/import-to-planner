using System.Collections.Concurrent;
using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Models;

namespace ImportToPlanner.Infrastructure.Graph.TenantMetadata;

/// <summary>
/// Provides an in-memory tenant metadata store for self-hosted single-app deployments.
/// </summary>
internal sealed class SelfHostTenantOperationalMetadataStore : ITenantOperationalMetadataStore
{
    private readonly ConcurrentDictionary<string, TenantOperationalMetadata> store =
        new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public Task<TenantOperationalMetadata?> GetAsync(string tenantId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        store.TryGetValue(tenantId, out var metadata);
        return Task.FromResult(metadata);
    }

    /// <inheritdoc/>
    public Task UpsertAsync(TenantOperationalMetadata metadata, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(metadata);
        store[metadata.TenantId] = metadata;
        return Task.CompletedTask;
    }
}
