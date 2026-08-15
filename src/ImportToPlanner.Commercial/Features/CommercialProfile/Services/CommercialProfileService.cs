using ImportToPlanner.Commercial.Common.Models;
using ImportToPlanner.Commercial.Features.CommercialAccess.Models;
using ImportToPlanner.Commercial.Features.CommercialAccess.Services;
using ImportToPlanner.Commercial.Features.CommercialProfile.Models;

namespace ImportToPlanner.Commercial.Features.CommercialProfile.Services;

/// <summary>
/// Handles commercial profile and lifecycle operations.
/// </summary>
public sealed class CommercialProfileService(
    ICommercialAccountsService commercialAccountsService,
    ICommercialAuditService commercialAuditService)
{
    private const string AccountDeletedOutcomeCode = "account_deleted";
    private const string AccountRestoredOutcomeCode = "account_restored";

    /// <summary>
    /// Gets the commercial account for the signed-in session.
    /// </summary>
    /// <param name="sessionIdentity">The identity context for the current session.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The account for the session if one exists; otherwise, <see langword="null" />.</returns>
    public Task<CommercialAccount?> GetProfileAsync(SessionIdentityContext sessionIdentity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sessionIdentity);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionIdentity.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionIdentity.UserId);

        return commercialAccountsService.GetAsync(sessionIdentity.TenantId, sessionIdentity.UserId, cancellationToken);
    }

    /// <summary>
    /// Marks the account for the signed-in session as deleted and records an audit event.
    /// </summary>
    /// <param name="sessionIdentity">The identity context for the current session.</param>
    /// <param name="occurredUtc">The UTC time the deletion was requested.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DeleteAccountAsync(
        SessionIdentityContext sessionIdentity,
        DateTimeOffset occurredUtc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sessionIdentity);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionIdentity.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionIdentity.UserId);

        var retentionExpiresUtc = occurredUtc.AddMonths(6);
        await commercialAccountsService
            .MarkDeletedAsync(
                sessionIdentity.TenantId,
                sessionIdentity.UserId,
                occurredUtc,
                retentionExpiresUtc,
                cancellationToken)
            .ConfigureAwait(false);

        await commercialAuditService
            .AppendAsync(
                new AccountAuditEvent(
                    sessionIdentity.TenantId,
                    sessionIdentity.UserId,
                    occurredUtc,
                    AccountAuditEventType.AccountDeleted,
                    AccountDeletedOutcomeCode,
                    occurredUtc.AddMonths(12)),
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Restores a deleted account for the signed-in session when it is still within its retention window.
    /// </summary>
    /// <param name="sessionIdentity">The identity context for the current session.</param>
    /// <param name="occurredUtc">The UTC time the restore was requested.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The outcome of the restore attempt.</returns>
    public async Task<CommercialAccountRestoreResult> RestoreAccountAsync(
        SessionIdentityContext sessionIdentity,
        DateTimeOffset occurredUtc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sessionIdentity);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionIdentity.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionIdentity.UserId);

        var account = await commercialAccountsService
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

        await commercialAccountsService
            .RestoreAsync(sessionIdentity.TenantId, sessionIdentity.UserId, occurredUtc, cancellationToken)
            .ConfigureAwait(false);

        await commercialAuditService
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

    /// <summary>
    /// Permanently removes deleted accounts and audit events whose retention period has expired.
    /// </summary>
    /// <param name="asOfUtc">The UTC time used to determine expiry.</param>
    /// <param name="batchSize">The maximum number of accounts to purge in this sweep.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The number of accounts purged.</returns>
    public async Task<int> PurgeExpiredAsync(DateTimeOffset asOfUtc, int batchSize, CancellationToken cancellationToken)
    {
        var effectiveBatchSize = Math.Max(0, batchSize);
        if (effectiveBatchSize == 0)
        {
            return 0;
        }

        var expiredAccounts = await commercialAccountsService
            .ListExpiredDeletedAsync(asOfUtc, effectiveBatchSize, cancellationToken)
            .ConfigureAwait(false);

        foreach (var account in expiredAccounts)
        {
            await commercialAccountsService
                .PurgeAsync(account.TenantId, account.UserId, cancellationToken)
                .ConfigureAwait(false);
        }

        await commercialAuditService
            .PurgeExpiredAsync(asOfUtc, effectiveBatchSize, cancellationToken)
            .ConfigureAwait(false);

        return expiredAccounts.Count;
    }
}
