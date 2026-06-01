using ImportToPlanner.Application.TenantContext.Models;

namespace ImportToPlanner.CommercialService.Features.TenantMetadata.Services;

/// <summary>
/// Defines operations for managing tenant operational metadata.
/// </summary>
public interface ITenantMetadataService
{
    /// <summary>
    /// Retrieves tenant operational metadata for the specified tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The tenant operational metadata if present; otherwise, null.</returns>
    Task<TenantOperationalMetadata?> GetAsync(string tenantId, CancellationToken cancellationToken);

    /// <summary>
    /// Upserts tenant operational metadata.
    /// </summary>
    /// <param name="metadata">The metadata to upsert.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task UpsertAsync(TenantOperationalMetadata metadata, CancellationToken cancellationToken);
}
