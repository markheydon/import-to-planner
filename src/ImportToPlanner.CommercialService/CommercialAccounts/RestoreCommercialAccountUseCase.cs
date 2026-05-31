using ImportToPlanner.CommercialService.Models;

namespace ImportToPlanner.CommercialService.CommercialAccounts;

/// <summary>
/// Restores a deleted commercial account when it is still within retention.
/// </summary>
public sealed class RestoreCommercialAccountUseCase(
    ICommercialAccountStore commercialAccountStore,
    ICommercialAuditStore commercialAuditStore)
{
    private const string AccountRestoredOutcomeCode = "account_restored";

    /// <summary>
    /// Restores the commercial account for the supplied session identity.
    /// </summary>
    /// <param name="sessionIdentity">The signed-in session identity context.</param>
    /// <param name="occurredUtc">The UTC timestamp when restoration was requested.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The restore outcome.</returns>
    public async Task<CommercialAccountRestoreResult> ExecuteAsync(
        SessionIdentityContext sessionIdentity,
        DateTimeOffset occurredUtc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sessionIdentity);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionIdentity.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionIdentity.UserId);

        var account = await commercialAccountStore
            .GetAsync(sessionIdentity.TenantId, sessionIdentity.UserId, cancellationToken)
            .ConfigureAwait(false);
        if (account is null)
        {
            return CommercialAccountRestoreResult.AccountNotFound;
        }

        if (account.Status != CommercialAccountStatus.Deleted)
        {
            return CommercialAccountRestoreResult.AccountNotDeleted;
        }

        if (account.RetentionExpiresUtc is null || account.RetentionExpiresUtc < occurredUtc)
        {
            return CommercialAccountRestoreResult.RetentionExpired;
        }

        await commercialAccountStore
            .RestoreAsync(sessionIdentity.TenantId, sessionIdentity.UserId, occurredUtc, cancellationToken)
            .ConfigureAwait(false);

        await commercialAuditStore
            .AppendAsync(
                new AccountAuditEvent(
                    sessionIdentity.TenantId,
                    sessionIdentity.UserId,
                    occurredUtc,
                    AccountAuditEventType.AccountRestored,
                    AccountRestoredOutcomeCode,
                    occurredUtc.AddMonths(12)),
                cancellationToken)
            .ConfigureAwait(false);

        return CommercialAccountRestoreResult.Restored;
    }
}
