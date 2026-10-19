using ImportToPlanner.Commercial.Credits;
using ImportToPlanner.Commercial.Models;
using ImportToPlanner.Tests.TestInfrastructure;

namespace ImportToPlanner.Tests.Credits;

public sealed class PaidCreditFifoConsumptionTests
{
    private static readonly DateTimeOffset OccurredUtc = new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset SharedGrantedAtUtc = new(2026, 8, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task RecordUsageAsync_ConsumesFreeMonthlyBeforeOldestPaidLot()
    {
        var store = new InMemoryCreditLedgerStore();
        await SeedFreeLotAsync(store, remaining: 1);
        await SeedPaidLotAsync(store, "pi_older", SharedGrantedAtUtc, remaining: 2);
        await SeedPaidLotAsync(store, "pi_newer", SharedGrantedAtUtc.AddDays(10), remaining: 2);

        await RecordOneUsageAsync(store);
        await RecordOneUsageAsync(store);
        await RecordOneUsageAsync(store);

        var lots = await store.GetLotsAsync("tenant-001", CancellationToken.None);
        Assert.Equal(0, lots.Single(lot => lot.LotType == LotType.FreeMonthly).RemainingQuantity);
        Assert.Equal(0, lots.Single(lot => lot.ExternalPaymentId == "pi_older").RemainingQuantity);
        Assert.Equal(2, lots.Single(lot => lot.ExternalPaymentId == "pi_newer").RemainingQuantity);
    }

    [Fact]
    public async Task RecordUsageAsync_WhenPaidLotsShareGrantedAtUtc_UsesLotIdTieBreak()
    {
        var store = new InMemoryCreditLedgerStore();
        await SeedPaidLotAsync(store, "pi_b", SharedGrantedAtUtc, remaining: 1);
        await SeedPaidLotAsync(store, "pi_a", SharedGrantedAtUtc, remaining: 1);

        await RecordOneUsageAsync(store);

        var lots = await store.GetLotsAsync("tenant-001", CancellationToken.None);
        Assert.Equal(0, lots.Single(lot => lot.LotId == "paid|pi_a").RemainingQuantity);
        Assert.Equal(1, lots.Single(lot => lot.LotId == "paid|pi_b").RemainingQuantity);
    }

    [Fact]
    public void SelectConsumableLot_OrdersPaidLotsByGrantedAtUtcThenLotId()
    {
        var lots = new[]
        {
            CreatePaidLot("paid|pi_newer", SharedGrantedAtUtc.AddDays(1)),
            CreatePaidLot("paid|pi_older", SharedGrantedAtUtc),
            CreatePaidLot("free", SharedGrantedAtUtc, LotType.FreeMonthly),
        };

        var selected = CreditLotSelector.SelectConsumableLot(lots, OccurredUtc);

        Assert.Equal("free", selected!.LotId);
    }

    private static async Task SeedFreeLotAsync(InMemoryCreditLedgerStore store, int remaining)
    {
        await store.TryGrantFreeMonthlyAsync(
            "tenant-001",
            CommercialCreditPolicy.GetUtcYearMonth(OccurredUtc),
            25,
            OccurredUtc.AddDays(-1),
            "user-001",
            CancellationToken.None);

        for (var index = 0; index < 25 - remaining; index++)
        {
            await RecordOneUsageAsync(store);
        }
    }

    private static Task<CreditGrantAttemptOutcome> SeedPaidLotAsync(
        InMemoryCreditLedgerStore store,
        string externalPaymentId,
        DateTimeOffset grantedAtUtc,
        int remaining)
        => store.TryGrantPaidPurchaseAsync(
            "tenant-001",
            externalPaymentId,
            $"evt_{externalPaymentId}",
            remaining,
            grantedAtUtc,
            "user-001",
            CancellationToken.None);

    private static async Task RecordOneUsageAsync(InMemoryCreditLedgerStore store)
    {
        await store.RecordUsageAsync(
            new RecordCreditUsageRequest(
                "tenant-001",
                "user-001",
                OccurredUtc,
                Guid.NewGuid().ToString("N"),
                Guid.NewGuid().ToString("N")),
            CancellationToken.None);
    }

    private static CreditLot CreatePaidLot(string lotId, DateTimeOffset grantedAtUtc, LotType lotType = LotType.Paid)
        => new(
            lotId,
            "tenant-001",
            lotType,
            5,
            5,
            grantedAtUtc,
            CommercialCreditPolicy.GetPaidLotExpiresAtUtc(grantedAtUtc),
            lotId.StartsWith("paid|", StringComparison.Ordinal) ? lotId["paid|".Length..] : null);
}
