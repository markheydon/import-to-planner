using ImportToPlanner.Commercial.Abstractions;
using ImportToPlanner.Commercial.Credits;
using ImportToPlanner.Commercial.Models;
using ImportToPlanner.Tests.TestInfrastructure;

namespace ImportToPlanner.Tests.Credits;

public sealed class ComplimentaryCreditEnsureTests
{
    private static readonly Guid AllowlistedTenant = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task EnsureAsync_ForComplimentaryTenant_SkipsGrantAndExpiryAndSetsUnlimitedFlag()
    {
        var store = new InMemoryCreditLedgerStore();
        var policy = new TestComplimentaryTenantPolicy(AllowlistedTenant.ToString());
        var useCase = new EnsureCurrentCreditBalanceUseCase(store, policy);
        var occurredUtc = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero);

        var outcome = await useCase.EnsureAsync(
            new EnsureCurrentCreditBalanceRequest(
                AllowlistedTenant.ToString(),
                "user-001",
                occurredUtc,
                EnsureBalanceReason.Preview),
            CancellationToken.None);

        var result = Assert.IsType<EnsureCurrentCreditBalanceOutcome.Succeeded>(outcome).Result;
        Assert.True(result.ComplimentaryUnlimitedAccess);
        Assert.Equal(0, result.RemainingCredits);
        Assert.False(result.GrantApplied);
        Assert.False(result.ExpiryApplied);
        Assert.Empty(store.GetTransactions(AllowlistedTenant.ToString()));
        Assert.Empty(await store.GetLotsAsync(AllowlistedTenant.ToString(), CancellationToken.None));
    }

    [Fact]
    public async Task EnsureAsync_ForComplimentaryTenant_WhenLedgerReadFails_ReturnsLedgerUnavailable()
    {
        var store = new UnavailableCreditLedgerStore();
        var policy = new TestComplimentaryTenantPolicy(AllowlistedTenant.ToString());
        var useCase = new EnsureCurrentCreditBalanceUseCase(store, policy);
        var occurredUtc = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero);

        var outcome = await useCase.EnsureAsync(
            new EnsureCurrentCreditBalanceRequest(
                AllowlistedTenant.ToString(),
                "user-001",
                occurredUtc,
                EnsureBalanceReason.Preview),
            CancellationToken.None);

        var failure = Assert.IsType<EnsureCurrentCreditBalanceOutcome.Failed>(outcome).Failure;
        Assert.Equal(CommercialCreditFailureCodes.LedgerUnavailable, failure.FailureCode);
    }

    private sealed class UnavailableCreditLedgerStore : ICreditLedgerStore
    {
        public Task<IReadOnlyList<CreditLot>> GetLotsAsync(string tenantId, CancellationToken cancellationToken)
            => throw new InvalidOperationException("Ledger unavailable.");

        public Task<bool> HasMonthGrantMarkerAsync(string tenantId, string utcYearMonth, CancellationToken cancellationToken)
            => throw new InvalidOperationException("Ledger unavailable.");

        public Task<bool> ExpireFreeLotAsync(CreditLot lot, DateTimeOffset occurredUtc, string? actorUserId, CancellationToken cancellationToken)
            => throw new InvalidOperationException("Ledger unavailable.");

        public Task<CreditGrantAttemptOutcome> TryGrantFreeMonthlyAsync(
            string tenantId,
            string utcYearMonth,
            int grantQuantity,
            DateTimeOffset grantedAtUtc,
            string? actorUserId,
            CancellationToken cancellationToken)
            => throw new InvalidOperationException("Ledger unavailable.");

        public Task<RecordCreditUsageOutcome> RecordUsageAsync(RecordCreditUsageRequest request, CancellationToken cancellationToken)
            => throw new InvalidOperationException("Ledger unavailable.");

        public Task<RecordCreditUsageOutcome> RecordComplimentaryUsageAsync(
            RecordCreditUsageRequest request,
            CancellationToken cancellationToken)
            => throw new InvalidOperationException("Ledger unavailable.");

        public Task<IReadOnlyList<CreditLedgerTransaction>> ListTransactionsAsync(
            string tenantId,
            DateTimeOffset occurredSinceUtc,
            CancellationToken cancellationToken)
            => throw new InvalidOperationException("Ledger unavailable.");

        public Task<bool> HasTransactionsBeforeAsync(
            string tenantId,
            DateTimeOffset beforeOccurredUtc,
            CancellationToken cancellationToken)
            => throw new InvalidOperationException("Ledger unavailable.");
    }
}
