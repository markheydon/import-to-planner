namespace ImportToPlanner.Application.Abstractions;

/// <summary>
/// Quota decision before starting a Planner task create during import execution.
/// </summary>
public enum TaskCreationQuotaStatus
{
    /// <summary>
    /// Create may proceed.
    /// </summary>
    Allow = 0,

    /// <summary>
    /// No credits remain for further creates.
    /// </summary>
    Exhausted = 1,

    /// <summary>
    /// Credit ledger is unavailable; fail closed.
    /// </summary>
    Unavailable = 2,
}

/// <summary>
/// Result of a pre-create quota check.
/// </summary>
/// <param name="Status">Quota decision.</param>
/// <param name="DiagnosticCode">Optional structured diagnostic code.</param>
/// <param name="RemainingCredits">Remaining credits when known.</param>
public sealed record TaskCreationQuotaResult(
    TaskCreationQuotaStatus Status,
    string? DiagnosticCode = null,
    int? RemainingCredits = null);

/// <summary>
/// Result of recording a successful task create against the credit ledger.
/// </summary>
/// <param name="Succeeded">Whether usage was recorded.</param>
/// <param name="DiagnosticCode">Optional structured diagnostic code when recording failed.</param>
/// <param name="RemainingCredits">Remaining credits when known after recording.</param>
public sealed record TaskCreationQuotaRecordResult(
    bool Succeeded,
    string? DiagnosticCode = null,
    int? RemainingCredits = null);

/// <summary>
/// Context for import execution credit metering.
/// </summary>
/// <param name="TenantId">Commercial tenant identifier.</param>
/// <param name="ActorUserId">Session user for audit.</param>
/// <param name="OccurredUtc">Operation timestamp.</param>
/// <param name="ImportRunId">Stable import run identifier.</param>
/// <param name="TaskName">Task display name for diagnostics.</param>
/// <param name="CreatedPlannerTaskId">Planner task identifier after create succeeds.</param>
public sealed record ImportTaskCreationQuotaContext(
    string TenantId,
    string ActorUserId,
    DateTimeOffset OccurredUtc,
    string ImportRunId,
    string TaskName,
    string? CreatedPlannerTaskId = null);

/// <summary>
/// Technology-neutral port for stopping the import create loop on credit limits.
/// </summary>
public interface IImportTaskCreationQuota
{
    /// <summary>
    /// Checks whether another task create may start.
    /// </summary>
    Task<TaskCreationQuotaResult> BeforeCreateAsync(
        ImportTaskCreationQuotaContext context,
        CancellationToken cancellationToken);

    /// <summary>
    /// Records one credit usage after a successful Planner task create.
    /// </summary>
    Task<TaskCreationQuotaRecordResult> RecordSuccessfulCreateAsync(
        ImportTaskCreationQuotaContext context,
        CancellationToken cancellationToken);
}
