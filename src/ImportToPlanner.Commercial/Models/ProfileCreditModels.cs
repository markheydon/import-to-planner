namespace ImportToPlanner.Commercial.Models;

/// <summary>
/// Profile activity row kind before UK English presentation.
/// </summary>
public enum ProfileCreditActivityKind
{
    /// <summary>
    /// Monthly free grant.
    /// </summary>
    FreeMonthlyGrant = 0,

    /// <summary>
    /// Unused free credits expired.
    /// </summary>
    FreeCreditExpiry = 1,

    /// <summary>
    /// Credits consumed by an import run (grouped).
    /// </summary>
    ImportRunUsage = 2,

    /// <summary>
    /// Paid credit purchase.
    /// </summary>
    PaidPurchase = 3,

    /// <summary>
    /// Paid lot expiry.
    /// </summary>
    PaidLotExpiry = 4,

    /// <summary>
    /// Refund or chargeback adjustment on unused paid credits.
    /// </summary>
    PaidAdjustment = 5,
}

/// <summary>
/// Request to load profile credit summary.
/// </summary>
/// <param name="TenantId">Commercial tenant identifier from session.</param>
/// <param name="ActorUserId">Session user for ensure audit.</param>
/// <param name="OccurredUtc">UTC instant for ensure and lot selection.</param>
public sealed record ProfileCreditSummaryRequest(
    string TenantId,
    string ActorUserId,
    DateTimeOffset OccurredUtc);

/// <summary>
/// Successful profile credit summary.
/// </summary>
/// <param name="RemainingCredits">Derived remaining credits when not complimentary.</param>
/// <param name="ComplimentaryUnlimitedAccess">Whether the tenant has complimentary unlimited credits.</param>
/// <param name="FreeMonthlyLot">Current-month free lot when applicable.</param>
/// <param name="PaidLots">Open paid lots (empty until paid purchases ship).</param>
/// <param name="EnsureExpiryApplied">Whether ensure applied expiry this call.</param>
/// <param name="EnsureGrantApplied">Whether ensure applied a grant this call.</param>
public sealed record ProfileCreditSummaryResult(
    int RemainingCredits,
    bool ComplimentaryUnlimitedAccess,
    ProfileFreeMonthlyLot? FreeMonthlyLot,
    IReadOnlyList<ProfilePaidLot> PaidLots,
    bool EnsureExpiryApplied,
    bool EnsureGrantApplied);

/// <summary>
/// Current free monthly lot snapshot for profile display.
/// </summary>
public sealed record ProfileFreeMonthlyLot(
    int GrantedQuantity,
    int RemainingQuantity,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset GrantedAtUtc);

/// <summary>
/// Open paid lot snapshot for profile display.
/// </summary>
public sealed record ProfilePaidLot(
    string LotId,
    int GrantedQuantity,
    int RemainingQuantity,
    DateTimeOffset GrantedAtUtc,
    DateTimeOffset ExpiresAtUtc);

/// <summary>
/// Request to load profile credit activity.
/// </summary>
/// <param name="TenantId">Commercial tenant identifier from session.</param>
/// <param name="ActorUserId">Session user for audit.</param>
/// <param name="OccurredUtc">UTC instant for the rolling activity window.</param>
public sealed record ProfileCreditActivityRequest(
    string TenantId,
    string ActorUserId,
    DateTimeOffset OccurredUtc);

/// <summary>
/// Profile credit activity within the rolling twelve-month window.
/// </summary>
/// <param name="Entries">Activity rows newest-first.</param>
/// <param name="HasOlderHistory">Whether ledger events exist before the window.</param>
/// <param name="ActivityWindowStartUtc">Inclusive window start (<c>OccurredUtc.AddMonths(-12)</c>).</param>
public sealed record ProfileCreditActivityResult(
    IReadOnlyList<ProfileCreditActivityEntry> Entries,
    bool HasOlderHistory,
    DateTimeOffset ActivityWindowStartUtc);

/// <summary>
/// One profile activity row.
/// </summary>
public sealed record ProfileCreditActivityEntry(
    DateTimeOffset OccurredUtc,
    ProfileCreditActivityKind ActivityKind,
    int Quantity,
    string? ImportRunId,
    bool IsCreditIncrease);

/// <summary>
/// Discriminated profile summary outcome.
/// </summary>
public abstract record GetProfileCreditSummaryOutcome
{
    /// <summary>
    /// Summary loaded successfully.
    /// </summary>
    /// <param name="Result">Summary snapshot.</param>
    public sealed record Succeeded(ProfileCreditSummaryResult Result) : GetProfileCreditSummaryOutcome;

    /// <summary>
    /// Summary load failed.
    /// </summary>
    /// <param name="Failure">Structured failure.</param>
    public sealed record Failed(CommercialCreditBalanceFailure Failure) : GetProfileCreditSummaryOutcome;
}

/// <summary>
/// Discriminated profile activity outcome.
/// </summary>
public abstract record GetProfileCreditActivityOutcome
{
    /// <summary>
    /// Activity loaded successfully.
    /// </summary>
    /// <param name="Result">Activity snapshot.</param>
    public sealed record Succeeded(ProfileCreditActivityResult Result) : GetProfileCreditActivityOutcome;

    /// <summary>
    /// Activity load failed.
    /// </summary>
    /// <param name="Failure">Structured failure.</param>
    public sealed record Failed(CommercialCreditBalanceFailure Failure) : GetProfileCreditActivityOutcome;
}
