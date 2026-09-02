using ImportToPlanner.Commercial.Models;

namespace ImportToPlanner.Commercial.Credits;

/// <summary>
/// Shared lot selection and balance helpers for credit consumption.
/// </summary>
internal static class CreditLotSelector
{
    /// <summary>
    /// Returns whether a lot can still be consumed at the supplied instant.
    /// </summary>
    public static bool IsConsumable(CreditLot lot, DateTimeOffset occurredUtc)
        => lot.RemainingQuantity > 0 && lot.ExpiresAtUtc > occurredUtc;

    /// <summary>
    /// Selects the next consumable lot using free-first then oldest-first ordering.
    /// </summary>
    public static CreditLot? SelectConsumableLot(IReadOnlyList<CreditLot> lots, DateTimeOffset occurredUtc)
        => lots
            .Where(lot => IsConsumable(lot, occurredUtc))
            .OrderBy(lot => lot.LotType == LotType.FreeMonthly ? 0 : 1)
            .ThenBy(lot => lot.GrantedAtUtc)
            .FirstOrDefault();

    /// <summary>
    /// Sums remaining quantity across consumable lots at the supplied instant.
    /// </summary>
    public static int SumConsumableRemaining(IReadOnlyList<CreditLot> lots, DateTimeOffset occurredUtc)
        => lots.Where(lot => IsConsumable(lot, occurredUtc)).Sum(lot => lot.RemainingQuantity);
}
