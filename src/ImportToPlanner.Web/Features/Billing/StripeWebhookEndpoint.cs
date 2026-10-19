using ImportToPlanner.Commercial.Abstractions;
using ImportToPlanner.Commercial.Credits;

namespace ImportToPlanner.Web.Features.Billing;

/// <summary>
/// Maps the Stripe webhook route when commercial mode is enabled.
/// </summary>
internal static class StripeWebhookEndpoint
{
    /// <summary>
    /// Maps <c>POST /webhooks/stripe</c> for signed payment fulfilment callbacks.
    /// </summary>
    public static IEndpointRouteBuilder MapStripeWebhook(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
            "/webhooks/stripe",
            async (
                HttpContext httpContext,
                IPaymentWebhookVerifier paymentWebhookVerifier,
                HandleVerifiedPaymentConfirmationUseCase handleVerifiedPaymentConfirmationUseCase,
                CancellationToken cancellationToken) =>
            {
                using var reader = new StreamReader(httpContext.Request.Body);
                var payload = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
                var signature = httpContext.Request.Headers["Stripe-Signature"].ToString();

                var confirmation = paymentWebhookVerifier.TryVerify(payload, signature);
                if (confirmation is null)
                {
                    return Results.BadRequest();
                }

                var outcome = await handleVerifiedPaymentConfirmationUseCase
                    .HandleAsync(confirmation, cancellationToken)
                    .ConfigureAwait(false);

                return outcome switch
                {
                    VerifiedPaymentHandleOutcome.LedgerWriteFailed => Results.StatusCode(StatusCodes.Status500InternalServerError),
                    _ => Results.Ok(),
                };
            });

        return endpoints;
    }
}
