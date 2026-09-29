namespace ImportToPlanner.Web.Features.CommercialAccounts;

/// <summary>
/// Represents configuration for the commercial account access flow.
/// </summary>
public sealed class CommercialModeOptions
{
    /// <summary>
    /// The configuration section name for commercial mode settings.
    /// </summary>
    public const string ConfigurationSectionName = "Features:CommercialMode";

    /// <summary>
    /// Gets or sets a value indicating whether commercial mode is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether scheduled retention sweeps are enabled.
    /// </summary>
    public bool RetentionSweepEnabled { get; set; }

    /// <summary>
    /// Reads commercial mode settings from application configuration at startup.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The bound commercial mode options.</returns>
    public static CommercialModeOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return configuration.GetSection(ConfigurationSectionName).Get<CommercialModeOptions>() ?? new CommercialModeOptions();
    }
}
