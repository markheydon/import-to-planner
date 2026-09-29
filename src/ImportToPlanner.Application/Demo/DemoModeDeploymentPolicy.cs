namespace ImportToPlanner.Application.Demo;

/// <summary>
/// Deployment configuration for operator-gated demonstration mode.
/// </summary>
public sealed class DemoModeDeploymentPolicy
{
    /// <summary>Configuration section name.</summary>
    public const string ConfigurationSectionName = "DemoMode";

    /// <summary>Entra object IDs and/or normalised UPNs permitted to use demo controls.</summary>
    public IReadOnlyList<string> OperatorAllowlist { get; init; } = [];

    /// <summary>When false, demo controls are hidden for all users.</summary>
    public bool DemoControlsEnabled { get; init; }
}
