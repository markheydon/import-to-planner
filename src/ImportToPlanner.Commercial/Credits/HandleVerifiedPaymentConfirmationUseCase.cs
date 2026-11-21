namespace ImportToPlanner.Commercial.Credits;

/// <summary>
/// Dispatches a verified payment provider confirmation to grant or adjustment use cases.
/// </summary>
public sealed class HandleVerifiedPaymentConfirmationUseCase(
    GrantPaidCreditsUseCase grantPaidCreditsUseCase,
    AdjustPaidCreditsForRefundUseCase adjustPaidCreditsForRefundUseCase)
{
    /// <summary>
    /// Handles a verified confirmation and returns whether the ledger was written successfully.
    /// </summary>
    public async Task<VerifiedPaymentHandleOutcome> HandleAsync(
        VerifiedPaymentConfirmation confirmation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(confirmation);
        cancellationToken.ThrowIfCancellationRequested();

        return confirmation.EventKind switch
        {
            VerifiedPaymentEventKind.PaymentConfirmed => await HandlePaymentConfirmedAsync(confirmation, cancellationToken)
                .ConfigureAwait(false),
            VerifiedPaymentEventKind.RefundOrDisputeConfirmed => await HandleRefundOrDisputeAsync(confirmation, cancellationToken)
                .ConfigureAwait(false),
            VerifiedPaymentEventKind.PaymentFailed or VerifiedPaymentEventKind.Ignored
                => new VerifiedPaymentHandleOutcome.AcknowledgedWithoutLedgerWrite(),
            _ => new VerifiedPaymentHandleOutcome.AcknowledgedWithoutLedgerWrite(),
        };
    }

    private async Task<VerifiedPaymentHandleOutcome> HandlePaymentConfirmedAsync(
        VerifiedPaymentConfirmation confirmation,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(confirmation.TenantId)
            || string.IsNullOrWhiteSpace(confirmation.PackId)
            || string.IsNullOrWhiteSpace(confirmation.ExternalPaymentId)
            || string.IsNullOrWhiteSpace(confirmation.ExternalEventId)
            || string.IsNullOrWhiteSpace(confirmation.ActorUserId))
        {
            return new VerifiedPaymentHandleOutcome.LedgerWriteFailed();
        }

        var outcome = await grantPaidCreditsUseCase.ExecuteAsync(
            new GrantPaidCreditsRequest(
                confirmation.TenantId,
                confirmation.PackId,
                confirmation.ExternalPaymentId,
                confirmation.ExternalEventId,
                confirmation.ActorUserId,
                confirmation.OccurredUtc),
            cancellationToken).ConfigureAwait(false);

        return outcome switch
        {
            GrantPaidCreditsOutcome.Granted => new VerifiedPaymentHandleOutcome.LedgerWriteSucceeded(),
            GrantPaidCreditsOutcome.AlreadyGranted => new VerifiedPaymentHandleOutcome.LedgerWriteSucceeded(),
            GrantPaidCreditsOutcome.Failed => new VerifiedPaymentHandleOutcome.LedgerWriteFailed(),
            _ => new VerifiedPaymentHandleOutcome.LedgerWriteFailed(),
        };
    }

    private async Task<VerifiedPaymentHandleOutcome> HandleRefundOrDisputeAsync(
        VerifiedPaymentConfirmation confirmation,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(confirmation.TenantId)
            || string.IsNullOrWhiteSpace(confirmation.ExternalPaymentId)
            || string.IsNullOrWhiteSpace(confirmation.ExternalAdjustmentId))
        {
            return new VerifiedPaymentHandleOutcome.LedgerWriteFailed();
        }

        var outcome = await adjustPaidCreditsForRefundUseCase.ExecuteAsync(
            new AdjustPaidCreditsForRefundRequest(
                confirmation.TenantId,
                confirmation.ExternalPaymentId,
                confirmation.ExternalAdjustmentId,
                confirmation.OccurredUtc),
            cancellationToken).ConfigureAwait(false);

        return outcome switch
        {
            AdjustPaidCreditsForRefundOutcome.Adjusted => new VerifiedPaymentHandleOutcome.LedgerWriteSucceeded(),
            AdjustPaidCreditsForRefundOutcome.AlreadyAdjusted => new VerifiedPaymentHandleOutcome.LedgerWriteSucceeded(),
            AdjustPaidCreditsForRefundOutcome.Failed => new VerifiedPaymentHandleOutcome.LedgerWriteFailed(),
            _ => new VerifiedPaymentHandleOutcome.LedgerWriteFailed(),
        };
    }
}

/// <summary>
/// Outcome of handling a verified payment provider confirmation.
/// </summary>
public abstract record VerifiedPaymentHandleOutcome
{
    /// <summary>
    /// Ledger write completed or duplicate delivery was safely ignored.
    /// </summary>
    public sealed record LedgerWriteSucceeded : VerifiedPaymentHandleOutcome;

    /// <summary>
    /// No ledger write was required.
    /// </summary>
    public sealed record AcknowledgedWithoutLedgerWrite : VerifiedPaymentHandleOutcome;

    /// <summary>
    /// A required ledger write failed.
    /// </summary>
    public sealed record LedgerWriteFailed : VerifiedPaymentHandleOutcome;
}
