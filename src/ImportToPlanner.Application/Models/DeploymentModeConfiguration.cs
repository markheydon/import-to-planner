namespace ImportToPlanner.Application.Models;

/// <summary>
/// Identifies the active deployment mode.
/// </summary>
public enum DeploymentMode
{
    SelfHostedSingleTenant,
    HostedSharedMultiTenant,
}

/// <summary>
/// Defines the active deployment-mode settings used by the app.
/// </summary>
/// <param name="Mode">The selected deployment mode.</param>
/// <param name="AuthorityTenant">The configured authority tenant identifier.</param>
/// <param name="UseGraphGateway">Indicates whether Graph-backed planner operations are enabled.</param>
/// <param name="HostedStorageEnabled">Indicates whether hosted storage integrations are configured.</param>
/// <param name="InitialReplicaPolicy">A rollout policy label for hosted replicas.</param>
/// <param name="RequiredScopes">The delegated scopes required for Graph access.</param>
/// <param name="AdminConsentUri">The optional administrator consent URI.</param>
public sealed record DeploymentModeConfiguration(
    DeploymentMode Mode,
    string AuthorityTenant,
    bool UseGraphGateway,
    bool HostedStorageEnabled,
    string InitialReplicaPolicy,
    IReadOnlyList<string> RequiredScopes,
    Uri? AdminConsentUri)
{
    /// <summary>
    /// Creates a deployment-mode configuration with safe defaults.
    /// </summary>
    public static DeploymentModeConfiguration Default { get; } = new(
        DeploymentMode.SelfHostedSingleTenant,
        "common",
        false,
        false,
        "SingleActiveReplica",
        [],
        null);
}
