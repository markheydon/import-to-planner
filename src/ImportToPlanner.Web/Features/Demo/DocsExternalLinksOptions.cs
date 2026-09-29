namespace ImportToPlanner.Web.Features.Demo;

/// <summary>
/// Configurable base URL for public documentation and legal pages.
/// </summary>
public sealed class DocsExternalLinksOptions
{
    /// <summary>Configuration section name.</summary>
    public const string ConfigurationSectionName = "DocsExternalLinks";

    /// <summary>Gets or sets the documentation site base URL.</summary>
    public string DocsBaseUrl { get; set; } = "https://docs.importplanner.app";
}
