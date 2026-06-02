using ImportToPlanner.CommercialService.Common.Models;
using ImportToPlanner.CommercialService.Features.CommercialAccess.Models;

namespace ImportToPlanner.CommercialService.Features.CommercialAccess.Services;

/// <summary>
/// Resolves commercial access decisions for authenticated sessions.
/// </summary>
public sealed class CommercialAccessService(
    ICommercialAccountsService commercialAccountsService,
    ICommercialAuditService commercialAuditService)
{
    private const string AccountCreatedOutcomeCode = "account_created";
    private const string SignInAllowedOutcomeCode = "sign_in_allowed";
    private const string SignInBlockedDeletedOutcomeCode = "sign_in_blocked_deleted";

    public async Task<CommercialAccessDecision> ResolveAccessAsync(
        SessionIdentityContext sessionIdentity,
        bool commercialModeEnabled,
        DateTimeOffset occurredUtc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sessionIdentity);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionIdentity.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionIdentity.UserId);

        cancellationToken.ThrowIfCancellationRequested();

        if (!commercialModeEnabled)
        {
            return new CommercialAccessDecision(
                CommercialAccessDecisionType.SelfHostedBypass,
                null,
                null,
                ShouldSignOut: false);
        }

        var existingAccount = await commercialAccountsService
            .GetAsync(sessionIdentity.TenantId, sessionIdentity.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (existingAccount is null)
        {
            var account = new CommercialAccount(
                sessionIdentity.TenantId,
                sessionIdentity.UserId,
                occurredUtc,
                CommercialAccountStatus.Active,
                DeletedUtc: null,
                RetentionExpiresUtc: null,
                RestoredUtc: null,
                LastSignInOutcomeUtc: occurredUtc);

            await commercialAccountsService.CreateAsync(account, cancellationToken).ConfigureAwait(false);
            await AppendAuditAsync(sessionIdentity.TenantId, sessionIdentity.UserId, occurredUtc, AccountAuditEventType.AccountCreated, AccountCreatedOutcomeCode, cancellationToken).ConfigureAwait(false);
            await AppendAuditAsync(sessionIdentity.TenantId, sessionIdentity.UserId, occurredUtc, AccountAuditEventType.SignInOutcome, SignInAllowedOutcomeCode, cancellationToken).ConfigureAwait(false);

            return new CommercialAccessDecision(
                CommercialAccessDecisionType.CreateAccount,
                CommercialAccountStatus.Active,
                null,
                ShouldSignOut: false);
        }

        if (existingAccount.Status == CommercialAccountStatus.Deleted)
        {
            await AppendAuditAsync(sessionIdentity.TenantId, sessionIdentity.UserId, occurredUtc, AccountAuditEventType.SignInOutcome, SignInBlockedDeletedOutcomeCode, cancellationToken).ConfigureAwait(false);

            return new CommercialAccessDecision(
                CommercialAccessDecisionType.OfferRestore,
                CommercialAccountStatus.Deleted,
                existingAccount.RetentionExpiresUtc,
                ShouldSignOut: false);
        }

        await AppendAuditAsync(sessionIdentity.TenantId, sessionIdentity.UserId, occurredUtc, AccountAuditEventType.SignInOutcome, SignInAllowedOutcomeCode, cancellationToken).ConfigureAwait(false);

        return new CommercialAccessDecision(
            CommercialAccessDecisionType.Allow,
            CommercialAccountStatus.Active,
            null,
            ShouldSignOut: false);
    }

    private Task AppendAuditAsync(
        string tenantId,
        string userId,
        DateTimeOffset occurredUtc,
        AccountAuditEventType eventType,
        string outcome,
        CancellationToken cancellationToken)
    {
        var auditEvent = new AccountAuditEvent(
            tenantId,
            userId,
            occurredUtc,
            eventType,
            outcome,
            occurredUtc.AddMonths(12));

        return commercialAuditService.AppendAsync(auditEvent, cancellationToken);
    }
}
