using ImportToPlanner.Commercial.Credits;

namespace ImportToPlanner.Commercial.Abstractions;

/// <summary>
/// Creates Stripe customers and checkout sessions without exposing provider types to Commercial.
/// </summary>
public interface ICreditCheckoutPaymentPort
{
    /// <summary>
    /// Creates a new payment-provider customer for the tenant partition.
    /// </summary>
    /// <param name="tenantId">Commercial tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Opaque customer identifier.</returns>
    Task<string> CreateCustomerAsync(string tenantId, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a one-time checkout session for a published pack price.
    /// </summary>
    /// <param name="request">Session parameters including metadata and return URLs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Provider-hosted redirect URL.</returns>
    Task<string> CreateCheckoutSessionAsync(
        CreditCheckoutSessionRequest request,
        CancellationToken cancellationToken);
}
