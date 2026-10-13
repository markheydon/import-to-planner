using ImportToPlanner.Commercial.Configuration;
using ImportToPlanner.Commercial.Credits;
using ImportToPlanner.Tests.TestInfrastructure;
using Microsoft.Extensions.Options;

namespace ImportToPlanner.Tests.Credits;

public sealed class StripeWebhookGrantTests
{
    private static readonly DateTimeOffset OccurredUtc = new(2026, 10, 2, 15, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_AsyncPaymentFailed_DoesNotGrantCredits()
    {
        var store = new InMemoryCreditLedgerStore();
        var handler = CreateHandler(store);
        var confirmation = new VerifiedPaymentConfirmation(
            "evt_failed",
            VerifiedPaymentEventKind.PaymentFailed,
            null,
            null,
            null,
            null,
            null,
            OccurredUtc);

        var outcome = await handler.HandleAsync(confirmation, CancellationToken.None);

        Assert.IsType<VerifiedPaymentHandleOutcome.AcknowledgedWithoutLedgerWrite>(outcome);
        Assert.Empty(await store.GetLotsAsync("tenant-001", CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_AbandonedCheckoutCompletedUnpaid_DoesNotGrantCredits()
    {
        var store = new InMemoryCreditLedgerStore();
        var handler = CreateHandler(store);
        var confirmation = new VerifiedPaymentConfirmation(
            "evt_unpaid",
            VerifiedPaymentEventKind.PaymentFailed,
            "cs_unpaid",
            null,
            "tenant-001",
            "starter",
            "user-001",
            OccurredUtc);

        var outcome = await handler.HandleAsync(confirmation, CancellationToken.None);

        Assert.IsType<VerifiedPaymentHandleOutcome.AcknowledgedWithoutLedgerWrite>(outcome);
        Assert.Empty(await store.GetLotsAsync("tenant-001", CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_PaymentConfirmed_GrantsCreditsThroughDispatch()
    {
        var store = new InMemoryCreditLedgerStore();
        var handler = CreateHandler(store);
        var confirmation = new VerifiedPaymentConfirmation(
            "evt_paid",
            VerifiedPaymentEventKind.PaymentConfirmed,
            "pi_paid",
            null,
            "tenant-001",
            "starter",
            "user-001",
            OccurredUtc);

        var outcome = await handler.HandleAsync(confirmation, CancellationToken.None);

        Assert.IsType<VerifiedPaymentHandleOutcome.LedgerWriteSucceeded>(outcome);
        Assert.Single(await store.GetLotsAsync("tenant-001", CancellationToken.None));
    }

    private static HandleVerifiedPaymentConfirmationUseCase CreateHandler(InMemoryCreditLedgerStore store)
    {
        var stripeOptions = Options.Create(new StripeOptions
        {
            CreditPacks =
            [
                new CreditPackOptions
                {
                    PackId = "starter",
                    CreditQuantity = 25,
                    Published = true,
                    StripePriceId = "price_test",
                },
            ],
        });
        var grant = new GrantPaidCreditsUseCase(store, stripeOptions);
        var adjust = new AdjustPaidCreditsForRefundUseCase(store);
        return new HandleVerifiedPaymentConfirmationUseCase(grant, adjust);
    }
}
