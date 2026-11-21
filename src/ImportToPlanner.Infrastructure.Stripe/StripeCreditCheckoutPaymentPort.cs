using ImportToPlanner.Commercial.Abstractions;
using ImportToPlanner.Commercial.Configuration;
using ImportToPlanner.Commercial.Credits;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace ImportToPlanner.Infrastructure.Stripe;

/// <summary>
/// Stripe Checkout Session adapter for one-time credit pack purchases.
/// </summary>
public sealed class StripeCreditCheckoutPaymentPort(
    StripeClient stripeClient,
    IOptions<StripeOptions> stripeOptions) : ICreditCheckoutPaymentPort
{
    /// <inheritdoc/>
    public async Task<string> CreateCustomerAsync(string tenantId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        cancellationToken.ThrowIfCancellationRequested();

        var customerService = new CustomerService(stripeClient);
        var customer = await customerService.CreateAsync(
            new CustomerCreateOptions
            {
                Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["tenant_id"] = tenantId,
                },
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(customer.Id))
        {
            throw new InvalidOperationException("Stripe did not return a customer identifier.");
        }

        return customer.Id;
    }

    /// <inheritdoc/>
    public async Task<string> CreateCheckoutSessionAsync(
        CreditCheckoutSessionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var sessionService = new SessionService(stripeClient);
        var options = new SessionCreateOptions
        {
            Mode = "payment",
            Customer = request.StripeCustomerId,
            ClientReferenceId = request.TenantId,
            SuccessUrl = request.SuccessUrl,
            CancelUrl = request.CancelUrl,
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Price = request.StripePriceId,
                    Quantity = 1,
                },
            ],
            Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["tenant_id"] = request.TenantId,
                ["pack_id"] = request.PackId,
                ["actor_user_id"] = request.ActorUserId,
            },
            AutomaticTax = new SessionAutomaticTaxOptions
            {
                Enabled = stripeOptions.Value.AutomaticTaxEnabled,
            },
            InvoiceCreation = new SessionInvoiceCreationOptions
            {
                Enabled = true,
            },
        };

        var idempotencyKey = Guid.NewGuid().ToString("N");
        var requestOptions = new RequestOptions { IdempotencyKey = idempotencyKey };

        var session = await sessionService
            .CreateAsync(options, requestOptions, cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(session.Url))
        {
            throw new InvalidOperationException("Stripe did not return a checkout URL.");
        }

        return session.Url;
    }
}
