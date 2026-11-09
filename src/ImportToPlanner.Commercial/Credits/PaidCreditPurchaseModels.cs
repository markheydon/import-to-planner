namespace ImportToPlanner.Commercial.Credits;

/// <summary>
/// Stable diagnostic and failure codes for paid credit checkout, grant, and refund flows.
/// </summary>
public static class PaidCreditPurchaseFailureCodes
{
    /// <summary>
    /// Commercial mode is disabled; checkout is not available.
    /// </summary>
    public const string CommercialOff = "billing.commercial_off";

    /// <summary>
    /// The tenant has complimentary unlimited credits and cannot start checkout.
    /// </summary>
    public const string ComplimentaryTenant = "billing.complimentary_tenant";

    /// <summary>
    /// The pack id is unknown or not published.
    /// </summary>
    public const string PackUnavailable = "billing.pack_unavailable";

    /// <summary>
    /// Tenant billing metadata storage is unavailable.
    /// </summary>
    public const string BillingStoreUnavailable = "billing.store_unavailable";

    /// <summary>
    /// The external payment provider could not create a checkout session.
    /// </summary>
    public const string PaymentProviderUnavailable = "billing.payment_provider_unavailable";

    /// <summary>
    /// The grant request tenant does not match the payment metadata tenant.
    /// </summary>
    public const string TenantMismatch = "billing.tenant_mismatch";

    /// <summary>
    /// No paid lot exists for the external payment identifier in this tenant.
    /// </summary>
    public const string PaidLotNotFound = "billing.paid_lot_not_found";

    /// <summary>
    /// The credit ledger could not commit the grant or adjustment.
    /// </summary>
    public const string LedgerCommitFailed = "billing.ledger_commit_failed";
}

/// <summary>
/// In-app surface that receives the user after checkout success or cancel.
/// </summary>
public enum CreditCheckoutReturnSurface
{
    /// <summary>
    /// Import preview step (insufficient-credits purchase path).
    /// </summary>
    Preview = 0,

    /// <summary>
    /// Commercial profile credits section.
    /// </summary>
    Profile = 1,
}

/// <summary>
/// Request to create an external checkout session for a published credit pack.
/// </summary>
/// <param name="TenantId">Commercial tenant identifier from the server session.</param>
/// <param name="ActorUserId">Session user identifier for audit.</param>
/// <param name="PackId">Chosen published pack slug.</param>
/// <param name="ReturnSurface">Preview or profile return URLs.</param>
/// <param name="SuccessUrl">Provider success redirect URL.</param>
/// <param name="CancelUrl">Provider cancel redirect URL.</param>
/// <param name="OccurredUtc">Timestamp when checkout was requested.</param>
public sealed record CreateCreditCheckoutRequest(
    string TenantId,
    string ActorUserId,
    string PackId,
    CreditCheckoutReturnSurface ReturnSurface,
    string SuccessUrl,
    string CancelUrl,
    DateTimeOffset OccurredUtc);

/// <summary>
/// Neutral checkout session parameters passed to the payment adapter.
/// </summary>
/// <param name="TenantId">Commercial tenant identifier.</param>
/// <param name="ActorUserId">Session user identifier for audit metadata.</param>
/// <param name="PackId">Published pack slug.</param>
/// <param name="StripePriceId">Provider price identifier for the pack.</param>
/// <param name="StripeCustomerId">Existing or newly created customer identifier.</param>
/// <param name="SuccessUrl">Success redirect URL.</param>
/// <param name="CancelUrl">Cancel redirect URL.</param>
/// <param name="OccurredUtc">Checkout request timestamp.</param>
public sealed record CreditCheckoutSessionRequest(
    string TenantId,
    string ActorUserId,
    string PackId,
    string StripePriceId,
    string StripeCustomerId,
    string SuccessUrl,
    string CancelUrl,
    DateTimeOffset OccurredUtc);

/// <summary>
/// Outcome of creating a credit checkout session.
/// </summary>
public abstract record CreateCreditCheckoutOutcome
{
    /// <summary>
    /// Checkout is ready; redirect the browser to the provider-hosted URL.
    /// </summary>
    /// <param name="RedirectUrl">Provider-hosted checkout URL.</param>
    public sealed record Ready(string RedirectUrl) : CreateCreditCheckoutOutcome;

    /// <summary>
    /// Checkout was rejected before contacting the provider or after a non-recoverable validation failure.
    /// </summary>
    /// <param name="DiagnosticCode">Structured diagnostic code for logging and presenters.</param>
    public sealed record Rejected(string DiagnosticCode) : CreateCreditCheckoutOutcome;
}

/// <summary>
/// Neutral payment confirmation produced by the payment adapter after signature verification.
/// </summary>
/// <param name="ExternalEventId">Opaque provider event identifier used for idempotency (<c>evt|</c> marker).</param>
/// <param name="EventKind">How the product should handle this delivery.</param>
/// <param name="ExternalPaymentId">Opaque payment identifier (<c>pay|</c> marker and lot key).</param>
/// <param name="ExternalAdjustmentId">Opaque refund or dispute identifier when applicable (<c>adj|</c> marker).</param>
/// <param name="TenantId">Tenant from verified checkout metadata.</param>
/// <param name="PackId">Pack from verified checkout metadata.</param>
/// <param name="ActorUserId">User who started checkout, for audit on grant only.</param>
/// <param name="OccurredUtc">Event timestamp from the verified payload.</param>
public sealed record VerifiedPaymentConfirmation(
    string ExternalEventId,
    VerifiedPaymentEventKind EventKind,
    string? ExternalPaymentId,
    string? ExternalAdjustmentId,
    string? TenantId,
    string? PackId,
    string? ActorUserId,
    DateTimeOffset OccurredUtc);

/// <summary>
/// Classification of a verified payment provider webhook delivery.
/// </summary>
public enum VerifiedPaymentEventKind
{
    /// <summary>
    /// Payment confirmed with paid status; grant paid credits.
    /// </summary>
    PaymentConfirmed = 0,

    /// <summary>
    /// Asynchronous payment failed; acknowledge without grant.
    /// </summary>
    PaymentFailed = 1,

    /// <summary>
    /// Refund succeeded or dispute closed as lost; adjust unused paid credits.
    /// </summary>
    RefundOrDisputeConfirmed = 2,

    /// <summary>
    /// Event type the product deliberately ignores; acknowledge without ledger write.
    /// </summary>
    Ignored = 3,
}

/// <summary>
/// Request to grant a paid credit lot after payment confirmation.
/// </summary>
/// <param name="TenantId">Commercial tenant identifier from verified metadata.</param>
/// <param name="PackId">Published pack slug from verified metadata.</param>
/// <param name="ExternalPaymentId">Opaque payment identifier for the lot and <c>pay|</c> marker.</param>
/// <param name="ExternalEventId">Opaque provider event identifier for the <c>evt|</c> marker.</param>
/// <param name="ActorUserId">User who started checkout (audit on the purchase transaction only).</param>
/// <param name="OccurredUtc">Payment confirmation instant from the verified event.</param>
public sealed record GrantPaidCreditsRequest(
    string TenantId,
    string PackId,
    string ExternalPaymentId,
    string ExternalEventId,
    string ActorUserId,
    DateTimeOffset OccurredUtc);

/// <summary>
/// Outcome of granting paid credits for a confirmed payment.
/// </summary>
public abstract record GrantPaidCreditsOutcome
{
    /// <summary>
    /// A new paid lot and purchase transaction were committed.
    /// </summary>
    public sealed record Granted : GrantPaidCreditsOutcome;

    /// <summary>
    /// The event or payment was already processed; ledger unchanged.
    /// </summary>
    public sealed record AlreadyGranted : GrantPaidCreditsOutcome;

    /// <summary>
    /// Grant was rejected or the ledger commit failed.
    /// </summary>
    /// <param name="FailureCode">Structured failure code.</param>
    public sealed record Failed(string FailureCode) : GrantPaidCreditsOutcome;
}

/// <summary>
/// Request to reduce unused paid credits after a confirmed refund or lost dispute.
/// </summary>
/// <param name="TenantId">Commercial tenant identifier from verified metadata.</param>
/// <param name="ExternalPaymentId">Payment identifier used to locate the paid lot.</param>
/// <param name="ExternalAdjustmentId">Opaque refund or dispute identifier for the <c>adj|</c> marker.</param>
/// <param name="OccurredUtc">Adjustment confirmation instant from the verified event.</param>
public sealed record AdjustPaidCreditsForRefundRequest(
    string TenantId,
    string ExternalPaymentId,
    string ExternalAdjustmentId,
    DateTimeOffset OccurredUtc);

/// <summary>
/// Outcome of adjusting paid credits for a refund or chargeback.
/// </summary>
public abstract record AdjustPaidCreditsForRefundOutcome
{
    /// <summary>
    /// Unused quantity was removed and an adjustment transaction was committed.
    /// </summary>
    public sealed record Adjusted : AdjustPaidCreditsForRefundOutcome;

    /// <summary>
    /// The adjustment identifier was already processed; ledger unchanged.
    /// </summary>
    public sealed record AlreadyAdjusted : AdjustPaidCreditsForRefundOutcome;

    /// <summary>
    /// Adjustment failed (for example missing lot or ledger error).
    /// </summary>
    /// <param name="FailureCode">Structured failure code.</param>
    public sealed record Failed(string FailureCode) : AdjustPaidCreditsForRefundOutcome;
}
