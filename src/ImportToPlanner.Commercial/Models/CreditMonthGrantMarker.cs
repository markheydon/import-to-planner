namespace ImportToPlanner.Commercial.Models;

/// <summary>
/// Insert-only uniqueness marker for one free grant per tenant per UTC month.
/// </summary>
/// <param name="TenantId">Owning tenant partition key.</param>
/// <param name="UtcYearMonth"><c>yyyyMM</c> in UTC.</param>
/// <param name="GrantedAtUtc">Instant of the winning insert.</param>
/// <param name="LotId">Lot created with the grant.</param>
public sealed record CreditMonthGrantMarker(
    string TenantId,
    string UtcYearMonth,
    DateTimeOffset GrantedAtUtc,
    string LotId);
