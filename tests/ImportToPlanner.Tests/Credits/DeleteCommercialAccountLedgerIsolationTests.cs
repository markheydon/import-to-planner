using ImportToPlanner.Application.Models;
using ImportToPlanner.Commercial.Abstractions;
using ImportToPlanner.Commercial.Credits;
using ImportToPlanner.Commercial.Models;
using ImportToPlanner.Commercial.Services;
using ImportToPlanner.Tests.TestDoubles;
using ImportToPlanner.Tests.TestInfrastructure;

namespace ImportToPlanner.Tests.Credits;

public sealed class DeleteCommercialAccountLedgerIsolationTests
{
    [Fact]
    public async Task DeleteRestoreAndPurge_DoNotChangeLedgerOrBillingMetadata()
    {
        var accountStore = new CommercialAccountStoreStub();
        var auditStore = new CommercialAuditStoreStub();
        var ledgerStore = new InMemoryCreditLedgerStore();
        var billingStore = new TenantBillingStoreStub();
        var identity = new SessionIdentityContext("tenant-001", "user-001", "user@contoso.com", "Contoso");
        var createdUtc = new DateTimeOffset(2026, 1, 10, 8, 0, 0, TimeSpan.Zero);
        var deletedUtc = new DateTimeOffset(2026, 5, 28, 9, 0, 0, TimeSpan.Zero);
        var restoredUtc = deletedUtc.AddDays(3);
        var purgeUtc = deletedUtc.AddMonths(6).AddDays(1);

        await accountStore.CreateAsync(
            new CommercialAccount(
                identity.TenantId,
                identity.UserId,
                createdUtc,
                CommercialAccountStatus.Active,
                DeletedUtc: null,
                RetentionExpiresUtc: null,
                RestoredUtc: null,
                LastSignInOutcomeUtc: createdUtc),
            CancellationToken.None);

        var grantedAtUtc = new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero);
        await ledgerStore.TryGrantFreeMonthlyAsync(
            identity.TenantId,
            CommercialCreditPolicy.GetUtcYearMonth(grantedAtUtc),
            25,
            grantedAtUtc,
            identity.UserId,
            CancellationToken.None);
        await ledgerStore.TryGrantPaidPurchaseAsync(
            identity.TenantId,
            "pi_isolation",
            "evt_isolation",
            10,
            grantedAtUtc,
            identity.UserId,
            CancellationToken.None);
        await billingStore.UpsertAsync(
            new TenantBillingMetadata(identity.TenantId, "cus_isolation", "inv_001", grantedAtUtc),
            CancellationToken.None);

        var transactionCountBefore = ledgerStore.GetTransactions(identity.TenantId).Count;
        var paidRemainingBefore = (await ledgerStore.GetLotsAsync(identity.TenantId, CancellationToken.None))
            .Where(lot => lot.LotType == LotType.Paid)
            .Sum(lot => lot.RemainingQuantity);
        var billingBefore = await billingStore.GetAsync(identity.TenantId, CancellationToken.None);

        var deleteUseCase = new DeleteCommercialAccountUseCase(accountStore, auditStore);
        var restoreUseCase = new RestoreCommercialAccountUseCase(accountStore, auditStore);
        var purgeUseCase = new PurgeExpiredCommercialAccountsUseCase(accountStore, auditStore);

        await deleteUseCase.ExecuteAsync(identity, deletedUtc, CancellationToken.None);
        AssertBalanceUnchanged(ledgerStore, billingStore, identity.TenantId, transactionCountBefore, paidRemainingBefore, billingBefore);

        var restoreResult = await restoreUseCase.ExecuteAsync(identity, restoredUtc, CancellationToken.None);
        Assert.Equal(CommercialAccountRestoreResult.Restored, restoreResult);
        AssertBalanceUnchanged(ledgerStore, billingStore, identity.TenantId, transactionCountBefore, paidRemainingBefore, billingBefore);

        await deleteUseCase.ExecuteAsync(identity, deletedUtc, CancellationToken.None);
        var purgedCount = await purgeUseCase.ExecuteAsync(purgeUtc, 10, CancellationToken.None);
        Assert.Equal(1, purgedCount);
        AssertBalanceUnchanged(ledgerStore, billingStore, identity.TenantId, transactionCountBefore, paidRemainingBefore, billingBefore);
    }

    private static void AssertBalanceUnchanged(
        InMemoryCreditLedgerStore ledgerStore,
        TenantBillingStoreStub billingStore,
        string tenantId,
        int expectedTransactionCount,
        int expectedPaidRemaining,
        TenantBillingMetadata? expectedBilling)
    {
        Assert.Equal(expectedTransactionCount, ledgerStore.GetTransactions(tenantId).Count);
        var paidRemaining = ledgerStore.GetLotsAsync(tenantId, CancellationToken.None).Result
            .Where(lot => lot.LotType == LotType.Paid)
            .Sum(lot => lot.RemainingQuantity);
        Assert.Equal(expectedPaidRemaining, paidRemaining);
        var billing = billingStore.GetAsync(tenantId, CancellationToken.None).Result;
        Assert.NotNull(billing);
        Assert.Equal(expectedBilling!.StripeCustomerId, billing.StripeCustomerId);
    }

    private sealed class TenantBillingStoreStub : ITenantBillingStore
    {
        private readonly Dictionary<string, TenantBillingMetadata> rows = new(StringComparer.Ordinal);

        public Task<TenantBillingMetadata?> GetAsync(string tenantId, CancellationToken cancellationToken)
        {
            rows.TryGetValue(tenantId, out var metadata);
            return Task.FromResult(metadata);
        }

        public Task UpsertAsync(TenantBillingMetadata metadata, CancellationToken cancellationToken)
        {
            rows[metadata.TenantId] = metadata;
            return Task.CompletedTask;
        }
    }
}
