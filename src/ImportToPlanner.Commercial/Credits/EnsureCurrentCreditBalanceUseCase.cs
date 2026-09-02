using ImportToPlanner.Commercial.Abstractions;
using ImportToPlanner.Commercial.Models;

namespace ImportToPlanner.Commercial.Credits;

/// <summary>
/// Lazy expiry and monthly free grant with derived remaining balance.
/// </summary>
public sealed class EnsureCurrentCreditBalanceUseCase(ICreditLedgerStore ledgerStore) : IEnsureCurrentCreditBalanceUseCase
{
    /// <inheritdoc/>
    /// <remarks>
    /// <see cref="EnsureCurrentCreditBalanceRequest.Reason"/> is recorded for audit and caller policy only;
    /// grant, expiry, and balance derivation behave the same for every reason.
    /// </remarks>
    public async Task<EnsureCurrentCreditBalanceOutcome> EnsureAsync(
        EnsureCurrentCreditBalanceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(request.TenantId))
        {
            return new EnsureCurrentCreditBalanceOutcome.Failed(
                new CommercialCreditBalanceFailure(CommercialCreditFailureCodes.LedgerUnavailable));
        }

        var expiryApplied = false;
        var grantApplied = false;
        var currentMonthStart = CommercialCreditPolicy.GetUtcMonthStart(request.OccurredUtc);

        IReadOnlyList<CreditLot> lots;
        try
        {
            lots = await ledgerStore.GetLotsAsync(request.TenantId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            return new EnsureCurrentCreditBalanceOutcome.Failed(
                new CommercialCreditBalanceFailure(CommercialCreditFailureCodes.LedgerUnavailable));
        }

        foreach (var lot in lots.Where(lot => lot.LotType == LotType.FreeMonthly && lot.RemainingQuantity > 0))
        {
            if (lot.ExpiresAtUtc > currentMonthStart)
            {
                continue;
            }

            try
            {
                var expired = await ledgerStore.ExpireFreeLotAsync(
                    lot,
                    request.OccurredUtc,
                    request.ActorUserId,
                    cancellationToken).ConfigureAwait(false);
                if (!expired)
                {
                    return new EnsureCurrentCreditBalanceOutcome.Failed(
                        new CommercialCreditBalanceFailure(CommercialCreditFailureCodes.ExpiryFailed));
                }

                expiryApplied = true;
            }
            catch (Exception)
            {
                return new EnsureCurrentCreditBalanceOutcome.Failed(
                    new CommercialCreditBalanceFailure(CommercialCreditFailureCodes.ExpiryFailed));
            }
        }

        var utcYearMonth = CommercialCreditPolicy.GetUtcYearMonth(request.OccurredUtc);
        var hasMarker = await ledgerStore.HasMonthGrantMarkerAsync(request.TenantId, utcYearMonth, cancellationToken)
            .ConfigureAwait(false);
        if (!hasMarker)
        {
            var grantOutcome = await ledgerStore.TryGrantFreeMonthlyAsync(
                request.TenantId,
                utcYearMonth,
                CommercialCreditPolicy.FreeMonthlyAllowance,
                request.OccurredUtc,
                request.ActorUserId,
                cancellationToken).ConfigureAwait(false);

            switch (grantOutcome)
            {
                case CreditGrantAttemptOutcome.Applied:
                    grantApplied = true;
                    break;
                case CreditGrantAttemptOutcome.AlreadyGranted:
                    break;
                case CreditGrantAttemptOutcome.Failure failure:
                    return new EnsureCurrentCreditBalanceOutcome.Failed(
                        new CommercialCreditBalanceFailure(failure.FailureCode));
            }
        }

        try
        {
            lots = await ledgerStore.GetLotsAsync(request.TenantId, cancellationToken).ConfigureAwait(false);
            var (remaining, freeRemaining, paidRemaining) = DeriveBalance(lots);
            return new EnsureCurrentCreditBalanceOutcome.Succeeded(
                CommercialCreditBalanceResult.Success(
                    remaining,
                    freeRemaining,
                    paidRemaining,
                    expiryApplied,
                    grantApplied));
        }
        catch (Exception)
        {
            return new EnsureCurrentCreditBalanceOutcome.Failed(
                new CommercialCreditBalanceFailure(CommercialCreditFailureCodes.LedgerUnavailable));
        }
    }

    internal static (int Remaining, int FreeRemaining, int PaidRemaining) DeriveBalance(IReadOnlyList<CreditLot> lots)
    {
        var freeRemaining = lots
            .Where(lot => lot.LotType == LotType.FreeMonthly)
            .Sum(lot => lot.RemainingQuantity);
        var paidRemaining = lots
            .Where(lot => lot.LotType == LotType.Paid)
            .Sum(lot => lot.RemainingQuantity);
        return (freeRemaining + paidRemaining, freeRemaining, paidRemaining);
    }
}
