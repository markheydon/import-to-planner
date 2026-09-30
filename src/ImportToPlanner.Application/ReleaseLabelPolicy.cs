namespace ImportToPlanner.Application;

/// <summary>
/// Environment-specific guardrails for when a bare shipping release label is allowed (FR-005).
/// </summary>
public sealed class ReleaseLabelPolicy
{
    /// <summary>Configuration section name.</summary>
    public const string ConfigurationSectionName = "ReleaseLabelPolicy";

    /// <summary>
    /// Gets or sets whether non-tag builds may present an unqualified shipping release label.
    /// Defaults to <see langword="false"/> for local, CI, staging, and production.
    /// </summary>
    public bool AllowUnqualifiedShippingLabel { get; init; }

    /// <summary>
    /// Gets or sets an optional diagnostic environment name (for example Staging or Production).
    /// </summary>
    public string? EnvironmentName { get; init; }
}
