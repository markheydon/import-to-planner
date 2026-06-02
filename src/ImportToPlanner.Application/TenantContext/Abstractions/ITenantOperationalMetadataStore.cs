using ImportToPlanner.Application.TenantContext.Models;

namespace ImportToPlanner.Application.TenantContext.Abstractions;

/// <summary>
/// Defines storage operations for tenant-scoped operational metadata.
/// </summary>
public interface ITenantOperationalMetadataStore
{
    /// <summary>
    /// Gets tenant metadata for the specified tenant identifier.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The metadata record when available; otherwise <see langword="null"/>.</returns>
    Task<TenantOperationalMetadata?> GetAsync(string tenantId, CancellationToken cancellationToken);

    /// <summary>
    /// Creates or updates a tenant metadata record.
    /// </summary>
    /// <param name="metadata">The metadata payload.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task UpsertAsync(TenantOperationalMetadata metadata, CancellationToken cancellationToken);
}
