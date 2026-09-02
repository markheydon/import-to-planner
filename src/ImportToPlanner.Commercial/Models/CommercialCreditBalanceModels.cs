namespace ImportToPlanner.Commercial.Models;

/// <summary>
/// Stable failure codes for commercial credit ledger operations.
/// </summary>
public static class CommercialCreditFailureCodes
{
    /// <summary>
    /// Ledger storage is unavailable.
    /// </summary>
    public const string LedgerUnavailable = "credits.ledger_unavailable";

    /// <summary>
    /// Remaining credits could not be loaded for an execution report.
    /// </summary>
    public const string BalanceReportUnavailable = "credits.balance_report_unavailable";

    /// <summary>
    /// Free monthly grant could not be written.
    /// </summary>
    public const string GrantFailed = "credits.grant_failed";

    /// <summary>
    /// Free lot expiry could not be written.
    /// </summary>
    public const string ExpiryFailed = "credits.expiry_failed";

    /// <summary>
    /// Credits exhausted before a create could start.
    /// </summary>
    public const string Exhausted = "credits.exhausted";

    /// <summary>
    /// Usage could not be recorded after a successful Planner create.
    /// </summary>
    public const string UsageRecordFailed = "credits.usage_record_failed";
}

/// <summary>
/// Request to ensure the current credit balance for a tenant.
/// </summary>
/// <param name="TenantId">Commercial tenant identifier.</param>
/// <param name="ActorUserId">Session user for audit.</param>
/// <param name="OccurredUtc">Grant or expiry timestamp.</param>
/// <param name="Reason">Why balance ensure was requested.</param>
public sealed record EnsureCurrentCreditBalanceRequest(
    string TenantId,
    string ActorUserId,
    DateTimeOffset OccurredUtc,
    EnsureBalanceReason Reason);

/// <summary>
/// Successful ensure balance response.
/// </summary>
/// <param name="RemainingCredits">Derived remaining credits (≥ 0).</param>
/// <param name="FreeRemaining">Remaining free credits.</param>
/// <param name="PaidRemaining">Remaining paid credits (0 in V1).</param>
/// <param name="ExpiryApplied">Whether expiry was applied this call.</param>
/// <param name="GrantApplied">Whether a new monthly grant was applied this call.</param>
public sealed record CommercialCreditBalanceResult(
    int RemainingCredits,
    int FreeRemaining,
    int PaidRemaining,
    bool ExpiryApplied,
    bool GrantApplied)
{
    /// <summary>
    /// Creates a successful balance result.
    /// </summary>
    public static CommercialCreditBalanceResult Success(
        int remainingCredits,
        int freeRemaining,
        int paidRemaining,
        bool expiryApplied,
        bool grantApplied)
        => new(remainingCredits, freeRemaining, paidRemaining, expiryApplied, grantApplied);
}

/// <summary>
/// Failed ensure balance response.
/// </summary>
/// <param name="FailureCode">Structured failure code.</param>
public sealed record CommercialCreditBalanceFailure(string FailureCode);

/// <summary>
/// Discriminated ensure balance outcome.
/// </summary>
public abstract record EnsureCurrentCreditBalanceOutcome
{
    /// <summary>
    /// Successful balance ensure.
    /// </summary>
    /// <param name="Result">Balance snapshot.</param>
    public sealed record Succeeded(CommercialCreditBalanceResult Result) : EnsureCurrentCreditBalanceOutcome;

    /// <summary>
    /// Failed balance ensure.
    /// </summary>
    /// <param name="Failure">Structured failure.</param>
    public sealed record Failed(CommercialCreditBalanceFailure Failure) : EnsureCurrentCreditBalanceOutcome;
}

/// <summary>
/// Request to record credit usage after a successful task create.
/// </summary>
/// <param name="TenantId">Commercial tenant identifier.</param>
/// <param name="ActorUserId">Session user for audit.</param>
/// <param name="OccurredUtc">Usage timestamp.</param>
/// <param name="ImportRunId">Import run identifier.</param>
/// <param name="CreatedPlannerTaskId">Created Planner task identifier.</param>
public sealed record RecordCreditUsageRequest(
    string TenantId,
    string ActorUserId,
    DateTimeOffset OccurredUtc,
    string ImportRunId,
    string CreatedPlannerTaskId);

/// <summary>
/// Outcome of recording credit usage.
/// </summary>
public abstract record RecordCreditUsageOutcome
{
    /// <summary>
    /// Usage recorded successfully.
    /// </summary>
    /// <param name="RemainingCredits">Remaining credits after usage.</param>
    public sealed record Success(int RemainingCredits) : RecordCreditUsageOutcome;

    /// <summary>
    /// Usage recording failed.
    /// </summary>
    /// <param name="FailureCode">Structured failure code.</param>
    public sealed record Failure(string FailureCode) : RecordCreditUsageOutcome;
}

/// <summary>
/// Outcome of attempting a free monthly grant.
/// </summary>
public abstract record CreditGrantAttemptOutcome
{
    /// <summary>
    /// Grant was applied.
    /// </summary>
    public sealed record Applied : CreditGrantAttemptOutcome;

    /// <summary>
    /// Grant already existed for the month.
    /// </summary>
    public sealed record AlreadyGranted : CreditGrantAttemptOutcome;

    /// <summary>
    /// Grant attempt failed.
    /// </summary>
    /// <param name="FailureCode">Structured failure code.</param>
    public sealed record Failure(string FailureCode) : CreditGrantAttemptOutcome;
}
