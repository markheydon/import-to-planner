using ImportToPlanner.Commercial.Credits;

namespace ImportToPlanner.Web.Tests.TestInfrastructure;

internal sealed class CreditCheckoutPaymentPortSubstitute : ICreditCheckoutPaymentPort
{
    public string RedirectUrl { get; set; } = "https://checkout.test.example/session";

    public int CreateCustomerCallCount { get; private set; }

    public int CreateCheckoutSessionCallCount { get; private set; }

    public CreditCheckoutSessionRequest? LastSessionRequest { get; private set; }

    public Task<string> CreateCustomerAsync(string tenantId, CancellationToken cancellationToken)
    {
        CreateCustomerCallCount++;
        return Task.FromResult("cus_test_customer");
    }

    public Task<string> CreateCheckoutSessionAsync(
        CreditCheckoutSessionRequest request,
        CancellationToken cancellationToken)
    {
        CreateCheckoutSessionCallCount++;
        LastSessionRequest = request;
        return Task.FromResult(RedirectUrl);
    }
}
