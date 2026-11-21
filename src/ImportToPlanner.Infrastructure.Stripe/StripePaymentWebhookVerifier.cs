using ImportToPlanner.Commercial.Abstractions;
using ImportToPlanner.Commercial.Configuration;
using ImportToPlanner.Commercial.Credits;
using Microsoft.Extensions.Options;
using Stripe;

namespace ImportToPlanner.Infrastructure.Stripe;

/// <summary>
/// Verifies Stripe webhook signatures and maps events to neutral payment confirmations.
/// </summary>
public sealed class StripePaymentWebhookVerifier(IOptions<StripeOptions> stripeOptions) : IPaymentWebhookVerifier
{
    /// <inheritdoc/>
    public VerifiedPaymentConfirmation? TryVerify(string payload, string stripeSignatureHeader)
    {
        ArgumentException.ThrowIfNullOrEmpty(payload);
        if (string.IsNullOrWhiteSpace(stripeSignatureHeader))
        {
            return null;
        }

        var webhookSecret = stripeOptions.Value.WebhookSecret;
        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            return null;
        }

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(payload, stripeSignatureHeader, webhookSecret);
        }
        catch (StripeException)
        {
            return null;
        }

        return MapEvent(stripeEvent);
    }

    internal static VerifiedPaymentConfirmation MapEvent(Event stripeEvent)
    {
        var occurredUtc = stripeEvent.Created;
        var eventId = stripeEvent.Id;

        switch (stripeEvent.Type)
        {
            case EventTypes.CheckoutSessionCompleted:
            case EventTypes.CheckoutSessionAsyncPaymentSucceeded:
                {
                    var session = stripeEvent.Data.Object as global::Stripe.Checkout.Session;
                    if (session is null)
                    {
                        return Ignored(eventId, occurredUtc);
                    }

                    if (!string.Equals(session.PaymentStatus, "paid", StringComparison.Ordinal))
                    {
                        return new VerifiedPaymentConfirmation(
                            eventId,
                            VerifiedPaymentEventKind.PaymentFailed,
                            ResolveExternalPaymentId(session),
                            null,
                            null,
                            null,
                            null,
                            occurredUtc);
                    }

                    return BuildPaymentConfirmed(eventId, session, occurredUtc);
                }

            case EventTypes.CheckoutSessionAsyncPaymentFailed:
                return new VerifiedPaymentConfirmation(
                    eventId,
                    VerifiedPaymentEventKind.PaymentFailed,
                    null,
                    null,
                    null,
                    null,
                    null,
                    occurredUtc);

            case EventTypes.RefundUpdated:
                {
                    var refund = stripeEvent.Data.Object as Refund;
                    if (refund is null || !string.Equals(refund.Status, "succeeded", StringComparison.Ordinal))
                    {
                        return Ignored(eventId, occurredUtc);
                    }

                    return new VerifiedPaymentConfirmation(
                        eventId,
                        VerifiedPaymentEventKind.RefundOrDisputeConfirmed,
                        refund.PaymentIntentId,
                        refund.Id,
                        refund.Metadata.GetValueOrDefault("tenant_id"),
                        null,
                        null,
                        occurredUtc);
                }

            case EventTypes.ChargeDisputeClosed:
                {
                    var dispute = stripeEvent.Data.Object as Dispute;
                    if (dispute is null || !string.Equals(dispute.Status, "lost", StringComparison.Ordinal))
                    {
                        return Ignored(eventId, occurredUtc);
                    }

                    return new VerifiedPaymentConfirmation(
                        eventId,
                        VerifiedPaymentEventKind.RefundOrDisputeConfirmed,
                        dispute.PaymentIntentId,
                        dispute.Id,
                        dispute.Metadata.GetValueOrDefault("tenant_id"),
                        null,
                        null,
                        occurredUtc);
                }

            default:
                return Ignored(eventId, occurredUtc);
        }
    }

    private static VerifiedPaymentConfirmation BuildPaymentConfirmed(
        string eventId,
        global::Stripe.Checkout.Session session,
        DateTimeOffset occurredUtc)
    {
        session.Metadata.TryGetValue("tenant_id", out var tenantId);
        session.Metadata.TryGetValue("pack_id", out var packId);
        session.Metadata.TryGetValue("actor_user_id", out var actorUserId);

        return new VerifiedPaymentConfirmation(
            eventId,
            VerifiedPaymentEventKind.PaymentConfirmed,
            ResolveExternalPaymentId(session),
            null,
            tenantId,
            packId,
            actorUserId,
            occurredUtc);
    }

    private static string? ResolveExternalPaymentId(global::Stripe.Checkout.Session session)
    {
        if (!string.IsNullOrWhiteSpace(session.PaymentIntentId))
        {
            return session.PaymentIntentId;
        }

        return string.IsNullOrWhiteSpace(session.Id) ? null : session.Id;
    }

    private static VerifiedPaymentConfirmation Ignored(string eventId, DateTimeOffset occurredUtc)
        => new(
            eventId,
            VerifiedPaymentEventKind.Ignored,
            null,
            null,
            null,
            null,
            null,
            occurredUtc);
}
