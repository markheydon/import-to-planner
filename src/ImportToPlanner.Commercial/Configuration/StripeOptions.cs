namespace ImportToPlanner.Commercial.Configuration;

/// <summary>
/// Stripe-related configuration bound for commercial credit checkout. Values are consumed by the web host and payment adapter; Commercial uses pack metadata only.
/// </summary>
public sealed class StripeOptions
{
    /// <summary>
    /// Configuration section name for Stripe settings.
    /// </summary>
    public const string ConfigurationSectionName = "Stripe";

    /// <summary>
    /// Gets or sets the Stripe API key (restricted sandbox key in non-production environments).
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the webhook signing secret used to verify event payloads.
    /// </summary>
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether Checkout should enable automatic tax. Leave false unless the Stripe account has an active tax registration.
    /// </summary>
    public bool AutomaticTaxEnabled { get; set; }

    /// <summary>
    /// Gets or sets the configured credit pack catalogue.
    /// </summary>
    public List<CreditPackOptions> CreditPacks { get; set; } = [];
}
