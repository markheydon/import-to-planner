namespace ImportToPlanner.Application.Models;

/// <summary>
/// Represents the final import execution report.
/// </summary>
public sealed record ImportExecutionResult
{
    /// <summary>
    /// Gets or sets the plan identifier of the created or reused plan.
    /// </summary>
    public string? PlanId { get; init; }

    /// <summary>
    /// Gets or sets the created item descriptions.
    /// </summary>
    public required IReadOnlyList<string> Created { get; init; }

    /// <summary>
    /// Gets or sets the reused or skipped item descriptions.
    /// </summary>
    public required IReadOnlyList<string> ReusedOrSkipped { get; init; }

    /// <summary>
    /// Gets or sets the execution errors.
    /// </summary>
    public required IReadOnlyList<string> Errors { get; init; }

    /// <summary>
    /// Gets or sets the post-import manual actions.
    /// </summary>
    public required IReadOnlyList<ManualAction> ManualActions { get; init; }

    /// <summary>
    /// Gets or sets the aggregate execution outcome summary.
    /// </summary>
    public ImportExecutionOutcomeSummary? OutcomeSummary { get; init; }
}

/// <summary>
/// Represents aggregate execution outcome counters for reporting consistency.
/// </summary>
/// <param name="CreatedCount">The number of created items.</param>
/// <param name="ReusedOrSkippedCount">The number of reused or skipped items.</param>
/// <param name="ErrorCount">The number of failed items.</param>
/// <param name="ManualActionCount">The number of manual follow-up actions.</param>
/// <param name="IsPartialSuccess">Indicates whether the run completed with both successes and failures.</param>
public sealed record ImportExecutionOutcomeSummary(
    int CreatedCount,
    int ReusedOrSkippedCount,
    int ErrorCount,
    int ManualActionCount,
    bool IsPartialSuccess);
