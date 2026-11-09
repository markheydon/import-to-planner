using ImportToPlanner.Commercial.Abstractions;
using ImportToPlanner.Commercial.Configuration;
using ImportToPlanner.Commercial.Models;
using Microsoft.Extensions.Options;

namespace ImportToPlanner.Commercial.Credits;

/// <summary>
/// Grants a paid credit lot after verified payment confirmation.
/// </summary>
public sealed class GrantPaidCreditsUseCase(
    ICreditLedgerStore ledgerStore,
    IOptions<StripeOptions> stripeOptions)
{
    /// <summary>
    /// Attempts to grant paid credits for a confirmed payment.
    /// </summary>
    public async Task<GrantPaidCreditsOutcome> ExecuteAsync(
        GrantPaidCreditsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(request.TenantId)
            || string.IsNullOrWhiteSpace(request.PackId)
            || string.IsNullOrWhiteSpace(request.ExternalPaymentId)
            || string.IsNullOrWhiteSpace(request.ExternalEventId)
            || string.IsNullOrWhiteSpace(request.ActorUserId))
        {
            return new GrantPaidCreditsOutcome.Failed(PaidCreditPurchaseFailureCodes.LedgerCommitFailed);
        }

        var pack = stripeOptions.Value.CreditPacks
            .FirstOrDefault(candidate =>
                candidate.Published
                && string.Equals(candidate.PackId, request.PackId, StringComparison.Ordinal));
        if (pack is null || pack.CreditQuantity <= 0)
        {
            return new GrantPaidCreditsOutcome.Failed(PaidCreditPurchaseFailureCodes.PackUnavailable);
        }

        try
        {
            var outcome = await ledgerStore.TryGrantPaidPurchaseAsync(
                request.TenantId,
                request.ExternalPaymentId,
                request.ExternalEventId,
                pack.CreditQuantity,
                request.OccurredUtc,
                request.ActorUserId,
                cancellationToken).ConfigureAwait(false);

            return outcome switch
            {
                CreditGrantAttemptOutcome.Applied => new GrantPaidCreditsOutcome.Granted(),
                CreditGrantAttemptOutcome.AlreadyGranted => new GrantPaidCreditsOutcome.AlreadyGranted(),
                CreditGrantAttemptOutcome.Failure => new GrantPaidCreditsOutcome.Failed(PaidCreditPurchaseFailureCodes.LedgerCommitFailed),
                _ => new GrantPaidCreditsOutcome.Failed(PaidCreditPurchaseFailureCodes.LedgerCommitFailed),
            };
        }
        catch (Exception)
        {
            return new GrantPaidCreditsOutcome.Failed(PaidCreditPurchaseFailureCodes.LedgerCommitFailed);
        }
    }
}
