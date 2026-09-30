namespace ImportToPlanner.Application.Models;

/// <summary>
/// Aggregate release and optional build metadata returned for About rendering.
/// </summary>
/// <param name="ProductName">The fixed product name (for example Import To Planner).</param>
/// <param name="ReleaseLabel">The deployment release label shown to signed-in users.</param>
/// <param name="BuildMetadata">Optional build diagnostics; omit About rows when fields are unknown.</param>
/// <param name="DeploymentEnvironmentName">Optional environment label from release policy (for example Staging).</param>
public sealed record ReleaseInformation(
    string ProductName,
    DeploymentReleaseLabel ReleaseLabel,
    BuildMetadata? BuildMetadata,
    string? DeploymentEnvironmentName = null);
