namespace ImportToPlanner.CommercialService.Features.CommercialAccess.Models;

/// <summary>
/// Represents an access decision for the current commercial account session.
/// </summary>
/// <param name="Decision">The high-level access decision.</param>
/// <param name="AccountStatus">The resolved account status when available.</param>
/// <param name="RetentionExpiresUtc">The retention expiry timestamp for deleted-account flows.</param>
/// <param name="ShouldSignOut">Indicates whether the web layer should sign out the current session.</param>
public sealed record CommercialAccessDecision(
    CommercialAccessDecisionType Decision,
    CommercialAccountStatus? AccountStatus,
    DateTimeOffset? RetentionExpiresUtc,
    bool ShouldSignOut);

/// <summary>
/// Represents the possible access decisions for commercial account handling.
/// </summary>
public enum CommercialAccessDecisionType
{
    Allow,
    CreateAccount,
    BlockedDeleted,
    OfferRestore,
    SelfHostedBypass,
}
