using ImportToPlanner.Application.TenantContext.Models;

namespace ImportToPlanner.CommercialService.TenantMetadata.Models;

/// <summary>
/// Gets tenant operational metadata for a tenant identifier.
/// </summary>
/// <param name="TenantId">The tenant identifier.</param>
public sealed record GetTenantOperationalMetadataRequest(string TenantId);

/// <summary>
/// Upserts tenant operational metadata.
/// </summary>
/// <param name="Metadata">The metadata to persist.</param>
public sealed record UpsertTenantOperationalMetadataRequest(TenantOperationalMetadata Metadata);
