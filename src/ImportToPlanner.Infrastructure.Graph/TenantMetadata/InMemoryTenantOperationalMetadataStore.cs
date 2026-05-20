using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Models;
using System.Collections.Concurrent;

namespace ImportToPlanner.Infrastructure.Graph.TenantMetadata;

/// <summary>
/// Provides an in-memory tenant metadata store used when hosted storage is not configured.
/// </summary>
public sealed class InMemoryTenantOperationalMetadataStore : ITenantOperationalMetadataStore
{
    private readonly ConcurrentDictionary<string, TenantOperationalMetadata> records =
        new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public Task<TenantOperationalMetadata?> GetAsync(string tenantId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        records.TryGetValue(tenantId, out var value);
        return Task.FromResult(value);
    }

    /// <inheritdoc/>
    public Task UpsertAsync(TenantOperationalMetadata metadata, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(metadata);

        records[metadata.TenantId] = metadata;
        return Task.CompletedTask;
    }
}
