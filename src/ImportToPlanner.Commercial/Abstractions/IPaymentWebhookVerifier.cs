using ImportToPlanner.Commercial.Credits;

namespace ImportToPlanner.Commercial.Abstractions;

/// <summary>
/// Verifies a signed payment provider webhook payload and maps it to a neutral confirmation record.
/// </summary>
public interface IPaymentWebhookVerifier
{
    /// <summary>
    /// Verifies the payload signature and maps the event, or returns <see langword="null"/> when the signature is invalid.
    /// </summary>
    VerifiedPaymentConfirmation? TryVerify(string payload, string stripeSignatureHeader);
}
