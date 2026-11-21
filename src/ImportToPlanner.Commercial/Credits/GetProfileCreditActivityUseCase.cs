using ImportToPlanner.Commercial.Abstractions;
using ImportToPlanner.Commercial.Models;

namespace ImportToPlanner.Commercial.Credits;

/// <summary>
/// Lists and aggregates ledger transactions for profile activity display.
/// </summary>
public sealed class GetProfileCreditActivityUseCase(
    ICreditLedgerStore ledgerStore,
    IComplimentaryTenantPolicy complimentaryTenantPolicy) : IGetProfileCreditActivityUseCase
{
    /// <inheritdoc/>
    public async Task<GetProfileCreditActivityOutcome> GetAsync(
        ProfileCreditActivityRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(request.TenantId) || string.IsNullOrWhiteSpace(request.ActorUserId))
        {
            return new GetProfileCreditActivityOutcome.Failed(
                new CommercialCreditBalanceFailure(CommercialCreditFailureCodes.LedgerUnavailable));
        }

        var windowStartUtc = request.OccurredUtc.AddMonths(-12);
        var isComplimentary = complimentaryTenantPolicy.IsComplimentary(request.TenantId);

        IReadOnlyList<CreditLedgerTransaction> transactions;
        try
        {
            transactions = await ledgerStore
                .ListTransactionsAsync(request.TenantId, windowStartUtc, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception)
        {
            return new GetProfileCreditActivityOutcome.Failed(
                new CommercialCreditBalanceFailure(CommercialCreditFailureCodes.LedgerUnavailable));
        }

        bool hasOlderHistory;
        try
        {
            hasOlderHistory = await ledgerStore
                .HasTransactionsBeforeAsync(request.TenantId, windowStartUtc, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception)
        {
            return new GetProfileCreditActivityOutcome.Failed(
                new CommercialCreditBalanceFailure(CommercialCreditFailureCodes.LedgerUnavailable));
        }

        var entries = new List<ProfileCreditActivityEntry>();
        var usageTransactions = transactions.Where(transaction => transaction.EntryType == CreditEntryType.Usage).ToList();

        if (!isComplimentary)
        {
            foreach (var transaction in transactions)
            {
                if (transaction.EntryType == CreditEntryType.Usage
                    || transaction.EntryType == CreditEntryType.ComplimentaryGrant)
                {
                    continue;
                }

                var mapped = MapNonUsageEntry(transaction);
                if (mapped is not null)
                {
                    entries.Add(mapped);
                }
            }
        }

        foreach (var group in usageTransactions.GroupBy(GetUsageGroupKey))
        {
            var latest = group.OrderByDescending(transaction => transaction.OccurredUtc).First();
            entries.Add(new ProfileCreditActivityEntry(
                latest.OccurredUtc,
                ProfileCreditActivityKind.ImportRunUsage,
                group.Sum(transaction => transaction.Quantity),
                latest.ImportRunId,
                false));
        }

        entries.Sort((left, right) => right.OccurredUtc.CompareTo(left.OccurredUtc));

        return new GetProfileCreditActivityOutcome.Succeeded(
            new ProfileCreditActivityResult(entries, hasOlderHistory, windowStartUtc));
    }

    private static string GetUsageGroupKey(CreditLedgerTransaction transaction)
        => string.IsNullOrWhiteSpace(transaction.ImportRunId)
            ? transaction.TransactionId
            : transaction.ImportRunId;

    private static ProfileCreditActivityEntry? MapNonUsageEntry(CreditLedgerTransaction transaction)
    {
        return transaction.EntryType switch
        {
            CreditEntryType.FreeGrant => new ProfileCreditActivityEntry(
                transaction.OccurredUtc,
                ProfileCreditActivityKind.FreeMonthlyGrant,
                transaction.Quantity,
                null,
                true),
            CreditEntryType.FreeExpiry => new ProfileCreditActivityEntry(
                transaction.OccurredUtc,
                ProfileCreditActivityKind.FreeCreditExpiry,
                transaction.Quantity,
                null,
                false),
            CreditEntryType.PaidPurchase => new ProfileCreditActivityEntry(
                transaction.OccurredUtc,
                ProfileCreditActivityKind.PaidPurchase,
                transaction.Quantity,
                null,
                true),
            CreditEntryType.PaidExpiry => new ProfileCreditActivityEntry(
                transaction.OccurredUtc,
                ProfileCreditActivityKind.PaidLotExpiry,
                transaction.Quantity,
                null,
                false),
            CreditEntryType.PaidAdjustment => new ProfileCreditActivityEntry(
                transaction.OccurredUtc,
                ProfileCreditActivityKind.PaidAdjustment,
                transaction.Quantity,
                null,
                false),
            _ => null,
        };
    }
}
