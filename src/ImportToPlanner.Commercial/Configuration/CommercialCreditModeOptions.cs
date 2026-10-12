namespace ImportToPlanner.Commercial.Configuration;

/// <summary>
/// Commercial credit mode configuration owned by the Commercial layer.
/// </summary>
public sealed class CommercialCreditModeOptions
{
    /// <summary>
    /// Configuration section name shared with hosted commercial feature flags.
    /// </summary>
    public const string ConfigurationSectionName = "Features:CommercialMode";

    /// <summary>
    /// Gets or sets comma-separated Entra tenant GUIDs with complimentary unlimited credits.
    /// </summary>
    public string ComplimentaryTenantIds { get; set; } = string.Empty;
}
