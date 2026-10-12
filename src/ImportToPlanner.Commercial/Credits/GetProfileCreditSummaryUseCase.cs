using ImportToPlanner.Commercial.Abstractions;
using ImportToPlanner.Commercial.Models;

namespace ImportToPlanner.Commercial.Credits;

/// <summary>
/// Ensures balance and maps profile credit summary fields.
/// </summary>
public sealed class GetProfileCreditSummaryUseCase(
    IEnsureCurrentCreditBalanceUseCase ensureCurrentCreditBalanceUseCase,
    ICreditLedgerStore ledgerStore) : IGetProfileCreditSummaryUseCase
{
    /// <inheritdoc/>
    public async Task<GetProfileCreditSummaryOutcome> GetAsync(
        ProfileCreditSummaryRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(request.TenantId) || string.IsNullOrWhiteSpace(request.ActorUserId))
        {
            return new GetProfileCreditSummaryOutcome.Failed(
                new CommercialCreditBalanceFailure(CommercialCreditFailureCodes.LedgerUnavailable));
        }

        var ensureOutcome = await ensureCurrentCreditBalanceUseCase.EnsureAsync(
            new EnsureCurrentCreditBalanceRequest(
                request.TenantId,
                request.ActorUserId,
                request.OccurredUtc,
                EnsureBalanceReason.Profile),
            cancellationToken).ConfigureAwait(false);

        if (ensureOutcome is EnsureCurrentCreditBalanceOutcome.Failed failed)
        {
            return new GetProfileCreditSummaryOutcome.Failed(failed.Failure);
        }

        var balance = ((EnsureCurrentCreditBalanceOutcome.Succeeded)ensureOutcome).Result;
        if (balance.ComplimentaryUnlimitedAccess)
        {
            return new GetProfileCreditSummaryOutcome.Succeeded(
                new ProfileCreditSummaryResult(
                    0,
                    true,
                    null,
                    [],
                    balance.ExpiryApplied,
                    balance.GrantApplied));
        }

        IReadOnlyList<CreditLot> lots;
        try
        {
            lots = await ledgerStore.GetLotsAsync(request.TenantId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            return new GetProfileCreditSummaryOutcome.Failed(
                new CommercialCreditBalanceFailure(CommercialCreditFailureCodes.LedgerUnavailable));
        }

        var utcYearMonth = CommercialCreditPolicy.GetUtcYearMonth(request.OccurredUtc);
        var currentFreeLot = lots
            .Where(lot => lot.LotType == LotType.FreeMonthly
                && CommercialCreditPolicy.GetUtcYearMonth(lot.GrantedAtUtc) == utcYearMonth
                && lot.GrantedQuantity > 0)
            .OrderByDescending(lot => lot.GrantedAtUtc)
            .FirstOrDefault();

        ProfileFreeMonthlyLot? freeMonthlyLot = currentFreeLot is null
            ? null
            : new ProfileFreeMonthlyLot(
                currentFreeLot.GrantedQuantity,
                currentFreeLot.RemainingQuantity,
                currentFreeLot.ExpiresAtUtc,
                currentFreeLot.GrantedAtUtc);

        var paidLots = lots
            .Where(lot => lot.LotType == LotType.Paid && CreditLotSelector.IsConsumable(lot, request.OccurredUtc))
            .OrderByDescending(lot => lot.GrantedAtUtc)
            .Select(lot => new ProfilePaidLot(
                lot.LotId,
                lot.GrantedQuantity,
                lot.RemainingQuantity,
                lot.GrantedAtUtc,
                lot.ExpiresAtUtc))
            .ToList();

        return new GetProfileCreditSummaryOutcome.Succeeded(
            new ProfileCreditSummaryResult(
                balance.RemainingCredits,
                false,
                freeMonthlyLot,
                paidLots,
                balance.ExpiryApplied,
                balance.GrantApplied));
    }
}
