using ImportToPlanner.Commercial.Abstractions;
using ImportToPlanner.Commercial.Models;

namespace ImportToPlanner.Commercial.Credits;

/// <summary>
/// Removes unused paid credits after a confirmed refund or lost dispute.
/// </summary>
public sealed class AdjustPaidCreditsForRefundUseCase(ICreditLedgerStore ledgerStore)
{
    /// <summary>
    /// Attempts to adjust paid credits for a refund or chargeback.
    /// </summary>
    public async Task<AdjustPaidCreditsForRefundOutcome> ExecuteAsync(
        AdjustPaidCreditsForRefundRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(request.TenantId)
            || string.IsNullOrWhiteSpace(request.ExternalPaymentId)
            || string.IsNullOrWhiteSpace(request.ExternalAdjustmentId))
        {
            return new AdjustPaidCreditsForRefundOutcome.Failed(PaidCreditPurchaseFailureCodes.LedgerCommitFailed);
        }

        try
        {
            var lot = await ledgerStore
                .GetPaidLotByExternalPaymentIdAsync(request.TenantId, request.ExternalPaymentId, cancellationToken)
                .ConfigureAwait(false);
            if (lot is null)
            {
                return new AdjustPaidCreditsForRefundOutcome.Failed(PaidCreditPurchaseFailureCodes.PaidLotNotFound);
            }

            var outcome = await ledgerStore.TryAdjustPaidCreditsAsync(
                request.TenantId,
                request.ExternalPaymentId,
                request.ExternalAdjustmentId,
                request.OccurredUtc,
                cancellationToken).ConfigureAwait(false);

            return outcome switch
            {
                PaidCreditAdjustmentAttemptOutcome.Applied => new AdjustPaidCreditsForRefundOutcome.Adjusted(),
                PaidCreditAdjustmentAttemptOutcome.AlreadyAdjusted => new AdjustPaidCreditsForRefundOutcome.AlreadyAdjusted(),
                PaidCreditAdjustmentAttemptOutcome.Failure failure => new AdjustPaidCreditsForRefundOutcome.Failed(failure.FailureCode),
                _ => new AdjustPaidCreditsForRefundOutcome.Failed(PaidCreditPurchaseFailureCodes.LedgerCommitFailed),
            };
        }
        catch (Exception)
        {
            return new AdjustPaidCreditsForRefundOutcome.Failed(PaidCreditPurchaseFailureCodes.LedgerCommitFailed);
        }
    }
}
