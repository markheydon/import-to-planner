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
        DeploymentModeConfiguration deploymentModeConfiguration,
        TenantContext? tenantContext,
        ConsentResolutionStatus consentStatus,
        string? failureCategory)
    {
        ArgumentNullException.ThrowIfNull(deploymentModeConfiguration);

        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["deployment.mode"] = deploymentModeConfiguration.Mode.ToString(),
            ["tenant.key"] = tenantContext?.TenantKey ?? "none",
            ["consent.status"] = consentStatus.ToString(),
            ["planner.gateway.mode"] = deploymentModeConfiguration.UseGraphGateway ? "Graph" : "InMemory",
            ["failure.category"] = string.IsNullOrWhiteSpace(failureCategory) ? "None" : failureCategory,
        };
    }
}
