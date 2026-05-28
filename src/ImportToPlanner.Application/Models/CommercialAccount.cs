namespace ImportToPlanner.Application.Models;

/// <summary>
/// Represents a persisted commercial account keyed by tenant and user identifiers.
/// </summary>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="UserId">The user identifier.</param>
/// <param name="CreatedUtc">The UTC timestamp when the account was created.</param>
/// <param name="Status">The current account status.</param>
/// <param name="DeletedUtc">The UTC timestamp when the account was marked deleted.</param>
/// <param name="RetentionExpiresUtc">The UTC timestamp when deleted-account retention expires.</param>
/// <param name="RestoredUtc">The UTC timestamp when the account was restored.</param>
/// <param name="LastSignInOutcomeUtc">The UTC timestamp for the last sign-in outcome update.</param>
public sealed record CommercialAccount(
    string TenantId,
    string UserId,
    DateTimeOffset CreatedUtc,
    CommercialAccountStatus Status,
    DateTimeOffset? DeletedUtc,
    DateTimeOffset? RetentionExpiresUtc,
    DateTimeOffset? RestoredUtc,
    DateTimeOffset? LastSignInOutcomeUtc);

/// <summary>
/// Represents the persisted lifecycle status of a commercial account.
/// </summary>
public enum CommercialAccountStatus
{
    Active,
    Deleted,
}
