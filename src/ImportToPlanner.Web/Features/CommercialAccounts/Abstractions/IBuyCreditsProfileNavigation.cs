namespace ImportToPlanner.Web.Features.CommercialAccounts.Abstractions;

/// <summary>
/// Optional navigation hook for buy-credits checkout when paid purchases are enabled.
/// </summary>
public interface IBuyCreditsProfileNavigation
{
    /// <summary>
    /// Relative URL for the buy-credits flow.
    /// </summary>
    string BuyCreditsRelativeUrl { get; }
}
