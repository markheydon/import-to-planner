using ImportToPlanner.Commercial.Abstractions;
using ImportToPlanner.Commercial.Credits;
using ImportToPlanner.Commercial.Models;
using ImportToPlanner.Tests.TestInfrastructure;

namespace ImportToPlanner.Tests.Credits;

public sealed class EnsureCurrentCreditBalanceUseCaseTests
{
    [Fact]
    public async Task EnsureAsync_FirstCallInUtcMonth_GrantsTwentyFiveAtOccurredUtc()
    {
        var store = new InMemoryCreditLedgerStore();
        var useCase = new EnsureCurrentCreditBalanceUseCase(store);
        var occurredUtc = new DateTimeOffset(2026, 9, 15, 14, 30, 0, TimeSpan.Zero);

        var outcome = await useCase.EnsureAsync(
            new EnsureCurrentCreditBalanceRequest("tenant-001", "user-001", occurredUtc, EnsureBalanceReason.Preview),
            CancellationToken.None);

        var result = Assert.IsType<EnsureCurrentCreditBalanceOutcome.Succeeded>(outcome).Result;
        Assert.Equal(25, result.RemainingCredits);
        Assert.True(result.GrantApplied);
        Assert.False(result.ExpiryApplied);

        var lots = await store.GetLotsAsync("tenant-001", CancellationToken.None);
        Assert.Single(lots);
        Assert.Equal(occurredUtc, lots[0].GrantedAtUtc);
        Assert.Equal(25, lots[0].GrantedQuantity);
    }

    [Fact]
    public async Task EnsureAsync_SecondCallSameUtcMonth_DoesNotGrantAgain()
    {
        var store = new InMemoryCreditLedgerStore();
        var useCase = new EnsureCurrentCreditBalanceUseCase(store);
        var occurredUtc = new DateTimeOffset(2026, 9, 2, 9, 0, 0, TimeSpan.Zero);

        await useCase.EnsureAsync(
            new EnsureCurrentCreditBalanceRequest("tenant-001", "user-001", occurredUtc, EnsureBalanceReason.Preview),
            CancellationToken.None);
        var secondOutcome = await useCase.EnsureAsync(
            new EnsureCurrentCreditBalanceRequest("tenant-001", "user-001", occurredUtc.AddHours(3), EnsureBalanceReason.Confirm),
            CancellationToken.None);

        var result = Assert.IsType<EnsureCurrentCreditBalanceOutcome.Succeeded>(secondOutcome).Result;
        Assert.Equal(25, result.RemainingCredits);
        Assert.False(result.GrantApplied);
        Assert.Equal(1, store.GetTransactions("tenant-001").Count(transaction => transaction.EntryType == CreditEntryType.FreeGrant));
    }

    [Fact]
    public async Task EnsureAsync_MonthBoundary_ExpiresLeftoverThenGrantsTwentyFive()
    {
        var store = new InMemoryCreditLedgerStore();
        var useCase = new EnsureCurrentCreditBalanceUseCase(store);
        var august = new DateTimeOffset(2026, 8, 31, 23, 0, 0, TimeSpan.Zero);

        await useCase.EnsureAsync(
            new EnsureCurrentCreditBalanceRequest("tenant-001", "user-001", august, EnsureBalanceReason.Preview),
            CancellationToken.None);

        for (var index = 0; index < 15; index++)
        {
            await store.RecordUsageAsync(
                new RecordCreditUsageRequest("tenant-001", "user-001", august, "run-1", $"task-{index}"),
                CancellationToken.None);
        }

        var september = new DateTimeOffset(2026, 9, 1, 0, 5, 0, TimeSpan.Zero);
        var outcome = await useCase.EnsureAsync(
            new EnsureCurrentCreditBalanceRequest("tenant-001", "user-001", september, EnsureBalanceReason.Preview),
            CancellationToken.None);

        var result = Assert.IsType<EnsureCurrentCreditBalanceOutcome.Succeeded>(outcome).Result;
        Assert.Equal(25, result.RemainingCredits);
        Assert.True(result.ExpiryApplied);
        Assert.True(result.GrantApplied);
        Assert.Equal(2, store.GetTransactions("tenant-001").Count(transaction => transaction.EntryType == CreditEntryType.FreeGrant));
    }

    [Fact]
    public async Task DormantTenant_WithoutEnsureCall_WritesNoGrantOrExpiryRows()
    {
        var store = new InMemoryCreditLedgerStore();

        Assert.Empty(store.GetTransactions("tenant-dormant"));
        Assert.False(await store.HasMonthGrantMarkerAsync("tenant-dormant", "202609", CancellationToken.None));
    }

    [Fact]
    public async Task EnsureAsync_PreviewAfterGrant_DoesNotWriteUsageTransactions()
    {
        var store = new InMemoryCreditLedgerStore();
        var useCase = new EnsureCurrentCreditBalanceUseCase(store);
        var occurredUtc = new DateTimeOffset(2026, 9, 2, 9, 0, 0, TimeSpan.Zero);

        await useCase.EnsureAsync(
            new EnsureCurrentCreditBalanceRequest("tenant-001", "user-001", occurredUtc, EnsureBalanceReason.Preview),
            CancellationToken.None);

        var previewOutcome = await useCase.EnsureAsync(
            new EnsureCurrentCreditBalanceRequest("tenant-001", "user-001", occurredUtc.AddHours(2), EnsureBalanceReason.Preview),
            CancellationToken.None);

        var result = Assert.IsType<EnsureCurrentCreditBalanceOutcome.Succeeded>(previewOutcome).Result;
        Assert.Equal(25, result.RemainingCredits);
        Assert.False(result.GrantApplied);
        Assert.DoesNotContain(
            store.GetTransactions("tenant-001"),
            transaction => transaction.EntryType == CreditEntryType.Usage);
    }

    [Fact]
    public async Task EnsureAsync_ConcurrentCallsSameMonth_YieldsSingleFreeGrant()
    {
        var store = new InMemoryCreditLedgerStore();
        var useCase = new EnsureCurrentCreditBalanceUseCase(store);
        var occurredUtc = new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero);
        var request = new EnsureCurrentCreditBalanceRequest("tenant-001", "user-001", occurredUtc, EnsureBalanceReason.Preview);

        await Task.WhenAll(
            useCase.EnsureAsync(request, CancellationToken.None),
            useCase.EnsureAsync(request, CancellationToken.None));

        Assert.Equal(
            1,
            store.GetTransactions("tenant-001").Count(transaction => transaction.EntryType == CreditEntryType.FreeGrant));
    }

    [Fact]
    public async Task EnsureAsync_WhenExpiryFails_ReturnsExpiryFailedWithoutGrant()
    {
        var inner = new InMemoryCreditLedgerStore();
        var store = new ExpiryFailingCreditLedgerStore(inner);
        var useCase = new EnsureCurrentCreditBalanceUseCase(store);
        var august = new DateTimeOffset(2026, 8, 15, 10, 0, 0, TimeSpan.Zero);

        await inner.TryGrantFreeMonthlyAsync(
            "tenant-001",
            "202608",
            25,
            august,
            "user-001",
            CancellationToken.None);

        var september = new DateTimeOffset(2026, 9, 1, 0, 5, 0, TimeSpan.Zero);
        var outcome = await useCase.EnsureAsync(
            new EnsureCurrentCreditBalanceRequest("tenant-001", "user-001", september, EnsureBalanceReason.Preview),
            CancellationToken.None);

        var failure = Assert.IsType<EnsureCurrentCreditBalanceOutcome.Failed>(outcome).Failure;
        Assert.Equal(CommercialCreditFailureCodes.ExpiryFailed, failure.FailureCode);
        Assert.False(await inner.HasMonthGrantMarkerAsync("tenant-001", "202609", CancellationToken.None));
    }

    [Fact]
    public async Task EnsureAsync_WhenGrantFails_ReturnsGrantFailedWithoutGrantMarker()
    {
        var store = new GrantFailingCreditLedgerStore();
        var useCase = new EnsureCurrentCreditBalanceUseCase(store);
        var occurredUtc = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero);

        var outcome = await useCase.EnsureAsync(
            new EnsureCurrentCreditBalanceRequest("tenant-001", "user-001", occurredUtc, EnsureBalanceReason.Preview),
            CancellationToken.None);

        var failure = Assert.IsType<EnsureCurrentCreditBalanceOutcome.Failed>(outcome).Failure;
        Assert.Equal(CommercialCreditFailureCodes.GrantFailed, failure.FailureCode);
        Assert.False(await store.HasMonthGrantMarkerAsync("tenant-001", "202609", CancellationToken.None));
    }

    private sealed class ExpiryFailingCreditLedgerStore(InMemoryCreditLedgerStore inner) : ICreditLedgerStore
    {
        public Task<IReadOnlyList<CreditLot>> GetLotsAsync(string tenantId, CancellationToken cancellationToken)
            => inner.GetLotsAsync(tenantId, cancellationToken);

        public Task<bool> HasMonthGrantMarkerAsync(string tenantId, string utcYearMonth, CancellationToken cancellationToken)
            => inner.HasMonthGrantMarkerAsync(tenantId, utcYearMonth, cancellationToken);

        public Task<bool> ExpireFreeLotAsync(CreditLot lot, DateTimeOffset occurredUtc, string? actorUserId, CancellationToken cancellationToken)
            => Task.FromResult(false);

        public Task<CreditGrantAttemptOutcome> TryGrantFreeMonthlyAsync(
            string tenantId,
            string utcYearMonth,
            int grantQuantity,
            DateTimeOffset grantedAtUtc,
            string? actorUserId,
            CancellationToken cancellationToken)
            => inner.TryGrantFreeMonthlyAsync(tenantId, utcYearMonth, grantQuantity, grantedAtUtc, actorUserId, cancellationToken);

        public Task<RecordCreditUsageOutcome> RecordUsageAsync(RecordCreditUsageRequest request, CancellationToken cancellationToken)
            => inner.RecordUsageAsync(request, cancellationToken);
    }

    private sealed class GrantFailingCreditLedgerStore : ICreditLedgerStore
    {
        public Task<IReadOnlyList<CreditLot>> GetLotsAsync(string tenantId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<CreditLot>>([]);

        public Task<bool> HasMonthGrantMarkerAsync(string tenantId, string utcYearMonth, CancellationToken cancellationToken)
            => Task.FromResult(false);

        public Task<bool> ExpireFreeLotAsync(CreditLot lot, DateTimeOffset occurredUtc, string? actorUserId, CancellationToken cancellationToken)
            => Task.FromResult(true);

        public Task<CreditGrantAttemptOutcome> TryGrantFreeMonthlyAsync(
            string tenantId,
            string utcYearMonth,
            int grantQuantity,
            DateTimeOffset grantedAtUtc,
            string? actorUserId,
            CancellationToken cancellationToken)
            => Task.FromResult<CreditGrantAttemptOutcome>(
                new CreditGrantAttemptOutcome.Failure(CommercialCreditFailureCodes.GrantFailed));

        public Task<RecordCreditUsageOutcome> RecordUsageAsync(RecordCreditUsageRequest request, CancellationToken cancellationToken)
            => Task.FromResult<RecordCreditUsageOutcome>(
                new RecordCreditUsageOutcome.Failure(CommercialCreditFailureCodes.Exhausted));
    }
}
