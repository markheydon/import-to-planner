using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Models;

namespace ImportToPlanner.Application.Services;

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
    public async Task ExecuteAsync(
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
        if (account is null
            || account.Status != CommercialAccountStatus.Deleted
            || account.RetentionExpiresUtc is null
            || account.RetentionExpiresUtc < occurredUtc)
        {
            return;
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
    }
}
