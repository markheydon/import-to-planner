using ImportToPlanner.Commercial.Models;

namespace ImportToPlanner.Commercial.Abstractions;

/// <summary>
/// Persists per-tenant Stripe billing metadata separate from the credit ledger and commercial account row.
/// </summary>
public interface ITenantBillingStore
{
    /// <summary>
    /// Loads billing metadata for the tenant, or <see langword="null"/> when absent.
    /// </summary>
    Task<TenantBillingMetadata?> GetAsync(string tenantId, CancellationToken cancellationToken);

    /// <summary>
    /// Creates or replaces billing metadata for the tenant.
    /// </summary>
    Task UpsertAsync(TenantBillingMetadata metadata, CancellationToken cancellationToken);
}
