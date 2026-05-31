namespace ImportToPlanner.CommercialService.CommercialAccounts;

/// <summary>
/// Purges expired deleted commercial accounts and expired audit records.
/// </summary>
public sealed class PurgeExpiredCommercialAccountsUseCase(
    ICommercialAccountStore commercialAccountStore,
    ICommercialAuditStore commercialAuditStore)
{
    /// <summary>
    /// Removes expired deleted accounts and purges expired audit records.
    /// </summary>
    /// <param name="asOfUtc">The UTC cut-off timestamp for expiry checks.</param>
    /// <param name="batchSize">The maximum number of accounts to purge per run.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of purged account records.</returns>
    public async Task<int> ExecuteAsync(DateTimeOffset asOfUtc, int batchSize, CancellationToken cancellationToken)
    {
        var effectiveBatchSize = Math.Max(0, batchSize);
        if (effectiveBatchSize == 0)
        {
            return 0;
        }

        var expiredAccounts = await commercialAccountStore
            .ListExpiredDeletedAsync(asOfUtc, effectiveBatchSize, cancellationToken)
            .ConfigureAwait(false);

        foreach (var account in expiredAccounts)
        {
            await commercialAccountStore
                .PurgeAsync(account.TenantId, account.UserId, cancellationToken)
                .ConfigureAwait(false);
        }

        await commercialAuditStore
            .PurgeExpiredAsync(asOfUtc, effectiveBatchSize, cancellationToken)
            .ConfigureAwait(false);

        return expiredAccounts.Count;
    }
}
