namespace ImportToPlanner.Commercial.Models;

/// <summary>
/// A dated allocation of credits for a commercial tenant.
/// </summary>
/// <param name="LotId">Stable lot identifier.</param>
/// <param name="TenantId">Owning tenant partition key.</param>
/// <param name="LotType">Lot allocation type.</param>
/// <param name="GrantedQuantity">Original granted quantity.</param>
/// <param name="RemainingQuantity">Remaining quantity (0 when fully used or expired).</param>
/// <param name="GrantedAtUtc">Actual grant instant.</param>
/// <param name="ExpiresAtUtc">Exclusive expiry instant.</param>
public sealed record CreditLot(
    string LotId,
    string TenantId,
    LotType LotType,
    int GrantedQuantity,
    int RemainingQuantity,
    DateTimeOffset GrantedAtUtc,
    DateTimeOffset ExpiresAtUtc);
