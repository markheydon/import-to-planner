using ImportToPlanner.CommercialService.Common.Models;
using ImportToPlanner.CommercialService.Features.CommercialAccess.Models;
using ImportToPlanner.CommercialService.Features.CommercialAccess.Services;
using ImportToPlanner.CommercialService.Features.CommercialProfile.Models;

namespace ImportToPlanner.CommercialService.Features.CommercialProfile.Services;

/// <summary>
/// Handles commercial profile and lifecycle operations.
/// </summary>
public sealed class CommercialProfileService(
    CommercialAccountsService commercialAccountsService,
    CommercialAuditService commercialAuditService)
{
    private const string AccountDeletedOutcomeCode = "account_deleted";
    private const string AccountRestoredOutcomeCode = "account_restored";

    public Task<CommercialAccount?> GetProfileAsync(SessionIdentityContext sessionIdentity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sessionIdentity);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionIdentity.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionIdentity.UserId);

        return commercialAccountsService.GetAsync(sessionIdentity.TenantId, sessionIdentity.UserId, cancellationToken);
    }

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
