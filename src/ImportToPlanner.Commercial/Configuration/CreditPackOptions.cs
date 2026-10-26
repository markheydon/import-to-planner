namespace ImportToPlanner.Commercial.Configuration;

/// <summary>
/// One credit pack in the hosted checkout catalogue (configuration only, not a table entity).
/// </summary>
public sealed class CreditPackOptions
{
    /// <summary>
    /// Gets or sets the stable pack slug. Required and unique among published packs.
    /// </summary>
    public string PackId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of credits granted to the ledger when payment is confirmed.
    /// </summary>
    public int CreditQuantity { get; set; }

    /// <summary>
    /// Gets or sets the payment provider Price identifier (for example <c>price_...</c>). Required when published.
    /// </summary>
    public string StripePriceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UK English label shown on preview and profile.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the pack price in minor currency units for display. Must match the provider Price.
    /// </summary>
    public int AmountMinor { get; set; }

    /// <summary>
    /// Gets or sets the ISO currency code for display (for example <c>gbp</c>).
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the pack is offered at checkout. Unpublished packs are hidden and rejected.
    /// </summary>
    public bool Published { get; set; }
}
