using ImportToPlanner.Application.Models;

namespace ImportToPlanner.Web;

/// <summary>
/// Builds privacy-safe hosted telemetry dimensions for logging and tracing scopes.
/// </summary>
internal static class HostedTelemetryHelper
{
    /// <summary>
    /// Builds privacy-safe hosted telemetry dimensions for logging and tracing scopes.
    /// </summary>
    internal static IReadOnlyDictionary<string, string> BuildHostedTelemetryDimensions(
        TenantAuthorityConfiguration tenantAuthorityConfiguration,
        TenantContext? tenantContext,
        ConsentResolutionStatus consentStatus,
        string? failureCategory)
    {
        ArgumentNullException.ThrowIfNull(tenantAuthorityConfiguration);

        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["authority.kind"] = tenantAuthorityConfiguration.AuthorityKind.ToString(),
            ["tenant.key"] = tenantContext?.TenantKey ?? "none",
            ["consent.status"] = consentStatus.ToString(),
            ["failure.category"] = string.IsNullOrWhiteSpace(failureCategory) ? "None" : failureCategory,
        };
    }
}
