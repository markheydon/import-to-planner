using ImportToPlanner.Commercial.Credits;
using ImportToPlanner.Commercial.Models;
using ImportToPlanner.Tests.TestInfrastructure;

namespace ImportToPlanner.Tests.Credits;

public sealed class AdjustPaidCreditsForRefundUseCaseTests
{
    private static readonly DateTimeOffset OccurredUtc = new(2026, 10, 5, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ExecuteAsync_AfterPartialUse_RemovesOnlyUnusedQuantity()
    {
        var store = new InMemoryCreditLedgerStore();
        var grantUseCase = new GrantPaidCreditsUseCase(
            store,
            Microsoft.Extensions.Options.Options.Create(new ImportToPlanner.Commercial.Configuration.StripeOptions
            {
                CreditPacks =
                [
                    new ImportToPlanner.Commercial.Configuration.CreditPackOptions
                    {
                        PackId = "starter",
                        CreditQuantity = 10,
                        Published = true,
                        StripePriceId = "price_test",
                    },
                ],
            }));
        await grantUseCase.ExecuteAsync(
            new GrantPaidCreditsRequest(
                "tenant-001",
                "starter",
                "pi_partial",
                "evt_partial",
                "user-001",
                OccurredUtc.AddDays(-1)),
            CancellationToken.None);

        var paidLot = (await store.GetLotsAsync("tenant-001", CancellationToken.None)).Single();
        store.SeedLot(paidLot with { RemainingQuantity = 4 });

        var adjustUseCase = new AdjustPaidCreditsForRefundUseCase(store);
        var outcome = await adjustUseCase.ExecuteAsync(
            new AdjustPaidCreditsForRefundRequest("tenant-001", "pi_partial", "re_001", OccurredUtc),
            CancellationToken.None);

        Assert.IsType<AdjustPaidCreditsForRefundOutcome.Adjusted>(outcome);
        var lotAfter = (await store.GetLotsAsync("tenant-001", CancellationToken.None)).Single();
        Assert.Equal(0, lotAfter.RemainingQuantity);
        var adjustment = Assert.Single(
            store.GetTransactions("tenant-001"),
            transaction => transaction.EntryType == CreditEntryType.PaidAdjustment);
        Assert.Equal(4, adjustment.Quantity);
    }

    [Fact]
    public async Task ExecuteAsync_WhenLotAlreadyEmpty_WritesZeroQuantityAdjustmentWithoutNegativeBalance()
    {
        var store = new InMemoryCreditLedgerStore();
        var lotId = CommercialCreditPolicy.BuildPaidLotId("pi_empty");
        store.SeedLot(new CreditLot(
            lotId,
            "tenant-001",
            LotType.Paid,
            5,
            0,
            OccurredUtc.AddMonths(-1),
            OccurredUtc.AddMonths(11),
            "pi_empty"));

        var adjustUseCase = new AdjustPaidCreditsForRefundUseCase(store);
        var outcome = await adjustUseCase.ExecuteAsync(
            new AdjustPaidCreditsForRefundRequest("tenant-001", "pi_empty", "re_empty", OccurredUtc),
            CancellationToken.None);

        Assert.IsType<AdjustPaidCreditsForRefundOutcome.Adjusted>(outcome);
        var lotAfter = (await store.GetLotsAsync("tenant-001", CancellationToken.None)).Single();
        Assert.Equal(0, lotAfter.RemainingQuantity);
        var adjustment = Assert.Single(
            store.GetTransactions("tenant-001"),
            transaction => transaction.EntryType == CreditEntryType.PaidAdjustment);
        Assert.Equal(0, adjustment.Quantity);
    }

    [Fact]
    public async Task ExecuteAsync_DuplicateAdjustmentId_ReturnsAlreadyAdjusted()
    {
        var store = new InMemoryCreditLedgerStore();
        await new GrantPaidCreditsUseCase(
            store,
            Microsoft.Extensions.Options.Options.Create(new ImportToPlanner.Commercial.Configuration.StripeOptions
            {
                CreditPacks =
                [
                    new ImportToPlanner.Commercial.Configuration.CreditPackOptions
                    {
                        PackId = "starter",
                        CreditQuantity = 3,
                        Published = true,
                        StripePriceId = "price_test",
                    },
                ],
            })).ExecuteAsync(
            new GrantPaidCreditsRequest(
                "tenant-001",
                "starter",
                "pi_dup",
                "evt_dup",
                "user-001",
                OccurredUtc.AddDays(-2)),
            CancellationToken.None);

        var adjustUseCase = new AdjustPaidCreditsForRefundUseCase(store);
        var request = new AdjustPaidCreditsForRefundRequest("tenant-001", "pi_dup", "re_dup", OccurredUtc);
        Assert.IsType<AdjustPaidCreditsForRefundOutcome.Adjusted>(
            await adjustUseCase.ExecuteAsync(request, CancellationToken.None));
        var replay = await adjustUseCase.ExecuteAsync(request, CancellationToken.None);

        Assert.IsType<AdjustPaidCreditsForRefundOutcome.AlreadyAdjusted>(replay);
        Assert.Single(
            store.GetTransactions("tenant-001"),
            transaction => transaction.EntryType == CreditEntryType.PaidAdjustment);
    }
}
