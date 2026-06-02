using ImportToPlanner.Application.TenantContext.Models;

namespace ImportToPlanner.Web.Features.CommercialAccounts.Backend;

internal sealed record SessionIdentityContext(
    string TenantId,
    string UserId,
    string? EmailAddress,
    string? TenantName);

internal sealed record CommercialAccount(
    string TenantId,
    string UserId,
    DateTimeOffset CreatedUtc,
    CommercialAccountStatus Status,
    DateTimeOffset? DeletedUtc,
    DateTimeOffset? RetentionExpiresUtc,
    DateTimeOffset? RestoredUtc,
    DateTimeOffset? LastSignInOutcomeUtc);

internal enum CommercialAccountStatus
{
    Active,
    Deleted,
}

internal sealed record CommercialAccessDecision(
    CommercialAccessDecisionType Decision,
    CommercialAccountStatus? AccountStatus,
    DateTimeOffset? RetentionExpiresUtc,
    bool ShouldSignOut);

internal enum CommercialAccessDecisionType
{
    Allow,
    CreateAccount,
    BlockedDeleted,
    OfferRestore,
    SelfHostedBypass,
}

internal enum CommercialAccountRestoreResult
{
    Restored,
    AccountNotFound,
    AccountNotDeleted,
    RetentionExpired,
}

internal sealed record ResolveCommercialAccessRequest(
    SessionIdentityContext SessionIdentity,
    bool CommercialModeEnabled,
    DateTimeOffset OccurredUtc);

internal sealed record GetCommercialProfileRequest(SessionIdentityContext SessionIdentity);

internal sealed record DeleteCommercialAccountRequest(
    SessionIdentityContext SessionIdentity,
    DateTimeOffset OccurredUtc);

internal sealed record RestoreCommercialAccountRequest(
    SessionIdentityContext SessionIdentity,
    DateTimeOffset OccurredUtc);

internal sealed record PurgeExpiredCommercialDataRequest(
    DateTimeOffset AsOfUtc,
    int BatchSize);

internal sealed record GetTenantOperationalMetadataRequest(string TenantId);

internal sealed record UpsertTenantOperationalMetadataRequest(TenantOperationalMetadata Metadata);
