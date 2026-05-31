namespace ImportToPlanner.Application.Models;

/// <summary>
/// Resolves hosted commercial access for the supplied session identity.
/// </summary>
/// <param name="SessionIdentity">The signed-in session identity.</param>
/// <param name="CommercialModeEnabled">Whether hosted commercial mode is enabled for this request.</param>
/// <param name="OccurredUtc">The operation timestamp in UTC.</param>
public sealed record ResolveCommercialAccessRequest(
    SessionIdentityContext SessionIdentity,
    bool CommercialModeEnabled,
    DateTimeOffset OccurredUtc);

/// <summary>
/// Gets the hosted commercial account profile for the supplied session identity.
/// </summary>
/// <param name="SessionIdentity">The signed-in session identity.</param>
public sealed record GetCommercialProfileRequest(SessionIdentityContext SessionIdentity);

/// <summary>
/// Soft-deletes the hosted commercial account for the supplied session identity.
/// </summary>
/// <param name="SessionIdentity">The signed-in session identity.</param>
/// <param name="OccurredUtc">The operation timestamp in UTC.</param>
public sealed record DeleteCommercialAccountRequest(
    SessionIdentityContext SessionIdentity,
    DateTimeOffset OccurredUtc);

/// <summary>
/// Restores a hosted commercial account during retention for the supplied session identity.
/// </summary>
/// <param name="SessionIdentity">The signed-in session identity.</param>
/// <param name="OccurredUtc">The operation timestamp in UTC.</param>
public sealed record RestoreCommercialAccountRequest(
    SessionIdentityContext SessionIdentity,
    DateTimeOffset OccurredUtc);

/// <summary>
/// Purges expired hosted commercial account and audit data.
/// </summary>
/// <param name="AsOfUtc">The UTC cutoff for purge evaluation.</param>
/// <param name="BatchSize">The maximum records to purge in this invocation.</param>
public sealed record PurgeExpiredCommercialDataRequest(
    DateTimeOffset AsOfUtc,
    int BatchSize);

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