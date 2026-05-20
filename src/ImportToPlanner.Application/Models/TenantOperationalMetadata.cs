namespace ImportToPlanner.Application.Models;

/// <summary>
/// Represents minimal tenant-scoped operational metadata retained for hosted operation.
/// </summary>
/// <param name="TenantId">The tenant identifier that owns this metadata.</param>
/// <param name="ConsentStatus">The latest consent status for the tenant.</param>
/// <param name="ConfigurationState">The optional tenant-scoped configuration state marker.</param>
/// <param name="LastConsentCheckUtc">The UTC timestamp for the last consent evaluation.</param>
/// <param name="LastSupportDiagnosticCode">An optional stable support diagnostic code.</param>
/// <param name="LastUpdatedUtc">The UTC timestamp when the record was last updated.</param>
public sealed record TenantOperationalMetadata(
    string TenantId,
    ConsentResolutionStatus ConsentStatus,
    string? ConfigurationState,
    DateTimeOffset? LastConsentCheckUtc,
    string? LastSupportDiagnosticCode,
    DateTimeOffset LastUpdatedUtc);
