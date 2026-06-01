using ImportToPlanner.CommercialService.CommercialAccounts.Abstractions;
using ImportToPlanner.CommercialService.CommercialAccounts.Models;

namespace ImportToPlanner.CommercialService.CommercialAccounts.Services;

/// <summary>
/// Marks a commercial account as deleted and records a lifecycle audit event.
/// </summary>
public sealed class DeleteCommercialAccountUseCase(
    ICommercialAccountStore commercialAccountStore,
    ICommercialAuditStore commercialAuditStore)
{
    private const string AccountDeletedOutcomeCode = "account_deleted";

    /// <summary>
    /// Deletes the commercial account for the supplied session identity.
    /// </summary>
    /// <param name="sessionIdentity">The signed-in session identity context.</param>
    /// <param name="occurredUtc">The UTC timestamp when deletion was requested.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task ExecuteAsync(
        SessionIdentityContext sessionIdentity,
        DateTimeOffset occurredUtc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sessionIdentity);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionIdentity.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionIdentity.UserId);

        var retentionExpiresUtc = occurredUtc.AddMonths(6);
        await commercialAccountStore
            .MarkDeletedAsync(
                sessionIdentity.TenantId,
                sessionIdentity.UserId,
                occurredUtc,
                retentionExpiresUtc,
                cancellationToken)
            .ConfigureAwait(false);

        await commercialAuditStore
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
}
