namespace ImportToPlanner.Commercial.Models;

/// <summary>
/// Credit lot allocation type.
/// </summary>
public enum LotType
{
    /// <summary>
    /// Monthly free allowance lot.
    /// </summary>
    FreeMonthly = 0,

    /// <summary>
    /// Reserved for paid purchases (not used in this increment).
    /// </summary>
    Paid = 1,
}

/// <summary>
/// Immutable credit ledger entry type.
/// </summary>
public enum CreditEntryType
{
    /// <summary>
    /// Monthly free grant.
    /// </summary>
    FreeGrant = 0,

    /// <summary>
    /// Credit consumed by a successfully created task.
    /// </summary>
    Usage = 1,

    /// <summary>
    /// Unused free credits expired at month boundary.
    /// </summary>
    FreeExpiry = 2,

    /// <summary>
    /// Reserved for paid purchases (not written in this increment).
    /// </summary>
    PaidPurchase = 3,

    /// <summary>
    /// Reserved for paid lot expiry (not written in this increment).
    /// </summary>
    PaidExpiry = 4,
}

/// <summary>
/// Reason a balance ensure was requested.
/// </summary>
public enum EnsureBalanceReason
{
    /// <summary>
    /// Successful commercial sign-in.
    /// </summary>
    SignIn = 0,

    /// <summary>
    /// Import preview generated.
    /// </summary>
    Preview = 1,

    /// <summary>
    /// Import confirm requested.
    /// </summary>
    Confirm = 2,

    /// <summary>
    /// Import execution started.
    /// </summary>
    Execute = 3,
}
