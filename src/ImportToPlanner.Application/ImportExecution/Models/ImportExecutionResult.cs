using ImportToPlanner.Application.Common.Models;

namespace ImportToPlanner.Application.ImportExecution.Models;

/// <summary>
/// Represents the final import execution outcome.
/// </summary>
public sealed record ImportExecutionResult
{
    /// <summary>
    /// Gets or sets the plan identifier of the reused plan.
    /// </summary>
    public string? PlanId { get; init; }

    /// <summary>
    /// Gets or sets the created items.
    /// </summary>
    public required IReadOnlyList<ImportExecutionItem> CreatedItems { get; init; }

    /// <summary>
    /// Gets or sets the reused or skipped items.
    /// </summary>
    public required IReadOnlyList<ImportExecutionItem> ReusedOrSkippedItems { get; init; }

    /// <summary>
    /// Gets or sets the execution failures.
    /// </summary>
    public required IReadOnlyList<PlannerOperationFailure> FailureItems { get; init; }

    /// <summary>
    /// Gets or sets the post-import manual actions.
    /// </summary>
    public required IReadOnlyList<ManualAction> ManualActions { get; init; }

    /// <summary>
    /// Gets or sets the aggregate execution outcome summary.
    /// </summary>
    public required ImportExecutionOutcomeSummary OutcomeSummary { get; init; }
}

/// <summary>
/// Represents a neutral execution item.
/// </summary>
/// <param name="Target">The item target type.</param>
/// <param name="Name">The item display name.</param>
/// <param name="Reference">Optional stable reference.</param>
public sealed record ImportExecutionItem(
    PlannerFailureTarget Target,
    string Name,
    string? Reference = null);

/// <summary>
/// Represents aggregate execution outcome counters for reporting consistency.
/// </summary>
/// <param name="CreatedCount">The number of created items.</param>
/// <param name="ReusedOrSkippedCount">The number of reused or skipped items.</param>
/// <param name="ErrorCount">The number of failed items.</param>
/// <param name="ManualActionCount">The number of manual follow-up actions.</param>
/// <param name="IsPartialSuccess">Indicates whether the run completed with both successes and failures.</param>
/// <param name="IsFullFailure">Indicates whether the run produced no successful outcomes and at least one error.</param>
public sealed record ImportExecutionOutcomeSummary(
    int CreatedCount,
    int ReusedOrSkippedCount,
    int ErrorCount,
    int ManualActionCount,
    bool IsPartialSuccess,
    bool IsFullFailure);
