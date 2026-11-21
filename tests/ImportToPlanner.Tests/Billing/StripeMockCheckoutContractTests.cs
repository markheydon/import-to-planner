using Stripe;
using Stripe.Checkout;

namespace ImportToPlanner.Tests.Billing;

public sealed class StripeMockCheckoutContractTests
{
    [Fact]
    public async Task CreateCheckoutSession_WhenStripeMockConfigured_AcceptsFixtureRequest()
    {
        var mockBaseUrl = Environment.GetEnvironmentVariable("STRIPE_MOCK_BASE_URL");
        if (string.IsNullOrWhiteSpace(mockBaseUrl))
        {
            return;
        }

        var stripeClient = new StripeClient(
            apiKey: "sk_test_mock",
            httpClient: new SystemNetHttpClient(new HttpClient { BaseAddress = new Uri(mockBaseUrl) }));

        var sessionService = new SessionService(stripeClient);
        var session = await sessionService.CreateAsync(
            new SessionCreateOptions
            {
                Mode = "payment",
                LineItems =
                [
                    new SessionLineItemOptions
                    {
                        Price = "price_1",
                        Quantity = 1,
                    },
                ],
                SuccessUrl = "https://example.test/success",
                CancelUrl = "https://example.test/cancel",
            },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(string.IsNullOrWhiteSpace(session.Id));
    }
}
