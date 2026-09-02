using ImportToPlanner.Commercial.Models;

namespace ImportToPlanner.Commercial.Abstractions;

/// <summary>
/// Persistence contract for the commercial credit ledger.
/// </summary>
public interface ICreditLedgerStore
{
    /// <summary>
    /// Loads all credit lots for a tenant.
    /// </summary>
    Task<IReadOnlyList<CreditLot>> GetLotsAsync(string tenantId, CancellationToken cancellationToken);

    /// <summary>
    /// Returns whether a month grant marker exists for the tenant and UTC month.
    /// </summary>
    Task<bool> HasMonthGrantMarkerAsync(
        string tenantId,
        string utcYearMonth,
        CancellationToken cancellationToken);

    /// <summary>
    /// Expires leftover quantity on a free lot and appends an expiry transaction.
    /// </summary>
    Task<bool> ExpireFreeLotAsync(
        CreditLot lot,
        DateTimeOffset occurredUtc,
        string? actorUserId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to grant the monthly free allowance using a month marker for idempotency.
    /// </summary>
    Task<CreditGrantAttemptOutcome> TryGrantFreeMonthlyAsync(
        string tenantId,
        string utcYearMonth,
        int grantQuantity,
        DateTimeOffset grantedAtUtc,
        string? actorUserId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Records one credit usage against an open lot.
    /// </summary>
    Task<RecordCreditUsageOutcome> RecordUsageAsync(
        RecordCreditUsageRequest request,
        CancellationToken cancellationToken);
}

/// <summary>
/// Ensures lazy expiry and monthly grant, returning live remaining credits.
/// </summary>
public interface IEnsureCurrentCreditBalanceUseCase
{
    /// <summary>
    /// Ensures the tenant balance is current for the supplied UTC instant.
    /// </summary>
    Task<EnsureCurrentCreditBalanceOutcome> EnsureAsync(
        EnsureCurrentCreditBalanceRequest request,
        CancellationToken cancellationToken);
}
