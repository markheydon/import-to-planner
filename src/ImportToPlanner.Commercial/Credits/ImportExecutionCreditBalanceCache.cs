namespace ImportToPlanner.Commercial.Credits;

/// <summary>
/// Per-request cache of ledger-derived remaining credits for a single import execution.
/// </summary>
public sealed class ImportExecutionCreditBalanceCache
{
    private string? cachedTenantId;
    private string? cachedUtcYearMonth;
    private int? cachedRemainingCredits;

    /// <summary>
    /// Returns cached remaining credits when the tenant and UTC month match.
    /// </summary>
    public bool TryGetRemaining(
        string tenantId,
        DateTimeOffset occurredUtc,
        out int remainingCredits)
    {
        var utcYearMonth = CommercialCreditPolicy.GetUtcYearMonth(occurredUtc);
        if (cachedRemainingCredits is not null
            && string.Equals(cachedTenantId, tenantId, StringComparison.Ordinal)
            && string.Equals(cachedUtcYearMonth, utcYearMonth, StringComparison.Ordinal))
        {
            remainingCredits = cachedRemainingCredits.Value;
            return true;
        }

        remainingCredits = 0;
        return false;
    }

    /// <summary>
    /// Stores remaining credits for the tenant and UTC month.
    /// </summary>
    public void SetRemaining(string tenantId, DateTimeOffset occurredUtc, int remainingCredits)
    {
        cachedTenantId = tenantId;
        cachedUtcYearMonth = CommercialCreditPolicy.GetUtcYearMonth(occurredUtc);
        cachedRemainingCredits = remainingCredits;
    }
}
