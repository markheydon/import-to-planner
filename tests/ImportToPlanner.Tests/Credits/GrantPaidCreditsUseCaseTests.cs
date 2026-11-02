using ImportToPlanner.Commercial.Configuration;
using ImportToPlanner.Commercial.Credits;
using ImportToPlanner.Commercial.Models;
using ImportToPlanner.Tests.TestInfrastructure;
using Microsoft.Extensions.Options;

namespace ImportToPlanner.Tests.Credits;

public sealed class GrantPaidCreditsUseCaseTests
{
    private static readonly DateTimeOffset GrantedAtUtc = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ExecuteAsync_ConfirmedPayment_GrantsOnePaidLotWithPackQuantityAndTwelveMonthExpiry()
    {
        var store = new InMemoryCreditLedgerStore();
        var useCase = CreateUseCase(store, CreateOptions("starter", 100, published: true));
        var request = CreateRequest("starter", "pi_001", "evt_001");

        var outcome = await useCase.ExecuteAsync(request, CancellationToken.None);

        Assert.IsType<GrantPaidCreditsOutcome.Granted>(outcome);
        var lots = await store.GetLotsAsync("tenant-001", CancellationToken.None);
        var paidLot = Assert.Single(lots);
        Assert.Equal(LotType.Paid, paidLot.LotType);
        Assert.Equal(100, paidLot.GrantedQuantity);
        Assert.Equal(100, paidLot.RemainingQuantity);
        Assert.Equal("pi_001", paidLot.ExternalPaymentId);
        Assert.Equal(CommercialCreditPolicy.BuildPaidLotId("pi_001"), paidLot.LotId);
        Assert.Equal(GrantedAtUtc, paidLot.GrantedAtUtc);
        Assert.Equal(CommercialCreditPolicy.GetPaidLotExpiresAtUtc(GrantedAtUtc), paidLot.ExpiresAtUtc);

        var purchase = Assert.Single(
            store.GetTransactions("tenant-001"),
            transaction => transaction.EntryType == CreditEntryType.PaidPurchase);
        Assert.Equal(100, purchase.Quantity);
        Assert.Equal("user-001", purchase.ActorUserId);
    }

    [Fact]
    public async Task ExecuteAsync_SameEventIdAndPaymentIdReplay_ReturnsAlreadyGrantedWithoutSecondLot()
    {
        var store = new InMemoryCreditLedgerStore();
        var useCase = CreateUseCase(store, CreateOptions("starter", 50, published: true));
        var request = CreateRequest("starter", "pi_002", "evt_002");

        Assert.IsType<GrantPaidCreditsOutcome.Granted>(await useCase.ExecuteAsync(request, CancellationToken.None));
        var replay = await useCase.ExecuteAsync(request, CancellationToken.None);

        Assert.IsType<GrantPaidCreditsOutcome.AlreadyGranted>(replay);
        Assert.Single(await store.GetLotsAsync("tenant-001", CancellationToken.None));
        Assert.Single(
            store.GetTransactions("tenant-001"),
            transaction => transaction.EntryType == CreditEntryType.PaidPurchase);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownPackId_ReturnsPackUnavailableFailure()
    {
        var store = new InMemoryCreditLedgerStore();
        var useCase = CreateUseCase(store, CreateOptions("starter", 50, published: true));
        var request = CreateRequest("missing-pack", "pi_003", "evt_003");

        var outcome = await useCase.ExecuteAsync(request, CancellationToken.None);

        var failure = Assert.IsType<GrantPaidCreditsOutcome.Failed>(outcome);
        Assert.Equal(PaidCreditPurchaseFailureCodes.PackUnavailable, failure.FailureCode);
        Assert.Empty(await store.GetLotsAsync("tenant-001", CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_UnpublishedPack_ReturnsPackUnavailableFailure()
    {
        var store = new InMemoryCreditLedgerStore();
        var useCase = CreateUseCase(store, CreateOptions("hidden", 25, published: false));
        var request = CreateRequest("hidden", "pi_004", "evt_004");

        var outcome = await useCase.ExecuteAsync(request, CancellationToken.None);

        var failure = Assert.IsType<GrantPaidCreditsOutcome.Failed>(outcome);
        Assert.Equal(PaidCreditPurchaseFailureCodes.PackUnavailable, failure.FailureCode);
    }

    private static GrantPaidCreditsUseCase CreateUseCase(
        InMemoryCreditLedgerStore store,
        IOptions<StripeOptions> options)
        => new(store, options);

    private static IOptions<StripeOptions> CreateOptions(string packId, int quantity, bool published)
        => Options.Create(new StripeOptions
        {
            CreditPacks =
            [
                new CreditPackOptions
                {
                    PackId = packId,
                    CreditQuantity = quantity,
                    Published = published,
                    StripePriceId = "price_test",
                    DisplayName = "Test pack",
                    AmountMinor = 1000,
                    Currency = "gbp",
                },
            ],
        });

    private static GrantPaidCreditsRequest CreateRequest(string packId, string paymentId, string eventId)
        => new(
            "tenant-001",
            packId,
            paymentId,
            eventId,
            "user-001",
            GrantedAtUtc);
}
