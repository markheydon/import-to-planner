using ImportToPlanner.Commercial.Abstractions;
using ImportToPlanner.Commercial.Credits;
using ImportToPlanner.Commercial.Models;
using ImportToPlanner.Tests.TestInfrastructure;
using Microsoft.Extensions.Logging.Abstractions;

namespace ImportToPlanner.Tests.Credits;

public sealed class ExpirePaidCreditLotsUseCaseTests
{
    private static readonly DateTimeOffset AsOfUtc = new(2027, 10, 2, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ExecuteAsync_DuePaidLot_AppendsPaidExpiryAndClearsRemaining()
    {
        var store = new InMemoryCreditLedgerStore();
        var grantedAtUtc = new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);
        await store.TryGrantPaidPurchaseAsync(
            "tenant-001",
            "pi_due",
            "evt_due",
            4,
            grantedAtUtc,
            "user-001",
            CancellationToken.None);

        var useCase = CreateUseCase(store, fixedClock: AsOfUtc);
        var result = await useCase.ExecuteAsync(AsOfUtc, CancellationToken.None);

        Assert.Equal(1, result.ExpiredLotCount);
        Assert.Equal(0, result.FailureCount);
        var lot = Assert.Single(await store.GetLotsAsync("tenant-001", CancellationToken.None));
        Assert.Equal(0, lot.RemainingQuantity);
        Assert.Contains(
            store.GetTransactions("tenant-001"),
            transaction => transaction.EntryType == CreditEntryType.PaidExpiry && transaction.Quantity == 4);
    }

    [Fact]
    public async Task ExecuteAsync_NotYetDuePaidLot_LeavesLedgerUnchanged()
    {
        var store = new InMemoryCreditLedgerStore();
        var grantedAtUtc = new DateTimeOffset(2027, 1, 1, 12, 0, 0, TimeSpan.Zero);
        await store.TryGrantPaidPurchaseAsync(
            "tenant-001",
            "pi_future",
            "evt_future",
            3,
            grantedAtUtc,
            "user-001",
            CancellationToken.None);

        var useCase = CreateUseCase(store, fixedClock: AsOfUtc);
        var result = await useCase.ExecuteAsync(AsOfUtc, CancellationToken.None);

        Assert.Equal(0, result.ExpiredLotCount);
        Assert.Equal(0, result.FailureCount);
        var lot = Assert.Single(await store.GetLotsAsync("tenant-001", CancellationToken.None));
        Assert.Equal(3, lot.RemainingQuantity);
        Assert.DoesNotContain(
            store.GetTransactions("tenant-001"),
            transaction => transaction.EntryType == CreditEntryType.PaidExpiry);
    }

    [Fact]
    public async Task ExecuteAsync_PartiallyConsumedDueLot_ExpiresOnlyUnusedQuantity()
    {
        var store = new InMemoryCreditLedgerStore();
        var grantedAtUtc = new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);
        await store.TryGrantPaidPurchaseAsync(
            "tenant-001",
            "pi_partial",
            "evt_partial",
            3,
            grantedAtUtc,
            "user-001",
            CancellationToken.None);
        await store.RecordUsageAsync(
            new RecordCreditUsageRequest(
                "tenant-001",
                "user-001",
                grantedAtUtc.AddDays(1),
                "run-001",
                "task-001"),
            CancellationToken.None);

        var useCase = CreateUseCase(store, fixedClock: AsOfUtc);
        var result = await useCase.ExecuteAsync(AsOfUtc, CancellationToken.None);

        Assert.Equal(1, result.ExpiredLotCount);
        var expiry = Assert.Single(
            store.GetTransactions("tenant-001"),
            transaction => transaction.EntryType == CreditEntryType.PaidExpiry);
        Assert.Equal(2, expiry.Quantity);
    }

    [Fact]
    public async Task ExecuteAsync_ZeroUnusedDueLot_SkipsExpiryTransaction()
    {
        var store = new InMemoryCreditLedgerStore();
        var grantedAtUtc = new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);
        await store.TryGrantPaidPurchaseAsync(
            "tenant-001",
            "pi_empty",
            "evt_empty",
            1,
            grantedAtUtc,
            "user-001",
            CancellationToken.None);
        await store.RecordUsageAsync(
            new RecordCreditUsageRequest(
                "tenant-001",
                "user-001",
                grantedAtUtc.AddDays(1),
                "run-002",
                "task-002"),
            CancellationToken.None);

        var useCase = CreateUseCase(store, fixedClock: AsOfUtc);
        var result = await useCase.ExecuteAsync(AsOfUtc, CancellationToken.None);

        Assert.Equal(0, result.ExpiredLotCount);
        Assert.DoesNotContain(
            store.GetTransactions("tenant-001"),
            transaction => transaction.EntryType == CreditEntryType.PaidExpiry);
    }

    private static ExpirePaidCreditLotsUseCase CreateUseCase(InMemoryCreditLedgerStore store, DateTimeOffset fixedClock)
        => new(
            store,
            new FixedUtcClock(fixedClock),
            NullLogger<ExpirePaidCreditLotsUseCase>.Instance);

    private sealed class FixedUtcClock(DateTimeOffset utcNow) : IUtcClock
    {
        public DateTimeOffset UtcNow => utcNow;
    }
}
