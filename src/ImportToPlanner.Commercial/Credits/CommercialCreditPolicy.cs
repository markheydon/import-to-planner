namespace ImportToPlanner.Commercial.Credits;

/// <summary>
/// V1 commercial credit policy constants and UTC calendar helpers.
/// </summary>
public static class CommercialCreditPolicy
{
    /// <summary>
    /// Free monthly allowance for hosted commercial tenants (not prorated).
    /// </summary>
    public const int FreeMonthlyAllowance = 25;

    /// <summary>
    /// Returns the UTC year-month key (<c>yyyyMM</c>) for the supplied instant.
    /// </summary>
    public static string GetUtcYearMonth(DateTimeOffset occurredUtc)
        => occurredUtc.UtcDateTime.ToString("yyyyMM", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>
    /// Returns the first instant of the UTC calendar month containing <paramref name="occurredUtc"/>.
    /// </summary>
    public static DateTimeOffset GetUtcMonthStart(DateTimeOffset occurredUtc)
    {
        var utc = occurredUtc.UtcDateTime;
        return new DateTimeOffset(utc.Year, utc.Month, 1, 0, 0, 0, TimeSpan.Zero);
    }

    /// <summary>
    /// Returns the exclusive expiry instant for a free monthly lot granted at <paramref name="grantedAtUtc"/>.
    /// </summary>
    public static DateTimeOffset GetFreeLotExpiresAtUtc(DateTimeOffset grantedAtUtc)
        => GetUtcMonthStart(grantedAtUtc).AddMonths(1);
}
